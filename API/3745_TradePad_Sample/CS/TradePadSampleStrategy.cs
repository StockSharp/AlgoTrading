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
/// Port of the MetaTrader TradePad sample that classifies multiple symbols by Stochastic oscillator readings.
/// </summary>
public class TradePadSampleStrategy : Strategy
{
private readonly StrategyParam<string> _symbolList;
private readonly StrategyParam<int> _timerPeriodSeconds;
private readonly StrategyParam<int> _stochasticLength;
private readonly StrategyParam<int> _stochasticKPeriod;
private readonly StrategyParam<int> _stochasticDPeriod;
private readonly StrategyParam<decimal> _upperLevel;
private readonly StrategyParam<decimal> _lowerLevel;
private readonly StrategyParam<DataType> _candleType;

private readonly Dictionary<Security, StochasticOscillator> _stochastics = new();
private readonly Dictionary<string, TrendStates> _trendStates = new();
private readonly Dictionary<string, decimal> _latestK = new();
private readonly Dictionary<string, DateTimeOffset> _lastUpdateTime = new();

/// <summary>
/// Represents the simplified trend state of a monitored symbol.
/// </summary>
public enum TrendStates
{
/// <summary>No reading calculated yet.</summary>
Unknown,

/// <summary>Stochastic value is above the configured upper threshold.</summary>
Uptrend,

/// <summary>Stochastic value is below the configured lower threshold.</summary>
Downtrend,

/// <summary>Stochastic value is inside the neutral zone.</summary>
Flat
}

/// <summary>
/// Initializes a new instance of the <see cref="TradePadSampleStrategy"/> class.
/// </summary>
public TradePadSampleStrategy()
{
_symbolList = Param(nameof(SymbolList), string.Empty)
.SetDisplay("Symbols", "Comma separated list of tickers monitored by the pad", "General");

_timerPeriodSeconds = Param(nameof(TimerPeriodSeconds), 5)
.SetRange(1, 3600)
.SetDisplay("Refresh Interval", "Minimum seconds between status refreshes per symbol", "General");

_stochasticLength = Param(nameof(StochasticLength), 5)
.SetRange(1, 100)
.SetDisplay("Stochastic Length", "Base period used for the %K calculation", "Indicators")
.SetCanOptimize(true);

_stochasticKPeriod = Param(nameof(StochasticKPeriod), 3)
.SetRange(1, 100)
.SetDisplay("Stochastic %K Smoothing", "Smoothing applied to the %K line", "Indicators")
.SetCanOptimize(true);

_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
.SetRange(1, 100)
.SetDisplay("Stochastic %D Period", "Smoothing applied to the %D line", "Indicators")
.SetCanOptimize(true);

_upperLevel = Param(nameof(UpperLevel), 80m)
.SetDisplay("Upper Threshold", "Level that marks an uptrend state", "Signals")
.SetCanOptimize(true);

_lowerLevel = Param(nameof(LowerLevel), 20m)
.SetDisplay("Lower Threshold", "Level that marks a downtrend state", "Signals")
.SetCanOptimize(true);

_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(15)))
.SetDisplay("Candle Type", "Time-frame used for indicator calculations", "Data");
}

/// <summary>
/// Comma separated list of tickers monitored by the TradePad.
/// </summary>
public string SymbolList
{
get => _symbolList.Value;
set => _symbolList.Value = value;
}

/// <summary>
/// Minimum seconds between status refreshes per symbol.
/// </summary>
public int TimerPeriodSeconds
{
get => _timerPeriodSeconds.Value;
set => _timerPeriodSeconds.Value = value;
}

/// <summary>
/// Base period used for the %K calculation.
/// </summary>
public int StochasticLength
{
get => _stochasticLength.Value;
set => _stochasticLength.Value = value;
}

/// <summary>
/// Smoothing applied to the %K line.
/// </summary>
public int StochasticKPeriod
{
get => _stochasticKPeriod.Value;
set => _stochasticKPeriod.Value = value;
}

/// <summary>
/// Smoothing applied to the %D line.
/// </summary>
public int StochasticDPeriod
{
get => _stochasticDPeriod.Value;
set => _stochasticDPeriod.Value = value;
}

/// <summary>
/// Level that marks an uptrend state.
/// </summary>
public decimal UpperLevel
{
get => _upperLevel.Value;
set => _upperLevel.Value = value;
}

/// <summary>
/// Level that marks a downtrend state.
/// </summary>
public decimal LowerLevel
{
get => _lowerLevel.Value;
set => _lowerLevel.Value = value;
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
/// Returns the latest trend state per symbol.
/// </summary>
public IReadOnlyDictionary<string, TrendStates> TrendStates => _trendStates;

/// <summary>
/// Returns the latest %K oscillator readings per symbol.
/// </summary>
public IReadOnlyDictionary<string, decimal> LatestKValues => _latestK;

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var securities = ResolveSecurities();
if (securities.Count == 0)
{
throw new InvalidOperationException("No securities available. Assign the main Security or specify SymbolList.");
}

foreach (var security in securities)
{
var stochastic = new StochasticOscillator
{
Length = Math.Max(1, StochasticLength),
KPeriod = Math.Max(1, StochasticKPeriod),
DPeriod = Math.Max(1, StochasticDPeriod)
};

_stochastics[security] = stochastic;

var subscription = SubscribeCandles(CandleType, security: security);
subscription.BindEx(stochastic, (candle, indicatorValue) => ProcessStochastic(security, candle, indicatorValue)).Start();

var symbolId = security.Id;
if (!_trendStates.ContainsKey(symbolId))
{
_trendStates[symbolId] = TrendStates.Unknown;
}
}
}

private List<Security> ResolveSecurities()
{
var result = new List<Security>();

var ids = SplitSymbols(SymbolList);
if (ids.Length == 0)
{
if (Security != null)
{
result.Add(Security);
}

return result;
}

foreach (var id in ids)
{
var security = this.GetSecurity(id);
if (security != null)
{
result.Add(security);
continue;
}

LogWarning("Security '{0}' was not found in the provider and will be skipped.", id);
}

return result;
}

private static string[] SplitSymbols(string symbols)
{
if (symbols.IsEmptyOrWhiteSpace())
{
return Array.Empty<string>();
}

var separators = new[] { ',', ';', '\t', '\n', '\r' };
var parts = symbols.Split(separators, StringSplitOptions.RemoveEmptyEntries);
for (var i = 0; i < parts.Length; i++)
{
parts[i] = parts[i].Trim();
}

return parts;
}

private void ProcessStochastic(Security security, ICandleMessage candle, IIndicatorValue indicatorValue)
{
if (candle.State != CandleStates.Finished)
{
return;
}

if (!indicatorValue.IsFinal)
{
return;
}

var stochasticValue = (StochasticOscillatorValue)indicatorValue;
if (stochasticValue.K is not decimal kValue)
{
return;
}

var symbolId = security.Id;
var currentState = DetermineState(kValue);
_latestK[symbolId] = kValue;

var now = candle.OpenTime;
var minInterval = TimeSpan.FromSeconds(Math.Max(1, TimerPeriodSeconds));
if (_lastUpdateTime.TryGetValue(symbolId, out var previous) && now - previous < minInterval)
{
return;
}

_lastUpdateTime[symbolId] = now;

if (!_trendStates.TryGetValue(symbolId, out var stored) || stored != currentState)
{
_trendStates[symbolId] = currentState;
LogInfo("{0}: {1} (K={2:0.##})", symbolId, currentState, kValue);
}
}

private TrendStates DetermineState(decimal kValue)
{
if (kValue >= UpperLevel)
{
return TrendStates.Uptrend;
}

if (kValue <= LowerLevel)
{
return TrendStates.Downtrend;
}

return TrendStates.Flat;
}
}

