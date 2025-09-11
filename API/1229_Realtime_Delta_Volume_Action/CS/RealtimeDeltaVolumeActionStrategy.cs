using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Realtime Delta Volume Action strategy.
/// Buys when cumulative delta volume exceeds threshold, sells when below negative threshold.
/// </summary>
public class RealtimeDeltaVolumeActionStrategy : Strategy
{
private readonly StrategyParam<decimal> _deltaThreshold;
private readonly StrategyParam<DataType> _candleType;

private decimal _deltaVolume;

/// <summary>
/// Volume delta threshold for entry.
/// </summary>
public decimal DeltaThreshold
{
get => _deltaThreshold.Value;
set => _deltaThreshold.Value = value;
}

/// <summary>
/// Candle type for calculations.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initialize <see cref="RealtimeDeltaVolumeActionStrategy"/>.
/// </summary>
public RealtimeDeltaVolumeActionStrategy()
{
_deltaThreshold = Param(nameof(DeltaThreshold), 100m)
.SetGreaterThanZero()
.SetDisplay("Delta Threshold", "Volume delta required to trade", "Parameters")
.SetCanOptimize(true)
.SetOptimize(50m, 300m, 50m);

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
_deltaVolume = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var candleSub = SubscribeCandles(CandleType);
candleSub.Bind(ProcessCandle).Start();

SubscribeTicks().Bind(ProcessTrade).Start();

StartProtection(
takeProfit: new Unit(3, UnitTypes.Percent),
stopLoss: new Unit(2, UnitTypes.Percent)
);

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, candleSub);
DrawOwnTrades(area);
}
}

private void ProcessTrade(Trade trade)
{
var delta = trade.OriginSide == Sides.Buy ? trade.Volume : -trade.Volume;
_deltaVolume += delta;
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
{
_deltaVolume = 0m;
return;
}

LogInfo($"Delta volume: {_deltaVolume}");

if (_deltaVolume > DeltaThreshold && Position <= 0)
BuyMarket(Volume + Math.Abs(Position));
else if (_deltaVolume < -DeltaThreshold && Position >= 0)
SellMarket(Volume + Math.Abs(Position));

_deltaVolume = 0m;
}
}
