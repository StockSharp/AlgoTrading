using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-stage FX Chaos breakout strategy converted from the MQL4 expert.
/// </summary>
public class FxChaosPyramidStrategy : Strategy
{
private readonly StrategyParam<DataType> _primaryCandleType;
private readonly StrategyParam<DataType> _dailyCandleType;
private readonly StrategyParam<int> _awesomeShortPeriod;
private readonly StrategyParam<int> _awesomeLongPeriod;
private readonly StrategyParam<decimal> _breakoutBufferPoints;
private readonly StrategyParam<int> _fractalWindowSize;
private readonly StrategyParam<int> _maxStages;
private readonly StrategyParam<bool> _requireProfitForNextStage;
private readonly StrategyParam<decimal> _volumeScale;
private readonly StrategyParam<decimal> _baseBalance;
private readonly StrategyParam<decimal> _balanceStep;

private AwesomeOscillator _awesomeOscillator = null!;
private FractalTracker _dailyTracker = null!;
private FractalTracker _primaryTracker = null!;

private DateTime _currentDay;
private decimal _currentDayOpen;
private bool _hasCurrentDayOpen;

private decimal _previousDayHigh;
private decimal _previousDayLow;
private bool _hasPreviousDayLevels;

private decimal _previousCandleHigh;
private decimal _previousCandleLow;
private bool _hasPreviousCandle;

private int _longStages;
private int _shortStages;

/// <summary>
/// Initialize <see cref="FxChaosPyramidStrategy"/>.
/// </summary>
public FxChaosPyramidStrategy()
{
_primaryCandleType = Param(nameof(PrimaryCandleType), TimeSpan.FromHours(4).TimeFrame())
.SetDisplay("Primary Candle", "Trading timeframe used for entries", "General");

_dailyCandleType = Param(nameof(DailyCandleType), TimeSpan.FromDays(1).TimeFrame())
.SetDisplay("Daily Candle", "Higher timeframe used for breakout levels", "General");

_awesomeShortPeriod = Param(nameof(AwesomeShortPeriod), 5)
.SetDisplay("AO Fast", "Fast period of the Awesome Oscillator", "Awesome Oscillator")
.SetGreaterThanZero();

_awesomeLongPeriod = Param(nameof(AwesomeLongPeriod), 34)
.SetDisplay("AO Slow", "Slow period of the Awesome Oscillator", "Awesome Oscillator")
.SetGreaterThanZero();

_breakoutBufferPoints = Param(nameof(BreakoutBufferPoints), 2m)
.SetDisplay("Breakout Buffer", "Additional buffer in price steps added to previous highs and lows", "Trading")
.SetNotNegative();

_fractalWindowSize = Param(nameof(FractalWindowSize), 5)
.SetDisplay("Fractal Window", "Number of candles required to confirm a fractal.", "Indicators")
.SetRange(5, 15);

_maxStages = Param(nameof(MaxStages), 5)
.SetDisplay("Max Stages", "Maximum number of pyramid entries", "Risk")
.SetRange(1, 5);

_requireProfitForNextStage = Param(nameof(RequireProfitForNextStage), true)
.SetDisplay("Require Profit", "Allow extra stages only when equity is above balance", "Risk");

_volumeScale = Param(nameof(VolumeScale), 1m)
.SetDisplay("Volume Scale", "Global multiplier applied to the predefined lot matrix", "Trading")
.SetGreaterThanZero();

_baseBalance = Param(nameof(BaseBalance), 3000m)
.SetDisplay("Base Balance", "Reference balance that maps to the smallest lot matrix", "Money Management")
.SetGreaterThanZero();

_balanceStep = Param(nameof(BalanceStep), 3000m)
.SetDisplay("Balance Step", "Balance increment that increases the maximum lot bucket", "Money Management")
.SetGreaterThanZero();

ApplyFractalWindow(_fractalWindowSize.Value);
}

/// <summary>
/// Primary trading timeframe.
/// </summary>
public DataType PrimaryCandleType
{
get => _primaryCandleType.Value;
set => _primaryCandleType.Value = value;
}

/// <summary>
/// Higher timeframe used to compute daily breakout levels.
/// </summary>
public DataType DailyCandleType
{
get => _dailyCandleType.Value;
set => _dailyCandleType.Value = value;
}

/// <summary>
/// Fast period for the Awesome Oscillator.
/// </summary>
public int AwesomeShortPeriod
{
get => _awesomeShortPeriod.Value;
set => _awesomeShortPeriod.Value = value;
}

/// <summary>
/// Slow period for the Awesome Oscillator.
/// </summary>
public int AwesomeLongPeriod
{
get => _awesomeLongPeriod.Value;
set => _awesomeLongPeriod.Value = value;
}

/// <summary>
/// Buffer in price steps added to the previous highs and lows before breakout validation.
/// </summary>
public decimal BreakoutBufferPoints
{
get => _breakoutBufferPoints.Value;
set => _breakoutBufferPoints.Value = value;
}

/// <summary>
/// Maximum number of pyramid stages.
/// </summary>
public int MaxStages
{
get => _maxStages.Value;
set => _maxStages.Value = value;
}

/// <summary>
/// Require open equity to be greater than balance before adding more stages.
/// </summary>
public bool RequireProfitForNextStage
{
get => _requireProfitForNextStage.Value;
set => _requireProfitForNextStage.Value = value;
}

public int FractalWindowSize
{
get => _fractalWindowSize.Value;
set => ApplyFractalWindow(value);
}

private void ApplyFractalWindow(int value)
{
var normalized = NormalizeWindow(value);
_fractalWindowSize.Value = normalized;
_dailyTracker = new FractalTracker(normalized);
_primaryTracker = new FractalTracker(normalized);
}

private static int NormalizeWindow(int value)
{
if (value < 5)
value = 5;

if ((value & 1) == 0)
value += 1;

return value;
}

/// <summary>
/// Global multiplier applied to the MQL4 lot matrix.
/// </summary>
public decimal VolumeScale
{
get => _volumeScale.Value;
set => _volumeScale.Value = value;
}

/// <summary>
/// Account balance associated with the smallest lot bucket.
/// </summary>
public decimal BaseBalance
{
get => _baseBalance.Value;
set => _baseBalance.Value = value;
}

/// <summary>
/// Balance increment that increases the lot bucket.
/// </summary>
public decimal BalanceStep
{
get => _balanceStep.Value;
set => _balanceStep.Value = value;
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
return
[
(Security, PrimaryCandleType),
(Security, DailyCandleType)
];
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();

ApplyFractalWindow(_fractalWindowSize.Value);

_currentDay = default;
_currentDayOpen = 0m;
_hasCurrentDayOpen = false;

_previousDayHigh = 0m;
_previousDayLow = 0m;
_hasPreviousDayLevels = false;

_previousCandleHigh = 0m;
_previousCandleLow = 0m;
_hasPreviousCandle = false;

_longStages = 0;
_shortStages = 0;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_awesomeOscillator = new AwesomeOscillator
{
ShortPeriod = AwesomeShortPeriod,
LongPeriod = AwesomeLongPeriod
};

var dailySubscription = SubscribeCandles(DailyCandleType);
dailySubscription.Bind(ProcessDailyCandle).Start();

var primarySubscription = SubscribeCandles(PrimaryCandleType);
primarySubscription.BindEx(_awesomeOscillator, ProcessPrimaryCandle).Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, primarySubscription);
DrawIndicator(area, _awesomeOscillator);
DrawOwnTrades(area);
}
}

private void ProcessDailyCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

_dailyTracker.Update(candle);

_previousDayHigh = candle.HighPrice;
_previousDayLow = candle.LowPrice;
_hasPreviousDayLevels = true;
}

private void ProcessPrimaryCandle(ICandleMessage candle, IIndicatorValue awesomeValue)
{
if (candle.State != CandleStates.Finished)
return;

UpdateDayContext(candle);
_primaryTracker.Update(candle);

if (!awesomeValue.IsFinal)
{
UpdatePreviousCandleLevels(candle);
return;
}

var awesome = awesomeValue.GetValue<decimal>();
var buffer = GetBuffer();

var dailyUp = _dailyTracker.LastUp;
var dailyDown = _dailyTracker.LastDown;
var intradayUp = _primaryTracker.LastUp;
var intradayDown = _primaryTracker.LastDown;

var close = candle.ClosePrice;
var open = candle.OpenPrice;

var dailyHighLevel = _previousDayHigh + buffer;
var dailyLowLevel = _previousDayLow - buffer;
var intradayHighLevel = _previousCandleHigh + buffer;
var intradayLowLevel = _previousCandleLow - buffer;

var longBaseSignal = _hasPreviousDayLevels && _hasCurrentDayOpen && dailyUp is decimal dailyUpValue &&
close < dailyUpValue && _currentDayOpen < dailyHighLevel && close > dailyHighLevel && awesome < 0m;

var shortBaseSignal = _hasPreviousDayLevels && _hasCurrentDayOpen && dailyDown is decimal dailyDownValue &&
close > dailyDownValue && _currentDayOpen > dailyLowLevel && close < dailyLowLevel && awesome > 0m;

var longAddSignal = intradayUp is decimal intradayUpValue && close < intradayUpValue &&
_hasPreviousCandle && open < intradayHighLevel && close > intradayHighLevel;

var shortAddSignal = intradayDown is decimal intradayDownValue && close > intradayDownValue &&
_hasPreviousCandle && open > intradayLowLevel && close < intradayLowLevel;

if (longBaseSignal && Position <= 0)
{
if (Position < 0)
{
ClosePosition();
_shortStages = 0;
}

if (Position <= 0)
{
EnterLongStage(1);
}
}
else if (shortBaseSignal && Position >= 0)
{
if (Position > 0)
{
ClosePosition();
_longStages = 0;
}

if (Position >= 0)
{
EnterShortStage(1);
}
}
else
{
HandlePyramiding(longAddSignal, shortAddSignal);
}

if (Position == 0)
{
_longStages = 0;
_shortStages = 0;
}

UpdatePreviousCandleLevels(candle);
}

private void HandlePyramiding(bool longAddSignal, bool shortAddSignal)
{
if (longAddSignal && Position > 0)
{
var nextStage = _longStages + 1;
if (nextStage <= MaxStages && CanAddStage(nextStage))
{
EnterLongStage(nextStage);
}
}
else if (shortAddSignal && Position < 0)
{
var nextStage = _shortStages + 1;
if (nextStage <= MaxStages && CanAddStage(nextStage))
{
EnterShortStage(nextStage);
}
}
}

private void EnterLongStage(int stage)
{
var volume = GetStageVolume(stage);
if (volume <= 0m)
return;

BuyMarket(volume);
_longStages = stage;
_shortStages = 0;
}

private void EnterShortStage(int stage)
{
var volume = GetStageVolume(stage);
if (volume <= 0m)
return;

SellMarket(volume);
_shortStages = stage;
_longStages = 0;
}

private bool CanAddStage(int stage)
{
if (stage <= 1)
return true;

if (!RequireProfitForNextStage)
return true;

if (Portfolio is null)
return false;

var equity = Portfolio.CurrentValue;
var balance = Portfolio.BeginValue;

if (equity <= 0m && balance <= 0m)
return false;

if (equity <= 0m)
equity = balance;

return equity > balance;
}

private decimal GetStageVolume(int stage)
{
var maxLots = CalculateMaxLots();
var set = StageVolumeSet.Create(maxLots);

return stage switch
{
1 => set.Stage1 * VolumeScale,
2 => set.Stage2 * VolumeScale,
3 => set.Stage3 * VolumeScale,
4 => set.Stage4 * VolumeScale,
5 => set.Stage5 * VolumeScale,
_ => 0m
};
}

private decimal CalculateMaxLots()
{
if (Portfolio is null)
return 3m;

var balance = Portfolio.CurrentValue;
if (balance <= 0m)
balance = Portfolio.BeginValue;

if (balance <= 0m)
return 3m;

var baseBalance = BaseBalance;
var balanceStep = BalanceStep;

if (balanceStep <= 0m)
return 3m;

var steps = (int)Math.Floor((double)((balance - baseBalance) / balanceStep));
if (steps < 0)
steps = 0;

var maxLots = 3m + 1.5m * steps;
return Math.Min(15m, maxLots);
}

private decimal GetBuffer()
{
var step = Security?.PriceStep;
if (step is not decimal priceStep || priceStep <= 0m)
priceStep = 1m;

return BreakoutBufferPoints * priceStep;
}

private void UpdateDayContext(ICandleMessage candle)
{
var day = candle.OpenTime.Date;
if (day == _currentDay && _hasCurrentDayOpen)
return;

_currentDay = day;
_currentDayOpen = candle.OpenPrice;
_hasCurrentDayOpen = true;

if (Position == 0)
{
_longStages = 0;
_shortStages = 0;
}
}

private void UpdatePreviousCandleLevels(ICandleMessage candle)
{
_previousCandleHigh = candle.HighPrice;
_previousCandleLow = candle.LowPrice;
_hasPreviousCandle = true;
}

private sealed class FractalTracker
{
private readonly int _windowSize;
private readonly CandleInfo[] _window;
private int _count;

public FractalTracker(int windowSize)
{
_windowSize = windowSize;
_window = new CandleInfo[_windowSize];
}

public decimal? LastUp { get; private set; }
public decimal? LastDown { get; private set; }

public void Update(ICandleMessage candle)
{
if (_count < _windowSize)
{
_window[_count++] = new CandleInfo(candle.HighPrice, candle.LowPrice);
if (_count == _windowSize)
Evaluate();
return;
}

for (var i = 0; i < _windowSize - 1; i++)
_window[i] = _window[i + 1];

_window[_windowSize - 1] = new CandleInfo(candle.HighPrice, candle.LowPrice);
Evaluate();
}

private void Evaluate()
{
if (_count < _windowSize)
return;

var center = _windowSize / 2;
var pivot = _window[center];
var isUp = true;
var isDown = true;

for (var i = 0; i < _windowSize; i++)
{
if (i == center)
continue;

var sample = _window[i];

if (i < center)
{
if (!(pivot.High > sample.High))
isUp = false;

if (!(pivot.Low < sample.Low))
isDown = false;
}
else
{
if (!(pivot.High >= sample.High))
isUp = false;

if (!(pivot.Low <= sample.Low))
isDown = false;
}

if (!isUp && !isDown)
break;
}

if (isUp)
{
LastUp = LastUp is decimal up && up > pivot.High ? up : pivot.High;
}
else if (isDown)
{
LastDown = LastDown is decimal down && down < pivot.Low ? down : pivot.Low;
}
}

private readonly struct CandleInfo
{
public CandleInfo(decimal high, decimal low)
{
High = high;
Low = low;
}

public decimal High { get; }
public decimal Low { get; }
}
}

private readonly struct StageVolumeSet
{
public StageVolumeSet(decimal stage1, decimal stage2, decimal stage3, decimal stage4, decimal stage5)
{
Stage1 = stage1;
Stage2 = stage2;
Stage3 = stage3;
Stage4 = stage4;
Stage5 = stage5;
}

public decimal Stage1 { get; }
public decimal Stage2 { get; }
public decimal Stage3 { get; }
public decimal Stage4 { get; }
public decimal Stage5 { get; }

public static StageVolumeSet Create(decimal maxLots)
{
if (maxLots < 2m)
{
return new StageVolumeSet(0.1m, 0.5m, 0.4m, 0.3m, 0.2m);
}

if (maxLots < 4m)
{
return new StageVolumeSet(0.2m, 1.0m, 0.8m, 0.6m, 0.4m);
}

if (maxLots < 5m)
{
return new StageVolumeSet(0.3m, 1.5m, 1.2m, 0.9m, 0.6m);
}

if (maxLots < 7m)
{
return new StageVolumeSet(0.4m, 2.0m, 1.6m, 1.2m, 0.8m);
}

if (maxLots < 8m)
{
return new StageVolumeSet(0.5m, 2.5m, 2.0m, 1.5m, 1.0m);
}

if (maxLots < 10m)
{
return new StageVolumeSet(0.6m, 3.0m, 2.4m, 1.8m, 1.2m);
}

if (maxLots < 11m)
{
return new StageVolumeSet(0.7m, 3.5m, 2.8m, 2.1m, 1.4m);
}

if (maxLots < 13m)
{
return new StageVolumeSet(0.8m, 4.0m, 3.2m, 2.4m, 1.6m);
}

if (maxLots < 14m)
{
return new StageVolumeSet(0.9m, 4.5m, 3.6m, 2.7m, 1.8m);
}

return new StageVolumeSet(1.0m, 5.0m, 4.0m, 3.0m, 2.0m);
}
}
}

