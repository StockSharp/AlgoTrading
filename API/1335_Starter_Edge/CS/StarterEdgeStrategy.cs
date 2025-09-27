using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Starter Edge strategy based on ZLEMA MACD with EMA filter and RSI confirmation.
/// </summary>
public class StarterEdgeStrategy : Strategy
{
private readonly StrategyParam<int> _zlemaLength;
private readonly StrategyParam<int> _shortLength;
private readonly StrategyParam<int> _longLength;
private readonly StrategyParam<int> _signalLength;
private readonly StrategyParam<int> _emaLength;
private readonly StrategyParam<decimal> _takeProfitPercent;
private readonly StrategyParam<decimal> _stopLossPercent;
private readonly StrategyParam<DataType> _candleType;

private ZeroLagExponentialMovingAverage _zlema = null!;
private ExponentialMovingAverage _fastMa = null!;
private ExponentialMovingAverage _slowMa = null!;
private SimpleMovingAverage _signalMa = null!;
private RelativeStrengthIndex _rsi = null!;
private ExponentialMovingAverage _ema = null!;

private decimal _prevMacd;
private decimal _prevSignal;
private decimal _prevHist;
private decimal _prevHist2;
private decimal _prevRsi;

/// <summary>
/// ZLEMA period length.
/// </summary>
public int ZlemaLength
{
get => _zlemaLength.Value;
set => _zlemaLength.Value = value;
}

/// <summary>
/// MACD fast EMA length.
/// </summary>
public int ShortLength
{
get => _shortLength.Value;
set => _shortLength.Value = value;
}

/// <summary>
/// MACD slow EMA length.
/// </summary>
public int LongLength
{
get => _longLength.Value;
set => _longLength.Value = value;
}

/// <summary>
/// MACD signal SMA length.
/// </summary>
public int SignalLength
{
get => _signalLength.Value;
set => _signalLength.Value = value;
}

/// <summary>
/// EMA filter length.
/// </summary>
public int EmaLength
{
get => _emaLength.Value;
set => _emaLength.Value = value;
}

/// <summary>
/// Take profit percentage.
/// </summary>
public decimal TakeProfitPercent
{
get => _takeProfitPercent.Value;
set => _takeProfitPercent.Value = value;
}

/// <summary>
/// Stop loss percentage.
/// </summary>
public decimal StopLossPercent
{
get => _stopLossPercent.Value;
set => _stopLossPercent.Value = value;
}

/// <summary>
/// Candle type to use.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Constructor.
/// </summary>
public StarterEdgeStrategy()
{
_zlemaLength = Param(nameof(ZlemaLength), 34)
.SetDisplay("ZLEMA Length")
.SetCanOptimize(true);

_shortLength = Param(nameof(ShortLength), 12)
.SetDisplay("MACD Short Length")
.SetCanOptimize(true);

_longLength = Param(nameof(LongLength), 26)
.SetDisplay("MACD Long Length")
.SetCanOptimize(true);

_signalLength = Param(nameof(SignalLength), 9)
.SetDisplay("Signal Length")
.SetCanOptimize(true);

_emaLength = Param(nameof(EmaLength), 100)
.SetDisplay("EMA 100 Length")
.SetCanOptimize(true);

_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
.SetDisplay("Take Profit %")
.SetCanOptimize(true);

_stopLossPercent = Param(nameof(StopLossPercent), 1m)
.SetDisplay("Stop Loss %")
.SetCanOptimize(true);

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type");
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

_zlema = new ZeroLagExponentialMovingAverage { Length = ZlemaLength };
_fastMa = new ExponentialMovingAverage { Length = ShortLength };
_slowMa = new ExponentialMovingAverage { Length = LongLength };
_signalMa = new SimpleMovingAverage { Length = SignalLength };
_ema = new ExponentialMovingAverage { Length = EmaLength };
_rsi = new RelativeStrengthIndex { Length = 14 };

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(_ema, _zlema, _rsi, ProcessCandle)
.Start();

StartProtection(
takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
useMarketOrders: true);

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _ema);
DrawIndicator(area, _zlema);
DrawIndicator(area, _fastMa);
DrawIndicator(area, _slowMa);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal zlemaValue, decimal rsiValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var fastValue = _fastMa.Process(zlemaValue, candle.ServerTime, true).ToDecimal();
var slowValue = _slowMa.Process(zlemaValue, candle.ServerTime, true).ToDecimal();
var macdLine = fastValue - slowValue;
var signalValue = _signalMa.Process(macdLine, candle.ServerTime, true).ToDecimal();
var histValue = macdLine - signalValue;

if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_signalMa.IsFormed)
{
_prevMacd = macdLine;
_prevSignal = signalValue;
_prevHist2 = _prevHist;
_prevHist = histValue;
_prevRsi = rsiValue;
return;
}

var macdCrossUp = _prevMacd <= _prevSignal && macdLine > signalValue;
var macdCrossDown = _prevMacd >= _prevSignal && macdLine < signalValue;
var linesParallel = Math.Abs(macdLine - signalValue) < 0.03m && Math.Abs(_prevMacd - _prevSignal) < 0.03m;
var histFalling = histValue < _prevHist && _prevHist > _prevHist2;
var wasAbove70 = _prevRsi > 70m && rsiValue <= 70m;
var wasBelow30 = _prevRsi < 30m && rsiValue >= 30m;

if (candle.ClosePrice > emaValue && macdCrossUp && !linesParallel && rsiValue > 50m && Position <= 0)
{
BuyMarket(Volume + Math.Abs(Position));
}
else if (candle.ClosePrice < emaValue && macdCrossDown && !linesParallel && rsiValue < 50m && Position >= 0)
{
SellMarket(Volume + Math.Abs(Position));
}

if (Position > 0 && (macdCrossDown || histFalling || wasAbove70))
{
SellMarket(Position);
}
else if (Position < 0 && (macdCrossUp || histFalling || wasBelow30))
{
BuyMarket(-Position);
}

_prevHist2 = _prevHist;
_prevHist = histValue;
_prevMacd = macdLine;
_prevSignal = signalValue;
_prevRsi = rsiValue;
}
}

