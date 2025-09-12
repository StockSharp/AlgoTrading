using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Williams %R strategy entering on oversold and exiting on breakout or overbought.
/// </summary>
public class WilliamsRStrategy : Strategy
{
private readonly StrategyParam<int> _lookbackPeriod;
private readonly StrategyParam<DataType> _candleType;

private WilliamsR _wpr = null!;
private decimal _prevHigh;

/// <summary>
/// Lookback period for Williams %R.
/// </summary>
public int LookbackPeriod
{
get => _lookbackPeriod.Value;
set => _lookbackPeriod.Value = value;
}

/// <summary>
/// Candle type to process.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initializes a new instance of <see cref="WilliamsRStrategy"/>.
/// </summary>
public WilliamsRStrategy()
{
_lookbackPeriod = Param(nameof(LookbackPeriod), 2)
.SetGreaterThanZero()
.SetDisplay("Lookback Period", "Williams %R length", "General")
.SetCanOptimize(true);

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");
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
_wpr = null!;
_prevHigh = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_wpr = new WilliamsR { Length = LookbackPeriod };
var subscription = SubscribeCandles(CandleType);
subscription.Bind(_wpr, ProcessCandle).Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _wpr);
DrawOwnTrades(area);
}

StartProtection();
}

private void ProcessCandle(ICandleMessage candle, decimal wr)
{
if (candle.State != CandleStates.Finished)
return;

var longSignal = wr < -90m;
var exitSignal = candle.ClosePrice > _prevHigh || wr > -30m;

if (!IsFormedAndOnlineAndAllowTrading())
{
_prevHigh = candle.HighPrice;
return;
}

if (longSignal && Position <= 0)
{
BuyMarket(Volume + (Position < 0 ? -Position : 0m));
}
else if (exitSignal && Position > 0)
{
SellMarket(Position);
}

_prevHigh = candle.HighPrice;
}
}
