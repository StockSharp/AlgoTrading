using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy that uses 5- and 8-period averages with optional risk management.
/// </summary>
public class FiveEightMaCrossStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _fastShift;
	private readonly StrategyParam<MovingAverageMethod> _fastMethod;
	private readonly StrategyParam<AppliedPrice> _fastPrice;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _slowShift;
	private readonly StrategyParam<MovingAverageMethod> _slowMethod;
	private readonly StrategyParam<AppliedPrice> _slowPrice;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _fastValues = new();
	private readonly Queue<decimal> _slowValues = new();

	private LengthIndicator<decimal>? _fastMa;
	private LengthIndicator<decimal>? _slowMa;

	private decimal _pipSize;
	private decimal _takeProfitDistance;
	private decimal _stopLossDistance;
	private decimal _trailingDistance;

	private decimal? _longEntryPrice;
	private decimal? _longTakeProfit;
	private decimal? _longStopLoss;
	private decimal? _longTrailingStop;

	private decimal? _shortEntryPrice;
	private decimal? _shortTakeProfit;
	private decimal? _shortStopLoss;
	private decimal? _shortTrailingStop;

	/// <summary>
	/// Initializes a new instance of <see cref="FiveEightMaCrossStrategy"/>.
	/// </summary>
	public FiveEightMaCrossStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetDisplay("Trade Volume", "Order volume for new positions", "Trading")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 40m)
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 0m)
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk")
			.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 0m)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk")
			.SetCanOptimize(true);

		_fastPeriod = Param(nameof(FastPeriod), 5)
			.SetDisplay("Fast Period", "Period of the fast moving average", "Indicators")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_fastShift = Param(nameof(FastShift), -1)
			.SetDisplay("Fast Shift", "Bars to offset the fast moving average", "Indicators")
			.SetCanOptimize(true);

		_fastMethod = Param(nameof(FastMethod), MovingAverageMethod.Exponential)
			.SetDisplay("Fast Method", "Smoothing method for the fast moving average", "Indicators");

		_fastPrice = Param(nameof(FastPrice), AppliedPrice.Close)
			.SetDisplay("Fast Price", "Applied price for the fast moving average", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 8)
			.SetDisplay("Slow Period", "Period of the slow moving average", "Indicators")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_slowShift = Param(nameof(SlowShift), 0)
			.SetDisplay("Slow Shift", "Bars to offset the slow moving average", "Indicators")
			.SetCanOptimize(true);

		_slowMethod = Param(nameof(SlowMethod), MovingAverageMethod.Exponential)
			.SetDisplay("Slow Method", "Smoothing method for the slow moving average", "Indicators");

		_slowPrice = Param(nameof(SlowPrice), AppliedPrice.Open)
			.SetDisplay("Slow Price", "Applied price for the slow moving average", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for analysis", "General");

		Volume = _tradeVolume.Value;
	}

	/// <summary>
	/// Trade volume used for new entries.
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
	/// Take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
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
	/// Period of the fast moving average.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Bars to offset the fast moving average.
	/// </summary>
	public int FastShift
	{
		get => _fastShift.Value;
		set => _fastShift.Value = value;
	}

	/// <summary>
	/// Smoothing method for the fast moving average.
	/// </summary>
	public MovingAverageMethod FastMethod
	{
		get => _fastMethod.Value;
		set => _fastMethod.Value = value;
	}

	/// <summary>
	/// Applied price for the fast moving average.
	/// </summary>
	public AppliedPrice FastPrice
	{
		get => _fastPrice.Value;
		set => _fastPrice.Value = value;
	}

	/// <summary>
	/// Period of the slow moving average.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Bars to offset the slow moving average.
	/// </summary>
	public int SlowShift
	{
		get => _slowShift.Value;
		set => _slowShift.Value = value;
	}

	/// <summary>
	/// Smoothing method for the slow moving average.
	/// </summary>
	public MovingAverageMethod SlowMethod
	{
		get => _slowMethod.Value;
		set => _slowMethod.Value = value;
	}

	/// <summary>
	/// Applied price for the slow moving average.
	/// </summary>
	public AppliedPrice SlowPrice
	{
		get => _slowPrice.Value;
		set => _slowPrice.Value = value;
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

		_fastValues.Clear();
		_slowValues.Clear();

		_fastMa = null;
		_slowMa = null;

		_pipSize = 0m;
		_takeProfitDistance = 0m;
		_stopLossDistance = 0m;
		_trailingDistance = 0m;

		ResetLongState();
		ResetShortState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_fastMa = CreateMovingAverage(FastMethod, FastPeriod, FastPrice);
		_slowMa = CreateMovingAverage(SlowMethod, SlowPeriod, SlowPrice);

		_pipSize = CalculatePipSize();
		_takeProfitDistance = ConvertPipsToPrice(TakeProfitPips);
		_stopLossDistance = ConvertPipsToPrice(StopLossPips);
		_trailingDistance = ConvertPipsToPrice(TrailingStopPips);

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_fastMa, _slowMa, ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
		{
		return;
		}

		if (_fastMa is null || _slowMa is null)
		{
		return;
		}

		if (!_fastMa.IsFormed || !_slowMa.IsFormed)
		{
		return;
		}

		if (!TryGetShiftedPair(_fastValues, fastValue, FastShift, out var fastPrevious, out var fastCurrent))
		{
		return;
		}

		if (!TryGetShiftedPair(_slowValues, slowValue, SlowShift, out var slowPrevious, out var slowCurrent))
		{
		return;
		}

		if (TryHandleRiskManagement(candle))
		{
		return;
		}

		var crossedUp = fastPrevious <= slowPrevious && fastCurrent > slowCurrent;
		var crossedDown = fastPrevious >= slowPrevious && fastCurrent < slowCurrent;

		if (crossedUp && Position <= 0m)
		{
		var volume = CalculateEntryVolume(true);
		if (volume > 0m)
		{
		BuyMarket(volume);
		InitializeLongState(candle.ClosePrice);
		}
		}
		else if (crossedDown && Position >= 0m)
		{
		var volume = CalculateEntryVolume(false);
		if (volume > 0m)
		{
		SellMarket(volume);
		InitializeShortState(candle.ClosePrice);
		}
		}
	}

	private bool TryHandleRiskManagement(ICandleMessage candle)
	{
		if (Position > 0m)
		{
		return CheckLongExit(candle);
		}

		if (Position < 0m)
		{
		return CheckShortExit(candle);
		}

		return false;
	}

	private bool CheckLongExit(ICandleMessage candle)
	{
		var exitVolume = Math.Abs(Position);
		if (exitVolume <= 0m)
		{
		return false;
		}

		if (_longTakeProfit is decimal takeProfit && candle.HighPrice >= takeProfit)
		{
		SellMarket(exitVolume);
		ResetLongState();
		return true;
		}

		if (_longStopLoss is decimal stopLoss && candle.LowPrice <= stopLoss)
		{
		SellMarket(exitVolume);
		ResetLongState();
		return true;
		}

		UpdateLongTrailing(candle);

		if (_longTrailingStop is decimal trailing && candle.LowPrice <= trailing)
		{
		SellMarket(exitVolume);
		ResetLongState();
		return true;
		}

		return false;
	}

	private bool CheckShortExit(ICandleMessage candle)
	{
		var exitVolume = Math.Abs(Position);
		if (exitVolume <= 0m)
		{
		return false;
		}

		if (_shortTakeProfit is decimal takeProfit && candle.LowPrice <= takeProfit)
		{
		BuyMarket(exitVolume);
		ResetShortState();
		return true;
		}

		if (_shortStopLoss is decimal stopLoss && candle.HighPrice >= stopLoss)
		{
		BuyMarket(exitVolume);
		ResetShortState();
		return true;
		}

		UpdateShortTrailing(candle);

		if (_shortTrailingStop is decimal trailing && candle.HighPrice >= trailing)
		{
		BuyMarket(exitVolume);
		ResetShortState();
		return true;
		}

		return false;
	}

	private decimal CalculateEntryVolume(bool openLong)
	{
		var baseVolume = Volume;
		if (baseVolume <= 0m)
		{
		return 0m;
		}

		if (openLong && Position < 0m)
		{
		return baseVolume + Math.Abs(Position);
		}

		if (!openLong && Position > 0m)
		{
		return baseVolume + Math.Abs(Position);
		}

		return baseVolume;
	}

	private void InitializeLongState(decimal entryPrice)
	{
		_longEntryPrice = entryPrice;
		_longTakeProfit = _takeProfitDistance > 0m ? entryPrice + _takeProfitDistance : null;
		_longStopLoss = _stopLossDistance > 0m ? entryPrice - _stopLossDistance : null;
		_longTrailingStop = _trailingDistance > 0m ? entryPrice - _trailingDistance : null;

		ResetShortState();
	}

	private void InitializeShortState(decimal entryPrice)
	{
		_shortEntryPrice = entryPrice;
		_shortTakeProfit = _takeProfitDistance > 0m ? entryPrice - _takeProfitDistance : null;
		_shortStopLoss = _stopLossDistance > 0m ? entryPrice + _stopLossDistance : null;
		_shortTrailingStop = _trailingDistance > 0m ? entryPrice + _trailingDistance : null;

		ResetLongState();
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longTakeProfit = null;
		_longStopLoss = null;
		_longTrailingStop = null;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortTakeProfit = null;
		_shortStopLoss = null;
		_shortTrailingStop = null;
	}

	private void UpdateLongTrailing(ICandleMessage candle)
	{
		if (_trailingDistance <= 0m || _longEntryPrice is null)
		{
		return;
		}

		var candidate = candle.ClosePrice - _trailingDistance;

		if (_longTrailingStop is null || candidate > _longTrailingStop)
		{
		_longTrailingStop = candidate;
		}
	}

	private void UpdateShortTrailing(ICandleMessage candle)
	{
		if (_trailingDistance <= 0m || _shortEntryPrice is null)
		{
		return;
		}

		var candidate = candle.ClosePrice + _trailingDistance;

		if (_shortTrailingStop is null || candidate < _shortTrailingStop)
		{
		_shortTrailingStop = candidate;
		}
	}

	private bool TryGetShiftedPair(Queue<decimal> buffer, decimal value, int shift, out decimal previous, out decimal current)
	{
		buffer.Enqueue(value);

		var offset = shift < 0 ? 0 : shift;
		var required = offset + 2;

		while (buffer.Count > required)
		{
		buffer.Dequeue();
		}

		if (buffer.Count < required)
		{
		previous = 0m;
		current = 0m;
		return false;
		}

		var items = buffer.ToArray();
		var currentIndex = items.Length - 1 - offset;
		var previousIndex = items.Length - 2 - offset;

		if (currentIndex < 0 || previousIndex < 0)
		{
		previous = 0m;
		current = 0m;
		return false;
		}

		current = items[currentIndex];
		previous = items[previousIndex];
		return true;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step : 0m;
	}

	private decimal ConvertPipsToPrice(decimal pips)
	{
		if (pips <= 0m)
		{
		return 0m;
		}

		if (_pipSize <= 0m)
		{
		return 0m;
		}

		return pips * _pipSize;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageMethod method, int period, AppliedPrice price)
	{
		var indicator = method switch
		{
		MovingAverageMethod.Simple => new SimpleMovingAverage(),
		MovingAverageMethod.Exponential => new ExponentialMovingAverage(),
		MovingAverageMethod.Smoothed => new SmoothedMovingAverage(),
		MovingAverageMethod.LinearWeighted => new WeightedMovingAverage(),
		_ => new SimpleMovingAverage(),
		};

		indicator.Length = Math.Max(1, period);
		indicator.CandlePrice = ConvertAppliedPrice(price);

		return indicator;
	}

	private static CandlePrice ConvertAppliedPrice(AppliedPrice price)
	{
		return price switch
		{
		AppliedPrice.Open => CandlePrice.Open,
		AppliedPrice.High => CandlePrice.High,
		AppliedPrice.Low => CandlePrice.Low,
		AppliedPrice.Median => CandlePrice.Median,
		AppliedPrice.Typical => CandlePrice.Typical,
		AppliedPrice.Weighted => CandlePrice.Weighted,
		_ => CandlePrice.Close,
		};
	}
}

/// <summary>
/// Supported moving average calculation methods.
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
	/// Smoothed moving average.
	/// </summary>
	Smoothed,

	/// <summary>
	/// Linear weighted moving average.
	/// </summary>
	LinearWeighted,
}

/// <summary>
/// Applied price options available for moving averages.
/// </summary>
public enum AppliedPrice
{
	/// <summary>
	/// Closing price of the candle.
	/// </summary>
	Close,

	/// <summary>
	/// Opening price of the candle.
	/// </summary>
	Open,

	/// <summary>
	/// Highest price of the candle.
	/// </summary>
	High,

	/// <summary>
	/// Lowest price of the candle.
	/// </summary>
	Low,

	/// <summary>
	/// Average of high and low prices.
	/// </summary>
	Median,

	/// <summary>
	/// Average of high, low and close prices.
	/// </summary>
	Typical,

	/// <summary>
	/// Weighted average of high, low and close prices.
	/// </summary>
	Weighted,
}
