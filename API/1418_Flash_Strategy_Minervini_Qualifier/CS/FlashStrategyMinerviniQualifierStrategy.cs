using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Flash strategy with Minervini stage analysis filter.
/// Combines EMA crossover, SuperTrend direction and momentum RSI.
/// </summary>
public class FlashStrategyMinerviniQualifierStrategy : Strategy
{
private readonly StrategyParam<int> _momRsiLength;
private readonly StrategyParam<decimal> _momRsiThreshold;
private readonly StrategyParam<int> _emaLength;
private readonly StrategyParam<decimal> _emaPercent;
private readonly StrategyParam<int> _superTrendPeriod;
private readonly StrategyParam<decimal> _superTrendMultiplier;
private readonly StrategyParam<DataType> _candleType;

private RelativeStrengthIndex _rsi = null!;
private ExponentialMovingAverage _emaTrail = null!;
private ExponentialMovingAverage _ema50 = null!;
private ExponentialMovingAverage _ema150 = null!;
private ExponentialMovingAverage _ema200 = null!;
private SuperTrend _superTrend = null!;

private decimal _prevClose;
private decimal? _prevTrail2;
private decimal _prevTrail1;

public int MomRsiLength { get => _momRsiLength.Value; set => _momRsiLength.Value = value; }
public decimal MomRsiThreshold { get => _momRsiThreshold.Value; set => _momRsiThreshold.Value = value; }
public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
public decimal EmaPercent { get => _emaPercent.Value; set => _emaPercent.Value = value; }
public int SuperTrendPeriod { get => _superTrendPeriod.Value; set => _superTrendPeriod.Value = value; }
public decimal SuperTrendMultiplier { get => _superTrendMultiplier.Value; set => _superTrendMultiplier.Value = value; }
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

public FlashStrategyMinerviniQualifierStrategy()
{
_momRsiLength = Param(nameof(MomRsiLength), 10)
.SetDisplay("Mom RSI Length", "Length for momentum RSI", "Parameters")
.SetGreaterThanZero();
_momRsiThreshold = Param(nameof(MomRsiThreshold), 60m)
.SetDisplay("Mom RSI Threshold", "Threshold for momentum RSI", "Parameters");
_emaLength = Param(nameof(EmaLength), 12)
.SetDisplay("EMA Length", "EMA period", "Parameters")
.SetGreaterThanZero();
_emaPercent = Param(nameof(EmaPercent), 0.01m)
.SetDisplay("EMA Percent", "Trailing percentage", "Parameters")
.SetGreaterThanZero();
_superTrendPeriod = Param(nameof(SuperTrendPeriod), 10)
.SetDisplay("SuperTrend Period", "ATR length for SuperTrend", "Parameters")
.SetGreaterThanZero();
_superTrendMultiplier = Param(nameof(SuperTrendMultiplier), 3m)
.SetDisplay("SuperTrend Multiplier", "ATR multiplier", "Parameters")
.SetGreaterThanZero();
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "Parameters");
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
_prevClose = 0m;
_prevTrail2 = null;
_prevTrail1 = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_rsi = new RelativeStrengthIndex { Length = MomRsiLength };
_emaTrail = new ExponentialMovingAverage { Length = EmaLength };
_ema50 = new ExponentialMovingAverage { Length = 50 };
_ema150 = new ExponentialMovingAverage { Length = 150 };
_ema200 = new ExponentialMovingAverage { Length = 200 };
_superTrend = new SuperTrend { Length = SuperTrendPeriod, Multiplier = SuperTrendMultiplier };

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx(_superTrend, _emaTrail, _ema50, _ema150, _ema200, ProcessCandle)
.Start();

StartProtection();
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue stVal, IIndicatorValue emaVal, IIndicatorValue ema50Val, IIndicatorValue ema150Val, IIndicatorValue ema200Val)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var st = (SuperTrendIndicatorValue)stVal;
decimal trail1 = emaVal.ToDecimal();
decimal ema50 = ema50Val.ToDecimal();
decimal ema150 = ema150Val.ToDecimal();
decimal ema200 = ema200Val.ToDecimal();

decimal mom = _prevClose == 0m ? 0m : candle.ClosePrice - _prevClose;
var rsiVal = _rsi.Process(mom);
if (!rsiVal.IsFinal)
return;
decimal rsi = rsiVal.ToDecimal();
_prevClose = candle.ClosePrice;

decimal trail2;
if (_prevTrail2 is null)
trail2 = trail1;
else
{
var sl = trail1 * EmaPercent;
var iff1 = trail1 > _prevTrail2.Value ? trail1 - sl : trail1 + sl;
var iff2 = trail1 < _prevTrail2.Value && _prevTrail1 < _prevTrail2.Value ? Math.Min(_prevTrail2.Value, trail1 + sl) : iff1;
trail2 = trail1 > _prevTrail2.Value && _prevTrail1 > _prevTrail2.Value ? Math.Max(_prevTrail2.Value, trail1 - sl) : iff2;
}
_prevTrail1 = trail1;
_prevTrail2 = trail2;

bool longCond = candle.ClosePrice > ema150 && ema50 > ema150 && ema150 > ema200;
bool shortCond = candle.ClosePrice < ema150 && ema50 < ema150 && ema150 < ema200;

bool buySignal = trail1 > trail2 && st.IsUpTrend && rsi > MomRsiThreshold;
bool sellSignal = trail1 < trail2 && !st.IsUpTrend && rsi > MomRsiThreshold;

if (Position <= 0 && longCond && buySignal)
BuyMarket();
else if (Position >= 0 && shortCond && sellSignal)
SellMarket();

if (Position > 0 && (trail1 < trail2 || !st.IsUpTrend))
SellMarket(Position);
else if (Position < 0 && (trail1 > trail2 || st.IsUpTrend))
BuyMarket(Math.Abs(Position));
}
}
