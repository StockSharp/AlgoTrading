using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy inspired by "Volume ValueWhen Velocity".
/// It looks for oversold RSI, low volatility ATR condition and compares previous
/// SMA breakout prices to measure distance. When all conditions are met a long trade is opened.
/// </summary>
public class VolumeValueWhenVelocityStrategy : Strategy
{
private readonly StrategyParam<int> _rsiLength;
private readonly StrategyParam<int> _rsiOversold;
private readonly StrategyParam<int> _atrSmall;
private readonly StrategyParam<int> _atrBig;
private readonly StrategyParam<int> _distance;
private readonly StrategyParam<DataType> _candleType;

private decimal _prevVolume1;
private decimal _prevVolume2;
private decimal _lastCross;
private decimal _prevCross;
private int _barsSinceCross;

/// <summary>
/// RSI length.
/// </summary>
public int RsiLength
{
get => _rsiLength.Value;
set => _rsiLength.Value = value;
}

/// <summary>
/// Oversold level.
/// </summary>
public int RsiOversold
{
get => _rsiOversold.Value;
set => _rsiOversold.Value = value;
}

/// <summary>
/// Short ATR period.
/// </summary>
public int AtrSmall
{
get => _atrSmall.Value;
set => _atrSmall.Value = value;
}

/// <summary>
/// Long ATR period.
/// </summary>
public int AtrBig
{
get => _atrBig.Value;
set => _atrBig.Value = value;
}

/// <summary>
/// Minimum distance between SMA breakout prices.
/// </summary>
public int Distance
{
get => _distance.Value;
set => _distance.Value = value;
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
/// Initializes <see cref="VolumeValueWhenVelocityStrategy"/>.
/// </summary>
public VolumeValueWhenVelocityStrategy()
{
_rsiLength = Param(nameof(RsiLength), 40)
.SetDisplay("RSI Length", "RSI period", "Indicators");

_rsiOversold = Param(nameof(RsiOversold), 60)
.SetDisplay("RSI Oversold", "Oversold level", "Indicators");

_atrSmall = Param(nameof(AtrSmall), 5)
.SetDisplay("ATR Small", "Short ATR period", "Indicators");

_atrBig = Param(nameof(AtrBig), 14)
.SetDisplay("ATR Big", "Long ATR period", "Indicators");

_distance = Param(nameof(Distance), 170)
.SetDisplay("Distance", "Minimum distance between breakouts", "Strategy");

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
_prevVolume1 = 0;
_prevVolume2 = 0;
_lastCross = 0;
_prevCross = 0;
_barsSinceCross = int.MaxValue;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var rsi = new RelativeStrengthIndex { Length = RsiLength };
var atrShort = new AverageTrueRange { Length = AtrSmall };
var atrLong = new AverageTrueRange { Length = AtrBig };
var sma = new SimpleMovingAverage { Length = 13 };

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(rsi, atrShort, atrLong, sma, ProcessCandle)
.Start();

StartProtection(
takeProfit: new Unit(2, UnitTypes.Percent),
stopLoss: new Unit(3, UnitTypes.Percent)
);

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, sma);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal atrShortValue, decimal atrLongValue, decimal smaValue)
{
if (candle.State != CandleStates.Finished)
return;

// track volumes for simple comparison
if (_prevVolume1 == 0)
{
_prevVolume1 = candle.TotalVolume;
return;
}

// update bars since last SMA breakout
if (candle.ClosePrice > smaValue)
{
_barsSinceCross = 0;
_prevCross = _lastCross;
_lastCross = candle.ClosePrice;
}
else
{
_barsSinceCross++;
}

var prevCloseChange = _prevCross - _lastCross;
var wasOversold = rsiValue <= RsiOversold;
var atrCondition = atrShortValue < atrLongValue;
var volumeCondition = candle.TotalVolume > _prevVolume1 && _prevVolume1 > _prevVolume2;

_prevVolume2 = _prevVolume1;
_prevVolume1 = candle.TotalVolume;

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (volumeCondition && atrCondition && wasOversold && prevCloseChange > Distance && _barsSinceCross < 5 && Position <= 0)
{
BuyMarket(Volume + Math.Abs(Position));
}
}
}
