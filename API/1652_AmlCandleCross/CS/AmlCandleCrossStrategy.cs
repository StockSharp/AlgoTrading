using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive Market Level candle cross strategy.
/// Opens position when AML value lies between candle open and close.
/// Reverses position if opposite condition occurs and <see cref="UseOpposite"/> is enabled.
/// </summary>
public class AmlCandleCrossStrategy : Strategy
{
private readonly StrategyParam<int> _fractal;
private readonly StrategyParam<int> _lag;
private readonly StrategyParam<int> _shift;
private readonly StrategyParam<bool> _useOpposite;
private readonly StrategyParam<DataType> _candleType;

/// <summary>
/// Fractal window size.
/// </summary>
public int Fractal { get => _fractal.Value; set => _fractal.Value = value; }

/// <summary>
/// Lag value for smoothing.
/// </summary>
public int Lag { get => _lag.Value; set => _lag.Value = value; }

/// <summary>
/// Horizontal shift for indicator.
/// </summary>
public int Shift { get => _shift.Value; set => _shift.Value = value; }

/// <summary>
/// Enable reversing position on opposite signals.
/// </summary>
public bool UseOpposite { get => _useOpposite.Value; set => _useOpposite.Value = value; }

/// <summary>
/// Candle type to subscribe.
/// </summary>
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

public AmlCandleCrossStrategy()
{
_fractal = Param(nameof(Fractal), 70).SetDisplay("Fractal").SetCanOptimize(true);
_lag = Param(nameof(Lag), 18).SetDisplay("Lag").SetCanOptimize(true);
_shift = Param(nameof(Shift), 0).SetDisplay("Shift").SetCanOptimize(true);
_useOpposite = Param(nameof(UseOpposite), true).SetDisplay("Use Opposite").SetCanOptimize(true);
_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type");
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var aml = new AdaptiveMarketLevel
{
Fractal = Fractal,
Lag = Lag,
Shift = Shift,
Step = Security?.PriceStep ?? 0.01m
};

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(aml, ProcessCandle)
.Start();
}

private void ProcessCandle(ICandleMessage candle, decimal amlValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var open = candle.OpenPrice;
var close = candle.ClosePrice;

if (Position == 0)
{
if (amlValue >= open && amlValue <= close && close > open)
{
BuyMarket();
}
else if (amlValue <= open && amlValue >= close && close < open)
{
SellMarket();
}
}
else if (UseOpposite)
{
if (Position > 0 && amlValue <= open && amlValue >= close && close < open)
{
SellMarket();
}
else if (Position < 0 && amlValue >= open && amlValue <= close && close > open)
{
BuyMarket();
}
}
}
}


/// <summary>
/// Adaptive Market Level indicator.
/// </summary>
public class AdaptiveMarketLevel : BaseIndicator
{
private int _pos;
private decimal[] _smooth = Array.Empty<decimal>();

private readonly List<decimal> _highs = new();
private readonly List<decimal> _lows = new();
private readonly List<decimal> _opens = new();
private readonly List<decimal> _closes = new();

/// <summary>
/// Fractal window size.
/// </summary>
public int Fractal { get; set; } = 70;

/// <summary>
/// Lag value for smoothing.
/// </summary>
public int Lag { get; set; } = 18;

/// <summary>
/// Horizontal shift, not used in calculation.
/// </summary>
public int Shift { get; set; }

/// <summary>
/// Minimal price step for threshold calculation.
/// </summary>
public decimal Step { get; set; } = 0.01m;

/// <inheritdoc />
public override void Reset()
{
base.Reset();
_pos = 0;
_smooth = new decimal[Lag + 1];
_highs.Clear();
_lows.Clear();
_opens.Clear();
_closes.Clear();
}

/// <inheritdoc />
protected override IIndicatorValue OnProcess(IIndicatorValue input)
{
var candle = input.GetValue<ICandleMessage>();

_highs.Add(candle.HighPrice);
_lows.Add(candle.LowPrice);
_opens.Add(candle.OpenPrice);
_closes.Add(candle.ClosePrice);

if (_highs.Count < Fractal * 2 || _highs.Count <= Lag)
return new DecimalIndicatorValue(this);

decimal r1 = Range(Fractal, 0) / Fractal;
decimal r2 = Range(Fractal, Fractal) / Fractal;
decimal r3 = Range(Fractal * 2, 0) / (Fractal * 2);

double dim = 0;
if (r1 + r2 > 0 && r3 > 0)
dim = (Math.Log((double)(r1 + r2)) - Math.Log((double)r3)) * 1.44269504088896;

var alpha = (decimal)Math.Exp(-Lag * (dim - 1.0));
if (alpha > 1m)
alpha = 1m;
if (alpha < 0.01m)
alpha = 0.01m;

var price = (candle.HighPrice + candle.LowPrice + 2m * candle.OpenPrice + 2m * candle.ClosePrice) / 6m;

var prevPos = (_pos - 1 + _smooth.Length) % _smooth.Length;
_smooth[_pos] = alpha * price + (1m - alpha) * _smooth[prevPos];

var lagPos = (_pos - Lag + _smooth.Length) % _smooth.Length;
var current = Math.Abs(_smooth[_pos] - _smooth[lagPos]) >= Lag * Lag * Step ? _smooth[_pos] : Value;

_pos = (_pos + 1) % _smooth.Length;

return new DecimalIndicatorValue(this, current);
}

private decimal Range(int count, int offset)
{
var end = _highs.Count - 1 - offset;
var start = end - count + 1;
if (start < 0)
start = 0;

var max = decimal.MinValue;
var min = decimal.MaxValue;

for (var i = start; i <= end; i++)
{
var h = _highs[i];
var l = _lows[i];
if (h > max)
max = h;
if (l < min)
min = l;
}

return max - min;
}
}
