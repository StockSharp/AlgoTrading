using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands breakout strategy with ADX and Supertrend filter,
/// MACD exits, volume spike condition and ATR-based trailing stop.
/// </summary>
public class NiftyOptionsTrendyMarketsWithTslStrategy : Strategy
{
private readonly StrategyParam<int> _bollingerPeriod;
private readonly StrategyParam<decimal> _bollingerMultiplier;
private readonly StrategyParam<int> _adxLength;
private readonly StrategyParam<decimal> _adxEntryThreshold;
private readonly StrategyParam<decimal> _adxExitThreshold;
private readonly StrategyParam<int> _superTrendLength;
private readonly StrategyParam<decimal> _superTrendMultiplier;
private readonly StrategyParam<int> _macdFast;
private readonly StrategyParam<int> _macdSlow;
private readonly StrategyParam<int> _macdSignal;
private readonly StrategyParam<int> _atrLength;
private readonly StrategyParam<decimal> _atrMultiplier;
private readonly StrategyParam<decimal> _volumeSpikeMultiplier;
private readonly StrategyParam<DataType> _candleType;

private readonly SimpleMovingAverage _volumeSma = new() { Length = 20 };

private decimal _prevClose;
private decimal _prevUpper;
private decimal _prevLower;
private decimal _prevMacd;
private decimal _prevSignal;
private decimal? _trailStop;
private bool _initialized;

public NiftyOptionsTrendyMarketsWithTslStrategy()
{
_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
.SetDisplay("Bollinger Period", "Bollinger Bands period", "General");
_bollingerMultiplier = Param(nameof(BollingerMultiplier), 2m)
.SetDisplay("Bollinger Mult", "Bollinger Bands multiplier", "General");
_adxLength = Param(nameof(AdxLength), 14)
.SetDisplay("ADX Length", "ADX calculation length", "General");
_adxEntryThreshold = Param(nameof(AdxEntryThreshold), 25m)
.SetDisplay("ADX Entry", "ADX threshold to enter", "General");
_adxExitThreshold = Param(nameof(AdxExitThreshold), 20m)
.SetDisplay("ADX Exit", "ADX threshold to exit", "General");
_superTrendLength = Param(nameof(SuperTrendLength), 10)
.SetDisplay("Supertrend Length", "Supertrend period", "General");
_superTrendMultiplier = Param(nameof(SuperTrendMultiplier), 3m)
.SetDisplay("Supertrend Mult", "Supertrend multiplier", "General");
_macdFast = Param(nameof(MacdFast), 12)
.SetDisplay("MACD Fast", "MACD fast EMA length", "General");
_macdSlow = Param(nameof(MacdSlow), 26)
.SetDisplay("MACD Slow", "MACD slow EMA length", "General");
_macdSignal = Param(nameof(MacdSignal), 9)
.SetDisplay("MACD Signal", "MACD signal EMA length", "General");
_atrLength = Param(nameof(AtrLength), 14)
.SetDisplay("ATR Length", "ATR calculation period", "General");
_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
.SetDisplay("ATR Mult", "ATR multiplier for trailing stop", "General");
_volumeSpikeMultiplier = Param(nameof(VolumeSpikeMultiplier), 1.5m)
.SetDisplay("Volume Spike Mult", "Volume spike multiplier", "General");
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
.SetDisplay("Candle Type", "Time frame for candles", "General");
}

public int BollingerPeriod { get => _bollingerPeriod.Value; set => _bollingerPeriod.Value = value; }
public decimal BollingerMultiplier { get => _bollingerMultiplier.Value; set => _bollingerMultiplier.Value = value; }
public int AdxLength { get => _adxLength.Value; set => _adxLength.Value = value; }
public decimal AdxEntryThreshold { get => _adxEntryThreshold.Value; set => _adxEntryThreshold.Value = value; }
public decimal AdxExitThreshold { get => _adxExitThreshold.Value; set => _adxExitThreshold.Value = value; }
public int SuperTrendLength { get => _superTrendLength.Value; set => _superTrendLength.Value = value; }
public decimal SuperTrendMultiplier { get => _superTrendMultiplier.Value; set => _superTrendMultiplier.Value = value; }
public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }
public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }
public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }
public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
public decimal VolumeSpikeMultiplier { get => _volumeSpikeMultiplier.Value; set => _volumeSpikeMultiplier.Value = value; }
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

StartProtection();

var bb = new BollingerBands
{
Length = BollingerPeriod,
Width = BollingerMultiplier
};
var adx = new AverageDirectionalIndex { Length = AdxLength };
var supertrend = new SuperTrend { Period = SuperTrendLength, Multiplier = SuperTrendMultiplier };
var macd = new MovingAverageConvergenceDivergenceSignal
{
Macd =
{
ShortMa = { Length = MacdFast },
LongMa = { Length = MacdSlow },
},
SignalMa = { Length = MacdSignal }
};
var atr = new AverageTrueRange { Length = AtrLength };

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx(bb, adx, supertrend, macd, atr, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, bb);
DrawIndicator(area, adx);
DrawIndicator(area, supertrend);
DrawIndicator(area, macd);
DrawIndicator(area, atr);
DrawOwnTrades(area);
}
}

private void ProcessCandle(
ICandleMessage candle,
IIndicatorValue bbValue,
IIndicatorValue adxValue,
IIndicatorValue supertrendValue,
IIndicatorValue macdValue,
IIndicatorValue atrValue)
{
if (candle.State != CandleStates.Finished)
return;

var volumeAvg = _volumeSma.Process(candle.TotalVolume, candle.OpenTime, true).ToDecimal();
if (!_volumeSma.IsFormed || !IsFormedAndOnlineAndAllowTrading())
return;

if (bbValue is not BollingerBandsValue bb ||
adxValue is not AverageDirectionalIndexValue adx ||
supertrendValue is not SuperTrendIndicatorValue st ||
macdValue is not MovingAverageConvergenceDivergenceSignalValue macd ||
atrValue is not DecimalIndicatorValue atrVal ||
bb.UpBand is not decimal upper ||
bb.LowBand is not decimal lower ||
bb.MovingAverage is not decimal middle ||
macd.Macd is not decimal macdLine ||
macd.Signal is not decimal signalLine)
return;

if (!_initialized)
{
_prevClose = candle.ClosePrice;
_prevUpper = upper;
_prevLower = lower;
_prevMacd = macdLine;
_prevSignal = signalLine;
_initialized = true;
return;
}

var supertrendLine = st.Value;
var adxMa = adx.MovingAverage;
var atr = atrVal.Value;
var volumeSpike = candle.TotalVolume > volumeAvg * VolumeSpikeMultiplier;

if (Position > 0)
{
var stop = candle.ClosePrice - atr * AtrMultiplier;
_trailStop = _trailStop.HasValue ? Math.Max(_trailStop.Value, stop) : stop;
}
else if (Position < 0)
{
var stop = candle.ClosePrice + atr * AtrMultiplier;
_trailStop = _trailStop.HasValue ? Math.Min(_trailStop.Value, stop) : stop;
}

var macdCrossunder = _prevMacd >= _prevSignal && macdLine < signalLine;
var macdCrossover = _prevMacd <= _prevSignal && macdLine > signalLine;

var longExit = Position > 0 && (macdCrossunder || adxMa < AdxExitThreshold || (_trailStop.HasValue && candle.LowPrice <= _trailStop.Value));
var shortExit = Position < 0 && (macdCrossover || adxMa < AdxExitThreshold || (_trailStop.HasValue && candle.HighPrice >= _trailStop.Value));

if (longExit)
{
SellMarket(Math.Abs(Position));
_trailStop = null;
}
else if (shortExit)
{
BuyMarket(Math.Abs(Position));
_trailStop = null;
}
else
{
var longEntry = _prevClose <= _prevUpper &&
candle.ClosePrice > upper &&
adxMa > AdxEntryThreshold &&
volumeSpike &&
candle.ClosePrice > supertrendLine &&
Position <= 0;

var shortEntry = _prevClose >= _prevLower &&
candle.ClosePrice < lower &&
adxMa > AdxEntryThreshold &&
volumeSpike &&
candle.ClosePrice < supertrendLine &&
Position >= 0;

if (longEntry)
{
BuyMarket(Volume + Math.Abs(Position));
_trailStop = candle.ClosePrice - atr * AtrMultiplier;
}
else if (shortEntry)
{
SellMarket(Volume + Math.Abs(Position));
_trailStop = candle.ClosePrice + atr * AtrMultiplier;
}
}

_prevClose = candle.ClosePrice;
_prevUpper = upper;
_prevLower = lower;
_prevMacd = macdLine;
_prevSignal = signalLine;
}
}
