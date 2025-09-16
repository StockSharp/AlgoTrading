using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy converted from MetaTrader 5 script.
/// Implements dual moving average signals with configurable methods, price sources, and shifts.
/// </summary>
public class MaCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<MovingAverageMethod> _fastMethod;
	private readonly StrategyParam<MovingAverageMethod> _slowMethod;
	private readonly StrategyParam<AppliedPrice> _fastPriceType;
	private readonly StrategyParam<AppliedPrice> _slowPriceType;
	private readonly StrategyParam<int> _fastShift;
	private readonly StrategyParam<int> _slowShift;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;

	private LengthIndicator<decimal> _fastIndicator = null!;
	private LengthIndicator<decimal> _slowIndicator = null!;
	private readonly List<decimal> _fastHistory = new();
	private readonly List<decimal> _slowHistory = new();

	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = Math.Max(1, value);
	}

	/// <summary>
	/// Slow moving average period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = Math.Max(1, value);
	}

	/// <summary>
	/// Calculation method for the fast moving average.
	/// </summary>
	public MovingAverageMethod FastMethod
	{
		get => _fastMethod.Value;
		set => _fastMethod.Value = value;
	}

	/// <summary>
	/// Calculation method for the slow moving average.
	/// </summary>
	public MovingAverageMethod SlowMethod
	{
		get => _slowMethod.Value;
		set => _slowMethod.Value = value;
	}

	/// <summary>
	/// Applied price for the fast moving average.
	/// </summary>
	public AppliedPrice FastPriceType
	{
		get => _fastPriceType.Value;
		set => _fastPriceType.Value = value;
	}

	/// <summary>
	/// Applied price for the slow moving average.
	/// </summary>
	public AppliedPrice SlowPriceType
	{
		get => _slowPriceType.Value;
		set => _slowPriceType.Value = value;
	}

	/// <summary>
	/// Shift (in bars) for the fast moving average output.
	/// </summary>
	public int FastShift
	{
		get => _fastShift.Value;
		set => _fastShift.Value = Math.Max(0, value);
	}

	/// <summary>
	/// Shift (in bars) for the slow moving average output.
	/// </summary>
	public int SlowShift
	{
		get => _slowShift.Value;
		set => _slowShift.Value = Math.Max(0, value);
	}

	/// <summary>
	/// Order volume used for entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = Math.Max(0.0001m, value);
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public MaCrossStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Period for the fast moving average", "Moving Averages")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Period for the slow moving average", "Moving Averages")
			.SetCanOptimize(true)
			.SetOptimize(5, 60, 1);

		_fastMethod = Param(nameof(FastMethod), MovingAverageMethod.Simple)
			.SetDisplay("Fast MA Method", "Calculation method for the fast moving average", "Moving Averages");

		_slowMethod = Param(nameof(SlowMethod), MovingAverageMethod.LinearWeighted)
			.SetDisplay("Slow MA Method", "Calculation method for the slow moving average", "Moving Averages");

		_fastPriceType = Param(nameof(FastPriceType), AppliedPrice.Close)
			.SetDisplay("Fast MA Price", "Applied price for the fast moving average", "Moving Averages");

		_slowPriceType = Param(nameof(SlowPriceType), AppliedPrice.Median)
			.SetDisplay("Slow MA Price", "Applied price for the slow moving average", "Moving Averages");

		_fastShift = Param(nameof(FastShift), 0)
			.SetDisplay("Fast MA Shift", "Shift of the fast moving average in bars", "Moving Averages");

		_slowShift = Param(nameof(SlowShift), 0)
			.SetDisplay("Slow MA Shift", "Shift of the slow moving average in bars", "Moving Averages");

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume for each market order", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculations", "Data");
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
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastIndicator = CreateMovingAverage(FastMethod, FastPeriod);
		_slowIndicator = CreateMovingAverage(SlowMethod, SlowPeriod);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastIndicator);
			DrawIndicator(area, _slowIndicator);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Work only with completed candles to avoid premature signals.
		if (candle.State != CandleStates.Finished)
			return;

		// Calculate applied prices according to user configuration.
		var fastPrice = GetAppliedPrice(candle, FastPriceType);
		var slowPrice = GetAppliedPrice(candle, SlowPriceType);

		// Update indicators with the selected prices.
		var fastValue = _fastIndicator.Process(fastPrice, candle.OpenTime, true).ToDecimal();
		var slowValue = _slowIndicator.Process(slowPrice, candle.OpenTime, true).ToDecimal();

		// Ensure both moving averages are fully formed before trading.
		if (!_fastIndicator.IsFormed || !_slowIndicator.IsFormed)
			return;

		UpdateHistory(_fastHistory, fastValue, FastShift);
		UpdateHistory(_slowHistory, slowValue, SlowShift);

		if (!HasEnoughValues(_fastHistory, FastShift) || !HasEnoughValues(_slowHistory, SlowShift))
			return;

		var fastCurrent = GetShiftedValue(_fastHistory, FastShift, 0);
		var fastPrevious = GetShiftedValue(_fastHistory, FastShift, 1);
		var slowCurrent = GetShiftedValue(_slowHistory, SlowShift, 0);

		var crossUp = IsCrossUp(fastPrevious, fastCurrent, slowCurrent);
		var crossDown = IsCrossDown(fastPrevious, fastCurrent, slowCurrent);

		if (!crossUp && !crossDown)
			return;

		// Trade only when strategy is ready and trading is allowed.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (crossUp)
		{
			// Close short exposure and open or add to a long position.
			if (Position < 0)
			{
				BuyMarket(OrderVolume + Math.Abs(Position));
			}
			else if (Position == 0)
			{
				BuyMarket(OrderVolume);
			}
		}
		else if (crossDown)
		{
			// Close long exposure and open or add to a short position.
			if (Position > 0)
			{
				SellMarket(OrderVolume + Math.Abs(Position));
			}
			else if (Position == 0)
			{
				SellMarket(OrderVolume);
			}
		}
	}

	private static bool HasEnoughValues(List<decimal> history, int shift)
	{
		return history.Count >= Math.Max(shift + 2, 2);
	}

	private static void UpdateHistory(List<decimal> history, decimal value, int shift)
	{
		history.Add(value);

		var maxCount = Math.Max(shift + 2, 2);
		while (history.Count > maxCount)
		{
			history.RemoveAt(0);
		}
	}

	private static decimal GetShiftedValue(List<decimal> history, int shift, int offset)
	{
		var index = history.Count - 1 - shift - offset;
		return history[index];
	}

	private static bool IsCrossUp(decimal fastPrevious, decimal fastCurrent, decimal slowCurrent)
	{
		return (fastPrevious <= slowCurrent && fastCurrent > slowCurrent)
			|| (fastPrevious < slowCurrent && fastCurrent >= slowCurrent);
	}

	private static bool IsCrossDown(decimal fastPrevious, decimal fastCurrent, decimal slowCurrent)
	{
		return (fastPrevious >= slowCurrent && fastCurrent < slowCurrent)
			|| (fastPrevious > slowCurrent && fastCurrent <= slowCurrent);
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageMethod method, int length)
	{
		return method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageMethod.LinearWeighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
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
			_ => candle.ClosePrice,
		};
	}

	/// <summary>
	/// Moving average calculation methods supported by the strategy.
	/// </summary>
	public enum MovingAverageMethod
	{
		/// <summary>Simple moving average.</summary>
		Simple,
		/// <summary>Exponential moving average.</summary>
		Exponential,
		/// <summary>Smoothed moving average.</summary>
		Smoothed,
		/// <summary>Linear weighted moving average.</summary>
		LinearWeighted,
	}

	/// <summary>
	/// Applied price modes compatible with MetaTrader inputs.
	/// </summary>
	public enum AppliedPrice
	{
		/// <summary>Use closing price of the candle.</summary>
		Close,
		/// <summary>Use opening price of the candle.</summary>
		Open,
		/// <summary>Use highest price of the candle.</summary>
		High,
		/// <summary>Use lowest price of the candle.</summary>
		Low,
		/// <summary>Use median price (high + low) / 2.</summary>
		Median,
		/// <summary>Use typical price (high + low + close) / 3.</summary>
		Typical,
		/// <summary>Use weighted price (high + low + 2 * close) / 4.</summary>
		Weighted,
	}
}
