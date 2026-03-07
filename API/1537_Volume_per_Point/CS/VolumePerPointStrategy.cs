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
/// Strategy based on volume per price point with RSI filter.
/// Buys when range decreases but volume increases, sells on opposite condition.
/// </summary>
public class VolumePerPointStrategy : Strategy
{
private readonly StrategyParam<int> _rsiLength;
private readonly StrategyParam<int> _rsiHigh;
private readonly StrategyParam<int> _rsiLow;
private readonly StrategyParam<bool> _useRsiFilter;
private readonly StrategyParam<int> _signalCooldownBars;
private readonly StrategyParam<DataType> _candleType;

private decimal _prevRange;
private decimal _prevVolume;
private decimal _prevClose;
private int _cooldownRemaining;

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
/// Bars to wait between trading actions.
/// </summary>
public int SignalCooldownBars
{
get => _signalCooldownBars.Value;
set => _signalCooldownBars.Value = value;
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

.SetOptimize(10, 20, 2);

_rsiHigh = Param(nameof(RsiHigh), 65)
.SetDisplay("RSI Above", "Upper RSI threshold", "Filters");

_rsiLow = Param(nameof(RsiLow), 35)
.SetDisplay("RSI Below", "Lower RSI threshold", "Filters");

_useRsiFilter = Param(nameof(UseRsiFilter), true)
.SetDisplay("Use RSI Filter", "Enable RSI filtering", "Filters");

_signalCooldownBars = Param(nameof(SignalCooldownBars), 12)
.SetGreaterThanZero()
.SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
_prevClose = 0;
_cooldownRemaining = 0;
}

/// <inheritdoc />
protected override void OnStarted2(DateTime time)
{
base.OnStarted2(time);

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

if (_cooldownRemaining > 0)
_cooldownRemaining--;

if (_prevRange == 0)
{
_prevRange = candle.HighPrice - candle.LowPrice;
_prevVolume = candle.TotalVolume;
_prevClose = candle.ClosePrice;
return;
}

var step = Security?.PriceStep ?? 0.0001m;
var range = Math.Max(candle.HighPrice - candle.LowPrice, step);
var previousRange = Math.Max(_prevRange, step);
var volume = candle.TotalVolume;
var volumePerPoint = volume / range;
var previousVolumePerPoint = _prevVolume / previousRange;
var bullishImpulse = candle.ClosePrice > candle.OpenPrice && candle.ClosePrice > _prevClose;
var bearishImpulse = candle.ClosePrice < candle.OpenPrice && candle.ClosePrice < _prevClose;
var buySignal = volumePerPoint >= previousVolumePerPoint * 1.5m && bullishImpulse && (!UseRsiFilter || rsiValue <= RsiLow);
var sellSignal = volumePerPoint >= previousVolumePerPoint * 1.5m && bearishImpulse && (!UseRsiFilter || rsiValue >= RsiHigh);

if (_cooldownRemaining == 0 && buySignal && Position <= 0)
{
BuyMarket(Volume + Math.Abs(Position));
_cooldownRemaining = SignalCooldownBars;
}
else if (_cooldownRemaining == 0 && sellSignal && Position >= 0)
{
SellMarket(Volume + Math.Abs(Position));
_cooldownRemaining = SignalCooldownBars;
}

_prevRange = range;
_prevVolume = volume;
_prevClose = candle.ClosePrice;
}
}
