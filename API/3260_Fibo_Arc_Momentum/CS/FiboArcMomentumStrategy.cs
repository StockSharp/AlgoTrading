using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader "FiboArc" expert advisor.
/// Combines linear weighted moving averages, momentum and MACD filters
/// with a simplified Fibonacci arc breakout confirmation.
/// </summary>
public class FiboArcMomentumStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _trendAnchorLength;
	private readonly StrategyParam<int> _arcAnchorLength;
	private readonly StrategyParam<decimal> _fibonacciRatio;
	private readonly StrategyParam<decimal> _stopLossDistance;
	private readonly StrategyParam<decimal> _takeProfitDistance;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<decimal> _trailingTrigger;
	private readonly StrategyParam<decimal> _trailingDistance;
	private readonly StrategyParam<bool> _enableBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTrigger;
	private readonly StrategyParam<decimal> _breakEvenOffset;

	private LinearWeightedMovingAverage _fastMa = null!;
	private LinearWeightedMovingAverage _slowMa = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private readonly List<CandleSnapshot> _history = new();
	private decimal? _previousOpen;
	private decimal? _previousFibLevel;
	private decimal? _previousMomentum;
	private decimal? _previousFast;
	private decimal? _previousSlow;
	private decimal? _previousMacd;
	private decimal? _previousSignal;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private bool _breakEvenActivated;

	private readonly struct CandleSnapshot
	{
		public CandleSnapshot(DateTimeOffset time, decimal open, decimal high, decimal low, decimal close)
		{
			Time = time;
			Open = open;
			High = high;
			Low = low;
			Close = close;
		}

		public DateTimeOffset Time { get; }
		public decimal Open { get; }
		public decimal High { get; }
		public decimal Low { get; }
		public decimal Close { get; }
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public FiboArcMomentumStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe for calculations", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
		.SetGreaterThanZero()
		.SetDisplay("Fast LWMA", "Fast linear weighted moving average length", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(3, 20, 1);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
		.SetGreaterThanZero()
		.SetDisplay("Slow LWMA", "Slow linear weighted moving average length", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(30, 150, 5);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Period", "Momentum lookback period", "Momentum")
		.SetCanOptimize(true)
		.SetOptimize(5, 25, 1);

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Threshold", "Minimum distance from 100 for momentum", "Momentum")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 1.5m, 0.1m);

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA period for MACD", "MACD")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA period for MACD", "MACD")
		.SetCanOptimize(true)
		.SetOptimize(20, 60, 1);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal EMA period for MACD", "MACD")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);

		_trendAnchorLength = Param(nameof(TrendAnchorLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("Trend Anchor", "Number of bars between base and arc anchor", "Fibonacci")
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 1);

		_arcAnchorLength = Param(nameof(ArcAnchorLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("Arc Anchor", "Bars offset for the second Fibonacci anchor", "Fibonacci")
		.SetCanOptimize(true)
		.SetOptimize(5, 40, 1);

		_fibonacciRatio = Param(nameof(FibonacciRatio), 0.618m)
		.SetGreaterThanZero()
		.SetDisplay("Fibonacci Ratio", "Ratio applied between anchors", "Fibonacci")
		.SetCanOptimize(true)
		.SetOptimize(0.236m, 0.786m, 0.05m);

		_stopLossDistance = Param(nameof(StopLossDistance), 0.0020m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss", "Fixed stop distance in price units", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.0005m, 0.0050m, 0.0005m);

		_takeProfitDistance = Param(nameof(TakeProfitDistance), 0.0050m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit", "Fixed take profit distance in price units", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.0010m, 0.0100m, 0.0005m);

		_enableTrailing = Param(nameof(EnableTrailing), true)
		.SetDisplay("Enable Trailing", "Toggle adaptive trailing stop", "Risk");

		_trailingTrigger = Param(nameof(TrailingTrigger), 0.0030m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Trigger", "Profit required before trailing activates", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.0010m, 0.0080m, 0.0005m);

		_trailingDistance = Param(nameof(TrailingDistance), 0.0015m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Distance", "Distance between price and trailing stop", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.0005m, 0.0040m, 0.0005m);

		_enableBreakEven = Param(nameof(EnableBreakEven), true)
		.SetDisplay("Enable Break-even", "Move stop to entry after defined profit", "Risk");

		_breakEvenTrigger = Param(nameof(BreakEvenTrigger), 0.0025m)
		.SetGreaterThanZero()
		.SetDisplay("Break-even Trigger", "Profit required to move stop to entry", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.0010m, 0.0060m, 0.0005m);

		_breakEvenOffset = Param(nameof(BreakEvenOffset), 0.0002m)
		.SetGreaterThanZero()
		.SetDisplay("Break-even Offset", "Additional profit locked after break-even", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.0001m, 0.0010m, 0.0001m);
	}

	/// <summary>
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast LWMA period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow LWMA period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Momentum lookback period.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum absolute distance from 100 for the momentum reading.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	/// <summary>
	/// Fast MACD period.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow MACD period.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Number of candles between the first anchor and the current bar.
	/// </summary>
	public int TrendAnchorLength
	{
		get => _trendAnchorLength.Value;
		set => _trendAnchorLength.Value = value;
	}

	/// <summary>
	/// Distance in candles to the second anchor of the Fibonacci arc.
	/// </summary>
	public int ArcAnchorLength
	{
		get => _arcAnchorLength.Value;
		set => _arcAnchorLength.Value = value;
	}

	/// <summary>
	/// Ratio applied when projecting the Fibonacci arc level.
	/// </summary>
	public decimal FibonacciRatio
	{
		get => _fibonacciRatio.Value;
		set => _fibonacciRatio.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in absolute price units.
	/// </summary>
	public decimal StopLossDistance
	{
		get => _stopLossDistance.Value;
		set => _stopLossDistance.Value = value;
	}

	/// <summary>
	/// Take-profit distance in absolute price units.
	/// </summary>
	public decimal TakeProfitDistance
	{
		get => _takeProfitDistance.Value;
		set => _takeProfitDistance.Value = value;
	}

	/// <summary>
	/// Enables or disables trailing stop updates.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Profit required before the trailing stop starts following price.
	/// </summary>
	public decimal TrailingTrigger
	{
		get => _trailingTrigger.Value;
		set => _trailingTrigger.Value = value;
	}

	/// <summary>
	/// Distance maintained between price and the trailing stop.
	/// </summary>
	public decimal TrailingDistance
	{
		get => _trailingDistance.Value;
		set => _trailingDistance.Value = value;
	}

	/// <summary>
	/// Enables automatic move to break-even.
	/// </summary>
	public bool EnableBreakEven
	{
		get => _enableBreakEven.Value;
		set => _enableBreakEven.Value = value;
	}

	/// <summary>
	/// Profit required before stop-loss moves to entry.
	/// </summary>
	public decimal BreakEvenTrigger
	{
		get => _breakEvenTrigger.Value;
		set => _breakEvenTrigger.Value = value;
	}

	/// <summary>
	/// Additional profit locked when the position is moved to break-even.
	/// </summary>
	public decimal BreakEvenOffset
	{
		get => _breakEvenOffset.Value;
		set => _breakEvenOffset.Value = value;
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

		_history.Clear();
		_previousOpen = null;
		_previousFibLevel = null;
		_previousMomentum = null;
		_previousFast = null;
		_previousSlow = null;
		_previousMacd = null;
		_previousSignal = null;
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_breakEvenActivated = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new LinearWeightedMovingAverage { Length = FastMaPeriod, CandlePrice = CandlePrice.Typical };
		_slowMa = new LinearWeightedMovingAverage { Length = SlowMaPeriod, CandlePrice = CandlePrice.Typical };
		_momentum = new Momentum { Length = MomentumPeriod };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastPeriod },
				LongMa = { Length = MacdSlowPeriod },
			},
			SignalMa = { Length = MacdSignalPeriod }
		};

		_history.Clear();
		_previousOpen = null;
		_previousFibLevel = null;
		_previousMomentum = null;
		_previousFast = null;
		_previousSlow = null;
		_previousMacd = null;
		_previousSignal = null;
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_breakEvenActivated = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_macd, _fastMa, _slowMa, _momentum, ProcessCandle)
		.Start();

		StartProtection(
		takeProfit: null,
		stopLoss: null
	);

	var area = CreateChartArea();
	if (area != null)
	{
		DrawCandles(area, subscription);
		DrawIndicator(area, _fastMa);
		DrawIndicator(area, _slowMa);
		DrawIndicator(area, _macd);
		DrawOwnTrades(area);
	}
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue momentumValue)
{
	if (candle.State != CandleStates.Finished)
	return;

	if (!macdValue.IsFinal || !fastValue.IsFinal || !slowValue.IsFinal || !momentumValue.IsFinal)
	return;

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	var macdData = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
	var macdLine = macdData.Macd;
	var signalLine = macdData.Signal;
	var fast = fastValue.ToDecimal();
	var slow = slowValue.ToDecimal();
	var momentumRaw = momentumValue.ToDecimal();
	var momentumDistance = Math.Abs(momentumRaw - 100m);

	var prevOpen = _previousOpen;
	var prevFib = _previousFibLevel;

	_history.Add(new CandleSnapshot(candle.OpenTime, candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice));
	TrimHistory();

	var fibLevel = CalculateFibonacciLevel();

	var fibCrossUp = prevOpen.HasValue && prevFib.HasValue && fibLevel.HasValue &&
	prevOpen.Value <= prevFib.Value && candle.OpenPrice >= fibLevel.Value;
	var fibCrossDown = prevOpen.HasValue && prevFib.HasValue && fibLevel.HasValue &&
	prevOpen.Value >= prevFib.Value && candle.OpenPrice <= fibLevel.Value;

	var bullishTrend = fast > slow;
	var bearishTrend = fast < slow;
	var momentumSupportsLong = momentumDistance >= MomentumThreshold;
	var momentumSupportsShort = momentumDistance >= MomentumThreshold;

	var macdAboveSignal = macdLine > signalLine;
	var macdBelowSignal = macdLine < signalLine;

	var macdCrossUp = _previousMacd.HasValue && _previousSignal.HasValue &&
	_previousMacd.Value <= _previousSignal.Value && macdAboveSignal;
	var macdCrossDown = _previousMacd.HasValue && _previousSignal.HasValue &&
	_previousMacd.Value >= _previousSignal.Value && macdBelowSignal;

	if (Position <= 0 && fibCrossUp && bullishTrend && momentumSupportsLong && (macdAboveSignal || macdCrossUp))
	{
		OpenLong(candle);
	}
	else if (Position >= 0 && fibCrossDown && bearishTrend && momentumSupportsShort && (macdBelowSignal || macdCrossDown))
	{
		OpenShort(candle);
	}
	else
	{
		ManageOpenPosition(candle, fibCrossUp, fibCrossDown, macdCrossUp, macdCrossDown, bullishTrend, bearishTrend);
	}

	_previousOpen = candle.OpenPrice;
	_previousFibLevel = fibLevel;
	_previousMomentum = momentumRaw;
	_previousFast = fast;
	_previousSlow = slow;
	_previousMacd = macdLine;
	_previousSignal = signalLine;
}

private void OpenLong(ICandleMessage candle)
{
	var volume = Volume + (Position < 0 ? Math.Abs(Position) : 0m);
	if (volume <= 0)
	return;

	BuyMarket(volume);

	_entryPrice = candle.ClosePrice;
	_stopPrice = _entryPrice - StopLossDistance;
	_takeProfitPrice = _entryPrice + TakeProfitDistance;
	_breakEvenActivated = false;

	LogInfo($"Long entry at {candle.ClosePrice}. Stop {_stopPrice}, target {_takeProfitPrice}.");
}

private void OpenShort(ICandleMessage candle)
{
	var volume = Volume + (Position > 0 ? Math.Abs(Position) : 0m);
	if (volume <= 0)
	return;

	SellMarket(volume);

	_entryPrice = candle.ClosePrice;
	_stopPrice = _entryPrice + StopLossDistance;
	_takeProfitPrice = _entryPrice - TakeProfitDistance;
	_breakEvenActivated = false;

	LogInfo($"Short entry at {candle.ClosePrice}. Stop {_stopPrice}, target {_takeProfitPrice}.");
}

private void ManageOpenPosition(ICandleMessage candle, bool fibCrossUp, bool fibCrossDown, bool macdCrossUp, bool macdCrossDown, bool bullishTrend, bool bearishTrend)
{
	if (Position > 0)
	{
		UpdateStopsForLong(candle);

		var exitByTrend = bearishTrend || macdCrossDown || fibCrossDown;
		if (exitByTrend)
		{
			SellMarket(Math.Abs(Position));
			ResetRiskState();
			return;
		}

		if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
		{
			SellMarket(Math.Abs(Position));
			ResetRiskState();
			return;
		}

		if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
		{
			SellMarket(Math.Abs(Position));
			ResetRiskState();
		}
	}
	else if (Position < 0)
	{
		UpdateStopsForShort(candle);

		var exitByTrend = bullishTrend || macdCrossUp || fibCrossUp;
		if (exitByTrend)
		{
			BuyMarket(Math.Abs(Position));
			ResetRiskState();
			return;
		}

		if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
		{
			BuyMarket(Math.Abs(Position));
			ResetRiskState();
			return;
		}

		if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
		{
			BuyMarket(Math.Abs(Position));
			ResetRiskState();
		}
	}
}

private void UpdateStopsForLong(ICandleMessage candle)
{
	if (!_entryPrice.HasValue)
	return;

	var currentPrice = candle.ClosePrice;
	var profit = currentPrice - _entryPrice.Value;

	if (EnableBreakEven && !_breakEvenActivated && profit >= BreakEvenTrigger)
	{
		_stopPrice = Math.Max(_stopPrice ?? decimal.MinValue, _entryPrice.Value + BreakEvenOffset);
		_breakEvenActivated = true;
	}

	if (EnableTrailing && profit >= TrailingTrigger)
	{
		var trailStop = candle.HighPrice - TrailingDistance;
		if (!_stopPrice.HasValue || trailStop > _stopPrice.Value)
		_stopPrice = trailStop;
	}
}

private void UpdateStopsForShort(ICandleMessage candle)
{
	if (!_entryPrice.HasValue)
	return;

	var currentPrice = candle.ClosePrice;
	var profit = _entryPrice.Value - currentPrice;

	if (EnableBreakEven && !_breakEvenActivated && profit >= BreakEvenTrigger)
	{
		_stopPrice = Math.Min(_stopPrice ?? decimal.MaxValue, _entryPrice.Value - BreakEvenOffset);
		_breakEvenActivated = true;
	}

	if (EnableTrailing && profit >= TrailingTrigger)
	{
		var trailStop = candle.LowPrice + TrailingDistance;
		if (!_stopPrice.HasValue || trailStop < _stopPrice.Value)
		_stopPrice = trailStop;
	}
}

private void ResetRiskState()
{
	_entryPrice = null;
	_stopPrice = null;
	_takeProfitPrice = null;
	_breakEvenActivated = false;
}

private void TrimHistory()
{
	var maxCount = Math.Max(TrendAnchorLength, ArcAnchorLength) + 5;
	if (_history.Count <= maxCount)
	return;

	var removeCount = _history.Count - maxCount;
	_history.RemoveRange(0, removeCount);
}

private decimal? CalculateFibonacciLevel()
{
	var required = Math.Max(TrendAnchorLength, ArcAnchorLength);
	if (_history.Count <= required)
	return null;

	var baseIndex = _history.Count - TrendAnchorLength;
	var anchorIndex = _history.Count - ArcAnchorLength - 1;

	if (baseIndex < 0 || baseIndex >= _history.Count)
	return null;

	if (anchorIndex < 0 || anchorIndex >= _history.Count)
	return null;

	var baseOpen = _history[baseIndex].Open;
	var anchorOpen = _history[anchorIndex].Open;
	var projected = anchorOpen + (baseOpen - anchorOpen) * FibonacciRatio;
	return projected;
}

/// <inheritdoc />
protected override void OnNewMyTrade(MyTrade trade)
{
	base.OnNewMyTrade(trade);

	if (trade.Order?.Security != Security)
	return;

	if (Position == 0)
	{
		ResetRiskState();
		return;
	}

	if (trade.Order?.Direction == Sides.Buy)
	{
		_entryPrice = trade.Trade.Price;
		_stopPrice = _entryPrice - StopLossDistance;
		_takeProfitPrice = _entryPrice + TakeProfitDistance;
		_breakEvenActivated = false;
	}
	else if (trade.Order?.Direction == Sides.Sell)
	{
		_entryPrice = trade.Trade.Price;
		_stopPrice = _entryPrice + StopLossDistance;
		_takeProfitPrice = _entryPrice - TakeProfitDistance;
		_breakEvenActivated = false;
	}
}
}
