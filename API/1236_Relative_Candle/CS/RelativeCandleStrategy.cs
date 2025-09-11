using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Compares candle changes with an index.
/// </summary>
public class RelativeCandleStrategy : Strategy
{
private readonly StrategyParam<string> _indexSymbol;
private readonly StrategyParam<int> _averageCloseLength;
private readonly StrategyParam<decimal> _averageZoomFactor;
private readonly StrategyParam<DataType> _candleType;

private Security _indexSecurity;
private decimal? _prevClose;
private decimal? _idxPrevClose;
private decimal _idxOpen;
private decimal _idxHigh;
private decimal _idxLow;
private decimal _idxClose;

private SMA _averageRelativeClose;

public string IndexSymbol { get => _indexSymbol.Value; set => _indexSymbol.Value = value; }
public int AverageCloseLength { get => _averageCloseLength.Value; set => _averageCloseLength.Value = value; }
public decimal AverageZoomFactor { get => _averageZoomFactor.Value; set => _averageZoomFactor.Value = value; }
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

public RelativeCandleStrategy()
{
_indexSymbol = Param(nameof(IndexSymbol), "IXIC")
.SetDisplay("Index Symbol", "Symbol used for comparison", "General");

_averageCloseLength = Param(nameof(AverageCloseLength), 10)
.SetGreaterThanZero()
.SetDisplay("Average Close Length", "Period for relative close SMA", "General");

_averageZoomFactor = Param(nameof(AverageZoomFactor), 5m)
.SetGreaterThanZero()
.SetDisplay("Average Zoom Factor", "Scaling for average relative close", "General");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Type of candles to use", "General");
}

public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
_indexSecurity = new Security { Id = IndexSymbol };
return [(Security, CandleType), (_indexSecurity, CandleType)];
}

protected override void OnReseted()
{
base.OnReseted();

_prevClose = null;
_idxPrevClose = null;
_idxOpen = 0m;
_idxHigh = 0m;
_idxLow = 0m;
_idxClose = 0m;
}

protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_averageRelativeClose = new SMA { Length = AverageCloseLength };

var indexSubscription = SubscribeCandles(_indexSecurity, CandleType);
indexSubscription.Bind(ProcessIndex).Start();

var subscription = SubscribeCandles(CandleType);
subscription.Bind(ProcessCandle).Start();
}

private void ProcessIndex(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

_idxOpen = candle.OpenPrice;
_idxHigh = candle.HighPrice;
_idxLow = candle.LowPrice;
_idxPrevClose = _idxClose;
_idxClose = candle.ClosePrice;
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

if (_idxPrevClose is null || _prevClose is null)
{
_prevClose = candle.ClosePrice;
return;
}

var chgIdxOpen = (_idxOpen / _idxPrevClose.Value) - 1m;
var chgIdxClose = (_idxClose / _idxPrevClose.Value) - 1m;
var idxRange = _idxHigh - _idxLow;
if (idxRange == 0m)
return;
var chgIdxCloseRange = (_idxClose - _idxLow) / idxRange;

var chgOpen = (candle.OpenPrice / _prevClose.Value) - 1m;
var chgClose = (candle.ClosePrice / _prevClose.Value) - 1m;
var range = candle.HighPrice - candle.LowPrice;
if (range == 0m)
return;
var chgCloseRange = (candle.ClosePrice - candle.LowPrice) / range;

var relativeOpen = (chgOpen - chgIdxOpen) * 100m;
var relativeClose = (chgClose - chgIdxClose) * 100m;
var relativeCloseRange = Math.Abs(relativeClose - relativeOpen) * (chgCloseRange - chgIdxCloseRange);
var relativeHigh = relativeCloseRange < 0m
? Math.Max(relativeClose, relativeOpen) - relativeCloseRange
: Math.Max(relativeOpen, relativeClose);
var relativeLow = relativeCloseRange >= 0m
? Math.Min(relativeOpen, relativeClose) - relativeCloseRange
: Math.Min(relativeOpen, relativeClose);

var avgValue = _averageRelativeClose.Process(relativeClose);
if (avgValue.IsFinal && avgValue.TryGetValue(out var avg))
{
var scaled = avg * AverageZoomFactor;
AddInfoLog($"RelO={relativeOpen:F2} RelH={relativeHigh:F2} RelL={relativeLow:F2} RelC={relativeClose:F2} AvgRelC={scaled:F2}");
}

_prevClose = candle.ClosePrice;
}
}
