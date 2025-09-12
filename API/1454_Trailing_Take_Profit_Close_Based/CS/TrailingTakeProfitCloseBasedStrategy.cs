using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SMA crossover with optional trailing take profit.
/// </summary>
public class TrailingTakeProfitCloseBasedStrategy : Strategy
{
private readonly StrategyParam<int> _fastLength;
private readonly StrategyParam<int> _slowLength;
private readonly StrategyParam<decimal> _takeProfit;
private readonly StrategyParam<decimal> _trailDistance;
private readonly StrategyParam<bool> _enableTrail;
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
/// Take profit percentage.
/// </summary>
public decimal TakeProfit
{
get => _takeProfit.Value;
set => _takeProfit.Value = value;
}

/// <summary>
/// Trailing distance percentage.
/// </summary>
public decimal TrailDistance
{
get => _trailDistance.Value;
set => _trailDistance.Value = value;
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
public TrailingTakeProfitCloseBasedStrategy()
{
_fastLength = Param(nameof(FastLength), 21)
.SetGreaterThanZero()
.SetDisplay("Fast SMA", "Fast moving average", "Parameters");

_slowLength = Param(nameof(SlowLength), 49)
.SetGreaterThanZero()
.SetDisplay("Slow SMA", "Slow moving average", "Parameters");

_takeProfit = Param(nameof(TakeProfit), 7m)
.SetDisplay("Take Profit %", "Target profit percentage", "Risk");

_trailDistance = Param(nameof(TrailDistance), 1m)
.SetDisplay("Trail Distance %", "Trailing distance after activation", "Risk");

_enableTrail = Param(nameof(EnableTrail), true)
.SetDisplay("Enable Trailing", "Use trailing after take profit hit", "Risk");

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
else if (!_wasFastBelow && isFastBelow && Position > 0)
{
SellMarket(Math.Abs(Position));
}

_wasFastBelow = isFastBelow;

if (Position > 0)
{
var target = _entryPrice * (1 + TakeProfit / 100m);

if (EnableTrail)
{
if (!_trailActive)
{
if (candle.HighPrice >= target)
{
_trailActive = true;
_trailLevel = candle.HighPrice;
}
}
else
{
_trailLevel = Math.Max(_trailLevel, candle.HighPrice);
var stop = _trailLevel * (1 - TrailDistance / 100m);
if (candle.ClosePrice <= stop)
{
SellMarket(Math.Abs(Position));
_trailActive = false;
}
}
}
else
{
if (candle.HighPrice >= target)
{
SellMarket(Math.Abs(Position));
}
}
}
}
}
