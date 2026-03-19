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
private decimal _prevSigmaProbsUp;
private decimal _prevSigmaProbsDown;
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
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
.SetDisplay("Candle Type", "Type of candles to use", "General");

_bbSmaPeriod = Param(nameof(BbSmaPeriod), 20)
.SetGreaterThanZero()
.SetDisplay("BB SMA Period", "Bollinger Bands SMA period", "Bollinger Bands")

.SetOptimize(10, 30, 5);

_bbStdDevMult = Param(nameof(BbStdDevMult), 2.5m)
.SetDisplay("BB StdDev Mult", "Bollinger Bands standard deviation multiplier", "Bollinger Bands")

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

_bayesPeriod = Param(nameof(BayesPeriod), 10)
.SetGreaterThanZero()
.SetDisplay("Bayes Period", "Lookback period for probability calculation", "Bayesian");

_lowerThreshold = Param(nameof(LowerThreshold), 30m)
.SetDisplay("Lower Threshold", "Probability threshold (%)", "Bayesian");

_useBwConfirmation = Param(nameof(UseBwConfirmation), false)
.SetDisplay("Use BW Confirmation", "Require Bill Williams confirmation", "Filters");

_jawLength = Param(nameof(JawLength), 13)
.SetGreaterThanZero()
.SetDisplay("Jaw Length", "Alligator jaw SMA length", "Filters");
}

/// <inheritdoc />
protected override void OnReseted()
{
	base.OnReseted();
	_bollingerBands = default;
	_smaClose = default;
	_aoFastSma = default;
	_aoSlowSma = default;
	_acSma = default;
	_jawSma = default;
	_bbUpperUpSma = default;
	_bbUpperDownSma = default;
	_bbBasisUpSma = default;
	_bbBasisDownSma = default;
	_smaUpSma = default;
	_smaDownSma = default;
	_prevAo = default;
	_prevAc = default;
	_prevSigmaProbsUp = default;
	_prevSigmaProbsDown = default;
	_prevProbPrime = default;
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
return [(Security, CandleType)];
}

/// <inheritdoc />
protected override void OnStarted2(DateTime time)
{
base.OnStarted2(time);

_bollingerBands = new BollingerBands { Length = BbSmaPeriod, Width = BbStdDevMult };
_smaClose = new SMA { Length = SmaPeriod };
_aoFastSma = new SMA { Length = AoFast };
_aoSlowSma = new SMA { Length = AoSlow };
_acSma = new SMA { Length = AcFast };
_jawSma = new SMA { Length = JawLength };

_bbUpperUpSma = new SMA { Length = BayesPeriod };
_bbUpperDownSma = new SMA { Length = BayesPeriod };
_bbBasisUpSma = new SMA { Length = BayesPeriod };
_bbBasisDownSma = new SMA { Length = BayesPeriod };
_smaUpSma = new SMA { Length = BayesPeriod };
_smaDownSma = new SMA { Length = BayesPeriod };

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx(_bollingerBands, ProcessCandle)
.Start();

StartProtection(
	takeProfit: new Unit(2, UnitTypes.Percent),
	stopLoss: new Unit(1, UnitTypes.Percent)
);

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _bollingerBands);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue)
{
if (candle.State != CandleStates.Finished)
return;

if (bbValue is not BollingerBandsValue bb ||
bb.UpBand is not decimal bbUpper ||
bb.LowBand is not decimal bbLower ||
bb.MovingAverage is not decimal bbBasis)
return;

var t = candle.ServerTime;
var close = candle.ClosePrice;
var median = (candle.HighPrice + candle.LowPrice) / 2m;

var smaVal = _smaClose.Process(new DecimalIndicatorValue(_smaClose, close, t) { IsFinal = true });
var aoFastVal = _aoFastSma.Process(new DecimalIndicatorValue(_aoFastSma, median, t) { IsFinal = true });
var aoSlowVal = _aoSlowSma.Process(new DecimalIndicatorValue(_aoSlowSma, median, t) { IsFinal = true });
var jawVal = _jawSma.Process(new DecimalIndicatorValue(_jawSma, close, t) { IsFinal = true });

if (!aoSlowVal.IsFormed || !smaVal.IsFormed)
return;

var smaClose = smaVal.GetValue<decimal>();
var aoFast = aoFastVal.GetValue<decimal>();
var aoSlow = aoSlowVal.GetValue<decimal>();
var jaw = jawVal.GetValue<decimal>();

var ao = aoFast - aoSlow;
var aoSmaValue = _acSma.Process(new DecimalIndicatorValue(_acSma, ao, candle.ServerTime) { IsFinal = true });
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

var probBbUpperUp = _bbUpperUpSma.Process(new DecimalIndicatorValue(_bbUpperUpSma, candle.ClosePrice > bbUpper ? 1m : 0m, candle.ServerTime) { IsFinal = true }).GetValue<decimal>();
var probBbUpperDown = _bbUpperDownSma.Process(new DecimalIndicatorValue(_bbUpperDownSma, candle.ClosePrice < bbUpper ? 1m : 0m, candle.ServerTime) { IsFinal = true }).GetValue<decimal>();
var probBbBasisUp = _bbBasisUpSma.Process(new DecimalIndicatorValue(_bbBasisUpSma, candle.ClosePrice > bbBasis ? 1m : 0m, candle.ServerTime) { IsFinal = true }).GetValue<decimal>();
var probBbBasisDown = _bbBasisDownSma.Process(new DecimalIndicatorValue(_bbBasisDownSma, candle.ClosePrice < bbBasis ? 1m : 0m, candle.ServerTime) { IsFinal = true }).GetValue<decimal>();
var probSmaUp = _smaUpSma.Process(new DecimalIndicatorValue(_smaUpSma, candle.ClosePrice > smaClose ? 1m : 0m, candle.ServerTime) { IsFinal = true }).GetValue<decimal>();
var probSmaDown = _smaDownSma.Process(new DecimalIndicatorValue(_smaDownSma, candle.ClosePrice < smaClose ? 1m : 0m, candle.ServerTime) { IsFinal = true }).GetValue<decimal>();

if (!_bbUpperUpSma.IsFormed)
return;

var sumBbUpper = probBbUpperUp + probBbUpperDown;
var sumBbBasis = probBbBasisUp + probBbBasisDown;
var sumSma = probSmaUp + probSmaDown;
if (sumBbUpper == 0 || sumBbBasis == 0 || sumSma == 0) { _prevAo = ao; _prevAc = ac; return; }
var probUpBbUpper = probBbUpperUp / sumBbUpper;
var probUpBbBasis = probBbBasisUp / sumBbBasis;
var probUpSma = probSmaUp / sumSma;

var numDown = probUpBbUpper * probUpBbBasis * probUpSma;
var denDown = numDown + (1m - probUpBbUpper) * (1m - probUpBbBasis) * (1m - probUpSma);
var sigmaProbsDown = denDown == 0m ? 0m : numDown / denDown;

var probDownBbUpper = probBbUpperDown / sumBbUpper;
var probDownBbBasis = probBbBasisDown / sumBbBasis;
var probDownSma = probSmaDown / sumSma;

var numUp = probDownBbUpper * probDownBbBasis * probDownSma;
var denUp = numUp + (1m - probDownBbUpper) * (1m - probDownBbBasis) * (1m - probDownSma);
var sigmaProbsUp = denUp == 0m ? 0m : numUp / denUp;

var numPrime = sigmaProbsDown * sigmaProbsUp;
var denPrime = numPrime + (1m - sigmaProbsDown) * (1m - sigmaProbsUp);
var probPrime = denPrime == 0m ? 0m : numPrime / denPrime;

var threshold = LowerThreshold / 100m;

// Signal: use Bayesian probability crossovers
var upperThreshold = 1m - threshold;
var longSignal = (sigmaProbsUp > upperThreshold && _prevSigmaProbsUp <= upperThreshold) ||
	(probPrime > upperThreshold && _prevProbPrime <= upperThreshold);
var shortSignal = (sigmaProbsDown > upperThreshold && _prevSigmaProbsDown <= upperThreshold) ||
	(probPrime < threshold && _prevProbPrime >= threshold);

if (longSignal && Position == 0)
BuyMarket();
else if (shortSignal && Position == 0)
SellMarket();

_prevAo = ao;
_prevAc = ac;
_prevSigmaProbsUp = sigmaProbsUp;
_prevSigmaProbsDown = sigmaProbsDown;
_prevProbPrime = probPrime;
}
}
