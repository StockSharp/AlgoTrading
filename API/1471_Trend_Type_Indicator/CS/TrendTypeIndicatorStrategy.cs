using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend Type Indicator strategy.
/// Detects uptrend, downtrend or sideways market using ATR and ADX.
/// Goes long on uptrend, short on downtrend and exits on sideways.
/// </summary>
public class TrendTypeIndicatorStrategy : Strategy
{
private readonly StrategyParam<bool> _useAtr;
private readonly StrategyParam<int> _atrLength;
private readonly StrategyParam<bool> _useEmaAtr;
private readonly StrategyParam<int> _atrMaLength;
private readonly StrategyParam<bool> _useAdx;
private readonly StrategyParam<int> _adxLength;
private readonly StrategyParam<decimal> _adxLimit;
private readonly StrategyParam<int> _smoothFactor;
private readonly StrategyParam<DataType> _candleType;

private IIndicator _atrMa = null!;
private SimpleMovingAverage _trendSma = null!;

/// <summary>
/// Use ATR condition.
/// </summary>
public bool UseAtr
{
get => _useAtr.Value;
set => _useAtr.Value = value;
}

/// <summary>
/// ATR period.
/// </summary>
public int AtrLength
{
get => _atrLength.Value;
set => _atrLength.Value = value;
}

/// <summary>
/// Use EMA for ATR moving average.
/// </summary>
public bool UseEmaAtr
{
get => _useEmaAtr.Value;
set => _useEmaAtr.Value = value;
}

/// <summary>
/// ATR MA length.
/// </summary>
public int AtrMaLength
{
get => _atrMaLength.Value;
set => _atrMaLength.Value = value;
}

/// <summary>
/// Use ADX condition.
/// </summary>
public bool UseAdx
{
get => _useAdx.Value;
set => _useAdx.Value = value;
}

/// <summary>
/// ADX period.
/// </summary>
public int AdxLength
{
get => _adxLength.Value;
set => _adxLength.Value = value;
}

/// <summary>
/// ADX limit for sideways market.
/// </summary>
public decimal AdxLimit
{
get => _adxLimit.Value;
set => _adxLimit.Value = value;
}

/// <summary>
/// Smoothing factor.
/// </summary>
public int SmoothFactor
{
get => _smoothFactor.Value;
set => _smoothFactor.Value = value;
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
/// Initializes a new instance of <see cref="TrendTypeIndicatorStrategy"/>.
/// </summary>
public TrendTypeIndicatorStrategy()
{
_useAtr = Param(nameof(UseAtr), true)
.SetDisplay("Use ATR", "Use ATR condition", "General");

_atrLength = Param(nameof(AtrLength), 14)
.SetDisplay("ATR Length", "ATR period", "General")
.SetCanOptimize(true);

_useEmaAtr = Param(nameof(UseEmaAtr), false)
.SetDisplay("Use EMA ATR", "Use EMA for ATR MA", "General");

_atrMaLength = Param(nameof(AtrMaLength), 20)
.SetDisplay("ATR MA Length", "ATR moving average length", "General")
.SetCanOptimize(true);

_useAdx = Param(nameof(UseAdx), true)
.SetDisplay("Use ADX", "Use ADX condition", "General");

_adxLength = Param(nameof(AdxLength), 14)
.SetDisplay("ADX Length", "ADX period", "General")
.SetCanOptimize(true);

_adxLimit = Param(nameof(AdxLimit), 25m)
.SetDisplay("ADX Limit", "Sideways ADX limit", "General")
.SetCanOptimize(true);

_smoothFactor = Param(nameof(SmoothFactor), 3)
.SetDisplay("Smooth", "Smoothing factor", "General")
.SetCanOptimize(true);

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
_atrMa = null!;
_trendSma = null!;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var atr = new AverageTrueRange { Length = AtrLength };
_atrMa = UseEmaAtr ? new ExponentialMovingAverage { Length = AtrMaLength } : new SimpleMovingAverage { Length = AtrMaLength };
var adx = new AverageDirectionalIndex { Length = AdxLength };
_trendSma = new SimpleMovingAverage { Length = SmoothFactor };

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx(atr, adx, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, atr);
DrawIndicator(area, adx);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue, IIndicatorValue adxValue)
{
if (candle.State != CandleStates.Finished)
return;

var atr = atrValue.ToDecimal();
var atrMaVal = _atrMa.Process(atr);

var adxTyped = (AverageDirectionalIndexValue)adxValue;
var plusDi = adxTyped.Dx.Plus;
var minusDi = adxTyped.Dx.Minus;
var adxMa = adxTyped.MovingAverage;

var sidewaysAtr = UseAtr && atrMaVal.IsFinal && atr <= atrMaVal.ToDecimal();
var sidewaysAdx = UseAdx && adxMa is decimal adx && adx <= AdxLimit;

var trendType = sidewaysAtr || sidewaysAdx ? 0m : plusDi > minusDi ? 2m : -2m;
var smoothVal = _trendSma.Process(trendType);
if (!smoothVal.IsFinal)
return;

var smoothType = Math.Round(smoothVal.ToDecimal() / 2m) * 2m;

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (smoothType == 2m && Position <= 0)
{
BuyMarket(Volume + Math.Abs(Position));
}
else if (smoothType == -2m && Position >= 0)
{
SellMarket(Volume + Math.Abs(Position));
}
else if (smoothType == 0m && Position != 0)
{
ClosePosition();
}
}
}
