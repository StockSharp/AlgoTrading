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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MultiTrader currency strength dashboard converted from MQL.
/// Calculates relative strength for the eight major currencies, suggests the strongest/weakest pair and optionally opens trades.
/// </summary>
public class MultiTraderStrategy : Strategy
{
private readonly StrategyParam<IEnumerable<Security>> _universeParam;
private readonly StrategyParam<DataType> _candleTypeParam;
private readonly StrategyParam<int> _buyLevelParam;
private readonly StrategyParam<int> _sellLevelParam;
private readonly StrategyParam<bool> _autoTradeParam;
private readonly StrategyParam<decimal> _orderVolumeParam;
private readonly StrategyParam<string> _prefixParam;
private readonly StrategyParam<string> _suffixParam;

private readonly Dictionary<string, Security> _canonicalToSecurity = new(StringComparer.OrdinalIgnoreCase);
private readonly Dictionary<string, decimal> _pairPercent = new(StringComparer.OrdinalIgnoreCase);
private readonly Dictionary<string, decimal> _currencyStrengths = new(StringComparer.OrdinalIgnoreCase);

private string _lastSuggestedPair;
private Sides? _lastSignalSide;

private static readonly Dictionary<string, PairComponent[]> _currencyFormulas = new(StringComparer.OrdinalIgnoreCase)
{
["AUD"] =
[
new("AUDJPY", false),
new("AUDNZD", false),
new("AUDUSD", false),
new("EURAUD", true),
new("GBPAUD", true),
new("AUDCHF", false),
new("AUDCAD", false),
],
["CAD"] =
[
new("CADJPY", false),
new("NZDCAD", true),
new("USDCAD", true),
new("EURCAD", true),
new("GBPCAD", true),
new("AUDCAD", true),
new("CADCHF", false),
],
["CHF"] =
[
new("CHFJPY", false),
new("NZDCHF", true),
new("USDCHF", true),
new("EURCHF", true),
new("GBPCHF", true),
new("AUDCHF", true),
new("CADCHF", true),
],
["EUR"] =
[
new("EURJPY", false),
new("EURNZD", false),
new("EURUSD", false),
new("EURCAD", false),
new("EURGBP", false),
new("EURAUD", false),
new("EURCHF", false),
],
["GBP"] =
[
new("GBPJPY", false),
new("GBPNZD", false),
new("GBPUSD", false),
new("GBPCAD", false),
new("EURGBP", true),
new("GBPAUD", false),
new("GBPCHF", false),
],
["JPY"] =
[
new("AUDJPY", true),
new("CHFJPY", true),
new("CADJPY", true),
new("EURJPY", true),
new("GBPJPY", true),
new("NZDJPY", true),
new("USDJPY", true),
],
["NZD"] =
[
new("NZDJPY", false),
new("GBPNZD", true),
new("NZDUSD", false),
new("NZDCAD", false),
new("EURNZD", true),
new("AUDNZD", true),
new("NZDCHF", false),
],
["USD"] =
[
new("AUDUSD", true),
new("USDCHF", false),
new("USDCAD", false),
new("EURUSD", true),
new("GBPUSD", true),
new("USDJPY", false),
new("NZDUSD", true),
],
};

/// <summary>
/// Securities used to calculate currency strength.
/// </summary>
public IEnumerable<Security> Universe
{
get => _universeParam.Value;
set => _universeParam.Value = value;
}

/// <summary>
/// Candle type used for percentage calculations.
/// </summary>
public DataType CandleType
{
get => _candleTypeParam.Value;
set => _candleTypeParam.Value = value;
}

/// <summary>
/// Upper threshold (in percent) that marks a currency as overbought.
/// </summary>
public int BuyLevel
{
get => _buyLevelParam.Value;
set => _buyLevelParam.Value = value;
}

/// <summary>
/// Lower threshold (in percent) that marks a currency as oversold.
/// </summary>
public int SellLevel
{
get => _sellLevelParam.Value;
set => _sellLevelParam.Value = value;
}

/// <summary>
/// Enables automated trade execution on suggested pairs.
/// </summary>
public bool EnableAutoTrading
{
get => _autoTradeParam.Value;
set => _autoTradeParam.Value = value;
}

/// <summary>
/// Volume used for market orders.
/// </summary>
public decimal OrderVolume
{
get => _orderVolumeParam.Value;
set => _orderVolumeParam.Value = value;
}

/// <summary>
/// Optional symbol prefix used by the trading venue (for example "m." or "FX_").
/// </summary>
public string SymbolPrefix
{
get => _prefixParam.Value;
set => _prefixParam.Value = value;
}

/// <summary>
/// Optional symbol suffix used by the trading venue (for example "-m" or ".FX").
/// </summary>
public string SymbolSuffix
{
get => _suffixParam.Value;
set => _suffixParam.Value = value;
}

/// <summary>
/// Initializes a new instance of the <see cref="MultiTraderStrategy"/> class.
/// </summary>
public MultiTraderStrategy()
{
_universeParam = Param<IEnumerable<Security>>(nameof(Universe))
.SetDisplay("Universe", "List of FX pairs used for calculations", "Data");

_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
.SetDisplay("Candle Type", "Timeframe used for strength calculations", "Data");

_buyLevelParam = Param(nameof(BuyLevel), 90)
.SetDisplay("Buy Level", "Currency strength threshold considered overbought", "Signals")
.SetCanOptimize(true);

_sellLevelParam = Param(nameof(SellLevel), 10)
.SetDisplay("Sell Level", "Currency strength threshold considered oversold", "Signals")
.SetCanOptimize(true);

_autoTradeParam = Param(nameof(EnableAutoTrading), false)
.SetDisplay("Auto Trade", "Automatically execute trades on suggested pairs", "Trading");

_orderVolumeParam = Param(nameof(OrderVolume), 1m)
.SetDisplay("Order Volume", "Volume used for market entries", "Trading");

_prefixParam = Param(nameof(SymbolPrefix), string.Empty)
.SetDisplay("Prefix", "Exchange specific prefix applied before pair codes", "Data");

_suffixParam = Param(nameof(SymbolSuffix), string.Empty)
.SetDisplay("Suffix", "Exchange specific suffix appended after pair codes", "Data");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
if (CandleType == null)
yield break;

var universe = Universe;
if (universe == null)
yield break;

foreach (var security in universe)
{
if (security != null)
yield return (security, CandleType);
}
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();

_canonicalToSecurity.Clear();
_pairPercent.Clear();
_currencyStrengths.Clear();
_lastSuggestedPair = null;
_lastSignalSide = null;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var candleType = CandleType ?? throw new InvalidOperationException("CandleType must be configured before starting.");

var securities = (Universe ?? Array.Empty<Security>())
.Where(s => s != null)
.ToArray();

if (securities.Length == 0)
throw new InvalidOperationException("Universe must contain at least one security.");

_canonicalToSecurity.Clear();

foreach (var security in securities)
{
var canonical = GetCanonicalPair(security.Code);
if (canonical.IsEmpty())
{
LogWarning($"Unable to determine canonical code for security '{security.Id}'.");
continue;
}

_canonicalToSecurity[canonical] = security;
var localCanonical = canonical;

SubscribeCandles(candleType, true, security)
.Bind(candle => ProcessCandle(candle, localCanonical))
.Start();
}

StartProtection();
}

private void ProcessCandle(ICandleMessage candle, string canonicalPair)
{
if (candle.State != CandleStates.Finished)
return;

var high = candle.HighPrice;
var low = candle.LowPrice;
if (high <= low)
return;

var percent = 100m * (candle.ClosePrice - low) / (high - low);
_pairPercent[canonicalPair] = percent;

UpdateStrengths();
}

private void UpdateStrengths()
{
var snapshot = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

foreach (var (currency, components) in _currencyFormulas)
{
if (!TryCalculateStrength(components, out var value))
return;

snapshot[currency] = value;
}

_currencyStrengths.Clear();
foreach (var (currency, value) in snapshot)
_currencyStrengths[currency] = value;

EvaluateSignals();
}

private bool TryCalculateStrength(IEnumerable<PairComponent> components, out decimal value)
{
decimal sum = 0m;
var count = 0;

foreach (var component in components)
{
if (!TryGetPairPercent(component.Pair, out var percent))
{
value = default;
return false;
}

sum += component.Invert ? 100m - percent : percent;
count++;
}

value = count == 0 ? 0m : Math.Round(sum / count, 2, MidpointRounding.AwayFromZero);
return true;
}

private bool TryGetPairPercent(string pair, out decimal value)
{
var canonical = pair.ToUpperInvariant();
if (_pairPercent.TryGetValue(canonical, out value))
return true;

var prefixed = (SymbolPrefix + pair + SymbolSuffix).ToUpperInvariant();
return _pairPercent.TryGetValue(prefixed, out value);
}

private void EvaluateSignals()
{
if (_currencyStrengths.Count < _currencyFormulas.Count)
return;

var ordered = _currencyStrengths.OrderByDescending(p => p.Value).ToArray();
if (ordered.Length == 0)
return;

var strongest = ordered.First();
var weakest = ordered.Last();

var snapshot = string.Join(", ", ordered.Select(p => $"{p.Key}:{p.Value:F1}"));
LogInfo($"Currency strengths snapshot: {snapshot}");

if (strongest.Value < BuyLevel || weakest.Value > SellLevel)
{
_lastSuggestedPair = null;
_lastSignalSide = null;
return;
}

var suggestion = BuildSuggestion(strongest.Key, weakest.Key);
if (suggestion == null)
return;

if (_lastSuggestedPair == suggestion.Value.Pair && _lastSignalSide == suggestion.Value.Side)
return;

_lastSuggestedPair = suggestion.Value.Pair;
_lastSignalSide = suggestion.Value.Side;

LogInfo($"Suggested pair: {suggestion.Value.Pair} | Strong: {strongest.Key} ({strongest.Value:F1}) | Weak: {weakest.Key} ({weakest.Value:F1}) | Side: {suggestion.Value.Side}");

if (EnableAutoTrading && OrderVolume > 0m && IsFormedAndOnlineAndAllowTrading())
ExecuteTrade(suggestion.Value);
}

private StrengthSuggestion? BuildSuggestion(string strongCurrency, string weakCurrency)
{
if (TryCreateSuggestion(strongCurrency, weakCurrency, Sides.Buy, out var direct))
return direct;

if (TryCreateSuggestion(weakCurrency, strongCurrency, Sides.Sell, out var inverse))
return inverse;

if (!strongCurrency.EqualsIgnoreCase("USD") && TryCreateSuggestion(strongCurrency, "USD", Sides.Buy, out var strongUsd))
return strongUsd;

if (!weakCurrency.EqualsIgnoreCase("USD") && TryCreateSuggestion("USD", weakCurrency, Sides.Sell, out var usdWeak))
return usdWeak;

return null;
}

private bool TryCreateSuggestion(string baseCurrency, string quoteCurrency, Sides side, out StrengthSuggestion suggestion)
{
suggestion = default;
var pair = (baseCurrency + quoteCurrency).ToUpperInvariant();

if (!_canonicalToSecurity.TryGetValue(pair, out var security))
return false;

suggestion = new StrengthSuggestion(pair, side, security);
return true;
}

private void ExecuteTrade(StrengthSuggestion suggestion)
{
var security = suggestion.Security;
if (security == null)
return;

var volume = OrderVolume;
if (volume <= 0m)
return;

var position = GetPositionValue(security, Portfolio) ?? 0m;

if (suggestion.Side == Sides.Buy)
{
var orderVolume = volume + (position < 0m ? Math.Abs(position) : 0m);
if (orderVolume > 0m)
BuyMarket(orderVolume, security);
}
else
{
var orderVolume = volume + (position > 0m ? Math.Abs(position) : 0m);
if (orderVolume > 0m)
SellMarket(orderVolume, security);
}
}

private string GetCanonicalPair(string code)
{
if (code.IsEmpty())
return string.Empty;

var trimmed = code;

if (!SymbolPrefix.IsEmpty() && trimmed.StartsWith(SymbolPrefix, StringComparison.OrdinalIgnoreCase))
trimmed = trimmed[SymbolPrefix.Length..];

if (!SymbolSuffix.IsEmpty() && trimmed.EndsWith(SymbolSuffix, StringComparison.OrdinalIgnoreCase))
trimmed = trimmed[..^SymbolSuffix.Length];

return trimmed.ToUpperInvariant();
}

private readonly record struct PairComponent(string Pair, bool Invert);

private readonly record struct StrengthSuggestion(string Pair, Sides Side, Security Security);
}

