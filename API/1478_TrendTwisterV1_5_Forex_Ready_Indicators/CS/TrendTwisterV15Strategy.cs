using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// TrendTwister strategy combining HMA, shifted EMA, RSI and two Stochastic oscillators.
/// </summary>
public class TrendTwisterV15Strategy : Strategy
{
private readonly StrategyParam<int> _hmaLength;
private readonly StrategyParam<int> _emaLength;
private readonly StrategyParam<int> _rsiLength;
private readonly StrategyParam<decimal> _profitFactor;
private readonly StrategyParam<DataType> _candleType;

private HullMovingAverage _hma = null!;
private ExponentialMovingAverage _ema = null!;
private RelativeStrengthIndex _rsi = null!;
private StochasticOscillator _stoch1 = null!;
private StochasticOscillator _stoch2 = null!;

private decimal _emaPrev1;
private decimal _emaPrev2;
private decimal _prevHma;
private decimal _entryPrice;
private decimal _stopPrice;
private decimal _targetPrice;

/// <summary>
/// HMA length.
/// </summary>
public int HmaLength { get => _hmaLength.Value; set => _hmaLength.Value = value; }

/// <summary>
/// EMA length.
/// </summary>
public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }

/// <summary>
/// RSI length.
/// </summary>
public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

/// <summary>
/// Profit factor for take profit calculation.
/// </summary>
public decimal ProfitFactor { get => _profitFactor.Value; set => _profitFactor.Value = value; }

/// <summary>
/// Candle type.
/// </summary>
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

public TrendTwisterV15Strategy()
{
_hmaLength = Param(nameof(HmaLength), 12)
.SetGreaterThanZero()
.SetDisplay("HMA Length", "Period for Hull MA", "Indicators")
.SetCanOptimize(true);
_emaLength = Param(nameof(EmaLength), 5)
.SetGreaterThanZero()
.SetDisplay("EMA Length", "Period for EMA", "Indicators")
.SetCanOptimize(true);
_rsiLength = Param(nameof(RsiLength), 14)
.SetGreaterThanZero()
.SetDisplay("RSI Length", "Period for RSI", "Indicators")
.SetCanOptimize(true);
_profitFactor = Param(nameof(ProfitFactor), 1.65m)
.SetGreaterThanZero()
.SetDisplay("Profit Factor", "Multiplier for take profit", "Risk")
.SetCanOptimize(true);
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
.SetDisplay("Candle Type", "Timeframe", "General");
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
_hma = null!;
_ema = null!;
_rsi = null!;
_stoch1 = null!;
_stoch2 = null!;
_emaPrev1 = 0m;
_emaPrev2 = 0m;
_prevHma = 0m;
_entryPrice = 0m;
_stopPrice = 0m;
_targetPrice = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_hma = new HullMovingAverage { Length = HmaLength };
_ema = new ExponentialMovingAverage { Length = EmaLength };
_rsi = new RelativeStrengthIndex { Length = RsiLength };
_stoch1 = new StochasticOscillator { KPeriod = 12, DPeriod = 3, Smooth = 1 };
_stoch2 = new StochasticOscillator { KPeriod = 5, DPeriod = 3, Smooth = 1 };

var subscription = SubscribeCandles(CandleType);
subscription.BindEx(_hma, _ema, _rsi, _stoch1, _stoch2, ProcessCandle).Start();
}

private void ProcessCandle(ICandleMessage candle, decimal hmaValue, decimal emaValue, decimal rsiValue, IIndicatorValue st1Value, IIndicatorValue st2Value)
{
if (candle.State != CandleStates.Finished)
return;

var st1 = (StochasticOscillatorValue)st1Value;
var st2 = (StochasticOscillatorValue)st2Value;
var emaShifted = _emaPrev2;

var hmaCrossAbove = _prevHma <= _emaPrev2 && hmaValue > emaShifted;
var hmaCrossBelow = _prevHma >= _emaPrev2 && hmaValue < emaShifted;

var longCondition = candle.ClosePrice > hmaValue && candle.ClosePrice > emaShifted && rsiValue > 50m && st1.K > 50m && st2.K > 50m && hmaCrossAbove;
var shortCondition = candle.ClosePrice < hmaValue && candle.ClosePrice < emaShifted && rsiValue < 50m && st1.K < 50m && st2.K < 50m && hmaCrossBelow;

if (Position == 0)
{
if (longCondition)
{
BuyMarket();
_entryPrice = candle.ClosePrice;
_stopPrice = Math.Min(candle.LowPrice, _emaPrev1);
var slDistance = candle.ClosePrice - _stopPrice;
_targetPrice = candle.ClosePrice + slDistance * ProfitFactor;
}
else if (shortCondition)
{
SellMarket();
_entryPrice = candle.ClosePrice;
_stopPrice = Math.Max(candle.HighPrice, _emaPrev1);
var slDistance = _stopPrice - candle.ClosePrice;
_targetPrice = candle.ClosePrice - slDistance * ProfitFactor;
}
}
else if (Position > 0)
{
if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _targetPrice || shortCondition)
SellMarket(Position);
}
else if (Position < 0)
{
if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _targetPrice || longCondition)
BuyMarket(-Position);
}

_emaPrev2 = _emaPrev1;
_emaPrev1 = emaValue;
_prevHma = hmaValue;
}
}
