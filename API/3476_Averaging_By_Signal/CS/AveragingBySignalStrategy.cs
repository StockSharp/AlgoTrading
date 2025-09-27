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
/// MetaTrader AveragingBySignal expert advisor ported to the high-level StockSharp API.
/// Implements moving-average crossover entries with optional averaging, shared take-profit,
/// and a single-order trailing stop inspired by the original MQL logic.
/// </summary>
public class AveragingBySignalStrategy : Strategy
{
/// <summary>
/// Position sizing mode that mirrors the MQL LotType input.
/// </summary>
public enum LotSizingModes
{
/// <summary>
/// Always use the base volume for every order.
/// </summary>
Fixed,

/// <summary>
/// Multiply the base volume for every additional layer.
/// </summary>
Multiplier,
}

/// <summary>
/// Moving average calculation method used by the expert advisor.
/// </summary>
public enum MovingAverageMethods
{
/// <summary>
/// Simple moving average (arithmetic mean).
/// </summary>
Simple,

/// <summary>
/// Exponential moving average.
/// </summary>
Exponential,

/// <summary>
/// Smoothed moving average (RMA/SMMA).
/// </summary>
Smoothed,

/// <summary>
/// Linear weighted moving average.
/// </summary>
LinearWeighted,
}

private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<decimal> _initialVolume;
private readonly StrategyParam<LotSizingModes> _lotSizing;
private readonly StrategyParam<decimal> _multiplier;
private readonly StrategyParam<int> _fastPeriod;
private readonly StrategyParam<MovingAverageMethods> _fastMethod;
private readonly StrategyParam<int> _slowPeriod;
private readonly StrategyParam<MovingAverageMethods> _slowMethod;
private readonly StrategyParam<int> _takeProfitPips;
private readonly StrategyParam<bool> _averagingBySignal;
private readonly StrategyParam<decimal> _layerDistancePips;
private readonly StrategyParam<int> _maxLayers;
private readonly StrategyParam<bool> _enableTrailing;
private readonly StrategyParam<decimal> _trailingStartPips;
private readonly StrategyParam<decimal> _trailingStepPips;

private IIndicator _fastIndicator = null!;
private IIndicator _slowIndicator = null!;
private readonly List<PositionEntry> _longEntries = new();
private readonly List<PositionEntry> _shortEntries = new();
private decimal? _previousFast;
private decimal? _previousSlow;
private decimal? _longTakeProfit;
private decimal? _shortTakeProfit;
private decimal? _longTrailingStop;
private decimal? _shortTrailingStop;
private decimal _pointSize;

private sealed class PositionEntry
{
public PositionEntry(decimal price, decimal volume)
{
Price = price;
Volume = volume;
}

public decimal Price { get; }
public decimal Volume { get; set; }
}

/// <summary>
/// Initializes a new instance of <see cref="AveragingBySignalStrategy"/>.
/// </summary>
public AveragingBySignalStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
.SetDisplay("Candle Type", "Primary timeframe used for both moving averages.", "General")
.SetCanOptimize(true);

_initialVolume = Param(nameof(InitialVolume), 0.1m)
.SetDisplay("Initial Volume", "Base lot size used for the first entry.", "Money Management")
.SetGreaterThanZero()
.SetCanOptimize(true);

_lotSizing = Param(nameof(LotSizing), LotSizingModes.Multiplier)
.SetDisplay("Lot Sizing", "Choose between fixed or multiplier-based sizing.", "Money Management");

_multiplier = Param(nameof(Multiplier), 2m)
.SetDisplay("Multiplier", "Lot multiplier applied to every additional layer.", "Money Management")
.SetGreaterThanZero()
.SetCanOptimize(true);

_fastPeriod = Param(nameof(FastPeriod), 28)
.SetDisplay("Fast Period", "Lookback for the fast moving average.", "Indicators")
.SetGreaterThanZero()
.SetCanOptimize(true);

_fastMethod = Param(nameof(FastMethod), MovingAverageMethods.LinearWeighted)
.SetDisplay("Fast Method", "Moving average method for the fast line.", "Indicators");

_slowPeriod = Param(nameof(SlowPeriod), 50)
.SetDisplay("Slow Period", "Lookback for the slow moving average.", "Indicators")
.SetGreaterThanZero()
.SetCanOptimize(true);

_slowMethod = Param(nameof(SlowMethod), MovingAverageMethods.Smoothed)
.SetDisplay("Slow Method", "Moving average method for the slow line.", "Indicators");

_takeProfitPips = Param(nameof(TakeProfitPips), 15)
.SetDisplay("Take Profit (pips)", "Shared profit target attached to the basket.", "Risk")
.SetNotNegative()
.SetCanOptimize(true);

_averagingBySignal = Param(nameof(AveragingBySignal), true)
.SetDisplay("Averaging By Signal", "Require a fresh signal before adding new layers.", "Averaging");

_layerDistancePips = Param(nameof(LayerDistancePips), 10m)
.SetDisplay("Layer Distance (pips)", "Minimal adverse move before adding a new order.", "Averaging")
.SetGreaterThanZero()
.SetCanOptimize(true);

_maxLayers = Param(nameof(MaxLayers), 10)
.SetDisplay("Max Layers", "Maximum number of simultaneous orders per direction.", "Averaging")
.SetGreaterThanZero()
.SetCanOptimize(true);

_enableTrailing = Param(nameof(EnableTrailing), false)
.SetDisplay("Trailing Stop", "Enable the single-order trailing stop logic.", "Protection");

_trailingStartPips = Param(nameof(TrailingStartPips), 10m)
.SetDisplay("Trailing Start (pips)", "Profit distance that activates the trailing stop.", "Protection")
.SetNotNegative()
.SetCanOptimize(true);

_trailingStepPips = Param(nameof(TrailingStepPips), 1m)
.SetDisplay("Trailing Step (pips)", "Minimal improvement required to raise the stop.", "Protection")
.SetNotNegative()
.SetCanOptimize(true);
}

/// <summary>
/// Candle type used to feed the indicators.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Base volume requested for the very first position in a basket.
/// </summary>
public decimal InitialVolume
{
get => _initialVolume.Value;
set => _initialVolume.Value = value;
}

/// <summary>
/// Sizing logic used when calculating volumes for averaging layers.
/// </summary>
public LotSizingModes LotSizing
{
get => _lotSizing.Value;
set => _lotSizing.Value = value;
}

/// <summary>
/// Multiplier applied when <see cref="LotSizingModes.Multiplier"/> is selected.
/// </summary>
public decimal Multiplier
{
get => _multiplier.Value;
set => _multiplier.Value = value;
}

/// <summary>
/// Lookback of the fast moving average.
/// </summary>
public int FastPeriod
{
get => _fastPeriod.Value;
set => _fastPeriod.Value = value;
}

/// <summary>
/// Moving average method for the fast line.
/// </summary>
public MovingAverageMethods FastMethod
{
get => _fastMethod.Value;
set => _fastMethod.Value = value;
}

/// <summary>
/// Lookback of the slow moving average.
/// </summary>
public int SlowPeriod
{
get => _slowPeriod.Value;
set => _slowPeriod.Value = value;
}

/// <summary>
/// Moving average method for the slow line.
/// </summary>
public MovingAverageMethods SlowMethod
{
get => _slowMethod.Value;
set => _slowMethod.Value = value;
}

/// <summary>
/// Profit target expressed in MetaTrader pips.
/// </summary>
public int TakeProfitPips
{
get => _takeProfitPips.Value;
set => _takeProfitPips.Value = value;
}

/// <summary>
/// Require a fresh signal before placing averaging orders.
/// </summary>
public bool AveragingBySignal
{
get => _averagingBySignal.Value;
set => _averagingBySignal.Value = value;
}

/// <summary>
/// Distance between averaging layers expressed in pips.
/// </summary>
public decimal LayerDistancePips
{
get => _layerDistancePips.Value;
set => _layerDistancePips.Value = value;
}

/// <summary>
/// Maximum number of simultaneous orders allowed per direction.
/// </summary>
public int MaxLayers
{
get => _maxLayers.Value;
set => _maxLayers.Value = value;
}

/// <summary>
/// Enable or disable the trailing stop borrowed from the MQL expert.
/// </summary>
public bool EnableTrailing
{
get => _enableTrailing.Value;
set => _enableTrailing.Value = value;
}

/// <summary>
/// Profit required before the trailing stop activates.
/// </summary>
public decimal TrailingStartPips
{
get => _trailingStartPips.Value;
set => _trailingStartPips.Value = value;
}

/// <summary>
/// Minimal improvement required to move the trailing stop.
/// </summary>
public decimal TrailingStepPips
{
get => _trailingStepPips.Value;
set => _trailingStepPips.Value = value;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

if (FastPeriod >= SlowPeriod)
{
LogError("FastPeriod must be strictly less than SlowPeriod to detect crossovers correctly.");
Stop();
return;
}

_fastIndicator = CreateMovingAverage(FastMethod, FastPeriod);
_slowIndicator = CreateMovingAverage(SlowMethod, SlowPeriod);

_pointSize = 0m;
_previousFast = null;
_previousSlow = null;
_longTakeProfit = null;
_shortTakeProfit = null;
_longTrailingStop = null;
_shortTrailingStop = null;
_longEntries.Clear();
_shortEntries.Clear();

Volume = AdjustVolume(InitialVolume);

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(_fastIndicator, _slowIndicator, OnCandle)
.Start();
}

private void OnCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
{
if (candle.State != CandleStates.Finished)
return;

var signal = EvaluateSignal(fastValue, slowValue);

ManageOrders(signal, candle.ClosePrice);
CheckExits(candle);
}

private int EvaluateSignal(decimal fastValue, decimal slowValue)
{
if (_previousFast is null || _previousSlow is null)
{
_previousFast = fastValue;
_previousSlow = slowValue;
return 0;
}

var previousFast = _previousFast.Value;
var previousSlow = _previousSlow.Value;

_previousFast = fastValue;
_previousSlow = slowValue;

if (previousFast < previousSlow && fastValue >= slowValue)
return 1;

if (previousFast > previousSlow && fastValue <= slowValue)
return -1;

return 0;
}

private void ManageOrders(int signal, decimal price)
{
if (Security is null)
return;

// Opening trades follow the original logic: first trade reacts to the crossover,
// averaging trades are optional and depend on the adverse move distance.
if (signal > 0 && _longEntries.Count == 0)
{
var volume = CalculateOrderVolume(0);
if (volume > 0m)
BuyMarket(volume);
}
else if (signal < 0 && _shortEntries.Count == 0)
{
var volume = CalculateOrderVolume(0);
if (volume > 0m)
SellMarket(volume);
}

var layerDistance = LayerDistancePips * EnsurePointSize();
if (layerDistance <= 0m)
return;

// Buy averaging logic mirrors OrderSend in the MQL version.
if (_longEntries.Count > 0 && _longEntries.Count < MaxLayers)
{
var allowBySignal = !AveragingBySignal || signal > 0;
if (allowBySignal)
{
var lowestPrice = GetLowestPrice(_longEntries);
if (lowestPrice.HasValue && price <= lowestPrice.Value - layerDistance)
{
var volume = CalculateOrderVolume(_longEntries.Count);
if (volume > 0m)
BuyMarket(volume);
}
}
}

// Sell averaging is symmetrical to the buy branch.
if (_shortEntries.Count > 0 && _shortEntries.Count < MaxLayers)
{
var allowBySignal = !AveragingBySignal || signal < 0;
if (allowBySignal)
{
var highestPrice = GetHighestPrice(_shortEntries);
if (highestPrice.HasValue && price >= highestPrice.Value + layerDistance)
{
var volume = CalculateOrderVolume(_shortEntries.Count);
if (volume > 0m)
SellMarket(volume);
}
}
}
}

private void CheckExits(ICandleMessage candle)
{
var price = candle.ClosePrice;

// Basket take-profit approximates the OrderModify logic from MQL.
if (TakeProfitPips > 0)
{
if (_longTakeProfit.HasValue && _longEntries.Count > 0 && price >= _longTakeProfit.Value)
{
var volume = Math.Max(Position, 0m);
if (volume > 0m)
SellMarket(volume);
}

if (_shortTakeProfit.HasValue && _shortEntries.Count > 0 && price <= _shortTakeProfit.Value)
{
var volume = Math.Max(-Position, 0m);
if (volume > 0m)
BuyMarket(volume);
}
}

if (!EnableTrailing)
return;

var start = TrailingStartPips * EnsurePointSize();
var step = TrailingStepPips * EnsurePointSize();

// Trailing stop is only active when a single order is open, replicating the EA behaviour.
if (_longEntries.Count == 1 && start > 0m)
{
var entry = _longEntries[0];
if (price - entry.Price >= start)
{
var candidate = price - start;
if (!_longTrailingStop.HasValue || candidate - _longTrailingStop.Value >= Math.Max(step, EnsurePointSize()))
_longTrailingStop = candidate;
}

if (_longTrailingStop.HasValue && price <= _longTrailingStop.Value)
{
var volume = Math.Max(Position, 0m);
if (volume > 0m)
SellMarket(volume);
}
}
else
{
_longTrailingStop = null;
}

if (_shortEntries.Count == 1 && start > 0m)
{
var entry = _shortEntries[0];
if (entry.Price - price >= start)
{
var candidate = price + start;
if (!_shortTrailingStop.HasValue || _shortTrailingStop.Value - candidate >= Math.Max(step, EnsurePointSize()))
_shortTrailingStop = candidate;
}

if (_shortTrailingStop.HasValue && price >= _shortTrailingStop.Value)
{
var volume = Math.Max(-Position, 0m);
if (volume > 0m)
BuyMarket(volume);
}
}
else
{
_shortTrailingStop = null;
}
}

/// <inheritdoc />
protected override void OnNewMyTrade(MyTrade trade)
{
base.OnNewMyTrade(trade);

if (Security is null || trade.Order.Security != Security)
return;

var volume = trade.Trade.Volume;
if (volume <= 0m)
return;

if (trade.Order.Side == Sides.Buy)
{
var remainder = ReduceEntries(_shortEntries, volume);
if (remainder > 0m)
_longEntries.Add(new PositionEntry(trade.Trade.Price, remainder));
}
else if (trade.Order.Side == Sides.Sell)
{
var remainder = ReduceEntries(_longEntries, volume);
if (remainder > 0m)
_shortEntries.Add(new PositionEntry(trade.Trade.Price, remainder));
}

UpdateTargets();
}

private void UpdateTargets()
{
if (_longEntries.Count == 0)
{
_longTakeProfit = null;
_longTrailingStop = null;
}
else if (TakeProfitPips > 0)
{
var average = CalculateAveragePrice(_longEntries);
_longTakeProfit = average + TakeProfitPips * EnsurePointSize();
}
else
{
_longTakeProfit = null;
}

if (_shortEntries.Count == 0)
{
_shortTakeProfit = null;
_shortTrailingStop = null;
}
else if (TakeProfitPips > 0)
{
var average = CalculateAveragePrice(_shortEntries);
_shortTakeProfit = average - TakeProfitPips * EnsurePointSize();
}
else
{
_shortTakeProfit = null;
}
}

private decimal CalculateOrderVolume(int layerIndex)
{
var volume = InitialVolume;

if (LotSizing == LotSizingModes.Multiplier)
{
for (var i = 0; i < layerIndex; i++)
volume *= Multiplier;
}

return AdjustVolume(volume);
}

private decimal AdjustVolume(decimal volume)
{
if (Security is null)
return volume;

var minVolume = Security.MinVolume ?? 0m;
var maxVolume = Security.MaxVolume;
var step = Security.VolumeStep ?? 0m;

if (step > 0m)
{
var steps = Math.Round(volume / step, MidpointRounding.AwayFromZero);
volume = steps * step;
}

if (minVolume > 0m && volume < minVolume)
volume = minVolume;

if (maxVolume.HasValue && maxVolume.Value > 0m && volume > maxVolume.Value)
volume = maxVolume.Value;

return volume;
}

private IIndicator CreateMovingAverage(MovingAverageMethods method, int length)
{
return method switch
{
MovingAverageMethods.Simple => new SimpleMovingAverage { Length = length },
MovingAverageMethods.Exponential => new ExponentialMovingAverage { Length = length },
MovingAverageMethods.Smoothed => new SmoothedMovingAverage { Length = length },
MovingAverageMethods.LinearWeighted => new WeightedMovingAverage { Length = length },
_ => new SimpleMovingAverage { Length = length },
};
}

private decimal EnsurePointSize()
{
if (_pointSize > 0m)
return _pointSize;

var step = Security?.PriceStep ?? 0m;
if (step <= 0m)
{
_pointSize = 0.0001m;
return _pointSize;
}

var scaled = step;
var digits = 0;
while (scaled < 1m && digits < 10)
{
scaled *= 10m;
digits++;
}

var adjust = (digits % 2 == 1) ? 10m : 1m;
_pointSize = step * adjust;
return _pointSize;
}

private static decimal? GetLowestPrice(List<PositionEntry> entries)
{
if (entries.Count == 0)
return null;

var lowest = entries[0].Price;
for (var i = 1; i < entries.Count; i++)
{
if (entries[i].Price < lowest)
lowest = entries[i].Price;
}

return lowest;
}

private static decimal? GetHighestPrice(List<PositionEntry> entries)
{
if (entries.Count == 0)
return null;

var highest = entries[0].Price;
for (var i = 1; i < entries.Count; i++)
{
if (entries[i].Price > highest)
highest = entries[i].Price;
}

return highest;
}

private static decimal CalculateAveragePrice(List<PositionEntry> entries)
{
decimal totalVolume = 0m;
decimal weightedPrice = 0m;

foreach (var entry in entries)
{
totalVolume += entry.Volume;
weightedPrice += entry.Price * entry.Volume;
}

return totalVolume > 0m ? weightedPrice / totalVolume : 0m;
}

private static decimal ReduceEntries(List<PositionEntry> entries, decimal volume)
{
var remaining = volume;

while (remaining > 0m && entries.Count > 0)
{
var entry = entries[0];
var used = Math.Min(entry.Volume, remaining);
entry.Volume -= used;
remaining -= used;

if (entry.Volume <= 0m)
entries.RemoveAt(0);
}

return remaining;
}
}

