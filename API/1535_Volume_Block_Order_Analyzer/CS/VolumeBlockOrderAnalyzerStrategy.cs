using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified port of the "Volume Block Order Analyzer" strategy.
/// Tracks cumulative volume impact and trades on extreme values.
/// </summary>
public class VolumeBlockOrderAnalyzerStrategy : Strategy
{
private readonly StrategyParam<decimal> _volumeThreshold;
private readonly StrategyParam<int> _lookbackPeriod;
private readonly StrategyParam<decimal> _impactDecay;
private readonly StrategyParam<decimal> _impactNormalization;
private readonly StrategyParam<decimal> _signalThreshold;
private readonly StrategyParam<decimal> _stopPercent;
private readonly StrategyParam<DataType> _candleType;

private decimal _cumulativeImpact;
private decimal _entryPrice;
private bool _isLong;

public decimal VolumeThreshold { get => _volumeThreshold.Value; set => _volumeThreshold.Value = value; }
public int LookbackPeriod { get => _lookbackPeriod.Value; set => _lookbackPeriod.Value = value; }
public decimal ImpactDecay { get => _impactDecay.Value; set => _impactDecay.Value = value; }
public decimal ImpactNormalization { get => _impactNormalization.Value; set => _impactNormalization.Value = value; }
public decimal SignalThreshold { get => _signalThreshold.Value; set => _signalThreshold.Value = value; }
public decimal StopPercent { get => _stopPercent.Value; set => _stopPercent.Value = value; }
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

public VolumeBlockOrderAnalyzerStrategy()
{
_volumeThreshold = Param(nameof(VolumeThreshold), 2.5m).SetDisplay("Volume Threshold", null, "Volume").SetGreaterThanZero();
_lookbackPeriod = Param(nameof(LookbackPeriod), 40).SetDisplay("Lookback Period", null, "Volume").SetGreaterThanZero();
_impactDecay = Param(nameof(ImpactDecay), 0.95m).SetDisplay("Impact Decay", null, "Impact");
_impactNormalization = Param(nameof(ImpactNormalization), 2.0m).SetDisplay("Impact Normalization", null, "Impact").SetGreaterThanZero();
_signalThreshold = Param(nameof(SignalThreshold), 0m).SetDisplay("Signal Threshold", null, "Strategy");
_stopPercent = Param(nameof(StopPercent), 10m).SetDisplay("Trailing Stop %", null, "Strategy").SetGreaterThanZero();
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type", null, "General");
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var avgVolume = new SMA { Length = LookbackPeriod };
var subscription = SubscribeCandles(CandleType);

subscription.Bind(avgVolume, (candle, avgVol) =>
{
if (candle.State != CandleStates.Finished)
return;

var isHighVolume = candle.TotalVolume > avgVol * VolumeThreshold;
var impact = 0m;

if (isHighVolume)
{
var direction = candle.ClosePrice > candle.OpenPrice ? 1m : candle.ClosePrice < candle.OpenPrice ? -1m : 0m;
var volumeWeight = avgVol == 0 ? 0 : candle.TotalVolume / avgVol;
impact = direction * volumeWeight / ImpactNormalization;
}

_cumulativeImpact = _cumulativeImpact * ImpactDecay + impact;

if (_cumulativeImpact > SignalThreshold && Position <= 0)
{
_entryPrice = candle.ClosePrice;
_isLong = true;
BuyMarket(Volume + Math.Abs(Position));
}
else if (_cumulativeImpact < -SignalThreshold && Position >= 0)
{
_entryPrice = candle.ClosePrice;
_isLong = false;
SellMarket(Volume + Math.Abs(Position));
}

if (Position != 0)
{
var stopLevel = _entryPrice * (1m + ( _isLong ? -1m : 1m) * StopPercent / 100m);
if (_isLong && candle.LowPrice <= stopLevel)
SellMarket(Math.Abs(Position));
else if (!_isLong && candle.HighPrice >= stopLevel)
BuyMarket(Math.Abs(Position));
}
});

subscription.Start();
}
}
