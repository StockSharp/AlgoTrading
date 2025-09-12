using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Defines stop loss calculation modes.
/// </summary>
public enum StopLossMode
{
/// <summary>
/// Stop loss is calculated from entry price.
/// </summary>
EntryPriceBased,

/// <summary>
/// Stop loss is based on the lowest low over a period.
/// </summary>
LowestLowBased
}

/// <summary>
/// Mutanabby AI Algo Pro strategy.
/// Enters long on bullish engulfing with RSI and price filters.
/// </summary>
public class MutanabbyAiAlgoProStrategy : Strategy
{
private readonly StrategyParam<decimal> _candleStabilityIndex;
private readonly StrategyParam<int> _rsiIndex;
private readonly StrategyParam<int> _candleDeltaLength;
private readonly StrategyParam<bool> _disableRepeatingSignals;
private readonly StrategyParam<bool> _enableStopLoss;
private readonly StrategyParam<StopLossMode> _stopLossMethod;
private readonly StrategyParam<decimal> _entryStopLossPercent;
private readonly StrategyParam<int> _lookbackPeriod;
private readonly StrategyParam<decimal> _stopLossBufferPercent;
private readonly StrategyParam<DataType> _candleType;

private decimal _prevOpen;
private decimal _prevClose;
private readonly Queue<decimal> _closeQueue = new();
private readonly Queue<decimal> _lowQueue = new();
private decimal _lowestLow = decimal.MaxValue;
private string _lastSignal = string.Empty;
private decimal _stopLossPrice;
private decimal _entryPrice;

/// <summary>
/// Minimum ratio between candle body and true range.
/// </summary>
public decimal CandleStabilityIndex
{
get => _candleStabilityIndex.Value;
set => _candleStabilityIndex.Value = value;
}

/// <summary>
/// RSI threshold.
/// </summary>
public int RsiIndex
{
get => _rsiIndex.Value;
set => _rsiIndex.Value = value;
}

/// <summary>
/// Bars for price comparison.
/// </summary>
public int CandleDeltaLength
{
get => _candleDeltaLength.Value;
set => _candleDeltaLength.Value = value;
}

/// <summary>
/// Prevent consecutive identical signals.
/// </summary>
public bool DisableRepeatingSignals
{
get => _disableRepeatingSignals.Value;
set => _disableRepeatingSignals.Value = value;
}

/// <summary>
/// Enable stop loss.
/// </summary>
public bool EnableStopLoss
{
get => _enableStopLoss.Value;
set => _enableStopLoss.Value = value;
}

/// <summary>
/// Stop loss calculation method.
/// </summary>
public StopLossMode StopLossMethod
{
get => _stopLossMethod.Value;
set => _stopLossMethod.Value = value;
}

/// <summary>
/// Entry based stop loss percent.
/// </summary>
public decimal EntryStopLossPercent
{
get => _entryStopLossPercent.Value;
set => _entryStopLossPercent.Value = value;
}

/// <summary>
/// Lookback period for lowest low stop.
/// </summary>
public int LookbackPeriod
{
get => _lookbackPeriod.Value;
set => _lookbackPeriod.Value = value;
}

/// <summary>
/// Buffer percent below lowest low.
/// </summary>
public decimal StopLossBufferPercent
{
get => _stopLossBufferPercent.Value;
set => _stopLossBufferPercent.Value = value;
}

/// <summary>
/// Candle type for calculations.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initializes a new instance of the <see cref="MutanabbyAiAlgoProStrategy"/> class.
/// </summary>
public MutanabbyAiAlgoProStrategy()
{
_candleStabilityIndex = Param(nameof(CandleStabilityIndex), 0.5m)
.SetDisplay("Candle Stability Index", "Minimum body/true range ratio", "Technical")
.SetRange(0m, 1m)
.SetCanOptimize(true)
.SetOptimize(0.1m, 1m, 0.1m);

_rsiIndex = Param(nameof(RsiIndex), 50)
.SetDisplay("RSI Index", "RSI threshold for entries", "Technical")
.SetRange(0, 100)
.SetCanOptimize(true);

_candleDeltaLength = Param(nameof(CandleDeltaLength), 5)
.SetDisplay("Candle Delta Length", "Bars for price comparison", "Technical")
.SetRange(3, 50)
.SetCanOptimize(true);

_disableRepeatingSignals = Param(nameof(DisableRepeatingSignals), false)
.SetDisplay("Disable Repeating Signals", "Avoid consecutive identical signals", "Technical");

_enableStopLoss = Param(nameof(EnableStopLoss), true)
.SetDisplay("Enable Stop Loss", "Activate stop loss", "Risk Management");

_stopLossMethod = Param(nameof(StopLossMethod), StopLossMode.EntryPriceBased)
.SetDisplay("Stop Loss Method", "Entry price or lowest low based", "Risk Management");

_entryStopLossPercent = Param(nameof(EntryStopLossPercent), 2.0m)
.SetDisplay("Entry Stop Loss %", "Stop loss percent from entry", "Risk Management")
.SetGreaterThanZero();

_lookbackPeriod = Param(nameof(LookbackPeriod), 10)
.SetDisplay("Lookback Period", "Bars for lowest low stop", "Risk Management")
.SetGreaterThanZero();

_stopLossBufferPercent = Param(nameof(StopLossBufferPercent), 0.5m)
.SetDisplay("Stop Loss Buffer %", "Additional buffer below lowest low", "Risk Management");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Type of candles to use", "General");
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
_prevOpen = 0;
_prevClose = 0;
_closeQueue.Clear();
_lowQueue.Clear();
_lowestLow = decimal.MaxValue;
_lastSignal = string.Empty;
_stopLossPrice = 0;
_entryPrice = 0;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var rsi = new RSI { Length = 14 };

var subscription = SubscribeCandles(CandleType);

subscription
.Bind(rsi, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, rsi);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

// update low queue
if (_lowQueue.Count == LookbackPeriod)
{
var removed = _lowQueue.Dequeue();
if (removed <= _lowestLow)
{
_lowestLow = decimal.MaxValue;
foreach (var v in _lowQueue)
{
if (v < _lowestLow)
_lowestLow = v;
}
}
}

_lowQueue.Enqueue(candle.LowPrice);
if (candle.LowPrice < _lowestLow)
_lowestLow = candle.LowPrice;

var priceN = _closeQueue.Count == CandleDeltaLength ? _closeQueue.Peek() : (decimal?)null;

var trueRange = candle.HighPrice - candle.LowPrice;
var stableCandle = trueRange > 0 && Math.Abs(candle.ClosePrice - candle.OpenPrice) / trueRange > CandleStabilityIndex;
var bullishEngulfing = _prevClose < _prevOpen && candle.ClosePrice > candle.OpenPrice && candle.ClosePrice > _prevOpen;
var rsiBelow = rsiValue < RsiIndex;
var decreaseOver = priceN != null && candle.ClosePrice < priceN;
var entrySignal = bullishEngulfing && stableCandle && rsiBelow && decreaseOver;

var bearishEngulfing = _prevClose > _prevOpen && candle.ClosePrice < candle.OpenPrice && candle.ClosePrice < _prevOpen;
var rsiAbove = rsiValue > 100 - RsiIndex;
var increaseOver = priceN != null && candle.ClosePrice > priceN;
var exitSignal = bearishEngulfing && stableCandle && rsiAbove && increaseOver;

if (entrySignal && Position <= 0 && (!DisableRepeatingSignals || _lastSignal != "buy"))
{
BuyMarket(Volume + Math.Abs(Position));
_entryPrice = candle.ClosePrice;
if (EnableStopLoss)
{
_stopLossPrice = StopLossMethod == StopLossMode.EntryPriceBased
? _entryPrice * (1 - EntryStopLossPercent / 100m)
: _lowestLow * (1 - StopLossBufferPercent / 100m);
}
_lastSignal = "buy";
}

if (exitSignal && Position > 0 && (!DisableRepeatingSignals || _lastSignal != "sell"))
{
SellMarket(Position);
_lastSignal = "sell";
}

if (EnableStopLoss && Position > 0 && _stopLossPrice > 0 && candle.ClosePrice <= _stopLossPrice)
{
SellMarket(Position);
_lastSignal = "sell";
}

_closeQueue.Enqueue(candle.ClosePrice);
if (_closeQueue.Count > CandleDeltaLength)
_closeQueue.Dequeue();

_prevOpen = candle.OpenPrice;
_prevClose = candle.ClosePrice;
}
}

