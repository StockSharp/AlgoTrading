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
/// Risk-based position sizing helper inspired by the NNFX manual trading panel.
/// The strategy offers manual commands to open long or short positions, split the entry into two halves,
/// and manage protective stop, take-profit, breakeven, and trailing logic derived from the original MQL implementation.
/// </summary>
public class NNFXAutoTradeStrategy : Strategy
{
private readonly StrategyParam<decimal> _riskPercent;
private readonly StrategyParam<decimal> _additionalCapital;
private readonly StrategyParam<bool> _useAdvancedTargets;
private readonly StrategyParam<decimal> _advancedStopPips;
private readonly StrategyParam<decimal> _advancedTakeProfitPips;
private readonly StrategyParam<bool> _usePreviousDailyAtr;
private readonly StrategyParam<int> _atrPeriod;
private readonly StrategyParam<decimal> _atrStopMultiplier;
private readonly StrategyParam<decimal> _atrTakeProfitMultiplier;
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<bool> _buyCommand;
private readonly StrategyParam<bool> _sellCommand;
private readonly StrategyParam<bool> _breakevenCommand;
private readonly StrategyParam<bool> _trailingCommand;
private readonly StrategyParam<bool> _closeAllCommand;

private decimal _previousPosition;
private decimal? _previousAtrValue;
private decimal _currentAtr;
private decimal _lastClose;
private decimal _lastHigh;
private decimal _lastLow;
private DateTimeOffset _lastCandleTime;

private decimal _longEntryPrice;
private decimal _shortEntryPrice;
private decimal _longStopDistance;
private decimal _shortStopDistance;
private decimal _longTargetDistance;
private decimal _shortTargetDistance;
private decimal _longStartMoveDistance;
private decimal _shortStartMoveDistance;
private decimal? _longStopPrice;
private decimal? _shortStopPrice;
private decimal? _longPartialTarget;
private decimal? _shortPartialTarget;
private decimal _longPartialVolume;
private decimal _shortPartialVolume;
private decimal _longRunnerVolume;
private decimal _shortRunnerVolume;
private decimal _longEntryAtr;
private decimal _shortEntryAtr;
private decimal _initialLongVolume;
private decimal _initialShortVolume;

public decimal RiskPercent
{
get => _riskPercent.Value;
set => _riskPercent.Value = value;
}

public decimal AdditionalCapital
{
get => _additionalCapital.Value;
set => _additionalCapital.Value = value;
}

public bool UseAdvancedTargets
{
get => _useAdvancedTargets.Value;
set => _useAdvancedTargets.Value = value;
}

public decimal AdvancedStopPips
{
get => _advancedStopPips.Value;
set => _advancedStopPips.Value = value;
}

public decimal AdvancedTakeProfitPips
{
get => _advancedTakeProfitPips.Value;
set => _advancedTakeProfitPips.Value = value;
}

public bool UsePreviousDailyAtr
{
get => _usePreviousDailyAtr.Value;
set => _usePreviousDailyAtr.Value = value;
}

public int AtrPeriod
{
get => _atrPeriod.Value;
set => _atrPeriod.Value = value;
}

public decimal AtrStopMultiplier
{
get => _atrStopMultiplier.Value;
set => _atrStopMultiplier.Value = value;
}

public decimal AtrTakeProfitMultiplier
{
get => _atrTakeProfitMultiplier.Value;
set => _atrTakeProfitMultiplier.Value = value;
}

public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

public bool BuyCommand
{
get => _buyCommand.Value;
set => _buyCommand.Value = value;
}

public bool SellCommand
{
get => _sellCommand.Value;
set => _sellCommand.Value = value;
}

public bool BreakevenCommand
{
get => _breakevenCommand.Value;
set => _breakevenCommand.Value = value;
}

public bool TrailingCommand
{
get => _trailingCommand.Value;
set => _trailingCommand.Value = value;
}

public bool CloseAllCommand
{
get => _closeAllCommand.Value;
set => _closeAllCommand.Value = value;
}

public NNFXAutoTradeStrategy()
{
_riskPercent = Param(nameof(RiskPercent), 2m)
.SetDisplay("Risk Percent", "Percentage of equity risked per trade", "Risk");
_additionalCapital = Param(nameof(AdditionalCapital), 0m)
.SetDisplay("Additional Capital", "External capital included in risk sizing", "Risk");
_useAdvancedTargets = Param(nameof(UseAdvancedTargets), false)
.SetDisplay("Use Advanced Targets", "Toggle manual stop/take distances instead of ATR multipliers", "Risk");
_advancedStopPips = Param(nameof(AdvancedStopPips), 0m)
.SetDisplay("Advanced Stop (pips)", "Manual stop distance in pips when advanced mode is enabled", "Risk");
_advancedTakeProfitPips = Param(nameof(AdvancedTakeProfitPips), 0m)
.SetDisplay("Advanced Take Profit (pips)", "Manual take-profit distance in pips when advanced mode is enabled", "Risk");
_usePreviousDailyAtr = Param(nameof(UsePreviousDailyAtr), true)
.SetDisplay("Use Previous Daily ATR", "Apply previous day's ATR during the first 12 hours", "ATR");
_atrPeriod = Param(nameof(AtrPeriod), 14)
.SetDisplay("ATR Period", "ATR lookback length", "ATR");
_atrStopMultiplier = Param(nameof(AtrStopMultiplier), 1.5m)
.SetDisplay("ATR Stop Multiplier", "Multiplier applied to ATR for stop distance", "ATR");
_atrTakeProfitMultiplier = Param(nameof(AtrTakeProfitMultiplier), 1m)
.SetDisplay("ATR Target Multiplier", "Multiplier applied to ATR for take-profit distance", "ATR");
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Candles used for ATR and manual management", "Data");
_buyCommand = Param(nameof(BuyCommand), false)
.SetDisplay("Buy", "Send the long entry command", "Manual");
_sellCommand = Param(nameof(SellCommand), false)
.SetDisplay("Sell", "Send the short entry command", "Manual");
_breakevenCommand = Param(nameof(BreakevenCommand), false)
.SetDisplay("Breakeven", "Move protective stop to the average entry price", "Manual");
_trailingCommand = Param(nameof(TrailingCommand), false)
.SetDisplay("Trailing", "Update trailing stop once using the original NNFX rules", "Manual");
_closeAllCommand = Param(nameof(CloseAllCommand), false)
.SetDisplay("Close All", "Flatten the position immediately", "Manual");
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();

_previousPosition = 0m;
_previousAtrValue = null;
_currentAtr = 0m;
_lastClose = 0m;
_lastHigh = 0m;
_lastLow = 0m;
_lastCandleTime = default;

ResetLongState();
ResetShortState();
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var atr = new AverageTrueRange
{
Length = AtrPeriod
};

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(atr, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, atr);
DrawOwnTrades(area);
}
}

/// <inheritdoc />
protected override void OnPositionReceived(Position position)
{
base.OnPositionReceived(position);

if (Position > 0m)
{
if (_previousPosition <= 0m)
{
_longEntryPrice = PositionPrice ?? _lastClose;
_longStopPrice = _longEntryPrice - _longStopDistance;
_longPartialTarget = _longTargetDistance > 0m ? _longEntryPrice + _longTargetDistance : null;
_initialLongVolume = Position;
if (_longPartialVolume <= 0m)
{
_longPartialVolume = NormalizeVolume(Position / 2m);
}
_longPartialVolume = Math.Min(_longPartialVolume, Position);
_longRunnerVolume = Math.Max(0m, Position - _longPartialVolume);
}
else
{
_longEntryPrice = PositionPrice ?? _longEntryPrice;
_initialLongVolume = Math.Max(_initialLongVolume, Position);
}
}
else if (Position < 0m)
{
if (_previousPosition >= 0m)
{
_shortEntryPrice = PositionPrice ?? _lastClose;
_shortStopPrice = _shortEntryPrice + _shortStopDistance;
_shortPartialTarget = _shortTargetDistance > 0m ? _shortEntryPrice - _shortTargetDistance : null;
_initialShortVolume = -Position;
if (_shortPartialVolume <= 0m)
{
_shortPartialVolume = NormalizeVolume(-Position / 2m);
}
_shortPartialVolume = Math.Min(_shortPartialVolume, -Position);
_shortRunnerVolume = Math.Max(0m, -Position - _shortPartialVolume);
}
else
{
_shortEntryPrice = PositionPrice ?? _shortEntryPrice;
_initialShortVolume = Math.Max(_initialShortVolume, -Position);
}
}
else
{
ResetLongState();
ResetShortState();
}

_previousPosition = Position;
}

private void ProcessCandle(ICandleMessage candle, decimal atrValue)
{
if (candle.State != CandleStates.Finished)
return;

_lastCandleTime = candle.OpenTime;
_lastClose = candle.ClosePrice;
_lastHigh = candle.HighPrice;
_lastLow = candle.LowPrice;

var atrToUse = atrValue;
if (UsePreviousDailyAtr && candle.OpenTime.TimeOfDay < TimeSpan.FromHours(12) && _previousAtrValue.HasValue)
{
atrToUse = _previousAtrValue.Value;
}

_currentAtr = atrToUse;
_previousAtrValue = atrValue;

HandleManualCommands();
ManageOpenPositions();
}

private void HandleManualCommands()
{
if (CloseAllCommand)
{
CloseAllImmediate();
CloseAllCommand = false;
}

if (BreakevenCommand)
{
MoveToBreakeven();
BreakevenCommand = false;
}

if (TrailingCommand)
{
ApplyTrailingOnce();
TrailingCommand = false;
}

if (BuyCommand)
{
TryEnterPosition(Sides.Buy);
BuyCommand = false;
}

if (SellCommand)
{
TryEnterPosition(Sides.Sell);
SellCommand = false;
}
}

private void ManageOpenPositions()
{
if (Position > 0m)
{
if (_longStopPrice.HasValue && _lastLow <= _longStopPrice.Value)
{
SellMarket(Position);
ResetLongState();
return;
}

if (_longPartialTarget.HasValue && _longPartialVolume > 0m && _lastHigh >= _longPartialTarget.Value)
{
var volume = Math.Min(_longPartialVolume, Position);
if (volume > 0m)
{
SellMarket(volume);
}
_longPartialVolume = 0m;
_longPartialTarget = null;
}
}
else if (Position < 0m)
{
if (_shortStopPrice.HasValue && _lastHigh >= _shortStopPrice.Value)
{
BuyMarket(-Position);
ResetShortState();
return;
}

if (_shortPartialTarget.HasValue && _shortPartialVolume > 0m && _lastLow <= _shortPartialTarget.Value)
{
var volume = Math.Min(_shortPartialVolume, -Position);
if (volume > 0m)
{
BuyMarket(volume);
}
_shortPartialVolume = 0m;
_shortPartialTarget = null;
}
}
}

private void TryEnterPosition(Sides side)
{
if (!IsFormedAndOnlineAndAllowTrading())
return;

if (side == Sides.Buy && Position > 0m)
return;

if (side == Sides.Sell && Position < 0m)
return;

var atr = _currentAtr;
if (atr <= 0m)
return;

var step = GetPriceStep();
if (step <= 0m)
return;

var stopDistance = GetStopDistance(step, atr);
if (stopDistance <= 0m)
return;

var targetDistance = GetTargetDistance(step, atr);
var startMove = GetStartMoveDistance(step, atr);

var volume = CalculateRiskVolume(stopDistance);
if (volume <= 0m)
return;

var partialVolume = NormalizeVolume(volume / 2m);
if (partialVolume <= 0m)
{
partialVolume = volume;
}
var runnerVolume = Math.Max(0m, volume - partialVolume);

if (side == Sides.Buy)
{
_longStopDistance = stopDistance;
_longTargetDistance = targetDistance;
_longStartMoveDistance = startMove;
_longPartialVolume = partialVolume;
_longRunnerVolume = runnerVolume;
_longEntryAtr = atr;
BuyMarket(volume);
}
else
{
_shortStopDistance = stopDistance;
_shortTargetDistance = targetDistance;
_shortStartMoveDistance = startMove;
_shortPartialVolume = partialVolume;
_shortRunnerVolume = runnerVolume;
_shortEntryAtr = atr;
SellMarket(volume);
}
}

private decimal CalculateRiskVolume(decimal stopDistance)
{
var security = Security;
var portfolio = Portfolio;
if (security == null || portfolio == null)
return 0m;

var equity = portfolio.CurrentValue + AdditionalCapital;
if (equity <= 0m)
return 0m;

var riskAmount = equity * RiskPercent / 100m;
if (riskAmount <= 0m)
return 0m;

var step = security.Step ?? 0m;
var stepPrice = security.StepPrice ?? 0m;
if (step <= 0m || stepPrice <= 0m)
return 0m;

var perUnitRisk = (stopDistance / step) * stepPrice;
if (perUnitRisk <= 0m)
return 0m;

var volume = riskAmount / perUnitRisk;
return NormalizeVolume(volume);
}

private decimal NormalizeVolume(decimal volume)
{
var security = Security;
if (security == null)
return 0m;

var step = security.VolumeStep ?? 1m;
if (step <= 0m)
step = 1m;

var steps = Math.Max(1m, Math.Floor(volume / step));
return steps * step;
}

private decimal GetPriceStep()
{
var security = Security;
if (security == null)
return 0m;

var step = security.Step ?? 0m;
if (step <= 0m)
{
step = security.PriceStep ?? 0m;
}

return step;
}

private decimal GetStopDistance(decimal step, decimal atr)
{
if (UseAdvancedTargets && AdvancedStopPips > 0m)
{
return AdvancedStopPips * step;
}

return atr * AtrStopMultiplier;
}

private decimal GetTargetDistance(decimal step, decimal atr)
{
if (UseAdvancedTargets && AdvancedTakeProfitPips > 0m)
{
return AdvancedTakeProfitPips * step;
}

return atr * AtrTakeProfitMultiplier;
}

private decimal GetStartMoveDistance(decimal step, decimal atr)
{
if (UseAdvancedTargets && AdvancedStopPips > 0m)
{
return AdvancedStopPips * step;
}

return atr * 2m;
}

private void MoveToBreakeven()
{
if (Position > 0m && _longEntryPrice > 0m)
{
_longStopPrice = _longEntryPrice;
LogInfo($"Long stop moved to breakeven at {_longStopPrice:F5}.");
}
else if (Position < 0m && _shortEntryPrice > 0m)
{
_shortStopPrice = _shortEntryPrice;
LogInfo($"Short stop moved to breakeven at {_shortStopPrice:F5}.");
}
}

private void ApplyTrailingOnce()
{
if (Position > 0m && _longEntryPrice > 0m)
{
var moved = _lastClose - _longEntryPrice;
if (moved > _longStartMoveDistance && _longEntryAtr > 0m)
{
var distance = UseAdvancedTargets && AdvancedStopPips > 0m
? AdvancedStopPips * GetPriceStep()
: _longEntryAtr * 2m;
var newStop = _lastClose - distance;
if (!_longStopPrice.HasValue || newStop > _longStopPrice.Value)
{
_longStopPrice = newStop;
LogInfo($"Trailing long stop updated to {_longStopPrice:F5}.");
}
}
}
else if (Position < 0m && _shortEntryPrice > 0m)
{
var moved = _shortEntryPrice - _lastClose;
if (moved > _shortStartMoveDistance && _shortEntryAtr > 0m)
{
var distance = UseAdvancedTargets && AdvancedStopPips > 0m
? AdvancedStopPips * GetPriceStep()
: _shortEntryAtr * 2m;
var newStop = _lastClose + distance;
if (!_shortStopPrice.HasValue || newStop < _shortStopPrice.Value)
{
_shortStopPrice = newStop;
LogInfo($"Trailing short stop updated to {_shortStopPrice:F5}.");
}
}
}
}

private void CloseAllImmediate()
{
CancelActiveOrders();

if (Position > 0m)
{
SellMarket(Position);
}
else if (Position < 0m)
{
BuyMarket(-Position);
}

ResetLongState();
ResetShortState();
}

private void ResetLongState()
{
_longEntryPrice = 0m;
_longStopDistance = 0m;
_longTargetDistance = 0m;
_longStartMoveDistance = 0m;
_longStopPrice = null;
_longPartialTarget = null;
_longPartialVolume = 0m;
_longRunnerVolume = 0m;
_longEntryAtr = 0m;
_initialLongVolume = 0m;
}

private void ResetShortState()
{
_shortEntryPrice = 0m;
_shortStopDistance = 0m;
_shortTargetDistance = 0m;
_shortStartMoveDistance = 0m;
_shortStopPrice = null;
_shortPartialTarget = null;
_shortPartialVolume = 0m;
_shortRunnerVolume = 0m;
_shortEntryAtr = 0m;
_initialShortVolume = 0m;
}
}

