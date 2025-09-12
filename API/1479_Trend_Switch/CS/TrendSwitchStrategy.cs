using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified trend switch strategy using linear regression slope and adaptive moving averages.
/// </summary>
public class TrendSwitchStrategy : Strategy
{
private readonly StrategyParam<int> _length;
private readonly StrategyParam<decimal> _threshold;
private readonly StrategyParam<decimal> _fastLimit;
private readonly StrategyParam<decimal> _slowLimit;
private readonly StrategyParam<decimal> _stopLoss;
private readonly StrategyParam<DataType> _candleType;

private LinearRegression _slope = null!;
private KaufmanAdaptiveMovingAverage _kama = null!;
private WeightedMovingAverage _trigger = null!;

private decimal _entryPrice;

/// <summary>
/// Lookback length.
/// </summary>
public int Length { get => _length.Value; set => _length.Value = value; }

/// <summary>
/// Slope threshold.
/// </summary>
public decimal Threshold { get => _threshold.Value; set => _threshold.Value = value; }

/// <summary>
/// Fast limit for adaptive moving average.
/// </summary>
public decimal FastLimit { get => _fastLimit.Value; set => _fastLimit.Value = value; }

/// <summary>
/// Slow limit for adaptive moving average.
/// </summary>
public decimal SlowLimit { get => _slowLimit.Value; set => _slowLimit.Value = value; }

/// <summary>
/// Stop loss percent.
/// </summary>
public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

/// <summary>
/// Candle type.
/// </summary>
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

public TrendSwitchStrategy()
{
_length = Param(nameof(Length), 10)
.SetGreaterThanZero()
.SetDisplay("Length", "Lookback period", "General")
.SetCanOptimize(true);
_threshold = Param(nameof(Threshold), 1m)
.SetGreaterThanZero()
.SetDisplay("Slope Threshold", "Minimal slope", "General")
.SetCanOptimize(true);
_fastLimit = Param(nameof(FastLimit), 0.5m)
.SetGreaterThanZero()
.SetDisplay("Fast Limit", "Fast limit for KAMA", "Indicators");
_slowLimit = Param(nameof(SlowLimit), 0.05m)
.SetGreaterThanZero()
.SetDisplay("Slow Limit", "Slow limit for KAMA", "Indicators");
_stopLoss = Param(nameof(StopLoss), 3m)
.SetGreaterThanZero()
.SetDisplay("Stop Loss %", "Stop loss percent", "Risk");
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
_slope = null!;
_kama = null!;
_trigger = null!;
_entryPrice = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_slope = new LinearRegression { Length = Length };
_kama = new KaufmanAdaptiveMovingAverage { Length = Length, FastSCPeriod = FastLimit, SlowSCPeriod = SlowLimit };
_trigger = new WeightedMovingAverage { Length = Length };

var subscription = SubscribeCandles(CandleType);
subscription.BindEx(_slope, _kama, _trigger, ProcessCandle).Start();
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue slopeValue, IIndicatorValue kamaValue, IIndicatorValue triggerValue)
{
if (candle.State != CandleStates.Finished)
return;

var slopeVal = ((LinearRegressionValue)slopeValue).LinearReg;
if (slopeVal is not decimal slope)
return;

var kama = kamaValue.ToDecimal();
var trigger = triggerValue.ToDecimal();
var upTrend = slope > Threshold && kama > trigger;
var downTrend = slope < -Threshold && kama < trigger;

if (Position == 0)
{
if (upTrend)
{
BuyMarket();
_entryPrice = candle.ClosePrice;
}
else if (downTrend)
{
SellMarket();
_entryPrice = candle.ClosePrice;
}
}
else if (Position > 0)
{
var stop = _entryPrice * (1m - StopLoss / 100m);
if (candle.LowPrice <= stop || downTrend)
SellMarket(Position);
}
else if (Position < 0)
{
var stop = _entryPrice * (1m + StopLoss / 100m);
if (candle.HighPrice >= stop || upTrend)
BuyMarket(-Position);
}
}
}
