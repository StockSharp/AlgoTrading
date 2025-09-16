using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that emulates the "Crossing of two iMA" MQL5 expert advisor.
/// It trades crossovers between two configurable moving averages with an optional third filter average.
/// Supports manual volume or percentage risk based sizing, simulated pending orders and trailing stop management.
/// </summary>
public class CrossingOfTwoIMAStrategy : Strategy
{
	/// <summary>
	/// Moving average calculation methods supported by the strategy.
	/// </summary>
	public enum MovingAverageMethod
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
		/// Smoothed (RMA) moving average.
		/// </summary>
		Smoothed,

		/// <summary>
		/// Linear weighted moving average.
		/// </summary>
		Weighted,
	}

	private readonly StrategyParam<int> _firstPeriod;
	private readonly StrategyParam<int> _firstShift;
	private readonly StrategyParam<MovingAverageMethod> _firstMethod;

	private readonly StrategyParam<int> _secondPeriod;
	private readonly StrategyParam<int> _secondShift;
	private readonly StrategyParam<MovingAverageMethod> _secondMethod;

	private readonly StrategyParam<bool> _useThirdAverage;
	private readonly StrategyParam<int> _thirdPeriod;
	private readonly StrategyParam<int> _thirdShift;
	private readonly StrategyParam<MovingAverageMethod> _thirdMethod;

	private readonly StrategyParam<bool> _useFixedVolume;
	private readonly StrategyParam<decimal> _riskPercent;

	private readonly StrategyParam<int> _priceLevelPips;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverage? _firstMa;
	private MovingAverage? _secondMa;
	private MovingAverage? _thirdMa;

	private readonly List<decimal> _firstValues = new();
	private readonly List<decimal> _secondValues = new();
	private readonly List<decimal> _thirdValues = new();
	private readonly List<DateTimeOffset> _openTimes = new();

	private decimal _pipSize;
	private decimal? _entryPrice;
	private decimal? _activeStopLoss;
	private decimal? _activeTakeProfit;
	private bool _isLongPosition;
	private PendingOrder? _pendingOrder;
	private DateTimeOffset? _lastEntryTime;

	private enum PendingOrderType
	{
		None,
		BuyStop,
		BuyLimit,
		SellStop,
		SellLimit,
	}

	private sealed class PendingOrder
	{
		public PendingOrderType Type { get; init; }
		public decimal EntryPrice { get; init; }
		public decimal? StopLoss { get; init; }
		public decimal? TakeProfit { get; init; }
		public decimal Volume { get; init; }
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CrossingOfTwoIMAStrategy"/> class.
	/// </summary>
	public CrossingOfTwoIMAStrategy()
	{
		_firstPeriod = Param(nameof(FirstMaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("First MA Period", "Period of the first moving average", "First Moving Average")
			.SetCanOptimize(true)
			.SetOptimize(2, 30, 1);

		_firstShift = Param(nameof(FirstMaShift), 3)
			.SetGreaterOrEqual(0)
			.SetDisplay("First MA Shift", "Shift applied to the first moving average", "First Moving Average");

		_firstMethod = Param(nameof(FirstMaMethod), MovingAverageMethod.Smoothed)
			.SetDisplay("First MA Method", "Calculation method of the first moving average", "First Moving Average");

		_secondPeriod = Param(nameof(SecondMaPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Second MA Period", "Period of the second moving average", "Second Moving Average")
			.SetCanOptimize(true)
			.SetOptimize(3, 60, 1);

		_secondShift = Param(nameof(SecondMaShift), 5)
			.SetGreaterOrEqual(0)
			.SetDisplay("Second MA Shift", "Shift applied to the second moving average", "Second Moving Average");

		_secondMethod = Param(nameof(SecondMaMethod), MovingAverageMethod.Smoothed)
			.SetDisplay("Second MA Method", "Calculation method of the second moving average", "Second Moving Average");

		_useThirdAverage = Param(nameof(UseThirdMovingAverage), true)
			.SetDisplay("Use Third MA", "Enable the third moving average as a directional filter", "Third Moving Average");

		_thirdPeriod = Param(nameof(ThirdMaPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Third MA Period", "Period of the third moving average", "Third Moving Average");

		_thirdShift = Param(nameof(ThirdMaShift), 8)
			.SetGreaterOrEqual(0)
			.SetDisplay("Third MA Shift", "Shift applied to the third moving average", "Third Moving Average");

		_thirdMethod = Param(nameof(ThirdMaMethod), MovingAverageMethod.Smoothed)
			.SetDisplay("Third MA Method", "Calculation method of the third moving average", "Third Moving Average");

		_useFixedVolume = Param(nameof(UseFixedVolume), true)
			.SetDisplay("Use Fixed Volume", "Use the strategy volume directly instead of risk based sizing", "Money Management");

		_riskPercent = Param(nameof(RiskPercent), 5m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Risk %", "Risk percentage of portfolio value per trade when position sizing is dynamic", "Money Management");

		_priceLevelPips = Param(nameof(PriceLevelPips), 0)
			.SetDisplay("Price Level (pips)", "Offset in pips for simulated pending orders (negative for stop, positive for limit)", "Orders");

		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetGreaterOrEqual(0)
			.SetDisplay("Stop Loss (pips)", "Initial stop loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetGreaterOrEqual(0)
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 10)
			.SetGreaterOrEqual(0)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 4)
			.SetGreaterOrEqual(0)
			.SetDisplay("Trailing Step (pips)", "Additional progress in pips required before the trailing stop is advanced", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series used for signals", "General");
	}

	/// <summary>
	/// Period of the first moving average.
	/// </summary>
	public int FirstMaPeriod
	{
		get => _firstPeriod.Value;
		set => _firstPeriod.Value = value;
	}

	/// <summary>
	/// Shift (in bars) of the first moving average.
	/// </summary>
	public int FirstMaShift
	{
		get => _firstShift.Value;
		set => _firstShift.Value = value;
	}

	/// <summary>
	/// Method used for the first moving average.
	/// </summary>
	public MovingAverageMethod FirstMaMethod
	{
		get => _firstMethod.Value;
		set => _firstMethod.Value = value;
	}

	/// <summary>
	/// Period of the second moving average.
	/// </summary>
	public int SecondMaPeriod
	{
		get => _secondPeriod.Value;
		set => _secondPeriod.Value = value;
	}

	/// <summary>
	/// Shift (in bars) of the second moving average.
	/// </summary>
	public int SecondMaShift
	{
		get => _secondShift.Value;
		set => _secondShift.Value = value;
	}

	/// <summary>
	/// Method used for the second moving average.
	/// </summary>
	public MovingAverageMethod SecondMaMethod
	{
		get => _secondMethod.Value;
		set => _secondMethod.Value = value;
	}

	/// <summary>
	/// Enables the third moving average filter.
	/// </summary>
	public bool UseThirdMovingAverage
	{
		get => _useThirdAverage.Value;
		set => _useThirdAverage.Value = value;
	}

	/// <summary>
	/// Period of the third moving average.
	/// </summary>
	public int ThirdMaPeriod
	{
		get => _thirdPeriod.Value;
		set => _thirdPeriod.Value = value;
	}

	/// <summary>
	/// Shift (in bars) of the third moving average.
	/// </summary>
	public int ThirdMaShift
	{
		get => _thirdShift.Value;
		set => _thirdShift.Value = value;
	}

	/// <summary>
	/// Method used for the third moving average.
	/// </summary>
	public MovingAverageMethod ThirdMaMethod
	{
		get => _thirdMethod.Value;
		set => _thirdMethod.Value = value;
	}

	/// <summary>
	/// Use fixed volume or percentage based sizing.
	/// </summary>
	public bool UseFixedVolume
	{
		get => _useFixedVolume.Value;
		set => _useFixedVolume.Value = value;
	}

	/// <summary>
	/// Risk percentage per trade when dynamic sizing is active.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Offset in pips that defines simulated pending order behavior.
	/// </summary>
	public int PriceLevelPips
	{
		get => _priceLevelPips.Value;
		set => _priceLevelPips.Value = value;
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
	/// Required additional progress (in pips) before advancing the trailing stop.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Primary candle type used for signal generation.
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

		_firstValues.Clear();
		_secondValues.Clear();
		_thirdValues.Clear();
		_openTimes.Clear();

		_entryPrice = null;
		_activeStopLoss = null;
		_activeTakeProfit = null;
		_isLongPosition = false;
		_pendingOrder = null;
		_lastEntryTime = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_firstMa = CreateMovingAverage(FirstMaMethod, FirstMaPeriod);
		_secondMa = CreateMovingAverage(SecondMaMethod, SecondMaPeriod);
		_thirdMa = UseThirdMovingAverage ? CreateMovingAverage(ThirdMaMethod, ThirdMaPeriod) : null;

		_firstValues.Clear();
		_secondValues.Clear();
		_thirdValues.Clear();
		_openTimes.Clear();

		_pipSize = Security?.PriceStep ?? 1m;
		var decimals = Security?.Decimals;
		if (decimals == 3 || decimals == 5)
			_pipSize *= 10m;

		var subscription = SubscribeCandles(CandleType);

		if (UseThirdMovingAverage && _thirdMa != null)
		{
			subscription
				.Bind(_firstMa, _secondMa, _thirdMa, ProcessCandle)
				.Start();
		}
		else
		{
			subscription
				.Bind(_firstMa, _secondMa, ProcessCandle)
				.Start();
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal firstValue, decimal secondValue)
	{
		ProcessCandleInternal(candle, firstValue, secondValue, null);
	}

	private void ProcessCandle(ICandleMessage candle, decimal firstValue, decimal secondValue, decimal thirdValue)
	{
		ProcessCandleInternal(candle, firstValue, secondValue, thirdValue);
	}

	private void ProcessCandleInternal(ICandleMessage candle, decimal firstValue, decimal secondValue, decimal? thirdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateOpenTimes(candle.OpenTime);

		HandlePendingOrders(candle);

		var positionChanged = false;
		ManageActivePosition(candle, ref positionChanged);

		UpdateSeries(_firstValues, FirstMaShift, firstValue);
		UpdateSeries(_secondValues, SecondMaShift, secondValue);

		if (UseThirdMovingAverage && thirdValue.HasValue)
			UpdateSeries(_thirdValues, ThirdMaShift, thirdValue.Value);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_firstMa?.IsFormed != true || _secondMa?.IsFormed != true)
			return;

		decimal? thirdCurrent = null;
		if (UseThirdMovingAverage)
		{
			if (_thirdMa?.IsFormed != true)
				return;

			thirdCurrent = GetSeriesValue(_thirdValues, ThirdMaShift, 0);
		}

		var first0 = GetSeriesValue(_firstValues, FirstMaShift, 0);
		var first1 = GetSeriesValue(_firstValues, FirstMaShift, 1);
		var first2 = GetSeriesValue(_firstValues, FirstMaShift, 2);

		var second0 = GetSeriesValue(_secondValues, SecondMaShift, 0);
		var second1 = GetSeriesValue(_secondValues, SecondMaShift, 1);
		var second2 = GetSeriesValue(_secondValues, SecondMaShift, 2);

		if (first0 is null || first1 is null || second0 is null || second1 is null)
			return;

		var priceLevelOffset = Math.Abs(PriceLevelPips) * _pipSize;

		var stopLoss = StopLossPips > 0 ? StopLossPips * _pipSize : 0m;
		var takeProfit = TakeProfitPips > 0 ? TakeProfitPips * _pipSize : 0m;

		var currentOpenTime = candle.OpenTime;
		var startTime = GetOpenTime(3) ?? DateTimeOffset.MinValue;
		var recentEntry = _lastEntryTime.HasValue && _lastEntryTime.Value >= startTime && _lastEntryTime.Value < currentOpenTime;

		if (first0 > second0 && first1 < second1)
		{
			if (!UseThirdMovingAverage || thirdCurrent is null || thirdCurrent < first0)
			{
				EnterLong(candle, stopLoss, takeProfit, priceLevelOffset);
				return;
			}
		}
		else if (first0 < second0 && first1 > second1)
		{
			if (!UseThirdMovingAverage || thirdCurrent is null || thirdCurrent > first0)
			{
				EnterShort(candle, stopLoss, takeProfit, priceLevelOffset);
				return;
			}
		}
		else if (first0 > second0 && first2 is not null && second2 is not null && first2 < second2)
		{
			if (!recentEntry && (!UseThirdMovingAverage || thirdCurrent is null || thirdCurrent < first0))
			{
				EnterLong(candle, stopLoss, takeProfit, priceLevelOffset);
				return;
			}
		}
		else if (first0 < second2 && first1 > second2 && second2 is not null)
		{
			if (!recentEntry && (!UseThirdMovingAverage || thirdCurrent is null || thirdCurrent > first0))
			{
				EnterShort(candle, stopLoss, takeProfit, priceLevelOffset);
			}
		}
	}

	private void EnterLong(ICandleMessage candle, decimal stopLossOffset, decimal takeProfitOffset, decimal priceLevelOffset)
	{
		if (Position > 0)
			return;

		var entryPrice = candle.ClosePrice;
		var stopPrice = stopLossOffset > 0m ? entryPrice - stopLossOffset : (decimal?)null;
		var takePrice = takeProfitOffset > 0m ? entryPrice + takeProfitOffset : (decimal?)null;

		var volume = CalculateOrderVolume(entryPrice, stopPrice);
		if (volume <= 0m)
			return;

		CancelPendingOrders();

		if (PriceLevelPips == 0)
		{
			var totalVolume = volume + (Position < 0 ? Math.Abs(Position) : 0m);
			if (totalVolume <= 0m)
				return;

			BuyMarket(totalVolume);
			_entryPrice = entryPrice;
			_activeStopLoss = stopPrice;
			_activeTakeProfit = takePrice;
			_isLongPosition = true;
			_lastEntryTime = candle.OpenTime;
		}
		else if (PriceLevelPips < 0)
		{
			var targetPrice = entryPrice + priceLevelOffset;
			var stop = stopPrice.HasValue ? stopPrice.Value + priceLevelOffset : (decimal?)null;
			var take = takePrice.HasValue ? takePrice.Value + priceLevelOffset : (decimal?)null;
			_pendingOrder = new PendingOrder
			{
				Type = PendingOrderType.BuyStop,
				EntryPrice = targetPrice,
				StopLoss = stop,
				TakeProfit = take,
				Volume = volume,
			};
		}
		else
		{
			var targetPrice = entryPrice - priceLevelOffset;
			var stop = stopPrice.HasValue ? stopPrice.Value - priceLevelOffset : (decimal?)null;
			var take = takePrice.HasValue ? takePrice.Value - priceLevelOffset : (decimal?)null;
			_pendingOrder = new PendingOrder
			{
				Type = PendingOrderType.BuyLimit,
				EntryPrice = targetPrice,
				StopLoss = stop,
				TakeProfit = take,
				Volume = volume,
			};
		}
	}

	private void EnterShort(ICandleMessage candle, decimal stopLossOffset, decimal takeProfitOffset, decimal priceLevelOffset)
	{
		if (Position < 0)
			return;

		var entryPrice = candle.ClosePrice;
		var stopPrice = stopLossOffset > 0m ? entryPrice + stopLossOffset : (decimal?)null;
		var takePrice = takeProfitOffset > 0m ? entryPrice - takeProfitOffset : (decimal?)null;

		var volume = CalculateOrderVolume(entryPrice, stopPrice);
		if (volume <= 0m)
			return;

		CancelPendingOrders();

		if (PriceLevelPips == 0)
		{
			var totalVolume = volume + (Position > 0 ? Math.Abs(Position) : 0m);
			if (totalVolume <= 0m)
				return;

			SellMarket(totalVolume);
			_entryPrice = entryPrice;
			_activeStopLoss = stopPrice;
			_activeTakeProfit = takePrice;
			_isLongPosition = false;
			_lastEntryTime = candle.OpenTime;
		}
		else if (PriceLevelPips < 0)
		{
			var targetPrice = entryPrice - priceLevelOffset;
			var stop = stopPrice.HasValue ? stopPrice.Value - priceLevelOffset : (decimal?)null;
			var take = takePrice.HasValue ? takePrice.Value - priceLevelOffset : (decimal?)null;
			_pendingOrder = new PendingOrder
			{
				Type = PendingOrderType.SellStop,
				EntryPrice = targetPrice,
				StopLoss = stop,
				TakeProfit = take,
				Volume = volume,
			};
		}
		else
		{
			var targetPrice = entryPrice + priceLevelOffset;
			var stop = stopPrice.HasValue ? stopPrice.Value + priceLevelOffset : (decimal?)null;
			var take = takePrice.HasValue ? takePrice.Value + priceLevelOffset : (decimal?)null;
			_pendingOrder = new PendingOrder
			{
				Type = PendingOrderType.SellLimit,
				EntryPrice = targetPrice,
				StopLoss = stop,
				TakeProfit = take,
				Volume = volume,
			};
		}
	}

	private void HandlePendingOrders(ICandleMessage candle)
	{
		if (_pendingOrder is null)
			return;

		var triggered = _pendingOrder.Type switch
		{
			PendingOrderType.BuyStop => candle.HighPrice >= _pendingOrder.EntryPrice,
			PendingOrderType.BuyLimit => candle.LowPrice <= _pendingOrder.EntryPrice,
			PendingOrderType.SellStop => candle.LowPrice <= _pendingOrder.EntryPrice,
			PendingOrderType.SellLimit => candle.HighPrice >= _pendingOrder.EntryPrice,
			_ => false,
		};

		if (!triggered)
			return;

		var volume = _pendingOrder.Volume;
		if (volume <= 0m)
		{
			_pendingOrder = null;
			return;
		}

		if (_pendingOrder.Type == PendingOrderType.BuyStop || _pendingOrder.Type == PendingOrderType.BuyLimit)
		{
			var totalVolume = volume + (Position < 0 ? Math.Abs(Position) : 0m);
			if (totalVolume > 0m)
			{
				BuyMarket(totalVolume);
				_entryPrice = _pendingOrder.EntryPrice;
				_activeStopLoss = _pendingOrder.StopLoss;
				_activeTakeProfit = _pendingOrder.TakeProfit;
				_isLongPosition = true;
				_lastEntryTime = candle.OpenTime;
			}
		}
		else
		{
			var totalVolume = volume + (Position > 0 ? Math.Abs(Position) : 0m);
			if (totalVolume > 0m)
			{
				SellMarket(totalVolume);
				_entryPrice = _pendingOrder.EntryPrice;
				_activeStopLoss = _pendingOrder.StopLoss;
				_activeTakeProfit = _pendingOrder.TakeProfit;
				_isLongPosition = false;
				_lastEntryTime = candle.OpenTime;
			}
		}

		_pendingOrder = null;
	}

	private void ManageActivePosition(ICandleMessage candle, ref bool positionChanged)
	{
		if (Position == 0)
			return;

		var positionVolume = Math.Abs(Position);
		if (positionVolume <= 0m)
			return;

		if (_isLongPosition)
		{
			if (_activeTakeProfit.HasValue && candle.HighPrice >= _activeTakeProfit.Value)
			{
				SellMarket(positionVolume);
				ResetPositionState();
				positionChanged = true;
				return;
			}

			if (_activeStopLoss.HasValue && candle.LowPrice <= _activeStopLoss.Value)
			{
				SellMarket(positionVolume);
				ResetPositionState();
				positionChanged = true;
				return;
			}

			UpdateTrailingForLong(candle);
		}
		else
		{
			if (_activeTakeProfit.HasValue && candle.LowPrice <= _activeTakeProfit.Value)
			{
				BuyMarket(positionVolume);
				ResetPositionState();
				positionChanged = true;
				return;
			}

			if (_activeStopLoss.HasValue && candle.HighPrice >= _activeStopLoss.Value)
			{
				BuyMarket(positionVolume);
				ResetPositionState();
				positionChanged = true;
				return;
			}

			UpdateTrailingForShort(candle);
		}
	}

	private void UpdateTrailingForLong(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;

		var targetStop = candle.ClosePrice - trailingDistance;
		if (!_activeStopLoss.HasValue || targetStop <= _activeStopLoss.Value)
			return;

		if (trailingStep <= 0m || _activeStopLoss.Value < targetStop - trailingStep)
			_activeStopLoss = targetStop;
	}

	private void UpdateTrailingForShort(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;

		var targetStop = candle.ClosePrice + trailingDistance;
		if (!_activeStopLoss.HasValue || targetStop >= _activeStopLoss.Value)
			return;

		if (trailingStep <= 0m || _activeStopLoss.Value > targetStop + trailingStep)
			_activeStopLoss = targetStop;
	}

	private decimal CalculateOrderVolume(decimal entryPrice, decimal? stopPrice)
	{
		if (UseFixedVolume || !stopPrice.HasValue)
			return Volume;

		var riskDistance = Math.Abs(entryPrice - stopPrice.Value);
		if (riskDistance <= 0m)
			return 0m;

		var equity = Portfolio?.CurrentValue ?? 0m;
		var riskAmount = equity * RiskPercent / 100m;
		return riskAmount > 0m ? riskAmount / riskDistance : 0m;
	}

	private void CancelPendingOrders()
	{
		_pendingOrder = null;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_activeStopLoss = null;
		_activeTakeProfit = null;
		_isLongPosition = false;
	}

	private void UpdateSeries(List<decimal> values, int shift, decimal value)
	{
		values.Add(value);
		var maxSize = Math.Max(shift + 3, 3);
		while (values.Count > maxSize)
			values.RemoveAt(0);
	}

	private static decimal? GetSeriesValue(List<decimal> values, int shift, int index)
	{
		var targetIndex = values.Count - 1 - shift - index;
		if (targetIndex < 0 || targetIndex >= values.Count)
			return null;

		return values[targetIndex];
	}

	private void UpdateOpenTimes(DateTimeOffset openTime)
	{
		_openTimes.Add(openTime);
		while (_openTimes.Count > 4)
			_openTimes.RemoveAt(0);
	}

	private DateTimeOffset? GetOpenTime(int index)
	{
		var targetIndex = _openTimes.Count - 1 - index;
		if (targetIndex < 0 || targetIndex >= _openTimes.Count)
			return null;

		return _openTimes[targetIndex];
	}

	private static MovingAverage CreateMovingAverage(MovingAverageMethod method, int length)
	{
		MovingAverage ma = method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage(),
			MovingAverageMethod.Exponential => new ExponentialMovingAverage(),
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage(),
			MovingAverageMethod.Weighted => new WeightedMovingAverage(),
			_ => new SimpleMovingAverage(),
		};

		ma.Length = Math.Max(1, length);
		return ma;
	}
}
