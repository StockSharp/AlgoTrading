namespace StockSharp.Samples.Strategies;

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

using StockSharp.Algo;
using StockSharp.Algo.Candles;

public class Fibo1Strategy : Strategy
{
private readonly StrategyParam<bool> _useMoneyTakeProfit;
private readonly StrategyParam<decimal> _moneyTakeProfit;
private readonly StrategyParam<bool> _usePercentTakeProfit;
private readonly StrategyParam<decimal> _percentTakeProfit;
private readonly StrategyParam<bool> _enableMoneyTrailing;
private readonly StrategyParam<decimal> _moneyTrailTarget;
private readonly StrategyParam<decimal> _moneyTrailStop;
private readonly StrategyParam<bool> _useEquityStop;
private readonly StrategyParam<decimal> _equityRiskPercent;
private readonly StrategyParam<decimal> _tradeVolume;
private readonly StrategyParam<int> _fastMaPeriod;
private readonly StrategyParam<int> _slowMaPeriod;
private readonly StrategyParam<int> _momentumPeriod;
private readonly StrategyParam<decimal> _momentumBuyThreshold;
private readonly StrategyParam<decimal> _momentumSellThreshold;
private readonly StrategyParam<int> _macdFastPeriod;
private readonly StrategyParam<int> _macdSlowPeriod;
private readonly StrategyParam<int> _macdSignalPeriod;
private readonly StrategyParam<decimal> _takeProfitPips;
private readonly StrategyParam<decimal> _stopLossPips;
private readonly StrategyParam<decimal> _trailingActivationPips;
private readonly StrategyParam<decimal> _trailingDistancePips;
private readonly StrategyParam<bool> _useCandleTrailing;
private readonly StrategyParam<int> _candleTrailingLength;
private readonly StrategyParam<decimal> _candleTrailingOffsetPips;
private readonly StrategyParam<bool> _moveToBreakEven;
private readonly StrategyParam<decimal> _breakEvenActivationPips;
private readonly StrategyParam<decimal> _breakEvenOffsetPips;
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<DataType> _momentumCandleType;
private readonly StrategyParam<DataType> _macdCandleType;

private LinearWeightedMovingAverage _fastMa = null!;
private LinearWeightedMovingAverage _slowMa = null!;
private Momentum _momentumIndicator = null!;
private MovingAverageConvergenceDivergenceSignal _macdIndicator = null!;
private Lowest _lowestIndicator = null!;
private Highest _highestIndicator = null!;

private decimal? _momentum1;
private decimal? _momentum2;
private decimal? _momentum3;
private bool _momentumReady;

private decimal? _macdMain;
private decimal? _macdSignal;
private bool _macdReady;

private decimal?[] _highHistory = null!;
private decimal?[] _lowHistory = null!;

private decimal _pipSize;
private decimal _initialEquity;
private decimal _moneyTrailPeak;
private decimal _equityPeak;
private decimal? _longEntryPrice;
private decimal? _shortEntryPrice;
private decimal? _longStopPrice;
private decimal? _shortStopPrice;

public bool UseMoneyTakeProfit
{
get => _useMoneyTakeProfit.Value;
set => _useMoneyTakeProfit.Value = value;
}

public decimal MoneyTakeProfit
{
get => _moneyTakeProfit.Value;
set => _moneyTakeProfit.Value = value;
}

public bool UsePercentTakeProfit
{
get => _usePercentTakeProfit.Value;
set => _usePercentTakeProfit.Value = value;
}

public decimal PercentTakeProfit
{
get => _percentTakeProfit.Value;
set => _percentTakeProfit.Value = value;
}

public bool EnableMoneyTrailing
{
get => _enableMoneyTrailing.Value;
set => _enableMoneyTrailing.Value = value;
}

public decimal MoneyTrailTarget
{
get => _moneyTrailTarget.Value;
set => _moneyTrailTarget.Value = value;
}

public decimal MoneyTrailStop
{
get => _moneyTrailStop.Value;
set => _moneyTrailStop.Value = value;
}

public bool UseEquityStop
{
get => _useEquityStop.Value;
set => _useEquityStop.Value = value;
}

public decimal EquityRiskPercent
{
get => _equityRiskPercent.Value;
set => _equityRiskPercent.Value = value;
}

public decimal TradeVolume
{
get => _tradeVolume.Value;
set => _tradeVolume.Value = value;
}

public int FastMaPeriod
{
get => _fastMaPeriod.Value;
set => _fastMaPeriod.Value = value;
}

public int SlowMaPeriod
{
get => _slowMaPeriod.Value;
set => _slowMaPeriod.Value = value;
}

public int MomentumPeriod
{
get => _momentumPeriod.Value;
set => _momentumPeriod.Value = value;
}

public decimal MomentumBuyThreshold
{
get => _momentumBuyThreshold.Value;
set => _momentumBuyThreshold.Value = value;
}

public decimal MomentumSellThreshold
{
get => _momentumSellThreshold.Value;
set => _momentumSellThreshold.Value = value;
}

public int MacdFastPeriod
{
get => _macdFastPeriod.Value;
set => _macdFastPeriod.Value = value;
}

public int MacdSlowPeriod
{
get => _macdSlowPeriod.Value;
set => _macdSlowPeriod.Value = value;
}

public int MacdSignalPeriod
{
get => _macdSignalPeriod.Value;
set => _macdSignalPeriod.Value = value;
}

public decimal TakeProfitPips
{
get => _takeProfitPips.Value;
set => _takeProfitPips.Value = value;
}

public decimal StopLossPips
{
get => _stopLossPips.Value;
set => _stopLossPips.Value = value;
}

public decimal TrailingActivationPips
{
get => _trailingActivationPips.Value;
set => _trailingActivationPips.Value = value;
}

public decimal TrailingDistancePips
{
get => _trailingDistancePips.Value;
set => _trailingDistancePips.Value = value;
}

public bool UseCandleTrailing
{
get => _useCandleTrailing.Value;
set => _useCandleTrailing.Value = value;
}

public int CandleTrailingLength
{
get => _candleTrailingLength.Value;
set => _candleTrailingLength.Value = value;
}

public decimal CandleTrailingOffsetPips
{
get => _candleTrailingOffsetPips.Value;
set => _candleTrailingOffsetPips.Value = value;
}

public bool MoveToBreakEven
{
get => _moveToBreakEven.Value;
set => _moveToBreakEven.Value = value;
}

public decimal BreakEvenActivationPips
{
get => _breakEvenActivationPips.Value;
set => _breakEvenActivationPips.Value = value;
}

public decimal BreakEvenOffsetPips
{
get => _breakEvenOffsetPips.Value;
set => _breakEvenOffsetPips.Value = value;
}

public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

public DataType MomentumCandleType
{
get => _momentumCandleType.Value;
set => _momentumCandleType.Value = value;
}

public DataType MacdCandleType
{
get => _macdCandleType.Value;
set => _macdCandleType.Value = value;
}

public Fibo1Strategy()
{
_useMoneyTakeProfit = Param(nameof(UseMoneyTakeProfit), false)
.SetDisplay("Use Money Take Profit", "Close all positions when unrealized profit reaches MoneyTakeProfit", "Risk");

_moneyTakeProfit = Param(nameof(MoneyTakeProfit), 10m)
.SetDisplay("Money Take Profit", "Target profit in account currency", "Risk")
.SetNotNegative();

_usePercentTakeProfit = Param(nameof(UsePercentTakeProfit), false)
.SetDisplay("Use Percent Take Profit", "Close positions once unrealized profit reaches PercentTakeProfit of initial equity", "Risk");

_percentTakeProfit = Param(nameof(PercentTakeProfit), 10m)
.SetDisplay("Percent Take Profit", "Profit target expressed as a percentage of initial equity", "Risk")
.SetNotNegative();

_enableMoneyTrailing = Param(nameof(EnableMoneyTrailing), true)
.SetDisplay("Enable Money Trailing", "Activate trailing based on floating profit", "Risk");

_moneyTrailTarget = Param(nameof(MoneyTrailTarget), 40m)
.SetDisplay("Money Trail Target", "Unrealized profit that enables money trailing", "Risk")
.SetNotNegative();

_moneyTrailStop = Param(nameof(MoneyTrailStop), 10m)
.SetDisplay("Money Trail Stop", "Maximum give back once money trailing is active", "Risk")
.SetNotNegative();

_useEquityStop = Param(nameof(UseEquityStop), true)
.SetDisplay("Use Equity Stop", "Close all trades when equity drawdown exceeds EquityRiskPercent", "Risk");

_equityRiskPercent = Param(nameof(EquityRiskPercent), 1m)
.SetDisplay("Equity Risk Percent", "Maximum allowed drawdown of peak equity", "Risk")
.SetNotNegative();

_tradeVolume = Param(nameof(TradeVolume), 1m)
.SetDisplay("Trade Volume", "Base volume for market orders", "Trading")
.SetCanOptimize(true)
.SetNotNegative();

_fastMaPeriod = Param(nameof(FastMaPeriod), 20)
.SetDisplay("Fast LWMA", "Period of the fast linear weighted moving average", "Indicators")
.SetCanOptimize(true);

_slowMaPeriod = Param(nameof(SlowMaPeriod), 100)
.SetDisplay("Slow LWMA", "Period of the slow linear weighted moving average", "Indicators")
.SetCanOptimize(true);

_momentumPeriod = Param(nameof(MomentumPeriod), 14)
.SetDisplay("Momentum Period", "Length of the momentum indicator", "Indicators")
.SetCanOptimize(true);

_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
.SetDisplay("Momentum Buy Threshold", "Minimum deviation from 100 required for long signals", "Indicators")
.SetCanOptimize(true)
.SetNotNegative();

_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
.SetDisplay("Momentum Sell Threshold", "Minimum deviation from 100 required for short signals", "Indicators")
.SetCanOptimize(true)
.SetNotNegative();

_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
.SetDisplay("MACD Fast Period", "Short moving average inside MACD", "Indicators")
.SetCanOptimize(true);

_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
.SetDisplay("MACD Slow Period", "Long moving average inside MACD", "Indicators")
.SetCanOptimize(true);

_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
.SetDisplay("MACD Signal Period", "Signal moving average inside MACD", "Indicators")
.SetCanOptimize(true);

_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
.SetDisplay("Take Profit (pips)", "Distance of the protective take profit", "Risk")
.SetNotNegative();

_stopLossPips = Param(nameof(StopLossPips), 20m)
.SetDisplay("Stop Loss (pips)", "Distance of the protective stop loss", "Risk")
.SetNotNegative();

_trailingActivationPips = Param(nameof(TrailingActivationPips), 40m)
.SetDisplay("Trailing Activation (pips)", "Profit required before price-based trailing starts", "Risk")
.SetNotNegative();

_trailingDistancePips = Param(nameof(TrailingDistancePips), 40m)
.SetDisplay("Trailing Distance (pips)", "Distance maintained when price-based trailing is active", "Risk")
.SetNotNegative();

_useCandleTrailing = Param(nameof(UseCandleTrailing), true)
.SetDisplay("Use Candle Trailing", "Trail stops at the extremes of the recent candles", "Risk");

_candleTrailingLength = Param(nameof(CandleTrailingLength), 3)
.SetDisplay("Candle Trailing Length", "Number of finished candles used for trailing", "Risk")
.SetCanOptimize(true)
.SetNotNegative();

_candleTrailingOffsetPips = Param(nameof(CandleTrailingOffsetPips), 3m)
.SetDisplay("Candle Trailing Offset (pips)", "Extra buffer added beyond the candle extreme", "Risk")
.SetNotNegative();

_moveToBreakEven = Param(nameof(MoveToBreakEven), true)
.SetDisplay("Move To Break Even", "Automatically protect the entry after a minimum profit", "Risk");

_breakEvenActivationPips = Param(nameof(BreakEvenActivationPips), 30m)
.SetDisplay("Break Even Activation (pips)", "Profit required to move the stop to break-even", "Risk")
.SetNotNegative();

_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 30m)
.SetDisplay("Break Even Offset (pips)", "Offset applied when moving the stop to break-even", "Risk")
.SetNotNegative();

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
.SetDisplay("Signal Candle", "Primary candle series used by the strategy", "Data")
.SetCanOptimize(true);

_momentumCandleType = Param(nameof(MomentumCandleType), TimeSpan.FromMinutes(15).TimeFrame())
.SetDisplay("Momentum Candle", "Candle series for the momentum filter", "Data");

_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(1).TimeFrame())
.SetDisplay("MACD Candle", "Higher timeframe used by the MACD filter", "Data");
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();

_momentum1 = null;
_momentum2 = null;
_momentum3 = null;
_momentumReady = false;
_macdMain = null;
_macdSignal = null;
_macdReady = false;

_highHistory = new decimal?[3];
_lowHistory = new decimal?[3];

_pipSize = 0m;
_initialEquity = 0m;
_moneyTrailPeak = 0m;
_equityPeak = 0m;
_longEntryPrice = null;
_shortEntryPrice = null;
_longStopPrice = null;
_shortStopPrice = null;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

Volume = TradeVolume;
_pipSize = CalculatePipSize();
_initialEquity = GetPortfolioValue();
_equityPeak = _initialEquity;
_moneyTrailPeak = 0m;

_fastMa = new LinearWeightedMovingAverage { Length = FastMaPeriod, CandlePrice = CandlePrice.Typical };
_slowMa = new LinearWeightedMovingAverage { Length = SlowMaPeriod, CandlePrice = CandlePrice.Typical };
_momentumIndicator = new Momentum { Length = MomentumPeriod };
_macdIndicator = new MovingAverageConvergenceDivergenceSignal
{
ShortPeriod = MacdFastPeriod,
LongPeriod = MacdSlowPeriod,
SignalPeriod = MacdSignalPeriod
};
_lowestIndicator = new Lowest { Length = Math.Max(1, CandleTrailingLength) };
_highestIndicator = new Highest { Length = Math.Max(1, CandleTrailingLength) };

var mainSubscription = SubscribeCandles(CandleType);
mainSubscription
.Bind(_fastMa, _slowMa, _lowestIndicator, _highestIndicator, ProcessMainCandle)
.Start();

var momentumSubscription = SubscribeCandles(MomentumCandleType);
momentumSubscription
.Bind(_momentumIndicator, ProcessMomentum)
.Start();

var macdSubscription = SubscribeCandles(MacdCandleType);
macdSubscription
.Bind(_macdIndicator, ProcessMacd)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, mainSubscription);
DrawIndicator(area, _fastMa);
DrawIndicator(area, _slowMa);
DrawIndicator(area, _momentumIndicator);
DrawIndicator(area, _macdIndicator);
DrawOwnTrades(area);
}

StartProtection();
}

/// <inheritdoc />
protected override void OnPositionChanged(decimal delta)
{
base.OnPositionChanged(delta);

if (Position == 0m)
{
_longEntryPrice = null;
_shortEntryPrice = null;
_longStopPrice = null;
_shortStopPrice = null;
_moneyTrailPeak = 0m;
return;
}

var entryPrice = PositionAvgPrice;
var stopDistance = ConvertPipsToPrice(StopLossPips);
var takeDistance = ConvertPipsToPrice(TakeProfitPips);

if (Position > 0m && delta > 0m)
{
_longEntryPrice = entryPrice;
_longStopPrice = stopDistance > 0m ? entryPrice - stopDistance : null;
if (stopDistance > 0m)
SetStopLoss(stopDistance, entryPrice, Position);
if (takeDistance > 0m)
SetTakeProfit(takeDistance, entryPrice, Position);
}
else if (Position < 0m && delta < 0m)
{
_shortEntryPrice = entryPrice;
_shortStopPrice = stopDistance > 0m ? entryPrice + stopDistance : null;
if (stopDistance > 0m)
SetStopLoss(stopDistance, entryPrice, Position);
if (takeDistance > 0m)
SetTakeProfit(takeDistance, entryPrice, Position);
}
}

private void ProcessMomentum(ICandleMessage candle, decimal momentumValue)
{
if (candle.State != CandleStates.Finished)
return;

// Store the latest three momentum readings so the filter can mimic the MQL checks.
_momentum3 = _momentum2;
_momentum2 = _momentum1;
_momentum1 = momentumValue;
_momentumReady = _momentumIndicator.IsFormed;
}

private void ProcessMacd(ICandleMessage candle, decimal macdValue, decimal signalValue)
{
if (candle.State != CandleStates.Finished)
return;

// Cache the MACD main and signal values coming from the higher timeframe stream.
_macdMain = macdValue;
_macdSignal = signalValue;
_macdReady = _macdIndicator.IsFormed;
}

private void ProcessMainCandle(ICandleMessage candle, decimal fastMaValue, decimal slowMaValue, decimal lowestValue, decimal highestValue)
{
if (candle.State != CandleStates.Finished)
return;

// Keep the recent highs and lows for the candle pattern filter.
UpdateHistory(_highHistory, candle.HighPrice);
UpdateHistory(_lowHistory, candle.LowPrice);

CheckEquityStop(candle);
ApplyMoneyTargets(candle);
UpdateTrailing(candle, lowestValue, highestValue);
ApplyBreakEven(candle);

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_momentumReady || !_macdReady)
return;

if (!HasHistory(_highHistory) || !HasHistory(_lowHistory))
return;

var macdMain = _macdMain;
var macdSignal = _macdSignal;
if (macdMain is null || macdSignal is null)
return;

var momentum1 = _momentum1;
var momentum2 = _momentum2;
var momentum3 = _momentum3;
if (momentum1 is null || momentum2 is null || momentum3 is null)
return;

var buyMomentum = Math.Max(Math.Max(CalculateMomentumDeviation(momentum1.Value), CalculateMomentumDeviation(momentum2.Value)), CalculateMomentumDeviation(momentum3.Value));
var sellMomentum = Math.Max(Math.Max(CalculateMomentumDeviation(momentum1.Value), CalculateMomentumDeviation(momentum2.Value)), CalculateMomentumDeviation(momentum3.Value));

var lowTwoAgo = _lowHistory[2]!.Value;
var highPrev = _highHistory[1]!.Value;
var highTwoAgo = _highHistory[2]!.Value;
var lowPrev = _lowHistory[1]!.Value;

var canOpenLong = Position <= 0m;
var canOpenShort = Position >= 0m;

if (canOpenLong && fastMaValue > slowMaValue && lowTwoAgo < highPrev && buyMomentum >= MomentumBuyThreshold && macdMain.Value > macdSignal.Value)
{
var volume = Volume + (Position < 0m ? Math.Abs(Position) : 0m);
if (volume > 0m)
{
// Reverse to long by covering an existing short before buying the base volume.
BuyMarket(volume);
}
}
else if (canOpenShort && fastMaValue < slowMaValue && lowPrev < highTwoAgo && sellMomentum >= MomentumSellThreshold && macdMain.Value < macdSignal.Value)
{
var volume = Volume + (Position > 0m ? Math.Abs(Position) : 0m);
if (volume > 0m)
{
// Reverse to short by flattening any long exposure before selling.
SellMarket(volume);
}
}
}

private void UpdateTrailing(ICandleMessage candle, decimal lowestValue, decimal highestValue)
{
var trailingDistance = ConvertPipsToPrice(TrailingDistancePips);
var trailingActivation = ConvertPipsToPrice(TrailingActivationPips);
var trailingOffset = ConvertPipsToPrice(CandleTrailingOffsetPips);

if (Position > 0m && _longEntryPrice is decimal longEntry)
{
var gain = candle.ClosePrice - longEntry;
if (UseCandleTrailing && _lowestIndicator.IsFormed)
{
var desiredStop = Math.Max(0m, lowestValue - trailingOffset);
if (desiredStop > 0m && desiredStop < candle.ClosePrice)
{
var distance = candle.ClosePrice - desiredStop;
if (_longStopPrice is null || desiredStop > _longStopPrice.Value)
{
// Track the trailing price that will be used when the next update arrives.
SetStopLoss(distance, candle.ClosePrice, Position);
_longStopPrice = desiredStop;
}
}
}
else if (trailingDistance > 0m && gain >= trailingActivation)
{
var desiredStop = candle.ClosePrice - trailingDistance;
if (_longStopPrice is null || desiredStop > _longStopPrice.Value)
{
// Apply the price distance trailing when the money-based trail is not active.
SetStopLoss(trailingDistance, candle.ClosePrice, Position);
_longStopPrice = desiredStop;
}
}
}
else if (Position < 0m && _shortEntryPrice is decimal shortEntry)
{
var gain = shortEntry - candle.ClosePrice;
if (UseCandleTrailing && _highestIndicator.IsFormed)
{
var desiredStop = highestValue + trailingOffset;
if (desiredStop > candle.ClosePrice)
{
var distance = desiredStop - candle.ClosePrice;
if (_shortStopPrice is null || desiredStop < _shortStopPrice.Value)
{
// Keep the protective stop above recent highs when trading short.
SetStopLoss(distance, candle.ClosePrice, Position);
_shortStopPrice = desiredStop;
}
}
}
else if (trailingDistance > 0m && gain >= trailingActivation)
{
var desiredStop = candle.ClosePrice + trailingDistance;
if (_shortStopPrice is null || desiredStop < _shortStopPrice.Value)
{
// Classic trailing for short positions using the configured distance.
SetStopLoss(trailingDistance, candle.ClosePrice, Position);
_shortStopPrice = desiredStop;
}
}
}
}

private void ApplyBreakEven(ICandleMessage candle)
{
if (!MoveToBreakEven)
return;

var breakEvenActivation = ConvertPipsToPrice(BreakEvenActivationPips);
var breakEvenOffset = ConvertPipsToPrice(BreakEvenOffsetPips);

if (breakEvenActivation <= 0m)
return;

if (Position > 0m && _longEntryPrice is decimal entry)
{
var gain = candle.ClosePrice - entry;
if (gain >= breakEvenActivation)
{
var desiredStop = entry + breakEvenOffset;
if (_longStopPrice is null || desiredStop > _longStopPrice.Value)
{
var distance = candle.ClosePrice - desiredStop;
if (distance > 0m)
{
// Lock in profits by moving the stop beyond the entry price.
SetStopLoss(distance, candle.ClosePrice, Position);
_longStopPrice = desiredStop;
}
}
}
}
else if (Position < 0m && _shortEntryPrice is decimal entryShort)
{
var gain = entryShort - candle.ClosePrice;
if (gain >= breakEvenActivation)
{
var desiredStop = entryShort - breakEvenOffset;
if (_shortStopPrice is null || desiredStop < _shortStopPrice.Value)
{
var distance = desiredStop - candle.ClosePrice;
if (distance > 0m)
{
// Protect shorts by pulling the stop below the entry anchor.
SetStopLoss(distance, candle.ClosePrice, Position);
_shortStopPrice = desiredStop;
}
}
}
}
}

private void ApplyMoneyTargets(ICandleMessage candle)
{
if (Position == 0m)
{
_moneyTrailPeak = 0m;
return;
}

var unrealized = GetUnrealizedPnL(candle);

if (UseMoneyTakeProfit && MoneyTakeProfit > 0m && unrealized >= MoneyTakeProfit)
{
ClosePosition();
return;
}

if (UsePercentTakeProfit && PercentTakeProfit > 0m && _initialEquity > 0m)
{
var target = _initialEquity * PercentTakeProfit / 100m;
if (unrealized >= target)
{
ClosePosition();
return;
}
}

if (EnableMoneyTrailing && MoneyTrailTarget > 0m && MoneyTrailStop > 0m)
{
if (unrealized >= MoneyTrailTarget)
{
_moneyTrailPeak = Math.Max(_moneyTrailPeak, unrealized);
if (_moneyTrailPeak - unrealized >= MoneyTrailStop)
ClosePosition();
}
else
{
_moneyTrailPeak = 0m;
}
}
}

private void CheckEquityStop(ICandleMessage candle)
{
if (!UseEquityStop || EquityRiskPercent <= 0m)
return;

var equity = GetPortfolioValue() + GetUnrealizedPnL(candle);
_equityPeak = Math.Max(_equityPeak, equity);

var drawdown = _equityPeak - equity;
var threshold = _equityPeak * EquityRiskPercent / 100m;

if (drawdown >= threshold && Position != 0)
ClosePosition();
}

private decimal CalculateMomentumDeviation(decimal momentum)
{
return Math.Abs(100m - momentum);
}

private static void UpdateHistory(decimal?[] history, decimal value)
{
for (var i = history.Length - 1; i > 0; i--)
{
history[i] = history[i - 1];
}

history[0] = value;
}

private static bool HasHistory(decimal?[] history)
{
for (var i = 0; i < history.Length; i++)
{
if (history[i] is null)
return false;
}

return true;
}

private decimal ConvertPipsToPrice(decimal pips)
{
if (pips <= 0m || _pipSize <= 0m)
return 0m;

return pips * _pipSize;
}

private decimal CalculatePipSize()
{
var priceStep = Security?.PriceStep ?? 0m;
if (priceStep <= 0m)
return 1m;

var decimals = GetDecimalPlaces(priceStep);
var factor = decimals == 3 || decimals == 5 ? 10m : 1m;
return priceStep * factor;
}

private static int GetDecimalPlaces(decimal value)
{
value = Math.Abs(value);
if (value == 0m)
return 0;

var bits = decimal.GetBits(value);
return (bits[3] >> 16) & 0xFF;
}

private decimal GetUnrealizedPnL(ICandleMessage candle)
{
if (Position == 0m)
return 0m;

var entry = PositionAvgPrice;
if (entry == 0m)
return 0m;

var diff = candle.ClosePrice - entry;
return diff * Position;
}

private decimal GetPortfolioValue()
{
var portfolio = Portfolio;
if (portfolio?.CurrentValue > 0m)
return portfolio.CurrentValue;

return 0m;
}
}

