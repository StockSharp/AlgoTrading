using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triple moving average crossover strategy.
/// Enters long when fast MA is above middle and middle above slow. Short when opposite.
/// </summary>
public class Vector3Strategy : Strategy
{
private readonly StrategyParam<int> _fastLength;
private readonly StrategyParam<int> _middleLength;
private readonly StrategyParam<int> _slowLength;
private readonly StrategyParam<DataType> _candleType;

private SimpleMovingAverage _fastMa = null!;
private SimpleMovingAverage _middleMa = null!;
private SimpleMovingAverage _slowMa = null!;

/// <summary>
/// Fast MA length.
/// </summary>
public int FastLength
{
get => _fastLength.Value;
set => _fastLength.Value = value;
}

/// <summary>
/// Middle MA length.
/// </summary>
public int MiddleLength
{
get => _middleLength.Value;
set => _middleLength.Value = value;
}

/// <summary>
/// Slow MA length.
/// </summary>
public int SlowLength
{
get => _slowLength.Value;
set => _slowLength.Value = value;
}

/// <summary>
/// Candle type to process.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initializes a new instance of <see cref="Vector3Strategy"/>.
/// </summary>
public Vector3Strategy()
{
_fastLength = Param(nameof(FastLength), 10)
.SetGreaterThanZero()
.SetDisplay("Fast Length", "Fast moving average period", "General")
.SetCanOptimize(true);

_middleLength = Param(nameof(MiddleLength), 50)
.SetGreaterThanZero()
.SetDisplay("Middle Length", "Middle moving average period", "General")
.SetCanOptimize(true);

_slowLength = Param(nameof(SlowLength), 100)
.SetGreaterThanZero()
.SetDisplay("Slow Length", "Slow moving average period", "General")
.SetCanOptimize(true);

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

_fastMa = default!;
_middleMa = default!;
_slowMa = default!;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_fastMa = new SimpleMovingAverage { Length = FastLength };
_middleMa = new SimpleMovingAverage { Length = MiddleLength };
_slowMa = new SimpleMovingAverage { Length = SlowLength };

var subscription = SubscribeCandles(CandleType);
subscription.Bind(_fastMa, _middleMa, _slowMa, ProcessCandle).Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _fastMa);
DrawIndicator(area, _middleMa);
DrawIndicator(area, _slowMa);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal fast, decimal middle, decimal slow)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var isLong = fast > middle && middle > slow;
var isShort = fast < middle && middle < slow;

if (isLong && Position <= 0)
{
var volume = Volume + (Position < 0 ? -Position : 0m);
BuyMarket(volume);
}
else if (isShort && Position >= 0)
{
var volume = Volume + (Position > 0 ? Position : 0m);
SellMarket(volume);
}
}
}

