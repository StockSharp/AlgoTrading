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
/// Dual moving average crossover strategy that reads moving averages from configurable timeframes.
/// Converted from the "Two MA Other TimeFrame Correct Intersection" MQL5 expert advisor.
/// </summary>
public class TwoMAOtherTimeFrameCorrectIntersectionStrategy : Strategy
{
private readonly StrategyParam<decimal> _tradeVolume;
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<DataType> _fastTimeFrame;
private readonly StrategyParam<DataType> _slowTimeFrame;
private readonly StrategyParam<int> _fastLength;
private readonly StrategyParam<int> _fastShift;
private readonly StrategyParam<MovingAverageKinds> _fastMethod;
private readonly StrategyParam<CandlePrice> _fastAppliedPrice;
private readonly StrategyParam<int> _slowLength;
private readonly StrategyParam<int> _slowShift;
private readonly StrategyParam<MovingAverageKinds> _slowMethod;
private readonly StrategyParam<CandlePrice> _slowAppliedPrice;

private readonly Queue<decimal> _fastShiftBuffer = new();
private readonly Queue<decimal> _slowShiftBuffer = new();

private decimal? _fastCurrent;
private decimal? _fastPrevious;
private decimal? _slowCurrent;
private decimal? _slowPrevious;
private bool _fastReady;
private bool _slowReady;

/// <summary>
/// Initializes strategy parameters.
/// </summary>
public TwoMAOtherTimeFrameCorrectIntersectionStrategy()
{
_tradeVolume = Param(nameof(TradeVolume), 0.1m)
.SetDisplay("Trade Volume", "Base volume for market orders", "Orders")
.SetGreaterThanZero();

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
.SetDisplay("Trading Candle Type", "Primary timeframe used for signal evaluation", "General");

_fastTimeFrame = Param(nameof(FastTimeFrame), TimeSpan.FromHours(1).TimeFrame())
.SetDisplay("Fast MA Timeframe", "Timeframe used for the fast moving average", "Indicators");

_slowTimeFrame = Param(nameof(SlowTimeFrame), TimeSpan.FromDays(1).TimeFrame())
.SetDisplay("Slow MA Timeframe", "Timeframe used for the slow moving average", "Indicators");

_fastLength = Param(nameof(FastLength), 12)
.SetDisplay("Fast MA Length", "Number of bars for the fast moving average", "Indicators")
.SetGreaterThanZero();

_fastShift = Param(nameof(FastShift), 0)
.SetDisplay("Fast MA Shift", "Horizontal shift applied to the fast moving average", "Indicators")
.SetNotNegative();

_fastMethod = Param(nameof(FastMethod), MovingAverageKinds.Simple)
.SetDisplay("Fast MA Method", "Smoothing method for the fast moving average", "Indicators");

_fastAppliedPrice = Param(nameof(FastAppliedPrice), CandlePrice.Close)
.SetDisplay("Fast MA Price", "Price source for the fast moving average", "Indicators");

_slowLength = Param(nameof(SlowLength), 12)
.SetDisplay("Slow MA Length", "Number of bars for the slow moving average", "Indicators")
.SetGreaterThanZero();

_slowShift = Param(nameof(SlowShift), 0)
.SetDisplay("Slow MA Shift", "Horizontal shift applied to the slow moving average", "Indicators")
.SetNotNegative();

_slowMethod = Param(nameof(SlowMethod), MovingAverageKinds.Simple)
.SetDisplay("Slow MA Method", "Smoothing method for the slow moving average", "Indicators");

_slowAppliedPrice = Param(nameof(SlowAppliedPrice), CandlePrice.Close)
.SetDisplay("Slow MA Price", "Price source for the slow moving average", "Indicators");
}

/// <summary>
/// Base trade volume used in market orders.
/// </summary>
public decimal TradeVolume
{
get => _tradeVolume.Value;
set => _tradeVolume.Value = value;
}

/// <summary>
/// Primary candle type used for synchronizing trade decisions.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Timeframe used when calculating the fast moving average.
/// </summary>
public DataType FastTimeFrame
{
get => _fastTimeFrame.Value;
set => _fastTimeFrame.Value = value;
}

/// <summary>
/// Timeframe used when calculating the slow moving average.
/// </summary>
public DataType SlowTimeFrame
{
get => _slowTimeFrame.Value;
set => _slowTimeFrame.Value = value;
}

/// <summary>
/// Number of bars used by the fast moving average.
/// </summary>
public int FastLength
{
get => _fastLength.Value;
set => _fastLength.Value = value;
}

/// <summary>
/// Number of bars used by the slow moving average.
/// </summary>
public int SlowLength
{
get => _slowLength.Value;
set => _slowLength.Value = value;
}

/// <summary>
/// Shift applied to the fast moving average output.
/// </summary>
public int FastShift
{
get => _fastShift.Value;
set => _fastShift.Value = value;
}

/// <summary>
/// Shift applied to the slow moving average output.
/// </summary>
public int SlowShift
{
get => _slowShift.Value;
set => _slowShift.Value = value;
}

/// <summary>
/// Smoothing method used for the fast moving average.
/// </summary>
public MovingAverageKinds FastMethod
{
get => _fastMethod.Value;
set => _fastMethod.Value = value;
}

/// <summary>
/// Smoothing method used for the slow moving average.
/// </summary>
public MovingAverageKinds SlowMethod
{
get => _slowMethod.Value;
set => _slowMethod.Value = value;
}

/// <summary>
/// Price source used by the fast moving average.
/// </summary>
public CandlePrice FastAppliedPrice
{
get => _fastAppliedPrice.Value;
set => _fastAppliedPrice.Value = value;
}

/// <summary>
/// Price source used by the slow moving average.
/// </summary>
public CandlePrice SlowAppliedPrice
{
get => _slowAppliedPrice.Value;
set => _slowAppliedPrice.Value = value;
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
return
[
(Security, CandleType),
(Security, FastTimeFrame),
(Security, SlowTimeFrame)
];
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();

_fastShiftBuffer.Clear();
_slowShiftBuffer.Clear();
_fastCurrent = null;
_slowCurrent = null;
_fastPrevious = null;
_slowPrevious = null;
_fastReady = false;
_slowReady = false;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

Volume = TradeVolume;
StartProtection();

_fastShiftBuffer.Clear();
_slowShiftBuffer.Clear();

var fastMa = CreateMovingAverage(FastMethod, Math.Max(1, FastLength), FastAppliedPrice);
var slowMa = CreateMovingAverage(SlowMethod, Math.Max(1, SlowLength), SlowAppliedPrice);

var fastSubscription = SubscribeCandles(FastTimeFrame);
fastSubscription
.Bind(fastMa, (candle, fastValue) =>
{
if (candle.State != CandleStates.Finished)
return;

if (!fastMa.IsFormed)
return;

var shifted = ApplyShift(_fastShiftBuffer, fastValue, FastShift);
_fastCurrent = shifted;
_fastReady = true;
})
.Start();

var slowSubscription = SubscribeCandles(SlowTimeFrame);
slowSubscription
.Bind(slowMa, (candle, slowValue) =>
{
if (candle.State != CandleStates.Finished)
return;

if (!slowMa.IsFormed)
return;

var shifted = ApplyShift(_slowShiftBuffer, slowValue, SlowShift);
_slowCurrent = shifted;
_slowReady = true;
})
.Start();

var tradingSubscription = SubscribeCandles(CandleType);
tradingSubscription
.Bind(candle =>
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (!_fastReady || !_slowReady)
return;

if (_fastCurrent is not decimal fastValue || _slowCurrent is not decimal slowValue)
return;

if (_fastPrevious is not decimal prevFast || _slowPrevious is not decimal prevSlow)
{
_fastPrevious = fastValue;
_slowPrevious = slowValue;
return;
}

if (prevFast < prevSlow && fastValue > slowValue)
{
EnterLong(candle, fastValue, slowValue);
}
else if (prevFast > prevSlow && fastValue < slowValue)
{
EnterShort(candle, fastValue, slowValue);
}

_fastPrevious = fastValue;
_slowPrevious = slowValue;
})
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, tradingSubscription);
DrawIndicator(area, fastMa);
DrawIndicator(area, slowMa);
DrawOwnTrades(area);
}
}

private void EnterLong(ICandleMessage candle, decimal fastValue, decimal slowValue)
{
if (Position > 0)
return;

var coveringVolume = Position < 0 ? Math.Abs(Position) : 0m;
var orderVolume = Volume + coveringVolume;

if (orderVolume <= 0)
return;

BuyMarket(orderVolume);
LogInfo($"Fast MA crossed above slow MA at {candle.OpenTime:O}. Fast={fastValue}, Slow={slowValue}");
}

private void EnterShort(ICandleMessage candle, decimal fastValue, decimal slowValue)
{
if (Position < 0)
return;

var coveringVolume = Position > 0 ? Math.Abs(Position) : 0m;
var orderVolume = Volume + coveringVolume;

if (orderVolume <= 0)
return;

SellMarket(orderVolume);
LogInfo($"Fast MA crossed below slow MA at {candle.OpenTime:O}. Fast={fastValue}, Slow={slowValue}");
}

private static decimal ApplyShift(Queue<decimal> buffer, decimal currentValue, int shift)
{
if (shift <= 0)
return currentValue;

var shiftedValue = buffer.Count < shift ? currentValue : buffer.Peek();

buffer.Enqueue(currentValue);

if (buffer.Count > shift)
buffer.Dequeue();

return shiftedValue;
}

private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageKinds kind, int length, CandlePrice price)
{
return kind switch
{
MovingAverageKinds.Simple => new SimpleMovingAverage { Length = length, CandlePrice = price },
MovingAverageKinds.Exponential => new ExponentialMovingAverage { Length = length, CandlePrice = price },
MovingAverageKinds.Smoothed => new SmoothedMovingAverage { Length = length, CandlePrice = price },
MovingAverageKinds.LinearWeighted => new WeightedMovingAverage { Length = length, CandlePrice = price },
_ => new SimpleMovingAverage { Length = length, CandlePrice = price }
};
}

/// <summary>
/// Moving average types supported by the strategy.
/// Matches the available options in the original MQL5 expert advisor.
/// </summary>
public enum MovingAverageKinds
{
Simple,
Exponential,
Smoothed,
LinearWeighted
}
}

