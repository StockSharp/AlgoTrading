using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dual phase trend regime strategy based on volatility and oscillator shifts.
/// </summary>
public class DualPhaseTrendRegimeStrategy : Strategy
{
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<TradeDirection> _direction;
private readonly StrategyParam<SignalSource> _signalSource;
private readonly StrategyParam<int> _lengthSlow;
private readonly StrategyParam<int> _lengthFast;
private readonly StrategyParam<int> _refitBars;
private readonly StrategyParam<int> _volLookback;
private readonly StrategyParam<int> _volSmoothLen;

private readonly List<decimal> _volHistory = [];
private int? _lastRefit;
private decimal? _lowCluster;
private decimal? _highCluster;
private int? _volRegime;
private int? _trendRegime;
private decimal? _lastClose;
private int _barIndex;
private decimal _prevFastOsc;
private decimal _prevSlowOsc;

private StandardDeviation _retStd = null!;
private SimpleMovingAverage _volSmooth = null!;
private LinearRegression _oscSlow = null!;
private LinearRegression _oscFast = null!;

/// <summary>
/// Trade direction options.
/// </summary>
public enum TradeDirection
{
LongShort,
LongOnly,
ShortOnly
}

/// <summary>
/// Signal source options.
/// </summary>
public enum SignalSource
{
RegimeShift,
OscillatorCross
}

/// <summary>
/// Candle type for strategy calculation.
/// </summary>
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

/// <summary>
/// Allowed trade direction.
/// </summary>
public TradeDirection Direction { get => _direction.Value; set => _direction.Value = value; }

/// <summary>
/// Source of entry signals.
/// </summary>
public SignalSource Source { get => _signalSource.Value; set => _signalSource.Value = value; }

/// <summary>
/// Slow oscillator period.
/// </summary>
public int LengthSlow { get => _lengthSlow.Value; set => _lengthSlow.Value = value; }

/// <summary>
/// Fast oscillator period.
/// </summary>
public int LengthFast { get => _lengthFast.Value; set => _lengthFast.Value = value; }

/// <summary>
/// Bars between volatility cluster recalculation.
/// </summary>
public int RefitBars { get => _refitBars.Value; set => _refitBars.Value = value; }

/// <summary>
/// Period for return volatility.
/// </summary>
public int VolLookback { get => _volLookback.Value; set => _volLookback.Value = value; }

/// <summary>
/// Smoothing length for volatility.
/// </summary>
public int VolSmoothLen { get => _volSmoothLen.Value; set => _volSmoothLen.Value = value; }

/// <summary>
/// Constructor.
/// </summary>
public DualPhaseTrendRegimeStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
.SetDisplay("Candle Type", "Timeframe for candles", "General");

_direction = Param(nameof(Direction), TradeDirection.LongShort)
.SetDisplay("Trade Direction", "Allowed trade direction", "General");

_signalSource = Param(nameof(Source), SignalSource.RegimeShift)
.SetDisplay("Signal Source", "Entry signal type", "General");

_lengthSlow = Param(nameof(LengthSlow), 36)
.SetGreaterThanZero()
.SetDisplay("Slow Osc Length", "Slow oscillator period", "Parameters");

_lengthFast = Param(nameof(LengthFast), 18)
.SetGreaterThanZero()
.SetDisplay("Fast Osc Length", "Fast oscillator period", "Parameters");

_refitBars = Param(nameof(RefitBars), 50)
.SetGreaterThanZero()
.SetDisplay("Volatility Refit Interval", "Bars between volatility cluster recalculation", "Volatility");

_volLookback = Param(nameof(VolLookback), 20)
.SetGreaterThanZero()
.SetDisplay("Current Volatility Period", "Return standard deviation period", "Volatility");

_volSmoothLen = Param(nameof(VolSmoothLen), 5)
.SetGreaterThanZero()
.SetDisplay("Volatility Smoothing Length", "SMA smoothing for volatility", "Volatility");
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
_volHistory.Clear();
_lastRefit = null;
_lowCluster = null;
_highCluster = null;
_volRegime = null;
_trendRegime = null;
_lastClose = null;
_barIndex = 0;
_prevFastOsc = 0m;
_prevSlowOsc = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_retStd = new StandardDeviation { Length = VolLookback };
_volSmooth = new SimpleMovingAverage { Length = VolSmoothLen };
_oscSlow = new LinearRegression { Length = LengthSlow };
_oscFast = new LinearRegression { Length = LengthFast };

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx(_oscSlow, _oscFast, ProcessCandle)
.Start();

StartProtection();
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue slowValue, IIndicatorValue fastValue)
{
if (candle.State != CandleStates.Finished)
return;

var slowSlope = ((LinearRegressionValue)slowValue).LinearRegSlope ?? 0m;
var fastSlope = ((LinearRegressionValue)fastValue).LinearRegSlope ?? 0m;

if (_lastClose is decimal last)
{
var ret = candle.ClosePrice / last - 1m;
var vNow = _retStd.Process(ret, candle.ServerTime, true).ToDecimal();
var vSm = _volSmooth.Process(vNow, candle.ServerTime, true).ToDecimal();

_volHistory.Add(vNow);
if (_volHistory.Count > 150)
_volHistory.RemoveAt(0);

if (_volHistory.Count >= 150 && (_lastRefit is null || _barIndex - _lastRefit >= RefitBars))
{
var (c1, c2) = MedianSplit(_volHistory);
if (c1 is decimal l && c2 is decimal h)
{
_lowCluster = _lowCluster is null ? l : _lowCluster + 0.1m * (l - _lowCluster.Value);
_highCluster = _highCluster is null ? h : _highCluster + 0.1m * (h - _highCluster.Value);
_lastRefit = _barIndex;
}
if (_lowCluster is decimal low && _highCluster is decimal high && low > high)
{
_lowCluster = high;
_highCluster = low;
}
}

if (_lowCluster is decimal lc && _highCluster is decimal hc)
_volRegime = vSm < (lc + hc) / 2m ? 0 : 1;
}

_lastClose = candle.ClosePrice;

if (!_oscSlow.IsFormed || !_oscFast.IsFormed)
{
_prevFastOsc = fastSlope;
_prevSlowOsc = slowSlope;
_barIndex++;
return;
}

var useOsc = _volRegime == 1 ? fastSlope : slowSlope;
var tr = useOsc > 0m ? 1 : useOsc < 0m ? -1 : 0;
var bullShift = tr == 1 && _trendRegime != 1;
var bearShift = tr == -1 && _trendRegime != -1;

var crossUp = _prevFastOsc <= _prevSlowOsc && fastSlope > slowSlope;
var crossDown = _prevFastOsc >= _prevSlowOsc && fastSlope < slowSlope;

bool longE, shortE, longX, shortX;
if (Source == SignalSource.RegimeShift)
{
longE = bullShift;
shortE = bearShift;
longX = bearShift;
shortX = bullShift;
}
else
{
longE = crossUp;
shortE = crossDown;
longX = crossDown;
shortX = crossUp;
}

if (longE && Direction != TradeDirection.ShortOnly)
{
if (Position < 0)
BuyMarket(-Position);
BuyMarket();
}

if (shortE && Direction != TradeDirection.LongOnly)
{
if (Position > 0)
SellMarket(Position);
SellMarket();
}

if (Position > 0 && longX)
SellMarket(Position);

if (Position < 0 && shortX)
BuyMarket(-Position);

_prevFastOsc = fastSlope;
_prevSlowOsc = slowSlope;
_trendRegime = tr;
_barIndex++;
}

private static (decimal? low, decimal? high) MedianSplit(List<decimal> values)
{
var n = values.Count;
if (n < 10)
return (null, null);

var sorted = new List<decimal>(values);
sorted.Sort();
var median = sorted[n / 2];

decimal sumLow = 0m, sumHigh = 0m;
var kLow = 0;
var kHigh = 0;

for (var i = 0; i < n; i++)
{
var v = sorted[i];
if (v < median)
{
sumLow += v;
kLow++;
}
else
{
sumHigh += v;
kHigh++;
}
}

return (kLow > 0 ? sumLow / kLow : null, kHigh > 0 ? sumHigh / kHigh : null);
}
}
