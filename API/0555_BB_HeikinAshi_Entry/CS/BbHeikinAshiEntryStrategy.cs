using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands + Heikin Ashi entry strategy with partial profit and trailing stop.
/// </summary>
public class BbHeikinAshiEntryStrategy : Strategy
{
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<int> _bollingerLength;
private readonly StrategyParam<decimal> _bollingerWidth;

private BollingerBands _bollinger;

private decimal _haOpenPrev1, _haOpenPrev2, _haOpenPrev3;
private decimal _haClosePrev1, _haClosePrev2, _haClosePrev3;
private decimal _haHighPrev1, _haHighPrev2, _haHighPrev3;
private decimal _haLowPrev1, _haLowPrev2, _haLowPrev3;
private decimal _upperBbPrev1, _upperBbPrev2, _upperBbPrev3;
private decimal _lowerBbPrev1, _lowerBbPrev2, _lowerBbPrev3;
private decimal _prevRawLow;
private decimal _prevRawHigh;

private decimal _entryPrice;
private decimal _initialStop;
private decimal _firstTarget;
private bool _firstTargetReached;
private decimal? _trailStop;

/// <summary>
/// Initialize BB Heikin Ashi Entry strategy.
/// </summary>
public BbHeikinAshiEntryStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
.SetDisplay("Candle Type", "Type of candles to use", "General");

_bollingerLength = Param(nameof(BollingerLength), 20)
.SetDisplay("Bollinger Length", "Period of Bollinger Bands", "Bollinger")
.SetCanOptimize(true)
.SetOptimize(10, 40, 5);

_bollingerWidth = Param(nameof(BollingerWidth), 2m)
.SetDisplay("Bollinger Width", "Standard deviation multiplier", "Bollinger")
.SetCanOptimize(true)
.SetOptimize(1m, 3m, 0.5m);
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
/// Bollinger period.
/// </summary>
public int BollingerLength
{
get => _bollingerLength.Value;
set => _bollingerLength.Value = value;
}

/// <summary>
/// Bollinger width (standard deviation).
/// </summary>
public decimal BollingerWidth
{
get => _bollingerWidth.Value;
set => _bollingerWidth.Value = value;
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
=> [(Security, CandleType)];

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();

_haOpenPrev1 = _haOpenPrev2 = _haOpenPrev3 = default;
_haClosePrev1 = _haClosePrev2 = _haClosePrev3 = default;
_haHighPrev1 = _haHighPrev2 = _haHighPrev3 = default;
_haLowPrev1 = _haLowPrev2 = _haLowPrev3 = default;
_upperBbPrev1 = _upperBbPrev2 = _upperBbPrev3 = default;
_lowerBbPrev1 = _lowerBbPrev2 = _lowerBbPrev3 = default;
_prevRawLow = _prevRawHigh = default;
_entryPrice = _initialStop = _firstTarget = default;
_firstTargetReached = default;
_trailStop = null;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_bollinger = new BollingerBands
{
Length = BollingerLength,
Width = BollingerWidth
};

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _bollinger);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
var haOpen = _haOpenPrev1 == 0
? (candle.OpenPrice + candle.ClosePrice) / 2m
: (_haOpenPrev1 + _haClosePrev1) / 2m;
var haHigh = Math.Max(Math.Max(candle.HighPrice, haOpen), haClose);
var haLow = Math.Min(Math.Min(candle.LowPrice, haOpen), haClose);

var bbRaw = (BollingerBandsValue)_bollinger.Process(new DecimalIndicatorValue(_bollinger, haClose));
decimal upper, lower;
if (bbRaw.UpBand is decimal u && bbRaw.LowBand is decimal l)
{
upper = u;
lower = l;
}
else
{
Shift(haOpen, haClose, haHigh, haLow, 0m, 0m, candle);
return;
}

if (_haOpenPrev3 != 0 && IsFormedAndOnlineAndAllowTrading())
{
var redCandle1 = _haClosePrev1 < _haOpenPrev1 && (_haLowPrev1 <= _lowerBbPrev1 || _haClosePrev1 <= _lowerBbPrev1);
var redCandle2 = _haClosePrev2 < _haOpenPrev2 && (_haLowPrev2 <= _lowerBbPrev2 || _haClosePrev2 <= _lowerBbPrev2);
var redCandle3 = _haClosePrev3 < _haOpenPrev3 && (_haLowPrev3 <= _lowerBbPrev3 || _haClosePrev3 <= _lowerBbPrev3);
var consecutiveBears = (redCandle1 && redCandle2) || (redCandle1 && redCandle2 && redCandle3);
var greenConfirmation = haClose > haOpen;
var aboveBb = haClose > lower;
var buySignal = consecutiveBears && greenConfirmation && aboveBb;

var greenCandle1 = _haClosePrev1 > _haOpenPrev1 && (_haHighPrev1 >= _upperBbPrev1 || _haClosePrev1 >= _upperBbPrev1);
var greenCandle2 = _haClosePrev2 > _haOpenPrev2 && (_haHighPrev2 >= _upperBbPrev2 || _haClosePrev2 >= _upperBbPrev2);
var greenCandle3 = _haClosePrev3 > _haOpenPrev3 && (_haHighPrev3 >= _upperBbPrev3 || _haClosePrev3 >= _upperBbPrev3);
var consecutiveBulls = (greenCandle1 && greenCandle2) || (greenCandle1 && greenCandle2 && greenCandle3);
var redConfirmation = haClose < haOpen;
var belowBb = haClose < upper;
var sellSignal = consecutiveBulls && redConfirmation && belowBb;

if (buySignal && Position <= 0)
{
_entryPrice = candle.ClosePrice;
_initialStop = _prevRawLow;
_firstTarget = _entryPrice + (_entryPrice - _initialStop);
_firstTargetReached = false;
_trailStop = null;
var volume = Volume + Math.Abs(Position);
BuyMarket(volume);
}
else if (sellSignal && Position >= 0)
{
_entryPrice = candle.ClosePrice;
_initialStop = _prevRawHigh;
_firstTarget = _entryPrice - (_prevRawHigh - _entryPrice);
_firstTargetReached = false;
_trailStop = null;
var volume = Volume + Math.Abs(Position);
SellMarket(volume);
}

if (Position > 0)
{
if (!_firstTargetReached)
{
if (candle.HighPrice >= _firstTarget)
{
SellMarket(Position / 2m);
_firstTargetReached = true;
_trailStop = _entryPrice;
}
}
else
{
_trailStop = Math.Max(_trailStop ?? _entryPrice, _prevRawLow);
}

var currentStop = _firstTargetReached ? _trailStop ?? _initialStop : _initialStop;
if (candle.LowPrice <= currentStop)
SellMarket(Position);
}
else if (Position < 0)
{
if (!_firstTargetReached)
{
if (candle.LowPrice <= _firstTarget)
{
BuyMarket(Math.Abs(Position) / 2m);
_firstTargetReached = true;
_trailStop = _entryPrice;
}
}
else
{
_trailStop = Math.Min(_trailStop ?? _entryPrice, _prevRawHigh);
}

var currentStop = _firstTargetReached ? _trailStop ?? _initialStop : _initialStop;
if (candle.HighPrice >= currentStop)
BuyMarket(Math.Abs(Position));
}
}

Shift(haOpen, haClose, haHigh, haLow, upper, lower, candle);
}

private void Shift(decimal haOpen, decimal haClose, decimal haHigh, decimal haLow, decimal upper, decimal lower, ICandleMessage candle)
{
_haOpenPrev3 = _haOpenPrev2;
_haOpenPrev2 = _haOpenPrev1;
_haOpenPrev1 = haOpen;

_haClosePrev3 = _haClosePrev2;
_haClosePrev2 = _haClosePrev1;
_haClosePrev1 = haClose;

_haHighPrev3 = _haHighPrev2;
_haHighPrev2 = _haHighPrev1;
_haHighPrev1 = haHigh;

_haLowPrev3 = _haLowPrev2;
_haLowPrev2 = _haLowPrev1;
_haLowPrev1 = haLow;

_upperBbPrev3 = _upperBbPrev2;
_upperBbPrev2 = _upperBbPrev1;
_upperBbPrev1 = upper;

_lowerBbPrev3 = _lowerBbPrev2;
_lowerBbPrev2 = _lowerBbPrev1;
_lowerBbPrev1 = lower;

_prevRawLow = candle.LowPrice;
_prevRawHigh = candle.HighPrice;
}
}

