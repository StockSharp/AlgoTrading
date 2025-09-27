using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the OzFx MetaTrader expert advisor that combines the Accelerator Oscillator and stochastic filter.
/// Opens five layered market orders whenever the momentum crosses the midline and the stochastic is aligned.
/// Mirrors the breakeven tightening that the original advisor applies after the first take-profit is reached.
/// </summary>
public class OzFxSimpleStrategy : Strategy
{
private readonly StrategyParam<int> _layersCount;
private readonly StrategyParam<int> _awesomeShortPeriod;
private readonly StrategyParam<int> _awesomeLongPeriod;
private readonly StrategyParam<int> _acceleratorAveragePeriod;
private readonly StrategyParam<int> _stochasticKSmooth;
private readonly StrategyParam<int> _stochasticDSmooth;

private readonly StrategyParam<decimal> _orderVolume;
private readonly StrategyParam<decimal> _stopLossPips;
private readonly StrategyParam<decimal> _takeProfitPips;
private readonly StrategyParam<decimal> _stochasticLevel;
private readonly StrategyParam<int> _stochasticLength;
private readonly StrategyParam<DataType> _candleType;

private AwesomeOscillator _awesomeOscillator = null!;
private SimpleMovingAverage _acceleratorAverage = null!;
private StochasticOscillator _stochastic = null!;

private readonly List<LayerInfo> _longLayers = new();
private readonly List<LayerInfo> _shortLayers = new();

private decimal? _previousAccelerator;
private bool _longBreakevenActive;
private bool _shortBreakevenActive;
private bool _longBlocked;
private bool _shortBlocked;
private DateTimeOffset? _lastEntryBar;

private decimal _pipSize;
private bool _pipInitialized;

private int _longCampaignId;
private int _shortCampaignId;

/// <summary>
/// Stores metadata for a single layered position.
/// </summary>
private sealed class LayerInfo
{
public decimal Volume;
public decimal EntryPrice;
public decimal? StopPrice;
public decimal? TakeProfitPrice;
public int CampaignId;
public int Index;
}

/// <summary>
/// Volume of each submitted market order layer.
/// </summary>
public int LayersCount
{
get => _layersCount.Value;
set => _layersCount.Value = value;
}

/// <summary>
/// Short period used for the Awesome Oscillator.
/// </summary>
public int AwesomeShortPeriod
{
get => _awesomeShortPeriod.Value;
set => _awesomeShortPeriod.Value = value;
}

/// <summary>
/// Long period used for the Awesome Oscillator.
/// </summary>
public int AwesomeLongPeriod
{
get => _awesomeLongPeriod.Value;
set => _awesomeLongPeriod.Value = value;
}

/// <summary>
/// Length of the moving average applied to the accelerator oscillator.
/// </summary>
public int AcceleratorAveragePeriod
{
get => _acceleratorAveragePeriod.Value;
set => _acceleratorAveragePeriod.Value = value;
}

/// <summary>
/// Smoothing factor of the stochastic %K line.
/// </summary>
public int StochasticKSmooth
{
get => _stochasticKSmooth.Value;
set => _stochasticKSmooth.Value = value;
}

/// <summary>
/// Smoothing factor of the stochastic %D line.
/// </summary>
public int StochasticDSmooth
{
get => _stochasticDSmooth.Value;
set => _stochasticDSmooth.Value = value;
}

public decimal OrderVolume
{
get => _orderVolume.Value;
set => _orderVolume.Value = value;
}

/// <summary>
/// Stop loss distance expressed in pips.
/// </summary>
public decimal StopLossPips
{
get => _stopLossPips.Value;
set => _stopLossPips.Value = value;
}

/// <summary>
/// Base take profit distance in pips.
/// </summary>
public decimal TakeProfitPips
{
get => _takeProfitPips.Value;
set => _takeProfitPips.Value = value;
}

/// <summary>
/// Stochastic threshold separating bullish and bearish momentum.
/// </summary>
public decimal StochasticLevel
{
get => _stochasticLevel.Value;
set => _stochasticLevel.Value = value;
}

/// <summary>
/// Main lookback length of the stochastic %K line.
/// </summary>
public int StochasticLength
{
get => _stochasticLength.Value;
set => _stochasticLength.Value = value;
}

/// <summary>
/// Candle type used for calculations and trading decisions.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initializes <see cref="OzFxSimpleStrategy"/> with default parameters.
/// </summary>
public OzFxSimpleStrategy()
{
_layersCount = Param(nameof(LayersCount), 5)
.SetGreaterThanZero()
.SetDisplay("Layers", "Number of layered market orders opened per signal.", "Execution")
.SetCanOptimize(true)
.SetOptimize(3, 7, 1);

_awesomeShortPeriod = Param(nameof(AwesomeShortPeriod), 5)
.SetGreaterThanZero()
.SetDisplay("AO Fast", "Short period of the Awesome Oscillator.", "Indicators")
.SetCanOptimize(true)
.SetOptimize(3, 10, 1);

_awesomeLongPeriod = Param(nameof(AwesomeLongPeriod), 34)
.SetGreaterThanZero()
.SetDisplay("AO Slow", "Long period of the Awesome Oscillator.", "Indicators")
.SetCanOptimize(true)
.SetOptimize(20, 60, 2);

_acceleratorAveragePeriod = Param(nameof(AcceleratorAveragePeriod), 5)
.SetGreaterThanZero()
.SetDisplay("Accelerator MA", "Length of the moving average smoothing the accelerator oscillator.", "Indicators")
.SetCanOptimize(true)
.SetOptimize(3, 10, 1);

_stochasticKSmooth = Param(nameof(StochasticKSmooth), 3)
.SetGreaterThanZero()
.SetDisplay("Stochastic %K Smooth", "Smoothing period applied to %K.", "Indicators")
.SetCanOptimize(true)
.SetOptimize(1, 5, 1);

_stochasticDSmooth = Param(nameof(StochasticDSmooth), 3)
.SetGreaterThanZero()
.SetDisplay("Stochastic %D Smooth", "Smoothing period applied to %D.", "Indicators")
.SetCanOptimize(true)
.SetOptimize(1, 5, 1);

_orderVolume = Param(nameof(OrderVolume), 0.1m)
.SetGreaterThanZero()
.SetDisplay("Order Volume", "Lot size per each market order layer.", "Trading")
.SetCanOptimize(true)
.SetOptimize(0.05m, 0.5m, 0.05m);

_stopLossPips = Param(nameof(StopLossPips), 100m)
.SetGreaterThanZero()
.SetDisplay("Stop Loss (pips)", "Protective stop distance applied to every order.", "Risk")
.SetCanOptimize(true)
.SetOptimize(50m, 200m, 25m);

_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
.SetGreaterThanZero()
.SetDisplay("Take Profit Step (pips)", "Distance between consecutive take-profit layers.", "Risk")
.SetCanOptimize(true)
.SetOptimize(25m, 150m, 25m);

_stochasticLevel = Param(nameof(StochasticLevel), 50m)
.SetDisplay("Stochastic Level", "%K threshold that splits bullish and bearish regimes.", "Signals")
.SetCanOptimize(true)
.SetOptimize(40m, 60m, 5m);

_stochasticLength = Param(nameof(StochasticLength), 5)
.SetGreaterThanZero()
.SetDisplay("Stochastic Length", "Lookback period of the stochastic %K.", "Signals")
.SetCanOptimize(true)
.SetOptimize(5, 14, 1);

_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
.SetDisplay("Candle Type", "Source candle series processed by the strategy.", "Data");
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

_longLayers.Clear();
_shortLayers.Clear();
_previousAccelerator = null;
_longBreakevenActive = false;
_shortBreakevenActive = false;
_longBlocked = false;
_shortBlocked = false;
_lastEntryBar = null;
_pipInitialized = false;
_pipSize = 0m;
_longCampaignId = 0;
_shortCampaignId = 0;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_awesomeOscillator = new AwesomeOscillator
{
ShortPeriod = AwesomeShortPeriod,
LongPeriod = AwesomeLongPeriod,
};

_acceleratorAverage = new SimpleMovingAverage
{
Length = AcceleratorAveragePeriod,
};

_stochastic = new StochasticOscillator
{
Length = StochasticLength,
K = { Length = StochasticKSmooth },
D = { Length = StochasticDSmooth },
};

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx(_awesomeOscillator, _stochastic, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _awesomeOscillator);
DrawIndicator(area, _stochastic);
DrawOwnTrades(area);
}
}

/// <summary>
/// Handles finished candles, evaluates signals, and manages active layers.
/// </summary>
private void ProcessCandle(ICandleMessage candle, IIndicatorValue awesomeValue, IIndicatorValue stochasticValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!awesomeValue.IsFinal || !stochasticValue.IsFinal)
return;

var stochastic = (StochasticOscillatorValue)stochasticValue;
if (stochastic.K is not decimal stochasticK)
return;

var awesome = awesomeValue.GetValue<decimal>();
var acceleratorAverage = _acceleratorAverage.Process(new DecimalIndicatorValue(_acceleratorAverage, awesome, candle.ServerTime));
if (!acceleratorAverage.IsFinal)
return;

var accelerator = awesome - acceleratorAverage.GetValue<decimal>();
var previousAccelerator = _previousAccelerator;
_previousAccelerator = accelerator;
if (previousAccelerator is null)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

ResetBlocksIfFlat();

var previousValue = previousAccelerator.Value;
var longSignal = stochasticK > StochasticLevel && accelerator > previousValue && accelerator > 0m && previousValue < 0m;
var shortSignal = stochasticK < StochasticLevel && accelerator < previousValue && accelerator < 0m && previousValue > 0m;

var pipSize = GetPipSize();
var stopDistance = StopLossPips > 0m ? StopLossPips * pipSize : 0m;
var takeDistance = TakeProfitPips > 0m ? TakeProfitPips * pipSize : 0m;

if (longSignal)
TryEnterLong(candle, stopDistance, takeDistance);

if (shortSignal)
TryEnterShort(candle, stopDistance, takeDistance);

ManageLongLayers(candle, longSignal, shortSignal);
ManageShortLayers(candle, longSignal, shortSignal);
}

/// <summary>
/// Opens a five-layer long stack if the conditions align.
/// </summary>
private void TryEnterLong(ICandleMessage candle, decimal stopDistance, decimal takeDistance)
{
if (_longBlocked || _shortLayers.Count != 0)
return;

if (_lastEntryBar == candle.OpenTime)
return;

var volume = OrderVolume;
if (volume <= 0m)
return;

var entryPrice = candle.ClosePrice;
var stopPrice = stopDistance > 0m ? entryPrice - stopDistance : (decimal?)null;

_longLayers.Clear();
_longCampaignId++;
_longBreakevenActive = false;

for (var index = 0; index < LayersCount; index++)
{
BuyMarket(volume);

decimal? takePrice = null;
if (index < LayersCount - 1 && takeDistance > 0m)
takePrice = entryPrice + takeDistance * (index + 1);

_longLayers.Add(new LayerInfo
{
Volume = volume,
EntryPrice = entryPrice,
StopPrice = stopPrice,
TakeProfitPrice = takePrice,
CampaignId = _longCampaignId,
Index = index,
});
}

_longBlocked = true;
_shortBlocked = false;
_lastEntryBar = candle.OpenTime;
LogInfo($"Opened long stack at {entryPrice}. Stop: {stopPrice}, take step: {takeDistance}.");
}

/// <summary>
/// Opens a five-layer short stack if the conditions align.
/// </summary>
private void TryEnterShort(ICandleMessage candle, decimal stopDistance, decimal takeDistance)
{
if (_shortBlocked || _longLayers.Count != 0)
return;

if (_lastEntryBar == candle.OpenTime)
return;

var volume = OrderVolume;
if (volume <= 0m)
return;

var entryPrice = candle.ClosePrice;
var stopPrice = stopDistance > 0m ? entryPrice + stopDistance : (decimal?)null;

_shortLayers.Clear();
_shortCampaignId++;
_shortBreakevenActive = false;

for (var index = 0; index < LayersCount; index++)
{
SellMarket(volume);

decimal? takePrice = null;
if (index < LayersCount - 1 && takeDistance > 0m)
takePrice = entryPrice - takeDistance * (index + 1);

_shortLayers.Add(new LayerInfo
{
Volume = volume,
EntryPrice = entryPrice,
StopPrice = stopPrice,
TakeProfitPrice = takePrice,
CampaignId = _shortCampaignId,
Index = index,
});
}

_shortBlocked = true;
_longBlocked = false;
_lastEntryBar = candle.OpenTime;
LogInfo($"Opened short stack at {entryPrice}. Stop: {stopPrice}, take step: {takeDistance}.");
}

/// <summary>
/// Applies exits and breakeven tightening for long layers.
/// </summary>
private void ManageLongLayers(ICandleMessage candle, bool longSignal, bool shortSignal)
{
if (_longLayers.Count == 0)
return;

var high = candle.HighPrice;
var low = candle.LowPrice;

for (var i = _longLayers.Count - 1; i >= 0; i--)
{
var layer = _longLayers[i];

if (layer.TakeProfitPrice is decimal takeProfit && high >= takeProfit)
{
SellMarket(layer.Volume);
_longLayers.RemoveAt(i);

if (!_longBreakevenActive)
{
_longBreakevenActive = true;
ApplyLongBreakeven();
}

continue;
}

if (layer.StopPrice is decimal stop && low <= stop)
{
SellMarket(layer.Volume);
_longLayers.RemoveAt(i);
}
}

if (shortSignal)
{
decimal totalVolume = 0m;
for (var i = 0; i < _longLayers.Count; i++)
totalVolume += _longLayers[i].Volume;

if (totalVolume > 0m)
SellMarket(totalVolume);

_longLayers.Clear();
_longBreakevenActive = false;
_longBlocked = false;
}

if (_longLayers.Count == 0)
{
_longBreakevenActive = false;
_longBlocked = false;
}
}

/// <summary>
/// Applies exits and breakeven tightening for short layers.
/// </summary>
private void ManageShortLayers(ICandleMessage candle, bool longSignal, bool shortSignal)
{
if (_shortLayers.Count == 0)
return;

var high = candle.HighPrice;
var low = candle.LowPrice;

for (var i = _shortLayers.Count - 1; i >= 0; i--)
{
var layer = _shortLayers[i];

if (layer.TakeProfitPrice is decimal takeProfit && low <= takeProfit)
{
BuyMarket(layer.Volume);
_shortLayers.RemoveAt(i);

if (!_shortBreakevenActive)
{
_shortBreakevenActive = true;
ApplyShortBreakeven();
}

continue;
}

if (layer.StopPrice is decimal stop && high >= stop)
{
BuyMarket(layer.Volume);
_shortLayers.RemoveAt(i);
}
}

if (longSignal)
{
decimal totalVolume = 0m;
for (var i = 0; i < _shortLayers.Count; i++)
totalVolume += _shortLayers[i].Volume;

if (totalVolume > 0m)
BuyMarket(totalVolume);

_shortLayers.Clear();
_shortBreakevenActive = false;
_shortBlocked = false;
}

if (_shortLayers.Count == 0)
{
_shortBreakevenActive = false;
_shortBlocked = false;
}
}

/// <summary>
/// Moves remaining long stops to breakeven after the first take profit fires.
/// </summary>
private void ApplyLongBreakeven()
{
for (var i = 0; i < _longLayers.Count; i++)
{
var layer = _longLayers[i];
var newStop = layer.EntryPrice;
if (layer.StopPrice is decimal stop && stop > newStop)
newStop = stop;

layer.StopPrice = newStop;
_longLayers[i] = layer;
}
}

/// <summary>
/// Moves remaining short stops to breakeven after the first take profit fires.
/// </summary>
private void ApplyShortBreakeven()
{
for (var i = 0; i < _shortLayers.Count; i++)
{
var layer = _shortLayers[i];
var newStop = layer.EntryPrice;
if (layer.StopPrice is decimal stop && stop < newStop)
newStop = stop;

layer.StopPrice = newStop;
_shortLayers[i] = layer;
}
}

/// <summary>
/// Resets block flags when the strategy is flat.
/// </summary>
private void ResetBlocksIfFlat()
{
if (_longLayers.Count == 0 && _shortLayers.Count == 0)
{
_longBlocked = false;
_shortBlocked = false;
}
}

/// <summary>
/// Calculates the pip size using the security metadata.
/// </summary>
private decimal GetPipSize()
{
if (_pipInitialized)
return _pipSize;

var security = Security;
var step = security?.MinPriceStep ?? 0m;
if (step <= 0m)
step = 0.0001m;

var decimals = security?.Decimals ?? 0;
var adjust = decimals == 3 || decimals == 5 ? 10m : 1m;

_pipSize = step * adjust;
if (_pipSize <= 0m)
_pipSize = step;

if (_pipSize <= 0m)
_pipSize = 0.0001m;

_pipInitialized = true;
return _pipSize;
}
}
