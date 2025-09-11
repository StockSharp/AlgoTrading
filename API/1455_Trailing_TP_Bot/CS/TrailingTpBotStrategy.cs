using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SMA crossover strategy with trailing take profit and stop loss.
/// </summary>
public class TrailingTpBotStrategy : Strategy
{
private readonly StrategyParam<int> _fastLength;
private readonly StrategyParam<int> _slowLength;
private readonly StrategyParam<decimal> _longTp;
private readonly StrategyParam<decimal> _shortTp;
private readonly StrategyParam<bool> _enableTrail;
private readonly StrategyParam<decimal> _trailPercent;
private readonly StrategyParam<decimal> _stopLoss;
private readonly StrategyParam<DataType> _candleType;

private bool _wasFastBelow;
private decimal _entryPrice;
private bool _trailActive;
private decimal _trailLevel;

/// <summary>
/// Fast SMA period length.
/// </summary>
public int FastLength
{
get => _fastLength.Value;
set => _fastLength.Value = value;
}

/// <summary>
/// Slow SMA period length.
/// </summary>
public int SlowLength
{
get => _slowLength.Value;
set => _slowLength.Value = value;
}

/// <summary>
/// Long take profit percent.
/// </summary>
public decimal LongTakeProfit
{
get => _longTp.Value;
set => _longTp.Value = value;
}

/// <summary>
/// Short take profit percent.
/// </summary>
public decimal ShortTakeProfit
{
get => _shortTp.Value;
set => _shortTp.Value = value;
}

/// <summary>
/// Enable trailing take profit.
/// </summary>
public bool EnableTrail
{
get => _enableTrail.Value;
set => _enableTrail.Value = value;
}

/// <summary>
/// Trailing distance percent.
/// </summary>
public decimal TrailPercent
{
get => _trailPercent.Value;
set => _trailPercent.Value = value;
}

/// <summary>
/// Stop loss percent.
/// </summary>
public decimal StopLossPercent
{
get => _stopLoss.Value;
set => _stopLoss.Value = value;
}

/// <summary>
/// Candle type.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Constructor.
/// </summary>
public TrailingTpBotStrategy()
{
_fastLength = Param(nameof(FastLength), 23)
.SetGreaterThanZero()
.SetDisplay("Fast SMA", "Fast moving average", "Parameters");

_slowLength = Param(nameof(SlowLength), 50)
.SetGreaterThanZero()
.SetDisplay("Slow SMA", "Slow moving average", "Parameters");

_longTp = Param(nameof(LongTakeProfit), 0.5m)
.SetDisplay("Long TP %", "Long take profit", "Risk");

_shortTp = Param(nameof(ShortTakeProfit), 0.5m)
.SetDisplay("Short TP %", "Short take profit", "Risk");

_enableTrail = Param(nameof(EnableTrail), true)
.SetDisplay("Enable Trailing", "Use trailing", "Risk");

_trailPercent = Param(nameof(TrailPercent), 1m)
.SetDisplay("Trail %", "Trailing distance", "Risk");

_stopLoss = Param(nameof(StopLossPercent), 3m)
.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Timeframe", "General");
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
_wasFastBelow = false;
_entryPrice = 0m;
_trailActive = false;
_trailLevel = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var fast = new SMA { Length = FastLength };
var slow = new SMA { Length = SlowLength };

var subscription = SubscribeCandles(CandleType);

subscription
.Bind(fast, slow, ProcessCandle)
.Start();
}

private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var isFastBelow = fastValue < slowValue;

if (_wasFastBelow && !isFastBelow && Position <= 0)
{
_entryPrice = candle.ClosePrice;
_trailActive = false;
BuyMarket(Volume + Math.Abs(Position));
}
else if (!_wasFastBelow && isFastBelow && Position >= 0)
{
_entryPrice = candle.ClosePrice;
_trailActive = false;
SellMarket(Volume + Math.Abs(Position));
}

_wasFastBelow = isFastBelow;

if (Position > 0)
{
var tp = _entryPrice * (1 + LongTakeProfit / 100m);
var sl = _entryPrice * (1 - StopLossPercent / 100m);

if (candle.LowPrice <= sl)
{
SellMarket(Math.Abs(Position));
return;
}

if (EnableTrail)
{
if (!_trailActive)
{
if (candle.HighPrice >= tp)
{
_trailActive = true;
_trailLevel = candle.HighPrice;
}
}
else
{
_trailLevel = Math.Max(_trailLevel, candle.HighPrice);
var stop = _trailLevel * (1 - TrailPercent / 100m);
if (candle.ClosePrice <= stop)
{
SellMarket(Math.Abs(Position));
_trailActive = false;
}
}
}
else if (candle.HighPrice >= tp)
{
SellMarket(Math.Abs(Position));
}
}
else if (Position < 0)
{
var tp = _entryPrice * (1 - ShortTakeProfit / 100m);
var sl = _entryPrice * (1 + StopLossPercent / 100m);

if (candle.HighPrice >= sl)
{
BuyMarket(Math.Abs(Position));
return;
}

if (EnableTrail)
{
if (!_trailActive)
{
if (candle.LowPrice <= tp)
{
_trailActive = true;
_trailLevel = candle.LowPrice;
}
}
else
{
_trailLevel = Math.Min(_trailLevel, candle.LowPrice);
var stop = _trailLevel * (1 + TrailPercent / 100m);
if (candle.ClosePrice >= stop)
{
BuyMarket(Math.Abs(Position));
_trailActive = false;
}
}
}
else if (candle.LowPrice <= tp)
{
BuyMarket(Math.Abs(Position));
}
}
}
}
