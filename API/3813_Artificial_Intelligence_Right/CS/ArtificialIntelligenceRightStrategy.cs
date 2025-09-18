using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Artificial Intelligence Right strategy replicates the MetaTrader perceptron logic over the Acceleration/Deceleration oscillator.
/// </summary>
public class ArtificialIntelligenceRightStrategy : Strategy
{
private readonly StrategyParam<int> _x1;
private readonly StrategyParam<int> _x2;
private readonly StrategyParam<int> _x3;
private readonly StrategyParam<int> _x4;
private readonly StrategyParam<decimal> _stopLossPoints;
private readonly StrategyParam<decimal> _spreadPoints;
private readonly StrategyParam<DataType> _candleType;

private readonly SimpleMovingAverage _aoFast = new() { Length = 5 };
private readonly SimpleMovingAverage _aoSlow = new() { Length = 34 };
private readonly SimpleMovingAverage _acMa = new() { Length = 5 };

private readonly decimal[] _acBuffer = new decimal[22];
private int _bufferCount;
private decimal _entryPrice;
private decimal _stopPrice;

/// <summary>
/// First perceptron weight.
/// </summary>
public int X1
{
get => _x1.Value;
set => _x1.Value = value;
}

/// <summary>
/// Second perceptron weight.
/// </summary>
public int X2
{
get => _x2.Value;
set => _x2.Value = value;
}

/// <summary>
/// Third perceptron weight.
/// </summary>
public int X3
{
get => _x3.Value;
set => _x3.Value = value;
}

/// <summary>
/// Fourth perceptron weight.
/// </summary>
public int X4
{
get => _x4.Value;
set => _x4.Value = value;
}

/// <summary>
/// Stop-loss distance expressed in price points (MetaTrader "Point").
/// </summary>
public decimal StopLossPoints
{
get => _stopLossPoints.Value;
set => _stopLossPoints.Value = value;
}

/// <summary>
/// Spread buffer in points used by the trailing filter.
/// </summary>
public decimal SpreadPoints
{
get => _spreadPoints.Value;
set => _spreadPoints.Value = value;
}

/// <summary>
/// Candle type for signal calculations.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initializes a new instance of <see cref="ArtificialIntelligenceRightStrategy"/>.
/// </summary>
public ArtificialIntelligenceRightStrategy()
{
_x1 = Param(nameof(X1), 135)
.SetDisplay("X1", "Perceptron weight 1", "Perceptron")
.SetCanOptimize(true)
.SetOptimize(0, 200, 5);

_x2 = Param(nameof(X2), 127)
.SetDisplay("X2", "Perceptron weight 2", "Perceptron")
.SetCanOptimize(true)
.SetOptimize(0, 200, 5);

_x3 = Param(nameof(X3), 16)
.SetDisplay("X3", "Perceptron weight 3", "Perceptron")
.SetCanOptimize(true)
.SetOptimize(0, 200, 5);

_x4 = Param(nameof(X4), 93)
.SetDisplay("X4", "Perceptron weight 4", "Perceptron")
.SetCanOptimize(true)
.SetOptimize(0, 200, 5);

_stopLossPoints = Param(nameof(StopLossPoints), 85m)
.SetDisplay("Stop Loss", "Stop-loss distance in points", "Risk")
.SetCanOptimize(true)
.SetOptimize(10m, 200m, 5m);

_spreadPoints = Param(nameof(SpreadPoints), 3m)
.SetDisplay("Spread", "Spread buffer in points", "Risk")
.SetCanOptimize(false);

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");

Volume = 1m;
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

Array.Clear(_acBuffer);
_bufferCount = 0;
_entryPrice = 0m;
_stopPrice = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var subscription = SubscribeCandles(CandleType);
subscription.Bind(ProcessCandle).Start();

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

var hl2 = (candle.HighPrice + candle.LowPrice) / 2m;

var aoFast = _aoFast.Process(hl2);
var aoSlow = _aoSlow.Process(hl2);
if (!aoFast.IsFinal || !aoSlow.IsFinal)
return;

var ao = aoFast.GetValue<decimal>() - aoSlow.GetValue<decimal>();

var acMa = _acMa.Process(ao);
if (!acMa.IsFinal)
return;

var ac = ao - acMa.GetValue<decimal>();

for (var i = 21; i > 0; i--)
_acBuffer[i] = _acBuffer[i - 1];
_acBuffer[0] = ac;

if (_bufferCount < 22)
{
_bufferCount++;
return;
}

var point = ResolvePointValue();
var stopDistance = StopLossPoints * point;
var spreadDistance = SpreadPoints * point;
var trailingTrigger = stopDistance * 2m + spreadDistance;
var signal = Perceptron();

if (Position > 0m)
{
if (candle.ClosePrice <= _stopPrice && stopDistance > 0m)
{
// Simulate protective stop-loss execution for long position.
SellMarket(Position);
_entryPrice = 0m;
_stopPrice = 0m;
return;
}

if (stopDistance <= 0m)
return;

if (candle.ClosePrice > _stopPrice + trailingTrigger)
{
if (signal < 0m)
{
// Reverse from long to short by selling double the current exposure.
SellMarket(Position * 2m);
_entryPrice = candle.ClosePrice;
_stopPrice = _entryPrice + stopDistance;
return;
}

// Trail stop-loss below market price preserving original distance.
_stopPrice = candle.ClosePrice - stopDistance;
}

return;
}

if (Position < 0m)
{
if (candle.ClosePrice >= _stopPrice && stopDistance > 0m)
{
// Simulate protective stop-loss execution for short position.
BuyMarket(-Position);
_entryPrice = 0m;
_stopPrice = 0m;
return;
}

if (stopDistance <= 0m)
return;

if (candle.ClosePrice < _stopPrice - trailingTrigger)
{
if (signal > 0m)
{
// Reverse from short to long by buying double the absolute exposure.
BuyMarket(-Position * 2m);
_entryPrice = candle.ClosePrice;
_stopPrice = _entryPrice - stopDistance;
return;
}

// Trail stop-loss above market price preserving original distance.
_stopPrice = candle.ClosePrice + stopDistance;
}

return;
}

if (signal > 0m)
{
// Start a new long position when perceptron turns positive.
BuyMarket(Volume);
_entryPrice = candle.ClosePrice;
_stopPrice = stopDistance > 0m ? _entryPrice - stopDistance : 0m;
}
else if (signal < 0m)
{
// Start a new short position when perceptron turns negative.
SellMarket(Volume);
_entryPrice = candle.ClosePrice;
_stopPrice = stopDistance > 0m ? _entryPrice + stopDistance : 0m;
}
}

private decimal Perceptron()
{
var w1 = X1 - 100m;
var w2 = X2 - 100m;
var w3 = X3 - 100m;
var w4 = X4 - 100m;

return w1 * _acBuffer[0] + w2 * _acBuffer[7] + w3 * _acBuffer[14] + w4 * _acBuffer[21];
}

private decimal ResolvePointValue()
{
var step = Security?.PriceStep;
if (step is null || step == 0m)
return 1m;

return step.Value;
}
}
