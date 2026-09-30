using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Elder's Impulse System.
/// Uses EMA direction and MACD histogram to determine impulse.
/// Green (bullish): EMA rising + MACD histogram rising -> buy
/// Red (bearish): EMA falling + MACD histogram falling -> sell
/// </summary>
public class ElderImpulseStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private decimal _prevEma;
	private decimal _prevHistogram;
	private bool _hasPrevValues;
	private Order _pendingOrder;

	/// <summary>
	/// EMA period.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MacdFastPeriod { get => _macdFastPeriod.Value; set => _macdFastPeriod.Value = value; }
	public int MacdSlowPeriod { get => _macdSlowPeriod.Value; set => _macdSlowPeriod.Value = value; }
	public int MacdSignalPeriod { get => _macdSignalPeriod.Value; set => _macdSignalPeriod.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ElderImpulseStrategy"/>.
	/// </summary>
	public ElderImpulseStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 13).SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period for EMA calculation", "Indicators")
			.SetOptimize(8, 21, 3);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12).SetGreaterThanZero()
			.SetDisplay("MACD Fast Period", "Fast EMA period.", "Indicators");
		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26).SetGreaterThanZero()
			.SetDisplay("MACD Slow Period", "Slow EMA period.", "Indicators");
		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9).SetGreaterThanZero()
			.SetDisplay("MACD Signal Period", "Signal EMA period.", "Indicators");
		_stopLossPercent = Param(nameof(StopLossPercent), 2m).SetNotNegative()
			.SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection");
		OrderRegistering += order => _pendingOrder = order;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevEma = default;
		_prevHistogram = default;
		_hasPrevValues = default;
		_pendingOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var macdSignal = new MovingAverageConvergenceDivergenceSignal();
		macdSignal.Macd.ShortMa.Length = MacdFastPeriod;
		macdSignal.Macd.LongMa.Length = MacdSlowPeriod;
		macdSignal.SignalMa.Length = MacdSignalPeriod;
		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ema, macdSignal, ProcessCandle, false)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, macdSignal);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native protection runs before the callback, including between finished candles.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (emaValue.IsEmpty)
			return;

		var emaDec = emaValue.GetValue<decimal>();
		if (emaDec == 0)
			return;

		var macdTyped = (IMovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
			return;

		var histogram = macd - signal;

		if (!_hasPrevValues)
		{
			_hasPrevValues = true;
			_prevEma = emaDec;
			_prevHistogram = histogram;
			return;
		}

		// Both slopes must be strict; a flat EMA or histogram is neutral.
		var impulse = emaDec > _prevEma && histogram > _prevHistogram ? 1
			: emaDec < _prevEma && histogram < _prevHistogram ? -1 : 0;

		if (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
		{
			_prevEma = emaDec;
			_prevHistogram = histogram;
			return;
		}

		// A qualified opposite entry reverses fully; otherwise loss of the held color exits only.
		if (impulse == 1 && candle.ClosePrice > emaDec && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (impulse == -1 && candle.ClosePrice < emaDec && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
		else if (Position > 0m && impulse != 1)
			SellMarket(Position);
		else if (Position < 0m && impulse != -1)
			BuyMarket(Math.Abs(Position));

		_prevEma = emaDec;
		_prevHistogram = histogram;
	}
}
