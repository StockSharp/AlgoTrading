using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MALR channel breakout strategy.
/// Enters long when price breaks above the upper MALR band and short when breaking below the lower band.
/// </summary>
public class MalrChannelBreakoutStrategy : Strategy
{
private readonly StrategyParam<int> _maPeriod;
private readonly StrategyParam<decimal> _channelReversal;
private readonly StrategyParam<decimal> _channelBreakout;
private readonly StrategyParam<DataType> _candleType;

private SimpleMovingAverage _sma;
private LinearWeightedMovingAverage _lwma;
private StandardDeviation _stdDev;

private decimal? _prevUpper;
private decimal? _prevLower;
private decimal? _prevClose;

/// <summary>
/// Moving average period.
/// </summary>
public int MaPeriod
{
get => _maPeriod.Value;
set => _maPeriod.Value = value;
}

/// <summary>
/// Channel reversal width multiplier.
/// </summary>
public decimal ChannelReversal
{
get => _channelReversal.Value;
set => _channelReversal.Value = value;
}

/// <summary>
/// Additional breakout width multiplier.
/// </summary>
public decimal ChannelBreakout
{
get => _channelBreakout.Value;
set => _channelBreakout.Value = value;
}

/// <summary>
/// Candle type for indicator calculations.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initialize strategy parameters.
/// </summary>
public MalrChannelBreakoutStrategy()
{
_maPeriod = Param(nameof(MaPeriod), 120)
.SetGreaterThanZero()
.SetDisplay("MA", "Moving average period", "General")
.SetCanOptimize(true)
.SetOptimize(50, 200, 10);

_channelReversal = Param(nameof(ChannelReversal), 1.1m)
.SetGreaterThanZero()
.SetDisplay("Reversal", "Channel reversal width", "General")
.SetCanOptimize(true)
.SetOptimize(0.5m, 2m, 0.1m);

_channelBreakout = Param(nameof(ChannelBreakout), 1.1m)
.SetGreaterThanZero()
.SetDisplay("Breakout", "Channel breakout width", "General")
.SetCanOptimize(true)
.SetOptimize(0.5m, 2m, 0.1m);

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle", "Candle type", "General");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
return [(Security, CandleType)];
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_sma = new SimpleMovingAverage { Length = MaPeriod };
_lwma = new LinearWeightedMovingAverage { Length = MaPeriod };
_stdDev = new StandardDeviation { Length = MaPeriod };

var subscription = SubscribeCandles(CandleType);
subscription.Bind(ProcessCandle).Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _sma);
DrawIndicator(area, _lwma);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

var smaValue = _sma.Process(candle.ClosePrice);
var lwmaValue = _lwma.Process(candle.ClosePrice);

if (!smaValue.IsFinal || !lwmaValue.IsFinal)
{
_prevClose = candle.ClosePrice;
return;
}

var ff = 3m * lwmaValue.ToDecimal() - 2m * smaValue.ToDecimal();
var stdValue = _stdDev.Process(candle.ClosePrice - ff);

if (!stdValue.IsFinal)
{
_prevClose = candle.ClosePrice;
_prevUpper = ff;
_prevLower = ff;
return;
}

var std = stdValue.ToDecimal();
var upper = ff + std * (ChannelReversal + ChannelBreakout);
var lower = ff - std * (ChannelReversal + ChannelBreakout);

if (_prevUpper.HasValue && _prevLower.HasValue && _prevClose.HasValue)
{
if (_prevUpper > _prevClose && upper <= candle.ClosePrice && Position <= 0)
BuyMarket();
else if (_prevLower < _prevClose && lower >= candle.ClosePrice && Position >= 0)
SellMarket();
}

_prevUpper = upper;
_prevLower = lower;
_prevClose = candle.ClosePrice;
}
}
