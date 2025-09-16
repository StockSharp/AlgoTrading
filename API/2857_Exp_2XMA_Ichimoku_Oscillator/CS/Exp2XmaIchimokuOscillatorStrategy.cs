using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that reproduces the Exp 2XMA Ichimoku oscillator logic using high level StockSharp API.
/// </summary>
public class Exp2XmaIchimokuOscillatorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _upPeriod1;
	private readonly StrategyParam<int> _downPeriod1;
	private readonly StrategyParam<int> _upPeriod2;
	private readonly StrategyParam<int> _downPeriod2;
	private readonly StrategyParam<int> _xLength1;
	private readonly StrategyParam<int> _xLength2;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<SmoothingMethod> _method1;
	private readonly StrategyParam<SmoothingMethod> _method2;
	private readonly StrategyParam<bool> _enableBuyOpen;
	private readonly StrategyParam<bool> _enableSellOpen;
	private readonly StrategyParam<bool> _enableBuyClose;
	private readonly StrategyParam<bool> _enableSellClose;

	private Highest _upHighest1 = null!;
	private Lowest _downLowest1 = null!;
	private Highest _upHighest2 = null!;
	private Lowest _downLowest2 = null!;
	private IIndicator _smoother1 = null!;
	private IIndicator _smoother2 = null!;

	private TrendColor?[] _colorHistory = Array.Empty<TrendColor?>();
	private decimal? _previousOscillator;

	/// <summary>
	/// Initializes a new instance of the <see cref="Exp2XmaIchimokuOscillatorStrategy"/> class.
	/// </summary>
	public Exp2XmaIchimokuOscillatorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Indicator Timeframe", "Timeframe used for the oscillator", "General");

		_upPeriod1 = Param(nameof(UpPeriod1), 6)
			.SetDisplay("Up Period #1", "Lookback for the first highest high", "Ichimoku")
			.SetCanOptimize(true)
			.SetOptimize(3, 12, 1);

		_downPeriod1 = Param(nameof(DownPeriod1), 6)
			.SetDisplay("Down Period #1", "Lookback for the first lowest low", "Ichimoku")
			.SetCanOptimize(true)
			.SetOptimize(3, 12, 1);

		_upPeriod2 = Param(nameof(UpPeriod2), 9)
			.SetDisplay("Up Period #2", "Lookback for the second highest high", "Ichimoku")
			.SetCanOptimize(true)
			.SetOptimize(6, 18, 1);

		_downPeriod2 = Param(nameof(DownPeriod2), 9)
			.SetDisplay("Down Period #2", "Lookback for the second lowest low", "Ichimoku")
			.SetCanOptimize(true)
			.SetOptimize(6, 18, 1);

		_xLength1 = Param(nameof(XLength1), 25)
			.SetDisplay("Smoothing Length #1", "Length of the first moving average", "Smoothing")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 5);

		_xLength2 = Param(nameof(XLength2), 80)
			.SetDisplay("Smoothing Length #2", "Length of the second moving average", "Smoothing")
			.SetCanOptimize(true)
			.SetOptimize(20, 120, 5);

		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Bar", "Shift used to evaluate oscillator colors", "Signals")
			.SetGreaterThanZero();

		_method1 = Param(nameof(Method1), SmoothingMethod.Simple)
			.SetDisplay("Smoothing Method #1", "Moving average type for the first series", "Smoothing");

		_method2 = Param(nameof(Method2), SmoothingMethod.Simple)
			.SetDisplay("Smoothing Method #2", "Moving average type for the second series", "Smoothing");

		_enableBuyOpen = Param(nameof(EnableBuyOpen), true)
			.SetDisplay("Enable Long Entries", "Allow opening long positions", "Trading");

		_enableSellOpen = Param(nameof(EnableSellOpen), true)
			.SetDisplay("Enable Short Entries", "Allow opening short positions", "Trading");

		_enableBuyClose = Param(nameof(EnableBuyClose), true)
			.SetDisplay("Enable Long Exits", "Allow closing long positions", "Trading");

		_enableSellClose = Param(nameof(EnableSellClose), true)
			.SetDisplay("Enable Short Exits", "Allow closing short positions", "Trading");

		Volume = 1m;
	}

	/// <summary>
	/// Timeframe used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period for the first highest high computation.
	/// </summary>
	public int UpPeriod1
	{
		get => _upPeriod1.Value;
		set => _upPeriod1.Value = value;
	}

	/// <summary>
	/// Period for the first lowest low computation.
	/// </summary>
	public int DownPeriod1
	{
		get => _downPeriod1.Value;
		set => _downPeriod1.Value = value;
	}

	/// <summary>
	/// Period for the second highest high computation.
	/// </summary>
	public int UpPeriod2
	{
		get => _upPeriod2.Value;
		set => _upPeriod2.Value = value;
	}

	/// <summary>
	/// Period for the second lowest low computation.
	/// </summary>
	public int DownPeriod2
	{
		get => _downPeriod2.Value;
		set => _downPeriod2.Value = value;
	}

	/// <summary>
	/// Length of the first smoothing average.
	/// </summary>
	public int XLength1
	{
		get => _xLength1.Value;
		set => _xLength1.Value = value;
	}

	/// <summary>
	/// Length of the second smoothing average.
	/// </summary>
	public int XLength2
	{
		get => _xLength2.Value;
		set => _xLength2.Value = value;
	}

	/// <summary>
	/// Number of bars used as a signal shift.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Smoothing method for the first averaged series.
	/// </summary>
	public SmoothingMethod Method1
	{
		get => _method1.Value;
		set => _method1.Value = value;
	}

	/// <summary>
	/// Smoothing method for the second averaged series.
	/// </summary>
	public SmoothingMethod Method2
	{
		get => _method2.Value;
		set => _method2.Value = value;
	}

	/// <summary>
	/// Enables long entries.
	/// </summary>
	public bool EnableBuyOpen
	{
		get => _enableBuyOpen.Value;
		set => _enableBuyOpen.Value = value;
	}

	/// <summary>
	/// Enables short entries.
	/// </summary>
	public bool EnableSellOpen
	{
		get => _enableSellOpen.Value;
		set => _enableSellOpen.Value = value;
	}

	/// <summary>
	/// Enables closing of long positions.
	/// </summary>
	public bool EnableBuyClose
	{
		get => _enableBuyClose.Value;
		set => _enableBuyClose.Value = value;
	}

	/// <summary>
	/// Enables closing of short positions.
	/// </summary>
	public bool EnableSellClose
	{
		get => _enableSellClose.Value;
		set => _enableSellClose.Value = value;
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

		_previousOscillator = null;
		_colorHistory = Array.Empty<TrendColor?>();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_upHighest1 = new Highest { Length = UpPeriod1 };
		_downLowest1 = new Lowest { Length = DownPeriod1 };
		_upHighest2 = new Highest { Length = UpPeriod2 };
		_downLowest2 = new Lowest { Length = DownPeriod2 };
		_smoother1 = CreateMovingAverage(Method1, XLength1);
		_smoother2 = CreateMovingAverage(Method2, XLength2);

		_colorHistory = new TrendColor?[SignalBar + 2];
		_previousOscillator = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Work only with closed candles.
		if (candle.State != CandleStates.Finished)
		return;

		// Update highest and lowest price envelopes.
		var highest1 = _upHighest1.Process(candle.HighPrice).ToDecimal();
		var lowest1 = _downLowest1.Process(candle.LowPrice).ToDecimal();
		var highest2 = _upHighest2.Process(candle.HighPrice).ToDecimal();
		var lowest2 = _downLowest2.Process(candle.LowPrice).ToDecimal();

		if (!_upHighest1.IsFormed || !_downLowest1.IsFormed || !_upHighest2.IsFormed || !_downLowest2.IsFormed)
		{
			_previousOscillator = null;
			return;
		}

		// Calculate Ichimoku style midpoints.
		var base1 = (highest1 + lowest1) / 2m;
		var base2 = (highest2 + lowest2) / 2m;

		var smooth1 = _smoother1.Process(base1, candle.CloseTime, true).ToDecimal();
		var smooth2 = _smoother2.Process(base2, candle.CloseTime, true).ToDecimal();

		var oscillator = smooth1 - smooth2;

		// Skip color calculation until both smoothers are formed.
		if (!_smoother1.IsFormed || !_smoother2.IsFormed)
		{
			_previousOscillator = oscillator;
			return;
		}

		var color = CalculateColor(oscillator);

		bool buyOpenSignal = false;
		bool sellOpenSignal = false;
		bool buyCloseSignal = false;
		bool sellCloseSignal = false;

		// Colors are stored with index 0 = latest finished bar.
		if (_colorHistory.Length > SignalBar + 1)
		{
			var recentColor = _colorHistory[SignalBar];
			var olderColor = _colorHistory[SignalBar + 1];

			if (recentColor.HasValue && olderColor.HasValue)
			{
				// Recreate the original MQL condition set.
				if (olderColor.Value is TrendColor.PositiveRising or TrendColor.NegativeRising)
				{
					if (EnableBuyOpen && recentColor.Value is TrendColor.PositiveFalling or TrendColor.NegativeFalling)
						buyOpenSignal = true;

					if (EnableSellClose)
						sellCloseSignal = true;
				}

				if (olderColor.Value is TrendColor.PositiveFalling or TrendColor.NegativeFalling)
				{
					if (EnableSellOpen && recentColor.Value is TrendColor.PositiveRising or TrendColor.NegativeRising)
						sellOpenSignal = true;

					if (EnableBuyClose)
						buyCloseSignal = true;
				}
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdateState(color, oscillator);
			return;
		}

		// Execute exits first to mimic the original expert behaviour.
		if (buyCloseSignal && Position > 0)
		{
			SellMarket(Position);
		}

		if (sellCloseSignal && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		var adjustedVolume = Volume + Math.Abs(Position);

		if (buyOpenSignal && Position <= 0 && adjustedVolume > 0)
		{
			BuyMarket(adjustedVolume);
		}

		if (sellOpenSignal && Position >= 0 && adjustedVolume > 0)
		{
			SellMarket(adjustedVolume);
		}

		UpdateState(color, oscillator);
	}

	private void UpdateState(TrendColor color, decimal oscillator)
	{
		_previousOscillator = oscillator;

		if (_colorHistory.Length == 0)
		return;

		for (var i = _colorHistory.Length - 1; i > 0; i--)
		{
			_colorHistory[i] = _colorHistory[i - 1];
		}

		_colorHistory[0] = color;
	}

	private TrendColor CalculateColor(decimal oscillator)
	{
		if (!_previousOscillator.HasValue)
		return TrendColor.Neutral;

		var prev = _previousOscillator.Value;

		if (oscillator >= 0m)
		{
			if (oscillator > prev)
				return TrendColor.PositiveRising;

			if (oscillator < prev)
				return TrendColor.PositiveFalling;
		}
		else
		{
			if (oscillator < prev)
				return TrendColor.NegativeFalling;

			if (oscillator > prev)
				return TrendColor.NegativeRising;
		}

		return _colorHistory.Length > 0 && _colorHistory[0].HasValue
		? _colorHistory[0]!.Value
		: TrendColor.Neutral;
	}

	private static IIndicator CreateMovingAverage(SmoothingMethod method, int length)
	{
		return method switch
		{
			SmoothingMethod.Simple => new SimpleMovingAverage { Length = length },
			SmoothingMethod.Exponential => new ExponentialMovingAverage { Length = length },
			SmoothingMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			SmoothingMethod.Weighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	/// <summary>
	/// Supported smoothing methods.
	/// </summary>
	public enum SmoothingMethod
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted,
	}

	private enum TrendColor
	{
		Neutral = 2,
		PositiveRising = 0,
		PositiveFalling = 1,
		NegativeRising = 3,
		NegativeFalling = 4,
	}
}
