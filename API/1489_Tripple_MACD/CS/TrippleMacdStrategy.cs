namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Combines four MACD indicators and an RSI filter.
/// Buys when averaged MACD turns positive with RSI below threshold.
/// </summary>
public class TrippleMacdStrategy : Strategy
{
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<int> _rsiPeriod;
private readonly StrategyParam<int> _signalPeriod;
private readonly StrategyParam<decimal> _takeProfitPercent;
private readonly StrategyParam<int> _fast1;
private readonly StrategyParam<int> _slow1;
private readonly StrategyParam<int> _fast2;
private readonly StrategyParam<int> _slow2;
private readonly StrategyParam<int> _fast3;
private readonly StrategyParam<int> _slow3;
private readonly StrategyParam<int> _fast4;
private readonly StrategyParam<int> _slow4;

private decimal _prevHist;

/// <summary>
/// Candle type for analysis.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// RSI period.
/// </summary>
public int RsiPeriod
{
get => _rsiPeriod.Value;
set => _rsiPeriod.Value = value;
}

/// <summary>
/// Signal period for all MACD indicators.
/// </summary>
public int SignalPeriod
{
get => _signalPeriod.Value;
set => _signalPeriod.Value = value;
}

/// <summary>
/// Take profit in percent.
/// </summary>
public decimal TakeProfitPercent
{
get => _takeProfitPercent.Value;
set => _takeProfitPercent.Value = value;
}

public int Fast1 { get => _fast1.Value; set => _fast1.Value = value; }
public int Slow1 { get => _slow1.Value; set => _slow1.Value = value; }
public int Fast2 { get => _fast2.Value; set => _fast2.Value = value; }
public int Slow2 { get => _slow2.Value; set => _slow2.Value = value; }
public int Fast3 { get => _fast3.Value; set => _fast3.Value = value; }
public int Slow3 { get => _slow3.Value; set => _slow3.Value = value; }
public int Fast4 { get => _fast4.Value; set => _fast4.Value = value; }
public int Slow4 { get => _slow4.Value; set => _slow4.Value = value; }

public TrippleMacdStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");
_rsiPeriod = Param(nameof(RsiPeriod), 14)
.SetDisplay("RSI Period", "RSI calculation length", "Indicators");
_signalPeriod = Param(nameof(SignalPeriod), 9)
.SetDisplay("Signal Period", "MACD signal period", "Indicators");
_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
.SetDisplay("Take Profit %", "Take profit percentage", "Risk");
_fast1 = Param(nameof(Fast1), 5).SetDisplay("Fast1", "Fast period of MACD1", "Indicators");
_slow1 = Param(nameof(Slow1), 8).SetDisplay("Slow1", "Slow period of MACD1", "Indicators");
_fast2 = Param(nameof(Fast2), 13).SetDisplay("Fast2", "Fast period of MACD2", "Indicators");
_slow2 = Param(nameof(Slow2), 21).SetDisplay("Slow2", "Slow period of MACD2", "Indicators");
_fast3 = Param(nameof(Fast3), 34).SetDisplay("Fast3", "Fast period of MACD3", "Indicators");
_slow3 = Param(nameof(Slow3), 144).SetDisplay("Slow3", "Slow period of MACD3", "Indicators");
_fast4 = Param(nameof(Fast4), 68).SetDisplay("Fast4", "Fast period of MACD4", "Indicators");
_slow4 = Param(nameof(Slow4), 288).SetDisplay("Slow4", "Slow period of MACD4", "Indicators");
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

StartProtection(new Unit(TakeProfitPercent, UnitTypes.Percent), new Unit(0));

var macd1 = new MovingAverageConvergenceDivergenceSignal
{
Macd = { ShortMa = { Length = Fast1 }, LongMa = { Length = Slow1 } },
SignalMa = { Length = SignalPeriod }
};
var macd2 = new MovingAverageConvergenceDivergenceSignal
{
Macd = { ShortMa = { Length = Fast2 }, LongMa = { Length = Slow2 } },
SignalMa = { Length = SignalPeriod }
};
var macd3 = new MovingAverageConvergenceDivergenceSignal
{
Macd = { ShortMa = { Length = Fast3 }, LongMa = { Length = Slow3 } },
SignalMa = { Length = SignalPeriod }
};
var macd4 = new MovingAverageConvergenceDivergenceSignal
{
Macd = { ShortMa = { Length = Fast4 }, LongMa = { Length = Slow4 } },
SignalMa = { Length = SignalPeriod }
};
var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx(macd1, macd2, macd3, macd4, rsi, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, macd1);
DrawIndicator(area, rsi);
DrawOwnTrades(area);
}
}


private void ProcessCandle(ICandleMessage candle, IIndicatorValue macd1Value, IIndicatorValue macd2Value, IIndicatorValue macd3Value, IIndicatorValue macd4Value, IIndicatorValue rsiValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!macd1Value.IsFinal || !macd2Value.IsFinal || !macd3Value.IsFinal || !macd4Value.IsFinal || !rsiValue.IsFinal)
return;

var m1 = (MovingAverageConvergenceDivergenceSignalValue)macd1Value;
var m2 = (MovingAverageConvergenceDivergenceSignalValue)macd2Value;
var m3 = (MovingAverageConvergenceDivergenceSignalValue)macd3Value;
var m4 = (MovingAverageConvergenceDivergenceSignalValue)macd4Value;
var rsi = rsiValue.ToDecimal();

var hist = ((m1.Macd + m2.Macd + m3.Macd + m4.Macd) / 4m) - ((m1.Signal + m2.Signal + m3.Signal + m4.Signal) / 4m);

if (IsFormedAndOnlineAndAllowTrading())
{
if (_prevHist <= 0m && hist > 0m && candle.ClosePrice > candle.OpenPrice && rsi < 55m && Position <= 0)
BuyMarket(Volume + Math.Abs(Position));
else if (Position > 0 && _prevHist > 0m && hist < 0m && candle.OpenPrice > candle.ClosePrice)
SellMarket(Math.Abs(Position));
}

_prevHist = hist;
}
}
