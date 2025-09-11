using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RedK slow smooth weighted moving average strategy.
/// Enters long when triple-smoothed WMA turns upward.
/// Enters short when it turns downward.
/// </summary>
public class RedkSlowSmoothAverageRssWmaStrategy : Strategy
{
private readonly StrategyParam<int> _combinedSmoothness;
private readonly StrategyParam<DataType> _candleType;

private decimal? _prevLl;
private bool? _prevUptrend;

/// <summary>
/// Combined smoothness value.
/// </summary>
public int CombinedSmoothness
{
get => _combinedSmoothness.Value;
set => _combinedSmoothness.Value = value;
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
/// Initializes <see cref="RedkSlowSmoothAverageRssWmaStrategy"/>.
/// </summary>
public RedkSlowSmoothAverageRssWmaStrategy()
{
_combinedSmoothness = Param(nameof(CombinedSmoothness), 15)
.SetGreaterThanZero()
.SetDisplay("Combined Smoothness", "Total smoothness for triple WMA", "Parameters")
.SetCanOptimize(true)
.SetOptimize(5, 30, 1);

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
_prevLl = null;
_prevUptrend = null;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var length = CombinedSmoothness;
int w1, w2, w3;

if (length > 2)
{
var w = length / 3m;
w2 = (int)Math.Round(w);
w1 = (int)Math.Round((length - w2) / 2m);
w3 = (int)((length - w2) / 2m);
}
else
{
w1 = w2 = w3 = 1;
}

var wma1 = new WeightedMovingAverage { Length = w1 };
var wma2 = new WeightedMovingAverage { Length = w2 };
var wma3 = new WeightedMovingAverage { Length = w3 };

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(candle => ProcessCandle(candle, wma1, wma2, wma3))
.Start();
}

private void ProcessCandle(ICandleMessage candle, WeightedMovingAverage wma1, WeightedMovingAverage wma2, WeightedMovingAverage wma3)
{
if (candle.State != CandleStates.Finished)
return;

var v1 = wma1.Process(new DecimalIndicatorValue(wma1, candle.ClosePrice, candle.OpenTime));
if (!v1.IsFormed)
return;

var l1 = v1.ToDecimal();

var v2 = wma2.Process(new DecimalIndicatorValue(wma2, l1, candle.OpenTime));
if (!v2.IsFormed)
return;

var l2 = v2.ToDecimal();

var v3 = wma3.Process(new DecimalIndicatorValue(wma3, l2, candle.OpenTime));
if (!v3.IsFormed)
return;

var ll = v3.ToDecimal();

if (_prevLl is null)
{
_prevLl = ll;
_prevUptrend = false;
return;
}

var uptrend = ll > _prevLl;
var swingUp = uptrend && _prevUptrend == false;
var swingDown = !uptrend && _prevUptrend == true;

if (swingUp && Position <= 0)
{
BuyMarket(Volume + Math.Abs(Position));
}
else if (swingDown && Position >= 0)
{
SellMarket(Volume + Math.Abs(Position));
}

_prevLl = ll;
_prevUptrend = uptrend;
}
}

