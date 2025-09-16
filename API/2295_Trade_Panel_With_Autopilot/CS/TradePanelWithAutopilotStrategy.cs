using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on "Trade panel with autopilot" from MQL5.
/// It aggregates signals from multiple time frames and can trade automatically.
/// </summary>
public class TradePanelWithAutopilotStrategy : Strategy
{
// parameters
private readonly StrategyParam<bool> _autopilot;
private readonly StrategyParam<decimal> _openThreshold;
private readonly StrategyParam<decimal> _closeThreshold;
private readonly StrategyParam<decimal> _fixedVolume;
private readonly StrategyParam<decimal> _volumePercent;
private readonly StrategyParam<bool> _useFixedVolume;
private readonly StrategyParam<bool> _useStopLoss;

// time frames used for signal calculation
private readonly TimeSpan[] _timeFrames =
[
TimeSpan.FromMinutes(1),
TimeSpan.FromMinutes(2),
TimeSpan.FromMinutes(3),
TimeSpan.FromMinutes(4),
TimeSpan.FromMinutes(5),
TimeSpan.FromMinutes(6),
TimeSpan.FromMinutes(10),
TimeSpan.FromMinutes(12),
TimeSpan.FromMinutes(15),
TimeSpan.FromMinutes(20),
TimeSpan.FromMinutes(30),
TimeSpan.FromHours(1),
TimeSpan.FromHours(2),
TimeSpan.FromHours(3),
TimeSpan.FromHours(4),
TimeSpan.FromHours(6),
TimeSpan.FromHours(8),
TimeSpan.FromHours(12),
TimeSpan.FromDays(1),
TimeSpan.FromDays(7),
TimeSpan.FromDays(30)
];

private readonly Dictionary<TimeSpan, (ICandleMessage Prev, ICandleMessage Curr)> _candles = [];
private readonly Queue<decimal> _fractalsHigh = new();
private readonly Queue<decimal> _fractalsLow = new();
private decimal? _lastUpperFractal;
private decimal? _lastLowerFractal;

// public properties for parameters
public bool Autopilot { get => _autopilot.Value; set => _autopilot.Value = value; }
public decimal OpenThreshold { get => _openThreshold.Value; set => _openThreshold.Value = value; }
public decimal CloseThreshold { get => _closeThreshold.Value; set => _closeThreshold.Value = value; }
public decimal FixedVolume { get => _fixedVolume.Value; set => _fixedVolume.Value = value; }
public decimal VolumePercent { get => _volumePercent.Value; set => _volumePercent.Value = value; }
public bool UseFixedVolume { get => _useFixedVolume.Value; set => _useFixedVolume.Value = value; }
public bool UseStopLoss { get => _useStopLoss.Value; set => _useStopLoss.Value = value; }

public TradePanelWithAutopilotStrategy()
{
_autopilot = Param(nameof(Autopilot), false)
.SetDisplay("Autopilot", "Enable automated trading", "General");

_openThreshold = Param(nameof(OpenThreshold), 85m)
.SetDisplay("Open %", "Threshold for new position", "General");

_closeThreshold = Param(nameof(CloseThreshold), 55m)
.SetDisplay("Close %", "Threshold for closing", "General");

_fixedVolume = Param(nameof(FixedVolume), 0.01m)
.SetDisplay("Fixed Volume", "Absolute volume value", "Trading");

_volumePercent = Param(nameof(VolumePercent), 0.01m)
.SetDisplay("Volume %", "Portfolio percent used when volume is dynamic", "Trading");

_useFixedVolume = Param(nameof(UseFixedVolume), false)
.SetDisplay("Use Fixed Volume", "Use fixed volume instead of percentage", "Trading");

_useStopLoss = Param(nameof(UseStopLoss), false)
.SetDisplay("Use Stop Loss", "Enable fractal based stop loss", "General");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
foreach (var tf in _timeFrames)
yield return (Security, tf.TimeFrame());
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();
_candles.Clear();
_fractalsHigh.Clear();
_fractalsLow.Clear();
_lastUpperFractal = null;
_lastLowerFractal = null;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

foreach (var tf in _timeFrames)
{
var t = tf;
var subscription = SubscribeCandles(tf.TimeFrame());
subscription.Bind(c => ProcessCandle(t, c)).Start();
}
}

private void ProcessCandle(TimeSpan tf, ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

if (_candles.TryGetValue(tf, out var pair))
pair.Prev = pair.Curr;

pair.Curr = candle;
_candles[tf] = pair;

if (tf == TimeSpan.FromMinutes(10))
UpdateFractals(candle);

CalculateSignals(candle);
}

private void UpdateFractals(ICandleMessage candle)
{
_fractalsHigh.Enqueue(candle.HighPrice);
_fractalsLow.Enqueue(candle.LowPrice);

if (_fractalsHigh.Count > 5)
_fractalsHigh.Dequeue();
if (_fractalsLow.Count > 5)
_fractalsLow.Dequeue();

if (_fractalsHigh.Count == 5)
{
var arr = _fractalsHigh.ToArray();
if (arr[2] > arr[0] && arr[2] > arr[1] && arr[2] > arr[3] && arr[2] > arr[4])
_lastUpperFractal = arr[2];
}

if (_fractalsLow.Count == 5)
{
var arr = _fractalsLow.ToArray();
if (arr[2] < arr[0] && arr[2] < arr[1] && arr[2] < arr[3] && arr[2] < arr[4])
_lastLowerFractal = arr[2];
}
}

private void CalculateSignals(ICandleMessage candle)
{
int buy = 0;
int sell = 0;

foreach (var pair in _candles.Values)
{
if (pair.Prev == null || pair.Curr == null)
continue;

var prev = pair.Prev;
var curr = pair.Curr;

if (curr.OpenPrice > prev.OpenPrice) buy++; else sell++;
if (curr.HighPrice > prev.HighPrice) buy++; else sell++;
if (curr.LowPrice > prev.LowPrice) buy++; else sell++;

var hlCurr = (curr.HighPrice + curr.LowPrice) / 2m;
var hlPrev = (prev.HighPrice + prev.LowPrice) / 2m;
if (hlCurr > hlPrev) buy++; else sell++;

if (curr.ClosePrice > prev.ClosePrice) buy++; else sell++;

var hlcCurr = (curr.HighPrice + curr.LowPrice + curr.ClosePrice) / 3m;
var hlcPrev = (prev.HighPrice + prev.LowPrice + prev.ClosePrice) / 3m;
if (hlcCurr > hlcPrev) buy++; else sell++;

var hlccCurr = (curr.HighPrice + curr.LowPrice + 2m * curr.ClosePrice) / 4m;
var hlccPrev = (prev.HighPrice + prev.LowPrice + 2m * prev.ClosePrice) / 4m;
if (hlccCurr > hlccPrev) buy++; else sell++;
}

var total = buy + sell;
if (total == 0)
return;

var buyPct = (decimal)buy / total * 100m;
var sellPct = (decimal)sell / total * 100m;

if (!Autopilot || !IsFormedAndOnlineAndAllowTrading())
return;

var vol = UseFixedVolume ? FixedVolume :
Math.Max(0.01m, (Portfolio?.CurrentValue ?? 0m) * VolumePercent / 1000m);

if (Position > 0)
{
if (buyPct < CloseThreshold)
SellMarket(Position);
else if (UseStopLoss && _lastLowerFractal is decimal sl && candle.ClosePrice < sl)
SellMarket(Position);
}
else if (Position < 0)
