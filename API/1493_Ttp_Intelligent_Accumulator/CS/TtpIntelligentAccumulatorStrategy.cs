using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that accumulates long positions when RSI falls below its average minus one standard deviation and exits portions when RSI rises above its average plus one standard deviation.
/// </summary>
public class TtpIntelligentAccumulatorStrategy : Strategy
{
private readonly StrategyParam<int> _rsiPeriod;
private readonly StrategyParam<int> _maPeriod;
private readonly StrategyParam<int> _stdPeriod;
private readonly StrategyParam<bool> _addWhileInLossOnly;
private readonly StrategyParam<decimal> _minProfit;
private readonly StrategyParam<decimal> _exitPercent;
private readonly StrategyParam<bool> _useDateFilter;
private readonly StrategyParam<DateTimeOffset> _startDate;
private readonly StrategyParam<DateTimeOffset> _endDate;
private readonly StrategyParam<DataType> _candleType;

private RelativeStrengthIndex _rsi;
private SimpleMovingAverage _rsiMa;
private StandardDeviation _rsiStd;

private decimal _avgEntryPrice;

/// <summary>
/// RSI period.
/// </summary>
public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

/// <summary>
/// Period for RSI moving average.
/// </summary>
public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }

/// <summary>
/// Period for RSI standard deviation.
/// </summary>
public int StdPeriod { get => _stdPeriod.Value; set => _stdPeriod.Value = value; }

/// <summary>
/// Add to position only while in loss.
/// </summary>
public bool AddWhileInLossOnly { get => _addWhileInLossOnly.Value; set => _addWhileInLossOnly.Value = value; }

/// <summary>
/// Minimum profit percentage required to exit.
/// </summary>
public decimal MinProfit { get => _minProfit.Value; set => _minProfit.Value = value; }

/// <summary>
/// Percentage of position to exit per signal.
/// </summary>
public decimal ExitPercent { get => _exitPercent.Value; set => _exitPercent.Value = value; }

/// <summary>
/// Use date filter for trading window.
/// </summary>
public bool UseDateFilter { get => _useDateFilter.Value; set => _useDateFilter.Value = value; }

/// <summary>
/// Start date of trading window.
/// </summary>
public DateTimeOffset StartDate { get => _startDate.Value; set => _startDate.Value = value; }

/// <summary>
/// End date of trading window.
/// </summary>
public DateTimeOffset EndDate { get => _endDate.Value; set => _endDate.Value = value; }

/// <summary>
/// Candle type.
/// </summary>
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

/// <summary>
/// Initialize strategy.
/// </summary>
public TtpIntelligentAccumulatorStrategy()
{
_rsiPeriod = Param(nameof(RsiPeriod), 7)
.SetGreaterThanZero()
.SetDisplay("RSI Period", "RSI calculation length", "Indicators")
.SetCanOptimize(true)
.SetOptimize(5, 14, 1);

_maPeriod = Param(nameof(MaPeriod), 14)
.SetGreaterThanZero()
.SetDisplay("MA Period", "Period for RSI moving average", "Indicators")
.SetCanOptimize(true)
.SetOptimize(10, 30, 2);

_stdPeriod = Param(nameof(StdPeriod), 14)
.SetGreaterThanZero()
.SetDisplay("Std Dev Period", "Period for RSI standard deviation", "Indicators")
.SetCanOptimize(true)
.SetOptimize(10, 30, 2);

_addWhileInLossOnly = Param(nameof(AddWhileInLossOnly), true)
.SetDisplay("Add While In Loss Only", "Add to position only if losing", "General");

_minProfit = Param(nameof(MinProfit), 0m)
.SetDisplay("Min Profit %", "Required open profit percentage to exit", "General");

_exitPercent = Param(nameof(ExitPercent), 100m)
.SetDisplay("Exit % Per Candle", "Percentage of position to exit per signal", "General");

_useDateFilter = Param(nameof(UseDateFilter), false)
.SetDisplay("Use Date Filter", "Restrict trading to specific dates", "Backtest");

_startDate = Param(nameof(StartDate), new DateTimeOffset(2022, 6, 1, 0, 0, 0, TimeSpan.Zero))
.SetDisplay("Start Date", "Backtest start date", "Backtest");

_endDate = Param(nameof(EndDate), new DateTimeOffset(2030, 7, 1, 0, 0, 0, TimeSpan.Zero))
.SetDisplay("End Date", "Backtest end date", "Backtest");

_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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

_rsi = null;
_rsiMa = null;
_rsiStd = null;
_avgEntryPrice = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
_rsiMa = new SimpleMovingAverage { Length = MaPeriod };
_rsiStd = new StandardDeviation { Length = StdPeriod };

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _rsi);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

var rsiValue = _rsi.Process(candle.ClosePrice);
if (!rsiValue.IsFinal)
return;

var rsimaValue = _rsiMa.Process(rsiValue);
var stdValue = _rsiStd.Process(rsiValue);

if (!rsimaValue.IsFinal || !stdValue.IsFinal)
return;

var rsi = rsiValue.GetValue<decimal>();
var rsima = rsimaValue.GetValue<decimal>();
var bbstd = stdValue.GetValue<decimal>();

var inTradeWindow = !UseDateFilter || (candle.OpenTime >= StartDate && candle.OpenTime < EndDate);

if (!inTradeWindow || !IsFormedAndOnlineAndAllowTrading())
return;

var entrySignal = rsi < rsima - bbstd;
var exitSignal = rsi > rsima + bbstd;

if (entrySignal && Position >= 0 && (Position == 0 || !AddWhileInLossOnly || candle.ClosePrice < _avgEntryPrice))
{
var prevPos = Position;
BuyMarket(Volume);
var newPos = prevPos + Volume;
_avgEntryPrice = prevPos == 0 ? candle.ClosePrice : (_avgEntryPrice * prevPos + candle.ClosePrice * Volume) / newPos;
}
else if (exitSignal && Position > 0)
{
var profitPercent = _avgEntryPrice == 0 ? 0 : (candle.ClosePrice - _avgEntryPrice) / _avgEntryPrice * 100m;
if (profitPercent > MinProfit)
{
var volumeToSell = Position * ExitPercent / 100m;
if (volumeToSell > 0)
{
SellMarket(volumeToSell);
if (volumeToSell >= Position)
_avgEntryPrice = 0m;
}
}
}
}
}
