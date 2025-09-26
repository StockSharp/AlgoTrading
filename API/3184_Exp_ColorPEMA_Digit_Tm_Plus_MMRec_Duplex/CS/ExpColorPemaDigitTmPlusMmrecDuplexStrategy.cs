using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that mimics the dual Color PEMA expert advisor with time based exits and money management.
/// Uses two independent Pentuple EMA streams to generate long and short signals.
/// </summary>
public class ExpColorPemaDigitTmPlusMmrecDuplexStrategy : Strategy
{
private readonly StrategyParam<DataType> _longCandleType;
private readonly StrategyParam<DataType> _shortCandleType;
private readonly StrategyParam<decimal> _longEmaLength;
private readonly StrategyParam<decimal> _shortEmaLength;
private readonly StrategyParam<AppliedPrice> _longPriceMode;
private readonly StrategyParam<AppliedPrice> _shortPriceMode;
private readonly StrategyParam<int> _longDigits;
private readonly StrategyParam<int> _shortDigits;
private readonly StrategyParam<int> _longSignalBar;
private readonly StrategyParam<int> _shortSignalBar;
private readonly StrategyParam<bool> _longAllowOpen;
private readonly StrategyParam<bool> _shortAllowOpen;
private readonly StrategyParam<bool> _longAllowClose;
private readonly StrategyParam<bool> _shortAllowClose;
private readonly StrategyParam<bool> _longAllowTimeExit;
private readonly StrategyParam<bool> _shortAllowTimeExit;
private readonly StrategyParam<int> _longTimeExitMinutes;
private readonly StrategyParam<int> _shortTimeExitMinutes;
private readonly StrategyParam<decimal> _tradeVolume;
private readonly StrategyParam<decimal> _stopLossSteps;
private readonly StrategyParam<decimal> _takeProfitSteps;

private PentupleExponentialMovingAverageIndicator _longPema;
private PentupleExponentialMovingAverageIndicator _shortPema;
private readonly List<int> _longColors = new();
private readonly List<int> _shortColors = new();
private DateTimeOffset? _longEntryTime;
private DateTimeOffset? _shortEntryTime;

/// <summary>
/// Initializes a new instance of the <see cref="ExpColorPemaDigitTmPlusMmrecDuplexStrategy"/> class.
/// </summary>
public ExpColorPemaDigitTmPlusMmrecDuplexStrategy()
{
_longCandleType = Param(nameof(LongCandleType), TimeSpan.FromHours(4).TimeFrame())
.SetDisplay("Long Candle Type", "Timeframe for long side", "General");

_shortCandleType = Param(nameof(ShortCandleType), TimeSpan.FromHours(4).TimeFrame())
.SetDisplay("Short Candle Type", "Timeframe for short side", "General");

_longEmaLength = Param(nameof(LongEmaLength), 50.01m)
.SetGreaterThanZero()
.SetDisplay("Long EMA Length", "Length of long side pentuple EMA", "Indicators")
.SetCanOptimize(true)
.SetOptimize(20m, 100m, 5m);

_shortEmaLength = Param(nameof(ShortEmaLength), 50.01m)
.SetGreaterThanZero()
.SetDisplay("Short EMA Length", "Length of short side pentuple EMA", "Indicators")
.SetCanOptimize(true)
.SetOptimize(20m, 100m, 5m);

_longPriceMode = Param(nameof(LongPriceMode), AppliedPrice.Close)
.SetDisplay("Long Price Mode", "Price source for long pentuple EMA", "Indicators");

_shortPriceMode = Param(nameof(ShortPriceMode), AppliedPrice.Close)
.SetDisplay("Short Price Mode", "Price source for short pentuple EMA", "Indicators");

_longDigits = Param(nameof(LongDigits), 2)
.SetDisplay("Long Rounding Digits", "Decimal rounding applied to long EMA", "Indicators");

_shortDigits = Param(nameof(ShortDigits), 2)
.SetDisplay("Short Rounding Digits", "Decimal rounding applied to short EMA", "Indicators");

_longSignalBar = Param(nameof(LongSignalBar), 1)
.SetDisplay("Long Signal Bar", "Bars back used for long signal", "Signals")
.SetCanOptimize(true)
.SetOptimize(1, 3, 1);

_shortSignalBar = Param(nameof(ShortSignalBar), 1)
.SetDisplay("Short Signal Bar", "Bars back used for short signal", "Signals")
.SetCanOptimize(true)
.SetOptimize(1, 3, 1);

_longAllowOpen = Param(nameof(LongAllowOpen), true)
.SetDisplay("Long Allow Open", "Enable long entries", "Signals");

_shortAllowOpen = Param(nameof(ShortAllowOpen), true)
.SetDisplay("Short Allow Open", "Enable short entries", "Signals");

_longAllowClose = Param(nameof(LongAllowClose), true)
.SetDisplay("Long Allow Close", "Enable long exits from indicator", "Signals");

_shortAllowClose = Param(nameof(ShortAllowClose), true)
.SetDisplay("Short Allow Close", "Enable short exits from indicator", "Signals");

_longAllowTimeExit = Param(nameof(LongAllowTimeExit), true)
.SetDisplay("Long Allow Time Exit", "Enable time based long exit", "Risk");

_shortAllowTimeExit = Param(nameof(ShortAllowTimeExit), true)
.SetDisplay("Short Allow Time Exit", "Enable time based short exit", "Risk");

_longTimeExitMinutes = Param(nameof(LongTimeExitMinutes), 1920)
.SetGreaterThanZero()
.SetDisplay("Long Time Exit (min)", "Minutes before long position is forced to close", "Risk");

_shortTimeExitMinutes = Param(nameof(ShortTimeExitMinutes), 1920)
.SetGreaterThanZero()
.SetDisplay("Short Time Exit (min)", "Minutes before short position is forced to close", "Risk");

_tradeVolume = Param(nameof(TradeVolume), 1m)
.SetGreaterThanZero()
.SetDisplay("Trade Volume", "Default order volume", "Trading")
.SetCanOptimize(true)
.SetOptimize(0.5m, 5m, 0.5m);

_stopLossSteps = Param(nameof(StopLossSteps), 1000m)
.SetNotNegative()
.SetDisplay("Stop Loss (steps)", "Protective stop in price steps", "Risk");

_takeProfitSteps = Param(nameof(TakeProfitSteps), 2000m)
.SetNotNegative()
.SetDisplay("Take Profit (steps)", "Profit target in price steps", "Risk");
}

/// <summary>
/// Candle type used for long side calculations.
/// </summary>
public DataType LongCandleType
{
get => _longCandleType.Value;
set => _longCandleType.Value = value;
}

/// <summary>
/// Candle type used for short side calculations.
/// </summary>
public DataType ShortCandleType
{
get => _shortCandleType.Value;
set => _shortCandleType.Value = value;
}

/// <summary>
/// Pentuple EMA length for the long stream.
/// </summary>
public decimal LongEmaLength
{
get => _longEmaLength.Value;
set => _longEmaLength.Value = value;
}

/// <summary>
/// Pentuple EMA length for the short stream.
/// </summary>
public decimal ShortEmaLength
{
get => _shortEmaLength.Value;
set => _shortEmaLength.Value = value;
}

/// <summary>
/// Price selection mode for the long stream.
/// </summary>
public AppliedPrice LongPriceMode
{
get => _longPriceMode.Value;
set => _longPriceMode.Value = value;
}

/// <summary>
/// Price selection mode for the short stream.
/// </summary>
public AppliedPrice ShortPriceMode
{
get => _shortPriceMode.Value;
set => _shortPriceMode.Value = value;
}

/// <summary>
/// Rounding digits used for the long indicator.
/// </summary>
public int LongDigits
{
get => _longDigits.Value;
set => _longDigits.Value = value;
}

/// <summary>
/// Rounding digits used for the short indicator.
/// </summary>
public int ShortDigits
{
get => _shortDigits.Value;
set => _shortDigits.Value = value;
}

/// <summary>
/// Number of bars back referenced for the long signal.
/// </summary>
public int LongSignalBar
{
get => _longSignalBar.Value;
set => _longSignalBar.Value = value;
}

/// <summary>
/// Number of bars back referenced for the short signal.
/// </summary>
public int ShortSignalBar
{
get => _shortSignalBar.Value;
set => _shortSignalBar.Value = value;
}

/// <summary>
/// Enables long side entries.
/// </summary>
public bool LongAllowOpen
{
get => _longAllowOpen.Value;
set => _longAllowOpen.Value = value;
}

/// <summary>
/// Enables short side entries.
/// </summary>
public bool ShortAllowOpen
{
get => _shortAllowOpen.Value;
set => _shortAllowOpen.Value = value;
}

/// <summary>
/// Enables long side indicator based exits.
/// </summary>
public bool LongAllowClose
{
get => _longAllowClose.Value;
set => _longAllowClose.Value = value;
}

/// <summary>
/// Enables short side indicator based exits.
/// </summary>
public bool ShortAllowClose
{
get => _shortAllowClose.Value;
set => _shortAllowClose.Value = value;
}

/// <summary>
/// Enables long side time based exit.
/// </summary>
public bool LongAllowTimeExit
{
get => _longAllowTimeExit.Value;
set => _longAllowTimeExit.Value = value;
}

/// <summary>
/// Enables short side time based exit.
/// </summary>
public bool ShortAllowTimeExit
{
get => _shortAllowTimeExit.Value;
set => _shortAllowTimeExit.Value = value;
}

/// <summary>
/// Minutes before a long position is forced to exit.
/// </summary>
public int LongTimeExitMinutes
{
get => _longTimeExitMinutes.Value;
set => _longTimeExitMinutes.Value = value;
}

/// <summary>
/// Minutes before a short position is forced to exit.
/// </summary>
public int ShortTimeExitMinutes
{
get => _shortTimeExitMinutes.Value;
set => _shortTimeExitMinutes.Value = value;
}

/// <summary>
/// Default order volume.
/// </summary>
public decimal TradeVolume
{
get => _tradeVolume.Value;
set => _tradeVolume.Value = value;
}

/// <summary>
/// Stop loss distance expressed in price steps.
/// </summary>
public decimal StopLossSteps
{
get => _stopLossSteps.Value;
set => _stopLossSteps.Value = value;
}

/// <summary>
/// Take profit distance expressed in price steps.
/// </summary>
public decimal TakeProfitSteps
{
get => _takeProfitSteps.Value;
set => _takeProfitSteps.Value = value;
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
yield return (Security, LongCandleType);

if (ShortCandleType != LongCandleType)
yield return (Security, ShortCandleType);
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();

_longColors.Clear();
_shortColors.Clear();
_longEntryTime = null;
_shortEntryTime = null;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

Volume = TradeVolume;

_longPema = new PentupleExponentialMovingAverageIndicator
{
Length = LongEmaLength,
AppliedPrice = LongPriceMode,
Digits = LongDigits
};

_shortPema = new PentupleExponentialMovingAverageIndicator
{
Length = ShortEmaLength,
AppliedPrice = ShortPriceMode,
Digits = ShortDigits
};

var longSubscription = SubscribeCandles(LongCandleType);
longSubscription
.BindEx(_longPema, ProcessLongStream)
.Start();

var shortSubscription = SubscribeCandles(ShortCandleType);
shortSubscription
.BindEx(_shortPema, ProcessShortStream)
.Start();

Unit stopLoss = StopLossSteps > 0m ? new Unit(StopLossSteps, UnitTypes.Step) : null;
Unit takeProfit = TakeProfitSteps > 0m ? new Unit(TakeProfitSteps, UnitTypes.Step) : null;

if (stopLoss != null || takeProfit != null)
{
StartProtection(takeProfit: takeProfit, stopLoss: stopLoss, useMarketOrders: true);
}

var priceArea = CreateChartArea();

if (priceArea != null)
{
DrawCandles(priceArea, longSubscription);
DrawIndicator(priceArea, _longPema);
DrawIndicator(priceArea, _shortPema);
DrawOwnTrades(priceArea);
}
}

private void ProcessLongStream(ICandleMessage candle, IIndicatorValue indicatorValue)
{
if (candle.State != CandleStates.Finished)
return;

if (indicatorValue is not PentupleExponentialMovingAverageValue value || !value.IsFinal)
return;

var color = DetermineColor(value.Previous, value.Current);
UpdateHistory(_longColors, color, LongSignalBar);

ProcessTimeExit(candle.CloseTime, LongAllowTimeExit, LongTimeExitMinutes, isLong: true);

if (_longColors.Count < Math.Max(2, Math.Max(1, LongSignalBar) + 1))
return;

var signalOffset = Math.Max(1, LongSignalBar);
var currentColor = GetColor(_longColors, signalOffset);
var previousColor = GetColor(_longColors, signalOffset + 1);

if (LongAllowOpen && currentColor == IndicatorColor.Up && previousColor != IndicatorColor.Up && Position <= 0)
{
BuyMarket();
_longEntryTime = candle.CloseTime;
}

if (LongAllowClose && currentColor == IndicatorColor.Down && Position > 0)
{
SellMarket();
_longEntryTime = null;
}
}

private void ProcessShortStream(ICandleMessage candle, IIndicatorValue indicatorValue)
{
if (candle.State != CandleStates.Finished)
return;

if (indicatorValue is not PentupleExponentialMovingAverageValue value || !value.IsFinal)
return;

var color = DetermineColor(value.Previous, value.Current);
UpdateHistory(_shortColors, color, ShortSignalBar);

ProcessTimeExit(candle.CloseTime, ShortAllowTimeExit, ShortTimeExitMinutes, isLong: false);

if (_shortColors.Count < Math.Max(2, Math.Max(1, ShortSignalBar) + 1))
return;

var signalOffset = Math.Max(1, ShortSignalBar);
var currentColor = GetColor(_shortColors, signalOffset);
var previousColor = GetColor(_shortColors, signalOffset + 1);

if (ShortAllowOpen && currentColor == IndicatorColor.Down && previousColor != IndicatorColor.Down && Position >= 0)
{
SellMarket();
_shortEntryTime = candle.CloseTime;
}

if (ShortAllowClose && currentColor == IndicatorColor.Up && Position < 0)
{
BuyMarket();
_shortEntryTime = null;
}
}

private void ProcessTimeExit(DateTimeOffset time, bool allowExit, int minutes, bool isLong)
{
if (!allowExit || minutes <= 0)
return;

var limit = TimeSpan.FromMinutes(minutes);

if (isLong)
{
if (_longEntryTime == null || Position <= 0)
return;

if (time - _longEntryTime >= limit)
{
SellMarket();
_longEntryTime = null;
}
}
else
{
if (_shortEntryTime == null || Position >= 0)
return;

if (time - _shortEntryTime >= limit)
{
BuyMarket();
_shortEntryTime = null;
}
}
}

private static int DetermineColor(decimal? previous, decimal current)
{
if (previous is null)
return IndicatorColor.Neutral;

var delta = current - previous.Value;

if (delta > 0)
return IndicatorColor.Up;

if (delta < 0)
return IndicatorColor.Down;

return IndicatorColor.Neutral;
}

private static void UpdateHistory(List<int> target, int color, int signalBar)
{
target.Add(color);

var maxSize = Math.Max(2, Math.Max(1, signalBar) + 2);

if (target.Count > maxSize)
target.RemoveAt(0);
}

private static int GetColor(List<int> source, int barsAgo)
{
var index = source.Count - barsAgo;
return index >= 0 && index < source.Count ? source[index] : IndicatorColor.Neutral;
}

/// <summary>
/// Enumeration for applied price selection.
/// </summary>
public enum AppliedPrice
{
/// <summary>
/// Candle close price.
/// </summary>
Close,

/// <summary>
/// Candle open price.
/// </summary>
Open,

/// <summary>
/// Candle high price.
/// </summary>
High,

/// <summary>
/// Candle low price.
/// </summary>
Low,

/// <summary>
/// Median price (high + low) / 2.
/// </summary>
Median,

/// <summary>
/// Typical price (high + low + close) / 3.
/// </summary>
Typical,

/// <summary>
/// Weighted close price (high + low + 2 * close) / 4.
/// </summary>
Weighted,

/// <summary>
/// Simple average of open and close.
/// </summary>
Simple,

/// <summary>
/// Quarter price (open + high + low + close) / 4.
/// </summary>
Quarter,

/// <summary>
/// Trend-following price #1.
/// </summary>
TrendFollow0,

/// <summary>
/// Trend-following price #2.
/// </summary>
TrendFollow1,

/// <summary>
/// Demark price.
/// </summary>
Demark
}

private static class IndicatorColor
{
public const int Down = 0;
public const int Neutral = 1;
public const int Up = 2;
}
}

/// <summary>
/// Pentuple exponential moving average indicator with color style output.
/// Provides current and previous values in the resulting indicator value.
/// </summary>
public sealed class PentupleExponentialMovingAverageIndicator : BaseIndicator<decimal>
{
private decimal? _ema1;
private decimal? _ema2;
private decimal? _ema3;
private decimal? _ema4;
private decimal? _ema5;
private decimal? _ema6;
private decimal? _ema7;
private decimal? _ema8;
private decimal? _previous;

/// <summary>
/// Gets or sets the smoothing length (can be fractional).
/// </summary>
public decimal Length { get; set; } = 50.01m;

/// <summary>
/// Gets or sets the applied price mode.
/// </summary>
public ExpColorPemaDigitTmPlusMmrecDuplexStrategy.AppliedPrice AppliedPrice { get; set; } = ExpColorPemaDigitTmPlusMmrecDuplexStrategy.AppliedPrice.Close;

/// <summary>
/// Gets or sets rounding digits.
/// </summary>
public int Digits { get; set; } = 2;

/// <inheritdoc />
protected override IIndicatorValue OnProcess(IIndicatorValue input)
{
if (input is not ICandleMessage candle || candle.State != CandleStates.Finished)
return new DecimalIndicatorValue(this, default, input.Time);

if (Length <= 0)
throw new InvalidOperationException("Length must be greater than zero.");

var price = GetAppliedPrice(candle);
var alpha = 2m / (Length + 1m);

_ema1 = CalculateEma(_ema1, price, alpha);
_ema2 = CalculateEma(_ema2, _ema1 ?? price, alpha);
_ema3 = CalculateEma(_ema3, _ema2 ?? price, alpha);
_ema4 = CalculateEma(_ema4, _ema3 ?? price, alpha);
_ema5 = CalculateEma(_ema5, _ema4 ?? price, alpha);
_ema6 = CalculateEma(_ema6, _ema5 ?? price, alpha);
_ema7 = CalculateEma(_ema7, _ema6 ?? price, alpha);
_ema8 = CalculateEma(_ema8, _ema7 ?? price, alpha);

if (_ema8 is null)
return new DecimalIndicatorValue(this, default, input.Time);

var ema1 = _ema1!.Value;
var ema2 = _ema2!.Value;
var ema3 = _ema3!.Value;
var ema4 = _ema4!.Value;
var ema5 = _ema5!.Value;
var ema6 = _ema6!.Value;
var ema7 = _ema7!.Value;
var ema8 = _ema8!.Value;

var value = 8m * ema1 - 28m * ema2 + 56m * ema3 - 70m * ema4 + 56m * ema5 - 28m * ema6 + 8m * ema7 - ema8;

if (Digits >= 0)
{
value = Math.Round(value, Digits, MidpointRounding.AwayFromZero);
}

var result = new PentupleExponentialMovingAverageValue(this, input, value, _previous);
_previous = value;
return result;
}

private static decimal CalculateEma(decimal? previous, decimal value, decimal alpha)
{
return previous is null ? value : previous.Value + alpha * (value - previous.Value);
}

private decimal GetAppliedPrice(ICandleMessage candle)
{
return AppliedPrice switch
{
ExpColorPemaDigitTmPlusMmrecDuplexStrategy.AppliedPrice.Close => candle.ClosePrice,
ExpColorPemaDigitTmPlusMmrecDuplexStrategy.AppliedPrice.Open => candle.OpenPrice,
ExpColorPemaDigitTmPlusMmrecDuplexStrategy.AppliedPrice.High => candle.HighPrice,
ExpColorPemaDigitTmPlusMmrecDuplexStrategy.AppliedPrice.Low => candle.LowPrice,
ExpColorPemaDigitTmPlusMmrecDuplexStrategy.AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
ExpColorPemaDigitTmPlusMmrecDuplexStrategy.AppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
ExpColorPemaDigitTmPlusMmrecDuplexStrategy.AppliedPrice.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
ExpColorPemaDigitTmPlusMmrecDuplexStrategy.AppliedPrice.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
ExpColorPemaDigitTmPlusMmrecDuplexStrategy.AppliedPrice.Quarter => (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m,
ExpColorPemaDigitTmPlusMmrecDuplexStrategy.AppliedPrice.TrendFollow0 => candle.ClosePrice > candle.OpenPrice ? candle.HighPrice : candle.ClosePrice < candle.OpenPrice ? candle.LowPrice : candle.ClosePrice,
ExpColorPemaDigitTmPlusMmrecDuplexStrategy.AppliedPrice.TrendFollow1 => candle.ClosePrice > candle.OpenPrice ? (candle.HighPrice + candle.ClosePrice) / 2m : candle.ClosePrice < candle.OpenPrice ? (candle.LowPrice + candle.ClosePrice) / 2m : candle.ClosePrice,
ExpColorPemaDigitTmPlusMmrecDuplexStrategy.AppliedPrice.Demark => CalculateDemarkPrice(candle),
_ => candle.ClosePrice,
};
}

private static decimal CalculateDemarkPrice(ICandleMessage candle)
{
var res = candle.HighPrice + candle.LowPrice + candle.ClosePrice;

if (candle.ClosePrice < candle.OpenPrice)
{
res = (res + candle.LowPrice) / 2m;
}
else if (candle.ClosePrice > candle.OpenPrice)
{
res = (res + candle.HighPrice) / 2m;
}
else
{
res = (res + candle.ClosePrice) / 2m;
}

return ((res - candle.LowPrice) + (res - candle.HighPrice)) / 2m;
}
}

/// <summary>
/// Complex indicator value for the pentuple EMA, exposing current and previous values.
/// </summary>
public sealed class PentupleExponentialMovingAverageValue : ComplexIndicatorValue
{
/// <summary>
/// Initializes a new instance of the <see cref="PentupleExponentialMovingAverageValue"/> class.
/// </summary>
public PentupleExponentialMovingAverageValue(IIndicator indicator, IIndicatorValue input, decimal current, decimal? previous)
: base(indicator, input, (nameof(Current), current), (nameof(Previous), previous))
{
}

/// <summary>
/// Gets the current pentuple EMA value.
/// </summary>
public decimal Current => (decimal)this[nameof(Current)];

/// <summary>
/// Gets the previous pentuple EMA value if available.
/// </summary>
public decimal? Previous => (decimal?)this[nameof(Previous)];
}
