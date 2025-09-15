using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on PPO main and signal line crossovers.
/// </summary>
public class PpoCloudStrategy : Strategy
{
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<int> _fastPeriod;
private readonly StrategyParam<int> _slowPeriod;
private readonly StrategyParam<int> _signalPeriod;
private readonly StrategyParam<bool> _enableLong;
private readonly StrategyParam<bool> _enableShort;
private readonly StrategyParam<bool> _closeLongOnSignal;
private readonly StrategyParam<bool> _closeShortOnSignal;
private readonly StrategyParam<decimal> _volume;

private Ppo _ppo = null!;
private decimal _prevPpo;
private decimal _prevSignal;
private bool _hasPrev;

/// <summary>
/// Candle type for strategy calculation.
/// </summary>
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

/// <summary>
/// Fast EMA length.
/// </summary>
public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }

/// <summary>
/// Slow EMA length.
/// </summary>
public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }

/// <summary>
/// Signal line EMA length.
/// </summary>
public int SignalPeriod { get => _signalPeriod.Value; set => _signalPeriod.Value = value; }

/// <summary>
/// Enable long entries.
/// </summary>
public bool EnableLong { get => _enableLong.Value; set => _enableLong.Value = value; }

/// <summary>
/// Enable short entries.
/// </summary>
public bool EnableShort { get => _enableShort.Value; set => _enableShort.Value = value; }

/// <summary>
/// Close long position on opposite signal.
/// </summary>
public bool CloseLongOnSignal { get => _closeLongOnSignal.Value; set => _closeLongOnSignal.Value = value; }

/// <summary>
/// Close short position on opposite signal.
/// </summary>
public bool CloseShortOnSignal { get => _closeShortOnSignal.Value; set => _closeShortOnSignal.Value = value; }

/// <summary>
/// Order volume.
/// </summary>
public decimal Volume { get => _volume.Value; set => _volume.Value = value; }

/// <summary>
/// Initializes <see cref="PpoCloudStrategy"/>.
/// </summary>
public PpoCloudStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");

_fastPeriod = Param(nameof(FastPeriod), 12)
.SetGreaterThanZero()
.SetDisplay("Fast Period", "Fast EMA length", "PPO")
.SetCanOptimize(true);

_slowPeriod = Param(nameof(SlowPeriod), 26)
.SetGreaterThanZero()
.SetDisplay("Slow Period", "Slow EMA length", "PPO")
.SetCanOptimize(true);

_signalPeriod = Param(nameof(SignalPeriod), 9)
.SetGreaterThanZero()
.SetDisplay("Signal Period", "Signal EMA length", "PPO")
.SetCanOptimize(true);

_enableLong = Param(nameof(EnableLong), true)
.SetDisplay("Enable Long", "Allow long trades", "Trading");

_enableShort = Param(nameof(EnableShort), true)
.SetDisplay("Enable Short", "Allow short trades", "Trading");

_closeLongOnSignal = Param(nameof(CloseLongOnSignal), true)
.SetDisplay("Close Long On Signal", "Close long on opposite crossover", "Trading");

_closeShortOnSignal = Param(nameof(CloseShortOnSignal), true)
.SetDisplay("Close Short On Signal", "Close short on opposite crossover", "Trading");

_volume = Param(nameof(Volume), 1m)
.SetGreaterThanZero()
.SetDisplay("Volume", "Order volume", "Trading");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
yield return (Security, CandleType);
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_ppo = new Ppo
{
ShortPeriod = FastPeriod,
LongPeriod = SlowPeriod,
SignalPeriod = SignalPeriod
};

var subscription = SubscribeCandles(CandleType);
subscription.Bind(_ppo, ProcessCandle).Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
var ppoArea = CreateChartArea();
DrawIndicator(ppoArea, _ppo);
DrawOwnTrades(area);
}

StartProtection();
}

private void ProcessCandle(ICandleMessage candle, decimal ppoValue, decimal signalValue, decimal histogram)
{
if (candle.State != CandleStates.Finished)
return;

if (!_ppo.IsFormed)
return;

if (_hasPrev)
{
var crossUp = _prevPpo <= _prevSignal && ppoValue > signalValue;
var crossDown = _prevPpo >= _prevSignal && ppoValue < signalValue;

if (crossUp)
{
if (CloseShortOnSignal && Position < 0)
BuyMarket(Math.Abs(Position));

if (EnableLong && Position <= 0)
BuyMarket(Volume);
}
else if (crossDown)
{
if (CloseLongOnSignal && Position > 0)
SellMarket(Position);

if (EnableShort && Position >= 0)
SellMarket(Volume);
}
}

_prevPpo = ppoValue;
_prevSignal = signalValue;
_hasPrev = true;
}
}

