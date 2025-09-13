using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pivot-based breakout strategy.
/// Buys when close crosses above daily pivot.
/// Sells when close crosses below daily pivot.
/// Uses selected support/resistance level as stop and target.
/// Optionally closes positions at 23:00.
/// </summary>
public class TcpPivotStopStrategy : Strategy
{
private readonly StrategyParam<int> _targetLevel;
private readonly StrategyParam<bool> _intradayOnly;
private readonly StrategyParam<DataType> _candleType;

private decimal _pivot;
private decimal _res1;
private decimal _res2;
private decimal _res3;
private decimal _sup1;
private decimal _sup2;
private decimal _sup3;

private decimal _prevClose;
private decimal _targetPrice;
private decimal _stopPrice;

/// <summary>
/// Pivot level used for take profit and stop loss (1-3).
/// </summary>
public int TargetLevel
{
get => _targetLevel.Value;
set => _targetLevel.Value = value;
}

/// <summary>
/// Close positions at 23:00 if enabled.
/// </summary>
public bool IntradayOnly
{
get => _intradayOnly.Value;
set => _intradayOnly.Value = value;
}

/// <summary>
/// Time frame for trading.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Constructor.
/// </summary>
public TcpPivotStopStrategy()
{
_targetLevel = Param(nameof(TargetLevel), 3)
.SetDisplay("Target Level", "Pivot level used for take profit and stop loss (1-3)", "General")
.SetCanOptimize(true)
.SetOptimize(1, 3, 1);

_intradayOnly = Param(nameof(IntradayOnly), false)
.SetDisplay("Intraday Only", "Close positions at 23:00", "General");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
.SetDisplay("Candle Type", "Time frame for trading", "General");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
return [(Security, CandleType), (Security, TimeSpan.FromDays(1).TimeFrame())];
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();

_pivot = _res1 = _res2 = _res3 = _sup1 = _sup2 = _sup3 = 0m;
_prevClose = 0m;
_targetPrice = 0m;
_stopPrice = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

StartProtection();

var dailySubscription = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());

dailySubscription
.Bind(candle =>
{
if (candle.State != CandleStates.Finished)
return;

var high = candle.HighPrice;
var low = candle.LowPrice;
var close = candle.ClosePrice;

_pivot = (high + low + close) / 3m;
_res1 = 2m * _pivot - low;
_sup1 = 2m * _pivot - high;
var diff = _res1 - _sup1;
_res2 = _pivot + diff;
_sup2 = _pivot - diff;
_res3 = high + 2m * (_pivot - low);
_sup3 = low - 2m * (high - _pivot);
})
.Start();

var subscription = SubscribeCandles(CandleType);

subscription
.Bind(ProcessCandle)
.Start();
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (IntradayOnly)
{
var time = candle.OpenTime;
if (time.Hour == 23 && Position != 0)
{
if (Position > 0)
SellMarket(Math.Abs(Position));
else
BuyMarket(Math.Abs(Position));

return;
}
}

var close = candle.ClosePrice;

if (Position > 0 && (_targetPrice != 0m || _stopPrice != 0m))
{
if (close >= _targetPrice || close <= _stopPrice)
SellMarket(Math.Abs(Position));
}
else if (Position < 0 && (_targetPrice != 0m || _stopPrice != 0m))
{
if (close <= _targetPrice || close >= _stopPrice)
BuyMarket(Math.Abs(Position));
}
else if (Position == 0 && _pivot != 0m && _prevClose != 0m)
{
if (_prevClose <= _pivot && close > _pivot)
{
_targetPrice = GetTargetPrice(true);
_stopPrice = GetStopPrice(true);
BuyMarket(Volume);
}
else if (_prevClose >= _pivot && close < _pivot)
{
_targetPrice = GetTargetPrice(false);
_stopPrice = GetStopPrice(false);
SellMarket(Volume);
}
}

_prevClose = close;
}

private decimal GetTargetPrice(bool isLong)
{
return TargetLevel switch
{
1 => isLong ? _res1 : _sup1,
2 => isLong ? _res2 : _sup2,
_ => isLong ? _res3 : _sup3,
};
}

private decimal GetStopPrice(bool isLong)
{
return TargetLevel switch
{
1 => isLong ? _sup1 : _res1,
2 => isLong ? _sup2 : _res2,
_ => isLong ? _sup3 : _res3,
};
}
}
