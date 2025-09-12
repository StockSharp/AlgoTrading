
using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Lube strategy based on friction levels and a FIR filter trend.
/// </summary>
public class LubeStrategy : Strategy
{
private readonly StrategyParam<int> _barsBack;
private readonly StrategyParam<int> _frictionLevel;
private readonly StrategyParam<int> _triggerLevel;
private readonly StrategyParam<int> _range;
private readonly StrategyParam<decimal> _leverage;
private readonly StrategyParam<bool> _enableShorts;
private readonly StrategyParam<DataType> _candleType;

private readonly Queue<decimal> _highs = new();
private readonly Queue<decimal> _lows = new();
private readonly Queue<decimal> _frictions = new();
private readonly Queue<decimal> _midfHist = new();
private readonly Queue<decimal> _lowf2Hist = new();
private readonly Queue<decimal> _highfHist = new();
private readonly Queue<decimal> _closeQueue = new();

private decimal _prevFir;
private int _barCount;

/// <summary>
/// Bars back to measure friction.
/// </summary>
public int BarsBack
{
get => _barsBack.Value;
set => _barsBack.Value = value;
}

/// <summary>
/// Friction level (0-100) to stop trade.
/// </summary>
public int FrictionLevel
{
get => _frictionLevel.Value;
set => _frictionLevel.Value = value;
}

/// <summary>
/// Trigger level below 0 to initiate trade.
/// </summary>
public int TriggerLevel
{
get => _triggerLevel.Value;
set => _triggerLevel.Value = value;
}

/// <summary>
/// Bars back to measure lowest friction.
/// </summary>
public int Range
{
get => _range.Value;
set => _range.Value = value;
}

/// <summary>
/// Leverage multiplier.
/// </summary>
public decimal Leverage
{
get => _leverage.Value;
set => _leverage.Value = value;
}

/// <summary>
/// Enable short trades.
/// </summary>
public bool EnableShorts
{
get => _enableShorts.Value;
set => _enableShorts.Value = value;
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
/// Initializes strategy parameters.
/// </summary>
public LubeStrategy()
{
_barsBack = Param(nameof(BarsBack), 500)
.SetGreaterThanZero()
.SetDisplay("Bars Back", "Bars back to measure friction", "General");

_frictionLevel = Param(nameof(FrictionLevel), 50)
.SetDisplay("Friction Level", "0-100 friction level to stop trade", "General");

_triggerLevel = Param(nameof(TriggerLevel), -10)
.SetDisplay("Trigger Level", "Pic lower than 0 to initiate trade", "General");

_range = Param(nameof(Range), 100)
.SetGreaterThanZero()
.SetDisplay("Range", "Bars back to measure lowest friction", "General");

_leverage = Param(nameof(Leverage), 2m)
.SetGreaterThanZero()
.SetDisplay("Leverage", "Leverage multiplier", "Trading");

_enableShorts = Param(nameof(EnableShorts), true)
.SetDisplay("Enable Shorts", "Allow short trades", "Trading");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");
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
_highs.Clear();
_lows.Clear();
_frictions.Clear();
_midfHist.Clear();
_lowf2Hist.Clear();
_highfHist.Clear();
_closeQueue.Clear();
_prevFir = 0m;
_barCount = 0;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

StartProtection();

var subscription = SubscribeCandles(CandleType);
subscription.Bind(ProcessCandle).Start();
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

_barCount++;

_highs.Enqueue(candle.HighPrice);
_lows.Enqueue(candle.LowPrice);

while (_highs.Count > BarsBack)
{
_highs.Dequeue();
_lows.Dequeue();
}

var highsArr = _highs.ToArray();
var lowsArr = _lows.ToArray();
var friction = 0m;
for (var i = 0; i < highsArr.Length; i++)
{
var idx = i + 1;
if (highsArr[i] >= candle.ClosePrice && lowsArr[i] <= candle.ClosePrice)
friction += (1m + BarsBack) / (idx + BarsBack);
}

_frictions.Enqueue(friction);
while (_frictions.Count > Range)
_frictions.Dequeue();

var lowf = decimal.MaxValue;
var highf = decimal.MinValue;
foreach (var f in _frictions)
{
if (f < lowf)
lowf = f;
if (f > highf)
highf = f;
}

var fl = FrictionLevel / 100m;
var tl = TriggerLevel / 100m;

var midf = lowf * (1m - fl) + highf * fl;
var lowf2 = lowf * (1m - tl) + highf * tl;

_midfHist.Enqueue(midf);
_lowf2Hist.Enqueue(lowf2);
_highfHist.Enqueue(highf);
if (_midfHist.Count > 6) _midfHist.Dequeue();
if (_lowf2Hist.Count > 6) _lowf2Hist.Dequeue();
if (_highfHist.Count > 6) _highfHist.Dequeue();

var midf5 = _midfHist.Count == 6 ? _midfHist.Peek() : midf;
var lowf25 = _lowf2Hist.Count == 6 ? _lowf2Hist.Peek() : lowf2;

_closeQueue.Enqueue(candle.ClosePrice);
if (_closeQueue.Count > 4)
_closeQueue.Dequeue();

if (_closeQueue.Count < 4)
return;

var closeArr = _closeQueue.ToArray();
var fir = (4m * closeArr[^1] + 3m * closeArr[^2] + 2m * closeArr[^3] + closeArr[^4]) / 10m;
var trend = fir > _prevFir ? 1 : -1;
_prevFir = fir;

var longSignal = friction < lowf25 && trend == 1;
var shortSignal = friction < lowf25 && trend == -1;
var end = friction > midf5;

var portfolioValue = Portfolio.CurrentValue ?? 0m;
var contracts = Math.Min(Math.Max(0.000001m, (portfolioValue / candle.ClosePrice) * Leverage), 1000000000m);

if (longSignal && _barCount > 20 && Position <= 0)
BuyMarket(contracts + Math.Abs(Position));
else if (shortSignal && EnableShorts && _barCount > 20 && Position >= 0)
SellMarket(contracts + Math.Abs(Position));

if (Position > 0 && (shortSignal || end))
SellMarket(Math.Abs(Position));
else if (Position < 0 && EnableShorts && (longSignal || end))
BuyMarket(Math.Abs(Position));
}
}
