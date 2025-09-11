using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pivot Point SuperTrend with trend filter strategy.
/// Combines a pivot-based SuperTrend line with a SuperTrend trend filter
/// and moving average confirmation.
/// </summary>
public class PivotPointSuperTrendTrendFilterStrategy : Strategy
{
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<int> _pivotPeriod;
private readonly StrategyParam<decimal> _factor;
private readonly StrategyParam<int> _atrPeriod;
private readonly StrategyParam<int> _trendAtrPeriod;
private readonly StrategyParam<decimal> _trendMultiplier;
private readonly StrategyParam<int> _maPeriod;
private readonly StrategyParam<DateTimeOffset> _startDate;
private readonly StrategyParam<DateTimeOffset> _endDate;

private readonly List<decimal> _highBuffer = new();
private readonly List<decimal> _lowBuffer = new();

private AverageTrueRange _atr;
private SuperTrend _trend;
private SimpleMovingAverage _ma;

private decimal? _center;
private decimal _tUp;
private decimal _tDown;
private int _pivotTrend = 1;
private decimal _prevClose;
private int _prevTrendDir = 1;

/// <summary>
/// Candle type for calculations.
/// </summary>
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

/// <summary>
/// Pivot lookback period.
/// </summary>
public int PivotPeriod { get => _pivotPeriod.Value; set => _pivotPeriod.Value = value; }

/// <summary>
/// ATR multiplier for pivot SuperTrend.
/// </summary>
public decimal Factor { get => _factor.Value; set => _factor.Value = value; }

/// <summary>
/// ATR period for pivot SuperTrend.
/// </summary>
public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

/// <summary>
/// ATR period for trend filter.
/// </summary>
public int TrendAtrPeriod { get => _trendAtrPeriod.Value; set => _trendAtrPeriod.Value = value; }

/// <summary>
/// ATR multiplier for trend filter.
/// </summary>
public decimal TrendMultiplier { get => _trendMultiplier.Value; set => _trendMultiplier.Value = value; }

/// <summary>
/// Moving average period.
/// </summary>
public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }

/// <summary>
/// Trading start date.
/// </summary>
public DateTimeOffset StartDate { get => _startDate.Value; set => _startDate.Value = value; }

/// <summary>
/// Trading end date.
/// </summary>
public DateTimeOffset EndDate { get => _endDate.Value; set => _endDate.Value = value; }

/// <summary>
/// Constructor.
/// </summary>
public PivotPointSuperTrendTrendFilterStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");

_pivotPeriod = Param(nameof(PivotPeriod), 2)
.SetGreaterThanZero()
.SetDisplay("Pivot Period", "Lookback for pivot detection", "Pivot")
.SetCanOptimize(true)
.SetOptimize(1, 10, 1);

_factor = Param(nameof(Factor), 3m)
.SetGreaterThanZero()
.SetDisplay("ATR Factor", "ATR multiplier", "Pivot")
.SetCanOptimize(true)
.SetOptimize(1m, 5m, 0.5m);

_atrPeriod = Param(nameof(AtrPeriod), 10)
.SetGreaterThanZero()
.SetDisplay("ATR Period", "ATR length", "Pivot")
.SetCanOptimize(true)
.SetOptimize(5, 20, 1);

_trendAtrPeriod = Param(nameof(TrendAtrPeriod), 10)
.SetGreaterThanZero()
.SetDisplay("Trend ATR Period", "ATR period for trend filter", "Trend Filter")
.SetCanOptimize(true)
.SetOptimize(5, 20, 1);

_trendMultiplier = Param(nameof(TrendMultiplier), 3m)
.SetGreaterThanZero()
.SetDisplay("Trend Multiplier", "ATR multiplier for trend filter", "Trend Filter")
.SetCanOptimize(true)
.SetOptimize(1m, 5m, 0.5m);

_maPeriod = Param(nameof(MaPeriod), 20)
.SetGreaterThanZero()
.SetDisplay("MA Period", "Moving average period", "Trend Filter")
.SetCanOptimize(true)
.SetOptimize(10, 50, 5);

_startDate = Param(nameof(StartDate), new DateTimeOffset(2018, 9, 1, 0, 0, 0, TimeSpan.Zero))
.SetDisplay("Start Date", "Trading start date", "General");

_endDate = Param(nameof(EndDate), new DateTimeOffset(9999, 1, 1, 23, 59, 0, TimeSpan.Zero))
.SetDisplay("End Date", "Trading end date", "General");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
=> [(Security, CandleType)];

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();
_highBuffer.Clear();
_lowBuffer.Clear();
_center = null;
_tUp = 0m;
_tDown = 0m;
_pivotTrend = 1;
_prevClose = 0m;
_prevTrendDir = 1;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_atr = new AverageTrueRange { Length = AtrPeriod };
_trend = new SuperTrend { Length = TrendAtrPeriod, Multiplier = TrendMultiplier };
_ma = new SimpleMovingAverage { Length = MaPeriod };

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx(_atr, _trend, _ma, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _trend);
DrawIndicator(area, _ma);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue, IIndicatorValue trendValue, IIndicatorValue maValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var atr = atrValue.ToDecimal();
var st = (SuperTrendIndicatorValue)trendValue;
var ma = maValue.ToDecimal();

var time = candle.OpenTime;
var inWindow = time >= StartDate && time <= EndDate;

var lastpp = UpdatePivot(candle);
if (lastpp is decimal pp)
_center = _center is null ? pp : (_center * 2m + pp) / 3m;

if (_center is null)
{
_prevClose = candle.ClosePrice;
_prevTrendDir = st.IsUpTrend ? 1 : -1;
return;
}

var up = _center.Value - Factor * atr;
var dn = _center.Value + Factor * atr;

var oldTUp = _tUp;
var oldTDown = _tDown;

var newTUp = _prevClose > oldTUp ? Math.Max(up, oldTUp) : up;
var newTDown = _prevClose < oldTDown ? Math.Min(dn, oldTDown) : dn;

var newTrend = candle.ClosePrice > oldTDown ? 1 : candle.ClosePrice < oldTUp ? -1 : _pivotTrend;
var bsignal = newTrend == 1 && _pivotTrend == -1;
var ssignal = newTrend == -1 && _pivotTrend == 1;

var trendDir = st.IsUpTrend ? 1 : -1;
var trendUp = trendDir == 1 && _prevTrendDir == -1;
var trendDown = trendDir == -1 && _prevTrendDir == 1;

var longCondition = (trendUp && candle.ClosePrice > ma) || (bsignal && inWindow);
var shortCondition = (trendDown && candle.ClosePrice < ma) || (ssignal && inWindow);

if (longCondition && Position <= 0)
BuyMarket(Volume + Math.Abs(Position));
else if (shortCondition && Position >= 0)
SellMarket(Volume + Math.Abs(Position));

_tUp = newTUp;
_tDown = newTDown;
_pivotTrend = newTrend;
_prevClose = candle.ClosePrice;
_prevTrendDir = trendDir;
}

private decimal? UpdatePivot(ICandleMessage candle)
{
var size = PivotPeriod * 2 + 1;

_highBuffer.Add(candle.HighPrice);
_lowBuffer.Add(candle.LowPrice);

if (_highBuffer.Count > size)
_highBuffer.RemoveAt(0);
if (_lowBuffer.Count > size)
_lowBuffer.RemoveAt(0);

if (_highBuffer.Count == size)
{
var center = PivotPeriod;
var candidate = _highBuffer[center];
var isPivot = true;
for (var i = 0; i < size; i++)
{
if (i == center)
continue;
if (_highBuffer[i] >= candidate)
{
isPivot = false;
break;
}
}
if (isPivot)
return candidate;
}

if (_lowBuffer.Count == size)
{
var center = PivotPeriod;
var candidate = _lowBuffer[center];
var isPivot = true;
for (var i = 0; i < size; i++)
{
if (i == center)
continue;
if (_lowBuffer[i] <= candidate)
{
isPivot = false;
break;
}
}
if (isPivot)
return candidate;
}

return null;
}
}
