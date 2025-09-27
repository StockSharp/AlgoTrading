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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy converted from the MetaTrader expert Exp_XWPR_Histogram_Vol.
/// It recreates the volume-weighted Williams %R histogram and trades on the colour
/// transitions produced by the original custom indicator.
/// </summary>
public class ExpXwprHistogramVolStrategy : Strategy
{
	private readonly StrategyParam<decimal> _primaryVolume;
	private readonly StrategyParam<decimal> _secondaryVolume;
	private readonly StrategyParam<bool> _allowLongEntry;
	private readonly StrategyParam<bool> _allowShortEntry;
	private readonly StrategyParam<bool> _allowLongExit;
	private readonly StrategyParam<bool> _allowShortExit;
	private readonly StrategyParam<decimal> _stopLossSteps;
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<decimal> _deviationSteps;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<VolumeAggregations> _volumeAggregation;
	private readonly StrategyParam<decimal> _highLevel2;
	private readonly StrategyParam<decimal> _highLevel1;
	private readonly StrategyParam<decimal> _lowLevel1;
	private readonly StrategyParam<decimal> _lowLevel2;
	private readonly StrategyParam<SmoothMethods> _smoothingMethod;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<int> _smoothingPhase;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<int> _colorHistory = new();

	private bool _primaryLongActive;
	private bool _secondaryLongActive;
	private bool _primaryShortActive;
	private bool _secondaryShortActive;

	private DateTimeOffset? _lastProcessedCandle;

	/// <summary>
	/// Primary trade volume triggered by the level-one buy or sell signals.
	/// </summary>
	public decimal PrimaryVolume
	{
		get => _primaryVolume.Value;
		set => _primaryVolume.Value = value;
	}

	/// <summary>
	/// Secondary trade volume triggered by the stronger signals.
	/// </summary>
	public decimal SecondaryVolume
	{
		get => _secondaryVolume.Value;
		set => _secondaryVolume.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool AllowLongEntry
	{
		get => _allowLongEntry.Value;
		set => _allowLongEntry.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool AllowShortEntry
	{
		get => _allowShortEntry.Value;
		set => _allowShortEntry.Value = value;
	}

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool AllowLongExit
	{
		get => _allowLongExit.Value;
		set => _allowLongExit.Value = value;
	}

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool AllowShortExit
	{
		get => _allowShortExit.Value;
		set => _allowShortExit.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in instrument steps.
	/// </summary>
	public decimal StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in instrument steps.
	/// </summary>
	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	/// <summary>
	/// Slippage allowance kept for compatibility with the MQL inputs.
	/// </summary>
	public decimal DeviationSteps
	{
		get => _deviationSteps.Value;
		set => _deviationSteps.Value = value;
	}

	/// <summary>
	/// Offset, in closed candles, used when reading the histogram colour.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Williams %R lookback period.
	/// </summary>
	public int WprPeriod
	{
		get => _wprPeriod.Value;
		set => _wprPeriod.Value = value;
	}

	/// <summary>
	/// Volume aggregation used by the histogram (tick count or real volume).
	/// </summary>
	public VolumeAggregations VolumeMode
	{
		get => _volumeAggregation.Value;
		set => _volumeAggregation.Value = value;
	}

	/// <summary>
	/// Upper threshold multiplier used for the strongest bullish signals.
	/// </summary>
	public decimal HighLevel2
	{
		get => _highLevel2.Value;
		set => _highLevel2.Value = value;
	}

	/// <summary>
	/// Upper threshold multiplier used for moderate bullish signals.
	/// </summary>
	public decimal HighLevel1
	{
		get => _highLevel1.Value;
		set => _highLevel1.Value = value;
	}

	/// <summary>
	/// Lower threshold multiplier used for moderate bearish signals.
	/// </summary>
	public decimal LowLevel1
	{
		get => _lowLevel1.Value;
		set => _lowLevel1.Value = value;
	}

	/// <summary>
	/// Lower threshold multiplier used for the strongest bearish signals.
	/// </summary>
	public decimal LowLevel2
	{
		get => _lowLevel2.Value;
		set => _lowLevel2.Value = value;
	}

	/// <summary>
	/// Smoothing method applied to the histogram and the volume baseline.
	/// </summary>
	public SmoothMethods SmoothingMethod
	{
		get => _smoothingMethod.Value;
		set => _smoothingMethod.Value = value;
	}

	/// <summary>
	/// Length of the smoothing filters.
	/// </summary>
	public int SmoothingLength
	{
		get => _smoothingLength.Value;
		set => _smoothingLength.Value = value;
	}

	/// <summary>
	/// Phase argument forwarded to smoothers that support it.
	/// </summary>
	public int SmoothingPhase
	{
		get => _smoothingPhase.Value;
		set => _smoothingPhase.Value = value;
	}

	/// <summary>
	/// Candle series used to evaluate the indicator.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Creates a new instance of the <see cref="ExpXwprHistogramVolStrategy"/>.
	/// </summary>
	public ExpXwprHistogramVolStrategy()
	{
		_primaryVolume = Param(nameof(PrimaryVolume), 0.1m)
		.SetDisplay("Primary Volume", "Volume used by first-level signals", "Money Management")
		.SetRange(0.01m, 10m)
		.SetCanOptimize(true);

		_secondaryVolume = Param(nameof(SecondaryVolume), 0.2m)
		.SetDisplay("Secondary Volume", "Volume used by second-level signals", "Money Management")
		.SetRange(0.01m, 10m)
		.SetCanOptimize(true);

		_allowLongEntry = Param(nameof(AllowLongEntry), true)
		.SetDisplay("Allow Long Entry", "Enable opening of long positions", "General");

		_allowShortEntry = Param(nameof(AllowShortEntry), true)
		.SetDisplay("Allow Short Entry", "Enable opening of short positions", "General");

		_allowLongExit = Param(nameof(AllowLongExit), true)
		.SetDisplay("Allow Long Exit", "Enable closing of long positions", "General");

		_allowShortExit = Param(nameof(AllowShortExit), true)
		.SetDisplay("Allow Short Exit", "Enable closing of short positions", "General");

		_stopLossSteps = Param(nameof(StopLossSteps), 1000m)
		.SetDisplay("Stop Loss", "Stop-loss distance in price steps", "Risk Management")
		.SetRange(0m, 5000m)
		.SetCanOptimize(true);

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 2000m)
		.SetDisplay("Take Profit", "Take-profit distance in price steps", "Risk Management")
		.SetRange(0m, 10000m)
		.SetCanOptimize(true);

		_deviationSteps = Param(nameof(DeviationSteps), 10m)
		.SetDisplay("Deviation", "Reserved slippage parameter", "Compatibility");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetDisplay("Signal Bar", "Number of closed candles used for signals", "Indicator")
		.SetRange(0, 5)
		.SetCanOptimize(true);

		_wprPeriod = Param(nameof(WprPeriod), 14)
		.SetDisplay("WPR Period", "Williams %R lookback period", "Indicator")
		.SetRange(5, 200)
		.SetCanOptimize(true);

		_volumeAggregation = Param(nameof(VolumeMode), VolumeAggregations.Tick)
		.SetDisplay("Volume Mode", "Volume source used in the histogram", "Indicator");

		_highLevel2 = Param(nameof(HighLevel2), 17m)
		.SetDisplay("High Level 2", "Upper histogram multiplier for strong bullish zones", "Indicator")
		.SetRange(-100m, 100m)
		.SetCanOptimize(true);

		_highLevel1 = Param(nameof(HighLevel1), 5m)
		.SetDisplay("High Level 1", "Upper histogram multiplier for mild bullish zones", "Indicator")
		.SetRange(-100m, 100m)
		.SetCanOptimize(true);

		_lowLevel1 = Param(nameof(LowLevel1), -5m)
		.SetDisplay("Low Level 1", "Lower histogram multiplier for mild bearish zones", "Indicator")
		.SetRange(-100m, 100m)
		.SetCanOptimize(true);

		_lowLevel2 = Param(nameof(LowLevel2), -17m)
		.SetDisplay("Low Level 2", "Lower histogram multiplier for strong bearish zones", "Indicator")
		.SetRange(-100m, 100m)
		.SetCanOptimize(true);

		_smoothingMethod = Param(nameof(SmoothingMethod), SmoothMethods.Sma)
		.SetDisplay("Smoothing Method", "Type of moving average applied", "Indicator");

		_smoothingLength = Param(nameof(SmoothingLength), 12)
		.SetDisplay("Smoothing Length", "Length of the histogram smoother", "Indicator")
		.SetRange(1, 200)
		.SetCanOptimize(true);

		_smoothingPhase = Param(nameof(SmoothingPhase), 15)
		.SetDisplay("Smoothing Phase", "Phase parameter forwarded to Jurik-based smoothers", "Indicator")
		.SetRange(-100, 100)
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for the indicator", "General");
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

		_primaryLongActive = false;
		_secondaryLongActive = false;
		_primaryShortActive = false;
		_secondaryShortActive = false;
		_colorHistory.Clear();
		_lastProcessedCandle = null;

		var histogram = new XwprHistogramVolIndicator
		{
			Period = WprPeriod,
			VolumeMode = VolumeMode,
			HighLevel2 = HighLevel2,
			HighLevel1 = HighLevel1,
			LowLevel1 = LowLevel1,
			LowLevel2 = LowLevel2,
			Method = SmoothingMethod,
			Length = SmoothingLength,
			Phase = SmoothingPhase,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(histogram, ProcessIndicator)
		.Start();

		var stopLossUnit = StopLossSteps > 0m ? new Unit(StopLossSteps, UnitTypes.Step) : null;
		var takeProfitUnit = TakeProfitSteps > 0m ? new Unit(TakeProfitSteps, UnitTypes.Step) : null;

		if (stopLossUnit is not null || takeProfitUnit is not null)
		{
			StartProtection(takeProfit: takeProfitUnit, stopLoss: stopLossUnit, useMarketOrders: true);
		}
	}

	private void ProcessIndicator(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_lastProcessedCandle == candle.OpenTime)
		return;

		if (indicatorValue is not XwprHistogramVolValue histogramValue)
		return;

		if (!histogramValue.IsFormed || histogramValue.Color is null)
		return;

		_lastProcessedCandle = candle.OpenTime;

		_colorHistory.Insert(0, histogramValue.Color.Value);

		var required = Math.Max(2, SignalBar + 2);
		if (_colorHistory.Count > required + 5)
		_colorHistory.RemoveRange(required + 5, _colorHistory.Count - (required + 5));

		if (_colorHistory.Count <= SignalBar + 1)
		return;

		var recent = _colorHistory[SignalBar];
		var older = _colorHistory[SignalBar + 1];

		var openPrimaryLong = false;
		var openSecondaryLong = false;
		var closeLong = false;
		var openPrimaryShort = false;
		var openSecondaryShort = false;
		var closeShort = false;

		if (older == 1)
		{
			if (AllowLongEntry && recent > 1)
			openPrimaryLong = true;
			if (AllowShortExit)
			closeShort = true;
		}

		if (older == 0)
		{
			if (AllowLongEntry && recent > 0)
			openSecondaryLong = true;
			if (AllowShortExit)
			closeShort = true;
		}

		if (older == 3)
		{
			if (AllowShortEntry && recent < 3)
			openPrimaryShort = true;
			if (AllowLongExit)
			closeLong = true;
		}

		if (older == 4)
		{
			if (AllowShortEntry && recent < 4)
			openSecondaryShort = true;
			if (AllowLongExit)
			closeLong = true;
		}

		ExecuteSignals(openPrimaryLong, openSecondaryLong, closeLong, openPrimaryShort, openSecondaryShort, closeShort);
	}

	private void ExecuteSignals(bool openPrimaryLong, bool openSecondaryLong, bool closeLong, bool openPrimaryShort, bool openSecondaryShort, bool closeShort)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var longVolume = 0m;
		if (closeLong && (Position > 0m || _primaryLongActive || _secondaryLongActive))
		{
			if (_primaryLongActive)
			longVolume += PrimaryVolume;
			if (_secondaryLongActive)
			longVolume += SecondaryVolume;
			if (longVolume <= 0m && Position > 0m)
			longVolume = Math.Abs(Position);

			if (longVolume > 0m)
			{
				SellMarket(longVolume);
				_primaryLongActive = false;
				_secondaryLongActive = false;
			}
		}

		var shortVolume = 0m;
		if (closeShort && (Position < 0m || _primaryShortActive || _secondaryShortActive))
		{
			if (_primaryShortActive)
			shortVolume += PrimaryVolume;
			if (_secondaryShortActive)
			shortVolume += SecondaryVolume;
			if (shortVolume <= 0m && Position < 0m)
			shortVolume = Math.Abs(Position);

			if (shortVolume > 0m)
			{
				BuyMarket(shortVolume);
				_primaryShortActive = false;
				_secondaryShortActive = false;
			}
		}

		if (openPrimaryLong && !_primaryLongActive && !_primaryShortActive && !_secondaryShortActive)
		{
			var volume = PrimaryVolume;
			if (volume > 0m)
			{
				BuyMarket(volume);
				_primaryLongActive = true;
			}
		}

		if (openSecondaryLong && !_secondaryLongActive && !_primaryShortActive && !_secondaryShortActive)
		{
			var volume = SecondaryVolume;
			if (volume > 0m)
			{
				BuyMarket(volume);
				_secondaryLongActive = true;
			}
		}

		if (openPrimaryShort && !_primaryShortActive && !_primaryLongActive && !_secondaryLongActive)
		{
			var volume = PrimaryVolume;
			if (volume > 0m)
			{
				SellMarket(volume);
				_primaryShortActive = true;
			}
		}

		if (openSecondaryShort && !_secondaryShortActive && !_primaryLongActive && !_secondaryLongActive)
		{
			var volume = SecondaryVolume;
			if (volume > 0m)
			{
				SellMarket(volume);
				_secondaryShortActive = true;
			}
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position <= 0m)
		{
			_primaryLongActive = false;
			_secondaryLongActive = false;
		}

		if (Position >= 0m)
		{
			_primaryShortActive = false;
			_secondaryShortActive = false;
		}
	}
}

/// <summary>
/// Volume aggregation used by <see cref="XwprHistogramVolIndicator"/>.
/// </summary>
public enum VolumeAggregations
{
	/// <summary>
	/// Use the number of ticks traded inside the candle.
	/// </summary>
	Tick,

	/// <summary>
	/// Use the reported real volume of the candle.
	/// </summary>
	Real
}

/// <summary>
/// Available smoothing methods.
/// </summary>
public enum SmoothMethods
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
	/// Smoothed moving average (RMA).
	/// </summary>
	Smma,

	/// <summary>
	/// Linear weighted moving average.
	/// </summary>
	Lwma,

	/// <summary>
	/// Jurik moving average.
	/// </summary>
	Jjma,

	/// <summary>
	/// JurX approximation (mapped to Jurik moving average).
	/// </summary>
	JurX,

	/// <summary>
	/// Parabolic moving average approximation.
	/// </summary>
	ParMa,

	/// <summary>
	/// Triple exponential moving average.
	/// </summary>
	T3,

	/// <summary>
	/// Variable index dynamic average approximation.
	/// </summary>
	Vidya,

	/// <summary>
	/// Kaufman adaptive moving average.
	/// </summary>
	Ama
}

/// <summary>
/// Indicator replicating the XWPR Histogram Vol custom indicator.
/// </summary>
public class XwprHistogramVolIndicator : BaseIndicator<decimal>
{
	private WilliamsR _williams;
	private IIndicator _valueSmoother;
	private IIndicator _volumeSmoother;

	private int _lastPeriod;
	private SmoothMethods _lastMethod;
	private int _lastLength;
	private int _lastPhase;

	/// <summary>
	/// Williams %R period.
	/// </summary>
	public int Period { get; set; } = 14;

	/// <summary>
	/// Volume aggregation used in the histogram.
	/// </summary>
	public VolumeAggregations VolumeMode { get; set; } = VolumeAggregations.Tick;

	/// <summary>
	/// Upper histogram multiplier for strong bullish states.
	/// </summary>
	public decimal HighLevel2 { get; set; } = 17m;

	/// <summary>
	/// Upper histogram multiplier for moderate bullish states.
	/// </summary>
	public decimal HighLevel1 { get; set; } = 5m;

	/// <summary>
	/// Lower histogram multiplier for moderate bearish states.
	/// </summary>
	public decimal LowLevel1 { get; set; } = -5m;

	/// <summary>
	/// Lower histogram multiplier for strong bearish states.
	/// </summary>
	public decimal LowLevel2 { get; set; } = -17m;

	/// <summary>
	/// Smoothing method applied to both the histogram and the baseline volume.
	/// </summary>
	public SmoothMethods Method { get; set; } = SmoothMethods.Sma;

	/// <summary>
	/// Length used by the smoothing filters.
	/// </summary>
	public int Length { get; set; } = 12;

	/// <summary>
	/// Phase parameter forwarded to Jurik-based smoothers.
	/// </summary>
	public int Phase { get; set; } = 15;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
		return new XwprHistogramVolValue(this, input, null, null, null, null, null, null, null, false);

		EnsureIndicators();

		var wprValue = _williams!.Process(input);
		if (!wprValue.IsFinal)
		return new XwprHistogramVolValue(this, input, null, null, null, null, null, null, null, false);

		var wpr = wprValue.ToDecimal();
		var volume = GetVolume(candle);

		var histogramRaw = (wpr + 50m) * volume;

		var histogramValue = _valueSmoother!.Process(new DecimalIndicatorValue(_valueSmoother, histogramRaw, input.Time));
		var volumeValue = _volumeSmoother!.Process(new DecimalIndicatorValue(_volumeSmoother, volume, input.Time));

		if (!histogramValue.IsFinal || !volumeValue.IsFinal)
		return new XwprHistogramVolValue(this, input, null, null, null, null, null, null, null, false);

		var histogram = histogramValue.ToDecimal();
		var baseline = volumeValue.ToDecimal();

		var maxLevel = HighLevel2 * baseline;
		var upperLevel = HighLevel1 * baseline;
		var lowerLevel = LowLevel1 * baseline;
		var minLevel = LowLevel2 * baseline;

		var color = 2;
		if (histogram > maxLevel)
		color = 0;
		else if (histogram > upperLevel)
		color = 1;
		else if (histogram < minLevel)
		color = 4;
		else if (histogram < lowerLevel)
		color = 3;

		IsFormed = true;

		return new XwprHistogramVolValue(this, input, histogram, baseline, maxLevel, upperLevel, lowerLevel, minLevel, color, true);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();
		_williams?.Reset();
		_valueSmoother?.Reset();
		_volumeSmoother?.Reset();
	}

	private decimal GetVolume(ICandleMessage candle)
	{
		return VolumeMode switch
		{
			VolumeAggregations.Tick => candle.TotalTicks.HasValue ? candle.TotalTicks.Value : candle.TotalVolume ?? 0m,
			VolumeAggregations.Real => candle.TotalVolume ?? (candle.TotalTicks.HasValue ? candle.TotalTicks.Value : 0m),
			_ => candle.TotalVolume ?? 0m,
		};
	}

	private void EnsureIndicators()
	{
		if (_williams == null || _lastPeriod != Period)
		{
			_williams = new WilliamsR { Length = Math.Max(1, Period) };
			_lastPeriod = Period;
		}

		if (_valueSmoother == null || _volumeSmoother == null || _lastMethod != Method || _lastLength != Length || _lastPhase != Phase)
		{
			_lastMethod = Method;
			_lastLength = Length;
			_lastPhase = Phase;

			_valueSmoother = CreateSmoother();
			_volumeSmoother = CreateSmoother();
		}
	}

	private IIndicator CreateSmoother()
	{
		var length = Math.Max(1, Length);
		return Method switch
		{
			SmoothMethods.Sma => new SimpleMovingAverage { Length = length },
			SmoothMethods.Ema => new ExponentialMovingAverage { Length = length },
			SmoothMethods.Smma => new SmoothedMovingAverage { Length = length },
			SmoothMethods.Lwma => new WeightedMovingAverage { Length = length },
			SmoothMethods.Jjma => CreateJurik(length),
			SmoothMethods.JurX => CreateJurik(length),
			SmoothMethods.ParMa => new ExponentialMovingAverage { Length = length },
			SmoothMethods.T3 => new TripleExponentialMovingAverage { Length = length },
			SmoothMethods.Vidya => new ExponentialMovingAverage { Length = length },
			SmoothMethods.Ama => new KaufmanAdaptiveMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	private IIndicator CreateJurik(int length)
	{
		var jurik = new JurikMovingAverage { Length = length };
		jurik.Phase = Phase;
		return jurik;
	}
}

/// <summary>
/// Indicator output describing the histogram and the derived colour.
/// </summary>
public class XwprHistogramVolValue : ComplexIndicatorValue
{
	/// <summary>
	/// Creates a new instance of <see cref="XwprHistogramVolValue"/>.
	/// </summary>
	public XwprHistogramVolValue(IIndicator indicator, IIndicatorValue input, decimal? histogram, decimal? baseline, decimal? high2, decimal? high1, decimal? low1, decimal? low2, int? color, bool isFormed)
	: base(indicator, input,
	(nameof(Histogram), histogram),
	(nameof(Baseline), baseline),
	(nameof(HighLevel2), high2),
	(nameof(HighLevel1), high1),
	(nameof(LowLevel1), low1),
	(nameof(LowLevel2), low2),
	(nameof(Color), color))
	{
		IsFormed = isFormed;
	}

	/// <summary>
	/// Smoothed histogram value.
	/// </summary>
	public decimal? Histogram => GetNullableDecimal(nameof(Histogram));

	/// <summary>
	/// Smoothed baseline volume.
	/// </summary>
	public decimal? Baseline => GetNullableDecimal(nameof(Baseline));

	/// <summary>
	/// Upper level associated with strong bullish signals.
	/// </summary>
	public decimal? HighLevel2 => GetNullableDecimal(nameof(HighLevel2));

	/// <summary>
	/// Upper level associated with moderate bullish signals.
	/// </summary>
	public decimal? HighLevel1 => GetNullableDecimal(nameof(HighLevel1));

	/// <summary>
	/// Lower level associated with moderate bearish signals.
	/// </summary>
	public decimal? LowLevel1 => GetNullableDecimal(nameof(LowLevel1));

	/// <summary>
	/// Lower level associated with strong bearish signals.
	/// </summary>
	public decimal? LowLevel2 => GetNullableDecimal(nameof(LowLevel2));

	/// <summary>
	/// Colour index reproduced from the MetaTrader indicator (0-4).
	/// </summary>
	public int? Color => GetValue(nameof(Color)) as int?;

	private decimal? GetNullableDecimal(string name)
	{
		var value = GetValue(name);
		return value is decimal d ? d : null;
	}
}

