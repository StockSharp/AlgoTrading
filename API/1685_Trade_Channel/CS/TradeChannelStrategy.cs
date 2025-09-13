
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trade Channel breakout strategy.
/// Uses Donchian Channel and ATR for stop management.
/// </summary>
public class TradeChannelStrategy : Strategy
{
private readonly StrategyParam<int> _channelPeriod;
private readonly StrategyParam<int> _atrPeriod;
private readonly StrategyParam<decimal> _trailing;
private readonly StrategyParam<DataType> _candleType;

private decimal _prevUpper;
private decimal _prevLower;
private decimal _stopPrice;

/// <summary>Period for price channel.</summary>
public int ChannelPeriod { get => _channelPeriod.Value; set => _channelPeriod.Value = value; }

/// <summary>ATR period.</summary>
public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

/// <summary>Trailing stop value. 0 disables trailing.</summary>
public decimal Trailing { get => _trailing.Value; set => _trailing.Value = value; }

/// <summary>Type of candles used.</summary>
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

/// <summary>
/// Initializes strategy parameters.
/// </summary>
public TradeChannelStrategy()
{
_channelPeriod = Param(nameof(ChannelPeriod), 20)
.SetDisplay("Channel Period", "Donchian channel period", "Indicators")
.SetCanOptimize(true)
.SetOptimize(10, 50, 5);

_atrPeriod = Param(nameof(AtrPeriod), 4)
.SetDisplay("ATR Period", "ATR length for stop calculation", "Indicators")
.SetCanOptimize(true)
.SetOptimize(2, 14, 1);

_trailing = Param(nameof(Trailing), 0m)
.SetDisplay("Trailing", "Trailing stop distance in price units", "Risk");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
.SetDisplay("Candle Type", "Type of candles for processing", "General");
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
_prevUpper = 0m;
_prevLower = 0m;
_stopPrice = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var donchian = new DonchianChannels { Length = ChannelPeriod };
var atr = new ATR { Length = AtrPeriod };

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx(donchian, atr, ProcessCandle)
.Start();

StartProtection();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, donchian);
DrawIndicator(area, atr);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue, IIndicatorValue atrValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var dc = (DonchianChannelsValue)donchianValue;
if (dc.UpperBand is not decimal upper || dc.LowerBand is not decimal lower)
return;

if (!atrValue.IsFinal)
return;

var atr = atrValue.GetValue<decimal>();

// Skip first value to get previous bands
if (_prevUpper == 0m && _prevLower == 0m)
{
_prevUpper = upper;
_prevLower = lower;
return;
}

var pivot = (upper + lower + candle.ClosePrice) / 3m;

// Entry logic
if (Position == 0)
{
var hitUpper = candle.HighPrice >= upper && upper == _prevUpper;
var retraceUpper = candle.ClosePrice < upper && upper == _prevUpper && candle.ClosePrice > pivot;

var hitLower = candle.LowPrice <= lower && lower == _prevLower;
var retraceLower = candle.ClosePrice > lower && lower == _prevLower && candle.ClosePrice < pivot;

if ((hitUpper || retraceUpper) && Position <= 0)
{
var volume = Volume + Math.Abs(Position);
BuyMarket(volume);
_stopPrice = lower - atr;
}
else if ((hitLower || retraceLower) && Position >= 0)
{
var volume = Volume + Math.Abs(Position);
SellMarket(volume);
_stopPrice = upper + atr;
}
}
else if (Position > 0)
{
var hitUpper = candle.HighPrice >= upper && upper == _prevUpper;
if (hitUpper)
{
SellMarket(Position);
}
else
{
if (Trailing > 0m)
{
var newStop = candle.ClosePrice - Trailing;
if (newStop > _stopPrice)
_stopPrice = newStop;
}

if (candle.LowPrice <= _stopPrice)
SellMarket(Position);
}
}
else if (Position < 0)
{
var hitLower = candle.LowPrice <= lower && lower == _prevLower;
if (hitLower)
{
BuyMarket(Math.Abs(Position));
}
else
{
if (Trailing > 0m)
{
var newStop = candle.ClosePrice + Trailing;
if (newStop < _stopPrice)
_stopPrice = newStop;
}

if (candle.HighPrice >= _stopPrice)
BuyMarket(Math.Abs(Position));
}
}

_prevUpper = upper;
_prevLower = lower;
}
}
