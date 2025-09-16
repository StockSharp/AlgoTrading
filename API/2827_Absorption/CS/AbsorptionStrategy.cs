using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Absorption outside bar breakout strategy.
/// Detects engulfing candles near recent extremes and places stop orders around the pattern.
/// </summary>
public class AbsorptionStrategy : Strategy
{
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<int> _maxSearch;
private readonly StrategyParam<decimal> _takeProfitBuy;
private readonly StrategyParam<decimal> _takeProfitSell;
private readonly StrategyParam<decimal> _trailingStop;
private readonly StrategyParam<decimal> _trailingStep;
private readonly StrategyParam<decimal> _indent;
private readonly StrategyParam<int> _orderExpirationHours;
private readonly StrategyParam<decimal> _breakeven;
private readonly StrategyParam<decimal> _breakevenProfit;

private Highest _highest;
private Lowest _lowest;

private ICandleMessage _prev1;
private ICandleMessage _prev2;

private bool _hasActiveOrders;
private decimal _pendingHigh;
private decimal _pendingLow;
private decimal _pendingBuyPrice;
private decimal _pendingSellPrice;
private decimal _pendingBuyStopLoss;
private decimal _pendingSellStopLoss;
private decimal _pendingBuyTakeProfit;
private decimal _pendingSellTakeProfit;
private DateTimeOffset? _ordersExpiry;

private decimal _entryPrice;
private decimal _stopLoss;
private decimal _takeProfit;
private decimal _prevPosition;
private bool _exitRequestActive;

/// <summary>
/// Candle type to analyze.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Number of candles to inspect for extreme prices.
/// </summary>
public int MaxSearch
{
get => _maxSearch.Value;
set => _maxSearch.Value = value;
}

/// <summary>
/// Take profit distance for long trades in price steps.
/// </summary>
public decimal TakeProfitBuy
{
get => _takeProfitBuy.Value;
set => _takeProfitBuy.Value = value;
}

/// <summary>
/// Take profit distance for short trades in price steps.
/// </summary>
public decimal TakeProfitSell
{
get => _takeProfitSell.Value;
set => _takeProfitSell.Value = value;
}

/// <summary>
/// Trailing stop distance in price steps.
/// </summary>
public decimal TrailingStop
{
get => _trailingStop.Value;
set => _trailingStop.Value = value;
}

/// <summary>
/// Minimal step for trailing stop updates in price steps.
/// </summary>
public decimal TrailingStep
{
get => _trailingStep.Value;
set => _trailingStep.Value = value;
}

/// <summary>
/// Indent distance around the reference candle in price steps.
/// </summary>
public decimal Indent
{
get => _indent.Value;
set => _indent.Value = value;
}

/// <summary>
/// Pending order expiration in hours.
/// </summary>
public int OrderExpirationHours
{
get => _orderExpirationHours.Value;
set => _orderExpirationHours.Value = value;
}

/// <summary>
/// Distance to move stop-loss to breakeven in price steps.
/// </summary>
public decimal Breakeven
{
get => _breakeven.Value;
set => _breakeven.Value = value;
}

/// <summary>
/// Profit needed before breakeven activation in price steps.
/// </summary>
public decimal BreakevenProfit
{
get => _breakevenProfit.Value;
set => _breakevenProfit.Value = value;
}

/// <summary>
/// Initializes <see cref="AbsorptionStrategy"/>.
/// </summary>
public AbsorptionStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
.SetDisplay("Candle Type", "Type of candles to process", "General");

_maxSearch = Param(nameof(MaxSearch), 10)
.SetGreaterThanZero()
.SetDisplay("Search Depth", "Bars to inspect for extremes", "Pattern");

_takeProfitBuy = Param(nameof(TakeProfitBuy), 10m)
.SetNotNegative()
.SetDisplay("Long TP", "Take profit for long trades (steps)", "Risk");

_takeProfitSell = Param(nameof(TakeProfitSell), 10m)
.SetNotNegative()
.SetDisplay("Short TP", "Take profit for short trades (steps)", "Risk");

_trailingStop = Param(nameof(TrailingStop), 5m)
.SetNotNegative()
.SetDisplay("Trailing Stop", "Trailing stop distance (steps)", "Risk");

_trailingStep = Param(nameof(TrailingStep), 5m)
.SetNotNegative()
.SetDisplay("Trailing Step", "Minimal move to update trailing stop (steps)", "Risk");

_indent = Param(nameof(Indent), 1m)
.SetNotNegative()
.SetDisplay("Indent", "Offset from high/low for entries (steps)", "Pattern");

_orderExpirationHours = Param(nameof(OrderExpirationHours), 8)
.SetGreaterThanZero()
.SetDisplay("Order Expiration", "Validity of pending orders in hours", "Pattern");

_breakeven = Param(nameof(Breakeven), 1m)
.SetNotNegative()
.SetDisplay("Breakeven", "Stop offset once breakeven triggers (steps)", "Risk");

_breakevenProfit = Param(nameof(BreakevenProfit), 10m)
.SetNotNegative()
.SetDisplay("Breakeven Profit", "Profit needed before moving to breakeven (steps)", "Risk");

Volume = 0.1m;
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

_prev1 = null;
_prev2 = null;
_hasActiveOrders = false;
_ordersExpiry = null;
_pendingHigh = 0m;
_pendingLow = 0m;
_pendingBuyPrice = 0m;
_pendingSellPrice = 0m;
_pendingBuyStopLoss = 0m;
_pendingSellStopLoss = 0m;
_pendingBuyTakeProfit = 0m;
_pendingSellTakeProfit = 0m;
_entryPrice = 0m;
_stopLoss = 0m;
_takeProfit = 0m;
_prevPosition = 0m;
_exitRequestActive = false;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

if (TrailingStop > 0m && TrailingStep <= 0m)
throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");

if (Breakeven > 0m)
{
if (BreakevenProfit <= 0m)
throw new InvalidOperationException("Breakeven profit must be positive when breakeven is enabled.");

if (BreakevenProfit <= Breakeven)
throw new InvalidOperationException("Breakeven profit must exceed breakeven distance.");
}

_highest = new Highest { Length = MaxSearch, CandlePrice = CandlePrice.High };
_lowest = new Lowest { Length = MaxSearch, CandlePrice = CandlePrice.Low };

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(_highest, _lowest, ProcessCandle)
.Start();

StartProtection();
}

private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue)
{
if (candle.State != CandleStates.Finished)
return;

HandlePositionChange();
ManageActivePosition(candle);

if (_hasActiveOrders && _ordersExpiry.HasValue && candle.CloseTime >= _ordersExpiry.Value)
{
CancelActiveOrders();
ClearPendingOrders();
}

if (!IsFormedAndOnlineAndAllowTrading())
{
UpdatePreviousCandles(candle);
_prevPosition = Position;
return;
}

if (Position == 0 && !_hasActiveOrders && _prev1 != null && _prev2 != null && _highest.IsFormed && _lowest.IsFormed)
{
TryPlaceOrders(candle, highestValue, lowestValue);
}

UpdatePreviousCandles(candle);

if (Position != 0 && _hasActiveOrders)
{
CancelActiveOrders();
ClearPendingOrders();
}

_prevPosition = Position;
}

private void TryPlaceOrders(ICandleMessage candle, decimal highestValue, decimal lowestValue)
{
var prev2Outside = _prev2.HighPrice > _prev1.HighPrice && _prev2.LowPrice < _prev1.LowPrice;
var prev1Outside = _prev1.HighPrice > _prev2.HighPrice && _prev1.LowPrice < _prev2.LowPrice;

var prev2IsExtreme = IsLowestBar(_prev2, _prev1, candle, lowestValue) || IsHighestBar(_prev2, _prev1, candle, highestValue);
var prev1IsExtreme = IsLowestBar(_prev1, _prev2, candle, lowestValue) || IsHighestBar(_prev1, _prev2, candle, highestValue);

if (prev2Outside && prev2IsExtreme)
{
PlaceEntryOrders(_prev2, candle);
}
else if (prev1Outside && prev1IsExtreme)
{
PlaceEntryOrders(_prev1, candle);
}
}

private void PlaceEntryOrders(ICandleMessage patternCandle, ICandleMessage currentCandle)
{
var volume = Volume;

if (volume <= 0m)
return;

var indent = GetPriceOffset(Indent);
var step = Security?.PriceStep ?? 0.0001m;

var buyPrice = patternCandle.HighPrice + indent;
var sellPrice = patternCandle.LowPrice - indent;

if (sellPrice <= 0m)
sellPrice = step;

var buyStopLoss = Math.Max(patternCandle.LowPrice - indent, step);
var sellStopLoss = patternCandle.HighPrice + indent;

var buyTakeOffset = GetPriceOffset(TakeProfitBuy);
var sellTakeOffset = GetPriceOffset(TakeProfitSell);

var buyTakeProfit = buyTakeOffset > 0m ? buyPrice + buyTakeOffset : 0m;
var sellTakeProfit = sellTakeOffset > 0m ? sellPrice - sellTakeOffset : 0m;

CancelActiveOrders();

BuyStop(volume, buyPrice);
SellStop(volume, sellPrice);

_hasActiveOrders = true;
_pendingHigh = patternCandle.HighPrice;
_pendingLow = patternCandle.LowPrice;
_pendingBuyPrice = buyPrice;
_pendingSellPrice = sellPrice;
_pendingBuyStopLoss = buyStopLoss;
_pendingSellStopLoss = sellStopLoss;
_pendingBuyTakeProfit = buyTakeProfit;
_pendingSellTakeProfit = sellTakeProfit;
_exitRequestActive = false;

_ordersExpiry = OrderExpirationHours > 0
? currentCandle.CloseTime + TimeSpan.FromHours(OrderExpirationHours)
: null;
}

private void HandlePositionChange()
{
if (Position > 0 && _prevPosition <= 0)
{
if (_hasActiveOrders)
{
CancelActiveOrders();
ClearPendingOrders();
}

_entryPrice = PositionPrice;
_stopLoss = _pendingBuyStopLoss;
_takeProfit = _pendingBuyTakeProfit;
_exitRequestActive = false;
}
else if (Position < 0 && _prevPosition >= 0)
{
if (_hasActiveOrders)
{
CancelActiveOrders();
ClearPendingOrders();
}

_entryPrice = PositionPrice;
_stopLoss = _pendingSellStopLoss;
_takeProfit = _pendingSellTakeProfit;
_exitRequestActive = false;
}
else if (Position == 0 && _prevPosition != 0)
{
_entryPrice = 0m;
_stopLoss = 0m;
_takeProfit = 0m;
_exitRequestActive = false;
}
}

private void ManageActivePosition(ICandleMessage candle)
{
if (_exitRequestActive)
return;

if (Position > 0)
{
UpdateBreakevenLong(candle);
UpdateTrailingLong(candle);

if (_stopLoss > 0m && candle.LowPrice <= _stopLoss)
{
SellMarket(Math.Abs(Position));
_exitRequestActive = true;
return;
}

if (_takeProfit > 0m && candle.HighPrice >= _takeProfit)
{
SellMarket(Math.Abs(Position));
_exitRequestActive = true;
}
}
else if (Position < 0)
{
UpdateBreakevenShort(candle);
UpdateTrailingShort(candle);

if (_stopLoss > 0m && candle.HighPrice >= _stopLoss)
{
BuyMarket(Math.Abs(Position));
_exitRequestActive = true;
return;
}

if (_takeProfit > 0m && candle.LowPrice <= _takeProfit)
{
BuyMarket(Math.Abs(Position));
_exitRequestActive = true;
}
}
}

private void UpdateBreakevenLong(ICandleMessage candle)
{
if (Breakeven <= 0m || BreakevenProfit <= 0m)
return;

if (_stopLoss >= _entryPrice + GetPriceOffset(Breakeven))
return;

if (candle.HighPrice - _entryPrice >= GetPriceOffset(BreakevenProfit))
_stopLoss = _entryPrice + GetPriceOffset(Breakeven);
}

private void UpdateBreakevenShort(ICandleMessage candle)
{
if (Breakeven <= 0m || BreakevenProfit <= 0m)
return;

if (_stopLoss <= _entryPrice - GetPriceOffset(Breakeven))
return;

if (_entryPrice - candle.LowPrice >= GetPriceOffset(BreakevenProfit))
_stopLoss = _entryPrice - GetPriceOffset(Breakeven);
}

private void UpdateTrailingLong(ICandleMessage candle)
{
if (TrailingStop <= 0m)
return;

var trailing = GetPriceOffset(TrailingStop);
var step = GetPriceOffset(TrailingStep);
var current = candle.HighPrice;

if (current - _entryPrice <= trailing + step)
return;

if (_stopLoss < current - (trailing + step))
_stopLoss = Math.Max(_stopLoss, current - trailing);
}

private void UpdateTrailingShort(ICandleMessage candle)
{
if (TrailingStop <= 0m)
return;

var trailing = GetPriceOffset(TrailingStop);
var step = GetPriceOffset(TrailingStep);
var current = candle.LowPrice;

if (_entryPrice - current <= trailing + step)
return;

if (_stopLoss == 0m || _stopLoss > current + trailing + step)
_stopLoss = current + trailing;
}

private void UpdatePreviousCandles(ICandleMessage candle)
{
_prev2 = _prev1;
_prev1 = candle;
}

private void ClearPendingOrders()
{
_hasActiveOrders = false;
_ordersExpiry = null;
_pendingHigh = 0m;
_pendingLow = 0m;
_pendingBuyPrice = 0m;
_pendingSellPrice = 0m;
_pendingBuyStopLoss = 0m;
_pendingSellStopLoss = 0m;
_pendingBuyTakeProfit = 0m;
_pendingSellTakeProfit = 0m;
}

private bool IsLowestBar(ICandleMessage candidate, ICandleMessage other, ICandleMessage current, decimal lowestValue)
{
if (!AreClose(candidate.LowPrice, lowestValue))
return false;

return candidate.LowPrice < other.LowPrice && candidate.LowPrice < current.LowPrice;
}

private bool IsHighestBar(ICandleMessage candidate, ICandleMessage other, ICandleMessage current, decimal highestValue)
{
if (!AreClose(candidate.HighPrice, highestValue))
return false;

return candidate.HighPrice > other.HighPrice && candidate.HighPrice > current.HighPrice;
}

private decimal GetPriceOffset(decimal value)
{
var step = Security?.PriceStep ?? 0.0001m;
return value * step;
}

private bool AreClose(decimal first, decimal second)
{
var tolerance = (Security?.PriceStep ?? 0.0001m) / 2m;
return Math.Abs(first - second) <= tolerance;
}
}
