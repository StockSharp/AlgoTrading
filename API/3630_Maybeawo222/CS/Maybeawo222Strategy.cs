using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// maybeawo222 moving average crossover with scheduled trading hours and staged breakeven.
/// </summary>
public class Maybeawo222Strategy : Strategy
{
private readonly StrategyParam<int> _movingPeriod;
private readonly StrategyParam<int> _movingShift;
private readonly StrategyParam<decimal> _stopLossPips;
private readonly StrategyParam<decimal> _takeProfitPips;
private readonly StrategyParam<decimal> _breakevenPips1;
private readonly StrategyParam<decimal> _breakevenPips2;
private readonly StrategyParam<decimal> _desiredBreakevenDistancePips1;
private readonly StrategyParam<decimal> _desiredBreakevenDistancePips2;
private readonly StrategyParam<int> _startHour;
private readonly StrategyParam<int> _endHour;
private readonly StrategyParam<decimal> _orderVolume;
private readonly StrategyParam<DataType> _candleType;

private readonly Queue<decimal> _maHistory = new();

private decimal _pipSize;
private decimal? _longStopPrice;
private decimal? _longTakePrice;
private decimal? _shortStopPrice;
private decimal? _shortTakePrice;
private bool _breakevenLevel1Reached;
private bool _breakevenLevel2Reached;

public int MovingPeriod
{
get => _movingPeriod.Value;
set => _movingPeriod.Value = value;
}

public int MovingShift
{
get => _movingShift.Value;
set => _movingShift.Value = value;
}

public decimal StopLossPips
{
get => _stopLossPips.Value;
set => _stopLossPips.Value = value;
}

public decimal TakeProfitPips
{
get => _takeProfitPips.Value;
set => _takeProfitPips.Value = value;
}

public decimal BreakevenPips1
{
get => _breakevenPips1.Value;
set => _breakevenPips1.Value = value;
}

public decimal BreakevenPips2
{
get => _breakevenPips2.Value;
set => _breakevenPips2.Value = value;
}

public decimal DesiredBreakevenDistancePips1
{
get => _desiredBreakevenDistancePips1.Value;
set => _desiredBreakevenDistancePips1.Value = value;
}

public decimal DesiredBreakevenDistancePips2
{
get => _desiredBreakevenDistancePips2.Value;
set => _desiredBreakevenDistancePips2.Value = value;
}

public int StartHour
{
get => _startHour.Value;
set => _startHour.Value = value;
}

public int EndHour
{
get => _endHour.Value;
set => _endHour.Value = value;
}

public decimal OrderVolume
{
get => _orderVolume.Value;
set => _orderVolume.Value = value;
}

public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

public Maybeawo222Strategy()
{
_movingPeriod = Param(nameof(MovingPeriod), 14)
.SetDisplay("MA Period", "Simple moving average period", "Indicators")
.SetCanOptimize(true);

_movingShift = Param(nameof(MovingShift), 0)
.SetDisplay("MA Shift", "Number of completed candles used as shift", "Indicators")
.SetCanOptimize(true);

_stopLossPips = Param(nameof(StopLossPips), 100m)
.SetDisplay("Stop Loss (pips)", "Fixed stop-loss distance in pips", "Risk")
.SetCanOptimize(true);

_takeProfitPips = Param(nameof(TakeProfitPips), 800m)
.SetDisplay("Take Profit (pips)", "Fixed take-profit distance in pips", "Risk")
.SetCanOptimize(true);

_breakevenPips1 = Param(nameof(BreakevenPips1), 180m)
.SetDisplay("Breakeven Trigger 1 (pips)", "Profit distance that triggers the first stop adjustment", "Risk")
.SetCanOptimize(true);

_breakevenPips2 = Param(nameof(BreakevenPips2), 500m)
.SetDisplay("Breakeven Trigger 2 (pips)", "Profit distance that triggers the second stop adjustment", "Risk")
.SetCanOptimize(true);

_desiredBreakevenDistancePips1 = Param(nameof(DesiredBreakevenDistancePips1), 60m)
.SetDisplay("Breakeven Stop 1 (pips)", "Stop distance applied when the first breakeven is triggered", "Risk")
.SetCanOptimize(true);

_desiredBreakevenDistancePips2 = Param(nameof(DesiredBreakevenDistancePips2), 350m)
.SetDisplay("Breakeven Stop 2 (pips)", "Stop distance applied when the second breakeven is triggered", "Risk")
.SetCanOptimize(true);

_startHour = Param(nameof(StartHour), 3)
.SetDisplay("Start Hour", "Trading window start hour (0-23)", "Schedule")
.SetCanOptimize(true);

_endHour = Param(nameof(EndHour), 22)
.SetDisplay("End Hour", "Trading window end hour (0-23)", "Schedule")
.SetCanOptimize(true);

_orderVolume = Param(nameof(OrderVolume), 0.5m)
.SetDisplay("Order Volume", "Base market order volume", "Trading")
.SetCanOptimize(true);

_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
.SetDisplay("Candle Type", "Primary candle series", "General");
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

_maHistory.Clear();
_pipSize = 0m;
_longStopPrice = null;
_longTakePrice = null;
_shortStopPrice = null;
_shortTakePrice = null;
_breakevenLevel1Reached = false;
_breakevenLevel2Reached = false;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_pipSize = CalculatePipSize();
Volume = OrderVolume;

var sma = new SimpleMovingAverage { Length = MovingPeriod };

var subscription = SubscribeCandles(CandleType);
subscription.Bind(sma, ProcessCandle).Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, sma);
DrawOwnTrades(area);
}
}

/// <inheritdoc />
protected override void OnPositionChanged(decimal delta)
{
base.OnPositionChanged(delta);

if (Position > 0m && delta > 0m)
{
// A new long position was opened or increased.
_longStopPrice = StopLossPips > 0m ? PositionPrice - StopLossPips * _pipSize : null;
_longTakePrice = TakeProfitPips > 0m ? PositionPrice + TakeProfitPips * _pipSize : null;
_shortStopPrice = null;
_shortTakePrice = null;
_breakevenLevel1Reached = false;
_breakevenLevel2Reached = false;
}
else if (Position < 0m && delta < 0m)
{
// A new short position was opened or increased.
_shortStopPrice = StopLossPips > 0m ? PositionPrice + StopLossPips * _pipSize : null;
_shortTakePrice = TakeProfitPips > 0m ? PositionPrice - TakeProfitPips * _pipSize : null;
_longStopPrice = null;
_longTakePrice = null;
_breakevenLevel1Reached = false;
_breakevenLevel2Reached = false;
}
else if (Position == 0m)
{
// All trades were closed, reset cached risk levels.
_longStopPrice = null;
_longTakePrice = null;
_shortStopPrice = null;
_shortTakePrice = null;
_breakevenLevel1Reached = false;
_breakevenLevel2Reached = false;
}
}

private void ProcessCandle(ICandleMessage candle, decimal maValue)
{
if (candle.State != CandleStates.Finished)
return;

ManageActivePosition(candle);

var shiftedMa = UpdateShiftedMa(maValue);
if (shiftedMa is not decimal ma)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var hour = candle.CloseTime.Hour;
if (hour < StartHour || hour >= EndHour)
return;

var open = candle.OpenPrice;
var close = candle.ClosePrice;

var buySignal = open < ma && close > ma;
var sellSignal = open > ma && close < ma;

if (buySignal && Position <= 0m)
{
EnterLong();
}
else if (sellSignal && Position >= 0m)
{
EnterShort();
}
}

private void EnterLong()
{
var volume = Volume;
if (Position < 0m)
volume += -Position;

if (volume > 0m)
BuyMarket(volume);
}

private void EnterShort()
{
var volume = Volume;
if (Position > 0m)
volume += Position;

if (volume > 0m)
SellMarket(volume);
}

private void ManageActivePosition(ICandleMessage candle)
{
if (Position > 0m)
{
if (TryCloseLongByStopOrTarget(candle))
return;

UpdateLongBreakeven(candle);
}
else if (Position < 0m)
{
if (TryCloseShortByStopOrTarget(candle))
return;

UpdateShortBreakeven(candle);
}
}

private bool TryCloseLongByStopOrTarget(ICandleMessage candle)
{
if (_longStopPrice is decimal stopPrice && candle.LowPrice <= stopPrice)
{
SellMarket(Position);
return true;
}

if (_longTakePrice is decimal takePrice && candle.HighPrice >= takePrice)
{
SellMarket(Position);
return true;
}

return false;
}

private bool TryCloseShortByStopOrTarget(ICandleMessage candle)
{
if (_shortStopPrice is decimal stopPrice && candle.HighPrice >= stopPrice)
{
BuyMarket(-Position);
return true;
}

if (_shortTakePrice is decimal takePrice && candle.LowPrice <= takePrice)
{
BuyMarket(-Position);
return true;
}

return false;
}

private void UpdateLongBreakeven(ICandleMessage candle)
{
var entry = PositionPrice;
var high = candle.HighPrice;

if (!_breakevenLevel1Reached && BreakevenPips1 > 0m)
{
var trigger = entry + BreakevenPips1 * _pipSize;
if (high >= trigger)
{
var newStop = entry + DesiredBreakevenDistancePips1 * _pipSize;
if (_longStopPrice is decimal currentStop)
_longStopPrice = Math.Max(currentStop, newStop);
else
_longStopPrice = newStop;

_breakevenLevel1Reached = true;
}
}

if (!_breakevenLevel2Reached && BreakevenPips2 > 0m)
{
var trigger = entry + BreakevenPips2 * _pipSize;
if (high >= trigger)
{
var newStop = entry + DesiredBreakevenDistancePips2 * _pipSize;
if (_longStopPrice is decimal currentStop)
_longStopPrice = Math.Max(currentStop, newStop);
else
_longStopPrice = newStop;

_breakevenLevel2Reached = true;
}
}
}

private void UpdateShortBreakeven(ICandleMessage candle)
{
var entry = PositionPrice;
var low = candle.LowPrice;

if (!_breakevenLevel1Reached && BreakevenPips1 > 0m)
{
var trigger = entry - BreakevenPips1 * _pipSize;
if (low <= trigger)
{
var newStop = entry - DesiredBreakevenDistancePips1 * _pipSize;
if (_shortStopPrice is decimal currentStop)
_shortStopPrice = Math.Min(currentStop, newStop);
else
_shortStopPrice = newStop;

_breakevenLevel1Reached = true;
}
}

if (!_breakevenLevel2Reached && BreakevenPips2 > 0m)
{
var trigger = entry - BreakevenPips2 * _pipSize;
if (low <= trigger)
{
var newStop = entry - DesiredBreakevenDistancePips2 * _pipSize;
if (_shortStopPrice is decimal currentStop)
_shortStopPrice = Math.Min(currentStop, newStop);
else
_shortStopPrice = newStop;

_breakevenLevel2Reached = true;
}
}
}

private decimal? UpdateShiftedMa(decimal currentValue)
{
_maHistory.Enqueue(currentValue);

while (_maHistory.Count > MovingShift + 1)
_maHistory.Dequeue();

if (_maHistory.Count < MovingShift + 1)
return null;

return _maHistory.Peek();
}

private decimal CalculatePipSize()
{
var step = Security?.PriceStep ?? 0m;
if (step <= 0m)
return 1m;

return step < 0.01m ? step * 10m : step;
}
}
