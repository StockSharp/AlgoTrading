namespace StockSharp.Samples.Strategies;

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

/// <summary>
/// Strategy that reproduces the MT5 expert Exp_XDeMarker_Histogram_Vol_Direct.
/// It evaluates the smoothed DeMarker histogram multiplied by volume
/// and opens trades when the short-term direction flips.
/// </summary>
public class XDeMarkerHistogramVolDirectStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _deMarkerPeriod;
	private readonly StrategyParam<VolumeSources> _volumeSource;
	private readonly StrategyParam<int> _highLevel2;
	private readonly StrategyParam<int> _highLevel1;
	private readonly StrategyParam<int> _lowLevel1;
	private readonly StrategyParam<int> _lowLevel2;
	private readonly StrategyParam<SmoothingMethods> _smoothingMethod;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<int> _smoothingPhase;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;

	private XDeMarkerHistogramVolDirectIndicator _indicator = null!;
	private readonly List<int> _directionHistory = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="XDeMarkerHistogramVolDirectStrategy"/> class.
	/// </summary>
	public XDeMarkerHistogramVolDirectStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(2).TimeFrame())
		.SetDisplay("Candle Type", "Source timeframe for calculations", "General");

		_deMarkerPeriod = Param(nameof(DeMarkerPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("DeMarker Period", "Length of the base DeMarker oscillator", "Indicator")
		.SetCanOptimize(true);

		_volumeSource = Param(nameof(VolumeSources), VolumeSources.Tick)
		.SetDisplay("Volume Source", "Volume stream used in calculations", "Indicator");

		_highLevel2 = Param(nameof(HighLevel2), 0)
		.SetDisplay("High Level 2", "Upper extreme multiplier", "Levels");

		_highLevel1 = Param(nameof(HighLevel1), 0)
		.SetDisplay("High Level 1", "Upper warning multiplier", "Levels");

		_lowLevel1 = Param(nameof(LowLevel1), 0)
		.SetDisplay("Low Level 1", "Lower warning multiplier", "Levels");

		_lowLevel2 = Param(nameof(LowLevel2), 0)
		.SetDisplay("Low Level 2", "Lower extreme multiplier", "Levels");

		_smoothingMethod = Param(nameof(SmoothingMethods), SmoothingMethods.Sma)
		.SetDisplay("Smoothing Method", "Moving average used for smoothing", "Indicator");

		_smoothingLength = Param(nameof(SmoothingLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("Smoothing Length", "Periods for smoothing both series", "Indicator")
		.SetCanOptimize(true);

		_smoothingPhase = Param(nameof(SmoothingPhase), 15)
		.SetDisplay("Smoothing Phase", "Compatibility placeholder for JJMA-style modes", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetDisplay("Signal Bar", "Historical offset used for signals (fixed to 1)", "Trading");

		_buyOpen = Param(nameof(BuyOpenEnabled), true)
		.SetDisplay("Allow Long Entry", "Enable opening long positions", "Trading");

		_sellOpen = Param(nameof(SellOpenEnabled), true)
		.SetDisplay("Allow Short Entry", "Enable opening short positions", "Trading");

		_buyClose = Param(nameof(BuyCloseEnabled), true)
		.SetDisplay("Allow Long Exit", "Enable closing long positions on opposite signals", "Trading");

		_sellClose = Param(nameof(SellCloseEnabled), true)
		.SetDisplay("Allow Short Exit", "Enable closing short positions on opposite signals", "Trading");
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
	/// Period of the base DeMarker oscillator.
	/// </summary>
	public int DeMarkerPeriod
	{
		get => _deMarkerPeriod.Value;
		set => _deMarkerPeriod.Value = value;
	}

	/// <summary>
	/// Source of volume used in indicator calculations.
	/// </summary>
	public VolumeSources VolumeSources
	{
		get => _volumeSource.Value;
		set => _volumeSource.Value = value;
	}

	/// <summary>
	/// Upper extreme multiplier.
	/// </summary>
	public int HighLevel2
	{
		get => _highLevel2.Value;
		set => _highLevel2.Value = value;
	}

	/// <summary>
	/// Upper warning multiplier.
	/// </summary>
	public int HighLevel1
	{
		get => _highLevel1.Value;
		set => _highLevel1.Value = value;
	}

	/// <summary>
	/// Lower warning multiplier.
	/// </summary>
	public int LowLevel1
	{
		get => _lowLevel1.Value;
		set => _lowLevel1.Value = value;
	}

	/// <summary>
	/// Lower extreme multiplier.
	/// </summary>
	public int LowLevel2
	{
		get => _lowLevel2.Value;
		set => _lowLevel2.Value = value;
	}

	/// <summary>
	/// Moving average type applied to the histogram and volume.
	/// </summary>
	public SmoothingMethods SmoothingMethods
	{
		get => _smoothingMethod.Value;
		set => _smoothingMethod.Value = value;
	}

	/// <summary>
	/// Length of the smoothing moving average.
	/// </summary>
	public int SmoothingLength
	{
		get => _smoothingLength.Value;
		set => _smoothingLength.Value = value;
	}

	/// <summary>
	/// Placeholder parameter for compatibility with the original script.
	/// </summary>
	public int SmoothingPhase
	{
		get => _smoothingPhase.Value;
		set => _smoothingPhase.Value = value;
	}

	/// <summary>
	/// Historical offset for signal evaluation (kept for reference, fixed at 1).
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Enables opening of long positions.
	/// </summary>
	public bool BuyOpenEnabled
	{
		get => _buyOpen.Value;
		set => _buyOpen.Value = value;
	}

	/// <summary>
	/// Enables opening of short positions.
	/// </summary>
	public bool SellOpenEnabled
	{
		get => _sellOpen.Value;
		set => _sellOpen.Value = value;
	}

	/// <summary>
	/// Enables closing of long positions on down signals.
	/// </summary>
	public bool BuyCloseEnabled
	{
		get => _buyClose.Value;
		set => _buyClose.Value = value;
	}

	/// <summary>
	/// Enables closing of short positions on up signals.
	/// </summary>
	public bool SellCloseEnabled
	{
		get => _sellClose.Value;
		set => _sellClose.Value = value;
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
		_directionHistory.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (SignalBar != 1)
		{
			LogWarning($"SignalBar value {SignalBar} is not supported. The strategy always uses a one-bar delay as in the original expert.");
		}

		_indicator = new XDeMarkerHistogramVolDirectIndicator
		{
			Period = DeMarkerPeriod,
			VolumeSources = VolumeSources,
			HighLevel2 = HighLevel2,
			HighLevel1 = HighLevel1,
			LowLevel1 = LowLevel1,
			LowLevel2 = LowLevel2,
			Method = SmoothingMethods,
			Length = SmoothingLength,
			Phase = SmoothingPhase
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_indicator, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _indicator);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var value = (XDeMarkerHistogramVolDirectValue)indicatorValue;
		if (!value.IsSignalFormed)
		return;

		var currentDirection = value.Direction;
		_directionHistory.Add(currentDirection);
		if (_directionHistory.Count > 4)
		_directionHistory.RemoveRange(0, _directionHistory.Count - 4);

		if (_directionHistory.Count < 2)
		return;

		var previousDirection = _directionHistory[^2];

		var closeShort = SellCloseEnabled && previousDirection == (int)DirectionColors.Up && Position < 0;
		var closeLong = BuyCloseEnabled && previousDirection == (int)DirectionColors.Down && Position > 0;
		var openLong = BuyOpenEnabled && previousDirection == (int)DirectionColors.Up && currentDirection == (int)DirectionColors.Down && Position <= 0;
		var openShort = SellOpenEnabled && previousDirection == (int)DirectionColors.Down && currentDirection == (int)DirectionColors.Up && Position >= 0;

		decimal? targetPosition = null;

		if (openLong)
		{
			targetPosition = Volume;
		}
		else if (openShort)
		{
			targetPosition = -Volume;
		}
		else if (closeLong || closeShort)
		{
			targetPosition = 0m;
		}

		if (targetPosition is decimal target)
		{
			AdjustPosition(target);
		}
	}

	private void AdjustPosition(decimal targetPosition)
	{
		var difference = targetPosition - Position;
		if (difference > 0m)
		{
			BuyMarket(difference);
		}
		else if (difference < 0m)
		{
			SellMarket(-difference);
		}
	}
}

/// <summary>
/// Source of volume for the indicator calculations.
/// </summary>
public enum VolumeSources
{
	/// <summary>
	/// Use tick count (number of trades).
	/// </summary>
	Tick,

	/// <summary>
	/// Use traded volume in units.
	/// </summary>
	Real
}

/// <summary>
/// Moving average types supported by the custom indicator.
/// </summary>
public enum SmoothingMethods
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
	/// Smoothed (RMA/SMMA) moving average.
	/// </summary>
	Smma,

	/// <summary>
	/// Linear weighted moving average.
	/// </summary>
	Wma
}

/// <summary>
/// Direction labels produced by the indicator.
/// </summary>
public enum DirectionColors
{
	/// <summary>
	/// Histogram is rising compared to the previous bar.
	/// </summary>
	Up = 0,

	/// <summary>
	/// Histogram is falling compared to the previous bar.
	/// </summary>
	Down = 1
}

/// <summary>
/// Custom indicator replicating XDeMarker_Histogram_Vol_Direct.
/// </summary>
public class XDeMarkerHistogramVolDirectIndicator : BaseIndicator<decimal>
{
	private readonly Queue<decimal> _deMax = new();
	private readonly Queue<decimal> _deMin = new();
	private decimal _sumDeMax;
	private decimal _sumDeMin;
	private decimal? _prevHigh;
	private decimal? _prevLow;
	private decimal? _prevSmoothedValue;
	private int _prevDirection = (int)DirectionColors.Up;
	private IIndicator _histogramMa = null!;
	private IIndicator _volumeMa = null!;

	/// <summary>
	/// Period of the DeMarker oscillator.
	/// </summary>
	public int Period { get; set; } = 14;

	/// <summary>
	/// Source of volume used for scaling the histogram.
	/// </summary>
	public VolumeSources VolumeSources { get; set; } = VolumeSources.Tick;

	/// <summary>
	/// Upper extreme multiplier.
	/// </summary>
	public int HighLevel2 { get; set; }

	/// <summary>
	/// Upper warning multiplier.
	/// </summary>
	public int HighLevel1 { get; set; }

	/// <summary>
	/// Lower warning multiplier.
	/// </summary>
	public int LowLevel1 { get; set; }

	/// <summary>
	/// Lower extreme multiplier.
	/// </summary>
	public int LowLevel2 { get; set; }

	/// <summary>
	/// Moving average type for smoothing.
	/// </summary>
	public SmoothingMethods Method { get; set; } = SmoothingMethods.Sma;

	/// <summary>
	/// Length of the smoothing windows for histogram and volume.
	/// </summary>
	public int Length { get; set; } = 12;

	/// <summary>
	/// Placeholder parameter to keep parity with the original implementation.
	/// </summary>
	public int Phase { get; set; } = 15;

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();
		_deMax.Clear();
		_deMin.Clear();
		_sumDeMax = 0m;
		_sumDeMin = 0m;
		_prevHigh = null;
		_prevLow = null;
		_prevSmoothedValue = null;
		_prevDirection = (int)DirectionColors.Up;
		_histogramMa?.Reset();
		_volumeMa?.Reset();
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
		{
			return new XDeMarkerHistogramVolDirectValue(this, input, false, 0m, 0m, 0m, 0m, 0m, _prevDirection, _prevDirection);
		}

		if (Period <= 0)
		throw new InvalidOperationException("Period must be greater than zero.");

		var high = candle.HighPrice;
		var low = candle.LowPrice;

		if (high == null || low == null)
		{
			return new XDeMarkerHistogramVolDirectValue(this, input, false, 0m, 0m, 0m, 0m, 0m, _prevDirection, _prevDirection);
		}

		if (_prevHigh is null || _prevLow is null)
		{
			_prevHigh = high.Value;
			_prevLow = low.Value;
			return new XDeMarkerHistogramVolDirectValue(this, input, false, 0m, 0m, 0m, 0m, 0m, _prevDirection, _prevDirection);
		}

		var deMax = Math.Max(high.Value - _prevHigh.Value, 0m);
		var deMin = Math.Max(_prevLow.Value - low.Value, 0m);

		_prevHigh = high.Value;
		_prevLow = low.Value;

		_deMax.Enqueue(deMax);
		_sumDeMax += deMax;
		if (_deMax.Count > Period)
		{
			_sumDeMax -= _deMax.Dequeue();
		}

		_deMin.Enqueue(deMin);
		_sumDeMin += deMin;
		if (_deMin.Count > Period)
		{
			_sumDeMin -= _deMin.Dequeue();
		}

		var enoughHistory = _deMax.Count >= Period;
		if (!enoughHistory)
		{
			return new XDeMarkerHistogramVolDirectValue(this, input, false, 0m, 0m, 0m, 0m, 0m, _prevDirection, _prevDirection);
		}

		var denom = _sumDeMax + _sumDeMin;
		var demarker = denom == 0m ? 0m : _sumDeMax / denom;
		var histogram = (demarker * 100m) - 50m;

		var volume = VolumeSources switch
		{
			VolumeSources.Tick => candle.TotalTicks.HasValue ? (decimal)candle.TotalTicks.Value : candle.TotalVolume ?? 0m,
			_ => candle.TotalVolume ?? 0m
		};

		var scaledHistogram = histogram * volume;

		_histogramMa ??= CreateMovingAverage(Method, Length);
		_volumeMa ??= CreateMovingAverage(Method, Length);

		var histogramValue = _histogramMa.Process(new DecimalIndicatorValue(_histogramMa, scaledHistogram, input.Time)).ToDecimal();
		var volumeValue = _volumeMa.Process(new DecimalIndicatorValue(_volumeMa, volume, input.Time)).ToDecimal();

		var upperExtreme = HighLevel2 * volumeValue;
		var upperWarning = HighLevel1 * volumeValue;
		var lowerWarning = LowLevel1 * volumeValue;
		var lowerExtreme = LowLevel2 * volumeValue;

		var histogramColor = 2;
		if (histogramValue > upperExtreme)
		{
			histogramColor = 0;
		}
		else if (histogramValue > upperWarning)
		{
			histogramColor = 1;
		}
		else if (histogramValue < lowerExtreme)
		{
			histogramColor = 4;
		}
		else if (histogramValue < lowerWarning)
		{
			histogramColor = 3;
		}

		var direction = _prevSmoothedValue is null
		? _prevDirection
		: histogramValue > _prevSmoothedValue.Value
		? (int)DirectionColors.Up
		: histogramValue < _prevSmoothedValue.Value
		? (int)DirectionColors.Down
		: _prevDirection;

		_prevSmoothedValue = histogramValue;
		_prevDirection = direction;

		var formed = _histogramMa.IsFormed && _volumeMa.IsFormed && enoughHistory;

		return new XDeMarkerHistogramVolDirectValue(
		this,
		input,
		formed,
		histogramValue,
		upperWarning,
		upperExtreme,
		lowerWarning,
		lowerExtreme,
		direction,
		histogramColor);
	}

	private static IIndicator CreateMovingAverage(SmoothingMethods method, int length)
	{
		return method switch
		{
			SmoothingMethods.Sma => new SimpleMovingAverage { Length = length },
			SmoothingMethods.Ema => new ExponentialMovingAverage { Length = length },
			SmoothingMethods.Smma => new SmoothedMovingAverage { Length = length },
			SmoothingMethods.Wma => new WeightedMovingAverage { Length = length },
			_ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
		};
	}
}

/// <summary>
/// Complex indicator value containing histogram and directional information.
/// </summary>
public class XDeMarkerHistogramVolDirectValue : ComplexIndicatorValue
{
	public XDeMarkerHistogramVolDirectValue(
	IIndicator indicator,
	IIndicatorValue input,
	bool isSignalFormed,
	decimal histogram,
	decimal upperWarning,
	decimal upperExtreme,
	decimal lowerWarning,
	decimal lowerExtreme,
	int direction,
	int histogramColor)
	: base(indicator, input,
	(nameof(IsSignalFormed), isSignalFormed),
	(nameof(Histogram), histogram),
	(nameof(UpperWarning), upperWarning),
	(nameof(UpperExtreme), upperExtreme),
	(nameof(LowerWarning), lowerWarning),
	(nameof(LowerExtreme), lowerExtreme),
	(nameof(Direction), direction),
	(nameof(HistogramColor), histogramColor))
	{
	}

	/// <summary>
	/// Indicates whether the indicator has enough data for trading decisions.
	/// </summary>
	public bool IsSignalFormed => (bool)GetValue(nameof(IsSignalFormed));

	/// <summary>
	/// Current smoothed histogram value.
	/// </summary>
	public decimal Histogram => (decimal)GetValue(nameof(Histogram));

	/// <summary>
	/// First upper level.
	/// </summary>
	public decimal UpperWarning => (decimal)GetValue(nameof(UpperWarning));

	/// <summary>
	/// Second upper level.
	/// </summary>
	public decimal UpperExtreme => (decimal)GetValue(nameof(UpperExtreme));

	/// <summary>
	/// First lower level.
	/// </summary>
	public decimal LowerWarning => (decimal)GetValue(nameof(LowerWarning));

	/// <summary>
	/// Second lower level.
	/// </summary>
	public decimal LowerExtreme => (decimal)GetValue(nameof(LowerExtreme));

	/// <summary>
	/// Direction flag used for trading decisions.
	/// </summary>
	public int Direction => (int)GetValue(nameof(Direction));

	/// <summary>
	/// Histogram color index for plotting.
	/// </summary>
	public int HistogramColor => (int)GetValue(nameof(HistogramColor));
}

