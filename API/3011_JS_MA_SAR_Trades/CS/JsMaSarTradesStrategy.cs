using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR breakout filtered by moving averages and ZigZag swing structure.
/// </summary>
public class JsMaSarTradesStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _fastMaShift;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _slowMaShift;
	private readonly StrategyParam<MovingAverageTypeEnum> _maType;
	private readonly StrategyParam<AppliedPriceType> _appliedPrice;
	private readonly StrategyParam<decimal> _sarStep;
	private readonly StrategyParam<decimal> _sarMaxStep;
	private readonly StrategyParam<int> _zigZagDepth;
	private readonly StrategyParam<decimal> _zigZagDeviation;
	private readonly StrategyParam<int> _zigZagBackstep;
	private readonly StrategyParam<DataType> _candleType;

	private LengthIndicator<decimal> _fastMa;
	private LengthIndicator<decimal> _slowMa;
	private ParabolicSar _sar;
	private Highest _highest;
	private Lowest _lowest;

	private readonly List<decimal> _fastHistory = new();
	private readonly List<decimal> _slowHistory = new();

	private decimal? _prevLowPivot;
	private decimal? _lastLowPivot;
	private decimal? _prevHighPivot;
	private decimal? _lastHighPivot;
	private int _barsSinceLowPivot;
	private int _barsSinceHighPivot;

	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal? _entryPrice;

	/// <summary>
	/// Order volume used for new positions.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimal trailing step in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Enable trading only between the selected hours.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Session start hour (inclusive).
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Session end hour (inclusive).
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Fast moving average shift (bars to the right).
	/// </summary>
	public int FastMaShift
	{
		get => _fastMaShift.Value;
		set => _fastMaShift.Value = value;
	}

	/// <summary>
	/// Slow moving average period.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average shift (bars to the right).
	/// </summary>
	public int SlowMaShift
	{
		get => _slowMaShift.Value;
		set => _slowMaShift.Value = value;
	}

	/// <summary>
	/// Moving average type used by both averages.
	/// </summary>
	public MovingAverageTypeEnum MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Candle price source for moving averages.
	/// </summary>
	public AppliedPriceType AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Parabolic SAR acceleration factor.
	/// </summary>
	public decimal SarStep
	{
		get => _sarStep.Value;
		set => _sarStep.Value = value;
	}

	/// <summary>
	/// Parabolic SAR maximum acceleration.
	/// </summary>
	public decimal SarMaxStep
	{
		get => _sarMaxStep.Value;
		set => _sarMaxStep.Value = value;
	}

	/// <summary>
	/// ZigZag lookback depth.
	/// </summary>
	public int ZigZagDepth
	{
		get => _zigZagDepth.Value;
		set => _zigZagDepth.Value = value;
	}

	/// <summary>
	/// ZigZag deviation threshold in pips.
	/// </summary>
	public decimal ZigZagDeviation
	{
		get => _zigZagDeviation.Value;
		set => _zigZagDeviation.Value = value;
	}

	/// <summary>
	/// Minimum bars between ZigZag pivots.
	/// </summary>
	public int ZigZagBackstep
	{
		get => _zigZagBackstep.Value;
		set => _zigZagBackstep.Value = value;
	}

	/// <summary>
	/// Trading candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize parameters.
	/// </summary>
	public JsMaSarTradesStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetDisplay("Order Volume", "Base order size", "Trading")
		.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetDisplay("Stop Loss", "Stop-loss distance in pips", "Risk")
		.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
		.SetDisplay("Take Profit", "Take-profit distance in pips", "Risk")
		.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
		.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
		.SetDisplay("Trailing Step", "Minimum move to tighten stop", "Risk");

		_useTimeFilter = Param(nameof(UseTimeFilter), true)
		.SetDisplay("Use Time Filter", "Enable trading window", "Timing");

		_startHour = Param(nameof(StartHour), 19)
		.SetDisplay("Start Hour", "Session start hour", "Timing");

		_endHour = Param(nameof(EndHour), 22)
		.SetDisplay("End Hour", "Session end hour", "Timing");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 55)
		.SetDisplay("Fast MA Period", "Length for fast average", "Moving Averages")
		.SetGreaterThanZero();

		_fastMaShift = Param(nameof(FastMaShift), 3)
		.SetDisplay("Fast MA Shift", "Shift for fast average", "Moving Averages");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 120)
		.SetDisplay("Slow MA Period", "Length for slow average", "Moving Averages")
		.SetGreaterThanZero();

		_slowMaShift = Param(nameof(SlowMaShift), 0)
		.SetDisplay("Slow MA Shift", "Shift for slow average", "Moving Averages");

		_maType = Param(nameof(MaType), MovingAverageTypeEnum.Smoothed)
		.SetDisplay("MA Type", "Moving average smoothing method", "Moving Averages");

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPriceType.Median)
		.SetDisplay("Applied Price", "Price source for MAs", "Moving Averages");

		_sarStep = Param(nameof(SarStep), 0.02m)
		.SetDisplay("SAR Step", "Parabolic SAR acceleration", "Parabolic SAR");

		_sarMaxStep = Param(nameof(SarMaxStep), 0.2m)
		.SetDisplay("SAR Max", "Parabolic SAR maximum", "Parabolic SAR");

		_zigZagDepth = Param(nameof(ZigZagDepth), 12)
		.SetDisplay("ZigZag Depth", "Pivot lookback depth", "ZigZag")
		.SetGreaterThanZero();

		_zigZagDeviation = Param(nameof(ZigZagDeviation), 5m)
		.SetDisplay("ZigZag Deviation", "Minimum swing size in pips", "ZigZag");

		_zigZagBackstep = Param(nameof(ZigZagBackstep), 3)
		.SetDisplay("ZigZag Backstep", "Minimal bars between pivots", "ZigZag");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Trading timeframe", "General");
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

		_fastHistory.Clear();
		_slowHistory.Clear();
		_prevLowPivot = null;
		_lastLowPivot = null;
		_prevHighPivot = null;
		_lastHighPivot = null;
		_barsSinceLowPivot = 0;
		_barsSinceHighPivot = 0;
		ResetPositionState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
		throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");

		_fastMa = CreateMovingAverage(MaType, FastMaPeriod);
		_slowMa = CreateMovingAverage(MaType, SlowMaPeriod);
		_sar = new ParabolicSar
		{
			Acceleration = SarStep,
			AccelerationMax = SarMaxStep
		};
		_highest = new Highest { Length = ZigZagDepth };
		_lowest = new Lowest { Length = ZigZagDepth };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_sar, _highest, _lowest, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sar);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal sarValue, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Calculate applied price and shifted moving averages on each finished bar.
		var price = GetAppliedPrice(candle);
		var fastValue = ProcessMovingAverage(_fastMa, price, candle.OpenTime, _fastHistory, FastMaShift);
		var slowValue = ProcessMovingAverage(_slowMa, price, candle.OpenTime, _slowHistory, SlowMaShift);

		// Refresh ZigZag swing information before trading decisions.
		UpdateZigZagPivots(candle, highestValue, lowestValue);

		// Evaluate protective exits first so stops fire immediately.
		if (HandleRiskManagement(candle))
			return;

		// Skip new entries outside the configured session window.
		if (UseTimeFilter && !IsWithinTradingWindow(candle.OpenTime))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_sar.IsFormed || fastValue is null || slowValue is null)
			return;

		if (_lastLowPivot is null || _prevLowPivot is null || _lastHighPivot is null || _prevHighPivot is null)
			return;

		var upTrend = _lastLowPivot > _prevLowPivot;
		var downTrend = _lastHighPivot < _prevHighPivot;

		if (Position > 0 && candle.ClosePrice < sarValue)
		{
			SellMarket(Position);
			ResetPositionState();
			return;
		}

		if (Position < 0 && candle.ClosePrice > sarValue)
		{
			BuyMarket(-Position);
			ResetPositionState();
			return;
		}

		var volume = OrderVolume + Math.Abs(Position);

		// Enter long when structure, SAR and moving averages confirm an uptrend.
		if (Position <= 0 && upTrend && candle.ClosePrice > sarValue && fastValue > slowValue)
		{
			if (!TryPrepareOrder(true, candle.ClosePrice))
				return;

			BuyMarket(volume);
			_entryPrice = candle.ClosePrice;
		}
		// Enter short when descending swings are confirmed by SAR and moving averages.
		else if (Position >= 0 && downTrend && candle.ClosePrice < sarValue && fastValue < slowValue)
		{
			if (!TryPrepareOrder(false, candle.ClosePrice))
				return;

			SellMarket(volume);
			_entryPrice = candle.ClosePrice;
		}
	}

	private decimal? ProcessMovingAverage(LengthIndicator<decimal> indicator, decimal input, DateTimeOffset time, List<decimal> history, int shift)
	{
		// Feed the moving average with the chosen price and accumulate history for shifts.
		var value = indicator.Process(input, time, true);
		if (!value.IsFinal)
			return null;

		var current = value.ToDecimal();
		history.Add(current);

		var maxCount = Math.Max(shift + 1, 1);
		if (history.Count > maxCount)
		history.RemoveAt(0);

		if (history.Count <= shift)
			return null;

		var index = history.Count - 1 - shift;
		return history[index];
	}

	private void UpdateZigZagPivots(ICandleMessage candle, decimal highestValue, decimal lowestValue)
	{
		_barsSinceLowPivot++;
		_barsSinceHighPivot++;

		var deviation = GetPipValue(ZigZagDeviation);

		// Accept a new high pivot only if backstep and deviation filters allow it.
		if (candle.HighPrice >= highestValue && _barsSinceHighPivot >= ZigZagBackstep)
		{
			if (_lastHighPivot is null || Math.Abs(candle.HighPrice - _lastHighPivot.Value) >= deviation)
			{
				_prevHighPivot = _lastHighPivot;
				_lastHighPivot = candle.HighPrice;
				_barsSinceHighPivot = 0;
			}
		}

		// Accept a new low pivot only if distance and spacing requirements are met.
		if (candle.LowPrice <= lowestValue && _barsSinceLowPivot >= ZigZagBackstep)
		{
			if (_lastLowPivot is null || Math.Abs(candle.LowPrice - _lastLowPivot.Value) >= deviation)
			{
				_prevLowPivot = _lastLowPivot;
				_lastLowPivot = candle.LowPrice;
				_barsSinceLowPivot = 0;
			}
		}
	}

	private bool HandleRiskManagement(ICandleMessage candle)
	{
		// Long side management: stop-loss, take-profit and trailing checks.
		if (Position > 0)
		{
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetPositionState();
				return true;
			}

			if (_takePrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
				ResetPositionState();
				return true;
			}

			UpdateTrailingStop(true, candle);
		}
		else if (Position < 0)
		{
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(-Position);
				ResetPositionState();
				return true;
			}

			if (_takePrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(-Position);
				ResetPositionState();
				return true;
			}

			UpdateTrailingStop(false, candle);
		}
		else
		{
			ResetPositionState();
		}

		return false;
	}

	private void UpdateTrailingStop(bool isLong, ICandleMessage candle)
	{
		// Trailing stop logic is optional and requires an active position entry price.
		if (TrailingStopPips <= 0m || _entryPrice is null)
			return;

		var distance = GetPipValue(TrailingStopPips);
		if (distance <= 0m)
			return;

		var step = GetPipValue(TrailingStepPips);

		if (isLong)
		{
			var move = candle.ClosePrice - _entryPrice.Value;
			if (move <= distance + step)
				return;

			var desiredStop = candle.ClosePrice - distance;
			var threshold = candle.ClosePrice - (distance + step);

			if (_stopPrice is null || _stopPrice.Value < threshold)
			_stopPrice = desiredStop;
		}
		else
		{
			var move = _entryPrice.Value - candle.ClosePrice;
			if (move <= distance + step)
				return;

			var desiredStop = candle.ClosePrice + distance;
			var threshold = candle.ClosePrice + (distance + step);

			if (_stopPrice is null || _stopPrice.Value > threshold)
			_stopPrice = desiredStop;
		}
	}

	private bool TryPrepareOrder(bool isLong, decimal price)
	{
		var point = GetPointValue();
		if (point <= 0m)
		{
			_stopPrice = null;
			_takePrice = null;
			return true;
		}

		// Prepare stop-loss target when it is enabled.
		decimal? stop = null;
		if (StopLossPips > 0m)
		{
			var distance = StopLossPips * point;
			stop = isLong ? price - distance : price + distance;
			if (isLong && stop >= price)
				return false;
			if (!isLong && stop <= price)
				return false;
		}

		decimal? take = null;
		if (TakeProfitPips > 0m)
		{
			var distance = TakeProfitPips * point;
			take = isLong ? price + distance : price - distance;
		}

		_stopPrice = stop;
		_takePrice = take;

		return true;
	}

	private decimal GetPointValue()
	{
		// Pip calculations depend on the instrument price step being available.
		var step = Security?.PriceStep ?? 0m;
		return step <= 0m ? 0m : step;
	}

	private decimal GetPipValue(decimal value)
	{
		var point = GetPointValue();
		return point <= 0m ? 0m : value * point;
	}

	private decimal GetAppliedPrice(ICandleMessage candle)
	{
		return AppliedPrice switch
		{
			AppliedPriceType.Open => candle.OpenPrice,
			AppliedPriceType.High => candle.HighPrice,
			AppliedPriceType.Low => candle.LowPrice,
			AppliedPriceType.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceType.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceType.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice * 2m) / 4m,
			_ => candle.ClosePrice,
		};
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var seconds = (int)time.TimeOfDay.TotalSeconds;
		var start = ClampHour(StartHour) * 3600;
		var end = ClampHour(EndHour) * 3600;
		return seconds >= start && seconds <= end;
	}

	private static int ClampHour(int hour)
	{
		if (hour < 0)
			return 0;
		if (hour > 23)
			return 23;
		return hour;
	}

	private void ResetPositionState()
	{
		_stopPrice = null;
		_takePrice = null;
		_entryPrice = null;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageTypeEnum type, int length)
	{
		return type switch
		{
			MovingAverageTypeEnum.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageTypeEnum.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageTypeEnum.Weighted => new WeightedMovingAverage { Length = length },
			_ => new SmoothedMovingAverage { Length = length },
		};
	}

	/// <summary>
	/// Moving average smoothing options.
	/// </summary>
	public enum MovingAverageTypeEnum
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted
	}

	/// <summary>
	/// Price source for moving averages.
	/// </summary>
	public enum AppliedPriceType
	{
		Close,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted
	}
}
