namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// DoubleUp2 strategy combining CCI and MACD with volume doubling.
/// </summary>
public class DoubleUp2Strategy : Strategy
{
private readonly StrategyParam<int> _cciPeriod;
private readonly StrategyParam<int> _macdFastPeriod;
private readonly StrategyParam<int> _macdSlowPeriod;
private readonly StrategyParam<int> _macdSignalPeriod;
private readonly StrategyParam<decimal> _threshold;
private readonly StrategyParam<decimal> _baseVolume;
private readonly StrategyParam<DataType> _candleType;

private decimal _entryPrice;
private int _martingaleStep;

/// <summary>
/// CCI period.
/// </summary>
public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }

/// <summary>
/// MACD fast EMA period.
/// </summary>
public int MacdFastPeriod { get => _macdFastPeriod.Value; set => _macdFastPeriod.Value = value; }

/// <summary>
/// MACD slow EMA period.
/// </summary>
public int MacdSlowPeriod { get => _macdSlowPeriod.Value; set => _macdSlowPeriod.Value = value; }

/// <summary>
/// MACD signal EMA period.
/// </summary>
public int MacdSignalPeriod { get => _macdSignalPeriod.Value; set => _macdSignalPeriod.Value = value; }

/// <summary>
/// Threshold for CCI and MACD signals.
/// </summary>
public decimal Threshold { get => _threshold.Value; set => _threshold.Value = value; }

/// <summary>
/// Base volume used before martingale scaling.
/// </summary>
public decimal BaseVolume { get => _baseVolume.Value; set => _baseVolume.Value = value; }

/// <summary>
/// Candle type for calculations.
/// </summary>
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

/// <summary>
/// Constructor.
/// </summary>
public DoubleUp2Strategy()
{
_cciPeriod = Param(nameof(CciPeriod), 8)
.SetDisplay("CCI Period", "Averaging period for CCI", "Indicators")
.SetCanOptimize(true)
.SetOptimize(4, 20, 1);

_macdFastPeriod = Param(nameof(MacdFastPeriod), 13)
.SetDisplay("MACD Fast", "Fast EMA period for MACD", "Indicators")
.SetCanOptimize(true)
.SetOptimize(5, 20, 1);

_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 33)
.SetDisplay("MACD Slow", "Slow EMA period for MACD", "Indicators")
.SetCanOptimize(true)
.SetOptimize(20, 50, 1);

_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 2)
.SetDisplay("MACD Signal", "Signal period for MACD", "Indicators")
.SetCanOptimize(true)
.SetOptimize(1, 10, 1);

_threshold = Param(nameof(Threshold), 230m)
.SetDisplay("Threshold", "CCI and MACD extreme level", "Strategy")
.SetCanOptimize(true)
.SetOptimize(50m, 300m, 10m);

_baseVolume = Param(nameof(BaseVolume), 0.1m)
.SetDisplay("Base Volume", "Initial position volume", "Strategy")
.SetCanOptimize(true)
.SetOptimize(0.1m, 1m, 0.1m);

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Type of candles used for calculations", "General");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
=> [(Security, CandleType)];

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();
_entryPrice = 0m;
_martingaleStep = 0;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var cci = new CommodityChannelIndex { Length = CciPeriod };
var macd = new MovingAverageConvergenceDivergence
{
ShortLength = MacdFastPeriod,
LongLength = MacdSlowPeriod,
SignalLength = MacdSignalPeriod
};

var subscription = SubscribeCandles(CandleType);
subscription.Bind(cci, macd, ProcessCandle).Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, cci);
DrawIndicator(area, macd);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal cciValue, decimal macdValue, decimal signal, decimal histogram)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

// Calculate current trade volume with martingale scaling
var volume = BaseVolume * (decimal)Math.Pow(2, _martingaleStep);

// Short entry condition
if (cciValue > Threshold && macdValue > Threshold)
{
if (Position > 0)
{
var profit = candle.ClosePrice - _entryPrice;
_martingaleStep = profit > 0m ? 0 : _martingaleStep + 1;
}

var total = volume + (Position > 0 ? Position : 0m);
SellMarket(total);
_entryPrice = candle.ClosePrice;
return;
}

// Long entry condition
if (cciValue < -Threshold && macdValue < -Threshold)
{
if (Position < 0)
{
var profit = _entryPrice - candle.ClosePrice;
_martingaleStep = profit > 0m ? 0 : _martingaleStep + 1;
}

var total = volume + (Position < 0 ? -Position : 0m);
BuyMarket(total);
_entryPrice = candle.ClosePrice;
return;
}

// Exit profitable long position
if (Position > 0 && candle.ClosePrice - _entryPrice > 120m * Security.PriceStep)
{
SellMarket(Position);
_martingaleStep += 2;
return;
}

// Exit profitable short position
if (Position < 0 && _entryPrice - candle.ClosePrice > 120m * Security.PriceStep)
{
BuyMarket(-Position);
_martingaleStep += 2;
}
}
}
