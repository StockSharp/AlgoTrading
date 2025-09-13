using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Three moving averages crossover strategy with trailing stop and take profit.
/// </summary>
public class Up3x1KrohaborDStrategy : Strategy
{
private readonly StrategyParam<int> _fastPeriod;
private readonly StrategyParam<int> _middlePeriod;
private readonly StrategyParam<int> _slowPeriod;
private readonly StrategyParam<decimal> _takeProfit;
private readonly StrategyParam<decimal> _stopLoss;
private readonly StrategyParam<decimal> _trailingStop;
private readonly StrategyParam<DataType> _candleType;

private decimal _prevFast;
private decimal _prevMiddle;
private decimal _prevSlow;

private decimal _entryPrice;
private decimal _takeProfitPrice;
private decimal _stopLossPrice;

public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
public int MiddlePeriod { get => _middlePeriod.Value; set => _middlePeriod.Value = value; }
public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

/// <summary>
/// Initializes <see cref="Up3x1KrohaborDStrategy"/>.
/// </summary>
public Up3x1KrohaborDStrategy()
{
Param(nameof(Volume), 1m)
.SetGreaterThanZero()
.SetDisplay("Volume", "Order volume", "General");

_fastPeriod = Param(nameof(FastPeriod), 24)
.SetGreaterThanZero()
.SetDisplay("Fast Period", "Fast MA period", "MA Settings");

_middlePeriod = Param(nameof(MiddlePeriod), 60)
.SetGreaterThanZero()
.SetDisplay("Middle Period", "Middle MA period", "MA Settings");

_slowPeriod = Param(nameof(SlowPeriod), 120)
.SetGreaterThanZero()
.SetDisplay("Slow Period", "Slow MA period", "MA Settings");

_takeProfit = Param(nameof(TakeProfit), 50m)
.SetGreaterThanZero()
.SetDisplay("Take Profit", "Take profit distance", "Risk");

_stopLoss = Param(nameof(StopLoss), 1100m)
.SetGreaterThanZero()
.SetDisplay("Stop Loss", "Stop loss distance", "Risk");

_trailingStop = Param(nameof(TrailingStop), 100m)
.SetGreaterThanZero()
.SetDisplay("Trailing Stop", "Trailing stop distance", "Risk");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");
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
_prevFast = 0m;
_prevMiddle = 0m;
_prevSlow = 0m;
_entryPrice = 0m;
_takeProfitPrice = 0m;
_stopLossPrice = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);
StartProtection();

var fastMa = new SMA { Length = FastPeriod };
var middleMa = new SMA { Length = MiddlePeriod };
var slowMa = new SMA { Length = SlowPeriod };

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(fastMa, middleMa, slowMa, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, fastMa);
DrawIndicator(area, middleMa);
DrawIndicator(area, slowMa);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal fast, decimal middle, decimal slow)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var fastCrossMidUp = fast > middle && _prevFast < _prevMiddle;
var fastCrossMidDown = fast < middle && _prevFast > _prevMiddle;

var fastMoreSlow = fast > slow && fast > _prevSlow && _prevFast > slow && _prevFast > _prevSlow;
var fastLessSlow = fast < slow && fast < _prevSlow && _prevFast < slow && _prevFast < _prevSlow;

var middleMoreSlow = middle > slow && middle > _prevSlow && _prevMiddle > slow && _prevMiddle > _prevSlow;
var middleLessSlow = middle < slow && middle < _prevSlow && _prevMiddle < slow && _prevMiddle < _prevSlow;

if (Position == 0)
{
if (fastCrossMidUp && fastMoreSlow && middleMoreSlow)
{
_entryPrice = candle.ClosePrice;
_takeProfitPrice = _entryPrice + TakeProfit;
_stopLossPrice = _entryPrice - StopLoss;
BuyMarket(Volume);
}
else if (fastCrossMidDown && fastLessSlow && middleLessSlow)
{
_entryPrice = candle.ClosePrice;
_takeProfitPrice = _entryPrice - TakeProfit;
_stopLossPrice = _entryPrice + StopLoss;
SellMarket(Volume);
}
}
else if (Position > 0)
{
if (TrailingStop > 0m)
{
var newStop = candle.ClosePrice - TrailingStop;
if (newStop > _stopLossPrice)
_stopLossPrice = newStop;
}

if (candle.ClosePrice >= _takeProfitPrice || candle.ClosePrice <= _stopLossPrice)
{
SellMarket(Math.Abs(Position));
_entryPrice = 0m;
}
}
else if (Position < 0)
{
if (TrailingStop > 0m)
{
var newStop = candle.ClosePrice + TrailingStop;
if (newStop < _stopLossPrice)
_stopLossPrice = newStop;
}

if (candle.ClosePrice <= _takeProfitPrice || candle.ClosePrice >= _stopLossPrice)
{
BuyMarket(Math.Abs(Position));
_entryPrice = 0m;
}
}

_prevFast = fast;
_prevMiddle = middle;
_prevSlow = slow;
}
}
