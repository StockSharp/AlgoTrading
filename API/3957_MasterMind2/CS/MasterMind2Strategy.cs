using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MasterMind 2 strategy converted from MQL4 implementation.
/// Uses Stochastic Oscillator and Williams %R to detect extreme conditions
/// and complements signals with stop management rules.
/// </summary>
public class MasterMind2Strategy : Strategy
{
private readonly StrategyParam<decimal> _tradeVolume;
private readonly StrategyParam<int> _stochasticPeriod;
private readonly StrategyParam<int> _stochasticK;
private readonly StrategyParam<int> _stochasticD;
private readonly StrategyParam<int> _williamsPeriod;
private readonly StrategyParam<decimal> _stopLossPoints;
private readonly StrategyParam<decimal> _takeProfitPoints;
private readonly StrategyParam<decimal> _trailingStopPoints;
private readonly StrategyParam<decimal> _trailingStepPoints;
private readonly StrategyParam<decimal> _breakEvenPoints;
private readonly StrategyParam<DataType> _candleType;

private decimal _entryPrice;
private decimal _stopPrice;
private decimal _takeProfitPrice;

/// <summary>
/// Trade volume in contracts.
/// </summary>
public decimal TradeVolume
{
get => _tradeVolume.Value;
set => _tradeVolume.Value = value;
}

/// <summary>
/// Period for the Stochastic Oscillator.
/// </summary>
public int StochasticPeriod
{
get => _stochasticPeriod.Value;
set => _stochasticPeriod.Value = value;
}

/// <summary>
/// Smoothing length for the %K line.
/// </summary>
public int StochasticK
{
get => _stochasticK.Value;
set => _stochasticK.Value = value;
}

/// <summary>
/// Smoothing length for the %D signal line.
/// </summary>
public int StochasticD
{
get => _stochasticD.Value;
set => _stochasticD.Value = value;
}

/// <summary>
/// Period for Williams %R.
/// </summary>
public int WilliamsPeriod
{
get => _williamsPeriod.Value;
set => _williamsPeriod.Value = value;
}

/// <summary>
/// Stop loss distance expressed in price points.
/// </summary>
public decimal StopLossPoints
{
get => _stopLossPoints.Value;
set => _stopLossPoints.Value = value;
}

/// <summary>
/// Take profit distance expressed in price points.
/// </summary>
public decimal TakeProfitPoints
{
get => _takeProfitPoints.Value;
set => _takeProfitPoints.Value = value;
}

/// <summary>
/// Trailing stop distance expressed in price points.
/// </summary>
public decimal TrailingStopPoints
{
get => _trailingStopPoints.Value;
set => _trailingStopPoints.Value = value;
}

/// <summary>
/// Minimum price improvement required to move the trailing stop.
/// </summary>
public decimal TrailingStepPoints
{
get => _trailingStepPoints.Value;
set => _trailingStepPoints.Value = value;
}

/// <summary>
/// Distance required to move the stop loss to break-even.
/// </summary>
public decimal BreakEvenPoints
{
get => _breakEvenPoints.Value;
set => _breakEvenPoints.Value = value;
}

/// <summary>
/// Candle type processed by the strategy.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initializes default parameters.
/// </summary>
public MasterMind2Strategy()
{
_tradeVolume = Param(nameof(TradeVolume), 0.1m)
.SetDisplay("Trade Volume", "Trade volume in contracts", "General");

_stochasticPeriod = Param(nameof(StochasticPeriod), 100)
.SetDisplay("Stochastic Period", "Period for the Stochastic Oscillator", "Indicators")
.SetCanOptimize(true);

_stochasticK = Param(nameof(StochasticK), 3)
.SetDisplay("Stochastic %K", "Smoothing length for %K", "Indicators")
.SetCanOptimize(true);

_stochasticD = Param(nameof(StochasticD), 3)
.SetDisplay("Stochastic %D", "Smoothing length for %D", "Indicators")
.SetCanOptimize(true);

_williamsPeriod = Param(nameof(WilliamsPeriod), 100)
.SetDisplay("Williams %R Period", "Lookback for Williams %R", "Indicators")
.SetCanOptimize(true);

_stopLossPoints = Param(nameof(StopLossPoints), 2000m)
.SetDisplay("Stop Loss", "Stop loss distance in price points", "Risk")
.SetCanOptimize(true);

_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m)
.SetDisplay("Take Profit", "Take profit distance in price points", "Risk")
.SetCanOptimize(true);

_trailingStopPoints = Param(nameof(TrailingStopPoints), 0m)
.SetDisplay("Trailing Stop", "Trailing stop distance in price points", "Risk")
.SetCanOptimize(true);

_trailingStepPoints = Param(nameof(TrailingStepPoints), 1m)
.SetDisplay("Trailing Step", "Minimum improvement to trail stop", "Risk")
.SetCanOptimize(true);

_breakEvenPoints = Param(nameof(BreakEvenPoints), 0m)
.SetDisplay("Break Even", "Distance to move stop to break-even", "Risk")
.SetCanOptimize(true);

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
.SetDisplay("Candle Type", "Candle type used for calculations", "General");
}

/// <inheritdoc />
public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
{
return [(Security, CandleType)];
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();
ResetStops();
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var stochastic = new StochasticOscillator
{
Length = StochasticPeriod,
KPeriod = StochasticK,
DPeriod = StochasticD
};

var williams = new WilliamsR
{
Length = WilliamsPeriod
};

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx(stochastic, williams, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, stochastic);
DrawIndicator(area, williams);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochasticValue, IIndicatorValue williamsValue)
{
// Only react to fully formed candles to mirror MQL logic.
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (!stochasticValue.IsFinal || !williamsValue.IsFinal)
return;

var stoch = (StochasticOscillatorValue)stochasticValue;
if (stoch.D is not decimal signal)
return;

var wpr = williamsValue.ToDecimal();

var step = Security?.PriceStep ?? 1m;
if (step <= 0m)
step = 1m;

ManageLongPosition(candle, step);
ManageShortPosition(candle, step);

// Generate entries only when no opposite position exists.
if (signal < 3m && wpr < -99.9m)
{
HandleBuySignal(candle, step);
}
else if (signal > 97m && wpr > -0.1m)
{
HandleSellSignal(candle, step);
}
}

private void ManageLongPosition(ICandleMessage candle, decimal step)
{
if (Position <= 0)
return;

// Move stop to entry once break-even condition is reached.
if (BreakEvenPoints > 0m && candle.ClosePrice - _entryPrice >= BreakEvenPoints * step &&
(_stopPrice == 0m || _stopPrice < _entryPrice))
{
_stopPrice = _entryPrice;
}

// Tighten trailing stop when price moves favorably.
if (TrailingStopPoints > 0m)
{
var candidateStop = candle.ClosePrice - TrailingStopPoints * step;
if (_stopPrice == 0m || candidateStop - _stopPrice >= TrailingStepPoints * step)
{
_stopPrice = candidateStop;
}
}

// Exit position when stop or target is triggered.
var stopHit = _stopPrice > 0m && candle.LowPrice <= _stopPrice;
var targetHit = _takeProfitPrice > 0m && candle.HighPrice >= _takeProfitPrice;
if (stopHit || targetHit)
{
SellMarket(Position);
ResetStops();
}
}

private void ManageShortPosition(ICandleMessage candle, decimal step)
{
if (Position >= 0)
return;

if (BreakEvenPoints > 0m && _entryPrice - candle.ClosePrice >= BreakEvenPoints * step &&
(_stopPrice == 0m || _stopPrice > _entryPrice))
{
_stopPrice = _entryPrice;
}

if (TrailingStopPoints > 0m)
{
var candidateStop = candle.ClosePrice + TrailingStopPoints * step;
if (_stopPrice == 0m || _stopPrice - candidateStop >= TrailingStepPoints * step)
{
_stopPrice = candidateStop;
}
}

var stopHit = _stopPrice > 0m && candle.HighPrice >= _stopPrice;
var targetHit = _takeProfitPrice > 0m && candle.LowPrice <= _takeProfitPrice;
if (stopHit || targetHit)
{
BuyMarket(Math.Abs(Position));
ResetStops();
}
}

private void HandleBuySignal(ICandleMessage candle, decimal step)
{
if (Position < 0)
{
// Close short before opening a long position.
BuyMarket(Math.Abs(Position));
ResetStops();
}

if (Position > 0 || TradeVolume <= 0m)
return;

BuyMarket(TradeVolume);
_entryPrice = candle.ClosePrice;
_stopPrice = StopLossPoints > 0m ? _entryPrice - StopLossPoints * step : 0m;
_takeProfitPrice = TakeProfitPoints > 0m ? _entryPrice + TakeProfitPoints * step : 0m;
}

private void HandleSellSignal(ICandleMessage candle, decimal step)
{
if (Position > 0)
{
// Close long before opening a short position.
SellMarket(Position);
ResetStops();
}

if (Position < 0 || TradeVolume <= 0m)
return;

SellMarket(TradeVolume);
_entryPrice = candle.ClosePrice;
_stopPrice = StopLossPoints > 0m ? _entryPrice + StopLossPoints * step : 0m;
_takeProfitPrice = TakeProfitPoints > 0m ? _entryPrice - TakeProfitPoints * step : 0m;
}

private void ResetStops()
{
_entryPrice = 0m;
_stopPrice = 0m;
_takeProfitPrice = 0m;
}
}
