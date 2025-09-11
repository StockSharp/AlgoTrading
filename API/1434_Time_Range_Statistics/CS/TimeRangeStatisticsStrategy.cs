using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Time range statistics strategy.
/// Collects basic statistics between start and end bars and trades based on percent change.
/// </summary>
public class TimeRangeStatisticsStrategy : Strategy
{
private readonly StrategyParam<int> _startIndex;
private readonly StrategyParam<int> _endIndex;
private readonly StrategyParam<DataType> _candleType;

private int _barIndex;
private decimal _sumPrice;
private decimal _sumVolume;
private decimal _maxPrice;
private decimal _minPrice;
private decimal _startPrice;
private int _count;
private int _gapCount;
private decimal? _prevHigh;
private decimal? _prevLow;

/// <summary>
/// Start bar index.
/// </summary>
public int StartIndex
{
get => _startIndex.Value;
set => _startIndex.Value = value;
}

/// <summary>
/// End bar index.
/// </summary>
public int EndIndex
{
get => _endIndex.Value;
set => _endIndex.Value = value;
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
/// Initializes a new instance of <see cref="TimeRangeStatisticsStrategy"/>.
/// </summary>
public TimeRangeStatisticsStrategy()
{
_startIndex = Param(nameof(StartIndex), 9000)
.SetGreaterThanZero()
.SetDisplay("Start Index", "Start bar index", "General")
.SetCanOptimize(true);

_endIndex = Param(nameof(EndIndex), 10000)
.SetGreaterThanZero()
.SetDisplay("End Index", "End bar index", "General")
.SetCanOptimize(true);

_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");
}

/// <inheritdoc />
public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
return [(Security, CandleType)];
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();

_barIndex = 0;
_sumPrice = 0m;
_sumVolume = 0m;
_maxPrice = 0m;
_minPrice = decimal.MaxValue;
_startPrice = 0m;
_count = 0;
_gapCount = 0;
_prevHigh = default;
_prevLow = default;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var subscription = SubscribeCandles(CandleType);
subscription.WhenNew(ProcessCandle).Start();
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

_barIndex++;

if (_barIndex < StartIndex || _barIndex > EndIndex)
{
_prevHigh = candle.HighPrice;
_prevLow = candle.LowPrice;
return;
}

if (_count == 0)
{
_startPrice = candle.ClosePrice;
_maxPrice = candle.ClosePrice;
_minPrice = candle.ClosePrice;
}

_sumPrice += candle.ClosePrice;
_sumVolume += candle.TotalVolume;
_maxPrice = Math.Max(_maxPrice, candle.ClosePrice);
_minPrice = Math.Min(_minPrice, candle.ClosePrice);
_count++;

if (_prevHigh != null && _prevLow != null)
{
if (candle.OpenPrice > _prevHigh || candle.OpenPrice < _prevLow)
_gapCount++;
}

_prevHigh = candle.HighPrice;
_prevLow = candle.LowPrice;

if (_barIndex != EndIndex)
return;

var mean = _sumPrice / _count;
var normRange = (_maxPrice - _minPrice) / (_maxPrice + _minPrice);
var percentChange = (candle.ClosePrice - _startPrice) / _startPrice * 100m;
var avgVolume = _sumVolume / _count;

this.LogInfo($"Mean={mean}; NormRange={normRange}; PercentChange={percentChange}; AvgVol={avgVolume}; Gaps={_gapCount}");

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (percentChange > 0 && Position <= 0)
BuyMarket();
else if (percentChange < 0 && Position >= 0)
SellMarket();
}
}
