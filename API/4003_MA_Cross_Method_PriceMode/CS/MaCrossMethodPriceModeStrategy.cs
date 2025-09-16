using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy converted from the MetaTrader script "MA_cross_Method_PriceMode".
/// Allows selecting the smoothing method, applied price and horizontal shift for each average.
/// </summary>
public class MaCrossMethodPriceModeStrategy : Strategy
{
	private readonly StrategyParam<int> _firstPeriod;
	private readonly StrategyParam<int> _secondPeriod;
	private readonly StrategyParam<MaMethod> _firstMethod;
	private readonly StrategyParam<MaMethod> _secondMethod;
	private readonly StrategyParam<AppliedPriceMode> _firstPriceMode;
	private readonly StrategyParam<AppliedPriceMode> _secondPriceMode;
	private readonly StrategyParam<int> _firstShift;
	private readonly StrategyParam<int> _secondShift;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;

	private LengthIndicator<decimal> _firstMa = null!;
	private LengthIndicator<decimal> _secondMa = null!;

	private readonly List<decimal> _firstValues = new();
	private readonly List<decimal> _secondValues = new();

	/// <summary>
	/// Initializes a new instance of <see cref="MaCrossMethodPriceModeStrategy"/>.
	/// </summary>
	public MaCrossMethodPriceModeStrategy()
	{
		_firstPeriod = Param(nameof(FirstPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Period", "Length of the first moving average.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(2, 50, 1);

		_secondPeriod = Param(nameof(SecondPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Period", "Length of the second moving average.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 100, 1);

		_firstMethod = Param(nameof(FirstMethod), MaMethod.Simple)
			.SetDisplay("Fast MA Method", "Smoothing method applied to the first moving average.", "Indicators")
			.SetCanOptimize(true);

		_secondMethod = Param(nameof(SecondMethod), MaMethod.LinearWeighted)
			.SetDisplay("Slow MA Method", "Smoothing method applied to the second moving average.", "Indicators")
			.SetCanOptimize(true);

		_firstPriceMode = Param(nameof(FirstPriceMode), AppliedPriceMode.Close)
			.SetDisplay("Fast MA Price", "Price source used for the first moving average.", "Indicators")
			.SetCanOptimize(true);

		_secondPriceMode = Param(nameof(SecondPriceMode), AppliedPriceMode.Median)
			.SetDisplay("Slow MA Price", "Price source used for the second moving average.", "Indicators")
			.SetCanOptimize(true);

		_firstShift = Param(nameof(FirstShift), 0)
			.SetGreaterOrEqualZero()
			.SetDisplay("Fast MA Shift", "Horizontal shift (in bars) applied to the first moving average.", "Indicators");

		_secondShift = Param(nameof(SecondShift), 0)
			.SetGreaterOrEqualZero()
			.SetDisplay("Slow MA Shift", "Horizontal shift (in bars) applied to the second moving average.", "Indicators");

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Base order volume used for new entries.", "Trading")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for price processing.", "General");
	}

	/// <summary>
	/// Period of the first moving average.
	/// </summary>
	public int FirstPeriod
	{
		get => _firstPeriod.Value;
		set => _firstPeriod.Value = value;
	}

	/// <summary>
	/// Period of the second moving average.
	/// </summary>
	public int SecondPeriod
	{
		get => _secondPeriod.Value;
		set => _secondPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing method applied to the first moving average.
	/// </summary>
	public MaMethod FirstMethod
	{
		get => _firstMethod.Value;
		set => _firstMethod.Value = value;
	}

	/// <summary>
	/// Smoothing method applied to the second moving average.
	/// </summary>
	public MaMethod SecondMethod
	{
		get => _secondMethod.Value;
		set => _secondMethod.Value = value;
	}

	/// <summary>
	/// Applied price mode for the first moving average.
	/// </summary>
	public AppliedPriceMode FirstPriceMode
	{
		get => _firstPriceMode.Value;
		set => _firstPriceMode.Value = value;
	}

	/// <summary>
	/// Applied price mode for the second moving average.
	/// </summary>
	public AppliedPriceMode SecondPriceMode
	{
		get => _secondPriceMode.Value;
		set => _secondPriceMode.Value = value;
	}

	/// <summary>
	/// Shift (in bars) applied to the first moving average values.
	/// </summary>
	public int FirstShift
	{
		get => _firstShift.Value;
		set => _firstShift.Value = value;
	}

	/// <summary>
	/// Shift (in bars) applied to the second moving average values.
	/// </summary>
	public int SecondShift
	{
		get => _secondShift.Value;
		set => _secondShift.Value = value;
	}

	/// <summary>
	/// Base order volume used for new positions.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Candle type (timeframe) processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_firstMa = null!;
		_secondMa = null!;
		_firstValues.Clear();
		_secondValues.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_firstMa = CreateMovingAverage(FirstMethod, FirstPeriod);
		_secondMa = CreateMovingAverage(SecondMethod, SecondPeriod);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _firstMa);
			DrawIndicator(area, _secondMa);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Work with completed candles only to avoid partial data.
		if (candle.State != CandleStates.Finished)
			return;

		var firstPrice = SelectPrice(candle, FirstPriceMode);
		var secondPrice = SelectPrice(candle, SecondPriceMode);

		var firstValue = _firstMa.Process(firstPrice, candle.OpenTime, true);
		var secondValue = _secondMa.Process(secondPrice, candle.OpenTime, true);

		if (!firstValue.IsFinal || !secondValue.IsFinal)
			return;

		var firstDecimal = firstValue.ToDecimal();
		var secondDecimal = secondValue.ToDecimal();

		UpdateBuffer(_firstValues, firstDecimal, FirstShift);
		UpdateBuffer(_secondValues, secondDecimal, SecondShift);

		if (!TryGetShiftedValues(_firstValues, FirstShift, out var firstCurrent, out var firstPrevious))
			return;

		if (!TryGetShiftedValues(_secondValues, SecondShift, out var secondCurrent, out _))
			return;

		var bullishCross = IsBullishCross(firstPrevious, firstCurrent, secondCurrent);
		var bearishCross = IsBearishCross(firstPrevious, firstCurrent, secondCurrent);

		if (bullishCross && OrderVolume > 0m && Position <= 0m)
		{
			var volumeToBuy = OrderVolume + (Position < 0m ? Math.Abs(Position) : 0m);
			BuyMarket(volumeToBuy);
		}
		else if (bearishCross && OrderVolume > 0m && Position >= 0m)
		{
			var volumeToSell = OrderVolume + (Position > 0m ? Position : 0m);
			SellMarket(volumeToSell);
		}
	}

	private static void UpdateBuffer(List<decimal> buffer, decimal value, int shift)
	{
		buffer.Add(value);

		var maxCount = Math.Max(shift + 2, 2);
		while (buffer.Count > maxCount)
		{
			buffer.RemoveAt(0);
		}
	}

	private static bool TryGetShiftedValues(IReadOnlyList<decimal> buffer, int shift, out decimal current, out decimal previous)
	{
		var currentIndex = buffer.Count - 1 - shift;
		var previousIndex = buffer.Count - 2 - shift;

		if (previousIndex < 0 || currentIndex < 0 || currentIndex >= buffer.Count)
		{
			current = default;
			previous = default;
			return false;
		}

		current = buffer[currentIndex];
		previous = buffer[previousIndex];
		return true;
	}

	private static bool IsBullishCross(decimal previousFast, decimal currentFast, decimal currentSlow)
	{
		return (previousFast <= currentSlow && currentFast > currentSlow)
			|| (previousFast < currentSlow && currentFast >= currentSlow);
	}

	private static bool IsBearishCross(decimal previousFast, decimal currentFast, decimal currentSlow)
	{
		return (previousFast >= currentSlow && currentFast < currentSlow)
			|| (previousFast > currentSlow && currentFast <= currentSlow);
	}

	private static decimal SelectPrice(ICandleMessage candle, AppliedPriceMode mode)
	{
		return mode switch
		{
			AppliedPriceMode.Close => candle.ClosePrice,
			AppliedPriceMode.Open => candle.OpenPrice,
			AppliedPriceMode.High => candle.HighPrice,
			AppliedPriceMode.Low => candle.LowPrice,
			AppliedPriceMode.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceMode.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceMode.Weighted => (candle.HighPrice + candle.LowPrice + (2m * candle.ClosePrice)) / 4m,
			_ => candle.ClosePrice
		};
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MaMethod method, int period)
	{
		return method switch
		{
			MaMethod.Simple => new SimpleMovingAverage { Length = period },
			MaMethod.Exponential => new ExponentialMovingAverage { Length = period },
			MaMethod.Smoothed => new SmoothedMovingAverage { Length = period },
			MaMethod.LinearWeighted => new WeightedMovingAverage { Length = period },
			_ => new SimpleMovingAverage { Length = period }
		};
	}

	/// <summary>
	/// Moving average smoothing methods that mirror the MetaTrader inputs.
	/// </summary>
	public enum MaMethod
	{
		Simple,
		Exponential,
		Smoothed,
		LinearWeighted
	}

	/// <summary>
	/// Applied price options equivalent to the MetaTrader constants.
	/// </summary>
	public enum AppliedPriceMode
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
