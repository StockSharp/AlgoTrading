using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triple moving average crossover strategy with trend confirmation.
/// Enters long when fast &gt; middle &gt; slow and short on the opposite order.
/// </summary>
public class WodismaTripleMaCrossoverStrategy : Strategy
{
private readonly StrategyParam<int> _fastLength;
private readonly StrategyParam<int> _midLength;
private readonly StrategyParam<int> _slowLength;
private readonly StrategyParam<DataType> _candleType;

/// <summary>
/// Fast MA period.
/// </summary>
public int FastLength
{
get => _fastLength.Value;
set => _fastLength.Value = value;
}

/// <summary>
/// Middle MA period.
/// </summary>
public int MidLength
{
get => _midLength.Value;
set => _midLength.Value = value;
}

/// <summary>
/// Slow MA period.
/// </summary>
public int SlowLength
{
get => _slowLength.Value;
set => _slowLength.Value = value;
}

/// <summary>
/// Candle type used by the strategy.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initializes <see cref="WodismaTripleMaCrossoverStrategy"/>.
/// </summary>
public WodismaTripleMaCrossoverStrategy()
{
_fastLength = Param(nameof(FastLength), 5)
.SetGreaterThanZero()
.SetDisplay("Fast MA", "Fast MA period", "MA");

_midLength = Param(nameof(MidLength), 20)
.SetGreaterThanZero()
.SetDisplay("Mid MA", "Middle MA period", "MA");

_slowLength = Param(nameof(SlowLength), 50)
.SetGreaterThanZero()
.SetDisplay("Slow MA", "Slow MA period", "MA");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
return [(Security, CandleType)];
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var fast = new SMA { Length = FastLength };
var mid = new SMA { Length = MidLength };
var slow = new SMA { Length = SlowLength };

var subscription = SubscribeCandles(CandleType);
subscription.Bind(fast, mid, slow, ProcessCandle).Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, fast);
DrawIndicator(area, mid);
DrawIndicator(area, slow);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal fast, decimal mid, decimal slow)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (fast > mid && mid > slow && Position <= 0)
{
BuyMarket(Volume + Math.Abs(Position));
}
else if (fast < mid && mid < slow && Position >= 0)
{
SellMarket(Volume + Math.Abs(Position));
}
}
}
