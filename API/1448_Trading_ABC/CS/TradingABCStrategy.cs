using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified ABC trading strategy using moving averages and stochastic oscillator.
/// Enters long when price is above all averages and stochastic %K crosses above %D.
/// Enters short when price is below all averages and stochastic %K crosses below %D.
/// Positions are protected by previous bar extremes.
/// </summary>
public class TradingABCStrategy : Strategy
{
private readonly StrategyParam<int> _sma1Length;
private readonly StrategyParam<int> _sma2Length;
private readonly StrategyParam<int> _ema1Length;
private readonly StrategyParam<int> _ema2Length;
private readonly StrategyParam<int> _stochLength;
private readonly StrategyParam<DataType> _candleType;

private decimal _prevK;
private decimal _prevD;
private decimal _lastLow;
private decimal _lastHigh;

/// <summary>
/// Length of first SMA.
/// </summary>
public int Sma1Length
{
get => _sma1Length.Value;
set => _sma1Length.Value = value;
}

/// <summary>
/// Length of second SMA.
/// </summary>
public int Sma2Length
{
get => _sma2Length.Value;
set => _sma2Length.Value = value;
}

/// <summary>
/// Length of first EMA.
/// </summary>
public int Ema1Length
{
get => _ema1Length.Value;
set => _ema1Length.Value = value;
}

/// <summary>
/// Length of second EMA.
/// </summary>
public int Ema2Length
{
get => _ema2Length.Value;
set => _ema2Length.Value = value;
}

/// <summary>
/// Stochastic length.
/// </summary>
public int StochLength
{
get => _stochLength.Value;
set => _stochLength.Value = value;
}

/// <summary>
/// Candle type used for processing.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initializes a new instance of the strategy.
/// </summary>
public TradingABCStrategy()
{
_sma1Length = Param(nameof(Sma1Length), 50)
.SetGreaterThanZero()
.SetDisplay("SMA1 Length", "Length of first SMA", "Trend");

_sma2Length = Param(nameof(Sma2Length), 100)
.SetGreaterThanZero()
.SetDisplay("SMA2 Length", "Length of second SMA", "Trend");

_ema1Length = Param(nameof(Ema1Length), 20)
.SetGreaterThanZero()
.SetDisplay("EMA1 Length", "Length of first EMA", "Trend");

_ema2Length = Param(nameof(Ema2Length), 40)
.SetGreaterThanZero()
.SetDisplay("EMA2 Length", "Length of second EMA", "Trend");

_stochLength = Param(nameof(StochLength), 5)
.SetGreaterThanZero()
.SetDisplay("Stoch Length", "Length of stochastic oscillator", "Signal");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Candles used for calculations", "General");
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
_prevK = 0m;
_prevD = 0m;
_lastLow = 0m;
_lastHigh = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var sma1 = new SMA { Length = Sma1Length };
var sma2 = new SMA { Length = Sma2Length };
var ema1 = new EMA { Length = Ema1Length };
var ema2 = new EMA { Length = Ema2Length };
var stoch = new Stochastic
{
Length = StochLength,
Smooth = 3,
Signal = 3
};

SubscribeCandles(CandleType)
.Bind(sma1, sma2, ema1, ema2, stoch, ProcessCandle)
.Start();
}

private void ProcessCandle(ICandleMessage candle, decimal sma1Value, decimal sma2Value, decimal ema1Value, decimal ema2Value, decimal kValue, decimal dValue)
{
if (candle.State != CandleStates.Finished)
return;

_lastLow = candle.LowPrice;
_lastHigh = candle.HighPrice;

var upTrend = candle.ClosePrice > sma1Value && candle.ClosePrice > sma2Value && candle.ClosePrice > ema1Value && candle.ClosePrice > ema2Value;
var downTrend = candle.ClosePrice < sma1Value && candle.ClosePrice < sma2Value && candle.ClosePrice < ema1Value && candle.ClosePrice < ema2Value;

if (upTrend && _prevK <= _prevD && kValue > dValue)
{
if (Position <= 0)
BuyMarket(Volume + Math.Abs(Position));
}
else if (downTrend && _prevK >= _prevD && kValue < dValue)
{
if (Position >= 0)
SellMarket(Volume + Math.Abs(Position));
}

if (Position > 0 && candle.ClosePrice < _lastLow)
SellMarket(Position);
else if (Position < 0 && candle.ClosePrice > _lastHigh)
BuyMarket(Math.Abs(Position));

_prevK = kValue;
_prevD = dValue;
}
}
