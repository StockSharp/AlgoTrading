using System;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// US Index First 30m Candle Strategy.
/// </summary>
public class UsIndexFirst30mCandleStrategy : Strategy
{
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<decimal> _riskReward;

private decimal? _firstHigh;
private decimal? _firstLow;
private decimal _carryHigh;
private decimal _carryLow;
private bool _rangeLocked;
private bool _tradedToday;
private DateTime _currentDate;
private decimal _entryPrice;
private decimal _stop;
private decimal _take;

/// <summary>
/// Candle type for trading.
/// </summary>
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

/// <summary>
/// Risk reward ratio.
/// </summary>
public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }

public UsIndexFirst30mCandleStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(10).TimeFrame())
.SetDisplay("Candle Type", "Trading timeframe", "General");

_riskReward = Param(nameof(RiskReward), 1m)
.SetGreaterThanZero()
.SetDisplay("Risk Reward", "Risk reward ratio", "Risk");
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();
_firstHigh = null;
_firstLow = null;
_rangeLocked = false;
_tradedToday = false;
_currentDate = DateTime.MinValue;
_entryPrice = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);
var sub = SubscribeCandles(CandleType);
sub.Bind(ProcessCandle).Start();
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

var date = candle.OpenTime.Date;
if (date != _currentDate)
{
_currentDate = date;
_firstHigh = null;
_firstLow = null;
_rangeLocked = false;
_tradedToday = false;
}

var time = candle.OpenTime.TimeOfDay;
var start = new TimeSpan(9, 30, 0);
var end = new TimeSpan(10, 0, 0);
var sessionEnd = new TimeSpan(16, 0, 0);

if (time >= start && time < end)
{
_firstHigh = _firstHigh is null ? candle.HighPrice : Math.Max(_firstHigh.Value, candle.HighPrice);
_firstLow = _firstLow is null ? candle.LowPrice : Math.Min(_firstLow.Value, candle.LowPrice);
}

if (!_rangeLocked && time >= end && _firstHigh is decimal fh && _firstLow is decimal fl)
{
_rangeLocked = true;
_carryHigh = fh;
_carryLow = fl;
}

if (time > sessionEnd)
return;

if (_rangeLocked && !_tradedToday)
{
if (Position == 0)
{
if (candle.HighPrice >= _carryHigh)
{
BuyMarket();
_entryPrice = candle.ClosePrice;
_stop = _carryLow;
_take = _carryHigh + (_carryHigh - _carryLow) * RiskReward;
_tradedToday = true;
}
else if (candle.LowPrice <= _carryLow)
{
SellMarket();
_entryPrice = candle.ClosePrice;
_stop = _carryHigh;
_take = _carryLow - (_carryHigh - _carryLow) * RiskReward;
_tradedToday = true;
}
}
}

if (Position > 0)
{
if (candle.LowPrice <= _stop || candle.HighPrice >= _take)
SellMarket(Position);
}
else if (Position < 0)
{
if (candle.HighPrice >= _stop || candle.LowPrice <= _take)
BuyMarket(Math.Abs(Position));
}
}
}
