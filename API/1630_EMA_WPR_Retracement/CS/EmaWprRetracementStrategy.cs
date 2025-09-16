using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Williams %R strategy with EMA trend filter and retracement gating.
/// </summary>
public class EmaWprRetracementStrategy : Strategy
{
private readonly StrategyParam<bool> _useEmaTrend;
private readonly StrategyParam<int> _barsInTrend;
private readonly StrategyParam<int> _emaTrend;
private readonly StrategyParam<int> _wprPeriod;
private readonly StrategyParam<decimal> _wprRetracement;
private readonly StrategyParam<bool> _useWprExit;
private readonly StrategyParam<decimal> _orderVolume;
private readonly StrategyParam<int> _maxTrades;
private readonly StrategyParam<decimal> _stopLoss;
private readonly StrategyParam<decimal> _takeProfit;
private readonly StrategyParam<bool> _useTrailingStop;
private readonly StrategyParam<decimal> _trailingStop;
private readonly StrategyParam<bool> _useUnprofitExit;
private readonly StrategyParam<int> _maxUnprofitBars;
private readonly StrategyParam<DataType> _candleType;

private readonly WilliamsR _wpr = new();
private readonly ExponentialMovingAverage _ema = new();
private readonly Queue<decimal> _emaValues = new();

private bool _canBuy = true;
private bool _canSell = true;
private int _unprofitBars;
private decimal _avgEntryPrice;

public bool UseEmaTrend { get => _useEmaTrend.Value; set => _useEmaTrend.Value = value; }
public int BarsInTrend { get => _barsInTrend.Value; set => _barsInTrend.Value = value; }
public int EmaTrend { get => _emaTrend.Value; set => _emaTrend.Value = value; }
public int WprPeriod { get => _wprPeriod.Value; set => _wprPeriod.Value = value; }
public decimal WprRetracement { get => _wprRetracement.Value; set => _wprRetracement.Value = value; }
public bool UseWprExit { get => _useWprExit.Value; set => _useWprExit.Value = value; }
public decimal OrderVolume { get => _orderVolume.Value; set => _orderVolume.Value = value; }
public int MaxTrades { get => _maxTrades.Value; set => _maxTrades.Value = value; }
public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
public bool UseTrailingStop { get => _useTrailingStop.Value; set => _useTrailingStop.Value = value; }
public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
public bool UseUnprofitExit { get => _useUnprofitExit.Value; set => _useUnprofitExit.Value = value; }
public int MaxUnprofitBars { get => _maxUnprofitBars.Value; set => _maxUnprofitBars.Value = value; }
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

public EmaWprRetracementStrategy()
{
_useEmaTrend = Param(nameof(UseEmaTrend), true)
.SetDisplay("Use EMA Trend", "Filter trades by EMA trend", "Trend");

_barsInTrend = Param(nameof(BarsInTrend), 1)
.SetGreaterThanZero()
.SetDisplay("Bars In Trend", "Required consecutive bars", "Trend");

_emaTrend = Param(nameof(EmaTrend), 144)
.SetGreaterThanZero()
.SetDisplay("EMA Trend", "EMA period for trend", "Trend");

_wprPeriod = Param(nameof(WprPeriod), 46)
.SetGreaterThanZero()
.SetDisplay("WPR Period", "Williams %R period", "Signals");

_wprRetracement = Param(nameof(WprRetracement), 30m)
.SetGreaterThanZero()
.SetDisplay("WPR Retracement", "Retracement needed for next trade", "Signals");

_useWprExit = Param(nameof(UseWprExit), true)
.SetDisplay("Use WPR Exit", "Exit when WPR leaves extreme", "Exit");

_orderVolume = Param(nameof(OrderVolume), 0.1m)
.SetGreaterThanZero()
.SetDisplay("Order Volume", "Volume per trade", "Position");

_maxTrades = Param(nameof(MaxTrades), 2)
.SetGreaterThanZero()
.SetDisplay("Max Trades", "Maximum concurrent trades", "Position");

_stopLoss = Param(nameof(StopLoss), 50m)
.SetGreaterThanZero()
.SetDisplay("Stop Loss", "Stop loss in price units", "Protection");

_takeProfit = Param(nameof(TakeProfit), 200m)
.SetGreaterThanZero()
.SetDisplay("Take Profit", "Take profit in price units", "Protection");

_useTrailingStop = Param(nameof(UseTrailingStop), false)
.SetDisplay("Use Trailing", "Enable trailing stop", "Protection");

_trailingStop = Param(nameof(TrailingStop), 10m)
.SetGreaterThanZero()
.SetDisplay("Trailing Stop", "Trailing distance", "Protection");

_useUnprofitExit = Param(nameof(UseUnprofitExit), false)
.SetDisplay("Use Unprofit Exit", "Exit if trade not profitable", "Exit");

_maxUnprofitBars = Param(nameof(MaxUnprofitBars), 5)
.SetGreaterThanZero()
.SetDisplay("Max Unprofit Bars", "Bars allowed without profit", "Exit");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");
}

public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
return [(Security, CandleType)];
}

protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_wpr.Length = WprPeriod;
_ema.Length = EmaTrend;

var subscription = SubscribeCandles(CandleType);
subscription.Bind(ProcessCandle).Start();

StartProtection(
takeProfit: new Unit(TakeProfit, UnitTypes.Price),
stopLoss: new Unit(StopLoss, UnitTypes.Price),
trailingStop: UseTrailingStop ? new Unit(TrailingStop, UnitTypes.Price) : null);

Volume = OrderVolume;
}

private enum TrendDir
{
None,
Up,
Down
}

private TrendDir GetTrend()
{
if (_emaValues.Count <= BarsInTrend)
return TrendDir.None;

var arr = _emaValues.ToArray();
var count = 0;
for (var i = 0; i < BarsInTrend; i++)
{
var prev = arr[^ (i + 2)];
var curr = arr[^ (i + 1)];
if (prev > curr)
count--;
else if (prev < curr)
count++;
}

if (count == BarsInTrend)
return TrendDir.Up;
if (count == -BarsInTrend)
return TrendDir.Down;
return TrendDir.None;
}

private void UpdateEntryPrice(decimal price, decimal volumeChange)
{
var posBefore = Position;
var posAfter = posBefore + volumeChange;

if (posBefore == 0 || Math.Sign(posBefore) != Math.Sign(posAfter))
{
_avgEntryPrice = price;
}
else
{
_avgEntryPrice = (_avgEntryPrice * Math.Abs(posBefore) + price * Math.Abs(volumeChange)) / Math.Abs(posAfter);
}
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var wpr = _wpr.Process(candle).ToDecimal();
var emaValue = _ema.Process(candle).ToDecimal();

_emaValues.Enqueue(emaValue);
while (_emaValues.Count > BarsInTrend + 1)
_emaValues.Dequeue();

if (wpr > -100 + WprRetracement)
_canBuy = true;
if (wpr < -WprRetracement)
_canSell = true;

if (Position != 0)
{
var profit = (candle.ClosePrice - _avgEntryPrice) * Position;
_unprofitBars = profit > 0 ? 0 : _unprofitBars + 1;
}
else
{
_unprofitBars = 0;
}

if (Position > 0)
{
if ((UseWprExit && wpr >= -0.01m) || (UseUnprofitExit && _unprofitBars > MaxUnprofitBars))
{
SellMarket(Position);
_avgEntryPrice = 0;
}
}
else if (Position < 0)
{
if ((UseWprExit && wpr <= -99.99m) || (UseUnprofitExit && _unprofitBars > MaxUnprofitBars))
{
BuyMarket(-Position);
_avgEntryPrice = 0;
}
}

var trend = UseEmaTrend ? GetTrend() : TrendDir.None;
var maxPos = MaxTrades * OrderVolume;

if (wpr <= -99.99m && _canBuy && (!UseEmaTrend || trend == TrendDir.Up) && Position < maxPos)
{
BuyMarket(OrderVolume);
_canBuy = false;
UpdateEntryPrice(candle.ClosePrice, OrderVolume);
}
else if (wpr >= -0.01m && _canSell && (!UseEmaTrend || trend == TrendDir.Down) && Position > -maxPos)
{
SellMarket(OrderVolume);
_canSell = false;
UpdateEntryPrice(candle.ClosePrice, -OrderVolume);
}
}
}
