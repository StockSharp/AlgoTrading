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
/// Strategy combining EMA trend, MACD crossovers, and RSI levels.
/// </summary>
public class EmaMacdRsiStrategy : Strategy
{
private readonly StrategyParam<int> _fastEmaLength;
private readonly StrategyParam<int> _slowEmaLength;
private readonly StrategyParam<int> _rsiLength;
private readonly StrategyParam<decimal> _rsiBuyLevel;
private readonly StrategyParam<decimal> _rsiSellLevel;
private readonly StrategyParam<DataType> _candleType;

private decimal _prevMacd;
private decimal _prevSignal;
private bool _isFirst = true;

/// <summary>
/// Fast EMA length.
/// </summary>
public int FastEmaLength
{
get => _fastEmaLength.Value;
set => _fastEmaLength.Value = value;
}

/// <summary>
/// Slow EMA length.
/// </summary>
public int SlowEmaLength
{
get => _slowEmaLength.Value;
set => _slowEmaLength.Value = value;
}

/// <summary>
/// RSI calculation length.
/// </summary>
public int RsiLength
{
get => _rsiLength.Value;
set => _rsiLength.Value = value;
}

/// <summary>
/// Minimum RSI level to allow buys.
/// </summary>
public decimal RsiBuyLevel
{
get => _rsiBuyLevel.Value;
set => _rsiBuyLevel.Value = value;
}

/// <summary>
/// Maximum RSI level to allow sells.
/// </summary>
public decimal RsiSellLevel
{
get => _rsiSellLevel.Value;
set => _rsiSellLevel.Value = value;
}

/// <summary>
/// Candle type.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initialize strategy parameters.
/// </summary>
public EmaMacdRsiStrategy()
{
_fastEmaLength = Param(nameof(FastEmaLength), 50)
.SetDisplay("Fast EMA", "Fast EMA length", "Indicators")

.SetOptimize(10, 100, 5);

_slowEmaLength = Param(nameof(SlowEmaLength), 200)
.SetDisplay("Slow EMA", "Slow EMA length", "Indicators")

.SetOptimize(50, 300, 10);

_rsiLength = Param(nameof(RsiLength), 14)
.SetDisplay("RSI Length", "RSI indicator length", "Indicators")

.SetOptimize(7, 21, 1);

_rsiBuyLevel = Param(nameof(RsiBuyLevel), 45m)
.SetDisplay("RSI Buy Level", "Minimum RSI for buy", "Trading Levels")

.SetOptimize(30m, 60m, 5m);

_rsiSellLevel = Param(nameof(RsiSellLevel), 55m)
.SetDisplay("RSI Sell Level", "Maximum RSI for sell", "Trading Levels")

.SetOptimize(40m, 70m, 5m);

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
.SetDisplay("Candle Type", "Type of candles to use", "General");
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
_prevMacd = default;
_prevSignal = default;
_isFirst = true;
}

/// <inheritdoc />
protected override void OnStarted2(DateTime time)
{
base.OnStarted2(time);

var fastEma = new EMA { Length = FastEmaLength };
var slowEma = new EMA { Length = SlowEmaLength };
var macd = new MovingAverageConvergenceDivergenceSignal
{
Macd = { ShortMa = { Length = 12 }, LongMa = { Length = 26 } },
SignalMa = { Length = 9 }
};
var rsi = new RelativeStrengthIndex { Length = RsiLength };

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx([macd, rsi, fastEma, slowEma], ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, fastEma);
DrawIndicator(area, slowEma);

var macdArea = CreateChartArea();
DrawIndicator(macdArea, macd);
DrawIndicator(macdArea, rsi);

DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)values[0];
if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
return;

var rsi = values[1].ToDecimal();
var fastEma = values[2].ToDecimal();
var slowEma = values[3].ToDecimal();

if (_isFirst)
{
_prevMacd = macd;
_prevSignal = signal;
_isFirst = false;
return;
}

var isBullish = fastEma > slowEma;
var isBearish = fastEma < slowEma;

var macdBullish = _prevMacd <= _prevSignal && macd > signal;
var macdBearish = _prevMacd >= _prevSignal && macd < signal;

var rsiBullish = rsi > RsiBuyLevel && rsi < 70m;
var rsiBearish = rsi < RsiSellLevel && rsi > 30m;

if (isBullish && macdBullish && rsiBullish && Position <= 0)
{
BuyMarket();
}
else if (isBearish && macdBearish && rsiBearish && Position >= 0)
{
SellMarket();
}

_prevMacd = macd;
_prevSignal = signal;
}
}
