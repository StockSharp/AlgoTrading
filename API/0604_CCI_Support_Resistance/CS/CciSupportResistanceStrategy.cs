namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that trades breakouts of CCI based support and resistance levels.
/// </summary>
public class CciSupportResistanceStrategy : Strategy
{
private enum TrendMode
{
Cross,
Slope
}

private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<int> _cciLength;
private readonly StrategyParam<int> _leftPivot;
private readonly StrategyParam<int> _rightPivot;
private readonly StrategyParam<decimal> _buffer;
private readonly StrategyParam<bool> _trendMatter;
private readonly StrategyParam<TrendMode> _trendType;
private readonly StrategyParam<int> _slowMaLength;
private readonly StrategyParam<int> _fastMaLength;
private readonly StrategyParam<int> _slopeLength;
private readonly StrategyParam<decimal> _ksl;
private readonly StrategyParam<decimal> _ktp;

private readonly Queue<decimal> _cciValues = new();
private readonly Queue<decimal> _slowValues = new();

private decimal _upperCci;
private decimal _lowerCci;
private decimal _longSl;
private decimal _longTp;
private decimal _shortSl;
private decimal _shortTp;
private int _trendState;

/// <summary>
/// Candle type.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// CCI calculation length.
/// </summary>
public int CciLength
{
get => _cciLength.Value;
set => _cciLength.Value = value;
}

/// <summary>
/// Bars to the left of pivot.
/// </summary>
public int LeftPivot
{
get => _leftPivot.Value;
set => _leftPivot.Value = value;
}

/// <summary>
/// Bars to the right of pivot.
/// </summary>
public int RightPivot
{
get => _rightPivot.Value;
set => _rightPivot.Value = value;
}

/// <summary>
/// Buffer added to pivots.
/// </summary>
public decimal Buffer
{
get => _buffer.Value;
set => _buffer.Value = value;
}

/// <summary>
/// Use trend filter.
/// </summary>
public bool TrendMatter
{
get => _trendMatter.Value;
set => _trendMatter.Value = value;
}

/// <summary>
/// Trend detection mode.
/// </summary>
public TrendMode TrendType
{
get => _trendType.Value;
set => _trendType.Value = value;
}

/// <summary>
/// Slow EMA length.
/// </summary>
public int SlowMaLength
{
get => _slowMaLength.Value;
set => _slowMaLength.Value = value;
}

/// <summary>
/// Fast EMA length.
/// </summary>
public int FastMaLength
{
get => _fastMaLength.Value;
set => _fastMaLength.Value = value;
}

/// <summary>
/// Bars for slope comparison.
/// </summary>
public int SlopeLength
{
get => _slopeLength.Value;
set => _slopeLength.Value = value;
}

/// <summary>
/// ATR multiplier for stop.
/// </summary>
public decimal Ksl
{
get => _ksl.Value;
set => _ksl.Value = value;
}

/// <summary>
/// ATR multiplier for target.
/// </summary>
public decimal Ktp
{
get => _ktp.Value;
set => _ktp.Value = value;
}

/// <summary>
/// Constructor.
/// </summary>
public CciSupportResistanceStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");

_cciLength = Param(nameof(CciLength), 50)
.SetGreaterThanZero()
.SetDisplay("CCI Length", "CCI calculation length", "Indicators")
.SetCanOptimize(true)
.SetOptimize(20, 100, 10);

_leftPivot = Param(nameof(LeftPivot), 50)
.SetGreaterThanZero()
.SetDisplay("Left Pivot", "Bars to the left", "Support")
.SetCanOptimize(true)
.SetOptimize(10, 60, 10);

_rightPivot = Param(nameof(RightPivot), 50)
.SetGreaterThanZero()
.SetDisplay("Right Pivot", "Bars to the right", "Support")
.SetCanOptimize(true)
.SetOptimize(10, 60, 10);

_buffer = Param(nameof(Buffer), 10m)
.SetRange(0m, 100m)
.SetDisplay("Buffer", "CCI buffer", "Support");

_trendMatter = Param(nameof(TrendMatter), true)
.SetDisplay("Trend Filter", "Use trend filter", "Trend");

_trendType = Param(nameof(TrendType), TrendMode.Cross)
.SetDisplay("Trend Type", "Cross or slope", "Trend");

_slowMaLength = Param(nameof(SlowMaLength), 100)
.SetGreaterThanZero()
.SetDisplay("Slow EMA", "Slow EMA length", "Trend");

_fastMaLength = Param(nameof(FastMaLength), 50)
.SetGreaterThanZero()
.SetDisplay("Fast EMA", "Fast EMA length", "Trend");

_slopeLength = Param(nameof(SlopeLength), 5)
.SetGreaterThanZero()
.SetDisplay("Slope Length", "Bars for slope", "Trend");

_ksl = Param(nameof(Ksl), 1.1m)
.SetDisplay("KSL", "ATR stop multiplier", "Risk");

_ktp = Param(nameof(Ktp), 2.2m)
.SetDisplay("KTP", "ATR target multiplier", "Risk");
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

_cciValues.Clear();
_slowValues.Clear();
_upperCci = _lowerCci = 0m;
_longSl = _shortSl = _longTp = _shortTp = 0m;
_trendState = 0;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var cci = new CommodityChannelIndex { Length = CciLength };
var sma = new SimpleMovingAverage { Length = CciLength };
var std = new StandardDeviation { Length = CciLength };
var emaFast = new ExponentialMovingAverage { Length = FastMaLength };
var emaSlow = new ExponentialMovingAverage { Length = SlowMaLength };
var lowest2 = new Lowest { Length = 2 };
var highest2 = new Highest { Length = 2 };
var atr = new AverageTrueRange { Length = 100 };

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(cci, sma, std, emaFast, emaSlow, lowest2, highest2, atr, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, cci);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle,
decimal cciValue,
decimal smaValue,
decimal stdValue,
decimal emaFastValue,
decimal emaSlowValue,
decimal lowestValue,
decimal highestValue,
decimal atrValue)
{
if (candle.State != CandleStates.Finished)
return;

_cciValues.Enqueue(cciValue);
var window = LeftPivot + RightPivot + 1;
if (_cciValues.Count < window)
return;

var arr = _cciValues.ToArray();
var idx = LeftPivot;
var center = arr[idx];

var isHigh = true;
var isLow = true;

for (var i = 0; i < arr.Length; i++)
{
if (i == idx)
continue;

if (arr[i] >= center)
isHigh = false;
if (arr[i] <= center)
isLow = false;
if (!isHigh && !isLow)
break;
}

if (isHigh)
_upperCci = center - Buffer;
if (isLow)
_lowerCci = center + Buffer;

_cciValues.Dequeue();

var factor = 0.015m * stdValue;
var resistance = _upperCci * factor + smaValue;
var support = _lowerCci * factor + smaValue;

_slowValues.Enqueue(emaSlowValue);
if (_slowValues.Count > SlopeLength)
_slowValues.Dequeue();

var trend = _trendState;
if (TrendType == TrendMode.Cross)
{
trend = emaFastValue > emaSlowValue ? 1 : emaFastValue < emaSlowValue ? -1 : _trendState;
}
else if (_slowValues.Count == SlopeLength)
{
var ago = _slowValues.Peek();
trend = emaSlowValue > ago ? 1 : emaSlowValue < ago ? -1 : _trendState;
}
_trendState = trend;

var bull = !TrendMatter || trend == 1;
var bear = !TrendMatter || trend == -1;

var price = candle.ClosePrice;

var buy = bull && lowestValue < support && price > candle.OpenPrice && price > support;
var sell = bear && highestValue > resistance && price < candle.OpenPrice && price < resistance;

if (buy && Position <= 0)
{
BuyMarket(Volume + Math.Abs(Position));
_longSl = price - atrValue * Ksl;
_longTp = price + atrValue * Ktp;
}
else if (sell && Position >= 0)
{
SellMarket(Volume + Math.Abs(Position));
_shortSl = price + atrValue * Ksl;
_shortTp = price - atrValue * Ktp;
}
else if (Position > 0)
{
_longSl = Math.Max(_longSl, price - atrValue * Ksl);
_longTp = Math.Max(_longTp, price + atrValue * Ktp);
if (candle.LowPrice <= _longSl || candle.HighPrice >= _longTp)
SellMarket(Position);
}
else if (Position < 0)
{
_shortSl = Math.Min(_shortSl, price + atrValue * Ksl);
_shortTp = Math.Min(_shortTp, price - atrValue * Ktp);
if (candle.HighPrice >= _shortSl || candle.LowPrice <= _shortTp)
BuyMarket(-Position);
}
}
}
