using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

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


/// <summary>
/// Initialize BB Heikin Ashi Entry strategy.
/// </summary>
public BbHeikinAshiEntryStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
.SetDisplay("Candle Type", "Type of candles to use", "General");

_bollingerLength = Param(nameof(BollingerLength), 20)
.SetDisplay("Bollinger Length", "Period of Bollinger Bands", "Bollinger")

.SetOptimize(10, 40, 5);

_bollingerWidth = Param(nameof(BollingerWidth), 2m)
.SetDisplay("Bollinger Width", "Standard deviation multiplier", "Bollinger")

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
}

/// <inheritdoc />
protected override void OnStarted2(DateTime time)
{
base.OnStarted2(time);

_bollinger = new BollingerBands
{
Length = BollingerLength,
Width = BollingerWidth
};

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx(_bollinger, ProcessCandle)
.Start();

StartProtection(
	takeProfit: new Unit(2, UnitTypes.Percent),
	stopLoss: new Unit(1, UnitTypes.Percent)
);

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _bollinger);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbResult)
{
if (candle.State != CandleStates.Finished)
return;

var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
var haOpen = _haOpenPrev1 == 0
? (candle.OpenPrice + candle.ClosePrice) / 2m
: (_haOpenPrev1 + _haClosePrev1) / 2m;
var haHigh = Math.Max(Math.Max(candle.HighPrice, haOpen), haClose);
var haLow = Math.Min(Math.Min(candle.LowPrice, haOpen), haClose);

if (bbResult is not BollingerBandsValue bbRaw ||
	bbRaw.UpBand is not decimal upper ||
	bbRaw.LowBand is not decimal lower)
{
	Shift(haOpen, haClose, haHigh, haLow, 0m, 0m, candle);
	return;
}

if (_haOpenPrev1 != 0)
{
var redCandle1 = _haClosePrev1 < _haOpenPrev1 && _haLowPrev1 <= _lowerBbPrev1;
var redCandle2 = _haClosePrev2 < _haOpenPrev2 && _haLowPrev2 <= _lowerBbPrev2;
var greenConfirmation = haClose > haOpen;
var buySignal = (redCandle1 || redCandle2) && greenConfirmation;

var greenCandle1 = _haClosePrev1 > _haOpenPrev1 && _haHighPrev1 >= _upperBbPrev1;
var greenCandle2 = _haClosePrev2 > _haOpenPrev2 && _haHighPrev2 >= _upperBbPrev2;
var redConfirmation = haClose < haOpen;
var sellSignal = (greenCandle1 || greenCandle2) && redConfirmation;

if (buySignal && Position == 0)
{
BuyMarket();
}
else if (sellSignal && Position == 0)
{
SellMarket();
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

