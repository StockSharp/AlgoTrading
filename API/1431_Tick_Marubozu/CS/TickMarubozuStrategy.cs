using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Tick Marubozu strategy with volume confirmation.
/// </summary>
public class TickMarubozuStrategy : Strategy
{
private readonly StrategyParam<decimal> _tickSize;
private readonly StrategyParam<int> _volLength;
private readonly StrategyParam<DataType> _candleType;

private SimpleMovingAverage _volSma;

/// <summary>
/// Minimum body and wick size.
/// </summary>
public decimal TickSize
{
get => _tickSize.Value;
set => _tickSize.Value = value;
}

/// <summary>
/// Length of volume SMA.
/// </summary>
public int VolLength
{
get => _volLength.Value;
set => _volLength.Value = value;
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
/// Initializes a new instance of <see cref="TickMarubozuStrategy"/>.
/// </summary>
public TickMarubozuStrategy()
{
_tickSize = Param(nameof(TickSize), 5m)
.SetGreaterThanZero()
.SetDisplay("Tick Size", "Minimum body/wick size", "General");

_volLength = Param(nameof(VolLength), 20)
.SetGreaterThanZero()
.SetDisplay("Volume SMA Length", "Length of volume average", "General")
.SetCanOptimize(true);

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
yield return (Security, CandleType);
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();
_volSma = null;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_volSma = new SimpleMovingAverage { Length = VolLength };

var subscription = SubscribeCandles(CandleType);
subscription.Bind(ProcessCandle).Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _volSma);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
var highWick = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
var lowWick = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;

var volValue = _volSma.Process(candle.TotalVolume);
if (!volValue.IsFinal)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var volMa = volValue.ToDecimal();
var highVolume = candle.TotalVolume > volMa;

var bearish = candle.ClosePrice < candle.OpenPrice && highWick <= TickSize && body >= TickSize;
var bullish = candle.ClosePrice > candle.OpenPrice && lowWick <= TickSize && body >= TickSize;

if (bullish && highVolume && Position <= 0)
{
var volume = Volume + (Position < 0 ? -Position : 0m);
BuyMarket(volume);
}
else if (bearish && highVolume && Position >= 0)
{
var volume = Volume + (Position > 0 ? Position : 0m);
SellMarket(volume);
}
}
}
