using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum breakout with RSI filter and trailing stop.
/// </summary>
public class TrailingStopWithRsiMomentumBasedStrategy : Strategy
{
private readonly StrategyParam<int> _momentumLength;
private readonly StrategyParam<int> _rsiLength;
private readonly StrategyParam<decimal> _rsiOverbought;
private readonly StrategyParam<decimal> _rsiOversold;
private readonly StrategyParam<decimal> _trailActivate;
private readonly StrategyParam<decimal> _trailPercent;
private readonly StrategyParam<DataType> _candleType;

private decimal _prevMomentum;
private decimal _entryPrice;
private bool _trailActive;
private decimal _trailLevel;
private bool _isLong;
private decimal _tickSize;

/// <summary>
/// Momentum calculation length.
/// </summary>
public int MomentumLength
{
get => _momentumLength.Value;
set => _momentumLength.Value = value;
}

/// <summary>
/// RSI period length.
/// </summary>
public int RsiLength
{
get => _rsiLength.Value;
set => _rsiLength.Value = value;
}

/// <summary>
/// Overbought RSI level.
/// </summary>
public decimal RsiOverbought
{
get => _rsiOverbought.Value;
set => _rsiOverbought.Value = value;
}

/// <summary>
/// Oversold RSI level.
/// </summary>
public decimal RsiOversold
{
get => _rsiOversold.Value;
set => _rsiOversold.Value = value;
}

/// <summary>
/// Trailing activation in percent.
/// </summary>
public decimal TrailActivate
{
get => _trailActivate.Value;
set => _trailActivate.Value = value;
}

/// <summary>
/// Trailing distance in percent.
/// </summary>
public decimal TrailPercent
{
get => _trailPercent.Value;
set => _trailPercent.Value = value;
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
/// Constructor.
/// </summary>
public TrailingStopWithRsiMomentumBasedStrategy()
{
_momentumLength = Param(nameof(MomentumLength), 12)
.SetGreaterThanZero()
.SetDisplay("Momentum Length", "Momentum period", "Parameters");

_rsiLength = Param(nameof(RsiLength), 14)
.SetGreaterThanZero()
.SetDisplay("RSI Length", "RSI period", "Parameters");

_rsiOverbought = Param(nameof(RsiOverbought), 70m)
.SetDisplay("RSI Overbought", "Overbought level", "Parameters");

_rsiOversold = Param(nameof(RsiOversold), 30m)
.SetDisplay("RSI Oversold", "Oversold level", "Parameters");

_trailActivate = Param(nameof(TrailActivate), 0m)
.SetDisplay("Trail Activation %", "PnL to activate trailing", "Risk");

_trailPercent = Param(nameof(TrailPercent), 0m)
.SetDisplay("Trail %", "Trailing distance", "Risk");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Timeframe", "General");
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
_prevMomentum = 0m;
_entryPrice = 0m;
_trailActive = false;
_trailLevel = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_tickSize = Security?.PriceStep ?? 1m;

var momentum = new Momentum { Length = MomentumLength };
var rsi = new RelativeStrengthIndex { Length = RsiLength };

var subscription = SubscribeCandles(CandleType);

subscription
.Bind(momentum, rsi, ProcessCandle)
.Start();
}

private void ProcessCandle(ICandleMessage candle, decimal momentumValue, decimal rsiValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var mom0 = momentumValue;
var mom1 = mom0 - _prevMomentum;
_prevMomentum = mom0;

ManageTrail(candle);

if (mom0 > 0 && mom1 > 0 && Position <= 0)
{
BuyStop(Volume + Math.Abs(Position), candle.HighPrice + _tickSize);
}
else if (mom0 < 0 && mom1 < 0 && Position >= 0)
{
SellStop(Volume + Math.Abs(Position), candle.LowPrice - _tickSize);
}

if (rsiValue >= RsiOverbought)
{
if (Position < 0)
BuyMarket(Math.Abs(Position));

if (Position <= 0)
{
_entryPrice = candle.ClosePrice;
BuyMarket(Volume);
_isLong = true;
}
}
else if (rsiValue <= RsiOversold)
{
if (Position > 0)
SellMarket(Math.Abs(Position));

if (Position >= 0)
{
_entryPrice = candle.ClosePrice;
SellMarket(Volume);
_isLong = false;
}
}
}

private void ManageTrail(ICandleMessage candle)
{
if (Position > 0)
{
if (!_trailActive)
{
if (TrailActivate <= 0m || candle.HighPrice >= _entryPrice * (1 + TrailActivate / 100m))
{
_trailActive = true;
_trailLevel = candle.HighPrice;
_isLong = true;
}
}
else
{
_trailLevel = Math.Max(_trailLevel, candle.HighPrice);
var stop = _trailLevel * (1 - TrailPercent / 100m);
if (candle.LowPrice <= stop)
{
SellMarket(Math.Abs(Position));
_trailActive = false;
}
}
}
else if (Position < 0)
{
if (!_trailActive)
{
if (TrailActivate <= 0m || candle.LowPrice <= _entryPrice * (1 - TrailActivate / 100m))
{
_trailActive = true;
_trailLevel = candle.LowPrice;
_isLong = false;
}
}
else
{
_trailLevel = Math.Min(_trailLevel, candle.LowPrice);
var stop = _trailLevel * (1 + TrailPercent / 100m);
if (candle.HighPrice >= stop)
{
BuyMarket(Math.Abs(Position));
_trailActive = false;
}
}
}
else
{
_trailActive = false;
}
}
}
