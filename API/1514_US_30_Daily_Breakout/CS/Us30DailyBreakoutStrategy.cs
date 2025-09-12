using System;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// US 30 Daily Breakout Strategy.
/// </summary>
public class Us30DailyBreakoutStrategy : Strategy
{
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<decimal> _takeProfitPips;
private readonly StrategyParam<decimal> _stopLossPips;

private ICandleMessage _prevDayCandle;
private decimal _prevDayHigh;
private decimal _prevDayLow;
private bool _breakoutTraded;
private bool _breakdownTraded;
private decimal _entryPrice;

/// <summary>
/// Candle type for intraday trading.
/// </summary>
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

/// <summary>
/// Take profit in points.
/// </summary>
public decimal TakeProfitPips { get => _takeProfitPips.Value; set => _takeProfitPips.Value = value; }

/// <summary>
/// Stop loss in points.
/// </summary>
public decimal StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }

public Us30DailyBreakoutStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Intraday candles", "General");

_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
.SetGreaterThanZero()
.SetDisplay("Take Profit", "Take profit points", "Risk");

_stopLossPips = Param(nameof(StopLossPips), 50m)
.SetGreaterThanZero()
.SetDisplay("Stop Loss", "Stop loss points", "Risk");
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();
_prevDayCandle = null;
_prevDayHigh = 0m;
_prevDayLow = 0m;
_breakoutTraded = false;
_breakdownTraded = false;
_entryPrice = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var intraday = SubscribeCandles(CandleType);
intraday.Bind(ProcessIntraday).Start();

var daily = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
daily.Bind(ProcessDaily).Start();
}

private void ProcessDaily(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

if (_prevDayCandle != null)
{
_prevDayHigh = _prevDayCandle.HighPrice;
_prevDayLow = _prevDayCandle.LowPrice;
_breakoutTraded = false;
_breakdownTraded = false;
}

_prevDayCandle = candle;
}

private void ProcessIntraday(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (Position == 0)
{
if (!_breakoutTraded && candle.ClosePrice > _prevDayHigh)
{
BuyMarket();
_entryPrice = candle.ClosePrice;
_breakoutTraded = true;
}
else if (!_breakdownTraded && candle.ClosePrice < _prevDayLow)
{
SellMarket();
_entryPrice = candle.ClosePrice;
_breakdownTraded = true;
}
}
else if (Position > 0)
{
if (candle.ClosePrice >= _entryPrice + TakeProfitPips || candle.ClosePrice <= _entryPrice - StopLossPips)
SellMarket(Position);
}
else if (Position < 0)
{
if (candle.ClosePrice <= _entryPrice - TakeProfitPips || candle.ClosePrice >= _entryPrice + StopLossPips)
BuyMarket(Math.Abs(Position));
}
}
}
