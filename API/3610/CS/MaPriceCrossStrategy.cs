using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the MA Price Cross MetaTrader 4 strategy to StockSharp.
/// Generates entries when the selected moving average crosses the current price inside a trading window.
/// </summary>
public class MaPriceCrossStrategy : Strategy
{
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<int> _maPeriod;
private readonly StrategyParam<MovingAverageMethod> _maMethod;
private readonly StrategyParam<AppliedPrice> _priceType;
private readonly StrategyParam<TimeSpan> _startTime;
private readonly StrategyParam<TimeSpan> _stopTime;
private readonly StrategyParam<decimal> _stopLossPoints;
private readonly StrategyParam<decimal> _takeProfitPoints;
private readonly StrategyParam<decimal> _orderVolume;

private IIndicator _movingAverage = null!;
private decimal? _previousAverage;

/// <summary>
/// Initializes a new instance of the <see cref="MaPriceCrossStrategy"/> class.
/// </summary>
public MaPriceCrossStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Timeframe used to feed the strategy", "General");

_maPeriod = Param(nameof(MaPeriod), 160)
.SetGreaterThanZero()
.SetDisplay("MA Period", "Number of bars used for the moving average", "Moving Average");

_maMethod = Param(nameof(MaMethod), MovingAverageMethod.Simple)
.SetDisplay("MA Method", "Moving average calculation method", "Moving Average")
.SetCanOptimize(true);

_priceType = Param(nameof(PriceType), AppliedPrice.Close)
.SetDisplay("Applied Price", "Price source forwarded to the moving average", "Moving Average")
.SetCanOptimize(true);

_startTime = Param(nameof(StartTime), new TimeSpan(1, 0, 0))
.SetDisplay("Start Time", "Time of day when order processing becomes active", "Trading Window");

_stopTime = Param(nameof(StopTime), new TimeSpan(22, 0, 0))
.SetDisplay("Stop Time", "Time of day when new orders are blocked", "Trading Window");

_stopLossPoints = Param(nameof(StopLossPoints), 200m)
.SetNonNegative()
.SetDisplay("Stop Loss Points", "Protective stop distance expressed in price points", "Protection")
.SetCanOptimize(true);

_takeProfitPoints = Param(nameof(TakeProfitPoints), 600m)
.SetNonNegative()
.SetDisplay("Take Profit Points", "Target distance expressed in price points", "Protection")
.SetCanOptimize(true);

_orderVolume = Param(nameof(OrderVolume), 0.1m)
.SetGreaterThanZero()
.SetDisplay("Order Volume", "Default volume for market orders", "Orders")
.SetCanOptimize(true);
}

/// <summary>
/// Candle type used to drive calculations.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Length of the moving average window.
/// </summary>
public int MaPeriod
{
get => _maPeriod.Value;
set => _maPeriod.Value = value;
}

/// <summary>
/// Moving average calculation method.
/// </summary>
public MovingAverageMethod MaMethod
{
get => _maMethod.Value;
set => _maMethod.Value = value;
}

/// <summary>
/// Price source used by the moving average.
/// </summary>
public AppliedPrice PriceType
{
get => _priceType.Value;
set => _priceType.Value = value;
}

/// <summary>
/// Time of day when trading becomes active.
/// </summary>
public TimeSpan StartTime
{
get => _startTime.Value;
set => _startTime.Value = value;
}

/// <summary>
/// Time of day when new entries are disallowed.
/// </summary>
public TimeSpan StopTime
{
get => _stopTime.Value;
set => _stopTime.Value = value;
}

/// <summary>
/// Stop-loss distance expressed in MetaTrader points.
/// </summary>
public decimal StopLossPoints
{
get => _stopLossPoints.Value;
set => _stopLossPoints.Value = value;
}

/// <summary>
/// Take-profit distance expressed in MetaTrader points.
/// </summary>
public decimal TakeProfitPoints
{
get => _takeProfitPoints.Value;
set => _takeProfitPoints.Value = value;
}

/// <summary>
/// Volume submitted with new market orders.
/// </summary>
public decimal OrderVolume
{
get => _orderVolume.Value;
set => _orderVolume.Value = value;
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
_previousAverage = null;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_movingAverage = CreateMovingAverage(MaMethod, MaPeriod);

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(ProcessCandle)
.Start();

Volume = OrderVolume;

var stopLoss = ToPriceDistance(StopLossPoints);
var takeProfit = ToPriceDistance(TakeProfitPoints);

StartProtection(
stopLoss: stopLoss > 0m ? new Unit(stopLoss, UnitTypes.Price) : null,
takeProfit: takeProfit > 0m ? new Unit(takeProfit, UnitTypes.Price) : null);
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

var currentPrice = GetAppliedPrice(candle, PriceType);
var maValue = _movingAverage.Process(currentPrice, candle.OpenTime, true);

if (!maValue.IsFinal || !_movingAverage.IsFormed)
return;

var currentAverage = maValue.GetValue<decimal>();

if (_previousAverage is null)
{
_previousAverage = currentAverage;
return;
}

var timeOfDay = candle.CloseTime.TimeOfDay;

if (!IsWithinTradingWindow(timeOfDay) || !IsFormedAndOnlineAndAllowTrading())
{
_previousAverage = currentAverage;
return;
}

var previousAverage = _previousAverage.Value;

var buySignal = previousAverage < currentPrice && currentAverage > currentPrice;
var sellSignal = previousAverage > currentPrice && currentAverage < currentPrice;

if (buySignal && Position <= 0)
{
if (Position < 0)
BuyMarket(Math.Abs(Position));

BuyMarket();
}
else if (sellSignal && Position >= 0)
{
if (Position > 0)
SellMarket(Position);

SellMarket();
}

_previousAverage = currentAverage;
}

private bool IsWithinTradingWindow(TimeSpan time)
{
var start = StartTime;
var stop = StopTime;

if (start == stop)
return true;

if (start < stop)
return time >= start && time < stop;

return time >= start || time < stop;
}

private decimal ToPriceDistance(decimal points)
{
var priceStep = Security?.PriceStep ?? 0m;

if (priceStep <= 0m)
return points;

return points * priceStep;
}

private static IIndicator CreateMovingAverage(MovingAverageMethod method, int length)
{
return method switch
{
MovingAverageMethod.Simple => new SimpleMovingAverage { Length = length },
MovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = length },
MovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = length },
MovingAverageMethod.LinearWeighted => new WeightedMovingAverage { Length = length },
_ => new SimpleMovingAverage { Length = length }
};
}

private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPrice priceType)
{
return priceType switch
{
AppliedPrice.Open => candle.OpenPrice,
AppliedPrice.High => candle.HighPrice,
AppliedPrice.Low => candle.LowPrice,
AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
AppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
AppliedPrice.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice * 2m) / 4m,
_ => candle.ClosePrice
};
}
}

/// <summary>
/// Moving average calculation methods compatible with MetaTrader options.
/// </summary>
public enum MovingAverageMethod
{
/// <summary>Simple moving average.</summary>
Simple,

/// <summary>Exponential moving average.</summary>
Exponential,

/// <summary>Smoothed moving average.</summary>
Smoothed,

/// <summary>Linear weighted moving average.</summary>
LinearWeighted
}

/// <summary>
/// Price sources that can feed the moving average calculation.
/// </summary>
public enum AppliedPrice
{
/// <summary>Use the candle close price.</summary>
Close,

/// <summary>Use the candle open price.</summary>
Open,

/// <summary>Use the candle high price.</summary>
High,

/// <summary>Use the candle low price.</summary>
Low,

/// <summary>Average of high and low prices.</summary>
Median,

/// <summary>Average of high, low, and close prices.</summary>
Typical,

/// <summary>Weighted average giving double weight to the close price.</summary>
Weighted
}
