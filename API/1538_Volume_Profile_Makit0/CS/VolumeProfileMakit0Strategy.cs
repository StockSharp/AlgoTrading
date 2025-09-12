using System;
using System.Collections.Generic;

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
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

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

// start new session on date change
if (_currentSession != candle.OpenTime.Date)
{
_currentSession = candle.OpenTime.Date;
_sessionHigh = candle.HighPrice;
_sessionLow = candle.LowPrice;
_pocPrice = candle.ClosePrice;
_maxVolume = candle.TotalVolume;
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

if (candle.ClosePrice > _pocPrice && Position <= 0)
BuyMarket(Volume + Math.Abs(Position));
else if (candle.ClosePrice < _pocPrice && Position >= 0)
SellMarket(Volume + Math.Abs(Position));

if (Position > 0 && candle.ClosePrice < _sessionMid)
SellMarket(Position);
else if (Position < 0 && candle.ClosePrice > _sessionMid)
BuyMarket(Math.Abs(Position));
}
}
