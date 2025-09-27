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

using System.Reflection;
using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Exp i-KlPrice Vol Direct strategy converted from the MetaTrader 5 expert advisor.
/// The system weights a custom oscillator by volume, applies multiple smoothing stages, and trades when the slope flips.
/// </summary>
public class ExpIKlPriceVolDirectStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<VolumeModes> _volumeMode;
	private readonly StrategyParam<SmoothingMethods> _priceMethod;
	private readonly StrategyParam<int> _priceLength;
	private readonly StrategyParam<int> _pricePhase;
	private readonly StrategyParam<SmoothingMethods> _rangeMethod;
	private readonly StrategyParam<int> _rangeLength;
	private readonly StrategyParam<int> _rangePhase;
	private readonly StrategyParam<int> _resultLength;
	private readonly StrategyParam<AppliedPrices> _appliedPrice;
	private readonly StrategyParam<decimal> _highLevel2;
	private readonly StrategyParam<decimal> _highLevel1;
	private readonly StrategyParam<decimal> _lowLevel1;
	private readonly StrategyParam<decimal> _lowLevel2;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _allowLongEntries;
	private readonly StrategyParam<bool> _allowShortEntries;
	private readonly StrategyParam<bool> _allowLongExits;
	private readonly StrategyParam<bool> _allowShortExits;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;

	private IIndicator _priceSmoother = null!;
	private IIndicator _rangeSmoother = null!;
	private IIndicator _resultSmoother = null!;
	private IIndicator _volumeSmoother = null!;

	private readonly List<int> _colorHistory = new();
	private decimal? _previousOscillatorValue;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpIKlPriceVolDirectStrategy"/> class.
	/// </summary>
	public ExpIKlPriceVolDirectStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for the KlPrice calculations", "Indicator");

		_volumeMode = Param(nameof(VolumeSource), VolumeModes.Tick)
		.SetDisplay("Volume Source", "Volume stream used to weight the oscillator", "Indicator");

		_priceMethod = Param(nameof(PriceMethod), SmoothingMethods.Sma)
		.SetDisplay("Price Method", "Smoothing applied to the base price", "Indicator");

		_priceLength = Param(nameof(PriceLength), 100)
		.SetGreaterThanZero()
		.SetDisplay("Price Length", "Length of the base price smoother", "Indicator");

		_pricePhase = Param(nameof(PricePhase), 15)
		.SetDisplay("Price Phase", "Phase parameter used by Jurik-based price smoothing", "Indicator");

		_rangeMethod = Param(nameof(RangeMethod), SmoothingMethods.Jjma)
		.SetDisplay("Range Method", "Smoothing applied to the high-low range", "Indicator");

		_rangeLength = Param(nameof(RangeLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("Range Length", "Length of the range smoother", "Indicator");

		_rangePhase = Param(nameof(RangePhase), 100)
		.SetDisplay("Range Phase", "Phase parameter used by Jurik-based range smoothing", "Indicator");

		_resultLength = Param(nameof(ResultLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("Result Length", "Length of the Jurik smoother applied to the weighted oscillator", "Indicator");

		_appliedPrice = Param(nameof(PriceMode), AppliedPrices.Close)
		.SetDisplay("Applied Price", "Price source processed by the indicator", "Indicator");

		_highLevel2 = Param(nameof(HighLevel2), 0m)
		.SetDisplay("High Level 2", "Outer bullish multiplier kept for compatibility", "Indicator");

		_highLevel1 = Param(nameof(HighLevel1), 0m)
		.SetDisplay("High Level 1", "Inner bullish multiplier kept for compatibility", "Indicator");

		_lowLevel1 = Param(nameof(LowLevel1), 0m)
		.SetDisplay("Low Level 1", "Inner bearish multiplier kept for compatibility", "Indicator");

		_lowLevel2 = Param(nameof(LowLevel2), 0m)
		.SetDisplay("Low Level 2", "Outer bearish multiplier kept for compatibility", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetRange(0, 20)
		.SetDisplay("Signal Bar", "Number of closed candles to skip before evaluating signals", "Trading");

		_allowLongEntries = Param(nameof(AllowLongEntries), true)
		.SetDisplay("Allow Long Entries", "Enable opening long positions", "Trading");

		_allowShortEntries = Param(nameof(AllowShortEntries), true)
		.SetDisplay("Allow Short Entries", "Enable opening short positions", "Trading");

		_allowLongExits = Param(nameof(AllowLongExits), true)
		.SetDisplay("Allow Long Exits", "Enable closing long positions on bearish color", "Trading");

		_allowShortExits = Param(nameof(AllowShortExits), true)
		.SetDisplay("Allow Short Exits", "Enable closing short positions on bullish color", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
		.SetDisplay("Stop Loss", "Protective stop distance expressed in price points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
		.SetDisplay("Take Profit", "Profit target distance expressed in price points", "Risk");
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
	/// Volume stream used to weight the oscillator (tick or real volume).
	/// </summary>
	public VolumeModes VolumeSource
	{
		get => _volumeMode.Value;
		set => _volumeMode.Value = value;
	}

	/// <summary>
	/// Smoothing method applied to the base price.
	/// </summary>
	public SmoothingMethods PriceMethod
	{
		get => _priceMethod.Value;
		set => _priceMethod.Value = value;
	}

	/// <summary>
	/// Period for the price smoothing stage.
	/// </summary>
	public int PriceLength
	{
		get => _priceLength.Value;
		set => _priceLength.Value = value;
	}

	/// <summary>
	/// Phase parameter for Jurik-based price smoothing.
	/// </summary>
	public int PricePhase
	{
		get => _pricePhase.Value;
		set => _pricePhase.Value = value;
	}

	/// <summary>
	/// Smoothing method applied to the price range (high-low).
	/// </summary>
	public SmoothingMethods RangeMethod
	{
		get => _rangeMethod.Value;
		set => _rangeMethod.Value = value;
	}

	/// <summary>
	/// Period for the range smoothing stage.
	/// </summary>
	public int RangeLength
	{
		get => _rangeLength.Value;
		set => _rangeLength.Value = value;
	}

	/// <summary>
	/// Phase parameter for Jurik-based range smoothing.
	/// </summary>
	public int RangePhase
	{
		get => _rangePhase.Value;
		set => _rangePhase.Value = value;
	}

	/// <summary>
	/// Length of the final Jurik smoother applied to the volume-weighted oscillator.
	/// </summary>
	public int ResultLength
	{
		get => _resultLength.Value;
		set => _resultLength.Value = value;
	}

	/// <summary>
	/// Applied price processed by the indicator.
	/// </summary>
	public AppliedPrices PriceMode
	{
		get => _appliedPrice.Value;
		set => _appliedPrice.Value = value;
	}

	/// <summary>
	/// Outer bullish multiplier retained for diagnostics.
	/// </summary>
	public decimal HighLevel2
	{
		get => _highLevel2.Value;
		set => _highLevel2.Value = value;
	}

	/// <summary>
	/// Inner bullish multiplier retained for diagnostics.
	/// </summary>
	public decimal HighLevel1
	{
		get => _highLevel1.Value;
		set => _highLevel1.Value = value;
	}

	/// <summary>
	/// Inner bearish multiplier retained for diagnostics.
	/// </summary>
	public decimal LowLevel1
	{
		get => _lowLevel1.Value;
		set => _lowLevel1.Value = value;
	}

	/// <summary>
	/// Outer bearish multiplier retained for diagnostics.
	/// </summary>
	public decimal LowLevel2
	{
		get => _lowLevel2.Value;
		set => _lowLevel2.Value = value;
	}

	/// <summary>
	/// Number of already closed candles to skip before reading the color map.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Enable opening long positions.
	/// </summary>
	public bool AllowLongEntries
	{
		get => _allowLongEntries.Value;
		set => _allowLongEntries.Value = value;
	}

	/// <summary>
	/// Enable opening short positions.
	/// </summary>
	public bool AllowShortEntries
	{
		get => _allowShortEntries.Value;
		set => _allowShortEntries.Value = value;
	}

	/// <summary>
	/// Enable closing long positions when the oscillator turns bearish.
	/// </summary>
	public bool AllowLongExits
	{
		get => _allowLongExits.Value;
		set => _allowLongExits.Value = value;
	}

	/// <summary>
	/// Enable closing short positions when the oscillator turns bullish.
	/// </summary>
	public bool AllowShortExits
	{
		get => _allowShortExits.Value;
		set => _allowShortExits.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price points (multiplied by <see cref="Security.PriceStep"/>).
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_colorHistory.Clear();
		_previousOscillatorValue = null;

		_priceSmoother?.Reset();
		_rangeSmoother?.Reset();
		_resultSmoother?.Reset();
		_volumeSmoother?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceSmoother = CreateSmoother(PriceMethod, PriceLength, PricePhase);
		_rangeSmoother = CreateSmoother(RangeMethod, RangeLength, RangePhase);
		_resultSmoother = CreateJurik(ResultLength, 100);
		_volumeSmoother = CreateJurik(ResultLength, 100);

		_colorHistory.Clear();
		_previousOscillatorValue = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.WhenCandlesFinished(ProcessCandle)
		.Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawOwnTrades(priceArea);
		}

		var indicatorArea = CreateChartArea();
		if (indicatorArea != null)
		{
			DrawIndicator(indicatorArea, _resultSmoother);
		}

		var step = Security?.PriceStep ?? 1m;
		var takeProfit = TakeProfitPoints > 0m ? new Unit(TakeProfitPoints * step, UnitTypes.Point) : null;
		var stopLoss = StopLossPoints > 0m ? new Unit(StopLossPoints * step, UnitTypes.Point) : null;

		StartProtection(takeProfit, stopLoss);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var appliedPrice = GetAppliedPrice(candle, PriceMode);
		var range = candle.HighPrice - candle.LowPrice;
		var volume = GetVolume(candle);

		var priceValue = _priceSmoother.Process(candle.OpenTime, appliedPrice);
		if (!priceValue.IsFinal || priceValue.Value is not decimal smoothedPrice)
		return;

		var rangeValue = _rangeSmoother.Process(candle.OpenTime, range);
		if (!rangeValue.IsFinal || rangeValue.Value is not decimal smoothedRange)
		return;

		if (smoothedRange == 0m)
		{
			smoothedRange = Security?.PriceStep ?? 1m;
			if (smoothedRange == 0m)
			smoothedRange = 1m;
		}

		var dwBand = smoothedPrice - smoothedRange;
		var oscillator = (appliedPrice - dwBand) / (2m * smoothedRange) * 100m - 50m;
		var weightedOscillator = oscillator * volume;

		var oscValue = _resultSmoother.Process(candle.OpenTime, weightedOscillator);
		var volValue = _volumeSmoother.Process(candle.OpenTime, volume);

		if (!oscValue.IsFinal || !volValue.IsFinal)
		return;

		if (oscValue.Value is not decimal smoothedOscillator)
		return;

		var color = CalculateColor(smoothedOscillator);
		AppendColor(color);

		var historyLength = _colorHistory.Count;
		if (historyLength < 2)
		return;

		var offset = Math.Max(0, SignalBar);
		var signalIndex = historyLength - 1 - offset;
		if (signalIndex <= 0)
		return;

		var currentColor = _colorHistory[signalIndex];
		var previousColor = _colorHistory[signalIndex - 1];

		var shouldCloseLong = AllowLongExits && currentColor == 1;
		var shouldCloseShort = AllowShortExits && currentColor == 0;
		var shouldOpenLong = AllowLongEntries && currentColor == 0 && previousColor == 1;
		var shouldOpenShort = AllowShortEntries && currentColor == 1 && previousColor == 0;

		if (shouldCloseLong && Position > 0)
		{
			SellMarket(Position);
		}

		if (shouldCloseShort && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		if (Volume <= 0m)
		return;

		if (shouldOpenLong && Position <= 0)
		{
			var orderVolume = Volume + Math.Abs(Position);
			BuyMarket(orderVolume);
		}
		else if (shouldOpenShort && Position >= 0)
		{
			var orderVolume = Volume + Math.Abs(Position);
			SellMarket(orderVolume);
		}
	}

	private int CalculateColor(decimal smoothedValue)
	{
		int color;

		if (_previousOscillatorValue is null)
		{
			color = 0;
		}
		else if (smoothedValue > _previousOscillatorValue.Value)
		{
			color = 0;
		}
		else if (smoothedValue < _previousOscillatorValue.Value)
		{
			color = 1;
		}
		else
		{
			color = _colorHistory.Count > 0 ? _colorHistory[^1] : 0;
		}

		_previousOscillatorValue = smoothedValue;
		return color;
	}

	private void AppendColor(int color)
	{
		_colorHistory.Add(color);

		var maxSize = Math.Max(50, SignalBar + 10);
		if (_colorHistory.Count > maxSize)
		{
			var remove = _colorHistory.Count - maxSize;
			_colorHistory.RemoveRange(0, remove);
		}
	}

	private decimal GetVolume(ICandleMessage candle)
	{
		return VolumeSource switch
		{
			VolumeModes.Tick => candle.TotalVolume,
			VolumeModes.Real => candle.TotalVolume,
			_ => candle.TotalVolume,
		};
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPrices priceMode)
	{
		return priceMode switch
		{
			AppliedPrices.Close => candle.ClosePrice,
			AppliedPrices.Open => candle.OpenPrice,
			AppliedPrices.High => candle.HighPrice,
			AppliedPrices.Low => candle.LowPrice,
			AppliedPrices.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrices.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPrices.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice * 2m) / 4m,
			AppliedPrices.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
			AppliedPrices.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
			AppliedPrices.TrendFollow0 => candle.ClosePrice > candle.OpenPrice ? candle.HighPrice : candle.ClosePrice < candle.OpenPrice ? candle.LowPrice : candle.ClosePrice,
			AppliedPrices.TrendFollow1 => candle.ClosePrice > candle.OpenPrice ? (candle.HighPrice + candle.ClosePrice) / 2m : candle.ClosePrice < candle.OpenPrice ? (candle.LowPrice + candle.ClosePrice) / 2m : candle.ClosePrice,
			AppliedPrices.Demark => CalculateDemarkPrice(candle),
			_ => candle.ClosePrice,
		};
	}

	private static decimal CalculateDemarkPrice(ICandleMessage candle)
	{
		var sum = candle.HighPrice + candle.LowPrice + candle.ClosePrice;

		if (candle.ClosePrice < candle.OpenPrice)
		sum = (sum + candle.LowPrice) / 2m;
		else if (candle.ClosePrice > candle.OpenPrice)
		sum = (sum + candle.HighPrice) / 2m;
		else
		sum = (sum + candle.ClosePrice) / 2m;

		return ((sum - candle.LowPrice) + (sum - candle.HighPrice)) / 2m;
	}

	private static IIndicator CreateSmoother(SmoothingMethods method, int length, int phase)
	{
		var normalizedLength = Math.Max(1, length);
		var offset = 0.5m + phase / 200m;
		offset = Math.Max(0m, Math.Min(1m, offset));

		return method switch
		{
			SmoothingMethods.Sma => new SimpleMovingAverage { Length = normalizedLength },
			SmoothingMethods.Ema => new ExponentialMovingAverage { Length = normalizedLength },
			SmoothingMethods.Smma => new SmoothedMovingAverage { Length = normalizedLength },
			SmoothingMethods.Lwma => new WeightedMovingAverage { Length = normalizedLength },
			SmoothingMethods.Jjma => CreateJurik(normalizedLength, phase),
			SmoothingMethods.Jurx => new ZeroLagExponentialMovingAverage { Length = normalizedLength },
			SmoothingMethods.Parma => new ArnaudLegouxMovingAverage { Length = normalizedLength, Offset = offset, Sigma = 6m },
			SmoothingMethods.T3 => new TripleExponentialMovingAverage { Length = normalizedLength },
			SmoothingMethods.Vidya => new ExponentialMovingAverage { Length = normalizedLength },
			SmoothingMethods.Ama => new KaufmanAdaptiveMovingAverage { Length = normalizedLength },
			_ => new SimpleMovingAverage { Length = normalizedLength },
		};
	}

	private static IIndicator CreateJurik(int length, int phase)
	{
		var jurik = new JurikMovingAverage { Length = Math.Max(1, length) };
		var property = jurik.GetType().GetProperty("Phase", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (property != null && property.CanWrite)
		{
			var clamped = Math.Max(-100, Math.Min(100, phase));
			property.SetValue(jurik, clamped);
		}

		return jurik;
	}

	public enum SmoothingMethods
	{
		/// <summary>Simple moving average.</summary>
		Sma,

		/// <summary>Exponential moving average.</summary>
		Ema,

		/// <summary>Smoothed moving average.</summary>
		Smma,

		/// <summary>Linear weighted moving average.</summary>
		Lwma,

		/// <summary>Jurik moving average.</summary>
		Jjma,

		/// <summary>Zero-lag exponential moving average (JurX approximation).</summary>
		Jurx,

		/// <summary>Parabolic moving average approximation.</summary>
		Parma,

		/// <summary>Tillson T3 moving average.</summary>
		T3,

		/// <summary>VIDYA approximation using exponential smoothing.</summary>
		Vidya,

		/// <summary>Kaufman adaptive moving average.</summary>
		Ama
	}

	/// <summary>
	/// Applied price options provided by the original indicator.
	/// </summary>
	public enum AppliedPrices
	{
		/// <summary>Close price.</summary>
		Close,

		/// <summary>Open price.</summary>
		Open,

		/// <summary>High price.</summary>
		High,

		/// <summary>Low price.</summary>
		Low,

		/// <summary>Median price (high + low) / 2.</summary>
		Median,

		/// <summary>Typical price (high + low + close) / 3.</summary>
		Typical,

		/// <summary>Weighted price (high + low + close * 2) / 4.</summary>
		Weighted,

		/// <summary>Simple average of open and close.</summary>
		Simple,

		/// <summary>Quarted price (open + high + low + close) / 4.</summary>
		Quarter,

		/// <summary>TrendFollow 0 price.</summary>
		TrendFollow0,

		/// <summary>TrendFollow 1 price.</summary>
		TrendFollow1,

		/// <summary>Demark price.</summary>
		Demark
	}

	/// <summary>
	/// Volume mode used to weight the oscillator.
	/// </summary>
	public enum VolumeModes
	{
		/// <summary>Tick volume.</summary>
		Tick,

		/// <summary>Real (exchange reported) volume.</summary>
		Real
	}
}
