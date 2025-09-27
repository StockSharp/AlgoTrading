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
/// Three moving average crossover strategy with channel-based exits.
/// Opens a long position when the fast and medium moving averages cross above the slow moving average and a short position when they cross below.
/// Optionally uses a Donchian channel to set trailing exit levels and supports fixed take-profit and stop-loss distances.
/// </summary>
public class ThreeMaCrossChannelStrategy : Strategy
{
private readonly StrategyParam<int> _fastLength;
private readonly StrategyParam<int> _mediumLength;
private readonly StrategyParam<int> _slowLength;
private readonly StrategyParam<int> _channelLength;
private readonly StrategyParam<MovingAverageTypeEnum> _fastType;
private readonly StrategyParam<MovingAverageTypeEnum> _mediumType;
private readonly StrategyParam<MovingAverageTypeEnum> _slowType;
private readonly StrategyParam<decimal> _takeProfit;
private readonly StrategyParam<decimal> _stopLoss;
private readonly StrategyParam<bool> _useChannelStop;
private readonly StrategyParam<DataType> _candleType;

private LengthIndicator<decimal> _fastMa = null!;
private LengthIndicator<decimal> _mediumMa = null!;
private LengthIndicator<decimal> _slowMa = null!;
private DonchianChannels _channel = null!;

private bool? _prevFastAboveSlow;
private bool? _prevMediumAboveSlow;
private decimal? _entryPrice;

/// <summary>
/// Length of the fast moving average.
/// </summary>
public int FastLength
{
get => _fastLength.Value;
set => _fastLength.Value = value;
}

/// <summary>
/// Length of the medium moving average.
/// </summary>
public int MediumLength
{
get => _mediumLength.Value;
set => _mediumLength.Value = value;
}

/// <summary>
/// Length of the slow moving average.
/// </summary>
public int SlowLength
{
get => _slowLength.Value;
set => _slowLength.Value = value;
}

/// <summary>
/// Period used for the Donchian channel.
/// </summary>
public int ChannelLength
{
get => _channelLength.Value;
set => _channelLength.Value = value;
}

/// <summary>
/// Moving-average type applied to the fast average.
/// </summary>
public MovingAverageTypeEnum FastType
{
get => _fastType.Value;
set => _fastType.Value = value;
}

/// <summary>
/// Moving-average type applied to the medium average.
/// </summary>
public MovingAverageTypeEnum MediumType
{
get => _mediumType.Value;
set => _mediumType.Value = value;
}

/// <summary>
/// Moving-average type applied to the slow average.
/// </summary>
public MovingAverageTypeEnum SlowType
{
get => _slowType.Value;
set => _slowType.Value = value;
}

/// <summary>
/// Take-profit distance measured in price units.
/// </summary>
public decimal TakeProfit
{
get => _takeProfit.Value;
set => _takeProfit.Value = value;
}

/// <summary>
/// Stop-loss distance measured in price units.
/// </summary>
public decimal StopLoss
{
get => _stopLoss.Value;
set => _stopLoss.Value = value;
}

/// <summary>
/// Enables exits based on Donchian channel boundaries.
/// </summary>
public bool UseChannelStop
{
get => _useChannelStop.Value;
set => _useChannelStop.Value = value;
}

/// <summary>
/// Candle type used for calculations.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initializes a new instance of the <see cref="ThreeMaCrossChannelStrategy"/> class.
/// </summary>
public ThreeMaCrossChannelStrategy()
{
_fastLength = Param(nameof(FastLength), 2)
.SetGreaterThanZero()
.SetDisplay("Fast MA", "Length of the fast moving average", "Moving Averages");

_mediumLength = Param(nameof(MediumLength), 4)
.SetGreaterThanZero()
.SetDisplay("Medium MA", "Length of the medium moving average", "Moving Averages");

_slowLength = Param(nameof(SlowLength), 30)
.SetGreaterThanZero()
.SetDisplay("Slow MA", "Length of the slow moving average", "Moving Averages");

_channelLength = Param(nameof(ChannelLength), 15)
.SetGreaterThanZero()
.SetDisplay("Channel", "Donchian channel lookback period", "Risk Management");

_fastType = Param(nameof(FastType), MovingAverageTypeEnum.EMA)
.SetDisplay("Fast MA Type", "Algorithm used for the fast average", "Moving Averages");

_mediumType = Param(nameof(MediumType), MovingAverageTypeEnum.EMA)
.SetDisplay("Medium MA Type", "Algorithm used for the medium average", "Moving Averages");

_slowType = Param(nameof(SlowType), MovingAverageTypeEnum.EMA)
.SetDisplay("Slow MA Type", "Algorithm used for the slow average", "Moving Averages");

_takeProfit = Param(nameof(TakeProfit), 0m)
.SetDisplay("Take Profit", "Distance to close profitable trades", "Risk Management")
.SetCanOptimize(true)
.SetOptimize(0m, 3m, 0.1m);

_stopLoss = Param(nameof(StopLoss), 0m)
.SetDisplay("Stop Loss", "Distance to limit losses", "Risk Management")
.SetCanOptimize(true)
.SetOptimize(0m, 3m, 0.1m);

_useChannelStop = Param(nameof(UseChannelStop), true)
.SetDisplay("Channel Exit", "Use Donchian channel boundaries for exits", "Risk Management");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Type of candles used by the strategy", "General");
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
_prevFastAboveSlow = null;
_prevMediumAboveSlow = null;
_entryPrice = null;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_fastMa = CreateMovingAverage(FastType, FastLength);
_mediumMa = CreateMovingAverage(MediumType, MediumLength);
_slowMa = CreateMovingAverage(SlowType, SlowLength);
_channel = new DonchianChannels { Length = ChannelLength };

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(_fastMa, _mediumMa, _slowMa, _channel, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _fastMa);
DrawIndicator(area, _mediumMa);
DrawIndicator(area, _slowMa);
DrawIndicator(area, _channel);
DrawOwnTrades(area);
}

StartProtection();
}

private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal mediumValue, decimal slowValue, decimal middleBand, decimal upperBand, decimal lowerBand)
{
if (candle.State != CandleStates.Finished)
return;

if (!_fastMa.IsFormed || !_mediumMa.IsFormed || !_slowMa.IsFormed)
return;

var fastAbove = fastValue > slowValue;
var mediumAbove = mediumValue > slowValue;

var fastCrossUp = _prevFastAboveSlow.HasValue && !_prevFastAboveSlow.Value && fastAbove;
var fastCrossDown = _prevFastAboveSlow.HasValue && _prevFastAboveSlow.Value && !fastAbove;
var mediumCrossUp = _prevMediumAboveSlow.HasValue && !_prevMediumAboveSlow.Value && mediumAbove;
var mediumCrossDown = _prevMediumAboveSlow.HasValue && _prevMediumAboveSlow.Value && !mediumAbove;

_prevFastAboveSlow = fastAbove;
_prevMediumAboveSlow = mediumAbove;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var buySignal = fastAbove && mediumAbove && (fastCrossUp || mediumCrossUp);
var sellSignal = !fastAbove && !mediumAbove && (fastCrossDown || mediumCrossDown);

if (Position > 0)
{
var shouldExit = sellSignal;

if (!shouldExit && _entryPrice.HasValue)
{
if (TakeProfit > 0m && candle.ClosePrice - _entryPrice.Value >= TakeProfit)
shouldExit = true;
if (StopLoss > 0m && _entryPrice.Value - candle.ClosePrice >= StopLoss)
shouldExit = true;
}

if (!shouldExit && UseChannelStop && candle.ClosePrice <= lowerBand)
shouldExit = true;

if (shouldExit)
{
ClosePosition();
_entryPrice = null;
return;
}
}
else if (Position < 0)
{
var shouldExit = buySignal;

if (!shouldExit && _entryPrice.HasValue)
{
if (TakeProfit > 0m && _entryPrice.Value - candle.ClosePrice >= TakeProfit)
shouldExit = true;
if (StopLoss > 0m && candle.ClosePrice - _entryPrice.Value >= StopLoss)
shouldExit = true;
}

if (!shouldExit && UseChannelStop && candle.ClosePrice >= upperBand)
shouldExit = true;

if (shouldExit)
{
ClosePosition();
_entryPrice = null;
return;
}
}

if (buySignal && Position <= 0)
{
var volume = Volume + Math.Abs(Position);
BuyMarket(volume);
_entryPrice = candle.ClosePrice;
}
else if (sellSignal && Position >= 0)
{
var volume = Volume + Math.Abs(Position);
SellMarket(volume);
_entryPrice = candle.ClosePrice;
}
}

private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageTypeEnum type, int length)
{
return type switch
{
MovingAverageTypeEnum.SMA => new SimpleMovingAverage { Length = length },
MovingAverageTypeEnum.EMA => new ExponentialMovingAverage { Length = length },
MovingAverageTypeEnum.SMMA => new SmoothedMovingAverage { Length = length },
MovingAverageTypeEnum.WMA => new WeightedMovingAverage { Length = length },
_ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported moving average type."),
};
}

/// <summary>
/// Moving-average algorithms supported by the strategy.
/// Matches the modes available in the original MetaTrader script.
/// </summary>
public enum MovingAverageTypeEnum
{
/// <summary>
/// Simple moving average.
/// </summary>
SMA,

/// <summary>
/// Exponential moving average.
/// </summary>
EMA,

/// <summary>
/// Smoothed moving average.
/// </summary>
SMMA,

/// <summary>
/// Linear weighted moving average.
/// </summary>
WMA,
}
}

