using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bayesian BBSMA Oscillator strategy combines Bollinger Bands and Bayesian probabilities with optional Bill Williams confirmation.
/// </summary>
public class BayesianBbsmaOscillatorStrategy : Strategy
{
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<int> _bbSmaPeriod;
private readonly StrategyParam<decimal> _bbStdDevMult;
private readonly StrategyParam<int> _aoFast;
private readonly StrategyParam<int> _aoSlow;
private readonly StrategyParam<int> _acFast;
private readonly StrategyParam<int> _smaPeriod;
private readonly StrategyParam<int> _bayesPeriod;
private readonly StrategyParam<decimal> _lowerThreshold;
private readonly StrategyParam<bool> _useBwConfirmation;
private readonly StrategyParam<int> _jawLength;

private BollingerBands _bollingerBands;
private SimpleMovingAverage _smaClose;
private SimpleMovingAverage _aoFastSma;
private SimpleMovingAverage _aoSlowSma;
private SimpleMovingAverage _acSma;
private SimpleMovingAverage _jawSma;

private SimpleMovingAverage _bbUpperUpSma;
private SimpleMovingAverage _bbUpperDownSma;
private SimpleMovingAverage _bbBasisUpSma;
private SimpleMovingAverage _bbBasisDownSma;
private SimpleMovingAverage _smaUpSma;
private SimpleMovingAverage _smaDownSma;

private decimal _prevAo;
private decimal _prevAc;
private decimal _prevSigmaProbsUp = 1m;
private decimal _prevSigmaProbsDown = 1m;
private decimal _prevProbPrime;

/// <summary>
/// Candle type for strategy calculation.
/// </summary>
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

/// <summary>
/// Bollinger SMA period.
/// </summary>
public int BbSmaPeriod { get => _bbSmaPeriod.Value; set => _bbSmaPeriod.Value = value; }

/// <summary>
/// Bollinger standard deviation multiplier.
/// </summary>
public decimal BbStdDevMult { get => _bbStdDevMult.Value; set => _bbStdDevMult.Value = value; }

/// <summary>
/// Fast period for Awesome Oscillator.
/// </summary>
public int AoFast { get => _aoFast.Value; set => _aoFast.Value = value; }

/// <summary>
/// Slow period for Awesome Oscillator.
/// </summary>
public int AoSlow { get => _aoSlow.Value; set => _aoSlow.Value = value; }

/// <summary>
/// Smoothing period for Accelerator Oscillator.
/// </summary>
public int AcFast { get => _acFast.Value; set => _acFast.Value = value; }

/// <summary>
/// Simple moving average period.
/// </summary>
public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }

/// <summary>
/// Bayesian lookback period.
/// </summary>
public int BayesPeriod { get => _bayesPeriod.Value; set => _bayesPeriod.Value = value; }

/// <summary>
/// Lower probability threshold.
/// </summary>
public decimal LowerThreshold { get => _lowerThreshold.Value; set => _lowerThreshold.Value = value; }

/// <summary>
/// Require Bill Williams confirmation.
/// </summary>
public bool UseBwConfirmation { get => _useBwConfirmation.Value; set => _useBwConfirmation.Value = value; }

/// <summary>
/// Alligator jaw length.
/// </summary>
public int JawLength { get => _jawLength.Value; set => _jawLength.Value = value; }

/// <summary>
/// Constructor.
/// </summary>
public BayesianBbsmaOscillatorStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Type of candles to use", "General");

_bbSmaPeriod = Param(nameof(BbSmaPeriod), 20)
.SetGreaterThanZero()
.SetDisplay("BB SMA Period", "Bollinger Bands SMA period", "Bollinger Bands")
.SetCanOptimize(true)
.SetOptimize(10, 30, 5);

_bbStdDevMult = Param(nameof(BbStdDevMult), 2.5m)
.SetDisplay("BB StdDev Mult", "Bollinger Bands standard deviation multiplier", "Bollinger Bands")
.SetCanOptimize(true)
.SetOptimize(1m, 4m, 0.5m);

_aoFast = Param(nameof(AoFast), 5)
.SetGreaterThanZero()
.SetDisplay("AO Fast", "Fast period for Awesome Oscillator", "Oscillators");

_aoSlow = Param(nameof(AoSlow), 34)
.SetGreaterThanZero()
.SetDisplay("AO Slow", "Slow period for Awesome Oscillator", "Oscillators");

_acFast = Param(nameof(AcFast), 5)
.SetGreaterThanZero()
.SetDisplay("AC Fast", "Smoothing period for Accelerator Oscillator", "Oscillators");

_smaPeriod = Param(nameof(SmaPeriod), 20)
.SetGreaterThanZero()
.SetDisplay("SMA Period", "Simple moving average period", "General");

_bayesPeriod = Param(nameof(BayesPeriod), 20)
.SetGreaterThanZero()
.SetDisplay("Bayes Period", "Lookback period for probability calculation", "Bayesian");

_lowerThreshold = Param(nameof(LowerThreshold), 15m)
.SetDisplay("Lower Threshold", "Probability threshold (%)", "Bayesian");

_useBwConfirmation = Param(nameof(UseBwConfirmation), false)
.SetDisplay("Use BW Confirmation", "Require Bill Williams confirmation", "Filters");

_jawLength = Param(nameof(JawLength), 13)
.SetGreaterThanZero()
.SetDisplay("Jaw Length", "Alligator jaw SMA length", "Filters");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
return [(Security, CandleType)];
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_bollingerBands = new BollingerBands { Length = BbSmaPeriod, Width = BbStdDevMult };
_smaClose = new SimpleMovingAverage { Length = SmaPeriod };
_aoFastSma = new SimpleMovingAverage { Length = AoFast };
_aoSlowSma = new SimpleMovingAverage { Length = AoSlow };
_acSma = new SimpleMovingAverage { Length = AcFast };
_jawSma = new SimpleMovingAverage { Length = JawLength };

_bbUpperUpSma = new SimpleMovingAverage { Length = BayesPeriod };
_bbUpperDownSma = new SimpleMovingAverage { Length = BayesPeriod };
_bbBasisUpSma = new SimpleMovingAverage { Length = BayesPeriod };
_bbBasisDownSma = new SimpleMovingAverage { Length = BayesPeriod };
_smaUpSma = new SimpleMovingAverage { Length = BayesPeriod };
_smaDownSma = new SimpleMovingAverage { Length = BayesPeriod };

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx(_bollingerBands, _smaClose, _aoFastSma, _aoSlowSma, _jawSma, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _bollingerBands);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue, IIndicatorValue smaValue,
IIndicatorValue aoFastValue, IIndicatorValue aoSlowValue, IIndicatorValue jawValue)
{
if (candle.State != CandleStates.Finished)
return;

if (bbValue is not BollingerBandsValue bb ||
bb.UpBand is not decimal bbUpper ||
bb.LowBand is not decimal bbLower ||
bb.MovingAverage is not decimal bbBasis)
return;

if (!smaValue.IsFormed || !aoFastValue.IsFormed || !aoSlowValue.IsFormed || !jawValue.IsFormed)
return;

var smaClose = smaValue.GetValue<decimal>();
var aoFast = aoFastValue.GetValue<decimal>();
var aoSlow = aoSlowValue.GetValue<decimal>();
var jaw = jawValue.GetValue<decimal>();

var ao = aoFast - aoSlow;
var aoSmaValue = _acSma.Process(ao, candle.ServerTime, true);
if (!aoSmaValue.IsFormed)
return;

var ac = ao - aoSmaValue.GetValue<decimal>();

var acIsBlue = ac > _prevAc;
var aoIsGreen = ao > _prevAo;
var acAoIsBullish = acIsBlue && aoIsGreen;
var acAoIsBearish = !acIsBlue && !aoIsGreen;
var acAoColorIndex = acAoIsBullish ? 1 : acAoIsBearish ? -1 : 0;

var pricesAreMovingAwayUpFromAlligator = candle.ClosePrice > jaw && candle.OpenPrice > jaw;
var pricesAreMovingAwayDownFromAlligator = candle.ClosePrice < jaw && candle.OpenPrice < jaw;

var probBbUpperUp = _bbUpperUpSma.Process(candle.ClosePrice > bbUpper ? 1m : 0m, candle.ServerTime, true).GetValue<decimal>();
var probBbUpperDown = _bbUpperDownSma.Process(candle.ClosePrice < bbUpper ? 1m : 0m, candle.ServerTime, true).GetValue<decimal>();
var probBbBasisUp = _bbBasisUpSma.Process(candle.ClosePrice > bbBasis ? 1m : 0m, candle.ServerTime, true).GetValue<decimal>();
var probBbBasisDown = _bbBasisDownSma.Process(candle.ClosePrice < bbBasis ? 1m : 0m, candle.ServerTime, true).GetValue<decimal>();
var probSmaUp = _smaUpSma.Process(candle.ClosePrice > smaClose ? 1m : 0m, candle.ServerTime, true).GetValue<decimal>();
var probSmaDown = _smaDownSma.Process(candle.ClosePrice < smaClose ? 1m : 0m, candle.ServerTime, true).GetValue<decimal>();

if (!_bbUpperUpSma.IsFormed || !_bbUpperDownSma.IsFormed ||
!_bbBasisUpSma.IsFormed || !_bbBasisDownSma.IsFormed ||
!_smaUpSma.IsFormed || !_smaDownSma.IsFormed)
return;

var probUpBbUpper = probBbUpperUp / (probBbUpperUp + probBbUpperDown);
var probUpBbBasis = probBbBasisUp / (probBbBasisUp + probBbBasisDown);
var probUpSma = probSmaUp / (probSmaUp + probSmaDown);

var numDown = probUpBbUpper * probUpBbBasis * probUpSma;
var denDown = numDown + (1m - probUpBbUpper) * (1m - probUpBbBasis) * (1m - probUpSma);
var sigmaProbsDown = denDown == 0m ? 0m : numDown / denDown;

var probDownBbUpper = probBbUpperDown / (probBbUpperDown + probBbUpperUp);
var probDownBbBasis = probBbBasisDown / (probBbBasisDown + probBbBasisUp);
var probDownSma = probSmaDown / (probSmaDown + probSmaUp);

var numUp = probDownBbUpper * probDownBbBasis * probDownSma;
var denUp = numUp + (1m - probDownBbUpper) * (1m - probDownBbBasis) * (1m - probDownSma);
var sigmaProbsUp = denUp == 0m ? 0m : numUp / denUp;

var numPrime = sigmaProbsDown * sigmaProbsUp;
var denPrime = numPrime + (1m - sigmaProbsDown) * (1m - sigmaProbsUp);
var probPrime = denPrime == 0m ? 0m : numPrime / denPrime;

var threshold = LowerThreshold / 100m;

var longUsingProbPrime = probPrime > threshold && _prevProbPrime == 0m;
var longUsingSigmaProbsUp = sigmaProbsUp < 1m && _prevSigmaProbsUp == 1m;

var shortUsingProbPrime = probPrime == 0m && _prevProbPrime > threshold;
var shortUsingSigmaProbsDown = sigmaProbsDown < 1m && _prevSigmaProbsDown == 1m;

var milanIsGreen = acAoColorIndex == 1;
var milanIsRed = acAoColorIndex == -1;

var bwConfirmationUp = UseBwConfirmation ? milanIsGreen && pricesAreMovingAwayUpFromAlligator : true;
var bwConfirmationDown = UseBwConfirmation ? milanIsRed && pricesAreMovingAwayDownFromAlligator : true;

var longSignal = bwConfirmationUp && (longUsingProbPrime || longUsingSigmaProbsUp);
var shortSignal = bwConfirmationDown && (shortUsingProbPrime || shortUsingSigmaProbsDown);

if (longSignal && Position <= 0)
BuyMarket();
else if (shortSignal && Position >= 0)
SellMarket();

_prevAo = ao;
_prevAc = ac;
_prevSigmaProbsUp = sigmaProbsUp;
_prevSigmaProbsDown = sigmaProbsDown;
_prevProbPrime = probPrime;
}
}
