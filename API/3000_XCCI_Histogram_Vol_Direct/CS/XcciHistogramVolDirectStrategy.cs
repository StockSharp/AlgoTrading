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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// XCCI Histogram Vol Direct strategy translated from the original MQL5 expert.
/// Combines Commodity Channel Index with smoothed volume to generate directional signals.
/// </summary>
public class XcciHistogramVolDirectStrategy : Strategy
{
private readonly StrategyParam<int> _cciPeriod;
private readonly StrategyParam<SmoothingMethod> _smoothingMethod;
private readonly StrategyParam<int> _smoothingLength;
private readonly StrategyParam<int> _smoothingPhase;
private readonly StrategyParam<decimal> _highLevel2;
private readonly StrategyParam<decimal> _highLevel1;
private readonly StrategyParam<decimal> _lowLevel1;
private readonly StrategyParam<decimal> _lowLevel2;
private readonly StrategyParam<int> _signalBar;
private readonly StrategyParam<bool> _allowLongEntries;
private readonly StrategyParam<bool> _allowShortEntries;
private readonly StrategyParam<bool> _allowLongExits;
private readonly StrategyParam<bool> _allowShortExits;
private readonly StrategyParam<decimal> _stopLossPoints;
private readonly StrategyParam<decimal> _takeProfitPoints;
private readonly StrategyParam<VolumeMode> _volumeSource;
private readonly StrategyParam<DataType> _candleType;

private CommodityChannelIndex _cci = null!;
private IIndicator _cciVolumeSmoother = null!;
private IIndicator _volumeSmoother = null!;
private readonly List<int> _colorHistory = new();
private decimal? _prevSmoothedValue;

/// <summary>
/// Available smoothing methods inspired by the original indicator.
/// </summary>
public enum SmoothingMethod
{
Sma,
Ema,
Smma,
Lwma,
Jjma,
Jurx,
Parabolic,
T3,
Vidya,
Ama
}

/// <summary>
/// Volume source used to weight the CCI values.
/// </summary>
public enum VolumeMode
{
Tick,
Real
}

/// <summary>
/// CCI period length.
/// </summary>
public int CciPeriod
{
get => _cciPeriod.Value;
set => _cciPeriod.Value = value;
}

/// <summary>
/// Selected smoothing method for both CCI*volume and pure volume streams.
/// </summary>
public SmoothingMethod Smoothing
{
get => _smoothingMethod.Value;
set => _smoothingMethod.Value = value;
}

/// <summary>
/// Length for the smoothing filters.
/// </summary>
public int SmoothingLength
{
get => _smoothingLength.Value;
set => _smoothingLength.Value = value;
}

/// <summary>
/// Phase parameter kept for compatibility with the original indicator.
/// Not every smoothing type uses this value directly.
/// </summary>
public int SmoothingPhase
{
get => _smoothingPhase.Value;
set => _smoothingPhase.Value = value;
}

/// <summary>
/// Upper extreme multiplier.
/// </summary>
public decimal HighLevel2
{
get => _highLevel2.Value;
set => _highLevel2.Value = value;
}

/// <summary>
/// Upper inner band multiplier.
/// </summary>
public decimal HighLevel1
{
get => _highLevel1.Value;
set => _highLevel1.Value = value;
}

/// <summary>
/// Lower inner band multiplier.
/// </summary>
public decimal LowLevel1
{
get => _lowLevel1.Value;
set => _lowLevel1.Value = value;
}

/// <summary>
/// Lower extreme multiplier.
/// </summary>
public decimal LowLevel2
{
get => _lowLevel2.Value;
set => _lowLevel2.Value = value;
}

/// <summary>
/// Index of the signal bar (0 = latest closed candle).
/// </summary>
public int SignalBar
{
get => _signalBar.Value;
set => _signalBar.Value = value;
}

/// <summary>
/// Allow opening long positions.
/// </summary>
public bool AllowLongEntries
{
get => _allowLongEntries.Value;
set => _allowLongEntries.Value = value;
}

/// <summary>
/// Allow opening short positions.
/// </summary>
public bool AllowShortEntries
{
get => _allowShortEntries.Value;
set => _allowShortEntries.Value = value;
}

/// <summary>
/// Allow closing long positions on opposite signals.
/// </summary>
public bool AllowLongExits
{
get => _allowLongExits.Value;
set => _allowLongExits.Value = value;
}

/// <summary>
/// Allow closing short positions on opposite signals.
/// </summary>
public bool AllowShortExits
{
get => _allowShortExits.Value;
set => _allowShortExits.Value = value;
}

/// <summary>
/// Stop-loss distance expressed in price points.
/// </summary>
public decimal StopLossPoints
{
get => _stopLossPoints.Value;
set => _stopLossPoints.Value = value;
}

/// <summary>
/// Take-profit distance expressed in price points.
/// </summary>
public decimal TakeProfitPoints
{
get => _takeProfitPoints.Value;
set => _takeProfitPoints.Value = value;
}

/// <summary>
/// Volume mode used for weighting.
/// </summary>
public VolumeMode VolumeSource
{
get => _volumeSource.Value;
set => _volumeSource.Value = value;
}

/// <summary>
/// Candle type used for calculations.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initializes <see cref="XcciHistogramVolDirectStrategy"/>.
/// </summary>
public XcciHistogramVolDirectStrategy()
{
_cciPeriod = Param(nameof(CciPeriod), 14)
.SetGreaterThanZero()
.SetDisplay("CCI Period", "Length for Commodity Channel Index", "Indicators")
.SetCanOptimize(true)
.SetOptimize(10, 40, 2);

_smoothingMethod = Param(nameof(Smoothing), SmoothingMethod.T3)
.SetDisplay("Smoothing Method", "Type of smoothing applied to CCI*volume", "Indicators");

_smoothingLength = Param(nameof(SmoothingLength), 12)
.SetGreaterThanZero()
.SetDisplay("Smoothing Length", "Number of periods for smoothing filters", "Indicators")
.SetCanOptimize(true)
.SetOptimize(5, 30, 1);

_smoothingPhase = Param(nameof(SmoothingPhase), 15)
.SetDisplay("Smoothing Phase", "Phase/offset parameter used by specific smoothers", "Indicators");

_highLevel2 = Param(nameof(HighLevel2), 100m)
.SetDisplay("High Level 2", "Outer bullish threshold multiplier", "Bands");

_highLevel1 = Param(nameof(HighLevel1), 80m)
.SetDisplay("High Level 1", "Inner bullish threshold multiplier", "Bands");

_lowLevel1 = Param(nameof(LowLevel1), -80m)
.SetDisplay("Low Level 1", "Inner bearish threshold multiplier", "Bands");

_lowLevel2 = Param(nameof(LowLevel2), -100m)
.SetDisplay("Low Level 2", "Outer bearish threshold multiplier", "Bands");

_signalBar = Param(nameof(SignalBar), 1)
.SetGreaterThanOrEqualToZero()
.SetDisplay("Signal Bar", "Number of closed candles to look back", "Signals");

_allowLongEntries = Param(nameof(AllowLongEntries), true)
.SetDisplay("Allow Long Entries", "Enable opening buy positions", "Trading");

_allowShortEntries = Param(nameof(AllowShortEntries), true)
.SetDisplay("Allow Short Entries", "Enable opening sell positions", "Trading");

_allowLongExits = Param(nameof(AllowLongExits), true)
.SetDisplay("Allow Long Exits", "Enable closing buy positions on sell signals", "Trading");

_allowShortExits = Param(nameof(AllowShortExits), true)
.SetDisplay("Allow Short Exits", "Enable closing sell positions on buy signals", "Trading");

_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
.SetGreaterThanOrEqualToZero()
.SetDisplay("Stop Loss Points", "Protective stop distance in price points", "Risk Management");

_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
.SetGreaterThanOrEqualToZero()
.SetDisplay("Take Profit Points", "Profit target distance in price points", "Risk Management");

_volumeSource = Param(nameof(VolumeSource), VolumeMode.Tick)
.SetDisplay("Volume Source", "Volume stream used for weighting", "Indicators");

_candleType = Param(nameof(CandleType), TimeSpan.FromHours(2).TimeFrame())
.SetDisplay("Candle Type", "Primary timeframe used by the strategy", "General");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
=> [(Security, CandleType)];

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();

_colorHistory.Clear();
_prevSmoothedValue = null;

_cci?.Reset();
_cciVolumeSmoother?.Reset();
_volumeSmoother?.Reset();
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_cci = new CommodityChannelIndex { Length = CciPeriod };
_cciVolumeSmoother = CreateSmoother(Smoothing, SmoothingLength, SmoothingPhase);
_volumeSmoother = CreateSmoother(Smoothing, SmoothingLength, SmoothingPhase);

_colorHistory.Clear();
_prevSmoothedValue = null;

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(_cci, ProcessCandle)
.Start();

var priceArea = CreateChartArea();
if (priceArea != null)
{
DrawCandles(priceArea, subscription);
DrawOwnTrades(priceArea);

var indicatorArea = CreateChartArea();
if (indicatorArea != null)
{
DrawIndicator(indicatorArea, _cciVolumeSmoother);
}
}

var step = Security?.PriceStep ?? 1m;
Unit takeProfit = TakeProfitPoints > 0m ? new Unit(TakeProfitPoints * step, UnitTypes.Point) : null;
Unit stopLoss = StopLossPoints > 0m ? new Unit(StopLossPoints * step, UnitTypes.Point) : null;

StartProtection(takeProfit, stopLoss);
}

private void ProcessCandle(ICandleMessage candle, decimal cciValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (!_cci.IsFormed)
return;

var volume = GetVolume(candle);
var weightedCci = cciValue * volume;

var smoothedCciValue = _cciVolumeSmoother.Process(candle.OpenTime, weightedCci);
var smoothedVolumeValue = _volumeSmoother.Process(candle.OpenTime, volume);

if (!_cciVolumeSmoother.IsFormed || !_volumeSmoother.IsFormed)
return;

if (smoothedCciValue.Value is not decimal smoothedCci)
return;

if (smoothedVolumeValue.Value is not decimal smoothedVolume)
return;

var color = CalculateColor(smoothedCci);
_colorHistory.Insert(0, color);

var required = Math.Max(SignalBar + 2, 3);
if (_colorHistory.Count > required)
_colorHistory.RemoveRange(required, _colorHistory.Count - required);

if (_colorHistory.Count <= SignalBar + 1)
return;

var signalColor = _colorHistory[SignalBar];
var previousColor = _colorHistory[SignalBar + 1];

var shouldCloseLong = false;
var shouldCloseShort = false;
var shouldOpenLong = false;
var shouldOpenShort = false;

if (previousColor == 0)
{
if (AllowLongEntries && signalColor == 1)
shouldOpenLong = true;

if (AllowShortExits)
shouldCloseShort = true;
}
else if (previousColor == 1)
{
if (AllowShortEntries && signalColor == 0)
shouldOpenShort = true;

if (AllowLongExits)
shouldCloseLong = true;
}

if (shouldCloseLong && Position > 0)
SellMarket(Position);

if (shouldCloseShort && Position < 0)
BuyMarket(Math.Abs(Position));

if (Volume <= 0)
return;

if (shouldOpenLong && Position <= 0)
{
var orderVolume = Volume + Math.Abs(Position);
BuyMarket(orderVolume);
}

if (shouldOpenShort && Position >= 0)
{
var orderVolume = Volume + Math.Abs(Position);
SellMarket(orderVolume);
}
}

private decimal GetVolume(ICandleMessage candle)
{
// StockSharp provides total traded volume per candle.
// Tick volume is approximated by total volume when tick counts are unavailable.
return VolumeSource switch
{
VolumeMode.Tick => candle.TotalVolume,
VolumeMode.Real => candle.TotalVolume,
_ => candle.TotalVolume
};
}

private int CalculateColor(decimal currentValue)
{
int color;

if (_prevSmoothedValue is null)
{
color = 0;
}
else if (currentValue > _prevSmoothedValue.Value)
{
color = 0;
}
else if (currentValue < _prevSmoothedValue.Value)
{
color = 1;
}
else
{
color = _colorHistory.Count > 0 ? _colorHistory[0] : 0;
}

_prevSmoothedValue = currentValue;
return color;
}

private static IIndicator CreateSmoother(SmoothingMethod method, int length, int phase)
{
var offset = 0.5m + phase / 200m;
offset = Math.Max(0m, Math.Min(1m, offset));

return method switch
{
SmoothingMethod.Sma => new SimpleMovingAverage { Length = length },
SmoothingMethod.Ema => new ExponentialMovingAverage { Length = length },
SmoothingMethod.Smma => new SmoothedMovingAverage { Length = length },
SmoothingMethod.Lwma => new WeightedMovingAverage { Length = length },
SmoothingMethod.Jjma => new JurikMovingAverage { Length = length },
SmoothingMethod.Jurx => new ZeroLagExponentialMovingAverage { Length = length },
SmoothingMethod.Parabolic => new ArnaudLegouxMovingAverage { Length = length, Offset = offset, Sigma = 6m },
SmoothingMethod.T3 => new TripleExponentialMovingAverage { Length = length },
SmoothingMethod.Vidya => new ExponentialMovingAverage { Length = length },
SmoothingMethod.Ama => new KaufmanAdaptiveMovingAverage { Length = length },
_ => new TripleExponentialMovingAverage { Length = length }
};
}
}