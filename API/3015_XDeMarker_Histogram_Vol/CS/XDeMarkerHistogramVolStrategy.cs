using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that emulates the Exp_XDeMarker_Histogram_Vol expert advisor using StockSharp high-level API.
/// </summary>
public class XDeMarkerHistogramVolStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _deMarkerPeriod;
	private readonly StrategyParam<decimal> _highLevel1;
	private readonly StrategyParam<decimal> _highLevel2;
	private readonly StrategyParam<decimal> _lowLevel1;
	private readonly StrategyParam<decimal> _lowLevel2;
	private readonly StrategyParam<SmoothingMethod> _smoothingMethod;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<VolumeSource> _volumeSource;
	private readonly StrategyParam<bool> _enableLongEntries;
	private readonly StrategyParam<bool> _enableShortEntries;
	private readonly StrategyParam<bool> _enableLongExits;
	private readonly StrategyParam<bool> _enableShortExits;

	private LengthIndicator<decimal>? _valueSmoother;
	private LengthIndicator<decimal>? _volumeSmoother;

	private readonly List<int> _stateBuffer = new();
	private readonly Queue<decimal> _deMaxQueue = new();
	private readonly Queue<decimal> _deMinQueue = new();
	private decimal _deMaxSum;
	private decimal _deMinSum;
	private decimal? _previousHigh;
	private decimal? _previousLow;

	/// <summary>
	/// Initializes a new instance of the <see cref="XDeMarkerHistogramVolStrategy"/> class.
	/// </summary>
	public XDeMarkerHistogramVolStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(2).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for signals", "General");

		_deMarkerPeriod = Param(nameof(DeMarkerPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("DeMarker Period", "Smoothing period used by the DeMarker calculation", "Indicator")
			.SetCanOptimize(true);

		_highLevel1 = Param(nameof(HighLevel1), 15m)
			.SetDisplay("High Level 1", "Upper alert level multiplied by smoothed volume", "Indicator")
			.SetCanOptimize(true);

		_highLevel2 = Param(nameof(HighLevel2), 20m)
			.SetDisplay("High Level 2", "Extreme upper level multiplied by smoothed volume", "Indicator")
			.SetCanOptimize(true);

		_lowLevel1 = Param(nameof(LowLevel1), -15m)
			.SetDisplay("Low Level 1", "Lower alert level multiplied by smoothed volume", "Indicator")
			.SetCanOptimize(true);

		_lowLevel2 = Param(nameof(LowLevel2), -20m)
			.SetDisplay("Low Level 2", "Extreme lower level multiplied by smoothed volume", "Indicator")
			.SetCanOptimize(true);

		_smoothingMethod = Param(nameof(Smoothing), SmoothingMethod.Simple)
			.SetDisplay("Smoothing", "Moving average type applied to the histogram and volume", "Indicator");

		_smoothingLength = Param(nameof(SmoothingLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing Length", "Length of the moving averages", "Indicator")
			.SetCanOptimize(true);

		_signalBar = Param(nameof(SignalBar), 1)
			.SetGreaterThanZero()
			.SetDisplay("Signal Bar", "Number of closed bars used for signal detection", "Trading")
			.SetCanOptimize(true);

		_volumeSource = Param(nameof(VolumeType), VolumeSource.Tick)
			.SetDisplay("Volume Type", "Source of volume data used in weighting", "Indicator");

		_enableLongEntries = Param(nameof(EnableLongEntries), true)
			.SetDisplay("Enable Long Entries", "Allow opening long positions", "Trading");

		_enableShortEntries = Param(nameof(EnableShortEntries), true)
			.SetDisplay("Enable Short Entries", "Allow opening short positions", "Trading");

		_enableLongExits = Param(nameof(EnableLongExits), true)
			.SetDisplay("Enable Long Exits", "Allow closing long positions on opposite signals", "Trading");

		_enableShortExits = Param(nameof(EnableShortExits), true)
			.SetDisplay("Enable Short Exits", "Allow closing short positions on opposite signals", "Trading");
	}

	/// <summary>
	/// Primary timeframe for the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// DeMarker averaging period.
	/// </summary>
	public int DeMarkerPeriod { get => _deMarkerPeriod.Value; set => _deMarkerPeriod.Value = value; }

	/// <summary>
	/// Upper alert level multiplier.
	/// </summary>
	public decimal HighLevel1 { get => _highLevel1.Value; set => _highLevel1.Value = value; }

	/// <summary>
	/// Upper extreme level multiplier.
	/// </summary>
	public decimal HighLevel2 { get => _highLevel2.Value; set => _highLevel2.Value = value; }

	/// <summary>
	/// Lower alert level multiplier.
	/// </summary>
	public decimal LowLevel1 { get => _lowLevel1.Value; set => _lowLevel1.Value = value; }

	/// <summary>
	/// Lower extreme level multiplier.
	/// </summary>
	public decimal LowLevel2 { get => _lowLevel2.Value; set => _lowLevel2.Value = value; }

	/// <summary>
	/// Smoothing method applied to the histogram.
	/// </summary>
	public SmoothingMethod Smoothing { get => _smoothingMethod.Value; set => _smoothingMethod.Value = value; }

	/// <summary>
	/// Length of the smoothing moving averages.
	/// </summary>
	public int SmoothingLength { get => _smoothingLength.Value; set => _smoothingLength.Value = value; }

	/// <summary>
	/// Number of closed bars used for signal comparison.
	/// </summary>
	public int SignalBar { get => _signalBar.Value; set => _signalBar.Value = value; }

	/// <summary>
	/// Type of volume used in weighting the oscillator.
	/// </summary>
	public VolumeSource VolumeType { get => _volumeSource.Value; set => _volumeSource.Value = value; }

	/// <summary>
	/// Enable opening long positions.
	/// </summary>
	public bool EnableLongEntries { get => _enableLongEntries.Value; set => _enableLongEntries.Value = value; }

	/// <summary>
	/// Enable opening short positions.
	/// </summary>
	public bool EnableShortEntries { get => _enableShortEntries.Value; set => _enableShortEntries.Value = value; }

	/// <summary>
	/// Enable closing long positions on opposite signals.
	/// </summary>
	public bool EnableLongExits { get => _enableLongExits.Value; set => _enableLongExits.Value = value; }

	/// <summary>
	/// Enable closing short positions on opposite signals.
	/// </summary>
	public bool EnableShortExits { get => _enableShortExits.Value; set => _enableShortExits.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_valueSmoother = null;
		_volumeSmoother = null;
		_stateBuffer.Clear();
		_deMaxQueue.Clear();
		_deMinQueue.Clear();
		_deMaxSum = 0m;
		_deMinSum = 0m;
		_previousHigh = null;
		_previousLow = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_valueSmoother = CreateMovingAverage(Smoothing, SmoothingLength);
		_volumeSmoother = CreateMovingAverage(Smoothing, SmoothingLength);

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenNew(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || _valueSmoother is null || _volumeSmoother is null)
			return;

		var deMarker = CalculateDeMarker(candle);
		if (deMarker is null)
			return;

		var volume = GetVolume(candle);
		var weightedValue = ((deMarker.Value * 100m) - 50m) * volume;

		var valueResult = _valueSmoother.Process(new DecimalIndicatorValue(_valueSmoother, weightedValue, candle.OpenTime));
		var volumeResult = _volumeSmoother.Process(new DecimalIndicatorValue(_volumeSmoother, volume, candle.OpenTime));

		if (!valueResult.IsFinal || !volumeResult.IsFinal)
			return;

		var smoothedValue = valueResult.ToDecimal();
		var smoothedVolume = volumeResult.ToDecimal();

		var currentState = CalculateState(smoothedValue, smoothedVolume);
		_stateBuffer.Add(currentState);

		var minimumStates = SignalBar + 1;
		if (_stateBuffer.Count < minimumStates)
			return;

		var currentIndex = _stateBuffer.Count - SignalBar;
		var previousIndex = currentIndex - 1;
		if (currentIndex <= 0 || previousIndex < 0)
			return;

		var signalState = _stateBuffer[currentIndex];
		var previousState = _stateBuffer[previousIndex];

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (signalState == 1 && previousState > 1)
		{
			if (EnableShortExits && Position < 0)
				ClosePosition();

			if (EnableLongEntries && Position <= 0)
				BuyMarket();
		}

		if (signalState == 0 && previousState > 0)
		{
			if (EnableShortExits && Position < 0)
				ClosePosition();

			if (EnableLongEntries && Position <= 0)
				BuyMarket();
		}

		if (signalState == 3 && previousState < 3)
		{
			if (EnableLongExits && Position > 0)
				ClosePosition();

			if (EnableShortEntries && Position >= 0)
				SellMarket();
		}

		if (signalState == 4 && previousState < 4)
		{
			if (EnableLongExits && Position > 0)
				ClosePosition();

			if (EnableShortEntries && Position >= 0)
				SellMarket();
		}

		var maxStates = SignalBar + 2;
		if (_stateBuffer.Count > maxStates)
			_stateBuffer.RemoveRange(0, _stateBuffer.Count - maxStates);
	}

	private decimal? CalculateDeMarker(ICandleMessage candle)
	{
		if (_previousHigh is null || _previousLow is null)
		{
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			return null;
		}

		var deMax = Math.Max(candle.HighPrice - _previousHigh.Value, 0m);
		var deMin = Math.Max(_previousLow.Value - candle.LowPrice, 0m);

		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;

		_deMaxQueue.Enqueue(deMax);
		_deMinQueue.Enqueue(deMin);
		_deMaxSum += deMax;
		_deMinSum += deMin;

		if (_deMaxQueue.Count > DeMarkerPeriod)
		{
			_deMaxSum -= _deMaxQueue.Dequeue();
			_deMinSum -= _deMinQueue.Dequeue();
		}

		if (_deMaxQueue.Count < DeMarkerPeriod)
			return null;

		var denominator = _deMaxSum + _deMinSum;
		if (denominator == 0m)
			return 0.5m;

		return _deMaxSum / denominator;
	}

	private decimal GetVolume(ICandleMessage candle)
	{
		var volume = candle.TotalVolume ?? 0m;

		return VolumeType switch
		{
			VolumeSource.Tick => volume,
			VolumeSource.Real => volume,
			_ => volume,
		};
	}

	private int CalculateState(decimal value, decimal smoothedVolume)
	{
		var maxLevel = HighLevel2 * smoothedVolume;
		var upperLevel = HighLevel1 * smoothedVolume;
		var lowerLevel = LowLevel1 * smoothedVolume;
		var minLevel = LowLevel2 * smoothedVolume;

		if (value > maxLevel)
			return 0;

		if (value > upperLevel)
			return 1;

		if (value < minLevel)
			return 4;

		if (value < lowerLevel)
			return 3;

		return 2;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(SmoothingMethod method, int length)
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
	/// Supported moving average types.
	/// </summary>
	public enum SmoothingMethod
	{
		/// <summary>Simple moving average.</summary>
		Simple,
		/// <summary>Exponential moving average.</summary>
		Exponential,
		/// <summary>Smoothed moving average.</summary>
		Smoothed,
		/// <summary>Weighted moving average.</summary>
		Weighted,
	}

	/// <summary>
	/// Volume source used for weighting.
	/// </summary>
	public enum VolumeSource
	{
		/// <summary>Use tick volume. In StockSharp it falls back to candle volume.</summary>
		Tick,
		/// <summary>Use exchange reported volume.</summary>
		Real,
	}
}
