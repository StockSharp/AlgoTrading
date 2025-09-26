using System;
using System.Collections.Generic;
using System.Reflection;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Combined strategy built from Skyscraper Fix, ColorAML and X2MA candle colour filters with MMRec-style position sizing.
/// </summary>
public class ExpSkyscraperFixColorAmlX2MaCandleMmRecStrategy : Strategy
{
	private readonly StrategyParam<DataType> _skyscraperCandleType;
	private readonly StrategyParam<int> _skyscraperLength;
	private readonly StrategyParam<decimal> _skyscraperKv;
	private readonly StrategyParam<decimal> _skyscraperPercentage;
	private readonly StrategyParam<SkyscraperCalculationMode> _skyscraperMode;
	private readonly StrategyParam<int> _skyscraperSignalBar;
	private readonly StrategyParam<bool> _skyscraperEnableLongEntry;
	private readonly StrategyParam<bool> _skyscraperEnableShortEntry;
	private readonly StrategyParam<bool> _skyscraperEnableLongExit;
	private readonly StrategyParam<bool> _skyscraperEnableShortExit;
	private readonly StrategyParam<decimal> _skyscraperNormalVolume;
	private readonly StrategyParam<decimal> _skyscraperReducedVolume;
	private readonly StrategyParam<int> _skyscraperBuyLossTrigger;
	private readonly StrategyParam<int> _skyscraperSellLossTrigger;

	private readonly StrategyParam<DataType> _colorAmlCandleType;
	private readonly StrategyParam<int> _colorAmlFractal;
	private readonly StrategyParam<int> _colorAmlLag;
	private readonly StrategyParam<int> _colorAmlSignalBar;
	private readonly StrategyParam<bool> _colorAmlEnableLongEntry;
	private readonly StrategyParam<bool> _colorAmlEnableShortEntry;
	private readonly StrategyParam<bool> _colorAmlEnableLongExit;
	private readonly StrategyParam<bool> _colorAmlEnableShortExit;
	private readonly StrategyParam<decimal> _colorAmlNormalVolume;
	private readonly StrategyParam<decimal> _colorAmlReducedVolume;
	private readonly StrategyParam<int> _colorAmlBuyLossTrigger;
	private readonly StrategyParam<int> _colorAmlSellLossTrigger;

	private readonly StrategyParam<DataType> _x2MaCandleType;
	private readonly StrategyParam<X2MaSmoothMethod> _x2MaFirstMethod;
	private readonly StrategyParam<int> _x2MaFirstLength;
	private readonly StrategyParam<int> _x2MaFirstPhase;
	private readonly StrategyParam<X2MaSmoothMethod> _x2MaSecondMethod;
	private readonly StrategyParam<int> _x2MaSecondLength;
	private readonly StrategyParam<int> _x2MaSecondPhase;
	private readonly StrategyParam<int> _x2MaGapPoints;
	private readonly StrategyParam<int> _x2MaSignalBar;
	private readonly StrategyParam<bool> _x2MaEnableLongEntry;
	private readonly StrategyParam<bool> _x2MaEnableShortEntry;
	private readonly StrategyParam<bool> _x2MaEnableLongExit;
	private readonly StrategyParam<bool> _x2MaEnableShortExit;
	private readonly StrategyParam<decimal> _x2MaNormalVolume;
	private readonly StrategyParam<decimal> _x2MaReducedVolume;
	private readonly StrategyParam<int> _x2MaBuyLossTrigger;
	private readonly StrategyParam<int> _x2MaSellLossTrigger;

	private SkyscraperFixIndicator _skyscraperIndicator;
	private ColorAmlIndicator _colorAmlIndicator;
	private X2MaCandleColorIndicator _x2MaIndicator;

	private readonly Queue<SkyscraperSignal> _skyscraperSignals = new();
	private readonly List<int> _colorAmlColors = new();
	private readonly List<int> _x2MaColors = new();

	private readonly ModuleState _skyscraperState = new();
	private readonly ModuleState _colorAmlState = new();
	private readonly ModuleState _x2MaState = new();

	private DateTimeOffset? _skyscraperLastLongSignal;
	private DateTimeOffset? _skyscraperLastShortSignal;
	private DateTimeOffset? _colorAmlLastLongSignal;
	private DateTimeOffset? _colorAmlLastShortSignal;
	private DateTimeOffset? _x2MaLastLongSignal;
	private DateTimeOffset? _x2MaLastShortSignal;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpSkyscraperFixColorAmlX2MaCandleMmRecStrategy"/> class.
	/// </summary>
	public ExpSkyscraperFixColorAmlX2MaCandleMmRecStrategy()
	{
		_skyscraperCandleType = Param(nameof(SkyscraperCandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Skyscraper Candle", "Timeframe for the Skyscraper Fix block", "Skyscraper");
		_skyscraperLength = Param(nameof(SkyscraperLength), 10)
		.SetGreaterThanZero()
		.SetDisplay("Skyscraper Length", "ATR lookback length", "Skyscraper");
		_skyscraperKv = Param(nameof(SkyscraperKv), 0.9m)
		.SetGreaterThanZero()
		.SetDisplay("Skyscraper Kv", "ATR step multiplier", "Skyscraper");
		_skyscraperPercentage = Param(nameof(SkyscraperPercentage), 0m)
		.SetDisplay("Skyscraper Percentage", "Midline percentage offset", "Skyscraper");
		_skyscraperMode = Param(nameof(SkyscraperMode), SkyscraperCalculationMode.HighLow)
		.SetDisplay("Skyscraper Mode", "Price source used for envelopes", "Skyscraper");
		_skyscraperSignalBar = Param(nameof(SkyscraperSignalBar), 1)
		.SetNotNegative()
		.SetDisplay("Skyscraper Signal Bar", "Delay in closed candles", "Skyscraper");
		_skyscraperEnableLongEntry = Param(nameof(SkyscraperEnableLongEntry), true)
		.SetDisplay("Skyscraper Buy", "Allow long entries", "Skyscraper");
		_skyscraperEnableShortEntry = Param(nameof(SkyscraperEnableShortEntry), true)
		.SetDisplay("Skyscraper Sell", "Allow short entries", "Skyscraper");
		_skyscraperEnableLongExit = Param(nameof(SkyscraperEnableLongExit), true)
		.SetDisplay("Skyscraper Close Long", "Allow long exits", "Skyscraper");
		_skyscraperEnableShortExit = Param(nameof(SkyscraperEnableShortExit), true)
		.SetDisplay("Skyscraper Close Short", "Allow short exits", "Skyscraper");
		_skyscraperNormalVolume = Param(nameof(SkyscraperNormalVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Skyscraper Normal Volume", "Default order volume", "Skyscraper");
		_skyscraperReducedVolume = Param(nameof(SkyscraperReducedVolume), 0.01m)
		.SetNotNegative()
		.SetDisplay("Skyscraper Reduced Volume", "Fallback volume after losses", "Skyscraper");
		_skyscraperBuyLossTrigger = Param(nameof(SkyscraperBuyLossTrigger), 2)
		.SetNotNegative()
		.SetDisplay("Skyscraper Buy Loss Trigger", "Losing trades before reducing long volume", "Skyscraper");
		_skyscraperSellLossTrigger = Param(nameof(SkyscraperSellLossTrigger), 2)
		.SetNotNegative()
		.SetDisplay("Skyscraper Sell Loss Trigger", "Losing trades before reducing short volume", "Skyscraper");

		_colorAmlCandleType = Param(nameof(ColorAmlCandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("ColorAML Candle", "Timeframe for ColorAML", "ColorAML");
		_colorAmlFractal = Param(nameof(ColorAmlFractal), 6)
		.SetGreaterThanZero()
		.SetDisplay("ColorAML Fractal", "Fractal window length", "ColorAML");
		_colorAmlLag = Param(nameof(ColorAmlLag), 7)
		.SetGreaterThanZero()
		.SetDisplay("ColorAML Lag", "Adaptive smoothing lag", "ColorAML");
		_colorAmlSignalBar = Param(nameof(ColorAmlSignalBar), 1)
		.SetNotNegative()
		.SetDisplay("ColorAML Signal Bar", "Bar offset for colour evaluation", "ColorAML");
		_colorAmlEnableLongEntry = Param(nameof(ColorAmlEnableLongEntry), true)
		.SetDisplay("ColorAML Buy", "Allow ColorAML long entries", "ColorAML");
		_colorAmlEnableShortEntry = Param(nameof(ColorAmlEnableShortEntry), true)
		.SetDisplay("ColorAML Sell", "Allow ColorAML short entries", "ColorAML");
		_colorAmlEnableLongExit = Param(nameof(ColorAmlEnableLongExit), true)
		.SetDisplay("ColorAML Close Long", "Allow ColorAML to exit longs", "ColorAML");
		_colorAmlEnableShortExit = Param(nameof(ColorAmlEnableShortExit), true)
		.SetDisplay("ColorAML Close Short", "Allow ColorAML to exit shorts", "ColorAML");
		_colorAmlNormalVolume = Param(nameof(ColorAmlNormalVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("ColorAML Normal Volume", "Default ColorAML volume", "ColorAML");
		_colorAmlReducedVolume = Param(nameof(ColorAmlReducedVolume), 0.01m)
		.SetNotNegative()
		.SetDisplay("ColorAML Reduced Volume", "Reduced ColorAML volume", "ColorAML");
		_colorAmlBuyLossTrigger = Param(nameof(ColorAmlBuyLossTrigger), 2)
		.SetNotNegative()
		.SetDisplay("ColorAML Buy Loss Trigger", "Losses required to reduce long volume", "ColorAML");
		_colorAmlSellLossTrigger = Param(nameof(ColorAmlSellLossTrigger), 2)
		.SetNotNegative()
		.SetDisplay("ColorAML Sell Loss Trigger", "Losses required to reduce short volume", "ColorAML");

		_x2MaCandleType = Param(nameof(X2MaCandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("X2MA Candle", "Timeframe for X2MA candles", "X2MA");
		_x2MaFirstMethod = Param(nameof(X2MaFirstMethod), X2MaSmoothMethod.Simple)
		.SetDisplay("First Method", "First smoothing method", "X2MA");
		_x2MaFirstLength = Param(nameof(X2MaFirstLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("First Length", "Length of the first smoothing stage", "X2MA");
		_x2MaFirstPhase = Param(nameof(X2MaFirstPhase), 15)
		.SetDisplay("First Phase", "Compatibility phase for Jurik", "X2MA");
		_x2MaSecondMethod = Param(nameof(X2MaSecondMethod), X2MaSmoothMethod.Jurik)
		.SetDisplay("Second Method", "Second smoothing method", "X2MA");
		_x2MaSecondLength = Param(nameof(X2MaSecondLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("Second Length", "Length of the second smoothing stage", "X2MA");
		_x2MaSecondPhase = Param(nameof(X2MaSecondPhase), 15)
		.SetDisplay("Second Phase", "Compatibility phase for the second stage", "X2MA");
		_x2MaGapPoints = Param(nameof(X2MaGapPoints), 10)
		.SetNotNegative()
		.SetDisplay("Gap Points", "Gap threshold in price steps", "X2MA");
		_x2MaSignalBar = Param(nameof(X2MaSignalBar), 1)
		.SetNotNegative()
		.SetDisplay("X2MA Signal Bar", "Bar offset before acting on colour", "X2MA");
		_x2MaEnableLongEntry = Param(nameof(X2MaEnableLongEntry), true)
		.SetDisplay("X2MA Buy", "Allow X2MA long entries", "X2MA");
		_x2MaEnableShortEntry = Param(nameof(X2MaEnableShortEntry), true)
		.SetDisplay("X2MA Sell", "Allow X2MA short entries", "X2MA");
		_x2MaEnableLongExit = Param(nameof(X2MaEnableLongExit), true)
		.SetDisplay("X2MA Close Long", "Allow X2MA to exit longs", "X2MA");
		_x2MaEnableShortExit = Param(nameof(X2MaEnableShortExit), true)
		.SetDisplay("X2MA Close Short", "Allow X2MA to exit shorts", "X2MA");
		_x2MaNormalVolume = Param(nameof(X2MaNormalVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("X2MA Normal Volume", "Default X2MA order volume", "X2MA");
		_x2MaReducedVolume = Param(nameof(X2MaReducedVolume), 0.01m)
		.SetNotNegative()
		.SetDisplay("X2MA Reduced Volume", "Reduced X2MA order volume", "X2MA");
		_x2MaBuyLossTrigger = Param(nameof(X2MaBuyLossTrigger), 2)
		.SetNotNegative()
		.SetDisplay("X2MA Buy Loss Trigger", "Losses required to reduce long volume", "X2MA");
		_x2MaSellLossTrigger = Param(nameof(X2MaSellLossTrigger), 2)
		.SetNotNegative()
		.SetDisplay("X2MA Sell Loss Trigger", "Losses required to reduce short volume", "X2MA");
	}

	/// <summary>Timeframe for the Skyscraper Fix block.</summary>
	public DataType SkyscraperCandleType
	{
		get => _skyscraperCandleType.Value;
		set => _skyscraperCandleType.Value = value;
	}

	/// <summary>ATR window length for Skyscraper Fix.</summary>
	public int SkyscraperLength
	{
		get => _skyscraperLength.Value;
		set => _skyscraperLength.Value = value;
	}

	/// <summary>ATR multiplier for Skyscraper Fix.</summary>
	public decimal SkyscraperKv
	{
		get => _skyscraperKv.Value;
		set => _skyscraperKv.Value = value;
	}

	/// <summary>Percentage offset for the Skyscraper midline.</summary>
	public decimal SkyscraperPercentage
	{
		get => _skyscraperPercentage.Value;
		set => _skyscraperPercentage.Value = value;
	}

	/// <summary>Price calculation mode used by Skyscraper Fix.</summary>
	public SkyscraperCalculationMode SkyscraperMode
	{
		get => _skyscraperMode.Value;
		set => _skyscraperMode.Value = value;
	}

	/// <summary>Bar delay for the Skyscraper signals.</summary>
	public int SkyscraperSignalBar
	{
		get => _skyscraperSignalBar.Value;
		set => _skyscraperSignalBar.Value = value;
	}

	/// <summary>Enable long entries generated by Skyscraper Fix.</summary>
	public bool SkyscraperEnableLongEntry
	{
		get => _skyscraperEnableLongEntry.Value;
		set => _skyscraperEnableLongEntry.Value = value;
	}

	/// <summary>Enable short entries generated by Skyscraper Fix.</summary>
	public bool SkyscraperEnableShortEntry
	{
		get => _skyscraperEnableShortEntry.Value;
		set => _skyscraperEnableShortEntry.Value = value;
	}

	/// <summary>Allow Skyscraper Fix to close long positions.</summary>
	public bool SkyscraperEnableLongExit
	{
		get => _skyscraperEnableLongExit.Value;
		set => _skyscraperEnableLongExit.Value = value;
	}

	/// <summary>Allow Skyscraper Fix to close short positions.</summary>
	public bool SkyscraperEnableShortExit
	{
		get => _skyscraperEnableShortExit.Value;
		set => _skyscraperEnableShortExit.Value = value;
	}

	/// <summary>Default volume for Skyscraper trades.</summary>
	public decimal SkyscraperNormalVolume
	{
		get => _skyscraperNormalVolume.Value;
		set => _skyscraperNormalVolume.Value = value;
	}

	/// <summary>Reduced volume used after Skyscraper losses.</summary>
	public decimal SkyscraperReducedVolume
	{
		get => _skyscraperReducedVolume.Value;
		set => _skyscraperReducedVolume.Value = value;
	}

	/// <summary>Number of consecutive long losses before Skyscraper reduces volume.</summary>
	public int SkyscraperBuyLossTrigger
	{
		get => _skyscraperBuyLossTrigger.Value;
		set => _skyscraperBuyLossTrigger.Value = value;
	}

	/// <summary>Number of consecutive short losses before Skyscraper reduces volume.</summary>
	public int SkyscraperSellLossTrigger
	{
		get => _skyscraperSellLossTrigger.Value;
		set => _skyscraperSellLossTrigger.Value = value;
	}

	/// <summary>Candle type for the ColorAML indicator.</summary>
	public DataType ColorAmlCandleType
	{
		get => _colorAmlCandleType.Value;
		set => _colorAmlCandleType.Value = value;
	}

	/// <summary>Fractal window length for ColorAML.</summary>
	public int ColorAmlFractal
	{
		get => _colorAmlFractal.Value;
		set => _colorAmlFractal.Value = value;
	}

	/// <summary>Lag parameter for ColorAML smoothing.</summary>
	public int ColorAmlLag
	{
		get => _colorAmlLag.Value;
		set => _colorAmlLag.Value = value;
	}

	/// <summary>Bar offset applied before reading ColorAML colours.</summary>
	public int ColorAmlSignalBar
	{
		get => _colorAmlSignalBar.Value;
		set => _colorAmlSignalBar.Value = value;
	}

	/// <summary>Enable long entries from the ColorAML block.</summary>
	public bool ColorAmlEnableLongEntry
	{
		get => _colorAmlEnableLongEntry.Value;
		set => _colorAmlEnableLongEntry.Value = value;
	}

	/// <summary>Enable short entries from the ColorAML block.</summary>
	public bool ColorAmlEnableShortEntry
	{
		get => _colorAmlEnableShortEntry.Value;
		set => _colorAmlEnableShortEntry.Value = value;
	}

	/// <summary>Allow ColorAML to close long positions.</summary>
	public bool ColorAmlEnableLongExit
	{
		get => _colorAmlEnableLongExit.Value;
		set => _colorAmlEnableLongExit.Value = value;
	}

	/// <summary>Allow ColorAML to close short positions.</summary>
	public bool ColorAmlEnableShortExit
	{
		get => _colorAmlEnableShortExit.Value;
		set => _colorAmlEnableShortExit.Value = value;
	}

	/// <summary>Default ColorAML trade volume.</summary>
	public decimal ColorAmlNormalVolume
	{
		get => _colorAmlNormalVolume.Value;
		set => _colorAmlNormalVolume.Value = value;
	}

	/// <summary>Reduced ColorAML trade volume after losses.</summary>
	public decimal ColorAmlReducedVolume
	{
		get => _colorAmlReducedVolume.Value;
		set => _colorAmlReducedVolume.Value = value;
	}

	/// <summary>Consecutive long losses that switch ColorAML to the reduced volume.</summary>
	public int ColorAmlBuyLossTrigger
	{
		get => _colorAmlBuyLossTrigger.Value;
		set => _colorAmlBuyLossTrigger.Value = value;
	}

	/// <summary>Consecutive short losses that switch ColorAML to the reduced volume.</summary>
	public int ColorAmlSellLossTrigger
	{
		get => _colorAmlSellLossTrigger.Value;
		set => _colorAmlSellLossTrigger.Value = value;
	}

	/// <summary>Timeframe used by the X2MA candle reconstruction.</summary>
	public DataType X2MaCandleType
	{
		get => _x2MaCandleType.Value;
		set => _x2MaCandleType.Value = value;
	}

	/// <summary>First smoothing method used by X2MA.</summary>
	public X2MaSmoothMethod X2MaFirstMethod
	{
		get => _x2MaFirstMethod.Value;
		set => _x2MaFirstMethod.Value = value;
	}

	/// <summary>Length of the first smoothing stage.</summary>
	public int X2MaFirstLength
	{
		get => _x2MaFirstLength.Value;
		set => _x2MaFirstLength.Value = value;
	}

	/// <summary>Phase parameter for the first smoothing stage.</summary>
	public int X2MaFirstPhase
	{
		get => _x2MaFirstPhase.Value;
		set => _x2MaFirstPhase.Value = value;
	}

	/// <summary>Second smoothing method used by X2MA.</summary>
	public X2MaSmoothMethod X2MaSecondMethod
	{
		get => _x2MaSecondMethod.Value;
		set => _x2MaSecondMethod.Value = value;
	}

	/// <summary>Length of the second smoothing stage.</summary>
	public int X2MaSecondLength
	{
		get => _x2MaSecondLength.Value;
		set => _x2MaSecondLength.Value = value;
	}

	/// <summary>Phase parameter for the second smoothing stage.</summary>
	public int X2MaSecondPhase
	{
		get => _x2MaSecondPhase.Value;
		set => _x2MaSecondPhase.Value = value;
	}

	/// <summary>Gap threshold expressed in price steps.</summary>
	public int X2MaGapPoints
	{
		get => _x2MaGapPoints.Value;
		set => _x2MaGapPoints.Value = value;
	}

	/// <summary>Bar offset applied before acting on X2MA colours.</summary>
	public int X2MaSignalBar
	{
		get => _x2MaSignalBar.Value;
		set => _x2MaSignalBar.Value = value;
	}

	/// <summary>Enable long entries from the X2MA block.</summary>
	public bool X2MaEnableLongEntry
	{
		get => _x2MaEnableLongEntry.Value;
		set => _x2MaEnableLongEntry.Value = value;
	}

	/// <summary>Enable short entries from the X2MA block.</summary>
	public bool X2MaEnableShortEntry
	{
		get => _x2MaEnableShortEntry.Value;
		set => _x2MaEnableShortEntry.Value = value;
	}

	/// <summary>Allow X2MA to close long positions.</summary>
	public bool X2MaEnableLongExit
	{
		get => _x2MaEnableLongExit.Value;
		set => _x2MaEnableLongExit.Value = value;
	}

	/// <summary>Allow X2MA to close short positions.</summary>
	public bool X2MaEnableShortExit
	{
		get => _x2MaEnableShortExit.Value;
		set => _x2MaEnableShortExit.Value = value;
	}

	/// <summary>Default X2MA trade volume.</summary>
	public decimal X2MaNormalVolume
	{
		get => _x2MaNormalVolume.Value;
		set => _x2MaNormalVolume.Value = value;
	}

	/// <summary>Reduced X2MA trade volume after losses.</summary>
	public decimal X2MaReducedVolume
	{
		get => _x2MaReducedVolume.Value;
		set => _x2MaReducedVolume.Value = value;
	}

	/// <summary>Consecutive long losses required to reduce X2MA volume.</summary>
	public int X2MaBuyLossTrigger
	{
		get => _x2MaBuyLossTrigger.Value;
		set => _x2MaBuyLossTrigger.Value = value;
	}

	/// <summary>Consecutive short losses required to reduce X2MA volume.</summary>
	public int X2MaSellLossTrigger
	{
		get => _x2MaSellLossTrigger.Value;
		set => _x2MaSellLossTrigger.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_skyscraperIndicator = null;
		_colorAmlIndicator = null;
		_x2MaIndicator = null;
		_skyscraperSignals.Clear();
		_colorAmlColors.Clear();
		_x2MaColors.Clear();
		_skyscraperState.Reset();
		_colorAmlState.Reset();
		_x2MaState.Reset();
		_skyscraperLastLongSignal = null;
		_skyscraperLastShortSignal = null;
		_colorAmlLastLongSignal = null;
		_colorAmlLastShortSignal = null;
		_x2MaLastLongSignal = null;
		_x2MaLastShortSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var priceStep = Security?.PriceStep ?? 0m;

		_skyscraperIndicator = new SkyscraperFixIndicator
		{
			Length = SkyscraperLength,
			Kv = SkyscraperKv,
			Percentage = SkyscraperPercentage,
			Mode = SkyscraperMode,
			PriceStep = priceStep
		};

		_colorAmlIndicator = new ColorAmlIndicator
		{
			Fractal = ColorAmlFractal,
			Lag = ColorAmlLag,
			PriceStep = priceStep
		};

		var gapValue = priceStep * X2MaGapPoints;

		_x2MaIndicator = new X2MaCandleColorIndicator(
			X2MaFirstMethod,
			X2MaFirstLength,
			X2MaFirstPhase,
			X2MaSecondMethod,
			X2MaSecondLength,
			X2MaSecondPhase,
			gapValue);

			_skyscraperState.Configure(SkyscraperNormalVolume, SkyscraperReducedVolume, SkyscraperBuyLossTrigger, SkyscraperSellLossTrigger);
			_colorAmlState.Configure(ColorAmlNormalVolume, ColorAmlReducedVolume, ColorAmlBuyLossTrigger, ColorAmlSellLossTrigger);
			_x2MaState.Configure(X2MaNormalVolume, X2MaReducedVolume, X2MaBuyLossTrigger, X2MaSellLossTrigger);

			var skyscraperSubscription = SubscribeCandles(SkyscraperCandleType);
			skyscraperSubscription
			.BindEx(_skyscraperIndicator, ProcessSkyscraper)
			.Start();

			var colorAmlSubscription = SubscribeCandles(ColorAmlCandleType);
			colorAmlSubscription
			.Bind(_colorAmlIndicator, ProcessColorAml)
			.Start();

			var x2MaSubscription = SubscribeCandles(X2MaCandleType);
			x2MaSubscription
			.Bind(_x2MaIndicator, ProcessX2Ma)
			.Start();
		}

		private void ProcessSkyscraper(ICandleMessage candle, IIndicatorValue indicatorValue)
		{
			if (candle.State != CandleStates.Finished)
			return;

			if (indicatorValue is not SkyscraperFixValue value || !value.HasValue)
			return;

			var signal = new SkyscraperSignal(
			value.Buy.HasValue,
			value.Lower.HasValue,
			value.Sell.HasValue,
			value.Upper.HasValue);

			_skyscraperSignals.Enqueue(signal);

			if (_skyscraperSignals.Count <= SkyscraperSignalBar)
			return;

			var target = _skyscraperSignals.Dequeue();

			HandleModuleSignals(
				_skyscraperState,
				SkyscraperEnableLongEntry,
				SkyscraperEnableLongExit,
				SkyscraperEnableShortEntry,
				SkyscraperEnableShortExit,
				target.OpenLong,
				target.CloseLong,
				target.OpenShort,
				target.CloseShort,
				candle.ClosePrice,
				GetSignalTime(candle, SkyscraperCandleType),
				ref _skyscraperLastLongSignal,
				ref _skyscraperLastShortSignal);
			}

			private void ProcessColorAml(ICandleMessage candle, decimal indicatorColor)
			{
				if (candle.State != CandleStates.Finished)
				return;

				var color = (int)Math.Round(indicatorColor);
				_colorAmlColors.Add(color);

				var keep = Math.Max(ColorAmlSignalBar + 3, 10);
				if (_colorAmlColors.Count > keep)
				_colorAmlColors.RemoveAt(0);

				var currentIndex = _colorAmlColors.Count - 1 - ColorAmlSignalBar;
				var previousIndex = currentIndex - 1;

				if (currentIndex < 0 || previousIndex < 0)
				return;

				var currentColor = _colorAmlColors[currentIndex];
				var previousColor = _colorAmlColors[previousIndex];

				var openLong = currentColor == 2 && previousColor != 2;
				var closeLong = currentColor == 0;
				var openShort = currentColor == 0 && previousColor != 0;
				var closeShort = currentColor == 2;

				HandleModuleSignals(
					_colorAmlState,
					ColorAmlEnableLongEntry,
					ColorAmlEnableLongExit,
					ColorAmlEnableShortEntry,
					ColorAmlEnableShortExit,
					openLong,
					closeLong,
					openShort,
					closeShort,
					candle.ClosePrice,
					GetSignalTime(candle, ColorAmlCandleType),
					ref _colorAmlLastLongSignal,
					ref _colorAmlLastShortSignal);
				}

				private void ProcessX2Ma(ICandleMessage candle, decimal indicatorColor)
				{
					if (candle.State != CandleStates.Finished)
					return;

					var color = (int)Math.Round(indicatorColor);
					_x2MaColors.Add(color);

					var keep = Math.Max(X2MaSignalBar + 3, 10);
					if (_x2MaColors.Count > keep)
					_x2MaColors.RemoveAt(0);

					var currentIndex = _x2MaColors.Count - 1 - X2MaSignalBar;
					var previousIndex = currentIndex - 1;

					if (currentIndex < 0 || previousIndex < 0)
					return;

					var currentColor = _x2MaColors[currentIndex];
					var previousColor = _x2MaColors[previousIndex];

					var openLong = currentColor == 2 && previousColor != 2;
					var closeLong = currentColor == 0;
					var openShort = currentColor == 0 && previousColor != 0;
					var closeShort = currentColor == 2;

					HandleModuleSignals(
						_x2MaState,
						X2MaEnableLongEntry,
						X2MaEnableLongExit,
						X2MaEnableShortEntry,
						X2MaEnableShortExit,
						openLong,
						closeLong,
						openShort,
						closeShort,
						candle.ClosePrice,
						GetSignalTime(candle, X2MaCandleType),
						ref _x2MaLastLongSignal,
						ref _x2MaLastShortSignal);
					}

					private void HandleModuleSignals(
						ModuleState state,
						bool allowLongEntry,
						bool allowLongExit,
						bool allowShortEntry,
						bool allowShortExit,
						bool openLong,
						bool closeLong,
						bool openShort,
						bool closeShort,
						decimal price,
						DateTimeOffset signalTime,
						ref DateTimeOffset? lastLongSignal,
						ref DateTimeOffset? lastShortSignal)
						{
							if (!IsFormedAndOnlineAndAllowTrading())
							return;

							if (allowLongExit && closeLong && Position > 0)
							{
								var volume = Position;
								if (volume > 0m)
								{
									SellMarket(volume);
									state.RegisterExit(true, price);
									lastLongSignal = signalTime;
								}
							}

							if (allowShortExit && closeShort && Position < 0)
							{
								var volume = Math.Abs(Position);
								if (volume > 0m)
								{
									BuyMarket(volume);
									state.RegisterExit(false, price);
									lastShortSignal = signalTime;
								}
							}

							if (allowLongEntry && openLong && Position <= 0 && lastLongSignal != signalTime)
							{
								if (Position < 0)
								{
									var cover = Math.Abs(Position);
									if (cover > 0m)
									{
										BuyMarket(cover);
										state.RegisterExit(false, price);
									}
								}

								var volume = state.GetVolume(true);
								if (volume > 0m)
								{
									BuyMarket(volume);
									state.RegisterEntry(true, price);
									lastLongSignal = signalTime;
								}
							}

							if (allowShortEntry && openShort && Position >= 0 && lastShortSignal != signalTime)
							{
								if (Position > 0)
								{
									var exitVolume = Position;
									if (exitVolume > 0m)
									{
										SellMarket(exitVolume);
										state.RegisterExit(true, price);
									}
								}

								var volume = state.GetVolume(false);
								if (volume > 0m)
								{
									SellMarket(volume);
									state.RegisterEntry(false, price);
									lastShortSignal = signalTime;
								}
							}
						}

						private static DateTimeOffset GetSignalTime(ICandleMessage candle, DataType candleType)
						{
							var timeFrame = candleType.Arg is TimeSpan span ? span : TimeSpan.Zero;
							return candle.CloseTime ?? candle.OpenTime + timeFrame;
						}

						private readonly record struct SkyscraperSignal(bool OpenLong, bool CloseLong, bool OpenShort, bool CloseShort);

						private sealed class ModuleState
						{
							private readonly Queue<bool> _longLosses = new();
							private readonly Queue<bool> _shortLosses = new();

							public decimal NormalVolume { get; private set; }
							public decimal ReducedVolume { get; private set; }
							public int LongLossTrigger { get; private set; }
							public int ShortLossTrigger { get; private set; }
							public decimal? LongEntryPrice { get; private set; }
							public decimal? ShortEntryPrice { get; private set; }

							public void Configure(decimal normalVolume, decimal reducedVolume, int longTrigger, int shortTrigger)
							{
								NormalVolume = normalVolume;
								ReducedVolume = reducedVolume;
								LongLossTrigger = Math.Max(0, longTrigger);
								ShortLossTrigger = Math.Max(0, shortTrigger);
							}

							public void Reset()
							{
								_longLosses.Clear();
								_shortLosses.Clear();
								LongEntryPrice = null;
								ShortEntryPrice = null;
							}

							public decimal GetVolume(bool isLong)
							{
								var normal = NormalVolume;
								if (normal <= 0m)
								return 0m;

								var trigger = isLong ? LongLossTrigger : ShortLossTrigger;
								if (trigger <= 0)
								return normal;

								var queue = isLong ? _longLosses : _shortLosses;
								if (queue.Count < trigger)
								return normal;

								foreach (var loss in queue)
								{
									if (!loss)
									return normal;
								}

								var reduced = ReducedVolume > 0m ? ReducedVolume : normal;
								return reduced;
							}

							public void RegisterEntry(bool isLong, decimal price)
							{
								if (isLong)
								LongEntryPrice = price;
								else
								ShortEntryPrice = price;
							}

							public void RegisterExit(bool isLong, decimal price)
							{
								if (isLong)
								{
									if (LongEntryPrice is decimal entry)
									RecordTrade(true, entry, price);

									LongEntryPrice = null;
								}
								else
								{
									if (ShortEntryPrice is decimal entry)
									RecordTrade(false, entry, price);

									ShortEntryPrice = null;
								}
							}

							private void RecordTrade(bool isLong, decimal entry, decimal exit)
							{
								var trigger = isLong ? LongLossTrigger : ShortLossTrigger;
								if (trigger <= 0)
								return;

								var isLoss = isLong ? exit < entry : exit > entry;
								var queue = isLong ? _longLosses : _shortLosses;

								queue.Enqueue(isLoss);

								while (queue.Count > trigger)
								queue.Dequeue();
							}
						}

						/// <summary>ColorAML indicator clone that outputs colour states.</summary>
						private sealed class ColorAmlIndicator : BaseIndicator<decimal>
						{
							private readonly List<decimal> _highs = new();
							private readonly List<decimal> _lows = new();
							private readonly List<decimal> _smooth = new();
							private decimal? _previousAml;
							private decimal _previousColor = 1m;

							public int Fractal { get; set; } = 6;
							public int Lag { get; set; } = 7;
							public decimal PriceStep { get; set; }

							protected override IIndicatorValue OnProcess(IIndicatorValue input)
							{
								if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
								return new DecimalIndicatorValue(this, default, input.Time);

								_highs.Add(candle.HighPrice);
								_lows.Add(candle.LowPrice);

								var fractal = Math.Max(1, Fractal);
								var lag = Math.Max(1, Lag);

								if (_highs.Count < Math.Max(fractal * 2, fractal + lag))
								{
									_previousAml = candle.ClosePrice;
									_previousColor = 1m;
									return new DecimalIndicatorValue(this, 1m, input.Time);
								}

								var price = (candle.HighPrice + candle.LowPrice + 2m * candle.OpenPrice + 2m * candle.ClosePrice) / 6m;
								var prevSmooth = _smooth.Count > 0 ? _smooth[^1] : price;

								var r1 = GetRange(_highs, _lows, _highs.Count - fractal, fractal) / fractal;
								var r2 = GetRange(_highs, _lows, _highs.Count - 2 * fractal, fractal) / fractal;
								var r3 = GetRange(_highs, _lows, _highs.Count - 2 * fractal, 2 * fractal) / (2m * fractal);

								double dim = 0;
								if (r1 + r2 > 0m && r3 > 0m)
								dim = (Math.Log((double)(r1 + r2)) - Math.Log((double)r3)) * 1.44269504088896;

								var alpha = Math.Exp(-lag * (dim - 1.0));
								alpha = Math.Min(alpha, 1.0);
								alpha = Math.Max(alpha, 0.01);
								var alphaDecimal = (decimal)alpha;

								var smooth = alphaDecimal * price + (1m - alphaDecimal) * prevSmooth;

								_smooth.Add(smooth);
								if (_smooth.Count > lag + 1)
								_smooth.RemoveAt(0);

								var referenceIndex = _smooth.Count - 1 - lag;
								var reference = referenceIndex >= 0 ? _smooth[referenceIndex] : smooth;

								var threshold = lag * lag * (PriceStep > 0m ? PriceStep : 0m);
								var aml = threshold <= 0m || Math.Abs(smooth - reference) >= threshold ? smooth : _previousAml ?? smooth;

								var color = _previousColor;
								if (_previousAml is decimal previous)
								{
									if (aml > previous)
									color = 2m;
									else if (aml < previous)
									color = 0m;
								}
								else
								{
									color = 1m;
								}

								_previousAml = aml;
								_previousColor = color;

								return new DecimalIndicatorValue(this, color, input.Time);
							}

							public override void Reset()
							{
								base.Reset();
								_highs.Clear();
								_lows.Clear();
								_smooth.Clear();
								_previousAml = null;
								_previousColor = 1m;
							}

							private static decimal GetRange(IReadOnlyList<decimal> highs, IReadOnlyList<decimal> lows, int start, int length)
							{
								var end = start + length;
								var max = decimal.MinValue;
								var min = decimal.MaxValue;

								for (var i = start; i < end; i++)
								{
									var high = highs[i];
									var low = lows[i];

									if (high > max)
									max = high;

									if (low < min)
									min = low;
								}

								return max - min;
							}
						}

						/// <summary>X2MA candle colour indicator with two-stage smoothing.</summary>
						private sealed class X2MaCandleColorIndicator : LengthIndicator<decimal>
						{
							private readonly MovingAveragePipeline _open;
							private readonly MovingAveragePipeline _high;
							private readonly MovingAveragePipeline _low;
							private readonly MovingAveragePipeline _close;
							private readonly decimal _gap;
							private decimal? _previousClose;

							public X2MaCandleColorIndicator(
							X2MaSmoothMethod firstMethod,
							int firstLength,
							int firstPhase,
							X2MaSmoothMethod secondMethod,
							int secondLength,
							int secondPhase,
							decimal gap)
							{
								_open = new MovingAveragePipeline(firstMethod, firstLength, firstPhase, secondMethod, secondLength, secondPhase);
								_high = new MovingAveragePipeline(firstMethod, firstLength, firstPhase, secondMethod, secondLength, secondPhase);
								_low = new MovingAveragePipeline(firstMethod, firstLength, firstPhase, secondMethod, secondLength, secondPhase);
								_close = new MovingAveragePipeline(firstMethod, firstLength, firstPhase, secondMethod, secondLength, secondPhase);
								_gap = gap;
							}

							protected override IIndicatorValue OnProcess(IIndicatorValue input)
							{
								if (!input.IsFinal)
								return new DecimalIndicatorValue(this, default, input.Time);

								var candle = input.GetValue<ICandleMessage>();
								var time = input.Time ?? candle.CloseTime ?? candle.OpenTime;

								var open = _open.Process(candle.OpenPrice, time);
								var close = _close.Process(candle.ClosePrice, time);
								var high = _high.Process(candle.HighPrice, time);
								var low = _low.Process(candle.LowPrice, time);

								if (open is null || close is null || high is null || low is null)
								return new DecimalIndicatorValue(this, default, input.Time);

								var smoothedOpen = open.Value;
								var smoothedClose = close.Value;

								if (_previousClose is null)
								_previousClose = smoothedClose;

								if (Math.Abs(smoothedOpen - smoothedClose) <= _gap && _previousClose is decimal prevClose)
								smoothedOpen = prevClose;

								_previousClose = smoothedClose;

								var color = smoothedOpen < smoothedClose ? 2m : smoothedOpen > smoothedClose ? 0m : 1m;
								return new DecimalIndicatorValue(this, color, input.Time);
							}

							private sealed class MovingAveragePipeline
							{
								private readonly IIndicator _first;
								private readonly IIndicator _second;

								public MovingAveragePipeline(
								X2MaSmoothMethod firstMethod,
								int firstLength,
								int firstPhase,
								X2MaSmoothMethod secondMethod,
								int secondLength,
								int secondPhase)
								{
									_first = CreateMovingAverage(firstMethod, firstLength, firstPhase);
									_second = CreateMovingAverage(secondMethod, secondLength, secondPhase);
								}

								public decimal? Process(decimal value, DateTimeOffset time)
								{
									var firstValue = _first.Process(value, time);
									if (!firstValue.IsFinal)
									return null;

									var intermediate = firstValue.ToDecimal();
									var secondValue = _second.Process(intermediate, time);
									return secondValue.IsFinal ? secondValue.ToDecimal() : null;
								}

								private static IIndicator CreateMovingAverage(X2MaSmoothMethod method, int length, int phase)
								{
									return method switch
									{
										X2MaSmoothMethod.Simple => new SimpleMovingAverage { Length = Math.Max(1, length) },
										X2MaSmoothMethod.Exponential => new ExponentialMovingAverage { Length = Math.Max(1, length) },
										X2MaSmoothMethod.Smoothed => new SmoothedMovingAverage { Length = Math.Max(1, length) },
										X2MaSmoothMethod.Weighted => new WeightedMovingAverage { Length = Math.Max(1, length) },
										X2MaSmoothMethod.Jurik => CreateJurik(length, phase),
										_ => new SimpleMovingAverage { Length = Math.Max(1, length) }
									};
								}

								private static IIndicator CreateJurik(int length, int phase)
								{
									var jurik = new JurikMovingAverage { Length = Math.Max(1, length) };
									var property = jurik.GetType().GetProperty("Phase", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
									if (property != null)
									{
										var value = Math.Max(-100, Math.Min(100, phase));
										property.SetValue(jurik, value);
									}

									return jurik;
								}
							}
						}
					}

					/// <summary>Supported smoothing methods for the X2MA candle indicator.</summary>
					public enum X2MaSmoothMethod
					{
						/// <summary>Simple moving average.</summary>
						Simple,

						/// <summary>Exponential moving average.</summary>
						Exponential,

						/// <summary>Smoothed moving average (RMA).</summary>
						Smoothed,

						/// <summary>Linear weighted moving average.</summary>
						Weighted,

						/// <summary>Jurik moving average.</summary>
						Jurik
					}
