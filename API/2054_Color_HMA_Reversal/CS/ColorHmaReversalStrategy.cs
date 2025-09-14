using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on Hull Moving Average slope reversals.
/// </summary>
public class ColorHmaReversalStrategy : Strategy
{
private readonly StrategyParam<int> _hmaPeriod;
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<bool> _buyOpen;
private readonly StrategyParam<bool> _sellOpen;
private readonly StrategyParam<bool> _buyClose;
private readonly StrategyParam<bool> _sellClose;

private decimal _prevValue1;
private decimal _prevValue2;

/// <summary>
/// Period for Hull Moving Average.
/// </summary>
public int HmaPeriod
{
get => _hmaPeriod.Value;
set => _hmaPeriod.Value = value;
}

/// <summary>
/// Candle type to process.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Allow opening long positions.
/// </summary>
public bool BuyOpen
{
get => _buyOpen.Value;
set => _buyOpen.Value = value;
}

/// <summary>
/// Allow opening short positions.
/// </summary>
public bool SellOpen
{
get => _sellOpen.Value;
set => _sellOpen.Value = value;
}

/// <summary>
/// Allow closing long positions.
/// </summary>
public bool BuyClose
{
get => _buyClose.Value;
set => _buyClose.Value = value;
}

/// <summary>
/// Allow closing short positions.
/// </summary>
public bool SellClose
{
get => _sellClose.Value;
set => _sellClose.Value = value;
}

/// <summary>
/// Initialize strategy parameters.
/// </summary>
public ColorHmaReversalStrategy()
{
_hmaPeriod = Param(nameof(HmaPeriod), 13)
.SetDisplay("HMA Period", "Hull Moving Average period", "Indicators")
.SetCanOptimize(true)
.SetOptimize(5, 30, 1);

_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");

_buyOpen = Param(nameof(BuyOpen), true)
.SetDisplay("Buy Open", "Allow opening long positions", "Trading");
_sellOpen = Param(nameof(SellOpen), true)
.SetDisplay("Sell Open", "Allow opening short positions", "Trading");
_buyClose = Param(nameof(BuyClose), true)
.SetDisplay("Buy Close", "Allow closing long positions", "Trading");
_sellClose = Param(nameof(SellClose), true)
.SetDisplay("Sell Close", "Allow closing short positions", "Trading");
}
/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();
_prevValue1 = default;
_prevValue2 = default;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var hma = new HullMovingAverage { Length = HmaPeriod };

var subscription = SubscribeCandles(CandleType);
subscription.Bind(hma, Process).Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, hma);
DrawOwnTrades(area);
}
}

private void Process(ICandleMessage candle, decimal hmaValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (_prevValue1 == default || _prevValue2 == default)
{
_prevValue2 = _prevValue1;
_prevValue1 = hmaValue;
return;
}

var wasFalling = _prevValue1 < _prevValue2;
var wasRising = _prevValue1 > _prevValue2;
var nowRising = hmaValue > _prevValue1;
var nowFalling = hmaValue < _prevValue1;

if (wasFalling && nowRising)
{
if (SellClose && Position < 0)
BuyMarket(Math.Abs(Position));

if (BuyOpen && Position <= 0)
BuyMarket(Volume);
}
else if (wasRising && nowFalling)
{
if (BuyClose && Position > 0)
SellMarket(Position);

if (SellOpen && Position >= 0)
SellMarket(Volume);
}

_prevValue2 = _prevValue1;
_prevValue1 = hmaValue;
}
}
