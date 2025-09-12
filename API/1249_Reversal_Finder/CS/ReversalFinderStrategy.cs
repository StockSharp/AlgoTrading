using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Reversal Finder strategy.
/// Detects potential reversal bars based on range expansion and extreme prices.
/// Enters long when a large range candle closes near its high after making a new low.
/// Enters short when a large range candle closes near its low after making a new high.
/// </summary>
public class ReversalFinderStrategy : Strategy
{
private readonly StrategyParam<int> _lookback;
private readonly StrategyParam<int> _smaLength;
private readonly StrategyParam<decimal> _rangeMultiple;
private readonly StrategyParam<decimal> _rangeThreshold;
private readonly StrategyParam<DataType> _candleType;

private SimpleMovingAverage _rangeSma;
private Highest _highest;
private Lowest _lowest;

private decimal? _prevHigh;
private decimal? _prevLow;

/// <summary>
/// Lookback period for highest high and lowest low.
/// </summary>
public int Lookback
{
get => _lookback.Value;
set => _lookback.Value = value;
}

/// <summary>
/// SMA length for average range calculation.
/// </summary>
public int SmaLength
{
get => _smaLength.Value;
set => _smaLength.Value = value;
}

/// <summary>
/// Range multiple threshold.
/// </summary>
public decimal RangeMultiple
{
get => _rangeMultiple.Value;
set => _rangeMultiple.Value = value;
}

/// <summary>
/// Range threshold as fraction (0-1).
/// </summary>
public decimal RangeThreshold
{
get => _rangeThreshold.Value;
set => _rangeThreshold.Value = value;
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
/// Initializes a new instance of <see cref="ReversalFinderStrategy"/>.
/// </summary>
public ReversalFinderStrategy()
{
_lookback = Param(nameof(Lookback), 20)
.SetGreaterThanZero()
.SetDisplay("Lookback", "Period for highest high/lowest low", "General")
.SetCanOptimize(true);

_smaLength = Param(nameof(SmaLength), 20)
.SetGreaterThanZero()
.SetDisplay("SMA Length", "Length for average range", "General")
.SetCanOptimize(true);

_rangeMultiple = Param(nameof(RangeMultiple), 1.5m)
.SetDisplay("Range Multiple", "Multiplier for average range", "General")
.SetCanOptimize(true);

_rangeThreshold = Param(nameof(RangeThreshold), 0.5m)
.SetDisplay("Range Threshold", "Fraction of range near extreme", "General")
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

_rangeSma = default;
_highest = default;
_lowest = default;
_prevHigh = default;
_prevLow = default;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_rangeSma = new SimpleMovingAverage { Length = SmaLength };
_highest = new Highest { Length = Lookback };
_lowest = new Lowest { Length = Lookback };

var subscription = SubscribeCandles(CandleType);
subscription.WhenNew(ProcessCandle).Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _rangeSma);
DrawIndicator(area, _highest);
DrawIndicator(area, _lowest);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

var range = candle.HighPrice - candle.LowPrice;
var avgRangeValue = _rangeSma.Process(range);

IIndicatorValue highestValue = default;
IIndicatorValue lowestValue = default;

if (_prevHigh != null && _prevLow != null)
{
highestValue = _highest.Process(_prevHigh.Value);
lowestValue = _lowest.Process(_prevLow.Value);
}

_prevHigh = candle.HighPrice;
_prevLow = candle.LowPrice;

if (!avgRangeValue.IsFinal || highestValue == null || !highestValue.IsFinal || lowestValue == null || !lowestValue.IsFinal)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var avgRange = avgRangeValue.ToDecimal();
var highest = highestValue.ToDecimal();
var lowest = lowestValue.ToDecimal();

var rangeCondition = range >= avgRange * RangeMultiple;

var longSignal = rangeCondition && candle.LowPrice < lowest &&
candle.ClosePrice >= candle.HighPrice - range * RangeThreshold;

var shortSignal = rangeCondition && candle.HighPrice > highest &&
candle.ClosePrice <= candle.LowPrice + range * RangeThreshold;

if (longSignal && Position <= 0)
{
var volume = Volume + (Position < 0 ? -Position : 0m);
BuyMarket(volume);
}
else if (shortSignal && Position >= 0)
{
var volume = Volume + (Position > 0 ? Position : 0m);
SellMarket(volume);
}
}
}

