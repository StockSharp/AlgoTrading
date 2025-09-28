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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader "ROC" expert advisor that combines multi timeframe momentum,
/// weighted moving average trend filters and a custom rate of change model.
/// Replicates the original money management features such as break-even, pip trailing
/// and money based profit protection while using the high level StockSharp API.
/// </summary>
public class RocStrategy : Strategy
{
private static readonly DataType MonthlyCandleType = TimeSpan.FromDays(30).TimeFrame();

private readonly StrategyParam<bool> _useTpInMoney;
private readonly StrategyParam<decimal> _tpInMoney;
private readonly StrategyParam<bool> _useTpInPercent;
private readonly StrategyParam<decimal> _tpInPercent;
private readonly StrategyParam<bool> _enableMoneyTrailing;
private readonly StrategyParam<decimal> _takeProfitMoney;
private readonly StrategyParam<decimal> _stopLossMoney;
private readonly StrategyParam<int> _periodMa0;
private readonly StrategyParam<int> _periodMa1;
private readonly StrategyParam<int> _barsV;
private readonly StrategyParam<int> _averBars;
private readonly StrategyParam<decimal> _kCoefficient;
private readonly StrategyParam<bool> _exitStrategy;
private readonly StrategyParam<decimal> _lotSize;
private readonly StrategyParam<decimal> _increaseFactor;
private readonly StrategyParam<decimal> _stopLossSteps;
private readonly StrategyParam<decimal> _takeProfitSteps;
private readonly StrategyParam<int> _fastMaPeriod;
private readonly StrategyParam<int> _slowMaPeriod;
private readonly StrategyParam<decimal> _momentumSellThreshold;
private readonly StrategyParam<decimal> _momentumBuyThreshold;
private readonly StrategyParam<bool> _useEquityStop;
private readonly StrategyParam<decimal> _totalEquityRisk;
private readonly StrategyParam<int> _maxTrades;
private readonly StrategyParam<decimal> _trailingStopSteps;
private readonly StrategyParam<bool> _useBreakEven;
private readonly StrategyParam<decimal> _breakEvenTriggerSteps;
private readonly StrategyParam<decimal> _breakEvenOffsetSteps;

private WeightedMovingAverage _fastMa = null!;
private WeightedMovingAverage _slowMa = null!;
private WeightedMovingAverage _ma0 = null!;
private WeightedMovingAverage _ma1 = null!;
private WeightedMovingAverage _ma2 = null!;
private WeightedMovingAverage _ma3 = null!;
private WeightedMovingAverage _ma02 = null!;
private WeightedMovingAverage _ma03 = null!;
private Momentum _momentum = null!;
private MovingAverageConvergenceDivergence _monthlyMacd = null!;

private readonly List<decimal> _ma0History = new();
private readonly List<decimal> _ma1History = new();
private readonly List<decimal> _ma2History = new();
private readonly List<decimal> _ma3History = new();
private readonly List<decimal> _ma02History = new();
private readonly List<decimal> _ma03History = new();

private decimal? _momentumAbs1;
private decimal? _momentumAbs2;
private decimal? _momentumAbs3;
private decimal? _macdMain;
private decimal? _macdSignal;
private decimal _pipSize;
private decimal _stepPrice;
private decimal _initialCapital;
private decimal _equityPeak;
private decimal _maxFloatingProfit;
private decimal? _stopPrice;
private decimal? _takeProfitPrice;
private decimal? _breakevenPrice;
private decimal _highestPrice;
private decimal _lowestPrice;
private decimal _previousRealizedPnL;
private int _longScaleCount;
private int _shortScaleCount;
private int _lossStreak;
private DataType _momentumType = null!;
private int _k2;
private int _k3;

/// <summary>
/// Initializes default parameters that mirror the MetaTrader template.
/// </summary>
public RocStrategy()
{
_useTpInMoney = Param(nameof(UseTpInMoney), false)
.SetDisplay("Use money take profit", "Close all positions when floating profit reaches TpInMoney.", "Money management");

_tpInMoney = Param(nameof(TpInMoney), 10m)
.SetDisplay("Money take profit", "Floating profit target measured in account currency.", "Money management");

_useTpInPercent = Param(nameof(UseTpInPercent), false)
.SetDisplay("Use percent take profit", "Close all positions when floating profit reaches TpInPercent percent of equity.", "Money management");

_tpInPercent = Param(nameof(TpInPercent), 10m)
.SetDisplay("Percent take profit", "Floating profit target expressed as a percentage of initial equity.", "Money management");

_enableMoneyTrailing = Param(nameof(EnableMoneyTrailing), true)
.SetDisplay("Enable money trailing", "Activate trailing logic that protects profit in currency terms.", "Money management");

_takeProfitMoney = Param(nameof(TakeProfitMoney), 40m)
.SetDisplay("Money trailing trigger", "Floating profit required before money trailing becomes active.", "Money management");

_stopLossMoney = Param(nameof(StopLossMoney), 10m)
.SetDisplay("Money trailing stop", "Maximum drawdown of floating profit tolerated after the trigger fired.", "Money management");

_periodMa0 = Param(nameof(PeriodMa0), 31)
.SetDisplay("Base LWMA period", "Length of the base LWMA used in the ROC model.", "ROC");

_periodMa1 = Param(nameof(PeriodMa1), 6)
.SetDisplay("Reference LWMA period", "Length of the primary LWMA used by the ROC model.", "ROC");

_barsV = Param(nameof(BarsV), 45)
.SetDisplay("Rate window", "Number of bars used when calculating the ROC offsets.", "ROC");

_averBars = Param(nameof(AverBars), 30)
.SetDisplay("Averaging bars", "Number of composite ROC values averaged for smoothing.", "ROC");

_kCoefficient = Param(nameof(KCoefficient), 41m)
.SetDisplay("K coefficient", "Sensitivity multiplier applied to ROC differentials.", "ROC");

_exitStrategy = Param(nameof(ExitStrategy), false)
.SetDisplay("Force exit", "Immediately close the position when enabled.", "Risk");

_lotSize = Param(nameof(LotSize), 0.01m)
.SetDisplay("Lot size", "Base position volume opened on each signal.", "Position sizing");

_increaseFactor = Param(nameof(IncreaseFactor), 0m)
.SetDisplay("Increase factor", "Adjusts volume after consecutive losing trades (matches MetaTrader IncreaseFactor).", "Position sizing");

_stopLossSteps = Param(nameof(StopLossSteps), 100m)
.SetDisplay("Stop loss (points)", "Initial protective stop distance expressed in MetaTrader points.", "Risk");

_takeProfitSteps = Param(nameof(TakeProfitSteps), 100m)
.SetDisplay("Take profit (points)", "Initial take profit distance expressed in MetaTrader points.", "Risk");

_fastMaPeriod = Param(nameof(FastMaPeriod), 1)
.SetDisplay("Fast LWMA", "Length of the fast LWMA trend filter.", "Trend");

_slowMaPeriod = Param(nameof(SlowMaPeriod), 5)
.SetDisplay("Slow LWMA", "Length of the slow LWMA trend filter.", "Trend");

_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
.SetDisplay("Momentum sell threshold", "Minimum absolute deviation from 100 required for bearish momentum.", "Filters");

_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
.SetDisplay("Momentum buy threshold", "Minimum absolute deviation from 100 required for bullish momentum.", "Filters");

_useEquityStop = Param(nameof(UseEquityStop), true)
.SetDisplay("Use equity stop", "Abort all positions when floating drawdown exceeds TotalEquityRisk.", "Risk");

_totalEquityRisk = Param(nameof(TotalEquityRisk), 1m)
.SetDisplay("Equity risk %", "Maximum allowed drawdown as percentage of the equity peak.", "Risk");

_maxTrades = Param(nameof(MaxTrades), 5)
.SetDisplay("Max trades", "Maximum number of scale-in operations per direction.", "Position sizing");

_trailingStopSteps = Param(nameof(TrailingStopSteps), 50m)
.SetDisplay("Trailing stop (points)", "Classic MetaTrader trailing stop expressed in points.", "Risk");

_useBreakEven = Param(nameof(UseBreakEven), false)
.SetDisplay("Use break-even", "Move stop-loss to break-even after BreakEvenTriggerSteps of profit.", "Risk");

_breakEvenTriggerSteps = Param(nameof(BreakEvenTriggerSteps), 30m)
.SetDisplay("Break-even trigger", "Profit required to arm the break-even stop in points.", "Risk");

_breakEvenOffsetSteps = Param(nameof(BreakEvenOffsetSteps), 30m)
.SetDisplay("Break-even offset", "Extra distance added beyond entry when break-even activates.", "Risk");
}

/// <summary>
/// Use money based take profit.
/// </summary>
public bool UseTpInMoney
{
get => _useTpInMoney.Value;
set => _useTpInMoney.Value = value;
}

/// <summary>
/// Take profit measured in account currency.
/// </summary>
public decimal TpInMoney
{
get => _tpInMoney.Value;
set => _tpInMoney.Value = value;
}

/// <summary>
/// Use percent based take profit.
/// </summary>
public bool UseTpInPercent
{
get => _useTpInPercent.Value;
set => _useTpInPercent.Value = value;
}

/// <summary>
/// Percent based take profit.
/// </summary>
public decimal TpInPercent
{
get => _tpInPercent.Value;
set => _tpInPercent.Value = value;
}

/// <summary>
/// Enable trailing of floating profit in money terms.
/// </summary>
public bool EnableMoneyTrailing
{
get => _enableMoneyTrailing.Value;
set => _enableMoneyTrailing.Value = value;
}

/// <summary>
/// Profit required before money trailing is armed.
/// </summary>
public decimal TakeProfitMoney
{
get => _takeProfitMoney.Value;
set => _takeProfitMoney.Value = value;
}

/// <summary>
/// Allowed pullback of floating profit once money trailing is active.
/// </summary>
public decimal StopLossMoney
{
get => _stopLossMoney.Value;
set => _stopLossMoney.Value = value;
}

/// <summary>
/// LWMA length used as the base of the ROC computation.
/// </summary>
public int PeriodMa0
{
get => _periodMa0.Value;
set => _periodMa0.Value = value;
}

/// <summary>
/// Primary LWMA length used by the ROC computation.
/// </summary>
public int PeriodMa1
{
get => _periodMa1.Value;
set => _periodMa1.Value = value;
}

/// <summary>
/// Number of bars used to compute ROC offsets.
/// </summary>
public int BarsV
{
get => _barsV.Value;
set => _barsV.Value = value;
}

/// <summary>
/// Number of composite ROC values averaged for smoothing.
/// </summary>
public int AverBars
{
get => _averBars.Value;
set => _averBars.Value = value;
}

/// <summary>
/// Sensitivity multiplier for the ROC lines.
/// </summary>
public decimal KCoefficient
{
get => _kCoefficient.Value;
set => _kCoefficient.Value = value;
}

/// <summary>
/// Enable or disable the manual exit routine.
/// </summary>
public bool ExitStrategy
{
get => _exitStrategy.Value;
set => _exitStrategy.Value = value;
}

/// <summary>
/// Base trade volume.
/// </summary>
public decimal LotSize
{
get => _lotSize.Value;
set => _lotSize.Value = value;
}

/// <summary>
/// Factor applied when increasing volume after consecutive losses.
/// </summary>
public decimal IncreaseFactor
{
get => _increaseFactor.Value;
set => _increaseFactor.Value = value;
}

/// <summary>
/// Initial stop loss distance expressed in MetaTrader points.
/// </summary>
public decimal StopLossSteps
{
get => _stopLossSteps.Value;
set => _stopLossSteps.Value = value;
}

/// <summary>
/// Initial take profit distance expressed in MetaTrader points.
/// </summary>
public decimal TakeProfitSteps
{
get => _takeProfitSteps.Value;
set => _takeProfitSteps.Value = value;
}

/// <summary>
/// Fast LWMA period.
/// </summary>
public int FastMaPeriod
{
get => _fastMaPeriod.Value;
set => _fastMaPeriod.Value = value;
}

/// <summary>
/// Slow LWMA period.
/// </summary>
public int SlowMaPeriod
{
get => _slowMaPeriod.Value;
set => _slowMaPeriod.Value = value;
}

/// <summary>
/// Bearish momentum threshold.
/// </summary>
public decimal MomentumSellThreshold
{
get => _momentumSellThreshold.Value;
set => _momentumSellThreshold.Value = value;
}

/// <summary>
/// Bullish momentum threshold.
/// </summary>
public decimal MomentumBuyThreshold
{
get => _momentumBuyThreshold.Value;
set => _momentumBuyThreshold.Value = value;
}

/// <summary>
/// Enable equity based stop.
/// </summary>
public bool UseEquityStop
{
get => _useEquityStop.Value;
set => _useEquityStop.Value = value;
}

/// <summary>
/// Allowed drawdown in percent of the equity peak.
/// </summary>
public decimal TotalEquityRisk
{
get => _totalEquityRisk.Value;
set => _totalEquityRisk.Value = value;
}

/// <summary>
/// Maximum number of layered trades per direction.
/// </summary>
public int MaxTrades
{
get => _maxTrades.Value;
set => _maxTrades.Value = value;
}

/// <summary>
/// Trailing stop distance expressed in points.
/// </summary>
public decimal TrailingStopSteps
{
get => _trailingStopSteps.Value;
set => _trailingStopSteps.Value = value;
}

/// <summary>
/// Use break-even protection.
/// </summary>
public bool UseBreakEven
{
get => _useBreakEven.Value;
set => _useBreakEven.Value = value;
}

/// <summary>
/// Profit in points required to activate break-even.
/// </summary>
public decimal BreakEvenTriggerSteps
{
get => _breakEvenTriggerSteps.Value;
set => _breakEvenTriggerSteps.Value = value;
}

/// <summary>
/// Offset applied when moving the stop to break-even.
/// </summary>
public decimal BreakEvenOffsetSteps
{
get => _breakEvenOffsetSteps.Value;
set => _breakEvenOffsetSteps.Value = value;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

ConfigureTimeFrames();

_fastMa = new WeightedMovingAverage { Length = FastMaPeriod };
_slowMa = new WeightedMovingAverage { Length = SlowMaPeriod };
_ma0 = new WeightedMovingAverage { Length = PeriodMa0 };
_ma1 = new WeightedMovingAverage { Length = PeriodMa1 };
_ma2 = new WeightedMovingAverage { Length = Math.Max(1, _k2 * PeriodMa1) };
_ma3 = new WeightedMovingAverage { Length = Math.Max(1, _k3 * PeriodMa1) };
_ma02 = new WeightedMovingAverage { Length = Math.Max(1, _k2 * PeriodMa0) };
_ma03 = new WeightedMovingAverage { Length = Math.Max(1, _k3 * PeriodMa0) };
_momentum = new Momentum { Length = 14 };
_monthlyMacd = new MovingAverageConvergenceDivergence
{
FastLength = 12,
SlowLength = 26,
SignalLength = 9
};

_pipSize = GetPipSize();
_stepPrice = Security?.StepPrice ?? 0m;
_initialCapital = Portfolio?.BeginValue ?? Portfolio?.CurrentValue ?? 0m;
_equityPeak = _initialCapital;
_previousRealizedPnL = PnL;
_maxFloatingProfit = 0m;
_longScaleCount = Position > 0m ? 1 : 0;
_shortScaleCount = Position < 0m ? 1 : 0;
_lossStreak = 0;

var mainSubscription = SubscribeCandles(CandleType);
mainSubscription.Bind(ProcessMainCandle).Start();

var momentumSubscription = SubscribeCandles(_momentumType, allowBuildFromSmallerTimeFrame: true);
momentumSubscription.Bind(_momentum, ProcessMomentum).Start();

var macdSubscription = SubscribeCandles(MonthlyCandleType, allowBuildFromSmallerTimeFrame: true);
macdSubscription.BindEx(_monthlyMacd, ProcessMacd).Start();

StartProtection();
}

/// <inheritdoc />
protected override void OnPositionReceived(Position position)
{
base.OnPositionReceived(position);

if (Position == 0m)
{
var realized = PnL - _previousRealizedPnL;
if (realized < 0m)
_lossStreak++;
else if (realized > 0m)
_lossStreak = 0;

_previousRealizedPnL = PnL;
_stopPrice = null;
_takeProfitPrice = null;
_breakevenPrice = null;
_highestPrice = 0m;
_lowestPrice = 0m;
_maxFloatingProfit = 0m;
_longScaleCount = 0;
_shortScaleCount = 0;
return;
}

if (Position > 0m)
{
_shortScaleCount = 0;
if (PositionPrice is decimal entry)
{
_stopPrice = entry - StepsToPrice(StopLossSteps);
_takeProfitPrice = entry + StepsToPrice(TakeProfitSteps);
_highestPrice = entry;
}
}
else if (Position < 0m)
{
_longScaleCount = 0;
if (PositionPrice is decimal entry)
{
_stopPrice = entry + StepsToPrice(StopLossSteps);
_takeProfitPrice = entry - StepsToPrice(TakeProfitSteps);
_lowestPrice = entry;
}
}

_previousRealizedPnL = PnL;
}

private void ProcessMainCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

var typical = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
var fastValue = _fastMa.Process(new DecimalIndicatorValue(_fastMa, typical, candle.OpenTime)).ToDecimal();
var slowValue = _slowMa.Process(new DecimalIndicatorValue(_slowMa, typical, candle.OpenTime)).ToDecimal();
var ma0Value = _ma0.Process(new DecimalIndicatorValue(_ma0, typical, candle.OpenTime)).ToDecimal();
var ma1Value = _ma1.Process(new DecimalIndicatorValue(_ma1, typical, candle.OpenTime)).ToDecimal();
var ma2Value = _ma2.Process(new DecimalIndicatorValue(_ma2, typical, candle.OpenTime)).ToDecimal();
var ma3Value = _ma3.Process(new DecimalIndicatorValue(_ma3, typical, candle.OpenTime)).ToDecimal();
var ma02Value = _ma02.Process(new DecimalIndicatorValue(_ma02, typical, candle.OpenTime)).ToDecimal();
var ma03Value = _ma03.Process(new DecimalIndicatorValue(_ma03, typical, candle.OpenTime)).ToDecimal();

UpdateHistory(_ma0History, ma0Value);
UpdateHistory(_ma1History, ma1Value);
UpdateHistory(_ma2History, ma2Value);
UpdateHistory(_ma3History, ma3Value);
UpdateHistory(_ma02History, ma02Value);
UpdateHistory(_ma03History, ma03Value);

if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_ma0.IsFormed || !_ma1.IsFormed || !_ma2.IsFormed || !_ma3.IsFormed || !_ma02.IsFormed || !_ma03.IsFormed)
return;

UpdateExtremes(candle);

if (ExitStrategy)
{
ClosePosition();
return;
}

if (CheckPriceStops(candle))
return;

if (TryApplyEquityStop(candle.ClosePrice))
return;

TryActivateBreakeven(candle.ClosePrice);

if (TryApplyBreakEvenExit(candle.ClosePrice))
return;

ApplyTrailingStop(candle);

if (TryApplyMoneyTargets(candle.ClosePrice))
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (_momentumAbs1 is not decimal mom1 || _momentumAbs2 is not decimal mom2 || _momentumAbs3 is not decimal mom3)
return;

if (_macdMain is not decimal macdMain || _macdSignal is not decimal macdSignal)
return;

var rocTrend = CalculateRocTrend();
if (rocTrend is null)
return;

var buyMomentumOk = mom1 >= MomentumBuyThreshold || mom2 >= MomentumBuyThreshold || mom3 >= MomentumBuyThreshold;
var sellMomentumOk = mom1 >= MomentumSellThreshold || mom2 >= MomentumSellThreshold || mom3 >= MomentumSellThreshold;

if (rocTrend == 1 && fastValue > slowValue && buyMomentumOk && macdMain > macdSignal)
{
if (Position < 0m)
CloseShort(candle.ClosePrice);

EnterLong(candle.ClosePrice);
}
else if (rocTrend == 2 && fastValue < slowValue && sellMomentumOk && macdMain < macdSignal)
{
if (Position > 0m)
CloseLong(candle.ClosePrice);

EnterShort(candle.ClosePrice);
}
}

private void ProcessMomentum(ICandleMessage candle, decimal momentum)
{
if (candle.State != CandleStates.Finished)
return;

var distance = Math.Abs(momentum - 100m);
_momentumAbs3 = _momentumAbs2;
_momentumAbs2 = _momentumAbs1;
_momentumAbs1 = distance;
}

private void ProcessMacd(ICandleMessage candle, IIndicatorValue value)
{
if (!value.IsFinal)
return;

if (value is MovingAverageConvergenceDivergenceSignalValue macdSignalValue)
{
_macdMain = macdSignalValue.Macd;
_macdSignal = macdSignalValue.Signal;
}
else if (value is MovingAverageConvergenceDivergenceValue macdValue)
{
_macdMain = macdValue.Macd;
_macdSignal = macdValue.Signal;
}
}

private void EnterLong(decimal price)
{
if (_longScaleCount >= MaxTrades)
return;

var volume = CalculateNextVolume();
if (volume <= 0m)
return;

BuyMarket(volume);
_longScaleCount++;
_maxFloatingProfit = 0m;
_highestPrice = price;
}

private void EnterShort(decimal price)
{
if (_shortScaleCount >= MaxTrades)
return;

var volume = CalculateNextVolume();
if (volume <= 0m)
return;

SellMarket(volume);
_shortScaleCount++;
_maxFloatingProfit = 0m;
_lowestPrice = price;
}

private void CloseLong(decimal price)
{
if (Position <= 0m)
return;

SellMarket(Position);
_stopPrice = null;
_takeProfitPrice = null;
_breakevenPrice = null;
_maxFloatingProfit = 0m;
}

private void CloseShort(decimal price)
{
if (Position >= 0m)
return;

BuyMarket(-Position);
_stopPrice = null;
_takeProfitPrice = null;
_breakevenPrice = null;
_maxFloatingProfit = 0m;
}

private bool CheckPriceStops(ICandleMessage candle)
{
if (Position > 0m)
{
if (_stopPrice is decimal stop && candle.LowPrice <= stop)
{
CloseLong(stop);
return true;
}

if (_takeProfitPrice is decimal take && candle.HighPrice >= take)
{
CloseLong(take);
return true;
}
}
else if (Position < 0m)
{
if (_stopPrice is decimal stop && candle.HighPrice >= stop)
{
CloseShort(stop);
return true;
}

if (_takeProfitPrice is decimal take && candle.LowPrice <= take)
{
CloseShort(take);
return true;
}
}

return false;
}

private void TryActivateBreakeven(decimal closePrice)
{
if (!UseBreakEven || _breakevenPrice is decimal)
return;

if (Position == 0m || PositionPrice is not decimal entry)
return;

var triggerDistance = StepsToPrice(BreakEvenTriggerSteps);
var offset = StepsToPrice(BreakEvenOffsetSteps);

if (triggerDistance <= 0m)
return;

if (Position > 0m && closePrice - entry >= triggerDistance)
{
_breakevenPrice = entry + offset;
_stopPrice = _breakevenPrice;
}
else if (Position < 0m && entry - closePrice >= triggerDistance)
{
_breakevenPrice = entry - offset;
_stopPrice = _breakevenPrice;
}
}

private bool TryApplyBreakEvenExit(decimal closePrice)
{
if (_breakevenPrice is not decimal breakEven || Position == 0m)
return false;

if (Position > 0m && closePrice <= breakEven)
{
CloseLong(breakEven);
return true;
}

if (Position < 0m && closePrice >= breakEven)
{
CloseShort(breakEven);
return true;
}

return false;
}

private void ApplyTrailingStop(ICandleMessage candle)
{
if (TrailingStopSteps <= 0m || Position == 0m || PositionPrice is not decimal entry)
return;

var distance = StepsToPrice(TrailingStopSteps);
if (distance <= 0m)
return;

if (Position > 0m)
{
var newStop = candle.ClosePrice - distance;
if (newStop > entry - distance && (_stopPrice is not decimal current || newStop > current))
_stopPrice = newStop;
}
else if (Position < 0m)
{
var newStop = candle.ClosePrice + distance;
if (newStop < entry + distance && (_stopPrice is not decimal current || newStop < current))
_stopPrice = newStop;
}
}

private bool TryApplyMoneyTargets(decimal closePrice)
{
if (Position == 0m)
return false;

var profit = GetFloatingProfit(closePrice);
if (profit <= 0m && !EnableMoneyTrailing)
return false;

if (UseTpInMoney && profit >= TpInMoney && profit > 0m)
{
ClosePosition();
return true;
}

if (UseTpInPercent && _initialCapital > 0m)
{
var target = _initialCapital * TpInPercent / 100m;
if (profit >= target && profit > 0m)
{
ClosePosition();
return true;
}
}

if (EnableMoneyTrailing && profit > 0m)
{
if (profit >= TakeProfitMoney)
_maxFloatingProfit = Math.Max(_maxFloatingProfit, profit);

if (_maxFloatingProfit > 0m && _maxFloatingProfit - profit >= StopLossMoney)
{
ClosePosition();
return true;
}
}

return false;
}

private bool TryApplyEquityStop(decimal closePrice)
{
if (!UseEquityStop)
return false;

var profit = GetFloatingProfit(closePrice);
var realized = PnL;
var equity = _initialCapital + realized + profit;
_equityPeak = Math.Max(_equityPeak, equity);

if (profit >= 0m || _equityPeak <= 0m)
return false;

var threshold = _equityPeak * TotalEquityRisk / 100m;
if (Math.Abs(profit) >= threshold)
{
ClosePosition();
return true;
}

return false;
}

private void ClosePosition()
{
if (Position > 0m)
{
SellMarket(Position);
}
else if (Position < 0m)
{
BuyMarket(-Position);
}

_stopPrice = null;
_takeProfitPrice = null;
_breakevenPrice = null;
_maxFloatingProfit = 0m;
}

private void UpdateExtremes(ICandleMessage candle)
{
if (Position > 0m)
_highestPrice = Math.Max(_highestPrice, candle.HighPrice);
else if (Position < 0m)
_lowestPrice = _lowestPrice == 0m ? candle.LowPrice : Math.Min(_lowestPrice, candle.LowPrice);
}

private decimal CalculateNextVolume()
{
var volume = LotSize;

if (IncreaseFactor > 0m && _lossStreak > 1)
{
var equity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
if (equity > 0m)
volume = equity * IncreaseFactor / 1000m;
}

return NormalizeVolume(volume);
}

private decimal NormalizeVolume(decimal volume)
{
var min = Security?.MinVolume ?? 0m;
var max = Security?.MaxVolume ?? decimal.MaxValue;
var step = Security?.VolumeStep ?? 0m;

if (step > 0m)
{
var ratio = Math.Round(volume / step, MidpointRounding.AwayFromZero);
volume = ratio * step;
}

if (min > 0m && volume < min)
volume = min;

if (volume > max)
volume = max;

return volume;
}

private decimal StepsToPrice(decimal steps)
{
if (_pipSize <= 0m)
return 0m;

return steps * _pipSize;
}

private decimal GetPipSize()
{
var step = Security?.PriceStep ?? 0m;
if (step <= 0m)
return 1m;

if (step < 1m)
return step * 10m;

return step;
}

private decimal GetFloatingProfit(decimal price)
{
if (Position == 0m || PositionPrice is not decimal entry)
return 0m;

var priceStep = Security?.PriceStep ?? 0m;
if (priceStep <= 0m || _stepPrice <= 0m)
return 0m;

var direction = Position > 0m ? 1m : -1m;
var priceDiff = (price - entry) * direction;
var steps = priceDiff / priceStep;
return steps * _stepPrice * Math.Abs(Position);
}

private void ConfigureTimeFrames()
{
var minutes = GetTimeFrameMinutes(CandleType);
_k2 = 3;
_k3 = 6;
_momentumType = CandleType;

switch (minutes)
{
case 1:
_k2 = 5;
_k3 = 15;
_momentumType = TimeSpan.FromMinutes(15).TimeFrame();
break;
case 5:
_k2 = 3;
_k3 = 6;
_momentumType = TimeSpan.FromMinutes(30).TimeFrame();
break;
case 15:
_k2 = 2;
_k3 = 4;
_momentumType = TimeSpan.FromHours(1).TimeFrame();
break;
case 30:
_k2 = 2;
_k3 = 8;
_momentumType = TimeSpan.FromHours(4).TimeFrame();
break;
case 60:
_k2 = 4;
_k3 = 24;
_momentumType = TimeSpan.FromDays(1).TimeFrame();
break;
case 240:
_k2 = 6;
_k3 = 42;
_momentumType = TimeSpan.FromDays(7).TimeFrame();
break;
case 1440:
_k2 = 7;
_k3 = 30;
_momentumType = TimeSpan.FromDays(30).TimeFrame();
break;
case 10080:
_k2 = 4;
_k3 = 12;
_momentumType = TimeSpan.FromDays(30).TimeFrame();
break;
case 43200:
_k2 = 3;
_k3 = 12;
_momentumType = TimeSpan.FromDays(30).TimeFrame();
break;
}
}

private static int GetTimeFrameMinutes(DataType type)
{
if (type.MessageType != typeof(TimeFrameCandleMessage))
return 1;

var tf = ((TimeFrameCandleMessage)type.Message).TimeFrame;
return (int)Math.Max(1, tf.TotalMinutes);
}

private void UpdateHistory(List<decimal> history, decimal value)
{
var maxShift = Math.Max(Math.Max(_k2 * BarsV, _k3 * BarsV), BarsV);
var capacity = AverBars + maxShift + 5;

history.Add(value);
if (history.Count > capacity)
history.RemoveAt(0);
}

private int? CalculateRocTrend()
{
var sh1 = BarsV;
var sh2 = _k2 * sh1;
var sh3 = _k3 * sh1;
var iterations = AverBars - 1;
if (iterations <= 0)
return null;

if (!HasShift(_ma0History, sh3 + iterations) || !HasShift(_ma1History, sh3 + iterations) || !HasShift(_ma2History, sh3 + iterations) ||
!HasShift(_ma3History, sh3 + iterations) || !HasShift(_ma02History, sh3 + iterations) || !HasShift(_ma03History, sh3 + iterations))
return null;

decimal sum = 0m;
decimal line4 = 0m;

for (var i = 1; i <= iterations; i++)
{
if (!TryGetShift(_ma0History, i, out var ma0) ||
!TryGetShift(_ma1History, i, out var ma1Current) ||
!TryGetShift(_ma1History, i + sh1, out var ma1Past) ||
!TryGetShift(_ma2History, i, out var ma2Current) ||
!TryGetShift(_ma2History, i + sh2, out var ma2Past) ||
!TryGetShift(_ma02History, i, out var ma02) ||
!TryGetShift(_ma3History, i, out var ma3Current) ||
!TryGetShift(_ma3History, i + sh3, out var ma3Past) ||
!TryGetShift(_ma03History, i, out var ma03))
{
return null;
}

var line1 = ma0 + KCoefficient * (ma1Current - ma1Past);
var line2 = ma02 + KCoefficient * (ma2Current - ma2Past);
var line3 = ma03 + KCoefficient * (ma3Current - ma3Past);
line4 = (line1 + line2 + line3) / 3m;
sum += line4;
}

var line5 = sum / iterations;
if (line4 < line5)
return 1;

if (line4 > line5)
return 2;

return 0;
}

private static bool HasShift(List<decimal> history, int shift)
{
return history.Count > shift;
}

private static bool TryGetShift(List<decimal> history, int shift, out decimal value)
{
value = 0m;
var index = history.Count - 1 - shift;
if (index < 0 || index >= history.Count)
return false;

value = history[index];
return true;
}
}

