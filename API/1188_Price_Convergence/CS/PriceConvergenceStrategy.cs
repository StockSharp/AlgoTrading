using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Price convergence strategy.
/// Calculates probability of price moving up or down based on OHLC4 sums.
/// Buys when probability of rising exceeds 50%, sells when probability of falling exceeds 50%.
/// </summary>
public class PriceConvergenceStrategy : Strategy
{
private readonly StrategyParam<bool> _fullHistory;
private readonly StrategyParam<int> _range;
private readonly StrategyParam<DataType> _candleType;

private SMA _totalSma;
private SMA _upSma;
private SMA _downSma;

private decimal _totalSum;
private decimal _upSum;
private decimal _downSum;

/// <summary>
/// Use full history for calculations.
/// </summary>
public bool FullHistory
{
get => _fullHistory.Value;
set => _fullHistory.Value = value;
}

/// <summary>
/// Range length when not using full history.
/// </summary>
public int Range
{
get => _range.Value;
set => _range.Value = value;
}

/// <summary>
/// The type of candles to use for strategy calculation.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Constructor.
/// </summary>
public PriceConvergenceStrategy()
{
_fullHistory = Param(nameof(FullHistory), true)
.SetDisplay("Full History", "Ignore range and use entire history", "General");

_range = Param(nameof(Range), 200)
.SetGreaterThanZero()
.SetDisplay("Range", "Number of candles for calculations", "General");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Type of candles to use", "General");
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
_totalSum = 0m;
_upSum = 0m;
_downSum = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

if (!FullHistory)
{
_totalSma = new SMA { Length = Range };
_upSma = new SMA { Length = Range };
_downSma = new SMA { Length = Range };
}

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var ohlc4 = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;

decimal probUp;
decimal probDown;

if (FullHistory)
{
_totalSum += ohlc4;
if (candle.ClosePrice >= candle.OpenPrice)
_upSum += ohlc4;
else
_downSum += ohlc4;

if (_totalSum == 0m)
return;

probUp = _upSum / _totalSum * 100m;
probDown = _downSum / _totalSum * 100m;
}
else
{
var totalAvg = (DecimalIndicatorValue)_totalSma.Process(ohlc4, candle.OpenTime, true);
var upAvg = (DecimalIndicatorValue)_upSma.Process(candle.ClosePrice >= candle.OpenPrice ? ohlc4 : 0m, candle.OpenTime, true);
var downAvg = (DecimalIndicatorValue)_downSma.Process(candle.ClosePrice <= candle.OpenPrice ? ohlc4 : 0m, candle.OpenTime, true);

if (!_totalSma.IsFormed)
return;

if (totalAvg.Value == 0m)
return;

probUp = upAvg.Value / totalAvg.Value * 100m;
probDown = downAvg.Value / totalAvg.Value * 100m;
}

if (probUp > 50m && Position <= 0)
{
BuyMarket(Volume + Math.Abs(Position));
}
else if (probDown > 50m && Position >= 0)
{
SellMarket(Volume + Math.Abs(Position));
}
}
}
