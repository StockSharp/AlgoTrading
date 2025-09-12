using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Scalping strategy using Williams %R oscillator.
/// Buys when %R leaves oversold zone and sells on exiting overbought zone.
/// </summary>
public class WilliamsRZoneScalperStrategy : Strategy
{
private readonly StrategyParam<int> _length;
private readonly StrategyParam<decimal> _overbought;
private readonly StrategyParam<decimal> _oversold;
private readonly StrategyParam<DataType> _candleType;

private decimal _prevWr;

/// <summary>
/// Williams %R period length.
/// </summary>
public int Length
{
get => _length.Value;
set => _length.Value = value;
}

/// <summary>
/// Overbought level.
/// </summary>
public decimal Overbought
{
get => _overbought.Value;
set => _overbought.Value = value;
}

/// <summary>
/// Oversold level.
/// </summary>
public decimal Oversold
{
get => _oversold.Value;
set => _oversold.Value = value;
}

/// <summary>
/// Candle type used by the strategy.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initializes <see cref="WilliamsRZoneScalperStrategy"/>.
/// </summary>
public WilliamsRZoneScalperStrategy()
{
_length = Param(nameof(Length), 14)
.SetGreaterThanZero()
.SetDisplay("%R Length", "Williams %R period", "General");

_overbought = Param(nameof(Overbought), -20m)
.SetDisplay("Overbought", "Overbought level", "General");

_oversold = Param(nameof(Oversold), -80m)
.SetDisplay("Oversold", "Oversold level", "General");

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
_prevWr = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var wr = new WilliamsR { Length = Length };
var subscription = SubscribeCandles(CandleType);
subscription.Bind(wr, ProcessCandle).Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, wr);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal wr)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (_prevWr <= Oversold && wr > Oversold && Position <= 0)
{
BuyMarket(Volume + Math.Abs(Position));
}
else if (_prevWr >= Overbought && wr < Overbought && Position >= 0)
{
SellMarket(Volume + Math.Abs(Position));
}

_prevWr = wr;
}
}
