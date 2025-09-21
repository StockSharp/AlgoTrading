using System;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader utility "Spread data collector".
/// Tracks bid/ask spreads, counts how many ticks fall inside configured point ranges, and prints yearly statistics.
/// </summary>
public class SpreadDataCollectorStrategy : Strategy
{
private readonly StrategyParam<int> _firstBucketPoints;
private readonly StrategyParam<int> _secondBucketPoints;
private readonly StrategyParam<int> _thirdBucketPoints;
private readonly StrategyParam<int> _fourthBucketPoints;
private readonly StrategyParam<int> _fifthBucketPoints;

private decimal _priceStep;

private int? _currentYear;

private long _lessThanFirst;
private long _betweenFirstSecond;
private long _betweenSecondThird;
private long _betweenThirdFourth;
private long _betweenFourthFifth;
private long _greaterThanFifth;

private decimal? _lastBid;
private decimal? _lastAsk;

/// <summary>
/// Upper bound of the first spread bucket, expressed in points.
/// </summary>
public int FirstBucketPoints
{
get => _firstBucketPoints.Value;
set => _firstBucketPoints.Value = value;
}

/// <summary>
/// Upper bound of the second spread bucket, expressed in points.
/// </summary>
public int SecondBucketPoints
{
get => _secondBucketPoints.Value;
set => _secondBucketPoints.Value = value;
}

/// <summary>
/// Upper bound of the third spread bucket, expressed in points.
/// </summary>
public int ThirdBucketPoints
{
get => _thirdBucketPoints.Value;
set => _thirdBucketPoints.Value = value;
}

/// <summary>
/// Upper bound of the fourth spread bucket, expressed in points.
/// </summary>
public int FourthBucketPoints
{
get => _fourthBucketPoints.Value;
set => _fourthBucketPoints.Value = value;
}

/// <summary>
/// Upper bound of the fifth spread bucket, expressed in points.
/// </summary>
public int FifthBucketPoints
{
get => _fifthBucketPoints.Value;
set => _fifthBucketPoints.Value = value;
}

public SpreadDataCollectorStrategy()
{
_firstBucketPoints = Param(nameof(FirstBucketPoints), 10)
.SetDisplay("Bucket #1 Upper Bound", "Upper bound of the first spread bucket in points.", "Spread Buckets");

_secondBucketPoints = Param(nameof(SecondBucketPoints), 20)
.SetDisplay("Bucket #2 Upper Bound", "Upper bound of the second spread bucket in points.", "Spread Buckets");

_thirdBucketPoints = Param(nameof(ThirdBucketPoints), 30)
.SetDisplay("Bucket #3 Upper Bound", "Upper bound of the third spread bucket in points.", "Spread Buckets");

_fourthBucketPoints = Param(nameof(FourthBucketPoints), 40)
.SetDisplay("Bucket #4 Upper Bound", "Upper bound of the fourth spread bucket in points.", "Spread Buckets");

_fifthBucketPoints = Param(nameof(FifthBucketPoints), 50)
.SetDisplay("Bucket #5 Upper Bound", "Upper bound of the fifth spread bucket in points.", "Spread Buckets");
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();
ResetState();
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

if (Security == null)
throw new InvalidOperationException("Main security must be assigned.");

_priceStep = Security.PriceStep ?? 0m;

if (_priceStep <= 0m)
throw new InvalidOperationException("Security.PriceStep must be configured with a positive value.");

ValidateBuckets();
ResetCounters();

// Subscribe to Level1 to receive bid/ask snapshots for spread monitoring.
SubscribeLevel1()
.Bind(ProcessLevel1)
.Start();
}

/// <inheritdoc />
protected override void OnStopped()
{
if (_currentYear is int year)
PrintYearSummary(year);

base.OnStopped();
}

private void ProcessLevel1(Level1ChangeMessage message)
{
var time = message.ServerTime;

if (time == default)
return;

if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue) && bidValue is decimal bid)
_lastBid = bid;

if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue) && askValue is decimal ask)
_lastAsk = ask;

if (_lastBid is not decimal lastBid || _lastAsk is not decimal lastAsk)
return;

var spread = lastAsk - lastBid;

if (spread <= 0m)
return;

var year = time.Year;

if (_currentYear != year)
{
if (_currentYear is int previousYear)
PrintYearSummary(previousYear);

ResetCounters();
_currentYear = year;
}

// Convert point thresholds to absolute price distances.
var firstLimit = FirstBucketPoints * _priceStep;
var secondLimit = SecondBucketPoints * _priceStep;
var thirdLimit = ThirdBucketPoints * _priceStep;
var fourthLimit = FourthBucketPoints * _priceStep;
var fifthLimit = FifthBucketPoints * _priceStep;

if (spread < firstLimit)
{
_lessThanFirst++;
}
else if (spread < secondLimit)
{
_betweenFirstSecond++;
}
else if (spread < thirdLimit)
{
_betweenSecondThird++;
}
else if (spread < fourthLimit)
{
_betweenThirdFourth++;
}
else if (spread < fifthLimit)
{
_betweenFourthFifth++;
}
else
{
_greaterThanFifth++;
}
}

private void ValidateBuckets()
{
if (!(FirstBucketPoints < SecondBucketPoints &&
SecondBucketPoints < ThirdBucketPoints &&
ThirdBucketPoints < FourthBucketPoints &&
FourthBucketPoints < FifthBucketPoints))
{
throw new InvalidOperationException("Bucket thresholds must be strictly increasing.");
}
}

private void ResetState()
{
_priceStep = 0m;
_currentYear = null;
_lastBid = null;
_lastAsk = null;
ResetCounters();
}

private void ResetCounters()
{
_lessThanFirst = 0;
_betweenFirstSecond = 0;
_betweenSecondThird = 0;
_betweenThirdFourth = 0;
_betweenFourthFifth = 0;
_greaterThanFifth = 0;
}

private void PrintYearSummary(int year)
{
// Report the accumulated spread statistics to the strategy log.
LogInfo($"Year={year} Spread<={FirstBucketPoints}pts={_lessThanFirst} Spread_{FirstBucketPoints}_{SecondBucketPoints}pts={_betweenFirstSecond} Spread_{SecondBucketPoints}_{ThirdBucketPoints}pts={_betweenSecondThird} Spread_{ThirdBucketPoints}_{FourthBucketPoints}pts={_betweenThirdFourth} Spread_{FourthBucketPoints}_{FifthBucketPoints}pts={_betweenFourthFifth} Spread>{FifthBucketPoints}pts={_greaterThanFifth}");
}
}
