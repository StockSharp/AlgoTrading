using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on support/resistance touch with EMA and RSI filters.
/// </summary>
public class Baseline2Strategy : Strategy
{
private readonly StrategyParam<int> _pivotLength;
private readonly StrategyParam<decimal> _touchBufferPercent;
private readonly StrategyParam<decimal> _stopLossAtrMult;
private readonly StrategyParam<decimal> _takeProfitAtrMult;
private readonly StrategyParam<int> _cooldownBars;
private readonly StrategyParam<int> _maxTradesPerDay;
private readonly StrategyParam<DataType> _candleType;

private ExponentialMovingAverage _emaFast;
private ExponentialMovingAverage _emaSlow;
private RelativeStrengthIndex _rsi;
private AverageTrueRange _atr;
private SimpleMovingAverage _atrAvg;
private SimpleMovingAverage _volumeSma;
private AverageDirectionalIndex _adx;
private Highest _highest;
private Lowest _lowest;

private int _barIndex;
private int _lastTradeBar;
private int _tradesToday;
private DateTime _currentDay;
private decimal _stopPrice;
private decimal _takePrice;

/// <summary>
/// Pivot strength for support/resistance levels.
/// </summary>
public int PivotLength
{
get => _pivotLength.Value;
set => _pivotLength.Value = value;
}

/// <summary>
/// Buffer percentage around support/resistance levels.
/// </summary>
public decimal TouchBufferPercent
{
get => _touchBufferPercent.Value;
set => _touchBufferPercent.Value = value;
}

/// <summary>
/// ATR multiplier for stop loss.
/// </summary>
public decimal StopLossAtrMult
{
get => _stopLossAtrMult.Value;
set => _stopLossAtrMult.Value = value;
}

/// <summary>
/// ATR multiplier for take profit.
/// </summary>
public decimal TakeProfitAtrMult
{
get => _takeProfitAtrMult.Value;
set => _takeProfitAtrMult.Value = value;
}

/// <summary>
/// Cooldown bars between trades.
/// </summary>
public int CooldownBars
{
get => _cooldownBars.Value;
set => _cooldownBars.Value = value;
}

/// <summary>
/// Maximum trades per day.
/// </summary>
public int MaxTradesPerDay
{
get => _maxTradesPerDay.Value;
set => _maxTradesPerDay.Value = value;
}

/// <summary>
/// Candle type used by the strategy.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initializes a new instance of the <see cref="Baseline2Strategy"/> class.
/// </summary>
public Baseline2Strategy()
{
_pivotLength = Param(nameof(PivotLength), 5)
.SetGreaterThanZero()
.SetDisplay("Pivot Length", "Strength of pivot for support/resistance", "General");

_touchBufferPercent = Param(nameof(TouchBufferPercent), 1m)
.SetGreaterOrEqual(0m)
.SetDisplay("Touch Buffer %", "Buffer for support/resistance touch", "General");

_stopLossAtrMult = Param(nameof(StopLossAtrMult), 1m)
.SetGreaterThanZero()
.SetDisplay("Stop Loss ATR Mult", "ATR multiplier for stop loss", "Risk Management");

_takeProfitAtrMult = Param(nameof(TakeProfitAtrMult), 2.2m)
.SetGreaterThanZero()
.SetDisplay("Take Profit ATR Mult", "ATR multiplier for take profit", "Risk Management");

_cooldownBars = Param(nameof(CooldownBars), 2)
.SetGreaterOrEqual(0)
.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Trading");

_maxTradesPerDay = Param(nameof(MaxTradesPerDay), 2)
.SetGreaterThanZero()
.SetDisplay("Max Trades Per Day", "Daily trade limit", "Trading");

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
_barIndex = 0;
_lastTradeBar = int.MinValue;
_tradesToday = 0;
_currentDay = default;
_stopPrice = 0m;
_takePrice = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_emaFast = new ExponentialMovingAverage { Length = 9 };
_emaSlow = new ExponentialMovingAverage { Length = 21 };
_rsi = new RelativeStrengthIndex { Length = 14 };
_atr = new AverageTrueRange { Length = 14 };
_atrAvg = new SimpleMovingAverage { Length = 50 };
_volumeSma = new SimpleMovingAverage { Length = 20 };
_adx = new AverageDirectionalIndex { Length = 14 };
_highest = new Highest { Length = PivotLength };
_lowest = new Lowest { Length = PivotLength };

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx([
_emaFast,
_emaSlow,
_rsi,
_atr,
_atrAvg,
_volumeSma,
_adx,
_highest,
_lowest
], ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _emaFast);
DrawIndicator(area, _emaSlow);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var emaFast = values[0].ToDecimal();
var emaSlow = values[1].ToDecimal();
var rsi = values[2].ToDecimal();
var atr = values[3].ToDecimal();
var atrAvg = values[4].ToDecimal();
var volAvg = values[5].ToDecimal();
var adxVal = (AverageDirectionalIndexValue)values[6];
if (adxVal.MovingAverage is not decimal adx)
return;
var highest = values[7].ToDecimal();
var lowest = values[8].ToDecimal();

var volumeOk = candle.TotalVolume > volAvg * 1.1m;
var emaSpread = Math.Abs(emaFast - emaSlow) / candle.ClosePrice;
var avoidEmaSqueeze = emaSpread > 0.003m;

var touchedSupport = candle.LowPrice <= lowest * (1 + TouchBufferPercent / 100m);
var touchedResistance = candle.HighPrice >= highest * (1 - TouchBufferPercent / 100m);

var time = candle.OpenTime.LocalDateTime;
var avoidTimeWindow = (time.Hour >= 11 && time.Hour <= 13) || (time.Hour >= 15 && time.Minute >= 45);
var avoidMorningShorts = time.Hour == 9 && time.Minute < 30;

var bullSignal = touchedSupport && emaFast > emaSlow && rsi > 50 && volumeOk && adx > 15 && !avoidTimeWindow;
var bearSignal = touchedResistance && emaFast < emaSlow && rsi < 45 && volumeOk && adx > 15 && !avoidTimeWindow && !avoidMorningShorts && avoidEmaSqueeze;

var safeAtr = Math.Min(atr, atrAvg * 1.5m);
var longSl = candle.ClosePrice - safeAtr * StopLossAtrMult;
var longTp = candle.ClosePrice + safeAtr * TakeProfitAtrMult;
var shortSl = candle.ClosePrice + safeAtr * StopLossAtrMult;
var shortTp = candle.ClosePrice - safeAtr * TakeProfitAtrMult;

var rrLongOk = (longTp - candle.ClosePrice) >= 2m * (candle.ClosePrice - longSl);
var rrShortOk = (candle.ClosePrice - shortTp) >= 2m * (shortSl - candle.ClosePrice);

if (_currentDay != candle.OpenTime.Date)
{
_currentDay = candle.OpenTime.Date;
_tradesToday = 0;
}

var canTrade = _barIndex - _lastTradeBar >= CooldownBars;
var canTradeToday = _tradesToday < MaxTradesPerDay;

if (Position > 0)
{
if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
{
SellMarket(Math.Abs(Position));
_lastTradeBar = _barIndex;
}
}
else if (Position < 0)
{
if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
{
BuyMarket(Math.Abs(Position));
_lastTradeBar = _barIndex;
}
}
else if (canTrade && canTradeToday)
{
if (bullSignal && rrLongOk)
{
var volume = Volume + Math.Abs(Position);
BuyMarket(volume);
_stopPrice = longSl;
_takePrice = longTp;
_lastTradeBar = _barIndex;
_tradesToday++;
}
else if (bearSignal && rrShortOk)
{
var volume = Volume + Math.Abs(Position);
SellMarket(volume);
_stopPrice = shortSl;
_takePrice = shortTp;
_lastTradeBar = _barIndex;
_tradesToday++;
}
}

_barIndex++;
}
}

