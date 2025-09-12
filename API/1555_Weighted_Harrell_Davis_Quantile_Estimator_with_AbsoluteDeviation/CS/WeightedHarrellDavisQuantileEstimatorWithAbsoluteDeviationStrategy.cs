using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Weighted Harrell-Davis quantile estimator with absolute deviation fences.
/// Buys when price drops below the lower fence and sells when price rises above the upper fence.
/// </summary>
public class WeightedHarrellDavisQuantileEstimatorWithAbsoluteDeviationStrategy : Strategy
{
private readonly StrategyParam<int> _length;
private readonly StrategyParam<decimal> _deviationMultiplier;
private readonly StrategyParam<DataType> _candleType;

private Median _median = null!;
private Median _mad = null!;

private decimal _upperBand;
private decimal _lowerBand;

/// <summary>
/// Period for median and deviation calculations.
/// </summary>
public int Length
{
get => _length.Value;
set => _length.Value = value;
}

/// <summary>
/// Multiplier for absolute deviation bands.
/// </summary>
public decimal DeviationMultiplier
{
get => _deviationMultiplier.Value;
set => _deviationMultiplier.Value = value;
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
/// Initializes a new instance of <see cref="WeightedHarrellDavisQuantileEstimatorWithAbsoluteDeviationStrategy"/>.
/// </summary>
public WeightedHarrellDavisQuantileEstimatorWithAbsoluteDeviationStrategy()
{
_length = Param(nameof(Length), 39)
.SetGreaterThanZero()
.SetDisplay("Length", "Lookback period", "General")
.SetCanOptimize(true);

_deviationMultiplier = Param(nameof(DeviationMultiplier), 1.213m)
.SetDisplay("Deviation Multiplier", "Band multiplier", "General")
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
_median = null!;
_mad = null!;
_upperBand = 0m;
_lowerBand = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_median = new Median { Length = Length };
_mad = new Median { Length = Length };

var subscription = SubscribeCandles(CandleType);
subscription.Bind(ProcessCandle).Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _median);
DrawIndicator(area, _mad);
DrawOwnTrades(area);
}

StartProtection();
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

var medianValue = _median.Process(candle.ClosePrice);
var deviation = Math.Abs(candle.ClosePrice - medianValue.ToDecimal());
var madValue = _mad.Process(deviation);

if (!medianValue.IsFinal || !madValue.IsFinal)
return;

var median = medianValue.ToDecimal();
var mad = madValue.ToDecimal() * 1.4826m;

_upperBand = median + DeviationMultiplier * mad;
_lowerBand = median - DeviationMultiplier * mad;

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (candle.ClosePrice > _upperBand && Position >= 0)
{
SellMarket(Volume + (Position > 0 ? Position : 0m));
}
else if (candle.ClosePrice < _lowerBand && Position <= 0)
{
BuyMarket(Volume + (Position < 0 ? -Position : 0m));
}
}
}
