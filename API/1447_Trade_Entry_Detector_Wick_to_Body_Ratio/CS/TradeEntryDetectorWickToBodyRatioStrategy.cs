using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Detects entries when candle wicks touch Bollinger Bands.
/// Long entries occur on large lower wicks below the lower band.
/// Short entries occur on large upper wicks above the upper band.
/// Exits at the opposite band or when a swing level is broken.
/// </summary>
public class TradeEntryDetectorWickToBodyRatioStrategy : Strategy
{
private readonly StrategyParam<int> _bollingerPeriod;
private readonly StrategyParam<decimal> _bollingerWidth;
private readonly StrategyParam<decimal> _wickToBodyRatio;
private readonly StrategyParam<DataType> _candleType;

private decimal? _swingLow;
private decimal? _swingHigh;

/// <summary>
/// Bollinger Bands period.
/// </summary>
public int BollingerPeriod
{
get => _bollingerPeriod.Value;
set => _bollingerPeriod.Value = value;
}

/// <summary>
/// Standard deviation multiplier for Bollinger Bands.
/// </summary>
public decimal BollingerWidth
{
get => _bollingerWidth.Value;
set => _bollingerWidth.Value = value;
}

/// <summary>
/// Minimum wick to body ratio to trigger entries.
/// </summary>
public decimal WickToBodyRatio
{
get => _wickToBodyRatio.Value;
set => _wickToBodyRatio.Value = value;
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
/// Initializes a new instance of the strategy.
/// </summary>
public TradeEntryDetectorWickToBodyRatioStrategy()
{
_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
.SetGreaterThanZero()
.SetDisplay("Bollinger Period", "Period of Bollinger Bands", "Indicator");

_bollingerWidth = Param(nameof(BollingerWidth), 2m)
.SetGreaterThanZero()
.SetDisplay("Bollinger Width", "Standard deviation multiplier", "Indicator");

_wickToBodyRatio = Param(nameof(WickToBodyRatio), 1m)
.SetGreaterThanZero()
.SetDisplay("Wick/Body Ratio", "Minimum wick to body ratio", "Setup");

_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
.SetDisplay("Candle Type", "Candles used for processing", "General");
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
_swingLow = null;
_swingHigh = null;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var bands = new BollingerBands
{
Length = BollingerPeriod,
Width = BollingerWidth
};

SubscribeCandles(CandleType)
.Bind(bands, ProcessCandle)
.Start();
}

private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower)
{
if (candle.State != CandleStates.Finished)
return;

var body = Math.Abs(candle.OpenPrice - candle.ClosePrice);
if (body == 0)
return;

var upperWick = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
var lowerWick = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;

if (lowerWick / body >= WickToBodyRatio && candle.LowPrice < lower)
{
_swingLow = candle.LowPrice;
if (Position <= 0)
BuyMarket(Volume + Math.Abs(Position));
}
else if (upperWick / body >= WickToBodyRatio && candle.HighPrice > upper)
{
_swingHigh = candle.HighPrice;
if (Position >= 0)
SellMarket(Volume + Math.Abs(Position));
}

if (Position > 0)
{
if (candle.ClosePrice >= upper)
SellMarket(Position);
else if (_swingLow is decimal stop && candle.ClosePrice <= stop)
SellMarket(Position);
}
else if (Position < 0)
{
if (candle.ClosePrice <= lower)
BuyMarket(Math.Abs(Position));
else if (_swingHigh is decimal stop && candle.ClosePrice >= stop)
BuyMarket(Math.Abs(Position));
}
}
}
