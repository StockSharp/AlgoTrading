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
/// Port of the MetaTrader strategy "More Orders After BreakEven".
/// Keeps opening additional long trades once earlier positions have been protected at break-even.
/// Tracks each entry individually to mimic MetaTrader ticket-based management including stop, take-profit, and break-even logic.
/// </summary>
public class MoreOrdersAfterBreakEvenStrategy : Strategy
{
private sealed class PositionEntry
{
public PositionEntry(decimal price, decimal volume, decimal stopPrice, decimal takeProfitPrice)
{
EntryPrice = price;
Volume = volume;
RemainingVolume = volume;
StopPrice = stopPrice;
TakeProfitPrice = takeProfitPrice;
}

public decimal EntryPrice { get; }

public decimal Volume { get; }

public decimal RemainingVolume { get; set; }

public decimal StopPrice { get; set; }

public decimal TakeProfitPrice { get; }

public bool BreakEvenReached { get; set; }

public bool IsClosing { get; set; }
}

private readonly StrategyParam<int> _maximumOrders;
private readonly StrategyParam<decimal> _takeProfitPips;
private readonly StrategyParam<decimal> _stopLossPips;
private readonly StrategyParam<decimal> _breakEvenPips;
private readonly StrategyParam<decimal> _tradeVolume;
private readonly StrategyParam<bool> _debugMode;

private readonly List<PositionEntry> _entries = new();

private decimal _bestBid;
private decimal _bestAsk;
private bool _hasBid;
private bool _hasAsk;
private decimal _pipSize;

/// <summary>
/// Maximum number of active orders that have not yet reached break-even protection.
/// </summary>
public int MaximumOrders
{
get => _maximumOrders.Value;
set => _maximumOrders.Value = value;
}

/// <summary>
/// Take-profit distance expressed in MetaTrader pips.
/// </summary>
public decimal TakeProfitPips
{
get => _takeProfitPips.Value;
set => _takeProfitPips.Value = value;
}

/// <summary>
/// Stop-loss distance expressed in MetaTrader pips.
/// </summary>
public decimal StopLossPips
{
get => _stopLossPips.Value;
set => _stopLossPips.Value = value;
}

/// <summary>
/// Break-even trigger distance in MetaTrader pips.
/// </summary>
public decimal BreakEvenPips
{
get => _breakEvenPips.Value;
set => _breakEvenPips.Value = value;
}

/// <summary>
/// Market volume for each new long order.
/// </summary>
public decimal TradeVolume
{
get => _tradeVolume.Value;
set => _tradeVolume.Value = value;
}

/// <summary>
/// Enables verbose diagnostic logging similar to MetaTrader's Comment output.
/// </summary>
public bool DebugMode
{
get => _debugMode.Value;
set => _debugMode.Value = value;
}

/// <summary>
/// Initializes a new instance of the <see cref="MoreOrdersAfterBreakEvenStrategy"/> class.
/// </summary>
public MoreOrdersAfterBreakEvenStrategy()
{
_maximumOrders = Param(nameof(MaximumOrders), 1)
.SetGreaterThanZero()
.SetDisplay("Maximum Orders", "Maximum number of pre-break-even positions allowed.", "Trading")
.SetCanOptimize(true)
.SetOptimize(1, 5, 1);

_takeProfitPips = Param(nameof(TakeProfitPips), 100m)
.SetNotNegative()
.SetDisplay("Take Profit (pips)", "Take-profit distance in MetaTrader pips.", "Risk")
.SetCanOptimize(true)
.SetOptimize(0m, 300m, 10m);

_stopLossPips = Param(nameof(StopLossPips), 200m)
.SetNotNegative()
.SetDisplay("Stop Loss (pips)", "Stop-loss distance in MetaTrader pips.", "Risk")
.SetCanOptimize(true)
.SetOptimize(0m, 400m, 10m);

_breakEvenPips = Param(nameof(BreakEvenPips), 10m)
.SetNotNegative()
.SetDisplay("Break-Even (pips)", "Distance before the stop is moved to the entry price.", "Risk")
.SetCanOptimize(true)
.SetOptimize(0m, 50m, 5m);

_tradeVolume = Param(nameof(TradeVolume), 0.01m)
.SetGreaterThanZero()
.SetDisplay("Trade Volume", "Volume submitted with every market buy order.", "Trading");

_debugMode = Param(nameof(DebugMode), true)
.SetDisplay("Debug Mode", "Log diagnostic messages that mirror the MetaTrader Comment output.", "Diagnostics");
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();

_entries.Clear();
_bestBid = 0m;
_bestAsk = 0m;
_hasBid = false;
_hasAsk = false;
_pipSize = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

EnsurePipSize();
StartProtection();

SubscribeLevel1()
.Bind(ProcessLevel1)
.Start();
}

private void ProcessLevel1(Level1ChangeMessage message)
{
if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj))
{
var bid = (decimal)bidObj;
if (bid > 0m)
{
_bestBid = bid;
_hasBid = true;
}
}

if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj))
{
var ask = (decimal)askObj;
if (ask > 0m)
{
_bestAsk = ask;
_hasAsk = true;
}
}

if (_hasBid && _hasAsk)
ProcessPrices();
}

private void ProcessPrices()
{
if (!IsFormedAndOnlineAndAllowTrading())
return;

var activeOrders = CountActivePreBreakEvenOrders();

if (activeOrders < MaximumOrders)
TryOpenNewOrder();

ApplyBreakEvenAndExits();

if (DebugMode)
LogInfo($"Active pre-break-even orders: {activeOrders}.");
}

private int CountActivePreBreakEvenOrders()
{
var count = 0;

for (var i = 0; i < _entries.Count; i++)
{
var entry = _entries[i];
if (entry.RemainingVolume > 0m && !entry.BreakEvenReached)
count++;
}

return count;
}

private void TryOpenNewOrder()
{
var volume = TradeVolume;
if (volume <= 0m)
return;

var order = BuyMarket(volume);

if (order != null && DebugMode)
LogInfo($"Submitting new long order for volume {volume} at ask {_bestAsk}.");
}

private void ApplyBreakEvenAndExits()
{
var bid = _bestBid;
var breakEvenDistance = GetBreakEvenDistance();

for (var i = 0; i < _entries.Count; i++)
{
var entry = _entries[i];

if (entry.RemainingVolume <= 0m)
continue;

if (!entry.BreakEvenReached && entry.StopPrice < entry.EntryPrice)
{
var triggerPrice = entry.EntryPrice + breakEvenDistance;
if (bid > triggerPrice)
{
entry.StopPrice = entry.EntryPrice;
entry.BreakEvenReached = true;

if (DebugMode)
LogInfo($"Break-even activated for entry at {entry.EntryPrice}.");
}
}

if (entry.IsClosing)
continue;

if (entry.TakeProfitPrice > 0m && bid >= entry.TakeProfitPrice)
{
entry.IsClosing = true;
SellMarket(entry.RemainingVolume);

if (DebugMode)
LogInfo($"Take-profit reached for entry at {entry.EntryPrice} (target {entry.TakeProfitPrice}).");

continue;
}

if (entry.StopPrice > 0m && bid <= entry.StopPrice)
{
entry.IsClosing = true;
SellMarket(entry.RemainingVolume);

if (DebugMode)
LogInfo($"Stop-loss triggered for entry at {entry.EntryPrice} (stop {entry.StopPrice}).");
}
}
}

/// <inheritdoc />
protected override void OnOwnTradeReceived(MyTrade trade)
{
base.OnOwnTradeReceived(trade);

if (trade?.Order == null || trade.Order.Security != Security)
return;

var volume = trade.Trade.Volume;
if (volume <= 0m)
return;

if (trade.Order.Side == Sides.Buy)
{
RegisterEntry(trade.Trade.Price, volume);
}
else if (trade.Order.Side == Sides.Sell)
{
ReduceEntries(volume);
}
}

private void RegisterEntry(decimal price, decimal volume)
{
var pip = EnsurePipSize();
var stopDistance = StopLossPips > 0m ? StopLossPips * pip : 0m;
var takeProfitDistance = TakeProfitPips > 0m ? TakeProfitPips * pip : 0m;

var stop = stopDistance > 0m ? price - stopDistance : 0m;
var takeProfit = takeProfitDistance > 0m ? price + takeProfitDistance : 0m;

var entry = new PositionEntry(price, volume, stop, takeProfit);
_entries.Add(entry);

if (DebugMode)
LogInfo($"Registered long entry at {price} with stop {stop} and take-profit {takeProfit}.");
}

private void ReduceEntries(decimal volume)
{
var remaining = volume;

for (var i = 0; i < _entries.Count && remaining > 0m; i++)
{
var entry = _entries[i];
if (entry.RemainingVolume <= 0m)
continue;

var used = Math.Min(entry.RemainingVolume, remaining);
entry.RemainingVolume -= used;
remaining -= used;

entry.IsClosing = false;

if (entry.RemainingVolume <= 0m)
{
_entries.RemoveAt(i);
i--;
}
}

if (remaining > 0m && DebugMode)
LogInfo($"Received sell volume {volume} exceeding tracked long entries by {remaining}.");
}

private decimal GetBreakEvenDistance()
{
var pip = EnsurePipSize();
return BreakEvenPips > 0m ? BreakEvenPips * pip : 0m;
}

private decimal EnsurePipSize()
{
if (_pipSize > 0m)
return _pipSize;

var security = Security;
if (security == null)
return 0m;

var step = security.PriceStep ?? 0m;
if (step <= 0m)
return 0m;

var decimals = security.Decimals;
if (decimals % 2 == 1)
step *= 10m;

_pipSize = step;
return _pipSize;
}
}

