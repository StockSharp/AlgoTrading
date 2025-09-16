using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on MACD line crossover with optional fresh signal check.
/// </summary>
public class ZeroLagMacdStrategy : Strategy
{
private readonly StrategyParam<int> _fastEmaLength;
private readonly StrategyParam<int> _slowEmaLength;
private readonly StrategyParam<int> _signalEmaLength;
private readonly StrategyParam<bool> _useFreshSignal;
private readonly StrategyParam<decimal> _volume;
private readonly StrategyParam<int> _startHour;
private readonly StrategyParam<int> _endHour;
private readonly StrategyParam<int> _killDay;
private readonly StrategyParam<int> _killHour;
private readonly StrategyParam<DataType> _candleType;

private decimal _prevMacd;
private decimal _prevSignal;
private bool _hasPrev;

/// <summary>
/// Fast EMA length for MACD.
/// </summary>
public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }

/// <summary>
/// Slow EMA length for MACD.
/// </summary>
public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }

/// <summary>
/// Signal EMA length for MACD.
/// </summary>
public int SignalEmaLength { get => _signalEmaLength.Value; set => _signalEmaLength.Value = value; }

/// <summary>
/// Require that the MACD crossover happens on the current bar.
/// </summary>
public bool UseFreshSignal { get => _useFreshSignal.Value; set => _useFreshSignal.Value = value; }

/// <summary>
/// Order volume.
/// </summary>
public decimal Volume { get => _volume.Value; set => _volume.Value = value; }

/// <summary>
/// Trading start hour.
/// </summary>
public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }

/// <summary>
/// Trading end hour.
/// </summary>
public int EndHour { get => _endHour.Value; set => _endHour.Value = value; }

/// <summary>
/// Day of week to force closing (0=Sunday).
/// </summary>
public int KillDay { get => _killDay.Value; set => _killDay.Value = value; }

/// <summary>
/// Hour to force closing on <see cref=\"KillDay\"/>.
/// </summary>
public int KillHour { get => _killHour.Value; set => _killHour.Value = value; }

/// <summary>
/// Candle type used for calculations.
/// </summary>
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

/// <summary>
/// Initialize <see cref=\"ZeroLagMacdStrategy\"/>.
/// </summary>
public ZeroLagMacdStrategy()
{
_fastEmaLength = Param(nameof(FastEmaLength), 2).SetGreaterThanZero();
_slowEmaLength = Param(nameof(SlowEmaLength), 34).SetGreaterThanZero();
_signalEmaLength = Param(nameof(SignalEmaLength), 2).SetGreaterThanZero();
_useFreshSignal = Param(nameof(UseFreshSignal), true)
.SetDisplay(\"Use Fresh Signal\", \"Require MACD crossover\", \"General\");
_volume = Param(nameof(Volume), 2m)
.SetGreaterThanZero()
.SetDisplay(\"Volume\", \"Order volume\", \"Trading\");
_startHour = Param(nameof(StartHour), 9)
.SetDisplay(\"Start Hour\", \"Trading start hour\", \"Time Filter\");
_endHour = Param(nameof(EndHour), 15)
.SetDisplay(\"End Hour\", \"Trading end hour\", \"Time Filter\");
_killDay = Param(nameof(KillDay), 5)
.SetDisplay(\"Kill Day\", \"Day to close trades (0=Sunday)\", \"Time Filter\");
_killHour = Param(nameof(KillHour), 21)
.SetDisplay(\"Kill Hour\", \"Hour to close on kill day\", \"Time Filter\");
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay(\"Candle Type\", \"Source candles\", \"General\");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
=> [(Security, CandleType)];

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();
_prevMacd = 0m;
_prevSignal = 0m;
_hasPrev = false;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var macd = new MovingAverageConvergenceDivergenceSignal
{
Macd =
{
ShortMa = { Length = FastEmaLength },
LongMa = { Length = SlowEmaLength },
},
SignalMa = { Length = SignalEmaLength }
};

var subscription = SubscribeCandles(CandleType);
subscription.BindEx(macd, ProcessCandle).Start();
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
{
if (candle.State != CandleStates.Finished)
return;

var time = candle.OpenTime;
var hour = time.Hour;
var day = (int)time.DayOfWeek;

// Close positions outside allowed trading time
var outside =
hour < StartHour ||
hour >= EndHour ||
(day == KillDay && hour == KillHour);

if (outside)
{
if (Position != 0)
ClosePosition();

_hasPrev = false;
return;
}

if (!macdValue.IsFinal)
return;

var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
return;

var isFresh = true;
if (UseFreshSignal && _hasPrev)
{
isFresh =
(_prevSignal > _prevMacd && signal < macd) ||
(_prevSignal < _prevMacd && signal > macd);

if (!isFresh)
{
_prevMacd = macd;
_prevSignal = signal;
return;
}
}

var buySignal = signal < macd;
var sellSignal = signal > macd;

// Close on opposite signal
if (Position > 0 && sellSignal)
{
ClosePosition();
}
else if (Position < 0 && buySignal)
{
ClosePosition();
}
else if (Position == 0)
{
if (buySignal)
BuyMarket(Volume);
else if (sellSignal)
SellMarket(Volume);
}

_prevMacd = macd;
_prevSignal = signal;
_hasPrev = true;
}
}
