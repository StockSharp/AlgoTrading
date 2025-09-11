using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Williams %R cross strategy with 200-period SMA filter and fixed targets.
/// </summary>
public class WilliamsRCrossStrategyWith200MaFilterStrategy : Strategy
{
private readonly StrategyParam<int> _wrLength;
private readonly StrategyParam<decimal> _crossThreshold;
private readonly StrategyParam<decimal> _takeProfit;
private readonly StrategyParam<decimal> _stopLoss;
private readonly StrategyParam<DataType> _candleType;

private decimal _prevWr;
private decimal _entryPrice;

/// <summary>
/// Williams %R length.
/// </summary>
public int WrLength
{
get => _wrLength.Value;
set => _wrLength.Value = value;
}

/// <summary>
/// Threshold offset for cross detection.
/// </summary>
public decimal CrossThreshold
{
get => _crossThreshold.Value;
set => _crossThreshold.Value = value;
}

/// <summary>
/// Take profit distance in price steps.
/// </summary>
public decimal TakeProfit
{
get => _takeProfit.Value;
set => _takeProfit.Value = value;
}

/// <summary>
/// Stop loss distance in price steps.
/// </summary>
public decimal StopLoss
{
get => _stopLoss.Value;
set => _stopLoss.Value = value;
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
/// Initializes a new instance of <see cref="WilliamsRCrossStrategyWith200MaFilterStrategy"/>.
/// </summary>
public WilliamsRCrossStrategyWith200MaFilterStrategy()
{
_wrLength = Param(nameof(WrLength), 14)
.SetGreaterThanZero()
.SetDisplay("%R Length", "Williams %R period", "General")
.SetCanOptimize(true);

_crossThreshold = Param(nameof(CrossThreshold), 10m)
.SetDisplay("Cross Threshold", "Offset from -50 level", "General")
.SetCanOptimize(true);

_takeProfit = Param(nameof(TakeProfit), 30m)
.SetDisplay("Take Profit", "Profit target in steps", "General")
.SetCanOptimize(true);

_stopLoss = Param(nameof(StopLoss), 20m)
.SetDisplay("Stop Loss", "Loss limit in steps", "General")
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
_prevWr = 0m;
_entryPrice = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var wpr = new WilliamsR { Length = WrLength };
var ma200 = new SimpleMovingAverage { Length = 200 };

var subscription = SubscribeCandles(CandleType);
subscription.Bind(wpr, ma200, ProcessCandle).Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, wpr);
DrawIndicator(area, ma200);
DrawOwnTrades(area);
}

StartProtection();
}

private void ProcessCandle(ICandleMessage candle, decimal wr, decimal ma)
{
if (candle.State != CandleStates.Finished)
{
_prevWr = wr;
return;
}

var thresholdLong = -50m - CrossThreshold;
var thresholdShort = -50m + CrossThreshold;

var enterLong = _prevWr < thresholdLong && wr >= thresholdLong && candle.ClosePrice > ma;
var enterShort = _prevWr > thresholdShort && wr <= thresholdShort && candle.ClosePrice < ma;

if (!IsFormedAndOnlineAndAllowTrading())
{
_prevWr = wr;
return;
}

if (enterLong && Position <= 0)
{
var volume = Volume + (Position < 0 ? -Position : 0m);
BuyMarket(volume);
_entryPrice = candle.ClosePrice;
}
else if (enterShort && Position >= 0)
{
var volume = Volume + (Position > 0 ? Position : 0m);
SellMarket(volume);
_entryPrice = candle.ClosePrice;
}
else if (Position > 0)
{
var tp = _entryPrice + TakeProfit * Security.PriceStep;
var sl = _entryPrice - StopLoss * Security.PriceStep;
if (candle.ClosePrice >= tp || candle.ClosePrice <= sl)
SellMarket(Position);
}
else if (Position < 0)
{
var tp = _entryPrice - TakeProfit * Security.PriceStep;
var sl = _entryPrice + StopLoss * Security.PriceStep;
if (candle.ClosePrice <= tp || candle.ClosePrice >= sl)
BuyMarket(-Position);
}

_prevWr = wr;
}
}
