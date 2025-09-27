namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy converted from the "FIBOCHANNEL" expert advisor.
/// It combines weighted moving averages, momentum, MACD, and a linear regression slope filter.
/// </summary>
public class FiboChannelLineStrategy : Strategy
{
private readonly StrategyParam<int> _momentumSampleSize;

private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<int> _fastMaPeriod;
private readonly StrategyParam<int> _slowMaPeriod;
private readonly StrategyParam<int> _momentumPeriod;
private readonly StrategyParam<decimal> _momentumThreshold;
private readonly StrategyParam<int> _channelLength;
private readonly StrategyParam<decimal> _slopeThreshold;
private readonly StrategyParam<int> _macdFastPeriod;
private readonly StrategyParam<int> _macdSlowPeriod;
private readonly StrategyParam<int> _macdSignalPeriod;
private readonly StrategyParam<decimal> _takeProfitPercent;
private readonly StrategyParam<decimal> _stopLossPercent;
private readonly StrategyParam<decimal> _equityRiskPercent;

private LinearWeightedMovingAverage _fastMa = null!;
private LinearWeightedMovingAverage _slowMa = null!;
private Momentum _momentum = null!;
private MovingAverageConvergenceDivergenceSignal _macd = null!;
private LinearRegSlope _slope = null!;

private readonly Queue<decimal> _momentumBuffer = new();

/// <summary>
/// Initializes a new instance of the <see cref="FiboChannelLineStrategy"/> class.
/// </summary>
public FiboChannelLineStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
.SetDisplay("Candle Type", "Candle aggregation for calculations.", "General");

_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
.SetGreaterThanZero()
.SetDisplay("Fast LWMA", "Length of the fast linear weighted moving average.", "Trend")
.SetCanOptimize(true)
.SetOptimize(3, 30, 1);

_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
.SetGreaterThanZero()
.SetDisplay("Slow LWMA", "Length of the slow linear weighted moving average.", "Trend")
.SetCanOptimize(true)
.SetOptimize(20, 150, 5);

_momentumPeriod = Param(nameof(MomentumPeriod), 14)
.SetGreaterThanZero()
.SetDisplay("Momentum Period", "Number of bars for the momentum oscillator.", "Momentum")
.SetCanOptimize(true)
.SetOptimize(5, 30, 1);

_momentumSampleSize = Param(nameof(MomentumSampleSize), 3)
.SetGreaterThanZero()
.SetDisplay("Momentum Samples", "Number of recent momentum readings stored.", "Momentum");

_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
.SetGreaterThanZero()
.SetDisplay("Momentum Threshold", "Minimal deviation from the neutral 100 level.", "Momentum")
.SetCanOptimize(true)
.SetOptimize(0.1m, 2m, 0.1m);

_channelLength = Param(nameof(ChannelLength), 50)
.SetGreaterThanZero()
.SetDisplay("Channel Length", "Bars for the linear regression slope filter.", "Channel")
.SetCanOptimize(true)
.SetOptimize(20, 120, 5);

_slopeThreshold = Param(nameof(SlopeThreshold), 0.0m)
.SetDisplay("Slope Threshold", "Minimal slope value to confirm the channel direction.", "Channel")
.SetCanOptimize(true)
.SetOptimize(-0.01m, 0.01m, 0.001m);

_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
.SetGreaterThanZero()
.SetDisplay("MACD Fast", "Fast EMA length inside MACD.", "MACD");

_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
.SetGreaterThanZero()
.SetDisplay("MACD Slow", "Slow EMA length inside MACD.", "MACD");

_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
.SetGreaterThanZero()
.SetDisplay("MACD Signal", "Signal line period for MACD.", "MACD");

_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
.SetGreaterThanZero()
.SetDisplay("Take Profit %", "Protective take profit distance in percent.", "Risk")
.SetCanOptimize(true)
.SetOptimize(0.5m, 5m, 0.5m);

_stopLossPercent = Param(nameof(StopLossPercent), 1m)
.SetGreaterThanZero()
.SetDisplay("Stop Loss %", "Protective stop loss distance in percent.", "Risk")
.SetCanOptimize(true)
.SetOptimize(0.5m, 5m, 0.5m);

_equityRiskPercent = Param(nameof(EquityRiskPercent), 3m)
.SetGreaterThanZero()
.SetDisplay("Equity Risk %", "Maximum equity drawdown before flattening.", "Risk")
.SetCanOptimize(true)
.SetOptimize(1m, 10m, 1m);
}

/// <summary>
/// Type of candles used for calculations.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Fast LWMA length.
/// </summary>
public int FastMaPeriod
{
get => _fastMaPeriod.Value;
set => _fastMaPeriod.Value = value;
}

/// <summary>
/// Slow LWMA length.
/// </summary>
public int SlowMaPeriod
{
get => _slowMaPeriod.Value;
set => _slowMaPeriod.Value = value;
}

/// <summary>
/// Momentum indicator period.
/// </summary>
public int MomentumPeriod
{
get => _momentumPeriod.Value;
set => _momentumPeriod.Value = value;
}

/// <summary>
/// Minimal momentum deviation from the neutral level.
/// </summary>
public decimal MomentumThreshold
{
get => _momentumThreshold.Value;
set => _momentumThreshold.Value = value;
}

/// <summary>
/// Number of recent momentum values kept for confirmation.
/// </summary>
public int MomentumSampleSize
{
get => _momentumSampleSize.Value;
set => _momentumSampleSize.Value = value;
}

/// <summary>
/// Number of bars for the slope calculation.
/// </summary>
public int ChannelLength
{
get => _channelLength.Value;
set => _channelLength.Value = value;
}

/// <summary>
/// Minimal absolute value of the slope to confirm the direction.
/// </summary>
public decimal SlopeThreshold
{
get => _slopeThreshold.Value;
set => _slopeThreshold.Value = value;
}

/// <summary>
/// Fast EMA length inside MACD.
/// </summary>
public int MacdFastPeriod
{
get => _macdFastPeriod.Value;
set => _macdFastPeriod.Value = value;
}

/// <summary>
/// Slow EMA length inside MACD.
/// </summary>
public int MacdSlowPeriod
{
get => _macdSlowPeriod.Value;
set => _macdSlowPeriod.Value = value;
}

/// <summary>
/// Signal line length for MACD.
/// </summary>
public int MacdSignalPeriod
{
get => _macdSignalPeriod.Value;
set => _macdSignalPeriod.Value = value;
}

/// <summary>
/// Take profit distance in percent.
/// </summary>
public decimal TakeProfitPercent
{
get => _takeProfitPercent.Value;
set => _takeProfitPercent.Value = value;
}

/// <summary>
/// Stop loss distance in percent.
/// </summary>
public decimal StopLossPercent
{
get => _stopLossPercent.Value;
set => _stopLossPercent.Value = value;
}

/// <summary>
/// Maximum equity drawdown allowed before flattening.
/// </summary>
public decimal EquityRiskPercent
{
get => _equityRiskPercent.Value;
set => _equityRiskPercent.Value = value;
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

_momentumBuffer.Clear();
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_fastMa = new LinearWeightedMovingAverage
{
Length = FastMaPeriod,
CandlePrice = CandlePrice.Typical
};

_slowMa = new LinearWeightedMovingAverage
{
Length = SlowMaPeriod,
CandlePrice = CandlePrice.Typical
};

_momentum = new Momentum
{
Length = MomentumPeriod
};

_macd = new MovingAverageConvergenceDivergenceSignal
{
Macd =
{
ShortMa = { Length = MacdFastPeriod },
LongMa = { Length = MacdSlowPeriod }
},
SignalMa = { Length = MacdSignalPeriod }
};

_slope = new LinearRegSlope
{
Length = ChannelLength
};

var subscription = SubscribeCandles(CandleType);

subscription
.BindEx(_fastMa, _slowMa, _momentum, _slope, _macd, ProcessCandle)
.Start();

var priceArea = CreateChartArea();
if (priceArea != null)
{
DrawCandles(priceArea, subscription);
DrawIndicator(priceArea, _fastMa);
DrawIndicator(priceArea, _slowMa);
DrawOwnTrades(priceArea);
}

var oscillatorArea = CreateChartArea();
if (oscillatorArea != null)
{
DrawIndicator(oscillatorArea, _momentum);
DrawIndicator(oscillatorArea, _macd);
DrawIndicator(oscillatorArea, _slope);
}

StartProtection(
stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
maxDrawdown: new Unit(EquityRiskPercent, UnitTypes.Percent)
);
}

private void ProcessCandle(
ICandleMessage candle,
IIndicatorValue fastValue,
IIndicatorValue slowValue,
IIndicatorValue momentumValue,
IIndicatorValue slopeValue,
IIndicatorValue macdValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_momentum.IsFormed || !_macd.IsFormed || !_slope.IsFormed)
return;

var fast = fastValue.ToDecimal();
var slow = slowValue.ToDecimal();
var momentum = momentumValue.ToDecimal();
var slope = slopeValue.ToDecimal();

var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
var macdLine = macdTyped.Macd;
var signalLine = macdTyped.Signal;

UpdateMomentumBuffer(momentum);

var hasMomentumImpulse = HasMomentumImpulse();

var isChannelRising = slope >= SlopeThreshold;
var isChannelFalling = slope <= -SlopeThreshold;

var goLong = fast > slow && macdLine > signalLine && hasMomentumImpulse && isChannelRising;
var goShort = fast < slow && macdLine < signalLine && hasMomentumImpulse && isChannelFalling;

if (goLong && Position <= 0)
{
CancelActiveOrders();

var volume = Volume + Math.Abs(Position);
if (volume > 0)
BuyMarket(volume);
}
else if (goShort && Position >= 0)
{
CancelActiveOrders();

var volume = Volume + Math.Abs(Position);
if (volume > 0)
SellMarket(volume);
}
else
{
if (Position > 0 && (!isChannelRising || macdLine <= signalLine))
{
CancelActiveOrders();
ClosePosition();
}
else if (Position < 0 && (!isChannelFalling || macdLine >= signalLine))
{
CancelActiveOrders();
ClosePosition();
}
}

}

private void UpdateMomentumBuffer(decimal momentum)
{
if (_momentumBuffer.Count == MomentumSampleSize)
_momentumBuffer.Dequeue();

_momentumBuffer.Enqueue(momentum);
}

private bool HasMomentumImpulse()
{
if (_momentumBuffer.Count < MomentumSampleSize)
return false;

foreach (var value in _momentumBuffer)
{
var deviation = Math.Abs(value - 100m);
if (deviation >= MomentumThreshold)
return true;
}

return false;
}
}
