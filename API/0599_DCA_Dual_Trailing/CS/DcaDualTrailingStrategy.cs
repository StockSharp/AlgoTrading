using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dollar cost averaging strategy with dual trailing stops.
/// </summary>
public class DcaDualTrailingStrategy : Strategy
{
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<int> _fastEmaLength;
private readonly StrategyParam<int> _slowEmaLength;
private readonly StrategyParam<bool> _useDateFilter;
private readonly StrategyParam<DateTimeOffset> _startDate;
private readonly StrategyParam<bool> _useAtrSpacing;
private readonly StrategyParam<int> _atrLength;
private readonly StrategyParam<decimal> _atrSo1Multiplier;
private readonly StrategyParam<decimal> _atrSo2Multiplier;
private readonly StrategyParam<decimal> _fallbackSo1Percent;
private readonly StrategyParam<decimal> _fallbackSo2Percent;
private readonly StrategyParam<int> _cooldownBars;
private readonly StrategyParam<decimal> _baseUsd;
private readonly StrategyParam<decimal> _so1Usd;
private readonly StrategyParam<decimal> _so2Usd;
private readonly StrategyParam<decimal> _trailStopPercent;
private readonly StrategyParam<decimal> _lockInTriggerPercent;
private readonly StrategyParam<decimal> _lockInTrailPercent;

private ExponentialMovingAverage _fastEma;
private ExponentialMovingAverage _slowEma;
private AverageTrueRange _atr;

private decimal? _prevFast;
private decimal? _prevSlow;

private decimal? _baseEntryPrice;
private decimal? _highestSinceEntry;
private decimal? _trailStopPrice;
private bool _lockInTriggered;
private decimal? _lockInPeak;
private decimal? _lockInStopPrice;

private int _safetyOrdersFilled;
private int _cooldownCounter;

/// <summary>
/// Candle type for calculation.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Fast EMA length.
/// </summary>
public int FastEmaLength
{
get => _fastEmaLength.Value;
set => _fastEmaLength.Value = value;
}

/// <summary>
/// Slow EMA length.
/// </summary>
public int SlowEmaLength
{
get => _slowEmaLength.Value;
set => _slowEmaLength.Value = value;
}

/// <summary>
/// Use start date filter.
/// </summary>
public bool UseDateFilter
{
get => _useDateFilter.Value;
set => _useDateFilter.Value = value;
}

/// <summary>
/// Start date.
/// </summary>
public DateTimeOffset StartDate
{
get => _startDate.Value;
set => _startDate.Value = value;
}

/// <summary>
/// Use ATR for safety order spacing.
/// </summary>
public bool UseAtrSpacing
{
get => _useAtrSpacing.Value;
set => _useAtrSpacing.Value = value;
}

/// <summary>
/// ATR period.
/// </summary>
public int AtrLength
{
get => _atrLength.Value;
set => _atrLength.Value = value;
}

/// <summary>
/// ATR multiplier for first safety order.
/// </summary>
public decimal AtrSo1Multiplier
{
get => _atrSo1Multiplier.Value;
set => _atrSo1Multiplier.Value = value;
}

/// <summary>
/// ATR multiplier for second safety order.
/// </summary>
public decimal AtrSo2Multiplier
{
get => _atrSo2Multiplier.Value;
set => _atrSo2Multiplier.Value = value;
}

/// <summary>
/// Fallback percentage drop for first safety order.
/// </summary>
public decimal FallbackSo1Percent
{
get => _fallbackSo1Percent.Value;
set => _fallbackSo1Percent.Value = value;
}

/// <summary>
/// Fallback percentage drop for second safety order.
/// </summary>
public decimal FallbackSo2Percent
{
get => _fallbackSo2Percent.Value;
set => _fallbackSo2Percent.Value = value;
}

/// <summary>
/// Bars to wait after base entry.
/// </summary>
public int CooldownBars
{
get => _cooldownBars.Value;
set => _cooldownBars.Value = value;
}

/// <summary>
/// Base order size in USD.
/// </summary>
public decimal BaseUsd
{
get => _baseUsd.Value;
set => _baseUsd.Value = value;
}

/// <summary>
/// First safety order size in USD.
/// </summary>
public decimal So1Usd
{
get => _so1Usd.Value;
set => _so1Usd.Value = value;
}

/// <summary>
/// Second safety order size in USD.
/// </summary>
public decimal So2Usd
{
get => _so2Usd.Value;
set => _so2Usd.Value = value;
}

/// <summary>
/// Standard trailing stop percent.
/// </summary>
public decimal TrailStopPercent
{
get => _trailStopPercent.Value;
set => _trailStopPercent.Value = value;
}

/// <summary>
/// Profit threshold to enable lock-in trail.
/// </summary>
public decimal LockInTriggerPercent
{
get => _lockInTriggerPercent.Value;
set => _lockInTriggerPercent.Value = value;
}

/// <summary>
/// Lock-in trailing percent after trigger.
/// </summary>
public decimal LockInTrailPercent
{
get => _lockInTrailPercent.Value;
set => _lockInTrailPercent.Value = value;
}

/// <summary>
/// Constructor.
/// </summary>
public DcaDualTrailingStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");

_fastEmaLength = Param(nameof(FastEmaLength), 9)
.SetGreaterThanZero()
.SetDisplay("Fast EMA Length", "Fast EMA period", "Trend");

_slowEmaLength = Param(nameof(SlowEmaLength), 21)
.SetGreaterThanZero()
.SetDisplay("Slow EMA Length", "Slow EMA period", "Trend");

_useDateFilter = Param(nameof(UseDateFilter), true)
.SetDisplay("Use Date Filter", "Enable start date filter", "General");

_startDate = Param(nameof(StartDate), new DateTimeOffset(2025, 3, 11, 0, 0, 0, TimeSpan.Zero))
.SetDisplay("Start Date", "Start date filter", "General");

_useAtrSpacing = Param(nameof(UseAtrSpacing), true)
.SetDisplay("Use ATR Spacing", "Use ATR for safety orders", "Safety Orders");

_atrLength = Param(nameof(AtrLength), 14)
.SetGreaterThanZero()
.SetDisplay("ATR Length", "ATR period", "Safety Orders");

_atrSo1Multiplier = Param(nameof(AtrSo1Multiplier), 1.2m)
.SetGreaterThanZero()
.SetDisplay("ATR SO1 Multiplier", "ATR multiplier for first safety order", "Safety Orders");

_atrSo2Multiplier = Param(nameof(AtrSo2Multiplier), 2.5m)
.SetGreaterThanZero()
.SetDisplay("ATR SO2 Multiplier", "ATR multiplier for second safety order", "Safety Orders");

_fallbackSo1Percent = Param(nameof(FallbackSo1Percent), 0.04m)
.SetRange(0m, 1m)
.SetDisplay("Fallback SO1 %", "Price drop for first safety order", "Safety Orders");

_fallbackSo2Percent = Param(nameof(FallbackSo2Percent), 0.08m)
.SetRange(0m, 1m)
.SetDisplay("Fallback SO2 %", "Price drop for second safety order", "Safety Orders");

_cooldownBars = Param(nameof(CooldownBars), 4)
.SetDisplay("Cooldown Bars", "Bars to wait after base entry", "Entries");

_baseUsd = Param(nameof(BaseUsd), 1000m)
.SetGreaterThanZero()
.SetDisplay("Base USD", "Base order size in USD", "Entries");

_so1Usd = Param(nameof(So1Usd), 1250m)
.SetGreaterThanZero()
.SetDisplay("SO1 USD", "First safety order size in USD", "Entries");

_so2Usd = Param(nameof(So2Usd), 1750m)
.SetGreaterThanZero()
.SetDisplay("SO2 USD", "Second safety order size in USD", "Entries");

_trailStopPercent = Param(nameof(TrailStopPercent), 0.08m)
.SetRange(0m, 1m)
.SetDisplay("Trail Stop %", "Standard trailing stop percent", "Risk");

_lockInTriggerPercent = Param(nameof(LockInTriggerPercent), 0.025m)
.SetRange(0m, 1m)
.SetDisplay("Lock-In Trigger %", "Profit threshold for lock-in", "Risk");

_lockInTrailPercent = Param(nameof(LockInTrailPercent), 0.015m)
.SetRange(0m, 1m)
.SetDisplay("Lock-In Trail %", "Trailing percent after lock-in", "Risk");
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

_prevFast = null;
_prevSlow = null;
_baseEntryPrice = null;
_highestSinceEntry = null;
_trailStopPrice = null;
_lockInTriggered = false;
_lockInPeak = null;
_lockInStopPrice = null;
_safetyOrdersFilled = 0;
_cooldownCounter = 0;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_fastEma = new() { Length = FastEmaLength };
_slowEma = new() { Length = SlowEmaLength };
_atr = new() { Length = AtrLength };

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(_fastEma, _slowEma, _atr, ProcessCandle)
.Start();

StartProtection();
}

private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal atr)
{
if (candle.State != CandleStates.Finished)
return;

if (_cooldownCounter > 0)
_cooldownCounter--;

if (UseDateFilter && candle.OpenTime < StartDate)
return;

var close = candle.ClosePrice;

var trendUp = false;
var trendDown = false;

if (_prevFast is decimal pf && _prevSlow is decimal ps)
{
trendUp = pf <= ps && fast > slow;
trendDown = pf >= ps && fast < slow;
}

_prevFast = fast;
_prevSlow = slow;

if (Position == 0 && trendUp && _cooldownCounter == 0)
{
var qty = BaseUsd / close;
RegisterBuy(qty);

_baseEntryPrice = close;
_highestSinceEntry = close;
_trailStopPrice = null;
_lockInTriggered = false;
_lockInPeak = null;
_lockInStopPrice = null;
_safetyOrdersFilled = 0;
_cooldownCounter = CooldownBars;
return;
}

if (Position <= 0)
return;

if (_baseEntryPrice is decimal basePrice)
{
var so1Trigger = UseAtrSpacing ? basePrice - atr * AtrSo1Multiplier : basePrice * (1 - FallbackSo1Percent);
var so2Trigger = UseAtrSpacing ? basePrice - atr * AtrSo2Multiplier : basePrice * (1 - FallbackSo2Percent);

if (_safetyOrdersFilled == 0 && close <= so1Trigger)
{
var qty = So1Usd / close;
RegisterBuy(qty);
_safetyOrdersFilled = 1;
}
else if (_safetyOrdersFilled == 1 && close <= so2Trigger)
{
var qty = So2Usd / close;
RegisterBuy(qty);
_safetyOrdersFilled = 2;
}
}

_highestSinceEntry = _highestSinceEntry is null ? close : Math.Max(_highestSinceEntry.Value, close);
_trailStopPrice = _highestSinceEntry.Value * (1 - TrailStopPercent);
var stopHitNormal = close < _trailStopPrice;

var stopHitLockIn = false;

if (!_lockInTriggered && _baseEntryPrice is decimal price && close >= price * (1 + LockInTriggerPercent))
{
_lockInTriggered = true;
_lockInPeak = close;
}

if (_lockInTriggered)
{
_lockInPeak = Math.Max(_lockInPeak ?? close, close);
_lockInStopPrice = _lockInPeak.Value * (1 - LockInTrailPercent);
stopHitLockIn = close < _lockInStopPrice;
}

if ((stopHitNormal || stopHitLockIn || trendDown) && Position > 0)
{
RegisterSell(Position);
_baseEntryPrice = null;
_highestSinceEntry = null;
_trailStopPrice = null;
_lockInTriggered = false;
_lockInPeak = null;
_lockInStopPrice = null;
_safetyOrdersFilled = 0;
}
}
}
