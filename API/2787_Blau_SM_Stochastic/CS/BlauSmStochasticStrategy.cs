using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Blau SM Stochastic based strategy converted from the MQL5 Expert Advisor.
/// </summary>
public class BlauSmStochasticStrategy : Strategy
{
	private readonly StrategyParam<BlauSmStochasticMode> _mode;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<int> _lookbackLength;
	private readonly StrategyParam<int> _firstSmoothingLength;
	private readonly StrategyParam<int> _secondSmoothingLength;
	private readonly StrategyParam<int> _thirdSmoothingLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<BlauSmSmoothMethod> _smoothMethod;
	private readonly StrategyParam<int> _phase;
	private readonly StrategyParam<BlauSmAppliedPrice> _priceType;
	private readonly StrategyParam<bool> _enableLongEntry;
	private readonly StrategyParam<bool> _enableShortEntry;
	private readonly StrategyParam<bool> _enableLongExit;
	private readonly StrategyParam<bool> _enableShortExit;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _mainHistory = new();
	private readonly List<decimal> _signalHistory = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="BlauSmStochasticStrategy"/> class.
	/// </summary>
	public BlauSmStochasticStrategy()
	{
		_mode = Param(nameof(Mode), BlauSmStochasticMode.Twist)
			.SetDisplay("Mode", "Signal generation mode", "Parameters");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetGreaterOrEqualZero()
			.SetDisplay("Signal Bar Shift", "Number of bars to shift indicator values", "Parameters");

		_lookbackLength = Param(nameof(LookbackLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Length", "Bars used to compute highest and lowest prices", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 25, 5);

		_firstSmoothingLength = Param(nameof(FirstSmoothingLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("First Smoothing", "Length of the first smoothing stage", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_secondSmoothingLength = Param(nameof(SecondSmoothingLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Second Smoothing", "Length of the second smoothing stage", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(3, 15, 2);

		_thirdSmoothingLength = Param(nameof(ThirdSmoothingLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Third Smoothing", "Length of the third smoothing stage", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(2, 10, 1);

		_signalLength = Param(nameof(SignalLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Signal Smoothing", "Length of the signal line smoothing", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(2, 12, 1);

		_smoothMethod = Param(nameof(SmoothMethod), BlauSmSmoothMethod.Ema)
			.SetDisplay("Smoothing Method", "Moving average type used for all smoothing stages", "Indicator");

		_phase = Param(nameof(Phase), 15)
			.SetDisplay("Phase", "Compatibility parameter kept from the original indicator", "Indicator");

		_priceType = Param(nameof(PriceType), BlauSmAppliedPrice.Close)
			.SetDisplay("Applied Price", "Price input used in oscillator calculations", "Indicator");

		_enableLongEntry = Param(nameof(EnableLongEntry), true)
			.SetDisplay("Enable Long Entry", "Allow opening long positions", "Trading");

		_enableShortEntry = Param(nameof(EnableShortEntry), true)
			.SetDisplay("Enable Short Entry", "Allow opening short positions", "Trading");

		_enableLongExit = Param(nameof(EnableLongExit), true)
			.SetDisplay("Enable Long Exit", "Allow closing existing long positions", "Trading");

		_enableShortExit = Param(nameof(EnableShortExit), true)
			.SetDisplay("Enable Short Exit", "Allow closing existing short positions", "Trading");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (points)", "Take-profit distance expressed in instrument points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 4000, 500);

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (points)", "Stop-loss distance expressed in instrument points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 4000, 500);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for indicator calculations", "General");
	}

	/// <summary>
	/// Strategy operating mode.
	/// </summary>
	public BlauSmStochasticMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Number of bars to shift indicator values before evaluation.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Length used to search for highs and lows.
	/// </summary>
	public int LookbackLength
	{
		get => _lookbackLength.Value;
		set => _lookbackLength.Value = value;
	}

	/// <summary>
	/// First smoothing stage length.
	/// </summary>
	public int FirstSmoothingLength
	{
		get => _firstSmoothingLength.Value;
		set => _firstSmoothingLength.Value = value;
	}

	/// <summary>
	/// Second smoothing stage length.
	/// </summary>
	public int SecondSmoothingLength
	{
		get => _secondSmoothingLength.Value;
		set => _secondSmoothingLength.Value = value;
	}

	/// <summary>
	/// Third smoothing stage length.
	/// </summary>
	public int ThirdSmoothingLength
	{
		get => _thirdSmoothingLength.Value;
		set => _thirdSmoothingLength.Value = value;
	}

	/// <summary>
	/// Signal line smoothing length.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Moving average method.
	/// </summary>
	public BlauSmSmoothMethod SmoothMethod
	{
		get => _smoothMethod.Value;
		set => _smoothMethod.Value = value;
	}

	/// <summary>
	/// Phase parameter kept for compatibility.
	/// </summary>
	public int Phase
	{
		get => _phase.Value;
		set => _phase.Value = value;
	}

	/// <summary>
	/// Applied price type.
	/// </summary>
	public BlauSmAppliedPrice PriceType
	{
		get => _priceType.Value;
		set => _priceType.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool EnableLongEntry
	{
		get => _enableLongEntry.Value;
		set => _enableLongEntry.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool EnableShortEntry
	{
		get => _enableShortEntry.Value;
		set => _enableShortEntry.Value = value;
	}

	/// <summary>
	/// Allow closing existing long positions.
	/// </summary>
	public bool EnableLongExit
	{
		get => _enableLongExit.Value;
		set => _enableLongExit.Value = value;
	}

	/// <summary>
	/// Allow closing existing short positions.
	/// </summary>
	public bool EnableShortExit
	{
		get => _enableShortExit.Value;
		set => _enableShortExit.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator subscription.
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

		// Clear stored indicator values.
		_mainHistory.Clear();
		_signalHistory.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var indicator = new BlauSmStochasticIndicator
		{
			LookbackLength = LookbackLength,
			FirstSmoothingLength = FirstSmoothingLength,
			SecondSmoothingLength = SecondSmoothingLength,
			ThirdSmoothingLength = ThirdSmoothingLength,
			SignalLength = SignalLength,
			SmoothMethod = SmoothMethod,
			Phase = Phase,
			PriceType = PriceType,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(indicator, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, indicator);
			DrawOwnTrades(area);
		}

		var step = Security.PriceStep ?? 1m;
		Unit? takeProfit = null;
		Unit? stopLoss = null;

		if (TakeProfitPoints > 0)
			takeProfit = new Unit(TakeProfitPoints * step, UnitTypes.Point);

		if (StopLossPoints > 0)
			stopLoss = new Unit(StopLossPoints * step, UnitTypes.Point);

		if (takeProfit != null || stopLoss != null)
			StartProtection(takeProfit, stopLoss);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (indicatorValue is not BlauSmStochasticValue value)
			return;

		// Store the latest indicator values for shifted access.
		UpdateHistory(value.Main, value.Signal);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var hasCurrent = TryGetMain(SignalBar, out var currentMain);
		var hasPrevious = TryGetMain(SignalBar + 1, out var previousMain);

		var hasCurrentSignal = TryGetSignal(SignalBar, out var currentSignal);
		var hasPreviousSignal = TryGetSignal(SignalBar + 1, out var previousSignal);

		if (!hasCurrent || !hasPrevious)
			return;

		var buyEntry = false;
		var sellEntry = false;
		var buyExit = false;
		var sellExit = false;

		switch (Mode)
		{
			case BlauSmStochasticMode.Breakdown:
			{
				// Detect histogram sign change through zero.
				if (previousMain > 0m && currentMain <= 0m)
				{
					if (EnableLongEntry)
						buyEntry = true;
					if (EnableShortExit)
						sellExit = true;
				}

				if (previousMain < 0m && currentMain >= 0m)
				{
					if (EnableShortEntry)
						sellEntry = true;
					if (EnableLongExit)
						buyExit = true;
				}
				break;
			}
			case BlauSmStochasticMode.Twist:
			{
				if (!TryGetMain(SignalBar + 2, out var olderMain))
					return;

				// Identify twists in momentum slope.
				if (previousMain < olderMain && currentMain > previousMain)
				{
					if (EnableLongEntry)
						buyEntry = true;
					if (EnableShortExit)
						sellExit = true;
				}

				if (previousMain > olderMain && currentMain < previousMain)
				{
					if (EnableShortEntry)
						sellEntry = true;
					if (EnableLongExit)
						buyExit = true;
				}
				break;
			}
			case BlauSmStochasticMode.CloudTwist:
			{
				if (!hasCurrentSignal || !hasPreviousSignal)
					return;

				// Watch for crossings between main and smoothed signal lines.
				if (previousMain > previousSignal && currentMain <= currentSignal)
				{
					if (EnableLongEntry)
						buyEntry = true;
					if (EnableShortExit)
						sellExit = true;
				}

				if (previousMain < previousSignal && currentMain >= currentSignal)
				{
					if (EnableShortEntry)
						sellEntry = true;
					if (EnableLongExit)
						buyExit = true;
				}
				break;
			}
		}

		// Close positions before opening opposite trades.
		if (buyExit && Position > 0)
			SellMarket(Position);

		if (sellExit && Position < 0)
			BuyMarket(-Position);

		if (buyEntry && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));

		if (sellEntry && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}

	private void UpdateHistory(decimal main, decimal signal)
	{
		_mainHistory.Add(main);
		_signalHistory.Add(signal);

		var max = SignalBar + 5;
		while (_mainHistory.Count > max)
			_mainHistory.RemoveAt(0);

		while (_signalHistory.Count > max)
			_signalHistory.RemoveAt(0);
	}

	private bool TryGetMain(int shift, out decimal value)
	{
		var index = _mainHistory.Count - 1 - shift;
		if (index < 0)
		{
			value = 0m;
			return false;
		}

		value = _mainHistory[index];
		return true;
	}

	private bool TryGetSignal(int shift, out decimal value)
	{
		var index = _signalHistory.Count - 1 - shift;
		if (index < 0)
		{
			value = 0m;
			return false;
		}

		value = _signalHistory[index];
		return true;
	}
}

/// <summary>
/// Custom indicator implementing the Blau SM Stochastic oscillator.
/// </summary>
public class BlauSmStochasticIndicator : BaseIndicator<decimal>
{
	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();

	private IIndicator _smooth1;
	private IIndicator _smooth2;
	private IIndicator _smooth3;
	private IIndicator _halfSmooth1;
	private IIndicator _halfSmooth2;
	private IIndicator _halfSmooth3;
	private IIndicator _signalSmooth;

	/// <summary>
	/// Bars used to search for highest and lowest values.
	/// </summary>
	public int LookbackLength { get; set; } = 5;

	/// <summary>
	/// First smoothing length.
	/// </summary>
	public int FirstSmoothingLength { get; set; } = 20;

	/// <summary>
	/// Second smoothing length.
	/// </summary>
	public int SecondSmoothingLength { get; set; } = 5;

	/// <summary>
	/// Third smoothing length.
	/// </summary>
	public int ThirdSmoothingLength { get; set; } = 3;

	/// <summary>
	/// Signal line smoothing length.
	/// </summary>
	public int SignalLength { get; set; } = 3;

	/// <summary>
	/// Moving average type used throughout the calculations.
	/// </summary>
	public BlauSmSmoothMethod SmoothMethod { get; set; } = BlauSmSmoothMethod.Ema;

	/// <summary>
	/// Phase parameter kept for compatibility with the original code.
	/// </summary>
	public int Phase { get; set; } = 15;

	/// <summary>
	/// Applied price type.
	/// </summary>
	public BlauSmAppliedPrice PriceType { get; set; } = BlauSmAppliedPrice.Close;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		IsFormed = false;

		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
			return new DecimalIndicatorValue(this, default, input.Time);

		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		while (_highs.Count > LookbackLength)
			_highs.RemoveAt(0);

		while (_lows.Count > LookbackLength)
			_lows.RemoveAt(0);

		EnsureIndicators();

		if (_highs.Count < LookbackLength || _lows.Count < LookbackLength)
			return new DecimalIndicatorValue(this, default, input.Time);

		var highest = GetMaximum(_highs);
		var lowest = GetMinimum(_lows);
		var price = GetAppliedPrice(candle, PriceType);

		var sm = price - 0.5m * (lowest + highest);
		var half = 0.5m * (highest - lowest);

		var sm1 = ProcessStage(_smooth1, sm, input.Time);
		if (sm1 is null)
			return new DecimalIndicatorValue(this, default, input.Time);

		var sm2 = ProcessStage(_smooth2, sm1.Value, input.Time);
		if (sm2 is null)
			return new DecimalIndicatorValue(this, default, input.Time);

		var sm3 = ProcessStage(_smooth3, sm2.Value, input.Time);
		if (sm3 is null)
			return new DecimalIndicatorValue(this, default, input.Time);

		var half1 = ProcessStage(_halfSmooth1, half, input.Time);
		if (half1 is null)
			return new DecimalIndicatorValue(this, default, input.Time);

		var half2 = ProcessStage(_halfSmooth2, half1.Value, input.Time);
		if (half2 is null)
			return new DecimalIndicatorValue(this, default, input.Time);

		var half3 = ProcessStage(_halfSmooth3, half2.Value, input.Time);
		if (half3 is null || half3.Value == 0m)
			return new DecimalIndicatorValue(this, default, input.Time);

		var main = 100m * sm3.Value / half3.Value;

		var signal = ProcessStage(_signalSmooth, main, input.Time);
		if (signal is null)
			return new DecimalIndicatorValue(this, default, input.Time);

		IsFormed = true;

		var histogram = main - signal.Value;
		return new BlauSmStochasticValue(this, input, main, signal.Value, histogram);
	}

	private void EnsureIndicators()
	{
		if (_smooth1 != null)
			return;

		_smooth1 = CreateAverage(FirstSmoothingLength);
		_smooth2 = CreateAverage(SecondSmoothingLength);
		_smooth3 = CreateAverage(ThirdSmoothingLength);
		_halfSmooth1 = CreateAverage(FirstSmoothingLength);
		_halfSmooth2 = CreateAverage(SecondSmoothingLength);
		_halfSmooth3 = CreateAverage(ThirdSmoothingLength);
		_signalSmooth = CreateAverage(SignalLength);
	}

	private static decimal GetMaximum(List<decimal> values)
	{
		var max = decimal.MinValue;
		foreach (var value in values)
		{
			if (value > max)
				max = value;
		}
		return max;
	}

	private static decimal GetMinimum(List<decimal> values)
	{
		var min = decimal.MaxValue;
		foreach (var value in values)
		{
			if (value < min)
				min = value;
		}
		return min;
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, BlauSmAppliedPrice priceType)
	{
		return priceType switch
		{
			BlauSmAppliedPrice.Close => candle.ClosePrice,
			BlauSmAppliedPrice.Open => candle.OpenPrice,
			BlauSmAppliedPrice.High => candle.HighPrice,
			BlauSmAppliedPrice.Low => candle.LowPrice,
			BlauSmAppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			BlauSmAppliedPrice.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
			BlauSmAppliedPrice.Weighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			BlauSmAppliedPrice.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
			BlauSmAppliedPrice.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			BlauSmAppliedPrice.TrendFollow0 => candle.ClosePrice > candle.OpenPrice ? candle.HighPrice : candle.ClosePrice < candle.OpenPrice ? candle.LowPrice : candle.ClosePrice,
			BlauSmAppliedPrice.TrendFollow1 => candle.ClosePrice > candle.OpenPrice ? (candle.HighPrice + candle.ClosePrice) / 2m : candle.ClosePrice < candle.OpenPrice ? (candle.LowPrice + candle.ClosePrice) / 2m : candle.ClosePrice,
			BlauSmAppliedPrice.Demark => CalculateDemarkPrice(candle),
			_ => candle.ClosePrice,
		};
	}

	private static decimal CalculateDemarkPrice(ICandleMessage candle)
	{
		var res = candle.HighPrice + candle.LowPrice + candle.ClosePrice;

		if (candle.ClosePrice < candle.OpenPrice)
			res = (res + candle.LowPrice) / 2m;
		else if (candle.ClosePrice > candle.OpenPrice)
			res = (res + candle.HighPrice) / 2m;
		else
			res = (res + candle.ClosePrice) / 2m;

		return ((res - candle.LowPrice) + (res - candle.HighPrice)) / 2m;
	}

	private IIndicator CreateAverage(int length)
	{
		return SmoothMethod switch
		{
			BlauSmSmoothMethod.Sma => new SimpleMovingAverage { Length = length },
			BlauSmSmoothMethod.Ema => new ExponentialMovingAverage { Length = length },
			BlauSmSmoothMethod.Smma => new SmoothedMovingAverage { Length = length },
			BlauSmSmoothMethod.Lwma => new WeightedMovingAverage { Length = length },
			_ => new ExponentialMovingAverage { Length = length },
		};
	}

	private static decimal? ProcessStage(IIndicator indicator, decimal value, DateTimeOffset time)
	{
		var result = indicator.Process(new DecimalIndicatorValue(indicator, value, time));
		return indicator.IsFormed ? result.ToDecimal() : null;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_highs.Clear();
		_lows.Clear();
		_smooth1 = null;
		_smooth2 = null;
		_smooth3 = null;
		_halfSmooth1 = null;
		_halfSmooth2 = null;
		_halfSmooth3 = null;
		_signalSmooth = null;
		IsFormed = false;
	}
}


public class BlauSmStochasticValue : ComplexIndicatorValue
{
	public BlauSmStochasticValue(IIndicator indicator, IIndicatorValue input, decimal main, decimal signal, decimal histogram)
		: base(indicator, input, (nameof(Main), main), (nameof(Signal), signal), (nameof(Histogram), histogram))
	{
	}

	/// <summary>
	/// Main oscillator value.
	/// </summary>
	public decimal Main => (decimal)GetValue(nameof(Main));

	/// <summary>
	/// Smoothed signal line.
	/// </summary>
	public decimal Signal => (decimal)GetValue(nameof(Signal));

	/// <summary>
	/// Histogram value (main minus signal).
	/// </summary>
	public decimal Histogram => (decimal)GetValue(nameof(Histogram));
}

/// <summary>
/// Signal generation modes.
/// </summary>
public enum BlauSmStochasticMode
{
	/// <summary>
	/// Uses histogram zero crossings.
	/// </summary>
	Breakdown,

	/// <summary>
	/// Uses momentum twists.
	/// </summary>
	Twist,

	/// <summary>
	/// Uses crossings between main and signal lines.
	/// </summary>
	CloudTwist
}

/// <summary>
/// Moving average options used in the oscillator smoothing stages.
/// </summary>
public enum BlauSmSmoothMethod
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
/// Applied price options for oscillator input.
/// </summary>
public enum BlauSmAppliedPrice
{
	/// <summary>
	/// Close price.
	/// </summary>
	Close,

	/// <summary>
	/// Open price.
	/// </summary>
	Open,

	/// <summary>
	/// High price.
	/// </summary>
	High,

	/// <summary>
	/// Low price.
	/// </summary>
	Low,

	/// <summary>
	/// (High + Low) / 2.
	/// </summary>
	Median,

	/// <summary>
	/// (Close + High + Low) / 3.
	/// </summary>
	Typical,

	/// <summary>
	/// (2 * Close + High + Low) / 4.
	/// </summary>
	Weighted,

	/// <summary>
	/// (Open + Close) / 2.
	/// </summary>
	Simple,

	/// <summary>
	/// (Open + Close + High + Low) / 4.
	/// </summary>
	Quarter,

	/// <summary>
	/// Trend-follow price using highs and lows.
	/// </summary>
	TrendFollow0,

	/// <summary>
	/// Average between close and extreme price in trend direction.
	/// </summary>
	TrendFollow1,

	/// <summary>
	/// Demark price variant.
	/// </summary>
	Demark
}
