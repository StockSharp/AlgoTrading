using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the Brandy Expert Advisor from MetaTrader 5.
/// Combines two configurable moving averages to generate entries and manages positions with trailing exits.
/// </summary>
public class BrandyStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _maClosePeriod;
	private readonly StrategyParam<int> _maCloseShift;
	private readonly StrategyParam<MovingAverageMethod> _maCloseMethod;
	private readonly StrategyParam<AppliedPriceType> _maCloseAppliedPrice;
	private readonly StrategyParam<int> _maCloseSignalBar;
	private readonly StrategyParam<int> _maOpenPeriod;
	private readonly StrategyParam<int> _maOpenShift;
	private readonly StrategyParam<MovingAverageMethod> _maOpenMethod;
	private readonly StrategyParam<AppliedPriceType> _maOpenAppliedPrice;
	private readonly StrategyParam<int> _maOpenSignalBar;
	private readonly StrategyParam<DataType> _candleType;

	private LengthIndicator<decimal>? _maOpenIndicator;
	private LengthIndicator<decimal>? _maCloseIndicator;
	private decimal _pipSize;
	private readonly Queue<decimal> _maOpenValues = new();
	private readonly Queue<decimal> _maCloseValues = new();
	private int _maxOpenQueueSize;
	private int _maxCloseQueueSize;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// Trading volume per order.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set
		{
			_tradeVolume.Value = value;
			Volume = value;
		}
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Step that defines how far the price must move before the trailing stop is advanced.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Period of the moving average calculated on the close series.
	/// </summary>
	public int MaClosePeriod
	{
		get => _maClosePeriod.Value;
		set => _maClosePeriod.Value = value;
	}

	/// <summary>
	/// Displacement applied to the moving average calculated on closes.
	/// </summary>
	public int MaCloseShift
	{
		get => _maCloseShift.Value;
		set => _maCloseShift.Value = value;
	}

	/// <summary>
	/// Moving average smoothing method for the close series.
	/// </summary>
	public MovingAverageMethod MaCloseMethod
	{
		get => _maCloseMethod.Value;
		set => _maCloseMethod.Value = value;
	}

	/// <summary>
	/// Price source used by the close moving average.
	/// </summary>
	public AppliedPriceType MaCloseAppliedPrice
	{
		get => _maCloseAppliedPrice.Value;
		set => _maCloseAppliedPrice.Value = value;
	}

	/// <summary>
	/// Bar index used as a signal reference for the close moving average.
	/// </summary>
	public int MaCloseSignalBar
	{
		get => _maCloseSignalBar.Value;
		set => _maCloseSignalBar.Value = value;
	}

	/// <summary>
	/// Period of the moving average calculated on the open series.
	/// </summary>
	public int MaOpenPeriod
	{
		get => _maOpenPeriod.Value;
		set => _maOpenPeriod.Value = value;
	}

	/// <summary>
	/// Displacement applied to the moving average calculated on opens.
	/// </summary>
	public int MaOpenShift
	{
		get => _maOpenShift.Value;
		set => _maOpenShift.Value = value;
	}

	/// <summary>
	/// Moving average smoothing method for the open series.
	/// </summary>
	public MovingAverageMethod MaOpenMethod
	{
		get => _maOpenMethod.Value;
		set => _maOpenMethod.Value = value;
	}

	/// <summary>
	/// Price source used by the open moving average.
	/// </summary>
	public AppliedPriceType MaOpenAppliedPrice
	{
		get => _maOpenAppliedPrice.Value;
		set => _maOpenAppliedPrice.Value = value;
	}

	/// <summary>
	/// Bar index used as a signal reference for the open moving average.
	/// </summary>
	public int MaOpenSignalBar
	{
		get => _maOpenSignalBar.Value;
		set => _maOpenSignalBar.Value = value;
	}

	/// <summary>
	/// Candle type used to feed the indicators.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BrandyStrategy"/> class.
	/// </summary>
	public BrandyStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Order size in lots", "General");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetNotNegative()
		.SetDisplay("Stop Loss (pips)", "Protective stop distance", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 150m)
		.SetNotNegative()
		.SetDisplay("Take Profit (pips)", "Profit target distance", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
		.SetNotNegative()
		.SetDisplay("Trailing Stop (pips)", "Distance for trailing stop", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
		.SetNotNegative()
		.SetDisplay("Trailing Step (pips)", "Additional move required before trailing", "Risk");

		_maClosePeriod = Param(nameof(MaClosePeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("MA Close Period", "Length of MA calculated on close", "Indicators");

		_maCloseShift = Param(nameof(MaCloseShift), 0)
		.SetNotNegative()
		.SetDisplay("MA Close Shift", "Forward shift applied to close MA", "Indicators");

		_maCloseMethod = Param(nameof(MaCloseMethod), MovingAverageMethod.Ema)
		.SetDisplay("MA Close Method", "Smoothing method for close MA", "Indicators");

		_maCloseAppliedPrice = Param(nameof(MaCloseAppliedPrice), AppliedPriceType.Close)
		.SetDisplay("MA Close Price", "Price source for close MA", "Indicators");

		_maCloseSignalBar = Param(nameof(MaCloseSignalBar), 0)
		.SetNotNegative()
		.SetDisplay("MA Close Signal Bar", "Reference bar index for close MA", "Indicators");

		_maOpenPeriod = Param(nameof(MaOpenPeriod), 70)
		.SetGreaterThanZero()
		.SetDisplay("MA Open Period", "Length of MA calculated on open", "Indicators");

		_maOpenShift = Param(nameof(MaOpenShift), 0)
		.SetNotNegative()
		.SetDisplay("MA Open Shift", "Forward shift applied to open MA", "Indicators");

		_maOpenMethod = Param(nameof(MaOpenMethod), MovingAverageMethod.Ema)
		.SetDisplay("MA Open Method", "Smoothing method for open MA", "Indicators");

		_maOpenAppliedPrice = Param(nameof(MaOpenAppliedPrice), AppliedPriceType.Close)
		.SetDisplay("MA Open Price", "Price source for open MA", "Indicators");

		_maOpenSignalBar = Param(nameof(MaOpenSignalBar), 0)
		.SetNotNegative()
		.SetDisplay("MA Open Signal Bar", "Reference bar index for open MA", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Time frame of input candles", "General");

		Volume = _tradeVolume.Value;
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

		_maOpenIndicator = null;
		_maCloseIndicator = null;
		_maOpenValues.Clear();
		_maCloseValues.Clear();
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
		throw new InvalidOperationException("Trailing step must be greater than zero when trailing stop is enabled.");

		_maOpenIndicator = CreateMovingAverage(MaOpenMethod, MaOpenPeriod);
		_maCloseIndicator = CreateMovingAverage(MaCloseMethod, MaClosePeriod);

		UpdatePipSize();
		UpdateQueueSizes();

		Volume = _tradeVolume.Value;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (_maOpenIndicator != null)
			DrawIndicator(area, _maOpenIndicator);
			if (_maCloseIndicator != null)
			DrawIndicator(area, _maCloseIndicator);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var openSource = GetAppliedPrice(candle, MaOpenAppliedPrice);
		var closeSource = GetAppliedPrice(candle, MaCloseAppliedPrice);

		var maOpen = _maOpenIndicator!.Process(openSource, candle.OpenTime, true).ToDecimal();
		var maClose = _maCloseIndicator!.Process(closeSource, candle.OpenTime, true).ToDecimal();

		EnqueueValue(_maOpenValues, maOpen, _maxOpenQueueSize);
		EnqueueValue(_maCloseValues, maClose, _maxCloseQueueSize);

		var maOpenPrev = GetQueueValue(_maOpenValues, 1 + MaOpenShift);
		var maOpenSignal = GetQueueValue(_maOpenValues, MaOpenSignalBar + MaOpenShift);
		var maClosePrev = GetQueueValue(_maCloseValues, 1 + MaCloseShift);
		var maCloseSignal = GetQueueValue(_maOpenValues, MaCloseSignalBar + MaOpenShift);

		if (maOpenPrev is null || maOpenSignal is null || maClosePrev is null || maCloseSignal is null)
		return;

		var longSignal = maOpenPrev > maOpenSignal && maClosePrev > maCloseSignal;
		var shortSignal = maOpenPrev < maOpenSignal && maClosePrev < maCloseSignal;

		if (Position == 0)
		{
			if (longSignal)
			{
				OpenLong(candle.ClosePrice);
			}
			else if (shortSignal)
			{
				OpenShort(candle.ClosePrice);
			}
		}
		else
		{
			ManageOpenPosition(candle, maOpenPrev.Value, maOpenSignal.Value);
		}
	}

	private void OpenLong(decimal price)
	{
		var volume = Volume;
		if (volume <= 0m)
		return;

		_entryPrice = price;
		_stopPrice = StopLossPips > 0m ? price - StopLossPips * _pipSize : null;
		_takePrice = TakeProfitPips > 0m ? price + TakeProfitPips * _pipSize : null;

		BuyMarket(volume);
	}

	private void OpenShort(decimal price)
	{
		var volume = Volume;
		if (volume <= 0m)
		return;

		_entryPrice = price;
		_stopPrice = StopLossPips > 0m ? price + StopLossPips * _pipSize : null;
		_takePrice = TakeProfitPips > 0m ? price - TakeProfitPips * _pipSize : null;

		SellMarket(volume);
	}

	private void ManageOpenPosition(ICandleMessage candle, decimal maOpenPrev, decimal maOpenSignal)
	{
		var position = Position;

		if (position > 0)
		{
			if (maOpenPrev < maOpenSignal)
			{
				SellMarket(position);
				ResetPositionState();
				return;
			}

			UpdateTrailingForLong(candle);

			if (_takePrice.HasValue && candle.HighPrice >= _takePrice.Value)
			{
				SellMarket(position);
				ResetPositionState();
				return;
			}

			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				SellMarket(position);
				ResetPositionState();
			}
		}
		else if (position < 0)
		{
			if (maOpenPrev > maOpenSignal)
			{
				BuyMarket(-position);
				ResetPositionState();
				return;
			}

			UpdateTrailingForShort(candle);

			if (_takePrice.HasValue && candle.LowPrice <= _takePrice.Value)
			{
				BuyMarket(-position);
				ResetPositionState();
				return;
			}

			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				BuyMarket(-position);
				ResetPositionState();
			}
		}
		else
		{
			ResetPositionState();
		}
	}

	private void UpdateTrailingForLong(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m || TrailingStepPips <= 0m || _entryPrice is null)
		return;

		var trailingStop = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;

		if (trailingStop <= 0m)
		return;

		var currentPrice = candle.ClosePrice;
		var entryPrice = _entryPrice.Value;

		if (currentPrice - entryPrice <= trailingStop + trailingStep)
		return;

		var threshold = currentPrice - (trailingStop + trailingStep);

		if (_stopPrice.HasValue && _stopPrice.Value >= threshold)
		return;

		var newStop = currentPrice - trailingStop;
		if (!_stopPrice.HasValue || newStop > _stopPrice.Value)
		_stopPrice = newStop;
	}

	private void UpdateTrailingForShort(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m || TrailingStepPips <= 0m || _entryPrice is null)
		return;

		var trailingStop = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;

		if (trailingStop <= 0m)
		return;

		var currentPrice = candle.ClosePrice;
		var entryPrice = _entryPrice.Value;

		if (entryPrice - currentPrice <= trailingStop + trailingStep)
		return;

		var threshold = currentPrice + trailingStop + trailingStep;

		if (_stopPrice.HasValue && _stopPrice.Value <= threshold)
		return;

		var newStop = currentPrice + trailingStop;
		if (!_stopPrice.HasValue || newStop < _stopPrice.Value)
		_stopPrice = newStop;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
	}

	private void UpdatePipSize()
	{
		var step = Security?.PriceStep ?? 1m;
		var decimals = Security?.Decimals;

		if (decimals == 3 || decimals == 5)
		_pipSize = step * 10m;
		else
		_pipSize = step;
	}

	private void UpdateQueueSizes()
	{
		var shiftOpen = Math.Max(0, MaOpenShift);
		var shiftClose = Math.Max(0, MaCloseShift);
		var openDepth = Math.Max(Math.Max(1 + shiftOpen, MaOpenSignalBar + shiftOpen), MaCloseSignalBar + shiftOpen);
		var closeDepth = Math.Max(1 + shiftClose, 1);

		_maxOpenQueueSize = Math.Max(2, openDepth + 2);
		_maxCloseQueueSize = Math.Max(2, closeDepth + 2);
	}

	private static void EnqueueValue(Queue<decimal> queue, decimal value, int maxSize)
	{
		queue.Enqueue(value);

		while (queue.Count > maxSize)
		queue.Dequeue();
	}

	private static decimal? GetQueueValue(Queue<decimal> queue, int indexFromCurrent)
	{
		if (indexFromCurrent < 0)
		return null;

		if (queue.Count <= indexFromCurrent)
		return null;

		var targetIndex = queue.Count - 1 - indexFromCurrent;
		var enumerator = queue.GetEnumerator();
		var currentIndex = 0;

		while (enumerator.MoveNext())
		{
			if (currentIndex == targetIndex)
			return enumerator.Current;

			currentIndex++;
		}

		return null;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageMethod method, int length)
	{
		return method switch
		{
		MovingAverageMethod.Sma => new SimpleMovingAverage { Length = length },
		MovingAverageMethod.Ema => new ExponentialMovingAverage { Length = length },
		MovingAverageMethod.Smma => new SmoothedMovingAverage { Length = length },
		MovingAverageMethod.Lwma => new WeightedMovingAverage { Length = length },
		_ => new ExponentialMovingAverage { Length = length }
		};
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPriceType priceType)
	{
		return priceType switch
		{
			AppliedPriceType.Close => candle.ClosePrice,
			AppliedPriceType.Open => candle.OpenPrice,
			AppliedPriceType.High => candle.HighPrice,
			AppliedPriceType.Low => candle.LowPrice,
			AppliedPriceType.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceType.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceType.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m,
			_ => candle.ClosePrice
		};
	}
}

/// <summary>
/// Supported moving average smoothing methods.
/// </summary>
public enum MovingAverageMethod
{
	/// <summary>
	/// Simple moving average.
	/// </summary>
	Sma,

	/// <summary>
	/// Exponential moving average.
	/// </summary>
	Ema,

	/// <summary>
	/// Smoothed moving average.
	/// </summary>
	Smma,

	/// <summary>
	/// Linear weighted moving average.
	/// </summary>
	Lwma
}

/// <summary>
/// Price sources that can be fed into the moving averages.
/// </summary>
public enum AppliedPriceType
{
	/// <summary>
	/// Candle close price.
	/// </summary>
	Close,

	/// <summary>
	/// Candle open price.
	/// </summary>
	Open,

	/// <summary>
	/// Candle high price.
	/// </summary>
	High,

	/// <summary>
	/// Candle low price.
	/// </summary>
	Low,

	/// <summary>
	/// Median price of the candle (high + low) / 2.
	/// </summary>
	Median,

	/// <summary>
	/// Typical price (high + low + close) / 3.
	/// </summary>
	Typical,

	/// <summary>
	/// Weighted price (high + low + 2 * close) / 4.
	/// </summary>
	Weighted
}
