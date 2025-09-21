using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the MT5 expert advisor "Exp_Skyscraper_Fix_ColorAML".
/// The strategy merges the Skyscraper Fix trend detector with the ColorAML adaptive level module.
/// </summary>
public class ExpSkyscraperFixColorAmlStrategy : Strategy
{
	private const int SkyscraperAtrPeriod = 15;
	private const int MaxHistory = 512;

	private readonly StrategyParam<DataType> _skyscraperCandleType;
	private readonly StrategyParam<bool> _skyscraperEnableLongEntry;
	private readonly StrategyParam<bool> _skyscraperEnableShortEntry;
	private readonly StrategyParam<bool> _skyscraperEnableLongExit;
	private readonly StrategyParam<bool> _skyscraperEnableShortExit;
	private readonly StrategyParam<int> _skyscraperLength;
	private readonly StrategyParam<decimal> _skyscraperMultiplier;
	private readonly StrategyParam<decimal> _skyscraperPercentage;
	private readonly StrategyParam<SkyscraperMethod> _skyscraperMode;
	private readonly StrategyParam<int> _skyscraperSignalBar;
	private readonly StrategyParam<decimal> _skyscraperVolume;
	private readonly StrategyParam<decimal> _skyscraperStopLoss;
	private readonly StrategyParam<decimal> _skyscraperTakeProfit;

	private readonly StrategyParam<DataType> _colorAmlCandleType;
	private readonly StrategyParam<bool> _colorAmlEnableLongEntry;
	private readonly StrategyParam<bool> _colorAmlEnableShortEntry;
	private readonly StrategyParam<bool> _colorAmlEnableLongExit;
	private readonly StrategyParam<bool> _colorAmlEnableShortExit;
	private readonly StrategyParam<int> _colorAmlFractal;
	private readonly StrategyParam<int> _colorAmlLag;
	private readonly StrategyParam<int> _colorAmlSignalBar;
	private readonly StrategyParam<decimal> _colorAmlVolume;
	private readonly StrategyParam<decimal> _colorAmlStopLoss;
	private readonly StrategyParam<decimal> _colorAmlTakeProfit;

	private readonly List<CandleSnapshot> _skyscraperCandles = new();
	private readonly List<CandleSnapshot> _colorAmlCandles = new();

	private ModuleSource? _activeModule;
	private decimal? _entryPrice;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpSkyscraperFixColorAmlStrategy"/> class.
	/// </summary>
	public ExpSkyscraperFixColorAmlStrategy()
	{
	_skyscraperCandleType = Param(nameof(SkyscraperCandleType), TimeSpan.FromHours(4).TimeFrame())
	.SetDisplay("Skyscraper Timeframe", "Timeframe used for the Skyscraper Fix module", "Skyscraper");

	_skyscraperEnableLongEntry = Param(nameof(SkyscraperEnableLongEntry), true)
	.SetDisplay("Skyscraper Long Entry", "Allow Skyscraper Fix to open long positions", "Skyscraper");

	_skyscraperEnableShortEntry = Param(nameof(SkyscraperEnableShortEntry), true)
	.SetDisplay("Skyscraper Short Entry", "Allow Skyscraper Fix to open short positions", "Skyscraper");

	_skyscraperEnableLongExit = Param(nameof(SkyscraperEnableLongExit), true)
	.SetDisplay("Skyscraper Long Exit", "Allow Skyscraper Fix to close existing longs", "Skyscraper");

	_skyscraperEnableShortExit = Param(nameof(SkyscraperEnableShortExit), true)
	.SetDisplay("Skyscraper Short Exit", "Allow Skyscraper Fix to close existing shorts", "Skyscraper");

	_skyscraperLength = Param(nameof(SkyscraperLength), 10)
	.SetGreaterThanZero()
	.SetDisplay("ATR Sample Length", "Number of ATR samples used for the Skyscraper step", "Skyscraper")
	.SetCanOptimize(true)
	.SetOptimize(5, 20, 1);

	_skyscraperMultiplier = Param(nameof(SkyscraperMultiplier), 0.9m)
	.SetNotNegative()
	.SetDisplay("Step Multiplier", "Coefficient applied to the ATR based step", "Skyscraper")
	.SetCanOptimize(true)
	.SetOptimize(0.3m, 1.5m, 0.1m);

	_skyscraperPercentage = Param(nameof(SkyscraperPercentage), 0m)
	.SetNotNegative()
	.SetDisplay("Percentage Offset", "Optional percentage displacement of the middle line", "Skyscraper");

	_skyscraperMode = Param(nameof(SkyscraperMode), SkyscraperMethod.HighLow)
	.SetDisplay("Boundary Source", "Defines whether the module uses High/Low or Close prices", "Skyscraper");

	_skyscraperSignalBar = Param(nameof(SkyscraperSignalBar), 1)
	.SetGreaterOrEqual(1)
	.SetDisplay("Signal Bar", "Number of completed candles to look back for Skyscraper signals", "Skyscraper");

	_skyscraperVolume = Param(nameof(SkyscraperVolume), 1m)
	.SetGreaterThanZero()
	.SetDisplay("Skyscraper Volume", "Order volume for Skyscraper Fix entries", "Skyscraper");

	_skyscraperStopLoss = Param(nameof(SkyscraperStopLoss), 1000m)
	.SetNotNegative()
	.SetDisplay("Skyscraper Stop Loss (pts)", "Protective stop distance in price steps", "Skyscraper");

	_skyscraperTakeProfit = Param(nameof(SkyscraperTakeProfit), 2000m)
	.SetNotNegative()
	.SetDisplay("Skyscraper Take Profit (pts)", "Profit target distance in price steps", "Skyscraper");

	_colorAmlCandleType = Param(nameof(ColorAmlCandleType), TimeSpan.FromHours(4).TimeFrame())
	.SetDisplay("ColorAML Timeframe", "Timeframe used for the ColorAML module", "ColorAML");

	_colorAmlEnableLongEntry = Param(nameof(ColorAmlEnableLongEntry), true)
	.SetDisplay("ColorAML Long Entry", "Allow ColorAML to open long positions", "ColorAML");

	_colorAmlEnableShortEntry = Param(nameof(ColorAmlEnableShortEntry), true)
	.SetDisplay("ColorAML Short Entry", "Allow ColorAML to open short positions", "ColorAML");

	_colorAmlEnableLongExit = Param(nameof(ColorAmlEnableLongExit), true)
	.SetDisplay("ColorAML Long Exit", "Allow ColorAML to close existing longs", "ColorAML");

	_colorAmlEnableShortExit = Param(nameof(ColorAmlEnableShortExit), true)
	.SetDisplay("ColorAML Short Exit", "Allow ColorAML to close existing shorts", "ColorAML");

	_colorAmlFractal = Param(nameof(ColorAmlFractal), 6)
	.SetGreaterThanZero()
	.SetDisplay("Fractal Length", "Fractal window for range calculations", "ColorAML")
	.SetCanOptimize(true)
	.SetOptimize(3, 12, 1);

	_colorAmlLag = Param(nameof(ColorAmlLag), 7)
	.SetGreaterThanZero()
	.SetDisplay("Lag", "Lag parameter controlling the adaptive smoothing", "ColorAML")
	.SetCanOptimize(true)
	.SetOptimize(3, 14, 1);

	_colorAmlSignalBar = Param(nameof(ColorAmlSignalBar), 1)
	.SetGreaterOrEqual(1)
	.SetDisplay("Signal Bar", "Number of completed candles to look back for ColorAML", "ColorAML");

	_colorAmlVolume = Param(nameof(ColorAmlVolume), 1m)
	.SetGreaterThanZero()
	.SetDisplay("ColorAML Volume", "Order volume for ColorAML entries", "ColorAML");

	_colorAmlStopLoss = Param(nameof(ColorAmlStopLoss), 1000m)
	.SetNotNegative()
	.SetDisplay("ColorAML Stop Loss (pts)", "Protective stop distance in price steps", "ColorAML");

	_colorAmlTakeProfit = Param(nameof(ColorAmlTakeProfit), 2000m)
	.SetNotNegative()
	.SetDisplay("ColorAML Take Profit (pts)", "Profit target distance in price steps", "ColorAML");
	}

	/// <summary>
	/// Candle type for the Skyscraper Fix module.
	/// </summary>
	public DataType SkyscraperCandleType
	{
	get => _skyscraperCandleType.Value;
	set => _skyscraperCandleType.Value = value;
	}

	/// <summary>
	/// Enable long entries for the Skyscraper Fix module.
	/// </summary>
	public bool SkyscraperEnableLongEntry
	{
	get => _skyscraperEnableLongEntry.Value;
	set => _skyscraperEnableLongEntry.Value = value;
	}

	/// <summary>
	/// Enable short entries for the Skyscraper Fix module.
	/// </summary>
	public bool SkyscraperEnableShortEntry
	{
	get => _skyscraperEnableShortEntry.Value;
	set => _skyscraperEnableShortEntry.Value = value;
	}

	/// <summary>
	/// Allow Skyscraper Fix to close existing long positions.
	/// </summary>
	public bool SkyscraperEnableLongExit
	{
	get => _skyscraperEnableLongExit.Value;
	set => _skyscraperEnableLongExit.Value = value;
	}

	/// <summary>
	/// Allow Skyscraper Fix to close existing short positions.
	/// </summary>
	public bool SkyscraperEnableShortExit
	{
	get => _skyscraperEnableShortExit.Value;
	set => _skyscraperEnableShortExit.Value = value;
	}

	/// <summary>
	/// ATR sampling length used inside Skyscraper Fix.
	/// </summary>
	public int SkyscraperLength
	{
	get => _skyscraperLength.Value;
	set => _skyscraperLength.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the ATR based step.
	/// </summary>
	public decimal SkyscraperMultiplier
	{
	get => _skyscraperMultiplier.Value;
	set => _skyscraperMultiplier.Value = value;
	}

	/// <summary>
	/// Optional percentage displacement of the Skyscraper middle line.
	/// </summary>
	public decimal SkyscraperPercentage
	{
	get => _skyscraperPercentage.Value;
	set => _skyscraperPercentage.Value = value;
	}

	/// <summary>
	/// Source mode for Skyscraper boundaries.
	/// </summary>
	public SkyscraperMethod SkyscraperMode
	{
	get => _skyscraperMode.Value;
	set => _skyscraperMode.Value = value;
	}

	/// <summary>
	/// Number of completed candles to look back for Skyscraper signals.
	/// </summary>
	public int SkyscraperSignalBar
	{
	get => _skyscraperSignalBar.Value;
	set => _skyscraperSignalBar.Value = value;
	}

	/// <summary>
	/// Volume used for Skyscraper Fix entries.
	/// </summary>
	public decimal SkyscraperVolume
	{
	get => _skyscraperVolume.Value;
	set => _skyscraperVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance for Skyscraper signals expressed in price steps.
	/// </summary>
	public decimal SkyscraperStopLoss
	{
	get => _skyscraperStopLoss.Value;
	set => _skyscraperStopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance for Skyscraper signals expressed in price steps.
	/// </summary>
	public decimal SkyscraperTakeProfit
	{
	get => _skyscraperTakeProfit.Value;
	set => _skyscraperTakeProfit.Value = value;
	}

	/// <summary>
	/// Candle type for the ColorAML module.
	/// </summary>
	public DataType ColorAmlCandleType
	{
	get => _colorAmlCandleType.Value;
	set => _colorAmlCandleType.Value = value;
	}

	/// <summary>
	/// Enable long entries for the ColorAML module.
	/// </summary>
	public bool ColorAmlEnableLongEntry
	{
	get => _colorAmlEnableLongEntry.Value;
	set => _colorAmlEnableLongEntry.Value = value;
	}

	/// <summary>
	/// Enable short entries for the ColorAML module.
	/// </summary>
	public bool ColorAmlEnableShortEntry
	{
	get => _colorAmlEnableShortEntry.Value;
	set => _colorAmlEnableShortEntry.Value = value;
	}

	/// <summary>
	/// Allow ColorAML to close existing long positions.
	/// </summary>
	public bool ColorAmlEnableLongExit
	{
	get => _colorAmlEnableLongExit.Value;
	set => _colorAmlEnableLongExit.Value = value;
	}

	/// <summary>
	/// Allow ColorAML to close existing short positions.
	/// </summary>
	public bool ColorAmlEnableShortExit
	{
	get => _colorAmlEnableShortExit.Value;
	set => _colorAmlEnableShortExit.Value = value;
	}

	/// <summary>
	/// Fractal window length used by ColorAML.
	/// </summary>
	public int ColorAmlFractal
	{
	get => _colorAmlFractal.Value;
	set => _colorAmlFractal.Value = value;
	}

	/// <summary>
	/// Lag parameter for ColorAML adaptive smoothing.
	/// </summary>
	public int ColorAmlLag
	{
	get => _colorAmlLag.Value;
	set => _colorAmlLag.Value = value;
	}

	/// <summary>
	/// Number of completed candles to look back for ColorAML signals.
	/// </summary>
	public int ColorAmlSignalBar
	{
	get => _colorAmlSignalBar.Value;
	set => _colorAmlSignalBar.Value = value;
	}

	/// <summary>
	/// Volume used for ColorAML entries.
	/// </summary>
	public decimal ColorAmlVolume
	{
	get => _colorAmlVolume.Value;
	set => _colorAmlVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance for ColorAML signals expressed in price steps.
	/// </summary>
	public decimal ColorAmlStopLoss
	{
	get => _colorAmlStopLoss.Value;
	set => _colorAmlStopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance for ColorAML signals expressed in price steps.
	/// </summary>
	public decimal ColorAmlTakeProfit
	{
	get => _colorAmlTakeProfit.Value;
	set => _colorAmlTakeProfit.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	StartProtection();

	var skyscraperSubscription = SubscribeCandles(SkyscraperCandleType);
	skyscraperSubscription
	.Bind(ProcessSkyscraperCandle)
	.Start();

	var colorAmlSubscription = SubscribeCandles(ColorAmlCandleType);
	colorAmlSubscription
	.Bind(ProcessColorAmlCandle)
	.Start();

	var chart = CreateChartArea();
	if (chart != null)
	{
	DrawCandles(chart, skyscraperSubscription);
	DrawOwnTrades(chart);
	}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
	base.OnReseted();

	_skyscraperCandles.Clear();
	_colorAmlCandles.Clear();
	ResetPositionState();
	}

	private void ProcessSkyscraperCandle(ICandleMessage candle)
	{
	if (candle.State != CandleStates.Finished)
	return;

	AddCandle(_skyscraperCandles, candle);

	var colors = SkyscraperCalculator.CalculateColors(_skyscraperCandles, GetPriceStep(), SkyscraperLength, SkyscraperMultiplier, SkyscraperPercentage, SkyscraperMode);
	if (colors.Length <= SkyscraperSignalBar)
	{
	CheckRisk(candle);
	return;
	}

	var current = colors[SkyscraperSignalBar];
	if (current is not int currentColor)
	{
	CheckRisk(candle);
	return;
	}

	var previous = colors.Length > SkyscraperSignalBar + 1 ? colors[SkyscraperSignalBar + 1] : null;

	var openLong = currentColor == 0 && SkyscraperEnableLongEntry && previous != 0;
	var openShort = currentColor == 1 && SkyscraperEnableShortEntry && previous != 1;
	var closeLong = currentColor == 1 && SkyscraperEnableLongExit;
	var closeShort = currentColor == 0 && SkyscraperEnableShortExit;

	ExecuteSignals(ModuleSource.Skyscraper, candle, openLong, closeLong, openShort, closeShort, SkyscraperVolume, SkyscraperStopLoss, SkyscraperTakeProfit);
	CheckRisk(candle);
	}

	private void ProcessColorAmlCandle(ICandleMessage candle)
	{
	if (candle.State != CandleStates.Finished)
	return;

	AddCandle(_colorAmlCandles, candle);

	var colors = ColorAmlCalculator.CalculateColors(_colorAmlCandles, ColorAmlFractal, ColorAmlLag, GetPriceStep());
	if (colors.Length <= ColorAmlSignalBar)
	{
	CheckRisk(candle);
	return;
	}

	var current = colors[ColorAmlSignalBar];
	if (current is not int currentColor)
	{
	CheckRisk(candle);
	return;
	}

	var previous = colors.Length > ColorAmlSignalBar + 1 ? colors[ColorAmlSignalBar + 1] : null;

	var openLong = currentColor == 2 && ColorAmlEnableLongEntry && previous != 2;
	var openShort = currentColor == 0 && ColorAmlEnableShortEntry && previous != 0;
	var closeLong = currentColor == 0 && ColorAmlEnableLongExit;
	var closeShort = currentColor == 2 && ColorAmlEnableShortExit;

	ExecuteSignals(ModuleSource.ColorAml, candle, openLong, closeLong, openShort, closeShort, ColorAmlVolume, ColorAmlStopLoss, ColorAmlTakeProfit);
	CheckRisk(candle);
	}

	private void ExecuteSignals(ModuleSource module, ICandleMessage candle, bool openLong, bool closeLong, bool openShort, bool closeShort, decimal volume, decimal stopLossPoints, decimal takeProfitPoints)
	{
	if (volume <= 0m)
	return;

	var needCloseLongOnly = closeLong && !openShort;
	var needCloseShortOnly = closeShort && !openLong;

	if (needCloseLongOnly && Position > 0m)
	{
	SellMarket(Position);
	ResetPositionState();
	}

	if (needCloseShortOnly && Position < 0m)
	{
	BuyMarket(-Position);
	ResetPositionState();
	}

	if (openLong)
	{
	var requiredVolume = volume;
	if (Position < 0m)
	requiredVolume += -Position;

	if (requiredVolume > 0m)
	{
	BuyMarket(requiredVolume);
	SetEntryState(module, candle.ClosePrice, stopLossPoints, takeProfitPoints, TradeDirection.Long);
	}
	}

	if (openShort)
	{
	var requiredVolume = volume;
	if (Position > 0m)
	requiredVolume += Position;

	if (requiredVolume > 0m)
	{
	SellMarket(requiredVolume);
	SetEntryState(module, candle.ClosePrice, stopLossPoints, takeProfitPoints, TradeDirection.Short);
	}
	}
	}

	private void CheckRisk(ICandleMessage candle)
	{
	if (_entryPrice is null)
	return;

	if (Position > 0m)
	{
	if (_stopLossPrice is decimal stop && candle.LowPrice <= stop)
	{
	SellMarket(Position);
	ResetPositionState();
	return;
	}

	if (_takeProfitPrice is decimal take && candle.HighPrice >= take)
	{
	SellMarket(Position);
	ResetPositionState();
	}
	}
	else if (Position < 0m)
	{
	if (_stopLossPrice is decimal stop && candle.HighPrice >= stop)
	{
	BuyMarket(-Position);
	ResetPositionState();
	return;
	}

	if (_takeProfitPrice is decimal take && candle.LowPrice <= take)
	{
	BuyMarket(-Position);
	ResetPositionState();
	}
	}
	}

	private void SetEntryState(ModuleSource module, decimal entryPrice, decimal stopLossPoints, decimal takeProfitPoints, TradeDirection direction)
	{
	_activeModule = module;
	_entryPrice = entryPrice;

	var step = GetPriceStep();

	switch (direction)
	{
	case TradeDirection.Long:
	_stopLossPrice = stopLossPoints > 0m ? entryPrice - stopLossPoints * step : null;
	_takeProfitPrice = takeProfitPoints > 0m ? entryPrice + takeProfitPoints * step : null;
	break;
	case TradeDirection.Short:
	_stopLossPrice = stopLossPoints > 0m ? entryPrice + stopLossPoints * step : null;
	_takeProfitPrice = takeProfitPoints > 0m ? entryPrice - takeProfitPoints * step : null;
	break;
	default:
	throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown trade direction.");
	}
	}

	private void ResetPositionState()
	{
	_activeModule = null;
	_entryPrice = null;
	_stopLossPrice = null;
	_takeProfitPrice = null;
	}

	private void AddCandle(List<CandleSnapshot> container, ICandleMessage candle)
	{
	container.Add(new CandleSnapshot(candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice));

	if (container.Count > MaxHistory)
	container.RemoveAt(0);
	}

	private decimal GetPriceStep()
	{
	var step = Security?.PriceStep;
	return step is > 0m ? step.Value : 0.0001m;
	}

	private enum ModuleSource
	{
	Skyscraper,
	ColorAml
	}

	private enum TradeDirection
	{
	Long,
	Short
	}

	/// <summary>
	/// Price source selection for Skyscraper Fix.
	/// </summary>
	public enum SkyscraperMethod
	{
	HighLow,
	Close
	}

	private readonly record struct CandleSnapshot(decimal Open, decimal High, decimal Low, decimal Close);

	private static class SkyscraperCalculator
	{
	public static int?[] CalculateColors(IReadOnlyList<CandleSnapshot> candles, decimal point, int length, decimal multiplier, decimal percentage, SkyscraperMethod method)
	{
	if (candles.Count == 0)
	return Array.Empty<int?>();

	point = point > 0m ? point : 0.0001m;

	var atrChrono = CalculateAtrSeries(candles, SkyscraperAtrPeriod);
	var count = candles.Count;

	var closeSeries = new decimal[count];
	var highSeries = new decimal[count];
	var lowSeries = new decimal[count];
	var atrSeries = new decimal?[count];

	for (var chrono = 0; chrono < count; chrono++)
	{
	var seriesIndex = count - 1 - chrono;
	var candle = candles[chrono];
	closeSeries[seriesIndex] = candle.Close;
	highSeries[seriesIndex] = candle.High;
	lowSeries[seriesIndex] = candle.Low;
	atrSeries[seriesIndex] = atrChrono[chrono];
	}

	var minRates = SkyscraperAtrPeriod + length + 1;
	if (count < minRates)
	return new int?[count];

	var colors = new int?[count];
	var start = count - minRates;
	var smin1 = closeSeries[start];
	var smax1 = closeSeries[start];
	var trend1 = 0;
	var lineBuffer = new decimal?[count];

	for (var bar = start; bar >= 0; bar--)
	{
	var trend0 = trend1;

	if (!TryGetAtrWindow(atrSeries, bar, length, out var atrMax, out var atrMin))
	{
	lineBuffer[bar] = bar + 1 < count ? lineBuffer[bar + 1] : null;
	colors[bar] = bar + 1 < count ? colors[bar + 1] : null;
	continue;
	}

	var stepTicks = (int)Math.Truncate((double)(0.5m * multiplier * (atrMax + atrMin) / point));
	if (stepTicks <= 0)
	{
	lineBuffer[bar] = bar + 1 < count ? lineBuffer[bar + 1] : null;
	colors[bar] = bar + 1 < count ? colors[bar + 1] : null;
	continue;
	}

	var xStep = stepTicks * point;
	var x2Step = 2m * xStep;

	decimal smax0;
	decimal smin0;
	if (method == SkyscraperMethod.HighLow)
	{
	smax0 = lowSeries[bar] + x2Step;
	smin0 = highSeries[bar] - x2Step;
	}
	else
	{
	smax0 = closeSeries[bar] + x2Step;
	smin0 = closeSeries[bar] - x2Step;
	}

	var close = closeSeries[bar];
	if (close > smax1)
	trend0 = 1;
	if (close < smin1)
	trend0 = -1;

	var adjust = percentage / 100m * stepTicks * point;
	var prevLine = bar + 1 < count ? lineBuffer[bar + 1] : null;

	if (trend0 > 0)
	{
	smin0 = Math.Max(smin0, smin1);
	var lineCandidate = smin0 + stepTicks * point;
	var candidate = lineCandidate - adjust;
	lineBuffer[bar] = prevLine.HasValue ? Math.Max(candidate, prevLine.Value) : candidate;
	colors[bar] = 0;
	}
	else
	{
	smax0 = Math.Min(smax0, smax1);
	var lineCandidate = smax0 - stepTicks * point;
	var candidate = lineCandidate + adjust;
	lineBuffer[bar] = prevLine.HasValue ? Math.Min(candidate, prevLine.Value) : candidate;
	colors[bar] = 1;
	}

	if (bar > 0)
	{
	smin1 = smin0;
	smax1 = smax0;
	trend1 = trend0;
	}
	}

	return colors;
	}

	private static bool TryGetAtrWindow(decimal?[] atrSeries, int start, int length, out decimal atrMax, out decimal atrMin)
	{
	atrMax = decimal.MinValue;
	atrMin = decimal.MaxValue;

	for (var index = start; index < start + length; index++)
	{
	if (index >= atrSeries.Length)
	return false;

	if (atrSeries[index] is not decimal value)
	return false;

	if (value > atrMax)
	atrMax = value;
	if (value < atrMin)
	atrMin = value;
	}

	return true;
	}

	private static List<decimal?> CalculateAtrSeries(IReadOnlyList<CandleSnapshot> candles, int period)
	{
	var atrValues = new List<decimal?>(candles.Count);

	if (candles.Count == 0)
	return atrValues;

	var trValues = new List<decimal>(candles.Count);
	decimal? prevClose = null;

	foreach (var candle in candles)
	{
	decimal tr;
	if (prevClose is decimal previous)
	{
	var range1 = candle.High - candle.Low;
	var range2 = Math.Abs(candle.High - previous);
	var range3 = Math.Abs(candle.Low - previous);
	tr = Math.Max(range1, Math.Max(range2, range3));
	}
	else
	{
	tr = candle.High - candle.Low;
	}

	trValues.Add(tr);
	prevClose = candle.Close;
	}

	decimal? prevAtr = null;
	for (var i = 0; i < trValues.Count; i++)
	{
	if (i + 1 < period)
	{
	atrValues.Add(null);
	continue;
	}

	if (i + 1 == period)
	{
	var sum = 0m;
	for (var j = i + 1 - period; j <= i; j++)
	sum += trValues[j];

	prevAtr = sum / period;
	atrValues.Add(prevAtr);
	continue;
	}

	prevAtr = ((prevAtr ?? 0m) * (period - 1) + trValues[i]) / period;
	atrValues.Add(prevAtr);
	}

	return atrValues;
	}
	}

	private static class ColorAmlCalculator
	{
	public static int?[] CalculateColors(IReadOnlyList<CandleSnapshot> candles, int fractal, int lag, decimal point)
	{
	if (candles.Count == 0)
	return Array.Empty<int?>();

	point = point > 0m ? point : 0.0001m;

	var count = candles.Count;
	var openSeries = new decimal[count];
	var highSeries = new decimal[count];
	var lowSeries = new decimal[count];
	var closeSeries = new decimal[count];

	for (var chrono = 0; chrono < count; chrono++)
	{
	var index = count - 1 - chrono;
	var candle = candles[chrono];
	openSeries[index] = candle.Open;
	highSeries[index] = candle.High;
	lowSeries[index] = candle.Low;
	closeSeries[index] = candle.Close;
	}

	var minRates = fractal + lag;
	var limit = count - minRates - 1;
	if (limit < 0)
	return new int?[count];

	var colors = new int?[count];
	var amlBuffer = new decimal?[count];
	var smoothValues = Enumerable.Repeat(0m, lag + 1).ToList();
	colors[limit] = 1;

	for (var bar = limit; bar >= 0; bar--)
	{
	var r1 = Range(fractal, bar, highSeries, lowSeries) / fractal;
	var r2 = Range(fractal, bar + fractal, highSeries, lowSeries) / fractal;
	var r3 = Range(2 * fractal, bar, highSeries, lowSeries) / (2m * fractal);

	decimal dim = 0m;
	if (r1 + r2 > 0m && r3 > 0m)
	{
	dim = (decimal)((Math.Log((double)(r1 + r2)) - Math.Log((double)r3)) * 1.44269504088896);
	}

	var alphaRaw = Math.Exp((double)(-lag * (dim - 1m)));
	var alpha = (decimal)Math.Min(Math.Max(alphaRaw, 0.01), 1.0);

	var price = (highSeries[bar] + lowSeries[bar] + 2m * openSeries[bar] + 2m * closeSeries[bar]) / 6m;
	var prevSmooth = smoothValues.Count > 1 ? smoothValues[1] : smoothValues[0];
	var newSmooth = alpha * price + (1m - alpha) * prevSmooth;

	smoothValues.Insert(0, newSmooth);
	if (smoothValues.Count > lag + 1)
	smoothValues.RemoveAt(smoothValues.Count - 1);

	var smoothLag = smoothValues.Count > lag ? smoothValues[lag] : smoothValues[^1];

	var prevAml = bar + 1 < amlBuffer.Length ? amlBuffer[bar + 1] : null;
	var threshold = lag * lag * point;
	var aml = Math.Abs(newSmooth - smoothLag) >= threshold ? newSmooth : prevAml ?? newSmooth;
	amlBuffer[bar] = aml;

	var prevColor = bar + 1 < colors.Length ? colors[bar + 1] : 1;
	var color = prevColor;

	if (prevAml.HasValue)
	{
	if (aml > prevAml.Value)
	color = 2;
	else if (aml < prevAml.Value)
	color = 0;
	}

	colors[bar] = color;
	}

	return colors;
	}

	private static decimal Range(int period, int start, decimal[] high, decimal[] low)
	{
	if (period <= 0)
	return 0m;

	var end = start + period;
	var max = decimal.MinValue;
	var min = decimal.MaxValue;

	for (var index = start; index < end; index++)
	{
	if (index >= high.Length)
	return 0m;

	var h = high[index];
	var l = low[index];

	if (h > max)
	max = h;
	if (l < min)
	min = l;
	}

	return max - min;
	}
	}
}
