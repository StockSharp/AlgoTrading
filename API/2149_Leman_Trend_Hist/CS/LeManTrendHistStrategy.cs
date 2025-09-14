using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified port of the LeManTrendHist MQL strategy.
/// Uses an EMA-based histogram as a placeholder for the original indicator.
/// TODO: implement true LeManTrendHist indicator logic.
/// </summary>
public class LeManTrendHistStrategy : Strategy
{
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<int> _emaPeriod;
private readonly StrategyParam<int> _signalBar;
private readonly StrategyParam<bool> _buyOpen;
private readonly StrategyParam<bool> _sellOpen;
private readonly StrategyParam<bool> _buyClose;
private readonly StrategyParam<bool> _sellClose;

private ExponentialMovingAverage _ema;
private decimal? _value1;
private decimal? _value2;
private decimal? _value3;

/// <summary>
/// Type of candles to process.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Period for the EMA placeholder.
/// </summary>
public int EmaPeriod
{
get => _emaPeriod.Value;
set => _emaPeriod.Value = value;
}

/// <summary>
/// Number of bars to look back for signals.
/// </summary>
public int SignalBar
{
get => _signalBar.Value;
set => _signalBar.Value = value;
}

/// <summary>
/// Allow opening long positions.
/// </summary>
public bool BuyPosOpen
{
get => _buyOpen.Value;
set => _buyOpen.Value = value;
}

/// <summary>
/// Allow opening short positions.
/// </summary>
public bool SellPosOpen
{
get => _sellOpen.Value;
set => _sellOpen.Value = value;
}

/// <summary>
/// Allow closing long positions.
/// </summary>
public bool BuyPosClose
{
get => _buyClose.Value;
set => _buyClose.Value = value;
}

/// <summary>
/// Allow closing short positions.
/// </summary>
public bool SellPosClose
{
get => _sellClose.Value;
set => _sellClose.Value = value;
}

/// <summary>
/// Initializes a new instance of <see cref="LeManTrendHistStrategy"/>.
/// </summary>
public LeManTrendHistStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");

_emaPeriod = Param(nameof(EmaPeriod), 3)
.SetGreaterThanZero()
.SetDisplay("EMA Period", "EMA length for placeholder", "Parameters");

_signalBar = Param(nameof(SignalBar), 1)
.SetGreaterThanZero()
.SetDisplay("Signal Bar", "Shift for indicator values", "Parameters");

_buyOpen = Param(nameof(BuyPosOpen), true)
.SetDisplay("Buy Open", "Allow long entries", "Trading");

_sellOpen = Param(nameof(SellPosOpen), true)
.SetDisplay("Sell Open", "Allow short entries", "Trading");

_buyClose = Param(nameof(BuyPosClose), true)
.SetDisplay("Buy Close", "Allow closing longs", "Trading");

_sellClose = Param(nameof(SellPosClose), true)
.SetDisplay("Sell Close", "Allow closing shorts", "Trading");
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

_ema = default;
_value1 = null;
_value2 = null;
_value3 = null;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

StartProtection();

_ema = new ExponentialMovingAverage { Length = EmaPeriod };

var subscription = SubscribeCandles(CandleType);
subscription.Bind(_ema, ProcessCandle).Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _ema);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal emaValue)
{
if (candle.State != CandleStates.Finished)
return;

// Shift stored values to maintain history of three bars
_value3 = _value2;
_value2 = _value1;
_value1 = emaValue;

if (_value1 is null || _value2 is null || _value3 is null)
return; // Not enough data yet

// Check for upward signal
if (_value2 < _value3)
{
if (BuyPosOpen && _value1 > _value2 && Position <= 0)
BuyMarket();

if (SellPosClose && Position < 0)
BuyMarket(Math.Abs(Position));
}
// Check for downward signal
else if (_value2 > _value3)
{
if (SellPosOpen && _value1 < _value2 && Position >= 0)
SellMarket();

if (BuyPosClose && Position > 0)
SellMarket(Position);
}
}
}
