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

using System.Globalization;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// High level port of the "XP Trade Manager Grid" expert advisor.
/// The strategy manages a martingale-like averaging grid where orders are
/// added after a fixed adverse move and profit targets depend on the order number.
/// </summary>
public class XpTradeManagerGridStrategy : Strategy
{
private readonly StrategyParam<int> _addNewTradeAfter;
private readonly StrategyParam<int> _takeProfit1Total;
private readonly StrategyParam<int> _takeProfit1Partitive;
private readonly StrategyParam<int> _takeProfit1Offset;
private readonly StrategyParam<int> _takeProfit2;
private readonly StrategyParam<int> _takeProfit3;
private readonly StrategyParam<int> _takeProfit4Total;
private readonly StrategyParam<int> _takeProfit5Total;
private readonly StrategyParam<int> _takeProfit6Total;
private readonly StrategyParam<int> _takeProfit7Total;
private readonly StrategyParam<int> _takeProfit8Total;
private readonly StrategyParam<int> _takeProfit9Total;
private readonly StrategyParam<int> _takeProfit10Total;
private readonly StrategyParam<int> _takeProfit11Total;
private readonly StrategyParam<int> _takeProfit12Total;
private readonly StrategyParam<int> _takeProfit13Total;
private readonly StrategyParam<int> _takeProfit14Total;
private readonly StrategyParam<int> _takeProfit15Total;
private readonly StrategyParam<int> _maxOrders;
private readonly StrategyParam<decimal> _riskPercent;
private readonly StrategyParam<decimal> _orderVolume;
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<bool> _autoRenewFirstOrder;

private readonly List<GridEntry> _buyEntries = new();
private readonly List<GridEntry> _sellEntries = new();
private readonly HashSet<int> _pendingBuyStages = new();
private readonly HashSet<int> _pendingSellStages = new();

private decimal _stage1ProfitPips;
private decimal _stage1ProfitCurrency;
private decimal? _lastStage1TakeProfitPrice;
private Sides? _lastStage1Direction;

private decimal _closingBuyVolume;
private decimal _closingSellVolume;

/// <summary>
/// Initializes strategy parameters with defaults from the MetaTrader version.
/// </summary>
public XpTradeManagerGridStrategy()
{
_addNewTradeAfter = Param(nameof(AddNewTradeAfter), 50)
.SetNotNegative()
.SetDisplay("Grid Step", "Distance in points before adding the next averaging order.", "Grid");

_takeProfit1Total = Param(nameof(TakeProfit1Total), 150)
.SetNotNegative()
.SetDisplay("TP1 Total", "Total profit target in points collected by the first order.", "Targets");

_takeProfit1Partitive = Param(nameof(TakeProfit1Partitive), 10)
.SetNotNegative()
.SetDisplay("TP1 Partial", "Partial take profit distance (points) assigned to the first order.", "Targets");

_takeProfit1Offset = Param(nameof(TakeProfit1Offset), 3)
.SetNotNegative()
.SetDisplay("TP1 Offset", "Minimum distance from the last TP before a new first order is allowed.", "Targets");

_takeProfit2 = Param(nameof(TakeProfit2), 40)
.SetNotNegative()
.SetDisplay("TP2", "Take profit distance for the second averaging order.", "Targets");

_takeProfit3 = Param(nameof(TakeProfit3), 50)
.SetNotNegative()
.SetDisplay("TP3", "Take profit distance for the third averaging order.", "Targets");

_takeProfit4Total = Param(nameof(TakeProfit4Total), 60)
.SetNotNegative()
.SetDisplay("TP4 Total", "Total profit (points) required when four orders are open.", "Targets");

_takeProfit5Total = Param(nameof(TakeProfit5Total), 70)
.SetNotNegative()
.SetDisplay("TP5 Total", "Total profit (points) required when five orders are open.", "Targets");

_takeProfit6Total = Param(nameof(TakeProfit6Total), 80)
.SetNotNegative()
.SetDisplay("TP6 Total", "Total profit (points) required when six orders are open.", "Targets");

_takeProfit7Total = Param(nameof(TakeProfit7Total), 90)
.SetNotNegative()
.SetDisplay("TP7 Total", "Total profit (points) required when seven orders are open.", "Targets");

_takeProfit8Total = Param(nameof(TakeProfit8Total), 100)
.SetNotNegative()
.SetDisplay("TP8 Total", "Total profit (points) required when eight orders are open.", "Targets");

_takeProfit9Total = Param(nameof(TakeProfit9Total), 120)
.SetNotNegative()
.SetDisplay("TP9 Total", "Total profit (points) required when nine orders are open.", "Targets");

_takeProfit10Total = Param(nameof(TakeProfit10Total), 150)
.SetNotNegative()
.SetDisplay("TP10 Total", "Total profit (points) required when ten orders are open.", "Targets");

_takeProfit11Total = Param(nameof(TakeProfit11Total), 180)
.SetNotNegative()
.SetDisplay("TP11 Total", "Total profit (points) required when eleven orders are open.", "Targets");

_takeProfit12Total = Param(nameof(TakeProfit12Total), 200)
.SetNotNegative()
.SetDisplay("TP12 Total", "Total profit (points) required when twelve orders are open.", "Targets");

_takeProfit13Total = Param(nameof(TakeProfit13Total), 220)
.SetNotNegative()
.SetDisplay("TP13 Total", "Total profit (points) required when thirteen orders are open.", "Targets");

_takeProfit14Total = Param(nameof(TakeProfit14Total), 250)
.SetNotNegative()
.SetDisplay("TP14 Total", "Total profit (points) required when fourteen orders are open.", "Targets");

_takeProfit15Total = Param(nameof(TakeProfit15Total), 300)
.SetNotNegative()
.SetDisplay("TP15 Total", "Total profit (points) required when fifteen orders are open.", "Targets");

_maxOrders = Param(nameof(MaxOrders), 15)
.SetGreaterThanZero()
.SetDisplay("Max Orders", "Maximum number of averaging orders per direction.", "Risk");

_riskPercent = Param(nameof(RiskPercent), 100m)
.SetNotNegative()
.SetDisplay("Risk %", "Maximum open loss in percent of the account value.", "Risk");

_orderVolume = Param(nameof(OrderVolume), 0.01m)
.SetNotNegative()
.SetDisplay("Order Volume", "Volume used for every averaging trade.", "Trading");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Data type used to trigger the management logic.", "General");

_autoRenewFirstOrder = Param(nameof(AutoRenewFirstOrder), true)
.SetDisplay("Auto Renew", "Automatically reopen the first order after a TP as in the EA.", "Automation");
}

/// <summary>
/// Distance in points before adding the next averaging order.
/// </summary>
public int AddNewTradeAfter
{
get => _addNewTradeAfter.Value;
set => _addNewTradeAfter.Value = value;
}

/// <summary>
/// Total profit target in points collected by the first order.
/// </summary>
public int TakeProfit1Total
{
get => _takeProfit1Total.Value;
set => _takeProfit1Total.Value = value;
}

/// <summary>
/// Partial take profit distance (points) assigned to the first order.
/// </summary>
public int TakeProfit1Partitive
{
get => _takeProfit1Partitive.Value;
set => _takeProfit1Partitive.Value = value;
}

/// <summary>
/// Minimum distance from the last TP before a new first order is allowed.
/// </summary>
public int TakeProfit1Offset
{
get => _takeProfit1Offset.Value;
set => _takeProfit1Offset.Value = value;
}

/// <summary>
/// Take profit distance for the second averaging order.
/// </summary>
public int TakeProfit2
{
get => _takeProfit2.Value;
set => _takeProfit2.Value = value;
}

/// <summary>
/// Take profit distance for the third averaging order.
/// </summary>
public int TakeProfit3
{
get => _takeProfit3.Value;
set => _takeProfit3.Value = value;
}

/// <summary>
/// Total profit (points) required when four orders are open.
/// </summary>
public int TakeProfit4Total
{
get => _takeProfit4Total.Value;
set => _takeProfit4Total.Value = value;
}

/// <summary>
/// Total profit (points) required when five orders are open.
/// </summary>
public int TakeProfit5Total
{
get => _takeProfit5Total.Value;
set => _takeProfit5Total.Value = value;
}

/// <summary>
/// Total profit (points) required when six orders are open.
/// </summary>
public int TakeProfit6Total
{
get => _takeProfit6Total.Value;
set => _takeProfit6Total.Value = value;
}

/// <summary>
/// Total profit (points) required when seven orders are open.
/// </summary>
public int TakeProfit7Total
{
get => _takeProfit7Total.Value;
set => _takeProfit7Total.Value = value;
}

/// <summary>
/// Total profit (points) required when eight orders are open.
/// </summary>
public int TakeProfit8Total
{
get => _takeProfit8Total.Value;
set => _takeProfit8Total.Value = value;
}

/// <summary>
/// Total profit (points) required when nine orders are open.
/// </summary>
public int TakeProfit9Total
{
get => _takeProfit9Total.Value;
set => _takeProfit9Total.Value = value;
}

/// <summary>
/// Total profit (points) required when ten orders are open.
/// </summary>
public int TakeProfit10Total
{
get => _takeProfit10Total.Value;
set => _takeProfit10Total.Value = value;
}

/// <summary>
/// Total profit (points) required when eleven orders are open.
/// </summary>
public int TakeProfit11Total
{
get => _takeProfit11Total.Value;
set => _takeProfit11Total.Value = value;
}

/// <summary>
/// Total profit (points) required when twelve orders are open.
/// </summary>
public int TakeProfit12Total
{
get => _takeProfit12Total.Value;
set => _takeProfit12Total.Value = value;
}

/// <summary>
/// Total profit (points) required when thirteen orders are open.
/// </summary>
public int TakeProfit13Total
{
get => _takeProfit13Total.Value;
set => _takeProfit13Total.Value = value;
}

/// <summary>
/// Total profit (points) required when fourteen orders are open.
/// </summary>
public int TakeProfit14Total
{
get => _takeProfit14Total.Value;
set => _takeProfit14Total.Value = value;
}

/// <summary>
/// Total profit (points) required when fifteen orders are open.
/// </summary>
public int TakeProfit15Total
{
get => _takeProfit15Total.Value;
set => _takeProfit15Total.Value = value;
}

/// <summary>
/// Maximum number of averaging orders per direction.
/// </summary>
public int MaxOrders
{
get => _maxOrders.Value;
set => _maxOrders.Value = value;
}

/// <summary>
/// Maximum open loss in percent of the account value.
/// </summary>
public decimal RiskPercent
{
get => _riskPercent.Value;
set => _riskPercent.Value = value;
}

/// <summary>
/// Volume used for every averaging trade.
/// </summary>
public decimal OrderVolume
{
get => _orderVolume.Value;
set => _orderVolume.Value = value;
}

/// <summary>
/// Data type used to trigger the management logic.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Automatically reopen the first order after a TP as in the EA.
/// </summary>
public bool AutoRenewFirstOrder
{
get => _autoRenewFirstOrder.Value;
set => _autoRenewFirstOrder.Value = value;
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
return [(Security, CandleType)];
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();

_buyEntries.Clear();
_sellEntries.Clear();
_pendingBuyStages.Clear();
_pendingSellStages.Clear();
_stage1ProfitPips = 0m;
_stage1ProfitCurrency = 0m;
_lastStage1TakeProfitPrice = null;
_lastStage1Direction = null;
_closingBuyVolume = 0m;
_closingSellVolume = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(candle => ProcessCandle(candle))
.Start();
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var currentPrice = candle.ClosePrice;
if (currentPrice <= 0m)
return;

TryRenewFirstOrder(currentPrice);
CheckStageTargets(currentPrice);
CheckGridExpansion(currentPrice);
CheckAggregateTakeProfit(currentPrice);
CheckRisk(currentPrice);
}

private void TryRenewFirstOrder(decimal currentPrice)
{
if (!AutoRenewFirstOrder)
return;

if (_buyEntries.Count > 0 || _sellEntries.Count > 0)
return;

if (_lastStage1Direction is not { } direction || _lastStage1TakeProfitPrice is not { } tpPrice)
return;

if (_stage1ProfitPips >= TakeProfit1Total)
return;

var step = GetPriceStep();
if (step <= 0m)
return;

var offset = TakeProfit1Offset * step;
if (offset <= 0m)
return;

if (Math.Abs(currentPrice - tpPrice) < offset)
return;

if (direction == Sides.Buy)
{
PlaceGridOrder(Sides.Buy, 1);
}
else if (direction == Sides.Sell)
{
PlaceGridOrder(Sides.Sell, 1);
}
}

private void CheckStageTargets(decimal currentPrice)
{
var step = GetPriceStep();
if (step <= 0m)
return;

var takeProfit1Distance = TakeProfit1Partitive * step;
var takeProfit2Distance = TakeProfit2 * step;
var takeProfit3Distance = TakeProfit3 * step;

if (takeProfit1Distance > 0m)
CheckStageTarget(_buyEntries, 1, currentPrice, takeProfit1Distance, Sides.Buy);

if (takeProfit1Distance > 0m)
CheckStageTarget(_sellEntries, 1, currentPrice, takeProfit1Distance, Sides.Sell);

if (takeProfit2Distance > 0m)
{
CheckStageTarget(_buyEntries, 2, currentPrice, takeProfit2Distance, Sides.Buy);
CheckStageTarget(_sellEntries, 2, currentPrice, takeProfit2Distance, Sides.Sell);
}

if (takeProfit3Distance > 0m)
{
CheckStageTarget(_buyEntries, 3, currentPrice, takeProfit3Distance, Sides.Buy);
CheckStageTarget(_sellEntries, 3, currentPrice, takeProfit3Distance, Sides.Sell);
}
}

private void CheckStageTarget(List<GridEntry> entries, int stage, decimal currentPrice, decimal distance, Sides direction)
{
var entry = entries.FirstOrDefault(e => e.Stage == stage);
if (entry == null)
return;

var reached = direction == Sides.Buy
? currentPrice >= entry.EntryPrice + distance
: currentPrice <= entry.EntryPrice - distance;

if (!reached)
return;

CloseStage(entry, direction);
}

private void CheckGridExpansion(decimal currentPrice)
{
var step = GetPriceStep();
if (step <= 0m)
return;

var spacing = AddNewTradeAfter * step;
if (spacing <= 0m)
return;

var askPrice = GetAskPrice(currentPrice);
var bidPrice = GetBidPrice(currentPrice);

TryAddNextStage(_buyEntries, Sides.Buy, askPrice, spacing, _pendingBuyStages);
TryAddNextStage(_sellEntries, Sides.Sell, bidPrice, spacing, _pendingSellStages);
}

private void TryAddNextStage(List<GridEntry> entries, Sides direction, decimal marketPrice, decimal spacing, HashSet<int> pendingStages)
{
var count = entries.Count;
if (count == 0)
return;

var lastStage = entries.Max(e => e.Stage);
if (lastStage >= MaxOrders)
return;

var lastEntry = entries.First(e => e.Stage == lastStage);
var diff = direction == Sides.Buy
? lastEntry.EntryPrice - marketPrice
: marketPrice - lastEntry.EntryPrice;

if (diff < spacing)
return;

var nextStage = lastStage + 1;
if (pendingStages.Contains(nextStage))
return;

PlaceGridOrder(direction, nextStage);
}

private void CheckAggregateTakeProfit(decimal currentPrice)
{
var countBuy = _buyEntries.Count;
var countSell = _sellEntries.Count;
var total = countBuy + countSell;

if (total < 4)
return;

var step = GetPriceStep();
if (step <= 0m)
return;

var takeProfitTotal = GetTakeProfitTotal(total);
if (takeProfitTotal <= 0)
return;

var direction = countBuy >= countSell ? Sides.Buy : Sides.Sell;
var entries = direction == Sides.Buy ? _buyEntries : _sellEntries;
var average = CalculateAveragePrice(entries);
if (average == null)
return;

var targetOffset = takeProfitTotal / (decimal)total * step;
var target = direction == Sides.Buy
? average.Value + targetOffset
: average.Value - targetOffset;

var reached = direction == Sides.Buy
? currentPrice >= target
: currentPrice <= target;

if (!reached)
return;

CloseEntireGrid("breakeven_exit");
}

private void CheckRisk(decimal currentPrice)
{
if (RiskPercent <= 0m)
return;

var step = GetPriceStep();
var stepPrice = GetStepPrice();
if (step <= 0m || stepPrice <= 0m)
return;

var floating = 0m;
foreach (var entry in _buyEntries)
{
var diff = currentPrice - entry.EntryPrice;
floating += diff / step * stepPrice * entry.Volume;
}

foreach (var entry in _sellEntries)
{
var diff = entry.EntryPrice - currentPrice;
floating += diff / step * stepPrice * entry.Volume;
}

var portfolioValue = Portfolio?.CurrentValue ?? Portfolio?.CurrentBalance ?? Portfolio?.BeginValue ?? 0m;
if (portfolioValue <= 0m)
return;

var maxLoss = -portfolioValue * RiskPercent / 100m;
if (floating < maxLoss)
{
CloseEntireGrid("risk_exit");
}
}

private void CloseStage(GridEntry entry, Sides direction)
{
if (entry.Volume <= 0m)
return;

var comment = $"order{entry.Stage}_tp";
if (direction == Sides.Buy)
{
SellMarket(entry.Volume, text: comment);
}
else
{
BuyMarket(entry.Volume, text: comment);
}
}

private void CloseEntireGrid(string reason)
{
var buyVolume = _buyEntries.Sum(e => e.Volume);
var sellVolume = _sellEntries.Sum(e => e.Volume);

if (buyVolume > 0m)
{
_closingBuyVolume += buyVolume;
SellMarket(buyVolume, text: reason);
}

if (sellVolume > 0m)
{
_closingSellVolume += sellVolume;
BuyMarket(sellVolume, text: reason);
}
}

private void PlaceGridOrder(Sides direction, int stage)
{
if (OrderVolume <= 0m)
return;

if (stage == 1 && _buyEntries.Count == 0 && _sellEntries.Count == 0 && _pendingBuyStages.Count == 0 && _pendingSellStages.Count == 0)
{
_stage1ProfitPips = 0m;
_stage1ProfitCurrency = 0m;
}

var comment = $"order{stage}";
if (direction == Sides.Buy)
{
_pendingBuyStages.Add(stage);
BuyMarket(OrderVolume, text: comment);
}
else
{
_pendingSellStages.Add(stage);
SellMarket(OrderVolume, text: comment);
}
}

private decimal GetPriceStep()
{
var step = Security?.PriceStep ?? 0m;
return step > 0m ? step : 0.0001m;
}

private decimal GetStepPrice()
{
var stepPrice = Security?.StepPrice ?? 0m;
return stepPrice > 0m ? stepPrice : 1m;
}

private decimal GetAskPrice(decimal currentPrice)
{
var ask = Security?.BestAsk?.Price ?? 0m;
return ask > 0m ? ask : currentPrice;
}

private decimal GetBidPrice(decimal currentPrice)
{
var bid = Security?.BestBid?.Price ?? 0m;
return bid > 0m ? bid : currentPrice;
}

private decimal? CalculateAveragePrice(IEnumerable<GridEntry> entries)
{
var list = entries.ToList();
if (list.Count == 0)
return null;

var sumVolume = list.Sum(e => e.Volume);
if (sumVolume <= 0m)
return null;

var weighted = list.Sum(e => e.EntryPrice * e.Volume);
return weighted / sumVolume;
}

private int GetTakeProfitTotal(int totalOrders)
{
return totalOrders switch
{
4 => TakeProfit4Total,
5 => TakeProfit5Total,
6 => TakeProfit6Total,
7 => TakeProfit7Total,
8 => TakeProfit8Total,
9 => TakeProfit9Total,
10 => TakeProfit10Total,
11 => TakeProfit11Total,
12 => TakeProfit12Total,
13 => TakeProfit13Total,
14 => TakeProfit14Total,
15 => TakeProfit15Total,
_ => TakeProfit15Total,
};
}

/// <inheritdoc />
protected override void OnOrderRegisterFailed(Order order, OrderFail fail)
{
base.OnOrderRegisterFailed(order, fail);
ReleasePendingStage(order);
}

/// <inheritdoc />
protected override void OnOrderCancelled(Order order)
{
base.OnOrderCancelled(order);
ReleasePendingStage(order);
}

private void ReleasePendingStage(Order order)
{
if (order?.Comment == null)
return;

if (order.Comment.StartsWith("order", true, CultureInfo.InvariantCulture))
{
var stage = ExtractStage(order.Comment);
if (stage == null)
return;

if (order.Side == Sides.Buy)
_pendingBuyStages.Remove(stage.Value);
else if (order.Side == Sides.Sell)
_pendingSellStages.Remove(stage.Value);
}
}

/// <inheritdoc />
protected override void OnNewMyTrade(MyTrade trade)
{
base.OnNewMyTrade(trade);

var order = trade.Order;
var execution = trade.Trade;
if (order == null || execution == null)
return;

var volume = trade.Volume;
if (volume <= 0m)
return;

var comment = order.Comment ?? string.Empty;

if (comment.StartsWith("order", true, CultureInfo.InvariantCulture) && !comment.Contains("_tp", StringComparison.Ordinal))
{
var stage = ExtractStage(comment);
if (stage == null)
return;

var entry = new GridEntry(stage.Value, execution.Price, volume);
if (order.Side == Sides.Buy)
{
_pendingBuyStages.Remove(stage.Value);
AddEntry(_buyEntries, entry);
}
else if (order.Side == Sides.Sell)
{
_pendingSellStages.Remove(stage.Value);
AddEntry(_sellEntries, entry);
}

return;
}

if (comment.Contains("_tp", StringComparison.Ordinal))
{
var stage = ExtractStage(comment);
if (stage == null)
return;

HandleTakeProfit(order.Side, stage.Value, execution.Price, volume);
return;
}

if (comment.EqualsIgnoreCase("breakeven_exit") ||
comment.EqualsIgnoreCase("risk_exit"))
{
HandleForcedExit(order.Side, execution.Price, volume);
}
}

private void HandleTakeProfit(Sides exitSide, int stage, decimal exitPrice, decimal volume)
{
var entries = exitSide == Sides.Sell ? _buyEntries : _sellEntries;
var entry = entries.FirstOrDefault(e => e.Stage == stage);
if (entry == null)
return;

var step = GetPriceStep();
var stepPrice = GetStepPrice();
var diff = exitSide == Sides.Sell
? exitPrice - entry.EntryPrice
: entry.EntryPrice - exitPrice;

var pips = Math.Abs(diff) / step;
var money = pips * stepPrice * entry.Volume;

if (stage == 1)
{
_stage1ProfitPips += pips;
_stage1ProfitCurrency += money;
_lastStage1TakeProfitPrice = exitPrice;
_lastStage1Direction = exitSide == Sides.Sell ? Sides.Buy : Sides.Sell;
}

entries.Remove(entry);
}

private void HandleForcedExit(Sides exitSide, decimal exitPrice, decimal volume)
{
if (exitSide == Sides.Sell)
{
_closingBuyVolume -= volume;
if (_closingBuyVolume <= 0m)
{
FinalizeForcedExit(_buyEntries, exitPrice, Sides.Buy);
_closingBuyVolume = 0m;
}
}
else if (exitSide == Sides.Buy)
{
_closingSellVolume -= volume;
if (_closingSellVolume <= 0m)
{
FinalizeForcedExit(_sellEntries, exitPrice, Sides.Sell);
_closingSellVolume = 0m;
}
}
}

private void FinalizeForcedExit(List<GridEntry> entries, decimal exitPrice, Sides direction)
{
var step = GetPriceStep();
var stepPrice = GetStepPrice();

foreach (var entry in entries.ToArray())
{
var diff = direction == Sides.Buy
? exitPrice - entry.EntryPrice
: entry.EntryPrice - exitPrice;

var pips = Math.Abs(diff) / step;
var money = pips * stepPrice * entry.Volume;
if (entry.Stage == 1)
{
_stage1ProfitPips += pips;
_stage1ProfitCurrency += money;
_lastStage1TakeProfitPrice = exitPrice;
_lastStage1Direction = direction;
}
}

entries.Clear();
}

private void AddEntry(List<GridEntry> entries, GridEntry entry)
{
var existing = entries.FirstOrDefault(e => e.Stage == entry.Stage);
if (existing != null)
{
existing.EntryPrice = entry.EntryPrice;
existing.Volume += entry.Volume;
}
else
{
entries.Add(entry);
}
}

private static int? ExtractStage(string comment)
{
var digits = new string(comment.Where(char.IsDigit).ToArray());
if (digits.IsEmpty())
return null;

if (int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stage))
return stage;

return null;
}

private sealed class GridEntry
{
public GridEntry(int stage, decimal entryPrice, decimal volume)
{
Stage = stage;
EntryPrice = entryPrice;
Volume = volume;
}

public int Stage { get; }
public decimal EntryPrice { get; set; }
public decimal Volume { get; set; }
}
}

