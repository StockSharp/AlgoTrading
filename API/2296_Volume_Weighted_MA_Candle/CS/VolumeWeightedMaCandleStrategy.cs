using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volume Weighted MA Candle strategy.
/// Opens long when VWMA candle changes from bullish to neutral or bearish.
/// Opens short when VWMA candle changes from bearish to neutral or bullish.
/// </summary>
public class VolumeWeightedMaCandleStrategy : Strategy
{
private readonly StrategyParam<int> _vwmaPeriod;
private readonly StrategyParam<DataType> _candleType;

private VolumeWeightedMovingAverage _openVwma = null!;
private VolumeWeightedMovingAverage _closeVwma = null!;

private decimal? _previousColor;

/// <summary>
/// Period for VWMA calculation.
/// </summary>
public int VwmaPeriod
{
get => _vwmaPeriod.Value;
set => _vwmaPeriod.Value = value;
}

/// <summary>
/// Candle type for strategy calculation.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initialize <see cref="VolumeWeightedMaCandleStrategy"/>.
/// </summary>
public VolumeWeightedMaCandleStrategy()
{
_vwmaPeriod = Param(nameof(VwmaPeriod), 12)
.SetGreaterThanZero()
.SetDisplay("VWMA Period", "Period for volume weighted moving averages", "Parameters")
.SetCanOptimize(true)
.SetOptimize(5, 30, 5);

_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
.SetDisplay("Candle Type", "Type of candles for calculations", "Parameters");
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

_openVwma = new VolumeWeightedMovingAverage
{
Length = VwmaPeriod,
CandlePrice = CandlePrice.Open
};

_closeVwma = new VolumeWeightedMovingAverage
{
Length = VwmaPeriod,
CandlePrice = CandlePrice.Close
};

var subscription = SubscribeCandles(CandleType);

subscription
.Bind(_openVwma, _closeVwma, ProcessCandle)
.Start();

StartProtection(
takeProfit: new Unit(2, UnitTypes.Percent),
stopLoss: new Unit(1, UnitTypes.Percent)
);

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _openVwma);
DrawIndicator(area, _closeVwma);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal openVwma, decimal closeVwma)
{
if (candle.State != CandleStates.Finished)
return;

var currentColor = openVwma < closeVwma ? 2m : openVwma > closeVwma ? 0m : 1m;

if (_previousColor is decimal prevColor)
{
if (prevColor == 2m && currentColor < 2m && Position <= 0)
{
var volume = Volume + Math.Abs(Position);
BuyMarket(volume);
}
else if (prevColor == 0m && currentColor > 0m && Position >= 0)
{
var volume = Volume + Math.Abs(Position);
SellMarket(volume);
}
}

_previousColor = currentColor;
}
}
