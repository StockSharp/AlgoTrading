using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pivot Point Supertrend strategy.
/// Calculates a dynamic center line from pivot points and ATR-based bands
/// to detect trend reversals.
/// </summary>
public class PivotPointSupertrendStrategy : Strategy
{
private readonly StrategyParam<int> _pivotPeriod;
private readonly StrategyParam<decimal> _atrFactor;
private readonly StrategyParam<int> _atrPeriod;
private readonly StrategyParam<DataType> _candleType;

private AverageTrueRange _atr;

private decimal[] _highBuffer;
private decimal[] _lowBuffer;
private int _bufFilled;
private int _bufLen;

private decimal _center;
private bool _centerInitialized;

private decimal _tUp;
private decimal _tDown;
private bool _hasTUp;
private bool _hasTDown;
private int _trend = 1;
private decimal _lastClose;

/// <summary>
/// Pivot period for pivot point detection.
/// </summary>
public int PivotPeriod
{
get => _pivotPeriod.Value;
set => _pivotPeriod.Value = value;
}

/// <summary>
/// ATR factor used for band calculation.
/// </summary>
public decimal AtrFactor
{
get => _atrFactor.Value;
set => _atrFactor.Value = value;
}

/// <summary>
/// ATR period.
/// </summary>
public int AtrPeriod
{
get => _atrPeriod.Value;
set => _atrPeriod.Value = value;
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
/// Initialize <see cref="PivotPointSupertrendStrategy"/>.
/// </summary>
public PivotPointSupertrendStrategy()
{
_pivotPeriod = Param(nameof(PivotPeriod), 2)
.SetDisplay("Pivot Period", "Pivot point period", "Indicators")
.SetCanOptimize(true)
.SetOptimize(1, 5, 1);

_atrFactor = Param(nameof(AtrFactor), 3m)
.SetDisplay("ATR Factor", "Multiplier for ATR bands", "Indicators")
.SetCanOptimize(true)
.SetOptimize(1m, 5m, 0.5m);

_atrPeriod = Param(nameof(AtrPeriod), 10)
.SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
.SetCanOptimize(true)
.SetOptimize(5, 20, 1);

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
.SetDisplay("Candle Type", "Type of candles to use", "General");
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

_atr = null;
_highBuffer = null;
_lowBuffer = null;
_bufFilled = 0;
_bufLen = 0;
_center = 0m;
_centerInitialized = false;
_tUp = 0m;
_tDown = 0m;
_hasTUp = false;
_hasTDown = false;
_trend = 1;
_lastClose = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_bufLen = PivotPeriod * 2 + 1;
_highBuffer = new decimal[_bufLen];
_lowBuffer = new decimal[_bufLen];
_bufFilled = 0;

_atr = new AverageTrueRange { Length = AtrPeriod };

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(_atr, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawOwnTrades(area);
}
}

private decimal? UpdatePivot(decimal high, decimal low)
{
for (var i = 0; i < _bufLen - 1; i++)
{
_highBuffer[i] = _highBuffer[i + 1];
_lowBuffer[i] = _lowBuffer[i + 1];
}

_highBuffer[_bufLen - 1] = high;
_lowBuffer[_bufLen - 1] = low;

if (_bufFilled < _bufLen)
{
_bufFilled++;
return null;
}

var pivotIndex = PivotPeriod;
var candidateHigh = _highBuffer[pivotIndex];
var isHigh = true;

for (var i = 0; i < _bufLen; i++)
{
if (i == pivotIndex)
continue;

if (_highBuffer[i] >= candidateHigh)
{
isHigh = false;
break;
}
}

if (isHigh)
return candidateHigh;

var candidateLow = _lowBuffer[pivotIndex];
var isLow = true;

for (var i = 0; i < _bufLen; i++)
{
if (i == pivotIndex)
continue;

if (_lowBuffer[i] <= candidateLow)
{
isLow = false;
break;
}
}

if (isLow)
return candidateLow;

return null;
}

private void ProcessCandle(ICandleMessage candle, decimal atrValue)
{
var pivot = UpdatePivot(candle.HighPrice, candle.LowPrice);

if (pivot is decimal pp)
{
if (!_centerInitialized)
{
_center = pp;
_centerInitialized = true;
}
else
_center = (_center * 2m + pp) / 3m;
}

if (candle.State != CandleStates.Finished)
{
_lastClose = candle.ClosePrice;
return;
}

if (!IsFormedAndOnlineAndAllowTrading())
{
_lastClose = candle.ClosePrice;
return;
}

if (!_centerInitialized)
{
_lastClose = candle.ClosePrice;
return;
}

var up = _center - AtrFactor * atrValue;
var dn = _center + AtrFactor * atrValue;

var prevTUp = _tUp;
var prevTDown = _tDown;
var prevTrend = _trend;

if (_hasTUp && _lastClose > prevTUp)
_tUp = Math.Max(up, prevTUp);
else
_tUp = up;
_hasTUp = true;

if (_hasTDown && _lastClose < prevTDown)
_tDown = Math.Min(dn, prevTDown);
else
_tDown = dn;
_hasTDown = true;

if (_hasTUp && _hasTDown)
{
_trend = _lastClose > prevTDown ? 1 : _lastClose < prevTUp ? -1 : prevTrend;

var bsignal = _trend == 1 && prevTrend == -1;
var ssignal = _trend == -1 && prevTrend == 1;

if (bsignal && Position <= 0)
BuyMarket(Volume + Math.Abs(Position));
else if (ssignal && Position >= 0)
SellMarket(Volume + Math.Abs(Position));
}

_lastClose = candle.ClosePrice;
}
}

