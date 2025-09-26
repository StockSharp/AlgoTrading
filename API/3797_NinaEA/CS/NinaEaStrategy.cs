using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SuperTrend-based conversion of the MetaTrader "NinaEA" expert advisor.
/// The strategy watches for SuperTrend direction flips and trades a single position accordingly.
/// </summary>
public class NinaEaStrategy : Strategy
{
private readonly StrategyParam<int> _atrPeriod;
private readonly StrategyParam<decimal> _atrMultiplier;
private readonly StrategyParam<decimal> _stopLossPoints;
private readonly StrategyParam<DataType> _candleType;

private SuperTrend _superTrend = null!;
private bool? _previousTrendUp;
private decimal? _longStopPrice;
private decimal? _shortStopPrice;

/// <summary>
/// Initializes a new instance of the <see cref="NinaEaStrategy"/> class.
/// </summary>
public NinaEaStrategy()
{

_atrPeriod = Param(nameof(AtrPeriod), 10)
.SetDisplay("ATR Period", "ATR length for the SuperTrend filter", "Indicators")
.SetCanOptimize(true)
.SetOptimize(5, 40, 5);

_atrMultiplier = Param(nameof(AtrMultiplier), 1m)
.SetDisplay("ATR Multiplier", "ATR multiplier applied by SuperTrend", "Indicators")
.SetCanOptimize(true)
.SetOptimize(0.5m, 4m, 0.5m);

_stopLossPoints = Param(nameof(StopLossPoints), 0m)
.SetDisplay("Stop Loss (points)", "Optional stop-loss distance expressed in price points", "Risk");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Source candle series", "Trading");
}


/// <summary>
/// ATR period forwarded to the SuperTrend indicator.
/// </summary>
public int AtrPeriod
{
get => _atrPeriod.Value;
set => _atrPeriod.Value = value;
}

/// <summary>
/// ATR multiplier forwarded to the SuperTrend indicator.
/// </summary>
public decimal AtrMultiplier
{
get => _atrMultiplier.Value;
set => _atrMultiplier.Value = value;
}

/// <summary>
/// Protective stop distance in price points (0 disables the stop-loss).
/// </summary>
public decimal StopLossPoints
{
get => _stopLossPoints.Value;
set => _stopLossPoints.Value = value;
}

/// <summary>
/// Candle type driving the strategy logic.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
yield return (Security, CandleType);
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_superTrend = new SuperTrend
{
Length = AtrPeriod,
Multiplier = AtrMultiplier
};

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx(_superTrend, ProcessCandle)
.Start();

// Start basic protection so trailing or manual risk controls can be added later.
StartProtection();
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue superTrendValue)
{
if (candle.State != CandleStates.Finished)
return;

// Ensure the strategy is ready and the indicator has produced reliable output.
if (!IsFormedAndOnlineAndAllowTrading() || !_superTrend.IsFormed)
return;

var superTrend = (SuperTrendIndicatorValue)superTrendValue;
var isUpTrend = superTrend.IsUpTrend;

// Apply manual stop-loss management before reacting to new signals.
ManageStops(candle);

// Close an opposite position immediately when the trend flips.
if (Position > 0 && !isUpTrend)
{
SellMarket(Position);
_longStopPrice = null;
}
else if (Position < 0 && isUpTrend)
{
BuyMarket(Math.Abs(Position));
_shortStopPrice = null;
}

// Open a new trade only on a confirmed SuperTrend direction change.
if (_previousTrendUp is bool prevUp)
{
if (isUpTrend && !prevUp && Position <= 0)
{
OpenLong(candle.ClosePrice);
}
else if (!isUpTrend && prevUp && Position >= 0)
{
OpenShort(candle.ClosePrice);
}
}

_previousTrendUp = isUpTrend;
}

private void ManageStops(ICandleMessage candle)
{
if (Security?.PriceStep is not decimal step || step <= 0m)
return;

// Exit a long position if the candle pierces the configured stop price.
if (Position > 0 && _longStopPrice is decimal longStop && candle.LowPrice <= longStop)
{
SellMarket(Position);
_longStopPrice = null;
}
// Exit a short position if the candle pierces the configured stop price.
else if (Position < 0 && _shortStopPrice is decimal shortStop && candle.HighPrice >= shortStop)
{
BuyMarket(Math.Abs(Position));
_shortStopPrice = null;
}
}

private void OpenLong(decimal entryPrice)
{
var volume = Volume;

// Offset an existing short position before establishing the new long.
if (Position < 0)
volume += Math.Abs(Position);

if (volume <= 0m)
return;

BuyMarket(volume);

_shortStopPrice = null;
_longStopPrice = CalculateStop(entryPrice, isLong: true);
}

private void OpenShort(decimal entryPrice)
{
var volume = Volume;

// Offset an existing long position before establishing the new short.
if (Position > 0)
volume += Position;

if (volume <= 0m)
return;

SellMarket(volume);

_longStopPrice = null;
_shortStopPrice = CalculateStop(entryPrice, isLong: false);
}

private decimal? CalculateStop(decimal entryPrice, bool isLong)
{
if (StopLossPoints <= 0m || Security?.PriceStep is not decimal step || step <= 0m)
return null;

var distance = step * StopLossPoints;

return isLong ? entryPrice - distance : entryPrice + distance;
}
}
