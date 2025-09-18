using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Emulates the "TradingLab Best MACD" MetaTrader strategy with StockSharp high level API.
/// Combines a 200 EMA trend filter, MACD cross momentum and custom-style support/resistance touch tracking.
/// </summary>
public class TradingLabBestMacdStrategy : Strategy
{
private readonly StrategyParam<int> _signalValidity;
private readonly StrategyParam<decimal> _orderVolume;
private readonly StrategyParam<int> _stopDistancePoints;
private readonly StrategyParam<DataType> _candleType;

private EMA _ema = null!;
private MovingAverageConvergenceDivergenceSignal _macd = null!;
private Highest _resistanceIndicator = null!;
private Lowest _supportIndicator = null!;

private int _resistanceTouchCounter;
private int _supportTouchCounter;
private int _macdDownCounter;
private int _macdUpCounter;

private decimal? _previousMacd;
private decimal? _previousSignal;
private decimal? _previousResistance;
private decimal? _previousSupport;
private decimal _previousHigh;
private decimal _previousLow;

private decimal? _pendingStopPrice;
private decimal? _pendingTakePrice;

/// <summary>
/// Number of finished candles that keep a touch or MACD signal active.
/// </summary>
public int SignalValidity
{
get => _signalValidity.Value;
set => _signalValidity.Value = value;
}

/// <summary>
/// Fixed market order volume used for entries.
/// </summary>
public decimal OrderVolume
{
get => _orderVolume.Value;
set => _orderVolume.Value = value;
}

/// <summary>
/// Distance in symbol points used to offset the stop loss from the EMA.
/// </summary>
public int StopDistancePoints
{
get => _stopDistancePoints.Value;
set => _stopDistancePoints.Value = value;
}

/// <summary>
/// Candle type that feeds the indicators.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initializes the TradingLab Best MACD strategy parameters.
/// </summary>
public TradingLabBestMacdStrategy()
{
_signalValidity = Param(nameof(SignalValidity), 7)
.SetDisplay("Signal Validity", "Number of finished candles a touch/MACD signal remains active", "Signals")
.SetCanOptimize(true)
.SetOptimize(3, 12, 1);

_orderVolume = Param(nameof(OrderVolume), 1m)
.SetDisplay("Order Volume", "Base order volume for entries", "General")
.SetRange(0.01m, 100m)
.SetCanOptimize(true);

_stopDistancePoints = Param(nameof(StopDistancePoints), 50)
.SetDisplay("Stop Distance (points)", "Offset in symbol points from the EMA for stop loss", "Risk")
.SetCanOptimize(true)
.SetOptimize(20, 120, 10);

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
.SetDisplay("Candle Type", "Primary timeframe used by the strategy", "General");
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

_resistanceTouchCounter = 0;
_supportTouchCounter = 0;
_macdDownCounter = 0;
_macdUpCounter = 0;

_previousMacd = null;
_previousSignal = null;
_previousResistance = null;
_previousSupport = null;
_previousHigh = 0m;
_previousLow = 0m;

_pendingStopPrice = null;
_pendingTakePrice = null;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

Volume = OrderVolume;

_ema = new EMA { Length = 200 };
_macd = new MovingAverageConvergenceDivergenceSignal
{
Macd =
{
ShortMa = { Length = 12 },
LongMa = { Length = 26 },
},
SignalMa = { Length = 9 }
};
_resistanceIndicator = new Highest { Length = 20 };
_supportIndicator = new Lowest { Length = 10 };

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx(_ema, _macd, _resistanceIndicator, _supportIndicator, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _ema);
DrawIndicator(area, _macd);
}
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue macdValue, IIndicatorValue resistanceValue, IIndicatorValue supportValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!emaValue.IsFinal || !macdValue.IsFinal || !resistanceValue.IsFinal || !supportValue.IsFinal)
return;

var ema = emaValue.ToDecimal();
var resistance = resistanceValue.ToDecimal();
var support = supportValue.ToDecimal();
var macd = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
var macdMain = macd.Macd;
var macdSignal = macd.Signal;

if (_previousMacd is null || _previousSignal is null || _previousResistance is null || _previousSupport is null)
{
_previousMacd = macdMain;
_previousSignal = macdSignal;
_previousResistance = resistance;
_previousSupport = support;
_previousHigh = candle.HighPrice;
_previousLow = candle.LowPrice;
return;
}

if (!IsFormedAndOnlineAndAllowTrading())
{
UpdatePreviousState(macdMain, macdSignal, resistance, support, candle);
return;
}

if (_resistanceTouchCounter > 0)
_resistanceTouchCounter--;
if (_supportTouchCounter > 0)
_supportTouchCounter--;
if (_macdDownCounter > 0)
_macdDownCounter--;
if (_macdUpCounter > 0)
_macdUpCounter--;

var touchedResistance = resistance > 0m && _previousResistance.HasValue && _previousHigh > _previousResistance.Value;
var touchedSupport = support > 0m && _previousSupport.HasValue && _previousLow < _previousSupport.Value;

if (touchedResistance)
_resistanceTouchCounter = SignalValidity;
if (touchedSupport)
_supportTouchCounter = SignalValidity;

var macdCrossUp = macdMain > macdSignal && _previousMacd < _previousSignal && macdMain < 0m;
var macdCrossDown = macdMain < macdSignal && _previousMacd > _previousSignal && macdMain > 0m;

if (macdCrossUp)
_macdUpCounter = SignalValidity;
if (macdCrossDown)
_macdDownCounter = SignalValidity;

ManageOpenPosition(candle);

if (Position <= 0)
{
TryEnterLong(candle, ema, macdMain, macdSignal);
}

if (Position >= 0)
{
TryEnterShort(candle, ema, macdMain, macdSignal);
}

UpdatePreviousState(macdMain, macdSignal, resistance, support, candle);
}

private void ManageOpenPosition(ICandleMessage candle)
{
if (Position > 0)
{
if (_pendingStopPrice.HasValue && candle.LowPrice <= _pendingStopPrice.Value)
{
SellMarket(Position);
LogInfo($"Exit long: candle low {candle.LowPrice:F5} hit stop {_pendingStopPrice.Value:F5}");
ClearProtectionLevels();
return;
}

if (_pendingTakePrice.HasValue && candle.HighPrice >= _pendingTakePrice.Value)
{
SellMarket(Position);
LogInfo($"Exit long: candle high {candle.HighPrice:F5} reached target {_pendingTakePrice.Value:F5}");
ClearProtectionLevels();
}
}
else if (Position < 0)
{
if (_pendingStopPrice.HasValue && candle.HighPrice >= _pendingStopPrice.Value)
{
BuyMarket(Math.Abs(Position));
LogInfo($"Exit short: candle high {candle.HighPrice:F5} hit stop {_pendingStopPrice.Value:F5}");
ClearProtectionLevels();
return;
}

if (_pendingTakePrice.HasValue && candle.LowPrice <= _pendingTakePrice.Value)
{
BuyMarket(Math.Abs(Position));
LogInfo($"Exit short: candle low {candle.LowPrice:F5} reached target {_pendingTakePrice.Value:F5}");
ClearProtectionLevels();
}
}
else
{
ClearProtectionLevels();
}
}

private void TryEnterLong(ICandleMessage candle, decimal ema, decimal macdMain, decimal macdSignal)
{
var hasMacdSignal = _macdUpCounter > 0;
var hasSupportSignal = _supportTouchCounter > 0;
var isFreshSignal = _macdUpCounter == SignalValidity || _supportTouchCounter == SignalValidity;

if (!hasMacdSignal || !hasSupportSignal || !isFreshSignal)
return;

if (candle.ClosePrice <= ema)
return;

var volume = GetEntryVolumeForLong();
if (volume <= 0m)
return;

var stepOffset = GetPriceOffset(StopDistancePoints);
var stop = ema - stepOffset;
var diff = candle.ClosePrice - ema + stepOffset;
var target = candle.ClosePrice + diff * 1.5m;

BuyMarket(volume);
_pendingStopPrice = stop;
_pendingTakePrice = target;

LogInfo($"Enter long: close {candle.ClosePrice:F5} above EMA {ema:F5}, MACD {macdMain:F5}/{macdSignal:F5}, stop {stop:F5}, target {target:F5}");
}

private void TryEnterShort(ICandleMessage candle, decimal ema, decimal macdMain, decimal macdSignal)
{
var hasMacdSignal = _macdDownCounter > 0;
var hasResistanceSignal = _resistanceTouchCounter > 0;
var isFreshSignal = _macdDownCounter == SignalValidity || _resistanceTouchCounter == SignalValidity;

if (!hasMacdSignal || !hasResistanceSignal || !isFreshSignal)
return;

if (candle.ClosePrice >= ema)
return;

var volume = GetEntryVolumeForShort();
if (volume <= 0m)
return;

var stepOffset = GetPriceOffset(StopDistancePoints);
var stop = ema + stepOffset;
var diff = ema - candle.ClosePrice + stepOffset;
var target = candle.ClosePrice - diff * 1.5m;

SellMarket(volume);
_pendingStopPrice = stop;
_pendingTakePrice = target;

LogInfo($"Enter short: close {candle.ClosePrice:F5} below EMA {ema:F5}, MACD {macdMain:F5}/{macdSignal:F5}, stop {stop:F5}, target {target:F5}");
}

private decimal GetEntryVolumeForLong()
{
var volume = OrderVolume;
if (Position < 0)
volume += Math.Abs(Position);
return volume;
}

private decimal GetEntryVolumeForShort()
{
var volume = OrderVolume;
if (Position > 0)
volume += Position;
return volume;
}

private decimal GetPriceOffset(int points)
{
if (points <= 0)
return 0m;

var step = Security?.PriceStep;
if (step is null || step.Value <= 0m)
return points;

return step.Value * points;
}

private void UpdatePreviousState(decimal macdMain, decimal macdSignal, decimal resistance, decimal support, ICandleMessage candle)
{
_previousMacd = macdMain;
_previousSignal = macdSignal;
_previousResistance = resistance;
_previousSupport = support;
_previousHigh = candle.HighPrice;
_previousLow = candle.LowPrice;
}

private void ClearProtectionLevels()
{
_pendingStopPrice = null;
_pendingTakePrice = null;
}
}
