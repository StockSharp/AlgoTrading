using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// XAU/USD strategy using ADX and Bollinger Bands.
/// Trades breakouts of Bollinger Bands when ADX confirms strong trend.
/// </summary>
public class XauUsdAdxBollingerStrategy : Strategy
{
private readonly StrategyParam<int> _adxPeriod;
private readonly StrategyParam<int> _bollPeriod;
private readonly StrategyParam<decimal> _adxThreshold;
private readonly StrategyParam<DataType> _candleType;

/// <summary>
/// ADX calculation period.
/// </summary>
public int AdxPeriod
{
get => _adxPeriod.Value;
set => _adxPeriod.Value = value;
}

/// <summary>
/// Bollinger Bands period.
/// </summary>
public int BollingerPeriod
{
get => _bollPeriod.Value;
set => _bollPeriod.Value = value;
}

/// <summary>
/// ADX threshold for confirming trend strength.
/// </summary>
public decimal AdxThreshold
{
get => _adxThreshold.Value;
set => _adxThreshold.Value = value;
}

/// <summary>
/// Candle type used by the strategy.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initializes <see cref="XauUsdAdxBollingerStrategy"/>.
/// </summary>
public XauUsdAdxBollingerStrategy()
{
_adxPeriod = Param(nameof(AdxPeriod), 14)
.SetGreaterThanZero()
.SetDisplay("ADX Period", "ADX calculation period", "Indicators");

_bollPeriod = Param(nameof(BollingerPeriod), 20)
.SetGreaterThanZero()
.SetDisplay("Bollinger Period", "Bollinger Bands period", "Indicators");

_adxThreshold = Param(nameof(AdxThreshold), 25m)
.SetDisplay("ADX Threshold", "Minimum ADX value", "Indicators");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
return [(Security, CandleType)];
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var adx = new ADX { Length = AdxPeriod };
var bb = new BollingerBands { Length = BollingerPeriod, Width = 2m };

var subscription = SubscribeCandles(CandleType);
subscription.Bind(adx, bb, ProcessCandle).Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, bb);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal adx, decimal middle, decimal upper, decimal lower)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (adx > AdxThreshold)
{
if (candle.ClosePrice > upper && Position <= 0)
{
BuyMarket(Volume + Math.Abs(Position));
}
else if (candle.ClosePrice < lower && Position >= 0)
{
SellMarket(Volume + Math.Abs(Position));
}
}
}
}
