using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Time of day / day of week sigma spike strategy.
/// Calculates return z-score and buys on spikes.
/// Counts spikes per hour for statistics.
/// </summary>
public class TimeOfDayDayOfWeekSigmaSpikeStrategy : Strategy
{
private readonly StrategyParam<decimal> _threshold;
private readonly StrategyParam<bool> _allDays;
private readonly StrategyParam<DayOfWeek> _dayOfWeek;
private readonly StrategyParam<int> _stdevLength;
private readonly StrategyParam<DataType> _candleType;

private readonly int[] _hourCounts = new int[24];
private StandardDeviation _stdev;
private decimal? _prevClose;
private decimal _prevSd;

/// <summary>
/// Sigma spike threshold.
/// </summary>
public decimal Threshold
{
get => _threshold.Value;
set => _threshold.Value = value;
}

/// <summary>
/// Use all days.
/// </summary>
public bool AllDays
{
get => _allDays.Value;
set => _allDays.Value = value;
}

/// <summary>
/// Day of week filter.
/// </summary>
public DayOfWeek DayOfWeekFilter
{
get => _dayOfWeek.Value;
set => _dayOfWeek.Value = value;
}

/// <summary>
/// Standard deviation length.
/// </summary>
public int StdevLength
{
get => _stdevLength.Value;
set => _stdevLength.Value = value;
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
/// Initializes a new instance of <see cref="TimeOfDayDayOfWeekSigmaSpikeStrategy"/>.
/// </summary>
public TimeOfDayDayOfWeekSigmaSpikeStrategy()
{
_threshold = Param(nameof(Threshold), 2.5m)
.SetGreaterThanZero()
.SetDisplay("Threshold", "Sigma spike threshold", "General")
.SetCanOptimize(true);

_allDays = Param(nameof(AllDays), false)
.SetDisplay("All Days", "Ignore day filter", "General");

_dayOfWeek = Param(nameof(DayOfWeekFilter), System.DayOfWeek.Monday)
.SetDisplay("Day Of Week", "Day filter", "General");

_stdevLength = Param(nameof(StdevLength), 20)
.SetGreaterThanZero()
.SetDisplay("Stdev Length", "Standard deviation length", "General")
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

_stdev = default;
_prevClose = default;
_prevSd = 0m;
Array.Clear(_hourCounts, 0, _hourCounts.Length);
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_stdev = new StandardDeviation { Length = StdevLength };

var subscription = SubscribeCandles(CandleType);
subscription.WhenNew(ProcessCandle).Start();
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

if (_prevClose == null)
{
_prevClose = candle.ClosePrice;
return;
}

var ret = candle.ClosePrice / _prevClose.Value - 1m;
var sdValue = _stdev.Process(ret);
if (!sdValue.IsFinal)
{
_prevClose = candle.ClosePrice;
return;
}

var sigma = _prevSd == 0m ? 0m : Math.Abs(ret / _prevSd);
_prevSd = sdValue.ToDecimal();
_prevClose = candle.ClosePrice;

if (sigma >= Threshold && (AllDays || candle.OpenTime.DayOfWeek == DayOfWeekFilter))
{
_hourCounts[candle.OpenTime.Hour]++;

if (Position <= 0 && IsFormedAndOnlineAndAllowTrading())
BuyMarket();
}
else if (Position > 0 && sigma < Threshold && IsFormedAndOnlineAndAllowTrading())
{
SellMarket(Position);
}
}
}
