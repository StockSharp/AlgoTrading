using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triple moving average crossover strategy with optional trailing stop.
/// </summary>
public class Up3x1Strategy : Strategy
{
private readonly StrategyParam<decimal> _volume;
private readonly StrategyParam<decimal> _takeProfit;
private readonly StrategyParam<decimal> _stopLoss;
private readonly StrategyParam<decimal> _trailingStop;
private readonly StrategyParam<int> _fastPeriod;
private readonly StrategyParam<int> _middlePeriod;
private readonly StrategyParam<int> _slowPeriod;
private readonly StrategyParam<DataType> _candleType;

private readonly SimpleMovingAverage _fastMa;
private readonly SimpleMovingAverage _middleMa;
private readonly SimpleMovingAverage _slowMa;

// Stored indicator values from the previous candle.
private decimal _fastPrev;
private decimal _middlePrev;
private decimal _slowPrev;
private bool _isInitialized;

// Entry price and current stop level for the open position.
private decimal _entryPrice;
private decimal _currentStop;

public Up3x1Strategy()
{
_volume = Param<decimal>(nameof(Volume), 0.1m)
.SetDisplay("Volume");
_takeProfit = Param<decimal>(nameof(TakeProfit), 150m)
.SetDisplay("Take Profit")
.SetCanOptimize(true);
_stopLoss = Param<decimal>(nameof(StopLoss), 100m)
.SetDisplay("Stop Loss")
.SetCanOptimize(true);
_trailingStop = Param<decimal>(nameof(TrailingStop), 100m)
.SetDisplay("Trailing Stop")
.SetCanOptimize(true);
_fastPeriod = Param(nameof(FastPeriod), 24)
.SetDisplay("Fast Period")
.SetCanOptimize(true);
_middlePeriod = Param(nameof(MiddlePeriod), 60)
.SetDisplay("Middle Period")
.SetCanOptimize(true);
_slowPeriod = Param(nameof(SlowPeriod), 120)
.SetDisplay("Slow Period")
.SetCanOptimize(true);
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type");

_fastMa = new SimpleMovingAverage { Length = FastPeriod };
_middleMa = new SimpleMovingAverage { Length = MiddlePeriod };
_slowMa = new SimpleMovingAverage { Length = SlowPeriod };
}

public decimal Volume { get => _volume.Value; set => _volume.Value = value; }
public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
public int MiddlePeriod { get => _middlePeriod.Value; set => _middlePeriod.Value = value; }
public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_fastMa.Length = FastPeriod;
_middleMa.Length = MiddlePeriod;
_slowMa.Length = SlowPeriod;

StartProtection();

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(_fastMa, _middleMa, _slowMa, ProcessCandle)
.Start();
}

private void ProcessCandle(ICandleMessage candle, decimal fast, decimal middle, decimal slow)
{
if (candle.State != CandleStates.Finished)
return;

// Use candle close price for calculations.
var price = candle.ClosePrice;

if (!_isInitialized)
{
_fastPrev = fast;
_middlePrev = middle;
_slowPrev = slow;
_isInitialized = true;
return;
}

// Generate signals when the fast and middle SMAs cross
// and are located relative to the slow SMA.
var buySignal = _fastPrev < _middlePrev && fast > middle && fast < slow && middle < slow;
var sellSignal = _fastPrev > _middlePrev && fast < middle && fast > slow && middle > slow;

_fastPrev = fast;
_middlePrev = middle;
_slowPrev = slow;

// No active position - check entry conditions.
if (Position == 0)
{
if (buySignal)
{
BuyMarket(Volume);
_entryPrice = price;
_currentStop = price - StopLoss;
}
else if (sellSignal)
{
SellMarket(Volume);
_entryPrice = price;
_currentStop = price + StopLoss;
}
return;
}

if (Position > 0)
{
// Close long position on take profit.
if (price - _entryPrice >= TakeProfit)
{
SellMarket(Position);
return;
}

if (TrailingStop > 0m)
{
// Adjust trailing stop to lock in profits.
_currentStop = Math.Max(_currentStop, price - TrailingStop);
if (price <= _currentStop)
SellMarket(Position);
}
else if (price <= _currentStop)
{
SellMarket(Position);
}
}
else if (Position < 0)
{
// Close short position on take profit.
if (_entryPrice - price >= TakeProfit)
{
BuyMarket(-Position);
return;
}

if (TrailingStop > 0m)
{
// Adjust trailing stop for short position.
_currentStop = Math.Min(_currentStop, price + TrailingStop);
if (price >= _currentStop)
BuyMarket(-Position);
}
else if (price >= _currentStop)
{
BuyMarket(-Position);
}
}
}
}

