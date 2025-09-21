using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Freeman strategy ported from MetaTrader that layers positions using moving average slope and RSI filters with ATR based risk controls.
/// </summary>
public class FreemanAtrMaRsiGridStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _distancePips;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrStopLossMultiplier;
	private readonly StrategyParam<decimal> _atrTakeProfitMultiplier;
	private readonly StrategyParam<bool> _useTrendFilter;
	private readonly StrategyParam<decimal> _distanceFromMaPips;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MovingAverageMethod> _maMethod;
	private readonly StrategyParam<AppliedPrice> _maPriceType;
	private readonly StrategyParam<bool> _useRsiFilter;
	private readonly StrategyParam<decimal> _rsiLevelUp;
	private readonly StrategyParam<decimal> _rsiLevelDown;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<AppliedPrice> _rsiPriceType;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _currentBarOffset;

	private AverageTrueRange _atr = null!;
	private LengthIndicator<decimal> _ma = null!;
	private RelativeStrengthIndex _rsi = null!;

	private readonly List<decimal> _maValues = new();
	private readonly List<decimal> _rsiValues = new();
	private readonly List<PositionInfo> _longPositions = new();
	private readonly List<PositionInfo> _shortPositions = new();

	private decimal _pipSize;

	/// <summary>
	/// Initializes a new instance of the <see cref="FreemanAtrMaRsiGridStrategy"/> class.
	/// </summary>
	public FreemanAtrMaRsiGridStrategy()
	{
		Volume = 1m;

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for the indicators", "General");

		_maxPositions = Param(nameof(MaxPositions), 5)
		.SetGreaterThanZero()
		.SetDisplay("Max Positions", "Maximum number of simultaneously open positions", "Risk")
		.SetCanOptimize(true);

		_distancePips = Param(nameof(DistancePips), 5m)
		.SetGreaterOrEqual(0m)
		.SetDisplay("Distance Between Entries (pips)", "Minimum price distance between layered entries", "Risk")
		.SetCanOptimize(true);

		_atrPeriod = Param(nameof(AtrPeriod), 9)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "ATR length used for stop loss and take profit", "Indicators")
		.SetCanOptimize(true);

		_atrStopLossMultiplier = Param(nameof(AtrStopLossMultiplier), 3m)
		.SetGreaterOrEqual(0m)
		.SetDisplay("ATR Stop Multiplier", "ATR multiplier that defines the protective stop", "Risk")
		.SetCanOptimize(true);

		_atrTakeProfitMultiplier = Param(nameof(AtrTakeProfitMultiplier), 2m)
		.SetGreaterOrEqual(0m)
		.SetDisplay("ATR Target Multiplier", "ATR multiplier that defines the profit target", "Risk")
		.SetCanOptimize(true);

		_useTrendFilter = Param(nameof(UseTrendFilter), true)
		.SetDisplay("Use MA Trend Filter", "Enable the moving average slope filter", "Filters");

		_distanceFromMaPips = Param(nameof(DistanceFromMaPips), 5m)
		.SetGreaterOrEqual(0m)
		.SetDisplay("Distance From MA (pips)", "Minimum distance between price and MA to validate entries", "Filters")
		.SetCanOptimize(true);

		_maPeriod = Param(nameof(MaPeriod), 30)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Moving average lookback period", "Indicators")
		.SetCanOptimize(true);

		_maShift = Param(nameof(MaShift), 1)
		.SetGreaterOrEqual(0)
		.SetDisplay("MA Shift", "Horizontal shift applied when reading MA values", "Indicators")
		.SetCanOptimize(true);

		_maMethod = Param(nameof(MaMethod), MovingAverageMethod.Simple)
		.SetDisplay("MA Method", "Moving average calculation method", "Indicators");

		_maPriceType = Param(nameof(MaPriceType), AppliedPrice.Median)
		.SetDisplay("MA Price", "Price source used by the moving average", "Indicators");

		_useRsiFilter = Param(nameof(UseRsiFilter), true)
		.SetDisplay("Use RSI Filter", "Enable the RSI level confirmation", "Filters");

		_rsiLevelUp = Param(nameof(RsiLevelUp), 65m)
		.SetDisplay("RSI Upper Level", "Upper RSI threshold that favours shorts", "Filters")
		.SetCanOptimize(true);

		_rsiLevelDown = Param(nameof(RsiLevelDown), 25m)
		.SetDisplay("RSI Lower Level", "Lower RSI threshold that favours longs", "Filters")
		.SetCanOptimize(true);

		_rsiPeriod = Param(nameof(RsiPeriod), 25)
		.SetGreaterThanZero()
		.SetDisplay("RSI Period", "Number of bars for RSI calculation", "Indicators")
		.SetCanOptimize(true);

		_rsiPriceType = Param(nameof(RsiPriceType), AppliedPrice.Median)
		.SetDisplay("RSI Price", "Price source used by RSI", "Indicators");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
		.SetGreaterOrEqual(0m)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance measured in pips", "Risk")
		.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
		.SetGreaterOrEqual(0m)
		.SetDisplay("Trailing Step (pips)", "Additional move required before trail adjustment", "Risk")
		.SetCanOptimize(true);

		_currentBarOffset = Param(nameof(CurrentBarOffset), 0)
		.SetGreaterOrEqual(0)
		.SetDisplay("Indicator Bar Offset", "Bar shift used when reading indicator values", "Indicators")
		.SetCanOptimize(true);
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneously open positions.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Minimum pip distance between consecutive entries.
	/// </summary>
	public decimal DistancePips
	{
		get => _distancePips.Value;
		set => _distancePips.Value = value;
	}

	/// <summary>
	/// ATR averaging period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for the protective stop.
	/// </summary>
	public decimal AtrStopLossMultiplier
	{
		get => _atrStopLossMultiplier.Value;
		set => _atrStopLossMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier for the profit target.
	/// </summary>
	public decimal AtrTakeProfitMultiplier
	{
		get => _atrTakeProfitMultiplier.Value;
		set => _atrTakeProfitMultiplier.Value = value;
	}

	/// <summary>
	/// Enables the moving average trend filter.
	/// </summary>
	public bool UseTrendFilter
	{
		get => _useTrendFilter.Value;
		set => _useTrendFilter.Value = value;
	}

	/// <summary>
	/// Minimum distance required between price and moving average.
	/// </summary>
	public decimal DistanceFromMaPips
	{
		get => _distanceFromMaPips.Value;
		set => _distanceFromMaPips.Value = value;
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Horizontal shift used when retrieving moving average values.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Moving average calculation method.
	/// </summary>
	public MovingAverageMethod MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Price source used by the moving average.
	/// </summary>
	public AppliedPrice MaPriceType
	{
		get => _maPriceType.Value;
		set => _maPriceType.Value = value;
	}

	/// <summary>
	/// Enables the RSI filter.
	/// </summary>
	public bool UseRsiFilter
	{
		get => _useRsiFilter.Value;
		set => _useRsiFilter.Value = value;
	}

	/// <summary>
	/// Upper RSI threshold that favours selling.
	/// </summary>
	public decimal RsiLevelUp
	{
		get => _rsiLevelUp.Value;
		set => _rsiLevelUp.Value = value;
	}

	/// <summary>
	/// Lower RSI threshold that favours buying.
	/// </summary>
	public decimal RsiLevelDown
	{
		get => _rsiLevelDown.Value;
		set => _rsiLevelDown.Value = value;
	}

	/// <summary>
	/// RSI averaging period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Price source used by RSI.
	/// </summary>
	public AppliedPrice RsiPriceType
	{
		get => _rsiPriceType.Value;
		set => _rsiPriceType.Value = value;
	}

	/// <summary>
	/// Trailing stop distance measured in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step required before the stop is moved again.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Offset applied when retrieving indicator values.
	/// </summary>
	public int CurrentBarOffset
	{
		get => _currentBarOffset.Value;
		set => _currentBarOffset.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_ma = CreateMovingAverage(MaMethod, MaPeriod);
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		_maValues.Clear();
		_rsiValues.Clear();
		_longPositions.Clear();
		_shortPositions.Clear();
		_pipSize = 0m;

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
			{
			DrawCandles(area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Feed technical indicators with the completed candle.
		var atrValue = _atr.Process(candle).ToNullableDecimal();
		var maInput = GetAppliedPrice(candle, MaPriceType);
		var maValue = _ma.Process(maInput, candle.OpenTime, true).ToNullableDecimal();
		if (maValue != null)
			AddValue(_maValues, maValue.Value);

		var rsiInput = GetAppliedPrice(candle, RsiPriceType);
		var rsiValue = _rsi.Process(rsiInput, candle.OpenTime, true).ToNullableDecimal();
		if (rsiValue != null)
			AddValue(_rsiValues, rsiValue.Value);

		// Skip processing until the strategy is fully ready to trade.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (atrValue == null)
			return;

		// Convert pip based settings into price units when security data is known.
		EnsurePipSize();

		// Refresh trailing stops before managing exits.
		UpdateTrailingStops(candle);
		// Close positions that hit stop loss or take profit on this candle.
		ManageExits(candle);

		var signal = CalculateSignal(candle);
		// No confirmed signal -> skip trading decisions for this bar.
		if (signal == 0)
			return;

		// Respect the configured limit for simultaneously open positions.
		if (_longPositions.Count + _shortPositions.Count >= MaxPositions)
			return;

		if (signal == 1)
			{
				// Long side logic: add to the grid when price falls by the required distance.
			if (_shortPositions.Count > 0)
				return;

			if (_longPositions.Count == 0)
				{
				OpenLong(candle, atrValue.Value);
			}
			else
				{
				var requiredDistance = DistancePips * _pipSize;
				var lastEntry = _longPositions[^1];
				if (DistancePips <= 0m || lastEntry.EntryPrice - candle.ClosePrice >= requiredDistance)
					OpenLong(candle, atrValue.Value);
			}
		}
		else if (signal == -1)
			{
				// Short side logic: layer entries when price rises enough against the position.
			if (_longPositions.Count > 0)
				return;

			if (_shortPositions.Count == 0)
				{
				OpenShort(candle, atrValue.Value);
			}
			else
				{
				var requiredDistance = DistancePips * _pipSize;
				var lastEntry = _shortPositions[^1];
				if (DistancePips <= 0m || candle.ClosePrice - lastEntry.EntryPrice >= requiredDistance)
					OpenShort(candle, atrValue.Value);
			}
		}
	}

	private int CalculateSignal(ICandleMessage candle)
	{
		var maSignal = 0;
		if (UseTrendFilter)
			{
				// Determine the slope of the moving average.
			var offset = CurrentBarOffset + MaShift;
			if (!TryGetSeriesValue(_maValues, offset, out var currentMa) ||
				!TryGetSeriesValue(_maValues, offset + 1, out var previousMa))
			return 0;

			if (currentMa > previousMa)
				maSignal = 1;
			else if (currentMa < previousMa)
				maSignal = -1;
			else
				return 0;

			if (DistanceFromMaPips > 0m)
				{
					// Enforce a minimum gap between price and the moving average before trading.
				var distanceThreshold = DistanceFromMaPips * _pipSize;
				var referencePrice = candle.ClosePrice;

				if (maSignal == 1 && referencePrice - currentMa < distanceThreshold)
					return 0;

				if (maSignal == -1 && currentMa - referencePrice < distanceThreshold)
					return 0;
			}
		}

		var rsiSignal = 0;
		if (UseRsiFilter)
			{
				// Evaluate RSI against overbought/oversold thresholds.
			if (!TryGetSeriesValue(_rsiValues, CurrentBarOffset, out var currentRsi))
				return 0;

			if (currentRsi > RsiLevelUp)
				rsiSignal = -11;
			else if (currentRsi < RsiLevelDown)
				rsiSignal = 1;
			else
				return 0;
		}

		if (!UseTrendFilter)
			return rsiSignal;

		if (!UseRsiFilter)
			return maSignal;

		// Combine filters according to the original MetaTrader behaviour.
		return maSignal == rsiSignal ? maSignal : 0;
	}

	private void OpenLong(ICandleMessage candle, decimal atrValue)
	{
		var entryPrice = candle.ClosePrice;
		// Calculate initial stop loss and take profit from ATR multiples.
		var stop = AtrStopLossMultiplier > 0m ? entryPrice - atrValue * AtrStopLossMultiplier : (decimal?)null;
		var target = AtrTakeProfitMultiplier > 0m ? entryPrice + atrValue * AtrTakeProfitMultiplier : (decimal?)null;

		// Submit market order for the configured volume.
		BuyMarket(Volume);

		_longPositions.Add(new PositionInfo
		{
			EntryPrice = entryPrice,
			Volume = Volume,
			StopLoss = stop,
			TakeProfit = target,
			IsLong = true
		});
	}

	private void OpenShort(ICandleMessage candle, decimal atrValue)
	{
		var entryPrice = candle.ClosePrice;
		// Calculate initial stop loss and take profit from ATR multiples.
		var stop = AtrStopLossMultiplier > 0m ? entryPrice + atrValue * AtrStopLossMultiplier : (decimal?)null;
		var target = AtrTakeProfitMultiplier > 0m ? entryPrice - atrValue * AtrTakeProfitMultiplier : (decimal?)null;

		// Submit market order for the configured volume.
		SellMarket(Volume);

		_shortPositions.Add(new PositionInfo
		{
			EntryPrice = entryPrice,
			Volume = Volume,
			StopLoss = stop,
			TakeProfit = target,
			IsLong = false
		});
	}

	private void ManageExits(ICandleMessage candle)
	{
		var longToClose = new List<PositionInfo>();
		foreach (var position in _longPositions)
		{
			if (position.StopLoss.HasValue && candle.LowPrice <= position.StopLoss.Value)
				{
				longToClose.Add(position);
				continue;
			}

			if (position.TakeProfit.HasValue && candle.HighPrice >= position.TakeProfit.Value)
				longToClose.Add(position);
		}

		foreach (var position in longToClose)
		{
			// Exit the long position when stop loss or take profit is reached.
			SellMarket(position.Volume);
			_longPositions.Remove(position);
		}

		var shortToClose = new List<PositionInfo>();
		foreach (var position in _shortPositions)
		{
			if (position.StopLoss.HasValue && candle.HighPrice >= position.StopLoss.Value)
				{
				shortToClose.Add(position);
				continue;
			}

			if (position.TakeProfit.HasValue && candle.LowPrice <= position.TakeProfit.Value)
				shortToClose.Add(position);
		}

		foreach (var position in shortToClose)
		{
			// Exit the short position when stop loss or take profit is reached.
			BuyMarket(position.Volume);
			_shortPositions.Remove(position);
		}
	}

	private void UpdateTrailingStops(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m)
			return;

		// Convert trailing configuration into price distances.

		var trailingDistance = TrailingStopPips * _pipSize;
		var activationDistance = (TrailingStopPips + TrailingStepPips) * _pipSize;

		foreach (var position in _longPositions)
		{
			var profit = candle.ClosePrice - position.EntryPrice;
			if (profit > activationDistance)
				{
				var newStop = candle.ClosePrice - trailingDistance;
				if (!position.StopLoss.HasValue || newStop > position.StopLoss.Value)
					position.StopLoss = newStop;
			}
		}

		foreach (var position in _shortPositions)
		{
			var profit = position.EntryPrice - candle.ClosePrice;
			if (profit > activationDistance)
				{
				var newStop = candle.ClosePrice + trailingDistance;
				if (!position.StopLoss.HasValue || newStop < position.StopLoss.Value)
					position.StopLoss = newStop;
			}
		}
	}

	private void EnsurePipSize()
	{
		if (_pipSize > 0m)
			return;

		// Derive pip size from the instrument price step to mimic MetaTrader behaviour.
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
			{
			_pipSize = 1m;
			return;
		}

		var decimals = Security?.Decimals;
		if (decimals != null && (decimals.Value == 3 || decimals.Value == 5))
			{
			_pipSize = priceStep * 10m;
			return;
		}

		_pipSize = priceStep;
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPrice priceType)
	{
		return priceType switch
		{
			AppliedPrice.Close => candle.ClosePrice,
			AppliedPrice.Open => candle.OpenPrice,
			AppliedPrice.High => candle.HighPrice,
			AppliedPrice.Low => candle.LowPrice,
			AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPrice.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m,
			_ => candle.ClosePrice
		};
	}

	private static bool TryGetSeriesValue(List<decimal> values, int offset, out decimal value)
	{
		var index = values.Count - 1 - offset;
		if (index < 0)
			{
			value = 0m;
			return false;
		}

		value = values[index];
		return true;
	}

	private static void AddValue(List<decimal> values, decimal value)
	{
		values.Add(value);
		const int maxCount = 5000;
		if (values.Count > maxCount)
			values.RemoveAt(0);
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageMethod method, int length)
	{
		return method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageMethod.Weighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length }
		};
	}

	private sealed class PositionInfo
	{
		public bool IsLong { get; set; }
		public decimal EntryPrice { get; set; }
		public decimal Volume { get; set; }
		public decimal? StopLoss { get; set; }
		public decimal? TakeProfit { get; set; }
	}

	/// <summary>
	/// Moving average calculation methods supported by the strategy.
	/// </summary>
	public enum MovingAverageMethod
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted
	}

	/// <summary>
	/// Price sources that mimic MetaTrader applied price constants.
	/// </summary>
	public enum AppliedPrice
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
