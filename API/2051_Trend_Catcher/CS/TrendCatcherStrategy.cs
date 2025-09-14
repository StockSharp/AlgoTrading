using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR trend catching strategy.
/// Uses Parabolic SAR and multiple moving averages for entries.
/// Includes dynamic stop-loss, take-profit, break-even and trailing stop.
/// </summary>
public class TrendCatcherStrategy : Strategy
{
private readonly StrategyParam<int> _slowMaPeriod;
private readonly StrategyParam<int> _fastMaPeriod;
private readonly StrategyParam<int> _fastMa2Period;
private readonly StrategyParam<decimal> _sarStep;
private readonly StrategyParam<decimal> _sarMax;
private readonly StrategyParam<decimal> _slMultiplier;
private readonly StrategyParam<decimal> _tpMultiplier;
private readonly StrategyParam<decimal> _minStopLoss;
private readonly StrategyParam<decimal> _maxStopLoss;
private readonly StrategyParam<decimal> _profitLevel;
private readonly StrategyParam<decimal> _breakevenOffset;
private readonly StrategyParam<decimal> _trailingThreshold;
private readonly StrategyParam<decimal> _trailingDistance;
private readonly StrategyParam<DataType> _candleType;

private bool _isInitialized;
private bool _isPriceAboveSarPrev;
private decimal _entryPrice;
private decimal _stopPrice;
private decimal _takePrice;
private bool _breakevenSet;

/// <summary>
/// Slow moving average period.
/// </summary>
public int SlowMaPeriod { get => _slowMaPeriod.Value; set => _slowMaPeriod.Value = value; }

/// <summary>
/// First fast moving average period.
/// </summary>
public int FastMaPeriod { get => _fastMaPeriod.Value; set => _fastMaPeriod.Value = value; }

/// <summary>
/// Second fast moving average period.
/// </summary>
public int FastMa2Period { get => _fastMa2Period.Value; set => _fastMa2Period.Value = value; }

/// <summary>
/// Parabolic SAR step.
/// </summary>
public decimal SarStep { get => _sarStep.Value; set => _sarStep.Value = value; }

/// <summary>
/// Parabolic SAR maximum step.
/// </summary>
public decimal SarMax { get => _sarMax.Value; set => _sarMax.Value = value; }

/// <summary>
/// Multiplier for stop-loss distance.
/// </summary>
public decimal SlMultiplier { get => _slMultiplier.Value; set => _slMultiplier.Value = value; }

/// <summary>
/// Multiplier for take-profit distance.
/// </summary>
public decimal TpMultiplier { get => _tpMultiplier.Value; set => _tpMultiplier.Value = value; }

/// <summary>
/// Minimum stop-loss distance.
/// </summary>
public decimal MinStopLoss { get => _minStopLoss.Value; set => _minStopLoss.Value = value; }

/// <summary>
/// Maximum stop-loss distance.
/// </summary>
public decimal MaxStopLoss { get => _maxStopLoss.Value; set => _maxStopLoss.Value = value; }

/// <summary>
/// Profit distance to move stop to breakeven.
/// </summary>
public decimal ProfitLevel { get => _profitLevel.Value; set => _profitLevel.Value = value; }

/// <summary>
/// Additional points for breakeven stop.
/// </summary>
public decimal BreakevenOffset { get => _breakevenOffset.Value; set => _breakevenOffset.Value = value; }

/// <summary>
/// Profit distance to enable trailing stop.
/// </summary>
public decimal TrailingThreshold { get => _trailingThreshold.Value; set => _trailingThreshold.Value = value; }

/// <summary>
/// Trailing stop distance.
/// </summary>
public decimal TrailingDistance { get => _trailingDistance.Value; set => _trailingDistance.Value = value; }

/// <summary>
/// The type of candles to use.
/// </summary>
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

/// <summary>
/// Constructor.
/// </summary>
public TrendCatcherStrategy()
{
_slowMaPeriod = Param(nameof(SlowMaPeriod), 200)
.SetDisplay("Slow MA Period", "Period of the slow moving average", "Moving Averages")
.SetCanOptimize(true)
.SetOptimize(100, 300, 50);

_fastMaPeriod = Param(nameof(FastMaPeriod), 50)
.SetDisplay("Fast MA Period", "Period of the first fast moving average", "Moving Averages")
.SetCanOptimize(true)
.SetOptimize(20, 80, 10);

_fastMa2Period = Param(nameof(FastMa2Period), 25)
.SetDisplay("Second Fast MA Period", "Period of the second fast moving average", "Moving Averages")
.SetCanOptimize(true)
.SetOptimize(10, 50, 5);

_sarStep = Param(nameof(SarStep), 0.004m)
.SetDisplay("SAR Step", "Parabolic SAR acceleration step", "Parabolic SAR")
.SetCanOptimize(true)
.SetOptimize(0.002m, 0.01m, 0.002m);

_sarMax = Param(nameof(SarMax), 0.2m)
.SetDisplay("SAR Max", "Parabolic SAR maximum acceleration", "Parabolic SAR")
.SetCanOptimize(true)
.SetOptimize(0.1m, 0.4m, 0.1m);

_slMultiplier = Param(nameof(SlMultiplier), 1m)
.SetDisplay("SL Multiplier", "Multiplier applied to SAR distance for stop-loss", "Risk")
.SetCanOptimize(true);

_tpMultiplier = Param(nameof(TpMultiplier), 1m)
.SetDisplay("TP Multiplier", "Take-profit as multiple of stop-loss", "Risk")
.SetCanOptimize(true);

_minStopLoss = Param(nameof(MinStopLoss), 10m)
.SetDisplay("Min Stop Loss", "Minimum allowed stop-loss distance", "Risk");

_maxStopLoss = Param(nameof(MaxStopLoss), 200m)
.SetDisplay("Max Stop Loss", "Maximum allowed stop-loss distance", "Risk");

_profitLevel = Param(nameof(ProfitLevel), 500m)
.SetDisplay("Breakeven Trigger", "Profit distance to move stop-loss to breakeven", "Trailing");

_breakevenOffset = Param(nameof(BreakevenOffset), 1m)
.SetDisplay("Breakeven Offset", "Extra points added to breakeven stop", "Trailing");

_trailingThreshold = Param(nameof(TrailingThreshold), 500m)
.SetDisplay("Trailing Trigger", "Profit distance to activate trailing stop", "Trailing");

_trailingDistance = Param(nameof(TrailingDistance), 10m)
.SetDisplay("Trailing Distance", "Distance for trailing stop once activated", "Trailing");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
_isInitialized = false;
_isPriceAboveSarPrev = false;
_entryPrice = 0;
_stopPrice = 0;
_takePrice = 0;
_breakevenSet = false;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var sar = new ParabolicSar
{
Acceleration = SarStep,
AccelerationStep = SarStep,
AccelerationMax = SarMax
};
var slowMa = new SMA { Length = SlowMaPeriod };
var fastMa = new SMA { Length = FastMaPeriod };
var fastMa2 = new SMA { Length = FastMa2Period };

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(sar, fastMa, fastMa2, slowMa, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, sar);
DrawIndicator(area, slowMa);
DrawIndicator(area, fastMa);
DrawIndicator(area, fastMa2);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal sarValue, decimal fastValue, decimal fast2Value, decimal slowValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var isPriceAboveSar = candle.ClosePrice > sarValue;

if (!_isInitialized)
{
_isPriceAboveSarPrev = isPriceAboveSar;
_isInitialized = true;
return;
}

var buySignal = isPriceAboveSar && !_isPriceAboveSarPrev && fastValue > slowValue && candle.ClosePrice > fast2Value;
var sellSignal = !isPriceAboveSar && _isPriceAboveSarPrev && fastValue < slowValue && candle.ClosePrice < fast2Value;

if (buySignal && Position <= 0)
EnterLong(candle, sarValue);
else if (sellSignal && Position >= 0)
EnterShort(candle, sarValue);
else if (buySignal && Position < 0)
BuyMarket(-Position);
else if (sellSignal && Position > 0)
SellMarket(Position);

if (Position != 0)
CheckExit(candle);

_isPriceAboveSarPrev = isPriceAboveSar;
}

private void EnterLong(ICandleMessage candle, decimal sarValue)
{
var distance = Math.Abs(candle.ClosePrice - sarValue) * SlMultiplier;
distance = Math.Min(Math.Max(distance, MinStopLoss), MaxStopLoss);

_entryPrice = candle.ClosePrice;
_stopPrice = _entryPrice - distance;
_takePrice = _entryPrice + distance * TpMultiplier;
_breakevenSet = false;

BuyMarket(Volume + Math.Abs(Position));
}

private void EnterShort(ICandleMessage candle, decimal sarValue)
{
var distance = Math.Abs(candle.ClosePrice - sarValue) * SlMultiplier;
distance = Math.Min(Math.Max(distance, MinStopLoss), MaxStopLoss);

_entryPrice = candle.ClosePrice;
_stopPrice = _entryPrice + distance;
_takePrice = _entryPrice - distance * TpMultiplier;
_breakevenSet = false;

SellMarket(Volume + Math.Abs(Position));
}

private void CheckExit(ICandleMessage candle)
{
var price = candle.ClosePrice;
if (Position > 0)
{
var profit = price - _entryPrice;

if (!_breakevenSet && profit >= ProfitLevel)
{
_stopPrice = _entryPrice + BreakevenOffset;
_breakevenSet = true;
}

if (profit >= TrailingThreshold)
{
var newStop = price - TrailingDistance;
if (newStop > _stopPrice)
_stopPrice = newStop;
}

if (price <= _stopPrice || price >= _takePrice)
SellMarket(Position);
}
else if (Position < 0)
{
var profit = _entryPrice - price;

if (!_breakevenSet && profit >= ProfitLevel)
{
_stopPrice = _entryPrice - BreakevenOffset;
_breakevenSet = true;
}

if (profit >= TrailingThreshold)
{
var newStop = price + TrailingDistance;
if (newStop < _stopPrice)
_stopPrice = newStop;
}

if (price >= _stopPrice || price <= _takePrice)
BuyMarket(-Position);
}
}
}
