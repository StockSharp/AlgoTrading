namespace StockSharp.Samples.Strategies;


using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on Delta RSI indicator.
/// </summary>
public class DeltaRsiStrategy : Strategy
{
private const int Up = 0;
private const int Pass = 1;
private const int Down = 2;

private readonly StrategyParam<int> _fastPeriod;
private readonly StrategyParam<int> _slowPeriod;
private readonly StrategyParam<int> _level;
private readonly StrategyParam<bool> _buyPosOpen;
private readonly StrategyParam<bool> _sellPosOpen;
private readonly StrategyParam<bool> _buyPosClose;
private readonly StrategyParam<bool> _sellPosClose;
private readonly StrategyParam<DataType> _candleType;

private int _prevColor = Pass;

public int FastPeriod
{
get => _fastPeriod.Value;
set => _fastPeriod.Value = value;
}

public int SlowPeriod
{
get => _slowPeriod.Value;
set => _slowPeriod.Value = value;
}

public int Level
{
get => _level.Value;
set => _level.Value = value;
}

public bool BuyPosOpen
{
get => _buyPosOpen.Value;
set => _buyPosOpen.Value = value;
}

public bool SellPosOpen
{
get => _sellPosOpen.Value;
set => _sellPosOpen.Value = value;
}

public bool BuyPosClose
{
get => _buyPosClose.Value;
set => _buyPosClose.Value = value;
}

public bool SellPosClose
{
get => _sellPosClose.Value;
set => _sellPosClose.Value = value;
}

public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

public DeltaRsiStrategy()
{
_fastPeriod = Param(nameof(FastPeriod), 14)
.SetDisplay("Fast RSI Period", "Length of fast RSI", "Parameters");

_slowPeriod = Param(nameof(SlowPeriod), 50)
.SetDisplay("Slow RSI Period", "Length of slow RSI", "Parameters");

_level = Param(nameof(Level), 50)
.SetDisplay("Signal Level", "RSI threshold level", "Parameters");

_buyPosOpen = Param(nameof(BuyPosOpen), true)
.SetDisplay("Open Long", "Allow opening long positions", "Parameters");

_sellPosOpen = Param(nameof(SellPosOpen), true)
.SetDisplay("Open Short", "Allow opening short positions", "Parameters");

_buyPosClose = Param(nameof(BuyPosClose), true)
.SetDisplay("Close Long", "Allow closing long positions", "Parameters");

_sellPosClose = Param(nameof(SellPosClose), true)
.SetDisplay("Close Short", "Allow closing short positions", "Parameters");

_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
return new[] {(Security, CandleType)};
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var rsiFast = new RelativeStrengthIndex
{
Length = FastPeriod
};

var rsiSlow = new RelativeStrengthIndex
{
Length = SlowPeriod
};

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(rsiFast, rsiSlow, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, rsiFast);
DrawIndicator(area, rsiSlow);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal rsiFast, decimal rsiSlow)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var color = Pass;
if (rsiSlow > Level && rsiFast > rsiSlow)
color = Up;
else if (rsiSlow < 100 - Level && rsiFast < rsiSlow)
color = Down;

if (_prevColor == Up && color != Up)
{
if (SellPosClose && Position < 0)
ClosePosition();

if (BuyPosOpen && Position <= 0)
BuyMarket();
}
else if (_prevColor == Down && color != Down)
{
if (BuyPosClose && Position > 0)
ClosePosition();

if (SellPosOpen && Position >= 0)
SellMarket();
}

_prevColor = color;
}
}
