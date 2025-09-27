using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy converted from MetaTrader 5.
/// Uses momentum confirmation, minimum distance filter and trailing stop management.
/// </summary>
public class CrossingMovingAverageStrategy : Strategy
{
	/// <summary>
	/// Moving average types supported by the strategy.
	/// </summary>
	public enum MovingAverageModes
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Simple,
		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Exponential,
		/// <summary>
		/// Smoothed moving average.
		/// </summary>
		Smoothed,
		/// <summary>
		/// Weighted moving average.
		/// </summary>
		Weighted
	}
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _minDistancePips;
	private readonly StrategyParam<decimal> _momentumFilter;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _fastShift;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _slowShift;
	private readonly StrategyParam<MovingAverageModes> _maMethod;
	private readonly StrategyParam<CandlePrice> _appliedPrice;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverage _fastMa;
	private MovingAverage _slowMa;
	private Momentum _momentum;

	private readonly List<decimal> _fastHistory = new();
	private readonly List<decimal> _slowHistory = new();
	private readonly List<decimal> _momentumHistory = new();

	private int _historyCapacity;
	private decimal _pipSize;
	private decimal? _momentumOffset;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;

	/// <summary>
	/// Base order volume.
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
	/// Minimum improvement in pips required to advance the trailing stop.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Minimum distance between averages in pips.
	/// </summary>
	public decimal MinDistancePips
	{
		get => _minDistancePips.Value;
		set => _minDistancePips.Value = value;
	}

	/// <summary>
	/// Momentum threshold filter.
	/// </summary>
	public decimal MomentumFilter
	{
		get => _momentumFilter.Value;
		set => _momentumFilter.Value = value;
	}

	/// <summary>
	/// Fast moving average length.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Fast moving average shift in bars.
	/// </summary>
	public int FastShift
	{
		get => _fastShift.Value;
		set => _fastShift.Value = value;
	}

	/// <summary>
	/// Slow moving average length.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Slow moving average shift in bars.
	/// </summary>
	public int SlowShift
	{
		get => _slowShift.Value;
		set => _slowShift.Value = value;
	}

	/// <summary>
	/// Selected moving average smoothing method.
	/// </summary>
	public MovingAverageModes MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Candle price used for indicator calculations.
	/// </summary>
	public CandlePrice AppliedPrice
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Momentum calculation length.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Candle data type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CrossingMovingAverageStrategy"/> class.
	/// </summary>
	public CrossingMovingAverageStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Base volume for market entries", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetRange(0m, 1000m)
		.SetDisplay("Stop Loss (pips)", "Stop loss distance expressed in pips", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(10m, 150m, 10m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
		.SetRange(0m, 1000m)
		.SetDisplay("Take Profit (pips)", "Take profit distance expressed in pips", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(10m, 150m, 10m);

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
		.SetRange(0m, 500m)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk Management");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
		.SetRange(0m, 500m)
		.SetDisplay("Trailing Step (pips)", "Minimum pip improvement required to move the trailing stop", "Risk Management");

		_minDistancePips = Param(nameof(MinDistancePips), 0m)
		.SetRange(0m, 500m)
		.SetDisplay("Minimum Distance (pips)", "Minimum distance between moving averages", "Strategy");

		_momentumFilter = Param(nameof(MomentumFilter), 0.1m)
		.SetRange(0m, 10m)
		.SetDisplay("Momentum Filter", "Minimum momentum delta required for entries", "Strategy")
		.SetCanOptimize(true)
		.SetOptimize(0.05m, 1m, 0.05m);

		_fastPeriod = Param(nameof(FastPeriod), 13)
		.SetRange(1, 500)
		.SetDisplay("Fast MA Period", "Length of the fast moving average", "Indicators");

		_fastShift = Param(nameof(FastShift), 1)
		.SetRange(0, 50)
		.SetDisplay("Fast MA Shift", "Forward shift in bars for the fast MA", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 34)
		.SetRange(1, 500)
		.SetDisplay("Slow MA Period", "Length of the slow moving average", "Indicators");

		_slowShift = Param(nameof(SlowShift), 3)
		.SetRange(0, 50)
		.SetDisplay("Slow MA Shift", "Forward shift in bars for the slow MA", "Indicators");

		_maMethod = Param(nameof(MaMethod), MovingAverageModes.Exponential)
		.SetDisplay("MA Method", "Moving average smoothing method", "Indicators")
		.SetCanOptimize(true);

		_appliedPrice = Param(nameof(AppliedPrice), CandlePrice.Close)
		.SetDisplay("Applied Price", "Price type used for indicators", "Indicators");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
		.SetRange(1, 500)
		.SetDisplay("Momentum Period", "Number of bars used by the momentum indicator", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle data type used for calculations", "General");
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
		_momentumHistory.Clear();

		_fastMa = null;
		_slowMa = null;
		_momentum = null;

		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
		_momentumOffset = null;
		_historyCapacity = 0;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_fastMa = CreateMovingAverage(MaMethod, FastPeriod);
		_slowMa = CreateMovingAverage(MaMethod, SlowPeriod);
		_momentum = new Momentum { Length = MomentumPeriod };

		_fastHistory.Clear();
		_slowHistory.Clear();
		_momentumHistory.Clear();
		_momentumOffset = null;

		_historyCapacity = Math.Max(FastShift, SlowShift) + 5;
		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);

			var indicatorArea = CreateChartArea();
			if (indicatorArea != null)
			{
				DrawIndicator(indicatorArea, _momentum);
			}

			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var price = GetPrice(candle, AppliedPrice);

		var fastValue = _fastMa.Process(price, candle.OpenTime, true);
		var slowValue = _slowMa.Process(price, candle.OpenTime, true);
		var momentumValue = _momentum.Process(price, candle.OpenTime, true);

		if (!fastValue.IsFinal || !slowValue.IsFinal || !momentumValue.IsFinal)
		return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_momentum.IsFormed)
		return;

		var fast = fastValue.ToDecimal();
		var slow = slowValue.ToDecimal();
		var momentumResult = momentumValue.ToDecimal();

		if (_momentumOffset is null)
		_momentumOffset = momentumResult > 10m ? 100m : 0m;

		StoreValue(_fastHistory, fast, _historyCapacity);
		StoreValue(_slowHistory, slow, _historyCapacity);
		StoreValue(_momentumHistory, momentumResult, MomentumPeriod + 5);

		if (!HasEnoughHistory())
		return;

		CheckExitByStops(candle);

		if (Position != 0)
		UpdateTrailing(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var minDistance = MinDistancePips > 0m && _pipSize > 0m ? MinDistancePips * _pipSize : 0m;
		var currentMomentum = GetHistoryValue(_momentumHistory, 1) - _momentumOffset.GetValueOrDefault();
		var previousMomentum = GetHistoryValue(_momentumHistory, 2) - _momentumOffset.GetValueOrDefault();

		var fastPrev1 = GetHistoryValue(_fastHistory, FastShift + 1);
		var fastPrev2 = GetHistoryValue(_fastHistory, FastShift + 2);
		var slowPrev1 = GetHistoryValue(_slowHistory, SlowShift + 1);
		var slowPrev2 = GetHistoryValue(_slowHistory, SlowShift + 2);

		var buySignal = fastPrev1 > slowPrev1 + minDistance && fastPrev2 < slowPrev2 - minDistance &&
		currentMomentum > MomentumFilter && currentMomentum > previousMomentum;
		var sellSignal = fastPrev1 < slowPrev1 - minDistance && fastPrev2 > slowPrev2 + minDistance &&
		currentMomentum < -MomentumFilter && currentMomentum < previousMomentum;

		if (buySignal && Position <= 0)
		{
			EnterLong(candle);
		}
		else if (sellSignal && Position >= 0)
		{
			EnterShort(candle);
		}
	}

	private void EnterLong(ICandleMessage candle)
	{
		var volume = Volume + Math.Abs(Position);
		if (volume <= 0)
		return;

		BuyMarket(volume);

		_longEntryPrice = candle.ClosePrice;
		_longStop = StopLossPips > 0m && _pipSize > 0m ? candle.ClosePrice - StopLossPips * _pipSize : null;
		_longTake = TakeProfitPips > 0m && _pipSize > 0m ? candle.ClosePrice + TakeProfitPips * _pipSize : null;

		ResetShortProtection();

		LogInfo($"Entered long at {candle.ClosePrice} with volume {volume}.");
	}

	private void EnterShort(ICandleMessage candle)
	{
		var volume = Volume + Math.Abs(Position);
		if (volume <= 0)
		return;

		SellMarket(volume);

		_shortEntryPrice = candle.ClosePrice;
		_shortStop = StopLossPips > 0m && _pipSize > 0m ? candle.ClosePrice + StopLossPips * _pipSize : null;
		_shortTake = TakeProfitPips > 0m && _pipSize > 0m ? candle.ClosePrice - TakeProfitPips * _pipSize : null;

		ResetLongProtection();

		LogInfo($"Entered short at {candle.ClosePrice} with volume {volume}.");
	}

	private void CheckExitByStops(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
			{
				var volume = Math.Abs(Position);
				if (volume > 0)
				{
					SellMarket(volume);
					LogInfo($"Long stop-loss triggered at {_longStop.Value}.");
				}

				ResetLongProtection();
				return;
			}

			if (_longTake.HasValue && candle.HighPrice >= _longTake.Value)
			{
				var volume = Math.Abs(Position);
				if (volume > 0)
				{
					SellMarket(volume);
					LogInfo($"Long take-profit triggered at {_longTake.Value}.");
				}

				ResetLongProtection();
			}
		}
		else if (Position < 0)
		{
			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
			{
				var volume = Math.Abs(Position);
				if (volume > 0)
				{
					BuyMarket(volume);
					LogInfo($"Short stop-loss triggered at {_shortStop.Value}.");
				}

				ResetShortProtection();
				return;
			}

			if (_shortTake.HasValue && candle.LowPrice <= _shortTake.Value)
			{
				var volume = Math.Abs(Position);
				if (volume > 0)
				{
					BuyMarket(volume);
					LogInfo($"Short take-profit triggered at {_shortTake.Value}.");
				}

				ResetShortProtection();
			}
		}
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m || _pipSize <= 0m)
		return;

		var offset = TrailingStopPips * _pipSize;
		var step = TrailingStepPips * _pipSize;

		if (Position > 0 && _longEntryPrice.HasValue)
		{
			var targetStop = candle.ClosePrice - offset;
			var threshold = candle.ClosePrice - (offset + step);

			if (!_longStop.HasValue || _longStop.Value < threshold)
			{
				_longStop = targetStop;
				LogInfo($"Trailing stop for long moved to {_longStop.Value}.");
			}
		}
		else if (Position < 0 && _shortEntryPrice.HasValue)
		{
			var targetStop = candle.ClosePrice + offset;
			var threshold = candle.ClosePrice + (offset + step);

			if (!_shortStop.HasValue || _shortStop.Value > threshold)
			{
				_shortStop = targetStop;
				LogInfo($"Trailing stop for short moved to {_shortStop.Value}.");
			}
		}
	}

	private bool HasEnoughHistory()
	{
		if (_fastHistory.Count <= FastShift + 2)
		return false;

		if (_slowHistory.Count <= SlowShift + 2)
		return false;

		if (_momentumHistory.Count <= 2)
		return false;

		return true;
	}

	private static void StoreValue(List<decimal> history, decimal value, int capacity)
	{
		history.Insert(0, value);

		if (history.Count > capacity && capacity > 0)
		history.RemoveAt(history.Count - 1);
	}

	private static decimal GetHistoryValue(List<decimal> history, int index)
	{
		return index < history.Count ? history[index] : history[^1];
	}

	private void ResetLongProtection()
	{
		_longEntryPrice = null;
		_longStop = null;
		_longTake = null;
	}

	private void ResetShortProtection()
	{
		_shortEntryPrice = null;
		_shortStop = null;
		_shortTake = null;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return 0m;

		var digits = GetDecimalDigits(step);

		if (digits == 3 || digits == 5)
		return step * 10m;

		return step;
	}

	private static int GetDecimalDigits(decimal value)
	{
		value = Math.Abs(value);
		var digits = 0;

		while (value != Math.Floor(value) && digits < 10)
		{
			value *= 10m;
			digits++;
		}

		return digits;
	}

	private static decimal GetPrice(ICandleMessage candle, CandlePrice priceType)
	{
		return priceType switch
		{
			CandlePrice.Open => candle.OpenPrice,
			CandlePrice.High => candle.HighPrice,
			CandlePrice.Low => candle.LowPrice,
			CandlePrice.Close => candle.ClosePrice,
			CandlePrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			CandlePrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			CandlePrice.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}

	private static MovingAverage CreateMovingAverage(MovingAverageModes mode, int length)
	{
		return mode switch
		{
			MovingAverageModes.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageModes.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageModes.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageModes.Weighted => new WeightedMovingAverage { Length = length },
			_ => new ExponentialMovingAverage { Length = length },
		};
	}
}
