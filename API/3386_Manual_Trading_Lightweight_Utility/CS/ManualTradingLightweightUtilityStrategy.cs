using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Manual trading helper that reproduces the "Manual Trading Lightweight Utility" MetaTrader panel.
/// </summary>
public class ManualTradingLightweightUtilityStrategy : Strategy
{
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<ManualOrderMode> _buyOrderMode;
private readonly StrategyParam<ManualOrderMode> _sellOrderMode;
private readonly StrategyParam<bool> _buyAutomaticPrice;
private readonly StrategyParam<bool> _sellAutomaticPrice;
private readonly StrategyParam<decimal> _buyManualPrice;
private readonly StrategyParam<decimal> _sellManualPrice;
private readonly StrategyParam<decimal> _defaultVolume;
private readonly StrategyParam<bool> _useIndividualVolumes;
private readonly StrategyParam<decimal> _buyVolume;
private readonly StrategyParam<decimal> _sellVolume;
private readonly StrategyParam<int> _takeProfitPoints;
private readonly StrategyParam<int> _stopLossPoints;
private readonly StrategyParam<int> _limitOrderPoints;
private readonly StrategyParam<int> _stopOrderPoints;
private readonly StrategyParam<bool> _buyRequest;
private readonly StrategyParam<bool> _sellRequest;
private readonly StrategyParam<string> _orderComment;

private bool _buyRequestHandled;
private bool _sellRequestHandled;
private bool _lastBuyRequest;
private bool _lastSellRequest;
private decimal _entryPrice;
private decimal _lastPosition;
private bool _stopTriggered;
private bool _takeProfitTriggered;
private bool _pointWarningIssued;

/// <summary>
/// Describes the execution mode of the manual order.
/// </summary>
public enum ManualOrderMode
{
/// <summary>
/// Send a market order immediately.
/// </summary>
MarketExecution,

/// <summary>
/// Register a limit order using the configured offset or manual price.
/// </summary>
PendingLimit,

/// <summary>
/// Register a stop order using the configured offset or manual price.
/// </summary>
PendingStop
}

/// <summary>
/// Initializes a new instance of the <see cref="ManualTradingLightweightUtilityStrategy"/> class.
/// </summary>
public ManualTradingLightweightUtilityStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Market data series used to evaluate offsets and protective levels.", "Market Data");

_buyOrderMode = Param(nameof(BuyOrderMode), ManualOrderMode.MarketExecution)
.SetDisplay("Buy Mode", "Execution mode for buy requests.", "Manual Controls");

_sellOrderMode = Param(nameof(SellOrderMode), ManualOrderMode.MarketExecution)
.SetDisplay("Sell Mode", "Execution mode for sell requests.", "Manual Controls");

_buyAutomaticPrice = Param(nameof(UseAutomaticBuyPrice), true)
.SetDisplay("Buy Price Mode", "Use automatic offsets for buy pending orders when enabled.", "Manual Controls");

_sellAutomaticPrice = Param(nameof(UseAutomaticSellPrice), true)
.SetDisplay("Sell Price Mode", "Use automatic offsets for sell pending orders when enabled.", "Manual Controls");

_buyManualPrice = Param(nameof(BuyManualPrice), 0m)
.SetRange(0m, 100000000m)
.SetDisplay("Buy Manual Price", "Absolute price for buy pending orders when automatic mode is disabled.", "Manual Controls");

_sellManualPrice = Param(nameof(SellManualPrice), 0m)
.SetRange(0m, 100000000m)
.SetDisplay("Sell Manual Price", "Absolute price for sell pending orders when automatic mode is disabled.", "Manual Controls");

_defaultVolume = Param(nameof(DefaultVolume), 1m)
.SetGreaterThanZero()
.SetDisplay("Default Volume", "Volume used when individual volumes are disabled.", "Volumes")
.SetCanOptimize(true)
.SetOptimize(0.1m, 10m, 0.1m);

_useIndividualVolumes = Param(nameof(UseIndividualVolumes), false)
.SetDisplay("Custom Side Volumes", "Enable separate volumes for buy and sell requests.", "Volumes");

_buyVolume = Param(nameof(BuyVolume), 1m)
.SetGreaterThanZero()
.SetDisplay("Buy Volume", "Volume applied to buy orders when custom volumes are enabled.", "Volumes")
.SetCanOptimize(true)
.SetOptimize(0.1m, 10m, 0.1m);

_sellVolume = Param(nameof(SellVolume), 1m)
.SetGreaterThanZero()
.SetDisplay("Sell Volume", "Volume applied to sell orders when custom volumes are enabled.", "Volumes")
.SetCanOptimize(true)
.SetOptimize(0.1m, 10m, 0.1m);

_takeProfitPoints = Param(nameof(TakeProfitPoints), 400)
.SetGreaterThanOrEqualZero()
.SetDisplay("Take Profit Points", "Distance in points used to close positions for profit. Zero disables the feature.", "Risk Management")
.SetCanOptimize(true)
.SetOptimize(50, 800, 50);

_stopLossPoints = Param(nameof(StopLossPoints), 200)
.SetGreaterThanOrEqualZero()
.SetDisplay("Stop Loss Points", "Distance in points used to protect positions from losses. Zero disables the feature.", "Risk Management")
.SetCanOptimize(true)
.SetOptimize(20, 400, 20);

_limitOrderPoints = Param(nameof(LimitOrderPoints), 50)
.SetGreaterThanOrEqualZero()
.SetDisplay("Limit Offset Points", "Offset in points applied to automatic limit prices.", "Pricing")
.SetCanOptimize(true)
.SetOptimize(5, 200, 5);

_stopOrderPoints = Param(nameof(StopOrderPoints), 50)
.SetGreaterThanOrEqualZero()
.SetDisplay("Stop Offset Points", "Offset in points applied to automatic stop prices.", "Pricing")
.SetCanOptimize(true)
.SetOptimize(5, 200, 5);

_buyRequest = Param(nameof(BuyRequest), false)
.SetDisplay("Buy Request", "Set to true to trigger the configured buy action. The value resets after execution.", "Manual Controls");

_sellRequest = Param(nameof(SellRequest), false)
.SetDisplay("Sell Request", "Set to true to trigger the configured sell action. The value resets after execution.", "Manual Controls");

_orderComment = Param(nameof(OrderComment), "Manual Trading")
.SetDisplay("Order Comment", "Text written to the log whenever a manual action is executed.", "Manual Controls");
}

/// <summary>
/// Candle type used to monitor prices and manage risk.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Execution mode for buy requests.
/// </summary>
public ManualOrderMode BuyOrderMode
{
get => _buyOrderMode.Value;
set => _buyOrderMode.Value = value;
}

/// <summary>
/// Execution mode for sell requests.
/// </summary>
public ManualOrderMode SellOrderMode
{
get => _sellOrderMode.Value;
set => _sellOrderMode.Value = value;
}

/// <summary>
/// Determines whether buy pending orders use automatic offsets.
/// </summary>
public bool UseAutomaticBuyPrice
{
get => _buyAutomaticPrice.Value;
set => _buyAutomaticPrice.Value = value;
}

/// <summary>
/// Determines whether sell pending orders use automatic offsets.
/// </summary>
public bool UseAutomaticSellPrice
{
get => _sellAutomaticPrice.Value;
set => _sellAutomaticPrice.Value = value;
}

/// <summary>
/// Absolute price for buy pending orders when automatic mode is disabled.
/// </summary>
public decimal BuyManualPrice
{
get => _buyManualPrice.Value;
set => _buyManualPrice.Value = value;
}

/// <summary>
/// Absolute price for sell pending orders when automatic mode is disabled.
/// </summary>
public decimal SellManualPrice
{
get => _sellManualPrice.Value;
set => _sellManualPrice.Value = value;
}

/// <summary>
/// Default volume applied when individual volumes are disabled.
/// </summary>
public decimal DefaultVolume
{
get => _defaultVolume.Value;
set => _defaultVolume.Value = value;
}

/// <summary>
/// Enables individual buy and sell volumes.
/// </summary>
public bool UseIndividualVolumes
{
get => _useIndividualVolumes.Value;
set => _useIndividualVolumes.Value = value;
}

/// <summary>
/// Volume assigned to buy requests when individual volumes are enabled.
/// </summary>
public decimal BuyVolume
{
get => _buyVolume.Value;
set => _buyVolume.Value = value;
}

/// <summary>
/// Volume assigned to sell requests when individual volumes are enabled.
/// </summary>
public decimal SellVolume
{
get => _sellVolume.Value;
set => _sellVolume.Value = value;
}

/// <summary>
/// Take profit distance in points.
/// </summary>
public int TakeProfitPoints
{
get => _takeProfitPoints.Value;
set => _takeProfitPoints.Value = value;
}

/// <summary>
/// Stop loss distance in points.
/// </summary>
public int StopLossPoints
{
get => _stopLossPoints.Value;
set => _stopLossPoints.Value = value;
}

/// <summary>
/// Offset in points used for automatic limit prices.
/// </summary>
public int LimitOrderPoints
{
get => _limitOrderPoints.Value;
set => _limitOrderPoints.Value = value;
}

/// <summary>
/// Offset in points used for automatic stop prices.
/// </summary>
public int StopOrderPoints
{
get => _stopOrderPoints.Value;
set => _stopOrderPoints.Value = value;
}

/// <summary>
/// Momentary trigger for buy orders.
/// </summary>
public bool BuyRequest
{
get => _buyRequest.Value;
set => _buyRequest.Value = value;
}

/// <summary>
/// Momentary trigger for sell orders.
/// </summary>
public bool SellRequest
{
get => _sellRequest.Value;
set => _sellRequest.Value = value;
}

/// <summary>
/// Comment written to the log when orders are submitted.
/// </summary>
public string OrderComment
{
get => _orderComment.Value;
set => _orderComment.Value = value;
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

_buyRequestHandled = !BuyRequest;
_sellRequestHandled = !SellRequest;
_lastBuyRequest = BuyRequest;
_lastSellRequest = SellRequest;
_entryPrice = 0m;
_lastPosition = Position;
_stopTriggered = false;
_takeProfitTriggered = false;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

Volume = GetDefaultVolume();

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

Volume = GetDefaultVolume();

UpdateRequestTracking();

if (!IsFormedAndOnlineAndAllowTrading())
return;

HandleBuyRequest(candle);
HandleSellRequest(candle);
ManageRisk(candle);
}

private void UpdateRequestTracking()
{
if (_lastBuyRequest != BuyRequest)
{
_buyRequestHandled = !BuyRequest;
_lastBuyRequest = BuyRequest;
}

if (_lastSellRequest != SellRequest)
{
_sellRequestHandled = !SellRequest;
_lastSellRequest = SellRequest;
}
}

private void HandleBuyRequest(ICandleMessage candle)
{
if (_buyRequestHandled || !BuyRequest)
return;

var volume = GetBuyVolume();
if (volume <= 0m)
{
LogWarning("Buy request ignored because volume is not positive.");
CompleteBuyRequest();
return;
}

switch (BuyOrderMode)
{
case ManualOrderMode.MarketExecution:
BuyMarket(volume);
LogInfo($"BUY market order submitted. Volume={volume}, Comment={OrderComment}.");
CompleteBuyRequest();
break;

case ManualOrderMode.PendingLimit:
{
var price = ResolveBuyPrice(candle, true);
if (price <= 0m)
{
LogWarning("Cannot place buy limit order because the resolved price is not positive.");
CompleteBuyRequest();
return;
}

BuyLimit(volume, price);
LogInfo($"BUY limit order submitted at {price}. Volume={volume}, Comment={OrderComment}.");
CompleteBuyRequest();
break;
}

case ManualOrderMode.PendingStop:
{
var price = ResolveBuyPrice(candle, false);
if (price <= 0m)
{
LogWarning("Cannot place buy stop order because the resolved price is not positive.");
CompleteBuyRequest();
return;
}

BuyStop(volume, price);
LogInfo($"BUY stop order submitted at {price}. Volume={volume}, Comment={OrderComment}.");
CompleteBuyRequest();
break;
}
}
}

private void HandleSellRequest(ICandleMessage candle)
{
if (_sellRequestHandled || !SellRequest)
return;

var volume = GetSellVolume();
if (volume <= 0m)
{
LogWarning("Sell request ignored because volume is not positive.");
CompleteSellRequest();
return;
}

switch (SellOrderMode)
{
case ManualOrderMode.MarketExecution:
SellMarket(volume);
LogInfo($"SELL market order submitted. Volume={volume}, Comment={OrderComment}.");
CompleteSellRequest();
break;

case ManualOrderMode.PendingLimit:
{
var price = ResolveSellPrice(candle, true);
if (price <= 0m)
{
LogWarning("Cannot place sell limit order because the resolved price is not positive.");
CompleteSellRequest();
return;
}

SellLimit(volume, price);
LogInfo($"SELL limit order submitted at {price}. Volume={volume}, Comment={OrderComment}.");
CompleteSellRequest();
break;
}

case ManualOrderMode.PendingStop:
{
var price = ResolveSellPrice(candle, false);
if (price <= 0m)
{
LogWarning("Cannot place sell stop order because the resolved price is not positive.");
CompleteSellRequest();
return;
}

SellStop(volume, price);
LogInfo($"SELL stop order submitted at {price}. Volume={volume}, Comment={OrderComment}.");
CompleteSellRequest();
break;
}
}
}

private void ManageRisk(ICandleMessage candle)
{
if (Position == 0m)
{
if (_lastPosition != 0m)
{
_lastPosition = 0m;
_entryPrice = 0m;
_stopTriggered = false;
_takeProfitTriggered = false;
}

return;
}

if (Position != _lastPosition)
{
_entryPrice = candle.ClosePrice;
_stopTriggered = false;
_takeProfitTriggered = false;
_lastPosition = Position;
}

var isLong = Position > 0m;
var volume = Math.Abs(Position);

var stopPrice = GetStopLossPrice(isLong, candle);
if (!_stopTriggered && stopPrice > 0m)
{
if ((isLong && candle.LowPrice <= stopPrice) || (!isLong && candle.HighPrice >= stopPrice))
{
if (isLong)
SellMarket(volume);
else
BuyMarket(volume);

_stopTriggered = true;
_takeProfitTriggered = true;
return;
}
}

var takeProfitPrice = GetTakeProfitPrice(isLong);
if (!_takeProfitTriggered && takeProfitPrice > 0m)
{
if ((isLong && candle.HighPrice >= takeProfitPrice) || (!isLong && candle.LowPrice <= takeProfitPrice))
{
if (isLong)
SellMarket(volume);
else
BuyMarket(volume);

_takeProfitTriggered = true;
_stopTriggered = true;
}
}
}

private void CompleteBuyRequest()
{
_buyRequestHandled = true;
BuyRequest = false;
_lastBuyRequest = false;
}

private void CompleteSellRequest()
{
_sellRequestHandled = true;
SellRequest = false;
_lastSellRequest = false;
}

private decimal ResolveBuyPrice(ICandleMessage candle, bool isLimit)
{
if (!UseAutomaticBuyPrice)
return BuyManualPrice;

var ask = GetBestAskOrClose(candle);
var offset = isLimit ? GetLimitOffset() : GetStopOffset();

return isLimit
? Math.Max(ask - offset, 0m)
: ask + offset;
}

private decimal ResolveSellPrice(ICandleMessage candle, bool isLimit)
{
if (!UseAutomaticSellPrice)
return SellManualPrice;

var bid = GetBestBidOrClose(candle);
var offset = isLimit ? GetLimitOffset() : GetStopOffset();

return isLimit
? bid + offset
: Math.Max(bid - offset, 0m);
}

private decimal GetStopLossPrice(bool isLong, ICandleMessage candle)
{
if (StopLossPoints <= 0 || _entryPrice <= 0m)
return 0m;

var offset = GetStopLossOffset(candle);
if (offset <= 0m)
return 0m;

return isLong
? Math.Max(_entryPrice - offset, 0m)
: _entryPrice + offset;
}

private decimal GetTakeProfitPrice(bool isLong)
{
if (TakeProfitPoints <= 0 || _entryPrice <= 0m)
return 0m;

var offset = GetTakeProfitOffset();
if (offset <= 0m)
return 0m;

return isLong
? _entryPrice + offset
: Math.Max(_entryPrice - offset, 0m);
}

private decimal GetBuyVolume()
{
if (UseIndividualVolumes)
return BuyVolume;

return GetDefaultVolume();
}

private decimal GetSellVolume()
{
if (UseIndividualVolumes)
return SellVolume;

return GetDefaultVolume();
}

private decimal GetDefaultVolume()
{
return DefaultVolume > 0m ? DefaultVolume : 0m;
}

private decimal GetLimitOffset()
{
var point = GetPointValue();
return point <= 0m ? 0m : LimitOrderPoints * point;
}

private decimal GetStopOffset()
{
var point = GetPointValue();
return point <= 0m ? 0m : StopOrderPoints * point;
}

private decimal GetTakeProfitOffset()
{
var point = GetPointValue();
return point <= 0m ? 0m : TakeProfitPoints * point;
}

private decimal GetStopLossOffset(ICandleMessage candle)
{
var point = GetPointValue();
if (point <= 0m)
return 0m;

var spread = GetSpread(candle);
return StopLossPoints * point + spread;
}

private decimal GetPointValue()
{
var step = Security?.PriceStep ?? Security?.MinPriceStep ?? 0m;
if (step <= 0m && !_pointWarningIssued)
{
LogWarning("Price step is not configured. Point-based offsets cannot be calculated.");
_pointWarningIssued = true;
}

return step;
}

private decimal GetSpread(ICandleMessage candle)
{
var ask = Security?.BestAsk?.Price;
var bid = Security?.BestBid?.Price;

if (ask is decimal a && bid is decimal b && a > 0m && b > 0m && a >= b)
return a - b;

var point = GetPointValue();
return point > 0m ? point : 0m;
}

private decimal GetBestAskOrClose(ICandleMessage candle)
{
return Security?.BestAsk?.Price ?? candle.ClosePrice;
}

private decimal GetBestBidOrClose(ICandleMessage candle)
{
return Security?.BestBid?.Price ?? candle.ClosePrice;
}
}
