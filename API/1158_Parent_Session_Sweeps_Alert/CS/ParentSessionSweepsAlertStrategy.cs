using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Detects sweeps of the previous session's range and trades in the opposite direction.
/// </summary>
public class ParentSessionSweepsAlertStrategy : Strategy
{
private readonly StrategyParam<decimal> _minRiskReward;
private readonly StrategyParam<bool> _useCandleFilter;
private readonly StrategyParam<DataType> _candleType;

private decimal? _prevHigh;
private decimal? _prevLow;
private decimal _sessionHigh;
private decimal _sessionLow;
private DateTimeOffset _currentSessionDate;

private decimal? _stopPrice;
private decimal? _targetPrice;
private bool _isLong;

/// <summary>
/// Minimum risk reward ratio.
/// </summary>
public decimal MinRiskReward
{
get => _minRiskReward.Value;
set => _minRiskReward.Value = value;
}

/// <summary>
/// Require candle close back inside previous range after sweep.
/// </summary>
public bool UseCandleFilter
{
get => _useCandleFilter.Value;
set => _useCandleFilter.Value = value;
}

/// <summary>
/// Candle type for processing.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initialize <see cref="ParentSessionSweepsAlertStrategy"/>.
/// </summary>
public ParentSessionSweepsAlertStrategy()
{
_minRiskReward = Param(nameof(MinRiskReward), 1m)
.SetDisplay("Min RR", "Minimum risk reward ratio", "General")
.SetGreaterOrEqual(0.1m);

_useCandleFilter = Param(nameof(UseCandleFilter), true)
.SetDisplay("Use Candle Filter", "Require candle close inside range", "General");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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

_prevHigh = null;
_prevLow = null;
_sessionHigh = 0m;
_sessionLow = 0m;
_currentSessionDate = default;
_stopPrice = null;
_targetPrice = null;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(ProcessCandle)
.Start();

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

var date = candle.OpenTime.Date;

if (_currentSessionDate != date)
{
_prevHigh = _sessionHigh != 0m ? _sessionHigh : _prevHigh;
_prevLow = _sessionLow != 0m ? _sessionLow : _prevLow;
_sessionHigh = candle.HighPrice;
_sessionLow = candle.LowPrice;
_currentSessionDate = date;
return;
}

_sessionHigh = Math.Max(_sessionHigh, candle.HighPrice);
_sessionLow = Math.Min(_sessionLow, candle.LowPrice);

if (Position == 0 && _prevHigh is decimal ph && _prevLow is decimal pl)
{
if (candle.HighPrice > ph && (!UseCandleFilter || candle.ClosePrice < ph))
{
EnterShort(candle);
}
else if (candle.LowPrice < pl && (!UseCandleFilter || candle.ClosePrice > pl))
{
EnterLong(candle);
}
}
else if (Position != 0 && _stopPrice is decimal stop && _targetPrice is decimal target)
{
if (_isLong)
{
if (candle.LowPrice <= stop || candle.HighPrice >= target)
{
SellMarket(Position);
_stopPrice = null;
_targetPrice = null;
}
}
else
{
if (candle.HighPrice >= stop || candle.LowPrice <= target)
{
BuyMarket(-Position);
_stopPrice = null;
_targetPrice = null;
}
}
}
}

private void EnterLong(ICandleMessage candle)
{
var entry = candle.ClosePrice;
var stop = candle.LowPrice;
var risk = entry - stop;
var target = entry + risk * MinRiskReward;

BuyMarket();

_isLong = true;
_stopPrice = stop;
_targetPrice = target;

LogInfo($"Bullish setup at {candle.OpenTime:O}. Entry={entry}, Stop={stop}, Target={target}");
}

private void EnterShort(ICandleMessage candle)
{
var entry = candle.ClosePrice;
var stop = candle.HighPrice;
var risk = stop - entry;
var target = entry - risk * MinRiskReward;

SellMarket();

_isLong = false;
_stopPrice = stop;
_targetPrice = target;

LogInfo($"Bearish setup at {candle.OpenTime:O}. Entry={entry}, Stop={stop}, Target={target}");
}
}
