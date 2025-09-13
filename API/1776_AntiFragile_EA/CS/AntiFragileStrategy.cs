using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid strategy that places layered limit orders above and below the market.
/// </summary>
public class AntiFragileStrategy : Strategy
{
private readonly StrategyParam<decimal> _startingVolume;
private readonly StrategyParam<decimal> _increasePercentage;
private readonly StrategyParam<decimal> _spaceBetweenTrades;
private readonly StrategyParam<int> _numberOfTrades;
private readonly StrategyParam<decimal> _stopLossPips;
private readonly StrategyParam<decimal> _trailingStopPips;
private readonly StrategyParam<bool> _tradeLong;
private readonly StrategyParam<bool> _tradeShort;
private readonly StrategyParam<DataType> _candleType;

private decimal _entryPriceLong;
private decimal _entryPriceShort;
private decimal _trailingLong;
private decimal _trailingShort;
private decimal _prevPosition;

/// <summary>
/// Initial order volume.
/// </summary>
public decimal StartingVolume { get => _startingVolume.Value; set => _startingVolume.Value = value; }

/// <summary>
/// Percentage increase for each subsequent order.
/// </summary>
public decimal IncreasePercentage { get => _increasePercentage.Value; set => _increasePercentage.Value = value; }

/// <summary>
/// Distance between grid orders in price steps.
/// </summary>
public decimal SpaceBetweenTrades { get => _spaceBetweenTrades.Value; set => _spaceBetweenTrades.Value = value; }

/// <summary>
/// Total number of grid levels per side.
/// </summary>
public int NumberOfTrades { get => _numberOfTrades.Value; set => _numberOfTrades.Value = value; }

/// <summary>
/// Initial stop loss distance in price steps.
/// </summary>
public decimal StopLossPips { get => _stopLossPips.Value; set => _stopLossPips.Value = value; }

/// <summary>
/// Trailing stop distance in price steps.
/// </summary>
public decimal TrailingStopPips { get => _trailingStopPips.Value; set => _trailingStopPips.Value = value; }

/// <summary>
/// Enable placing long grid orders.
/// </summary>
public bool TradeLong { get => _tradeLong.Value; set => _tradeLong.Value = value; }

/// <summary>
/// Enable placing short grid orders.
/// </summary>
public bool TradeShort { get => _tradeShort.Value; set => _tradeShort.Value = value; }

/// <summary>
/// Candle type used to monitor price for trailing stops.
/// </summary>
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

/// <summary>
/// Initializes a new instance of the strategy.
/// </summary>
public AntiFragileStrategy()
{
_startingVolume = Param(nameof(StartingVolume), 0.1m)
.SetGreaterThanZero()
.SetDisplay("Starting Volume", "Initial order volume", "Trading");

_increasePercentage = Param(nameof(IncreasePercentage), 1m)
.SetDisplay("Increase %", "Percent increase for each grid level", "Trading");

_spaceBetweenTrades = Param(nameof(SpaceBetweenTrades), 700m)
.SetGreaterThanZero()
.SetDisplay("Spacing", "Distance between grid levels in steps", "Trading");

_numberOfTrades = Param(nameof(NumberOfTrades), 50)
.SetGreaterThanZero()
.SetDisplay("Levels", "Number of grid levels per side", "Trading");

_stopLossPips = Param(nameof(StopLossPips), 300m)
.SetGreaterThanZero()
.SetDisplay("Stop Loss", "Initial stop loss in steps", "Risk");

_trailingStopPips = Param(nameof(TrailingStopPips), 100m)
.SetGreaterThanZero()
.SetDisplay("Trail", "Trailing stop distance in steps", "Risk");

_tradeLong = Param(nameof(TradeLong), true)
.SetDisplay("Trade Long", "Enable long grid", "General");

_tradeShort = Param(nameof(TradeShort), true)
.SetDisplay("Trade Short", "Enable short grid", "General");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Candle type for trailing", "General");
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
_entryPriceLong = 0m;
_entryPriceShort = 0m;
_trailingLong = 0m;
_trailingShort = 0m;
_prevPosition = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var step = Security.PriceStep ?? 1m;
var bid = Security.BestBid?.Price ?? Security.LastTrade?.Price ?? 0m;
var ask = Security.BestAsk?.Price ?? Security.LastTrade?.Price ?? 0m;

for (var i = 1; i <= NumberOfTrades; i++)
{
var volume = Math.Round(StartingVolume * (1m + (i - 1) * (IncreasePercentage / 100m)), 2);
var offset = SpaceBetweenTrades * i * step;

if (TradeLong)
{
var price = bid - offset;
BuyLimit(price, volume);
}

if (TradeShort)
{
var price = ask + offset;
SellLimit(price, volume);
}
}

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

var step = Security.PriceStep ?? 1m;

if (Position > 0)
{
if (_prevPosition <= 0)
{
_entryPriceLong = candle.ClosePrice;
_trailingLong = _entryPriceLong - StopLossPips * step;
}

var move = candle.ClosePrice - _entryPriceLong;
if (move > TrailingStopPips * step)
_trailingLong = Math.Max(_trailingLong, candle.ClosePrice - TrailingStopPips * step);

if (candle.ClosePrice <= _trailingLong)
{
SellMarket(Position);
_entryPriceLong = 0m;
_trailingLong = 0m;
}
}
else if (Position < 0)
{
if (_prevPosition >= 0)
{
_entryPriceShort = candle.ClosePrice;
_trailingShort = _entryPriceShort + StopLossPips * step;
}

var move = _entryPriceShort - candle.ClosePrice;
if (move > TrailingStopPips * step)
_trailingShort = Math.Min(_trailingShort, candle.ClosePrice + TrailingStopPips * step);

if (candle.ClosePrice >= _trailingShort)
{
BuyMarket(Math.Abs(Position));
_entryPriceShort = 0m;
_trailingShort = 0m;
}
}

_prevPosition = Position;
}
}
