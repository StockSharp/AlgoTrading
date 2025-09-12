using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on volume per price point with RSI filter.
/// Buys when range decreases but volume increases, sells on opposite condition.
/// </summary>
public class VolumePerPointStrategy : Strategy
{
private readonly StrategyParam<int> _rsiLength;
private readonly StrategyParam<int> _rsiHigh;
private readonly StrategyParam<int> _rsiLow;
private readonly StrategyParam<bool> _useRsiFilter;
private readonly StrategyParam<DataType> _candleType;

private decimal _prevRange;
private decimal _prevVolume;

/// <summary>
/// RSI length.
/// </summary>
public int RsiLength
{
get => _rsiLength.Value;
set => _rsiLength.Value = value;
}

/// <summary>
/// Upper RSI threshold.
/// </summary>
public int RsiHigh
{
get => _rsiHigh.Value;
set => _rsiHigh.Value = value;
}

/// <summary>
/// Lower RSI threshold.
/// </summary>
public int RsiLow
{
get => _rsiLow.Value;
set => _rsiLow.Value = value;
}

/// <summary>
/// Use RSI filter.
/// </summary>
public bool UseRsiFilter
{
get => _useRsiFilter.Value;
set => _useRsiFilter.Value = value;
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
/// Initializes <see cref="VolumePerPointStrategy"/>.
/// </summary>
public VolumePerPointStrategy()
{
_rsiLength = Param(nameof(RsiLength), 14)
.SetDisplay("RSI Length", "Period for RSI", "Indicators")
.SetCanOptimize(true)
.SetOptimize(10, 20, 2);

_rsiHigh = Param(nameof(RsiHigh), 60)
.SetDisplay("RSI Above", "Upper RSI threshold", "Filters");

_rsiLow = Param(nameof(RsiLow), 40)
.SetDisplay("RSI Below", "Lower RSI threshold", "Filters");

_useRsiFilter = Param(nameof(UseRsiFilter), false)
.SetDisplay("Use RSI Filter", "Enable RSI filtering", "Filters");

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
_prevRange = 0;
_prevVolume = 0;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var rsi = new RelativeStrengthIndex { Length = RsiLength };

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(rsi, ProcessCandle)
.Start();

StartProtection(
takeProfit: new Unit(2, UnitTypes.Percent),
stopLoss: new Unit(1, UnitTypes.Percent)
);

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, rsi);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (_prevRange == 0)
{
_prevRange = candle.HighPrice - candle.LowPrice;
_prevVolume = candle.TotalVolume;
return;
}

var range = candle.HighPrice - candle.LowPrice;
var volume = candle.TotalVolume;

var dpaint = range < _prevRange && volume > _prevVolume;
var epaint = range > _prevRange && volume < _prevVolume;
var rsiOk = !UseRsiFilter || rsiValue > RsiHigh || rsiValue < RsiLow;

if (dpaint && rsiOk && Position <= 0)
BuyMarket(Volume + Math.Abs(Position));
else if (epaint && rsiOk && Position >= 0)
SellMarket(Volume + Math.Abs(Position));

_prevRange = range;
_prevVolume = volume;
}
}
