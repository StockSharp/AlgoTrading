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
/// Currency strength inspired strategy that trades a single symbol based on EMA trend and momentum filters.
/// </summary>
public class SmartAcTraderStrategy : Strategy
{
private readonly StrategyParam<int> _fastMaPeriod;
private readonly StrategyParam<int> _slowMaPeriod;
private readonly StrategyParam<int> _rocPeriod;
private readonly StrategyParam<decimal> _buyMomentumThreshold;
private readonly StrategyParam<decimal> _sellMomentumThreshold;
private readonly StrategyParam<decimal> _stopLossPoints;
private readonly StrategyParam<decimal> _takeProfitPoints;
private readonly StrategyParam<bool> _useTrailingStop;
private readonly StrategyParam<decimal> _trailingStopPoints;
private readonly StrategyParam<bool> _useBreakEven;
private readonly StrategyParam<decimal> _breakEvenTriggerPoints;
private readonly StrategyParam<decimal> _breakEvenOffsetPoints;
private readonly StrategyParam<DataType> _candleType;

private decimal? _entryPrice;
private decimal _highestPrice;
private decimal _lowestPrice;
private decimal _priceStep;
private bool _breakEvenArmed;

/// <summary>
/// Initializes a new instance of the <see cref="SmartAcTraderStrategy"/> class.
/// </summary>
public SmartAcTraderStrategy()
{
_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
.SetGreaterThanZero()
.SetDisplay("Fast EMA", "Length of the fast EMA trend filter", "Trend")
.SetCanOptimize(true)
.SetOptimize(4, 20, 2);

_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
.SetGreaterThanZero()
.SetDisplay("Slow EMA", "Length of the slow EMA trend filter", "Trend")
.SetCanOptimize(true)
.SetOptimize(30, 150, 10);

_rocPeriod = Param(nameof(RocPeriod), 13)
.SetGreaterThanZero()
.SetDisplay("ROC Period", "Rate of change lookback for momentum confirmation", "Momentum")
.SetCanOptimize(true)
.SetOptimize(5, 30, 5);

_buyMomentumThreshold = Param(nameof(BuyMomentumThreshold), 0.3m)
.SetDisplay("Buy Momentum", "Minimum positive ROC to allow long entries", "Momentum")
.SetCanOptimize(true)
.SetOptimize(0.1m, 1.0m, 0.1m);

_sellMomentumThreshold = Param(nameof(SellMomentumThreshold), 0.3m)
.SetDisplay("Sell Momentum", "Minimum negative ROC (absolute) to allow short entries", "Momentum")
.SetCanOptimize(true)
.SetOptimize(0.1m, 1.0m, 0.1m);

_stopLossPoints = Param(nameof(StopLossPoints), 100m)
.SetGreaterThanZero()
.SetDisplay("Stop Loss", "Protective stop in price steps", "Risk")
.SetCanOptimize(true)
.SetOptimize(20m, 200m, 20m);

_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
.SetGreaterThanZero()
.SetDisplay("Take Profit", "Target distance in price steps", "Risk")
.SetCanOptimize(true)
.SetOptimize(20m, 150m, 10m);

_useTrailingStop = Param(nameof(UseTrailingStop), true)
.SetDisplay("Use Trailing", "Enable candle close based trailing stop", "Risk");

_trailingStopPoints = Param(nameof(TrailingStopPoints), 40m)
.SetGreaterThanZero()
.SetDisplay("Trailing", "Distance in price steps for the trailing stop", "Risk")
.SetCanOptimize(true)
.SetOptimize(10m, 100m, 10m);

_useBreakEven = Param(nameof(UseBreakEven), true)
.SetDisplay("Use Break Even", "Enable break-even protection", "Risk");

_breakEvenTriggerPoints = Param(nameof(BreakEvenTriggerPoints), 30m)
.SetGreaterThanZero()
.SetDisplay("Break Even Trigger", "Profit in price steps required to arm break-even", "Risk")
.SetCanOptimize(true)
.SetOptimize(10m, 80m, 10m);

_breakEvenOffsetPoints = Param(nameof(BreakEvenOffsetPoints), 30m)
.SetGreaterThanZero()
.SetDisplay("Break Even Offset", "Distance in price steps kept after break-even is armed", "Risk")
.SetCanOptimize(true)
.SetOptimize(10m, 80m, 10m);

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Source candles for indicators", "General");
}

/// <summary>
/// Fast EMA period.
/// </summary>
public int FastMaPeriod
{
get => _fastMaPeriod.Value;
set => _fastMaPeriod.Value = value;
}

/// <summary>
/// Slow EMA period.
/// </summary>
public int SlowMaPeriod
{
get => _slowMaPeriod.Value;
set => _slowMaPeriod.Value = value;
}

/// <summary>
/// Rate of change lookback period.
/// </summary>
public int RocPeriod
{
get => _rocPeriod.Value;
set => _rocPeriod.Value = value;
}

/// <summary>
/// Minimum momentum required to buy.
/// </summary>
public decimal BuyMomentumThreshold
{
get => _buyMomentumThreshold.Value;
set => _buyMomentumThreshold.Value = value;
}

/// <summary>
/// Minimum absolute momentum required to sell.
/// </summary>
public decimal SellMomentumThreshold
{
get => _sellMomentumThreshold.Value;
set => _sellMomentumThreshold.Value = value;
}

/// <summary>
/// Stop loss in price steps.
/// </summary>
public decimal StopLossPoints
{
get => _stopLossPoints.Value;
set => _stopLossPoints.Value = value;
}

/// <summary>
/// Take profit in price steps.
/// </summary>
public decimal TakeProfitPoints
{
get => _takeProfitPoints.Value;
set => _takeProfitPoints.Value = value;
}

/// <summary>
/// Enables trailing stop management.
/// </summary>
public bool UseTrailingStop
{
get => _useTrailingStop.Value;
set => _useTrailingStop.Value = value;
}

/// <summary>
/// Trailing stop distance in price steps.
/// </summary>
public decimal TrailingStopPoints
{
get => _trailingStopPoints.Value;
set => _trailingStopPoints.Value = value;
}

/// <summary>
/// Enables break-even protection.
/// </summary>
public bool UseBreakEven
{
get => _useBreakEven.Value;
set => _useBreakEven.Value = value;
}

/// <summary>
/// Profit required to arm break-even.
/// </summary>
public decimal BreakEvenTriggerPoints
{
get => _breakEvenTriggerPoints.Value;
set => _breakEvenTriggerPoints.Value = value;
}

/// <summary>
/// Price offset preserved after break-even is armed.
/// </summary>
public decimal BreakEvenOffsetPoints
{
get => _breakEvenOffsetPoints.Value;
set => _breakEvenOffsetPoints.Value = value;
}

/// <summary>
/// Candle type used for analysis.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
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

ResetPositionState();
_priceStep = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

if (Security?.PriceStep is not decimal step || step <= 0m)
{
LogError("Price step is not defined. Strategy stopped.");
Stop();
return;
}

_priceStep = step;

StartProtection();

var fastEma = new EMA { Length = FastMaPeriod };
var slowEma = new EMA { Length = SlowMaPeriod };
var roc = new RateOfChange { Length = RocPeriod };

var subscription = SubscribeCandles(CandleType);

var trendArea = CreateChartArea();
var momentumArea = CreateChartArea("Momentum");

if (trendArea != null)
{
var priceElement = trendArea.CreateCandleElement();
if (priceElement != null)
priceElement.Color = null;

var fastElement = trendArea.CreateIndicatorElement();
if (fastElement != null)
fastElement.Indicator = fastEma;

var slowElement = trendArea.CreateIndicatorElement();
if (slowElement != null)
slowElement.Indicator = slowEma;
}

if (momentumArea != null)
{
var rocElement = momentumArea.CreateIndicatorElement();
if (rocElement != null)
rocElement.Indicator = roc;
}

subscription
.Bind(fastEma, slowEma, roc, ProcessCandle)
.Start();
}

/// <inheritdoc />
protected override void OnOwnTradeReceived(MyTrade trade)
{
base.OnOwnTradeReceived(trade);

if (Position == 0m)
{
ResetPositionState();
return;
}

if (_entryPrice is null)
{
_entryPrice = trade.Trade.Price;
_highestPrice = trade.Trade.Price;
_lowestPrice = trade.Trade.Price;
_breakEvenArmed = false;
LogInfo($"Entry price initialized at {trade.Trade.Price:0.#####} for position {Position}.");
}
else
{
if (Position > 0m)
_highestPrice = Math.Max(_highestPrice, trade.Trade.Price);
else if (Position < 0m)
_lowestPrice = Math.Min(_lowestPrice, trade.Trade.Price);
}
}

private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal rocValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var closePrice = candle.ClosePrice;

if (Position > 0m)
{
ManageLongPosition(closePrice);
}
else if (Position < 0m)
{
ManageShortPosition(closePrice);
}

if (Position <= 0m && fastValue > slowValue && rocValue >= BuyMomentumThreshold)
{
ResetPositionState();
var volume = Volume + Math.Abs(Position);
BuyMarket(volume);
LogInfo($"Buy signal at {closePrice:0.#####}. Fast EMA {fastValue:0.###} above slow EMA {slowValue:0.###}, ROC {rocValue:0.###}.");
}
else if (Position >= 0m && fastValue < slowValue && rocValue <= -SellMomentumThreshold)
{
ResetPositionState();
var volume = Volume + Math.Abs(Position);
SellMarket(volume);
LogInfo($"Sell signal at {closePrice:0.#####}. Fast EMA {fastValue:0.###} below slow EMA {slowValue:0.###}, ROC {rocValue:0.###}.");
}
}

private void ManageLongPosition(decimal price)
{
if (_entryPrice is null || _priceStep <= 0m)
return;

_highestPrice = Math.Max(_highestPrice, price);

var stopLevel = _entryPrice.Value - StopLossPoints * _priceStep;
if (StopLossPoints > 0m && price <= stopLevel)
{
SellMarket(Position);
LogInfo($"Stop loss hit for long position at {price:0.#####}.");
ResetPositionState();
return;
}

var takeProfitLevel = _entryPrice.Value + TakeProfitPoints * _priceStep;
if (TakeProfitPoints > 0m && price >= takeProfitLevel)
{
SellMarket(Position);
LogInfo($"Take profit hit for long position at {price:0.#####}.");
ResetPositionState();
return;
}

if (UseBreakEven)
{
var trigger = _entryPrice.Value + BreakEvenTriggerPoints * _priceStep;
if (!_breakEvenArmed && BreakEvenTriggerPoints > 0m && price >= trigger)
{
_breakEvenArmed = true;
LogInfo($"Break-even armed for long position at {price:0.#####}.");
}

if (_breakEvenArmed)
{
var breakEvenPrice = _entryPrice.Value + BreakEvenOffsetPoints * _priceStep;
if (BreakEvenOffsetPoints > 0m && price <= breakEvenPrice)
{
SellMarket(Position);
LogInfo($"Break-even exit for long position at {price:0.#####}.");
ResetPositionState();
return;
}
}
}

if (UseTrailingStop)
{
var trailingLevel = _highestPrice - TrailingStopPoints * _priceStep;
if (TrailingStopPoints > 0m && price <= trailingLevel)
{
SellMarket(Position);
LogInfo($"Trailing stop exit for long position at {price:0.#####}.");
ResetPositionState();
}
}
}

private void ManageShortPosition(decimal price)
{
if (_entryPrice is null || _priceStep <= 0m)
return;

_lowestPrice = Math.Min(_lowestPrice, price);

var stopLevel = _entryPrice.Value + StopLossPoints * _priceStep;
if (StopLossPoints > 0m && price >= stopLevel)
{
BuyMarket(-Position);
LogInfo($"Stop loss hit for short position at {price:0.#####}.");
ResetPositionState();
return;
}

var takeProfitLevel = _entryPrice.Value - TakeProfitPoints * _priceStep;
if (TakeProfitPoints > 0m && price <= takeProfitLevel)
{
BuyMarket(-Position);
LogInfo($"Take profit hit for short position at {price:0.#####}.");
ResetPositionState();
return;
}

if (UseBreakEven)
{
var trigger = _entryPrice.Value - BreakEvenTriggerPoints * _priceStep;
if (!_breakEvenArmed && BreakEvenTriggerPoints > 0m && price <= trigger)
{
_breakEvenArmed = true;
LogInfo($"Break-even armed for short position at {price:0.#####}.");
}

if (_breakEvenArmed)
{
var breakEvenPrice = _entryPrice.Value - BreakEvenOffsetPoints * _priceStep;
if (BreakEvenOffsetPoints > 0m && price >= breakEvenPrice)
{
BuyMarket(-Position);
LogInfo($"Break-even exit for short position at {price:0.#####}.");
ResetPositionState();
return;
}
}
}

if (UseTrailingStop)
{
var trailingLevel = _lowestPrice + TrailingStopPoints * _priceStep;
if (TrailingStopPoints > 0m && price >= trailingLevel)
{
BuyMarket(-Position);
LogInfo($"Trailing stop exit for short position at {price:0.#####}.");
ResetPositionState();
}
}
}

private void ResetPositionState()
{
_entryPrice = null;
_highestPrice = 0m;
_lowestPrice = decimal.MaxValue;
_breakEvenArmed = false;
}
}

