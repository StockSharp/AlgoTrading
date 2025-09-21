using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// 1-hour EUR/USD breakout strategy using dual moving averages and MACD swing detection.
/// </summary>
public class OneHourEurUsdStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _fastMaShift;
	private readonly StrategyParam<MovingAverageKind> _fastMaType;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _slowMaShift;
	private readonly StrategyParam<MovingAverageKind> _slowMaType;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _fastShiftQueue = new();
	private readonly Queue<decimal> _slowShiftQueue = new();

	private decimal _prevHigh1;
	private decimal _prevHigh2;
	private decimal _prevLow1;
	private decimal _prevLow2;
	private int _completedBars;

	private decimal _macdPrev1;
	private decimal _macdPrev2;
	private decimal _macdPrev3;
	private decimal _macdPrev4;
	private int _macdCount;

	private decimal? _entryPrice;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;

	private decimal _pipSize;

	/// <summary>
	/// Initializes a new instance of <see cref="OneHourEurUsdStrategy"/>.
	/// </summary>
	public OneHourEurUsdStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetDisplay("Trade Volume", "Base order volume", "Orders")
			.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 150)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance", "Risk")
			.SetNotNegative();

		_takeProfitPips = Param(nameof(TakeProfitPips), 150)
			.SetDisplay("Take Profit (pips)", "Take profit distance", "Risk")
			.SetNotNegative();

		_trailingStopPips = Param(nameof(TrailingStopPips), 5)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk")
			.SetNotNegative();

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetDisplay("Trailing Step (pips)", "Step before trailing moves", "Risk")
			.SetNotNegative();

		_lookbackPeriod = Param(nameof(LookbackPeriod), 10)
			.SetDisplay("Lookback", "Reserved lookback (matches original script)", "General")
			.SetGreaterThanZero();

		_fastMaLength = Param(nameof(FastMaLength), 10)
			.SetDisplay("Fast MA Length", "Fast MA period", "Indicators")
			.SetGreaterThanZero();

		_fastMaShift = Param(nameof(FastMaShift), 0)
			.SetDisplay("Fast MA Shift", "Shift for fast MA", "Indicators")
			.SetNotNegative();

		_fastMaType = Param(nameof(FastMaType), MovingAverageKind.Simple)
			.SetDisplay("Fast MA Type", "Type of fast MA", "Indicators");

		_slowMaLength = Param(nameof(SlowMaLength), 100)
			.SetDisplay("Slow MA Length", "Slow MA period", "Indicators")
			.SetGreaterThanZero();

		_slowMaShift = Param(nameof(SlowMaShift), 0)
			.SetDisplay("Slow MA Shift", "Shift for slow MA", "Indicators")
			.SetNotNegative();

		_slowMaType = Param(nameof(SlowMaType), MovingAverageKind.Simple)
			.SetDisplay("Slow MA Type", "Type of slow MA", "Indicators");

		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetDisplay("MACD Fast", "Fast EMA for MACD", "Indicators")
			.SetGreaterThanZero();

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetDisplay("MACD Slow", "Slow EMA for MACD", "Indicators")
			.SetGreaterThanZero();

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetDisplay("MACD Signal", "Signal EMA for MACD", "Indicators")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");
	}

	/// <summary>
	/// Base order volume used for entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Step that must be covered before trailing stop is tightened.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Reserved lookback value kept for compatibility with the original EA.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Shift applied to the fast moving average.
	/// </summary>
	public int FastMaShift
	{
		get => _fastMaShift.Value;
		set => _fastMaShift.Value = value;
	}

	/// <summary>
	/// Type of moving average used for the fast curve.
	/// </summary>
	public MovingAverageKind FastMaType
	{
		get => _fastMaType.Value;
		set => _fastMaType.Value = value;
	}

	/// <summary>
	/// Slow moving average period.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Shift applied to the slow moving average.
	/// </summary>
	public int SlowMaShift
	{
		get => _slowMaShift.Value;
		set => _slowMaShift.Value = value;
	}

	/// <summary>
	/// Type of moving average used for the slow curve.
	/// </summary>
	public MovingAverageKind SlowMaType
	{
		get => _slowMaType.Value;
		set => _slowMaType.Value = value;
	}

	/// <summary>
	/// Fast EMA length for the MACD indicator.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length for the MACD indicator.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// Signal EMA length for the MACD indicator.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

	_fastShiftQueue.Clear();
	_slowShiftQueue.Clear();

	_prevHigh1 = 0m;
	_prevHigh2 = 0m;
	_prevLow1 = 0m;
	_prevLow2 = 0m;
	_completedBars = 0;

	_macdPrev1 = 0m;
	_macdPrev2 = 0m;
	_macdPrev3 = 0m;
	_macdPrev4 = 0m;
	_macdCount = 0;

	_entryPrice = null;
	_stopLossPrice = null;
	_takeProfitPrice = null;

	_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	if (TrailingStopPips > 0 && TrailingStepPips <= 0)
	throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");

	Volume = TradeVolume;
	_pipSize = CalculatePipSize();

	var fastMa = CreateMovingAverage(FastMaType, FastMaLength);
	var slowMa = CreateMovingAverage(SlowMaType, SlowMaLength);
	var macd = new MovingAverageConvergenceDivergence
	{
		ShortPeriod = MacdFastLength,
		LongPeriod = MacdSlowLength,
		SignalPeriod = MacdSignalLength
	};

	// Bind indicators to incoming candles and start receiving data.
	var subscription = SubscribeCandles(CandleType);
	subscription
		.Bind(fastMa, slowMa, macd, ProcessCandle)
		.Start();

	var chartArea = CreateChartArea();
	if (chartArea != null)
	{
		DrawCandles(chartArea, subscription);
		DrawIndicator(chartArea, fastMa);
		DrawIndicator(chartArea, slowMa);
		DrawIndicator(chartArea, macd);
		DrawOwnTrades(chartArea);
	}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal macdLine, decimal macdSignal, decimal macdHistogram)
	{
	if (candle.State != CandleStates.Finished)
	return;

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	// MACD signal and histogram outputs are kept for completeness.
	_ = macdSignal;
	_ = macdHistogram;

	// Apply configured shift to both moving averages.
	var fastShifted = ApplyShift(_fastShiftQueue, fastValue, FastMaShift);
	var slowShifted = ApplyShift(_slowShiftQueue, slowValue, SlowMaShift);

	// Manage trailing exits before looking for fresh signals.
	HandleActivePosition(candle);

	var canEvaluate = _completedBars >= 2 && _macdCount >= 4;
	if (canEvaluate)
	{
	var longSignal =
		(fastShifted > slowShifted &&
		 ((
			_macdPrev1 > _macdPrev2 &&
			_macdPrev2 < _macdPrev3 &&
			_macdPrev2 < 0m &&
			candle.ClosePrice > _prevHigh1) ||
			(
				_macdPrev2 > _macdPrev3 &&
				_macdPrev3 < _macdPrev4 &&
				_macdPrev3 < 0m &&
				candle.ClosePrice > _prevHigh2))) &&
		Position <= 0;

	var shortSignal =
		(fastShifted < slowShifted &&
		 ((
			_macdPrev1 < _macdPrev2 &&
			_macdPrev2 > _macdPrev3 &&
			_macdPrev2 > 0m &&
			candle.ClosePrice < _prevLow1) ||
			(
				_macdPrev2 < _macdPrev3 &&
				_macdPrev3 > _macdPrev4 &&
				_macdPrev3 > 0m &&
				candle.ClosePrice < _prevLow2))) &&
		Position >= 0;

	if (longSignal)
	{
		var volume = TradeVolume + Math.Abs(Position);
			if (volume > 0m)
			{
			BuyMarket(volume);
			InitializePositionState(true, candle.ClosePrice);
			}
	}
	else if (shortSignal)
	{
		var volume = TradeVolume + Math.Abs(Position);
			if (volume > 0m)
			{
			SellMarket(volume);
			InitializePositionState(false, candle.ClosePrice);
			}
	}
	}

	// Update historical data after evaluating signals.
	_prevHigh2 = _prevHigh1;
	_prevLow2 = _prevLow1;
	_prevHigh1 = candle.HighPrice;
	_prevLow1 = candle.LowPrice;
	_completedBars = Math.Min(_completedBars + 1, int.MaxValue);

	_macdPrev4 = _macdPrev3;
	_macdPrev3 = _macdPrev2;
	_macdPrev2 = _macdPrev1;
	_macdPrev1 = macdLine;
	_macdCount = Math.Min(_macdCount + 1, int.MaxValue);
	}

	private void HandleActivePosition(ICandleMessage candle)
	{
	if (Position == 0m || _entryPrice is null)
	return;

	var stepDistance = TrailingStepPips * _pipSize;
	var trailingDistance = TrailingStopPips * _pipSize;

	if (Position > 0)
	{
	if (TrailingStopPips > 0)
	{
		var profit = candle.ClosePrice - _entryPrice.Value;
		var activationThreshold = candle.ClosePrice - (trailingDistance + stepDistance);

		if (profit > trailingDistance + stepDistance &&
		(!_stopLossPrice.HasValue || _stopLossPrice.Value < activationThreshold))
		{
			// Move stop loss closer as soon as profit covers distance plus step.
			_stopLossPrice = candle.ClosePrice - trailingDistance;
		}
	}

	if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
	{
		SellMarket(Position);
		ResetPositionState();
		return;
	}

	if (_stopLossPrice.HasValue && candle.LowPrice <= _stopLossPrice.Value)
	{
		SellMarket(Position);
		ResetPositionState();
	}
	}
	else if (Position < 0)
	{
	if (TrailingStopPips > 0)
	{
		var profit = _entryPrice.Value - candle.ClosePrice;
		var activationThreshold = candle.ClosePrice + (trailingDistance + stepDistance);

		if (profit > trailingDistance + stepDistance &&
		(!_stopLossPrice.HasValue || _stopLossPrice.Value > activationThreshold))
		{
			// Tighten stop loss when price moves favorably by configured amount.
			_stopLossPrice = candle.ClosePrice + trailingDistance;
		}
	}

	if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
	{
		BuyMarket(Math.Abs(Position));
		ResetPositionState();
		return;
	}

	if (_stopLossPrice.HasValue && candle.HighPrice >= _stopLossPrice.Value)
	{
		BuyMarket(Math.Abs(Position));
		ResetPositionState();
	}
	}
	}

	private void InitializePositionState(bool isLong, decimal entryPrice)
	{
	_entryPrice = entryPrice;

	_stopLossPrice = StopLossPips > 0
	? entryPrice + (isLong ? -1m : 1m) * StopLossPips * _pipSize
	: null;

	_takeProfitPrice = TakeProfitPips > 0
	? entryPrice + (isLong ? 1m : -1m) * TakeProfitPips * _pipSize
	: null;
	}

	private void ResetPositionState()
	{
	_entryPrice = null;
	_stopLossPrice = null;
	_takeProfitPrice = null;
	}

	private decimal ApplyShift(Queue<decimal> queue, decimal currentValue, int shift)
	{
	if (shift <= 0)
	return currentValue;

	var shifted = queue.Count < shift ? currentValue : queue.Peek();

	queue.Enqueue(currentValue);

	if (queue.Count > shift)
	queue.Dequeue();

	return shifted;
	}

	private decimal CalculatePipSize()
	{
	var step = Security?.PriceStep ?? 0m;
	if (step <= 0m)
	return 0.0001m;

	var digits = (int)Math.Round(Math.Log10((double)(1m / step)));
	return digits == 3 || digits == 5 ? step * 10m : step;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageKind type, int length)
	{
	return type switch
	{
		MovingAverageKind.Simple => new SimpleMovingAverage { Length = length },
		MovingAverageKind.Exponential => new ExponentialMovingAverage { Length = length },
		MovingAverageKind.Smoothed => new SmoothedMovingAverage { Length = length },
		MovingAverageKind.LinearWeighted => new WeightedMovingAverage { Length = length },
		_ => new SimpleMovingAverage { Length = length }
	};
	}

	/// <summary>
	/// Moving average options matching the original MQL enumeration.
	/// </summary>
	public enum MovingAverageKind
	{
	Simple,
	Exponential,
	Smoothed,
	LinearWeighted
	}
}
