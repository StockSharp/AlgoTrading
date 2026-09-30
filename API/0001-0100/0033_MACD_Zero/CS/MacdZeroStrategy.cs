using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Enters while the MACD line approaches zero, before reaching it.
/// Exits on a signal-line crossing and protects actual fills with a percent stop.
/// </summary>
public class MacdZeroStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private decimal _prevMacd;
	private bool _hasPrev;
	private decimal _prevSignal;
	private Order _pendingOrder;

	/// <summary>
	/// Fast EMA period for MACD calculation.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD calculation.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line period for MACD calculation.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Type of candles used for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the MACD Zero strategy.
	/// </summary>
	public MacdZeroStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 12).SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period for MACD", "MACD")
			.SetOptimize(8, 16, 2);

		_slowPeriod = Param(nameof(SlowPeriod), 26).SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period for MACD", "MACD")
			.SetOptimize(15, 30, 2);

		_signalPeriod = Param(nameof(SignalPeriod), 9).SetGreaterThanZero()
			.SetDisplay("Signal", "Signal line period for MACD", "MACD")
			.SetOptimize(7, 12, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m).SetNotNegative()
			.SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection");
		OrderRegistering += order => _pendingOrder = order;
	}

	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevMacd = default;
		_hasPrev = default;
		_prevSignal = default;
		_pendingOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (FastPeriod >= SlowPeriod)
			throw new InvalidOperationException("FastPeriod must be below SlowPeriod.");
		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastPeriod },
				LongMa = { Length = SlowPeriod },
			},
			SignalMa = { Length = SignalPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native protection runs before this callback, including between finished candles.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		// Complex values capture formation before processing their inner indicators.
		// Check the indicator's post-processing state to seed the first fully formed bar.
		if (candle.State != CandleStates.Finished || !macdValue.Indicator.IsFormed || !IsFormedAndOnlineAndAllowTrading())
			return;
		var value = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (value.Macd is not decimal macd || value.Signal is not decimal signal)
			return;
		if (!_hasPrev)
		{
			_hasPrev = true;
			_prevMacd = macd;
			_prevSignal = signal;
			return;
		}
		if (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
		{
			_prevMacd = macd;
			_prevSignal = signal;
			return;
		}
		var signalCross = _prevMacd <= _prevSignal && macd > signal
			|| _prevMacd >= _prevSignal && macd < signal;
		// The published exit is a crossing in either direction, not a zero-line exit.
		if (Position != 0m && signalCross)
		{
			if (Position > 0m) SellMarket(Position);
			else BuyMarket(Math.Abs(Position));
		}
		else if (Position == 0m && macd < 0m && macd > _prevMacd)
			BuyMarket(Volume);
		else if (Position == 0m && macd > 0m && macd < _prevMacd)
			SellMarket(Volume);
		_prevMacd = macd;
		_prevSignal = signal;
	}
}
