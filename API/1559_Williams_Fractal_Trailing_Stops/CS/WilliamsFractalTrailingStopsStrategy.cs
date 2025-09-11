using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using Williams fractals as trailing stops.
/// Position flips when price breaks the trailing stop level.
/// </summary>
public class WilliamsFractalTrailingStopsStrategy : Strategy
{
private readonly StrategyParam<decimal> _bufferPercent;
private readonly StrategyParam<DataType> _candleType;

private readonly Queue<decimal> _highs = new();
private readonly Queue<decimal> _lows = new();
private decimal? _longStop;
private decimal? _shortStop;

/// <summary>
/// Buffer percentage added to fractal price.
/// </summary>
public decimal BufferPercent
{
get => _bufferPercent.Value;
set => _bufferPercent.Value = value;
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
/// Initializes <see cref="WilliamsFractalTrailingStopsStrategy"/>.
/// </summary>
public WilliamsFractalTrailingStopsStrategy()
{
_bufferPercent = Param(nameof(BufferPercent), 0m)
.SetDisplay("Stop Buffer %", "Percent buffer added to fractal price", "General");

_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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
_highs.Clear();
_lows.Clear();
_longStop = null;
_shortStop = null;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var subscription = SubscribeCandles(CandleType);
subscription.Bind(ProcessCandle).Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

_highs.Enqueue(candle.HighPrice);
_lows.Enqueue(candle.LowPrice);

if (_highs.Count > 5)
_highs.Dequeue();
if (_lows.Count > 5)
_lows.Dequeue();

if (_highs.Count == 5 && _lows.Count == 5)
{
var hs = _highs.ToArray();
var ls = _lows.ToArray();

if (hs[2] > hs[0] && hs[2] > hs[1] && hs[2] > hs[3] && hs[2] > hs[4])
{
var price = hs[2] * (1m + BufferPercent / 100m);
_shortStop = _shortStop is decimal s ? Math.Min(s, price) : price;
}

if (ls[2] < ls[0] && ls[2] < ls[1] && ls[2] < ls[3] && ls[2] < ls[4])
{
var price = ls[2] * (1m - BufferPercent / 100m);
_longStop = _longStop is decimal l ? Math.Max(l, price) : price;
}
}

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (_shortStop is decimal shortStop && candle.ClosePrice > shortStop && Position <= 0)
{
BuyMarket(Volume + Math.Abs(Position));
}
else if (_longStop is decimal longStop && candle.ClosePrice < longStop && Position >= 0)
{
SellMarket(Volume + Math.Abs(Position));
}
}
}
