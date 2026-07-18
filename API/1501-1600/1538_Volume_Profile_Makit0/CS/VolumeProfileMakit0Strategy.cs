using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified volume profile strategy.
/// Tracks session high, low, mid and point of control based on maximum candle volume.
/// Buys when price is above POC and sells when below.
/// </summary>
public class VolumeProfileMakit0Strategy : Strategy
{
private readonly StrategyParam<DataType> _candleType;

private DateTime _currentSession;
private decimal _sessionHigh;
private decimal _sessionLow;
private decimal _sessionMid;
private decimal _pocPrice;
private decimal _maxVolume;
private bool _sessionTradeDone;

/// <summary>
/// Candle type.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initializes <see cref="VolumeProfileMakit0Strategy"/>.
/// </summary>
public VolumeProfileMakit0Strategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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
_currentSession = default;
_sessionHigh = 0;
_sessionLow = 0;
_sessionMid = 0;
_pocPrice = 0;
_maxVolume = 0;
_sessionTradeDone = false;
}

/// <inheritdoc />
protected override void OnStarted2(DateTime time)
{
base.OnStarted2(time);

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(ProcessCandle)
.Start();

StartProtection(
takeProfit: new Unit(2, UnitTypes.Percent),
stopLoss: new Unit(1, UnitTypes.Percent)
);

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

var sessionStart = GetSessionStart(candle.OpenTime.Date);

// start new session on session boundary
if (_currentSession != sessionStart)
{
	if (Position > 0)
		SellMarket(Position);
	else if (Position < 0)
		BuyMarket(Math.Abs(Position));

_currentSession = sessionStart;
_sessionHigh = candle.HighPrice;
_sessionLow = candle.LowPrice;
_sessionMid = candle.ClosePrice;
_pocPrice = candle.ClosePrice;
_maxVolume = candle.TotalVolume;
_sessionTradeDone = false;
return;
}

_sessionHigh = Math.Max(_sessionHigh, candle.HighPrice);
_sessionLow = Math.Min(_sessionLow, candle.LowPrice);
_sessionMid = (_sessionHigh + _sessionLow) / 2m;

if (candle.TotalVolume > _maxVolume)
{
_maxVolume = candle.TotalVolume;
_pocPrice = candle.ClosePrice;
}

if (!IsFormedAndOnlineAndAllowTrading())
return;

var bullishProfile = candle.ClosePrice > _pocPrice && candle.ClosePrice > _sessionMid;
var bearishProfile = candle.ClosePrice < _pocPrice && candle.ClosePrice < _sessionMid;

if (!_sessionTradeDone && Position == 0 && bullishProfile)
{
BuyMarket(Volume);
_sessionTradeDone = true;
}
else if (!_sessionTradeDone && Position == 0 && bearishProfile)
{
SellMarket(Volume);
_sessionTradeDone = true;
}

if (Position > 0 && candle.ClosePrice < _pocPrice && candle.ClosePrice < _sessionMid)
SellMarket(Position);
else if (Position < 0 && candle.ClosePrice > _pocPrice && candle.ClosePrice > _sessionMid)
BuyMarket(Math.Abs(Position));
}

private static DateTime GetSessionStart(DateTime date)
{
	return new DateTime(date.Year, date.Month, 1);
}
}
