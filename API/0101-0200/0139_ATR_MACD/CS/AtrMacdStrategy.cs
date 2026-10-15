using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ATR MACD strategy.
/// A MACD cross above its signal line goes long and a cross below goes short, the opposite cross reversing the position.
/// Each new position is sized as Volume times the average of the last AtrAvgPeriod ATR values divided by the current ATR,
/// rounded down to the volume step, so higher volatility trades smaller. A percent stop limits the loss.
/// </summary>
public class AtrMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _atrAvgPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private bool? _prevAbove;
	private readonly List<decimal> _atrs = [];

	/// <summary>
	/// Fast EMA period of MACD.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// Slow EMA period of MACD.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// Signal line period of MACD.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Period of ATR.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR values the typical volatility is averaged over.
	/// </summary>
	public int AtrAvgPeriod
	{
		get => _atrAvgPeriod.Value;
		set => _atrAvgPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public AtrMacdStrategy()
	{
		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period of MACD", "MACD");

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period of MACD", "MACD");

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal line period of MACD", "MACD");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period of ATR", "Sizing");

		_atrAvgPeriod = Param(nameof(AtrAvgPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("ATR Average Period", "ATR values the typical volatility is averaged over", "Sizing");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevAbove = null;
		_atrs.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevAbove = null;
		_atrs.Clear();

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, atr, ProcessCandle)
			.Start();

		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);

		// The stop has to see prices between candles, not only at their close.
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, macd);
				DrawIndicator(oscillators, atr);
			}
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atrValue.IsFormed)
		{
			_atrs.Add(atrValue.GetValue<decimal>());

			if (_atrs.Count > AtrAvgPeriod)
				_atrs.RemoveAt(0);
		}

		if (!macdValue.IsFormed)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
			return;

		var isAbove = macd > signal;
		var wasAbove = _prevAbove;
		_prevAbove = isAbove;

		if (wasAbove is not bool was || was == isAbove || _atrs.Count < AtrAvgPeriod)
			return;

		var currentAtr = _atrs[^1];

		if (currentAtr <= 0 || !IsFormedAndOnlineAndAllowTrading())
			return;

		var size = Volume * _atrs.Average() / currentAtr;

		if (Security?.VolumeStep is decimal step && step > 0)
			size = Math.Max(step, Math.Floor(size / step) * step);

		if (isAbove && Position <= 0)
			BuyMarket(size + Math.Abs(Position));
		else if (!isAbove && Position >= 0)
			SellMarket(size + Math.Abs(Position));
	}
}
