namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Simple EMA crossover strategy with optional stop loss and take profit.
/// </summary>
public class SimulatorStrategy : Strategy
{
private readonly StrategyParam<int> _fastPeriod;
private readonly StrategyParam<int> _slowPeriod;
private readonly StrategyParam<decimal> _stopLoss;
private readonly StrategyParam<decimal> _takeProfit;
private readonly StrategyParam<DataType> _candleType;

private decimal _prevFast;
private decimal _prevSlow;
private decimal _entryPrice;
private decimal _stopPrice;
private decimal _takePrice;

/// <summary>
/// Fast EMA period.
/// </summary>
public int FastPeriod
{
get => _fastPeriod.Value;
set => _fastPeriod.Value = value;
}

/// <summary>
/// Slow EMA period.
/// </summary>
public int SlowPeriod
{
get => _slowPeriod.Value;
set => _slowPeriod.Value = value;
}

/// <summary>
/// Stop loss in price units.
/// </summary>
public decimal StopLoss
{
get => _stopLoss.Value;
set => _stopLoss.Value = value;
}

/// <summary>
/// Take profit in price units.
/// </summary>
public decimal TakeProfit
{
get => _takeProfit.Value;
set => _takeProfit.Value = value;
}

/// <summary>
/// Candle type used for calculations.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initializes a new instance of the strategy.
/// </summary>
public SimulatorStrategy()
{
_fastPeriod = Param(nameof(FastPeriod), 13)
.SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
.SetCanOptimize(true)
.SetOptimize(5, 30, 1);

_slowPeriod = Param(nameof(SlowPeriod), 50)
.SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
.SetCanOptimize(true)
.SetOptimize(20, 200, 5);

_stopLoss = Param(nameof(StopLoss), 0.005m)
.SetDisplay("Stop Loss", "Stop loss offset", "Risk");

_takeProfit = Param(nameof(TakeProfit), 0.005m)
.SetDisplay("Take Profit", "Take profit offset", "Risk");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var fastEma = new ExponentialMovingAverage { Length = FastPeriod };
var slowEma = new ExponentialMovingAverage { Length = SlowPeriod };

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(fastEma, slowEma, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (_prevFast == default || _prevSlow == default)
{
_prevFast = fast;
_prevSlow = slow;
return;
}

if (Position > 0)
{
if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
ClosePosition();
}
else if (Position < 0)
{
if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
ClosePosition();
}

if (_prevFast <= _prevSlow && fast > slow)
{
if (Position < 0)
ClosePosition();

if (Position <= 0)
{
BuyMarket();
_entryPrice = candle.ClosePrice;
_stopPrice = _entryPrice - StopLoss;
_takePrice = _entryPrice + TakeProfit;
}
}
else if (_prevFast >= _prevSlow && fast < slow)
{
if (Position > 0)
ClosePosition();

if (Position >= 0)
{
SellMarket();
_entryPrice = candle.ClosePrice;
_stopPrice = _entryPrice + StopLoss;
_takePrice = _entryPrice - TakeProfit;
}
}

_prevFast = fast;
_prevSlow = slow;
}
}

