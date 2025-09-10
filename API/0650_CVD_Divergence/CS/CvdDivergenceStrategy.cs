using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on CVD divergence with HMA trend, RSI, MACD and volume filter.
/// </summary>
public class CvdDivergenceStrategy : Strategy
{
private readonly StrategyParam<int> _hmaFastLength;
private readonly StrategyParam<int> _hmaSlowLength;
private readonly StrategyParam<int> _rsiLength;
private readonly StrategyParam<int> _rsiOverbought;
private readonly StrategyParam<int> _rsiOversold;
private readonly StrategyParam<int> _macdFast;
private readonly StrategyParam<int> _macdSlow;
private readonly StrategyParam<int> _macdSignal;
private readonly StrategyParam<int> _volumeMaLength;
private readonly StrategyParam<decimal> _volumeMultiplier;
private readonly StrategyParam<int> _cvdLength;
private readonly StrategyParam<int> _divergenceLookback;
private readonly StrategyParam<DataType> _candleType;

private HullMovingAverage _hmaFast;
private HullMovingAverage _hmaSlow;
private RelativeStrengthIndex _rsi;
private MovingAverageConvergenceDivergence _macd;
private SimpleMovingAverage _volumeMa;
private SimpleMovingAverage _cvdMa;
private Highest _priceHighest;
private Lowest _priceLowest;

private decimal _prevMacdHist;
private decimal _prevCvd;
private decimal _lastPriceHigh;
private decimal _lastPriceLow;
private decimal _lastCvdHigh;
private decimal _lastCvdLow;

public int HmaFastLength { get => _hmaFastLength.Value; set => _hmaFastLength.Value = value; }
public int HmaSlowLength { get => _hmaSlowLength.Value; set => _hmaSlowLength.Value = value; }
public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
public int RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
public int RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }
public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }
public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }
public int VolumeMaLength { get => _volumeMaLength.Value; set => _volumeMaLength.Value = value; }
public decimal VolumeMultiplier { get => _volumeMultiplier.Value; set => _volumeMultiplier.Value = value; }
public int CvdLength { get => _cvdLength.Value; set => _cvdLength.Value = value; }
public int DivergenceLookback { get => _divergenceLookback.Value; set => _divergenceLookback.Value = value; }
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

public CvdDivergenceStrategy()
{
_hmaFastLength = Param(nameof(HmaFastLength), 20)
.SetGreaterThanZero()
.SetDisplay("HMA Fast Length", "Length of fast HMA", "General");

_hmaSlowLength = Param(nameof(HmaSlowLength), 50)
.SetGreaterThanZero()
.SetDisplay("HMA Slow Length", "Length of slow HMA", "General");

_rsiLength = Param(nameof(RsiLength), 14)
.SetGreaterThanZero()
.SetDisplay("RSI Length", "RSI calculation length", "General");

_rsiOverbought = Param(nameof(RsiOverbought), 70)
.SetDisplay("RSI Overbought", "Overbought RSI level", "General");

_rsiOversold = Param(nameof(RsiOversold), 30)
.SetDisplay("RSI Oversold", "Oversold RSI level", "General");

_macdFast = Param(nameof(MacdFast), 12)
.SetGreaterThanZero()
.SetDisplay("MACD Fast", "MACD fast period", "General");

_macdSlow = Param(nameof(MacdSlow), 26)
.SetGreaterThanZero()
.SetDisplay("MACD Slow", "MACD slow period", "General");

_macdSignal = Param(nameof(MacdSignal), 9)
.SetGreaterThanZero()
.SetDisplay("MACD Signal", "MACD signal period", "General");

_volumeMaLength = Param(nameof(VolumeMaLength), 20)
.SetGreaterThanZero()
.SetDisplay("Volume MA Length", "Length of volume moving average", "General");

_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.5m)
.SetGreaterThanZero()
.SetDisplay("Volume Multiplier", "Multiplier for high volume filter", "General");

_cvdLength = Param(nameof(CvdLength), 14)
.SetGreaterThanZero()
.SetDisplay("CVD Length", "Length of CVD smoothing", "General");

_divergenceLookback = Param(nameof(DivergenceLookback), 5)
.SetGreaterThanZero()
.SetDisplay("Divergence Lookback", "Lookback for divergence detection", "General");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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

_prevMacdHist = 0m;
_prevCvd = 0m;
_lastPriceHigh = 0m;
_lastPriceLow = 0m;
_lastCvdHigh = 0m;
_lastCvdLow = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_hmaFast = new HullMovingAverage { Length = HmaFastLength };
_hmaSlow = new HullMovingAverage { Length = HmaSlowLength };
_rsi = new RelativeStrengthIndex { Length = RsiLength };
_macd = new MovingAverageConvergenceDivergence
{
ShortPeriod = MacdFast,
LongPeriod = MacdSlow,
SignalPeriod = MacdSignal
};
_volumeMa = new SimpleMovingAverage { Length = VolumeMaLength };
_cvdMa = new SimpleMovingAverage { Length = CvdLength };
_priceHighest = new Highest { Length = DivergenceLookback };
_priceLowest = new Lowest { Length = DivergenceLookback };

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(_hmaFast, _hmaSlow, _rsi, _macd, ProcessCandle)
.Start();

StartProtection();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _hmaFast);
DrawIndicator(area, _hmaSlow);
DrawIndicator(area, _rsi);
DrawIndicator(area, _macd);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal hmaFast, decimal hmaSlow, decimal rsi, decimal macdLine, decimal macdSignal, decimal macdHist)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var volMaValue = _volumeMa.Process(candle.TotalVolume);
if (!volMaValue.IsFinal)
return;
var volumeMa = volMaValue.ToDecimal();
var highVolume = candle.TotalVolume > volumeMa * VolumeMultiplier;

var sign = candle.ClosePrice > candle.OpenPrice ? 1m : candle.ClosePrice < candle.OpenPrice ? -1m : 0m;
var cvd = _cvdMa.Process(candle.TotalVolume * sign).ToDecimal();

var priceHigh = _priceHighest.Process(candle.HighPrice).ToDecimal();
var priceLow = _priceLowest.Process(candle.LowPrice).ToDecimal();

var bullishDiv = priceLow < _lastPriceLow && cvd > _lastCvdLow;
var bearishDiv = priceHigh > _lastPriceHigh && cvd < _lastCvdHigh;

if (priceLow < _lastPriceLow)
{
_lastPriceLow = priceLow;
_lastCvdLow = cvd;
}

if (priceHigh > _lastPriceHigh)
{
_lastPriceHigh = priceHigh;
_lastCvdHigh = cvd;
}

var hmaBullish = hmaFast > hmaSlow && candle.ClosePrice > hmaFast;
var rsiBullish = rsi < RsiOverbought && rsi > 40m;
var macdBullish = macdLine > macdSignal && macdHist > _prevMacdHist;
var cvdBullish = bullishDiv || cvd > _prevCvd;
var longCond = hmaBullish && rsiBullish && macdBullish && highVolume && cvdBullish;

var hmaBearish = hmaFast < hmaSlow && candle.ClosePrice < hmaFast;
var rsiBearish = rsi > RsiOversold && rsi < 60m;
var macdBearish = macdLine < macdSignal && macdHist < _prevMacdHist;
var cvdBearish = bearishDiv || cvd < _prevCvd;
var shortCond = hmaBearish && rsiBearish && macdBearish && highVolume && cvdBearish;

var longExit = candle.ClosePrice < hmaFast || rsi > RsiOverbought || macdLine < macdSignal;
var shortExit = candle.ClosePrice > hmaFast || rsi < RsiOversold || macdLine > macdSignal;

if (longCond && Position <= 0)
{
if (Position < 0)
BuyMarket(Math.Abs(Position));
BuyMarket(Volume + Math.Abs(Position));
}
else if (shortCond && Position >= 0)
{
if (Position > 0)
SellMarket(Position);
SellMarket(Volume + Math.Abs(Position));
}
else if (Position > 0 && longExit)
{
SellMarket(Position);
}
else if (Position < 0 && shortExit)
{
BuyMarket(Math.Abs(Position));
}

_prevMacdHist = macdHist;
_prevCvd = cvd;
}
}
