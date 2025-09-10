using StockSharp.Algo.Strategies;
using StockSharp.Algo.Indicators;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Channel breakout strategy with Kelly ratio position sizing.
/// </summary>
public class BuiltInKellyRatioStrategy : Strategy
{
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<int> _length;
private readonly StrategyParam<decimal> _multiplier;
private readonly StrategyParam<int> _atrLength;
private readonly StrategyParam<bool> _useEma;
private readonly StrategyParam<bool> _useKelly;
private readonly StrategyParam<bool> _useTakeProfit;
private readonly StrategyParam<bool> _useStopLoss;
private readonly StrategyParam<decimal> _takeProfit;
private readonly StrategyParam<decimal> _stopLoss;

private MovingAverage _ma;
private AverageTrueRange _atr;

private decimal _prevClose;
private decimal _prevUpper;
private decimal _prevLower;

private decimal _entryPrice;
private decimal _takeProfitPrice;
private decimal _stopLossPrice;
private decimal _positionQty;
private bool _isLong;

private int _winTrades;
private int _lossTrades;
private decimal _grossProfit;
private decimal _grossLoss;
private decimal _kelly;
private decimal _equity;

public BuiltInKellyRatioStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Type of candles to use", "General");

_length = Param(nameof(Length), 20)
.SetDisplay("MA Length", "Moving average length", "Channel")
.SetCanOptimize(true)
.SetOptimize(10, 40, 5);

_multiplier = Param(nameof(Multiplier), 1m)
.SetDisplay("Multiplier", "Range multiplier", "Channel")
.SetCanOptimize(true)
.SetOptimize(0.5m, 2m, 0.5m);

_atrLength = Param(nameof(AtrLength), 10)
.SetDisplay("ATR Length", "ATR period", "Channel")
.SetCanOptimize(true)
.SetOptimize(5, 20, 5);

_useEma = Param(nameof(UseEma), true)
.SetDisplay("Use EMA", "Use exponential moving average", "Channel");

_useKelly = Param(nameof(UseKelly), true)
.SetDisplay("Use Kelly", "Use Kelly ratio for position sizing", "Kelly");

_useTakeProfit = Param(nameof(UseTakeProfit), false)
.SetDisplay("Use Take Profit", "Enable take profit", "Protection");

_useStopLoss = Param(nameof(UseStopLoss), false)
.SetDisplay("Use Stop Loss", "Enable stop loss", "Protection");

_takeProfit = Param(nameof(TakeProfit), 10m)
.SetDisplay("Take Profit (%)", "Take profit percent", "Protection");

_stopLoss = Param(nameof(StopLoss), 1m)
.SetDisplay("Stop Loss (%)", "Stop loss percent", "Protection");
}

public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
public int Length { get => _length.Value; set => _length.Value = value; }
public decimal Multiplier { get => _multiplier.Value; set => _multiplier.Value = value; }
public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
public bool UseEma { get => _useEma.Value; set => _useEma.Value = value; }
public bool UseKelly { get => _useKelly.Value; set => _useKelly.Value = value; }
public bool UseTakeProfit { get => _useTakeProfit.Value; set => _useTakeProfit.Value = value; }
public bool UseStopLoss { get => _useStopLoss.Value; set => _useStopLoss.Value = value; }
public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
=> [(Security, CandleType)];

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();

_prevClose = _prevUpper = _prevLower = 0m;
_entryPrice = _takeProfitPrice = _stopLossPrice = 0m;
_positionQty = 0m;
_isLong = default;

_winTrades = _lossTrades = 0;
_grossProfit = _grossLoss = 0m;
_kelly = 0m;
_equity = 10000m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_ma = UseEma
? new ExponentialMovingAverage { Length = Length }
: new SimpleMovingAverage { Length = Length };

_atr = new AverageTrueRange { Length = AtrLength };

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(_ma, _atr, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _ma);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal ma, decimal atr)
{
if (candle.State != CandleStates.Finished)
return;

var upper = ma + atr * Multiplier;
var lower = ma - atr * Multiplier;

var crossUpper = _prevClose != 0m && _prevClose <= _prevUpper && candle.ClosePrice > upper;
var crossLower = _prevClose != 0m && _prevClose >= _prevLower && candle.ClosePrice < lower;

if (crossUpper && Position <= 0 && IsFormedAndOnlineAndAllowTrading())
{
var qty = CalculateVolume(candle.ClosePrice);
_entryPrice = candle.ClosePrice;
_positionQty = qty;
_isLong = true;
if (UseTakeProfit)
_takeProfitPrice = _entryPrice * (1 + TakeProfit / 100m);
if (UseStopLoss)
_stopLossPrice = _entryPrice * (1 - StopLoss / 100m);
BuyMarket(qty);
}
else if (crossLower && Position >= 0 && IsFormedAndOnlineAndAllowTrading())
{
var qty = CalculateVolume(candle.ClosePrice);
_entryPrice = candle.ClosePrice;
_positionQty = qty;
_isLong = false;
if (UseTakeProfit)
_takeProfitPrice = _entryPrice * (1 - TakeProfit / 100m);
if (UseStopLoss)
_stopLossPrice = _entryPrice * (1 + StopLoss / 100m);
SellMarket(qty);
}

if (Position > 0)
{
if (UseTakeProfit && candle.HighPrice >= _takeProfitPrice)
{
SellMarket(Position);
UpdateStats(_takeProfitPrice, true);
}
else if (UseStopLoss && candle.LowPrice <= _stopLossPrice)
{
SellMarket(Position);
UpdateStats(_stopLossPrice, true);
}
}
else if (Position < 0)
{
var absPos = Math.Abs(Position);
if (UseTakeProfit && candle.LowPrice <= _takeProfitPrice)
{
BuyMarket(absPos);
UpdateStats(_takeProfitPrice, false);
}
else if (UseStopLoss && candle.HighPrice >= _stopLossPrice)
{
BuyMarket(absPos);
UpdateStats(_stopLossPrice, false);
}
}

_prevClose = candle.ClosePrice;
_prevUpper = upper;
_prevLower = lower;
}

private decimal CalculateVolume(decimal price)
{
var qty = 10000m / price;
if (UseKelly && _kelly > 0)
qty = (_equity * _kelly) / price;
return qty;
}

private void UpdateStats(decimal exitPrice, bool wasLong)
{
var volume = _positionQty;
if (volume <= 0)
return;

var pnl = wasLong ? (exitPrice - _entryPrice) * volume : (_entryPrice - exitPrice) * volume;
_equity += pnl;

if (pnl > 0)
{
_winTrades++;
_grossProfit += pnl;
}
else if (pnl < 0)
{
_lossTrades++;
_grossLoss += -pnl;
}

var closedTrades = _winTrades + _lossTrades;
if (closedTrades > 0 && _winTrades > 0 && _lossTrades > 0)
{
var krp = (decimal)_winTrades / closedTrades;
var krw = _grossProfit / _winTrades;
var krl = _grossLoss / _lossTrades;
if (krl > 0)
_kelly = krp - (1m - krp) / (krw / krl);
}
else
{
_kelly = 0m;
}

_positionQty = 0m;
}
}

