using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy reacting to volume delta similar to "Tape" indicator.
/// </summary>
public class TapeStrategy : Strategy
{
private readonly StrategyParam<decimal> _threshold;
private readonly StrategyParam<DataType> _candleType;

private decimal _lastPrice;
private decimal _lastVolume;

public decimal VolumeDeltaThreshold { get => _threshold.Value; set => _threshold.Value = value; }
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

public TapeStrategy()
{
_threshold = Param(nameof(VolumeDeltaThreshold), 1000000m)
.SetGreaterThanZero()
.SetDisplay("Volume Δ", "Volume delta threshold", "General");
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
=> [(Security, CandleType)];

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();
_lastPrice = 0m;
_lastVolume = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);
SubscribeCandles(CandleType).Bind(Process).Start();
}

private void Process(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

var deltaVolume = (candle.TotalVolume - _lastVolume) * Math.Sign(candle.ClosePrice - _lastPrice);

if (deltaVolume > VolumeDeltaThreshold && Position <= 0)
BuyMarket(Volume + Math.Abs(Position));
else if (deltaVolume < -VolumeDeltaThreshold && Position >= 0)
SellMarket(Volume + Math.Abs(Position));

_lastPrice = candle.ClosePrice;
_lastVolume = candle.TotalVolume;
}
}
