using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified Reflex & Trendflex crossover strategy.
/// </summary>
public class ReflexTrendflexStrategy : Strategy
{
private readonly StrategyParam<int> _reflexLen;
private readonly StrategyParam<int> _trendflexLen;
private readonly StrategyParam<DataType> _candleType;

private decimal _prevReflex;
private decimal _prevTrend;

public int ReflexLength { get => _reflexLen.Value; set => _reflexLen.Value = value; }
public int TrendflexLength { get => _trendflexLen.Value; set => _trendflexLen.Value = value; }
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

public ReflexTrendflexStrategy()
{
_reflexLen = Param(nameof(ReflexLength), 20)
.SetGreaterThanZero()
.SetDisplay("Reflex Length", "Reflex EMA length", "General");
_trendflexLen = Param(nameof(TrendflexLength), 20)
.SetGreaterThanZero()
.SetDisplay("Trendflex Length", "Trendflex EMA length", "General");
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Type of candles to process", "General");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
=> [(Security, CandleType)];

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();
_prevReflex = 0m;
_prevTrend = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var reflex = new EMA { Length = ReflexLength };
var trend = new EMA { Length = TrendflexLength };

var subscription = SubscribeCandles(CandleType);
subscription.Bind(reflex, trend, Process).Start();
}

private void Process(ICandleMessage candle, decimal reflexVal, decimal trendVal)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var prevDiff = _prevReflex - _prevTrend;
var currDiff = reflexVal - trendVal;

if (prevDiff <= 0 && currDiff > 0)
{
if (Position <= 0)
BuyMarket(Volume + Math.Abs(Position));
}
else if (prevDiff >= 0 && currDiff < 0)
{
if (Position >= 0)
SellMarket(Volume + Math.Abs(Position));
}

_prevReflex = reflexVal;
_prevTrend = trendVal;
}
}
