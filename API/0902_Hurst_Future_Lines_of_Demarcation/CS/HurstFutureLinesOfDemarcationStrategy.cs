using System;
using System.Collections.Generic;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Indicators;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public enum CloseTrigger
{
Price,
Signal,
Trade,
Trend,
None
}

public class HurstFutureLinesOfDemarcationStrategy : Strategy
{
private SimpleMovingAverage _sma;
private readonly Queue<decimal> _signalQueue = new();
private readonly Queue<decimal> _tradeQueue = new();
private readonly Queue<decimal> _trendQueue = new();
private int _signalOffset;
private int _tradeOffset;
private int _trendOffset;
private decimal? _signal;
private decimal? _trade;
private decimal? _trend;
private decimal? _prevPrice;
private decimal? _prevSignal;
private decimal? _prevClose1;
private decimal? _prevClose2;
private int _state;

private StrategyParam<bool> _smoothFld;
private StrategyParam<int> _fldSmoothing;
private StrategyParam<int> _signalCycleLength;
private StrategyParam<int> _tradeCycleLength;
private StrategyParam<int> _trendCycleLength;
private StrategyParam<CloseTrigger> _closeTrigger1;
private StrategyParam<CloseTrigger> _closeTrigger2;

public bool SmoothFld { get => _smoothFld.Value; set => _smoothFld.Value = value; }
public int FldSmoothing { get => _fldSmoothing.Value; set => _fldSmoothing.Value = value; }
public int SignalCycleLength { get => _signalCycleLength.Value; set => _signalCycleLength.Value = value; }
public int TradeCycleLength { get => _tradeCycleLength.Value; set => _tradeCycleLength.Value = value; }
public int TrendCycleLength { get => _trendCycleLength.Value; set => _trendCycleLength.Value = value; }
public CloseTrigger CloseTrigger1 { get => _closeTrigger1.Value; set => _closeTrigger1.Value = value; }
public CloseTrigger CloseTrigger2 { get => _closeTrigger2.Value; set => _closeTrigger2.Value = value; }

public HurstFutureLinesOfDemarcationStrategy()
{
_smoothFld = Param(nameof(SmoothFld), false)
.SetDisplay("Smooth FLD", "Use smoothing for FLD", "FLD");
_fldSmoothing = Param(nameof(FldSmoothing), 5)
.SetDisplay("FLD Smoothing", "SMA length for FLD smoothing", "FLD");
_signalCycleLength = Param(nameof(SignalCycleLength), 5)
.SetDisplay("Signal Cycle Length", "Quarter cycle length", "Cycles");
_tradeCycleLength = Param(nameof(TradeCycleLength), 20)
.SetDisplay("Trade Cycle Length", "Trade cycle length", "Cycles");
_trendCycleLength = Param(nameof(TrendCycleLength), 80)
.SetDisplay("Trend Cycle Length", "Trend cycle length", "Cycles");
_closeTrigger1 = Param(nameof(CloseTrigger1), CloseTrigger.Price)
.SetDisplay("Close Trigger 1", "First value for exit cross", "Exit");
_closeTrigger2 = Param(nameof(CloseTrigger2), CloseTrigger.Trade)
.SetDisplay("Close Trigger 2", "Second value for exit cross", "Exit");
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();

_signalQueue.Clear();
_tradeQueue.Clear();
_trendQueue.Clear();
_signal = _trade = _trend = null;
_prevPrice = _prevSignal = null;
_prevClose1 = _prevClose2 = null;
_state = 0;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var length = SmoothFld ? FldSmoothing : 1;
_sma = new SimpleMovingAverage { Length = length };

_signalOffset = (int)Math.Round(SignalCycleLength / 2m);
_tradeOffset = (int)Math.Round(TradeCycleLength / 2m);
_trendOffset = (int)Math.Round(TrendCycleLength / 2m);

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(ProcessCandle)
.Start();
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

var price = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
var fld = _sma.Process(price, candle.OpenTime, true).ToDecimal();

_signalQueue.Enqueue(fld);
_tradeQueue.Enqueue(fld);
_trendQueue.Enqueue(fld);

_signal = _signalQueue.Count > _signalOffset ? _signalQueue.Dequeue() : null;
_trade = _tradeQueue.Count > _tradeOffset ? _tradeQueue.Dequeue() : null;
_trend = _trendQueue.Count > _trendOffset ? _trendQueue.Dequeue() : null;

if (_signal.HasValue && _trade.HasValue && _trend.HasValue)
{
UpdateState(price, _signal.Value, _trade.Value, _trend.Value);

if (_prevPrice.HasValue && _prevSignal.HasValue)
{
var crossUp = _prevPrice <= _prevSignal && price > _signal;
var crossDown = _prevPrice >= _prevSignal && price < _signal;

if (crossUp && _state == 1)
BuyMarket();
else if (crossDown && _state == 6)
SellMarket();
}

var close1 = GetTriggerValue(CloseTrigger1, price, _signal, _trade, _trend);
var close2 = GetTriggerValue(CloseTrigger2, price, _signal, _trade, _trend);

if (close1.HasValue && close2.HasValue && _prevClose1.HasValue && _prevClose2.HasValue)
{
var crossUnder = _prevClose1 >= _prevClose2 && close1.Value < close2.Value;
var crossOver = _prevClose1 <= _prevClose2 && close1.Value > close2.Value;

if (Position > 0 && crossUnder)
ClosePosition();
else if (Position < 0 && crossOver)
ClosePosition();
}

_prevClose1 = close1;
_prevClose2 = close2;
}

_prevPrice = price;
_prevSignal = _signal;
}

private static decimal? GetTriggerValue(CloseTrigger trigger, decimal price, decimal? signal, decimal? trade, decimal? trend)
{
return trigger switch
{
CloseTrigger.Price => price,
CloseTrigger.Signal => signal,
CloseTrigger.Trade => trade,
CloseTrigger.Trend => trend,
_ => null,
};
}

private void UpdateState(decimal price, decimal signal, decimal trade, decimal trend)
{
if (signal > trade && trade > trend)
_state = 1;
if (_state == 1 && price < signal)
_state = 2;
if (signal < trade && trade > trend)
_state = 3;
if (_state == 3 && price < signal)
_state = 4;
if (signal < trade && trade < trend)
_state = 5;
if (_state == 5 && price < signal)
_state = 6;
if (signal > trade && trade < trend)
_state = 7;
if (_state == 7 && price < signal)
_state = 8;
}
}
