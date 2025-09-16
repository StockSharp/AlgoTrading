using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// FATL MACD trend-following strategy.
/// Opens long positions when the indicator turns upward
/// and short positions when it turns downward.
/// Closes existing positions on opposite signals.
/// </summary>
public class FatlMacdStrategy : Strategy
{
private readonly StrategyParam<int> _fastLength;
private readonly StrategyParam<int> _slowLength;
private readonly StrategyParam<int> _signalLength;
private readonly StrategyParam<DataType> _candleType;

private decimal _prev1;
private decimal _prev2;
private bool _isInitialized;

/// <summary>
/// Fast EMA period used by MACD.
/// </summary>
public int FastLength
{
get => _fastLength.Value;
set => _fastLength.Value = value;
}

/// <summary>
/// Slow EMA period used by MACD.
/// </summary>
public int SlowLength
{
get => _slowLength.Value;
set => _slowLength.Value = value;
}

/// <summary>
/// Signal EMA period used by MACD.
/// </summary>
public int SignalLength
{
get => _signalLength.Value;
set => _signalLength.Value = value;
}

/// <summary>
/// Candle type for market data subscription.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initializes parameters.
/// </summary>
public FatlMacdStrategy()
{
_fastLength = Param(nameof(FastLength), 12)
.SetDisplay("Fast EMA", "Period of the fast moving average", "MACD")
.SetGreaterThanZero()
.SetCanOptimize(true);

_slowLength = Param(nameof(SlowLength), 26)
.SetDisplay("Slow EMA", "Period of the slow moving average", "MACD")
.SetGreaterThanZero()
.SetCanOptimize(true);

_signalLength = Param(nameof(SignalLength), 9)
.SetDisplay("Signal EMA", "Period of the signal line", "MACD")
.SetGreaterThanZero()
.SetCanOptimize(true);

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Type of candles for processing", "General");
}

/// <inheritdoc />
public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
return [(Security, CandleType)];
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();
_prev1 = _prev2 = 0m;
_isInitialized = false;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var macd = new MovingAverageConvergenceDivergence
{
ShortPeriod = FastLength,
LongPeriod = SlowLength,
SignalPeriod = SignalLength
};

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(macd, Process)
.Start();
}

private void Process(ICandleMessage candle, decimal macdValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (!_isInitialized)
{
_prev2 = _prev1 = macdValue;
_isInitialized = true;
return;
}

// Indicator turned upward
if (_prev1 < _prev2)
{
// Close short positions
if (Position < 0)
BuyMarket(-Position);

// Open long if indicator keeps rising
if (macdValue > _prev1 && Position <= 0)
BuyMarket();
}
// Indicator turned downward
else if (_prev1 > _prev2)
{
// Close long positions
if (Position > 0)
SellMarket(Position);

// Open short if indicator keeps falling
if (macdValue < _prev1 && Position >= 0)
SellMarket();
}

_prev2 = _prev1;
_prev1 = macdValue;
}
}
