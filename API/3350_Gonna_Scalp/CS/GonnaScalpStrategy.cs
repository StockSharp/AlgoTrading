using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "Gonna Scalp" MetaTrader expert advisor built on top of the StockSharp high level API.
/// Combines weighted moving averages, momentum, CCI, ATR, stochastic and MACD filters to locate intraday scalping entries.
/// </summary>
public class GonnaScalpStrategy : Strategy
{
private readonly StrategyParam<decimal> _tradeVolume;
private readonly StrategyParam<int> _fastMaPeriod;
private readonly StrategyParam<int> _slowMaPeriod;
private readonly StrategyParam<int> _momentumPeriod;
private readonly StrategyParam<decimal> _momentumBuyThreshold;
private readonly StrategyParam<decimal> _momentumSellThreshold;
private readonly StrategyParam<decimal> _stopLossSteps;
private readonly StrategyParam<decimal> _takeProfitSteps;
private readonly StrategyParam<DataType> _candleType;

private WeightedMovingAverage _fastMa = null!;
private WeightedMovingAverage _slowMa = null!;
private Momentum _momentum = null!;
private CommodityChannelIndex _cci = null!;
private AverageTrueRange _atr = null!;
private StochasticOscillator _stochastic = null!;
private MovingAverageConvergenceDivergence _macd = null!;

private readonly Queue<decimal> _momentumAbsHistory = new();

private decimal? _currentFastMa;
private decimal? _currentSlowMa;
private decimal? _currentMomentum;
private decimal? _currentCci;
private decimal? _currentAtr;
private decimal? _currentStochastic;
private decimal? _currentMacdMain;
private decimal? _currentMacdSignal;
private decimal? _currentMacdHistogram;

private decimal? _previousCci;
private decimal? _previousAtr;
private decimal? _previousStochastic;

private DateTimeOffset? _lastProcessedTime;
private DateTimeOffset? _stochasticTime;
private DateTimeOffset? _macdTime;

private ICandleMessage? _previousCandle;
private ICandleMessage? _twoCandlesAgo;

private decimal _pipSize;
private decimal _priceStep;

/// <summary>
/// Initializes parameters with defaults that match the original expert advisor.
/// </summary>
public GonnaScalpStrategy()
{
_tradeVolume = Param(nameof(TradeVolume), 0.01m)
.SetDisplay("Trade volume", "Base position size in lots.", "Position sizing")
.SetCanOptimize(true);

_fastMaPeriod = Param(nameof(FastMaPeriod), 1)
.SetDisplay("Fast LWMA", "Length of the fast weighted moving average.", "Trend filters")
.SetCanOptimize(true);

_slowMaPeriod = Param(nameof(SlowMaPeriod), 5)
.SetDisplay("Slow LWMA", "Length of the slow weighted moving average.", "Trend filters")
.SetCanOptimize(true);

_momentumPeriod = Param(nameof(MomentumPeriod), 14)
.SetDisplay("Momentum period", "Number of bars used by the momentum filter.", "Momentum")
.SetCanOptimize(true);

_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
.SetDisplay("Momentum buy threshold", "Minimum absolute deviation from 100 required to allow long entries.", "Momentum")
.SetCanOptimize(true);

_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
.SetDisplay("Momentum sell threshold", "Minimum absolute deviation from 100 required to allow short entries.", "Momentum")
.SetCanOptimize(true);

_stopLossSteps = Param(nameof(StopLossSteps), 200m)
.SetDisplay("Stop loss (points)", "Protective stop distance expressed in MetaTrader points.", "Risk")
.SetCanOptimize(true);

_takeProfitSteps = Param(nameof(TakeProfitSteps), 200m)
.SetDisplay("Take profit (points)", "Profit target distance expressed in MetaTrader points.", "Risk")
.SetCanOptimize(true);

_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
.SetDisplay("Primary candles", "Timeframe used for the trading logic.", "Data")
.SetCanOptimize(true);
}

/// <summary>
/// Base position size in lots.
/// </summary>
public decimal TradeVolume
{
get => _tradeVolume.Value;
set => _tradeVolume.Value = value;
}

/// <summary>
/// Length of the fast weighted moving average filter.
/// </summary>
public int FastMaPeriod
{
get => _fastMaPeriod.Value;
set => _fastMaPeriod.Value = value;
}

/// <summary>
/// Length of the slow weighted moving average filter.
/// </summary>
public int SlowMaPeriod
{
get => _slowMaPeriod.Value;
set => _slowMaPeriod.Value = value;
}

/// <summary>
/// Number of bars used by the momentum filter.
/// </summary>
public int MomentumPeriod
{
get => _momentumPeriod.Value;
set => _momentumPeriod.Value = value;
}

/// <summary>
/// Minimum absolute momentum deviation required for long entries.
/// </summary>
public decimal MomentumBuyThreshold
{
get => _momentumBuyThreshold.Value;
set => _momentumBuyThreshold.Value = value;
}

/// <summary>
/// Minimum absolute momentum deviation required for short entries.
/// </summary>
public decimal MomentumSellThreshold
{
get => _momentumSellThreshold.Value;
set => _momentumSellThreshold.Value = value;
}

/// <summary>
/// Stop loss distance expressed in MetaTrader points.
/// </summary>
public decimal StopLossSteps
{
get => _stopLossSteps.Value;
set => _stopLossSteps.Value = value;
}

/// <summary>
/// Take profit distance expressed in MetaTrader points.
/// </summary>
public decimal TakeProfitSteps
{
get => _takeProfitSteps.Value;
set => _takeProfitSteps.Value = value;
}

/// <summary>
/// Candle type used for all indicator calculations.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

Volume = AlignVolume(TradeVolume);

_fastMa = new WeightedMovingAverage { Length = FastMaPeriod, CandlePrice = CandlePrice.Typical };
_slowMa = new WeightedMovingAverage { Length = SlowMaPeriod, CandlePrice = CandlePrice.Typical };
_momentum = new Momentum { Length = MomentumPeriod };
_cci = new CommodityChannelIndex { Length = 20 };
_atr = new AverageTrueRange { Length = 12 };
_stochastic = new StochasticOscillator { KPeriod = 5, DPeriod = 3, Smooth = 3 };
_macd = new MovingAverageConvergenceDivergence { FastLength = 12, SlowLength = 26, SignalLength = 9 };

_momentumAbsHistory.Clear();
_previousCci = null;
_previousAtr = null;
_previousStochastic = null;
_previousCandle = null;
_twoCandlesAgo = null;
_lastProcessedTime = null;
_stochasticTime = null;
_macdTime = null;

_pipSize = GetPipSize();
_priceStep = Security?.PriceStep ?? 0m;

StartProtection();

var subscription = SubscribeCandles(CandleType);
subscription.Bind(_fastMa, _slowMa, _momentum, _cci, _atr, ProcessPrimaryIndicators);
subscription.BindEx(_stochastic, ProcessStochastic);
subscription.BindEx(_macd, ProcessMacd);
subscription.Start();
}

private void ProcessPrimaryIndicators(ICandleMessage candle, decimal fastMa, decimal slowMa, decimal momentum, decimal cci, decimal atr)
{
if (candle.State != CandleStates.Finished)
return;

_currentFastMa = fastMa;
_currentSlowMa = slowMa;
_currentMomentum = momentum;
_currentCci = cci;
_currentAtr = atr;

TryEvaluate(candle);
}

private void ProcessStochastic(ICandleMessage candle, IIndicatorValue value)
{
if (candle.State != CandleStates.Finished)
return;

if (value is not StochasticOscillatorValue stochValue || stochValue.Main is not decimal mainValue)
return;

_currentStochastic = mainValue;
_stochasticTime = candle.OpenTime;

TryEvaluate(candle);
}

private void ProcessMacd(ICandleMessage candle, IIndicatorValue value)
{
if (candle.State != CandleStates.Finished)
return;

if (value is not MovingAverageConvergenceDivergenceValue macdValue)
return;

if (macdValue.Macd is not decimal macdMain || macdValue.Signal is not decimal macdSignal || macdValue.Histogram is not decimal histogram)
return;

_currentMacdMain = macdMain;
_currentMacdSignal = macdSignal;
_currentMacdHistogram = histogram;
_macdTime = candle.OpenTime;

TryEvaluate(candle);
}

private void TryEvaluate(ICandleMessage candle)
{
if (_currentFastMa is null || _currentSlowMa is null || _currentMomentum is null || _currentCci is null || _currentAtr is null)
return;

if (_currentStochastic is null || _currentMacdMain is null || _currentMacdSignal is null || _currentMacdHistogram is null)
return;

if (_stochasticTime != candle.OpenTime || _macdTime != candle.OpenTime)
return;

if (_lastProcessedTime == candle.OpenTime)
return;

var momentumAbs = Math.Abs(100m - _currentMomentum.Value);
_momentumAbsHistory.Enqueue(momentumAbs);
while (_momentumAbsHistory.Count > 3)
_momentumAbsHistory.Dequeue();

EvaluateSignals(candle);

_previousCci = _currentCci;
_previousAtr = _currentAtr;
_previousStochastic = _currentStochastic;
_twoCandlesAgo = _previousCandle;
_previousCandle = candle;
_lastProcessedTime = candle.OpenTime;
}

private void EvaluateSignals(ICandleMessage candle)
{
if (_currentFastMa is null || _currentSlowMa is null || _currentCci is null || _currentAtr is null || _currentStochastic is null)
return;

if (_currentMacdMain is null || _currentMacdSignal is null || _currentMacdHistogram is null)
return;

ApplyRiskManagement(candle);

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (Position != 0m)
return;

if (_momentumAbsHistory.Count < 1)
return;

var buyVotes = 0;
var sellVotes = 0;

if (_currentFastMa.Value > _currentSlowMa.Value)
buyVotes++;
else if (_currentFastMa.Value < _currentSlowMa.Value)
sellVotes++;

if (_previousCci is decimal prevCci)
{
if (_currentCci.Value > prevCci && _currentCci.Value < 75m)
buyVotes++;
else if (_currentCci.Value < prevCci && _currentCci.Value >= -75m)
sellVotes++;
}

if (_previousAtr is decimal prevAtr)
{
if (_currentAtr.Value > prevAtr)
buyVotes++;
else if (_currentAtr.Value < prevAtr)
sellVotes++;
}

if (_currentMacdHistogram.Value > 0m)
buyVotes++;
else if (_currentMacdHistogram.Value < 0m)
sellVotes++;

if (_currentMacdMain.Value > _currentMacdSignal.Value)
buyVotes++;
else if (_currentMacdMain.Value < _currentMacdSignal.Value)
sellVotes++;

if (_previousStochastic is decimal prevStoch)
{
if (_currentStochastic.Value >= 88m || (_currentStochastic.Value > prevStoch && _currentStochastic.Value > 50m))
buyVotes++;
else if (_currentStochastic.Value <= 12m || (_currentStochastic.Value < prevStoch && _currentStochastic.Value < 50m))
sellVotes++;
}

var allowLong = false;
var allowShort = false;

foreach (var value in _momentumAbsHistory)
{
if (!allowLong && value >= MomentumBuyThreshold)
allowLong = true;

if (!allowShort && value >= MomentumSellThreshold)
allowShort = true;
}

if (allowLong && buyVotes > sellVotes && _previousCandle is not null && _twoCandlesAgo is not null)
{
if (_twoCandlesAgo.LowPrice < _previousCandle.HighPrice)
OpenLong();
}
else if (allowShort && sellVotes > buyVotes && _previousCandle is not null && _twoCandlesAgo is not null)
{
if (_previousCandle.LowPrice < _twoCandlesAgo.HighPrice)
OpenShort();
}
}

private void OpenLong()
{
var volume = AlignVolume(TradeVolume);
if (volume <= 0m)
return;

BuyMarket(volume);
}

private void OpenShort()
{
var volume = AlignVolume(TradeVolume);
if (volume <= 0m)
return;

SellMarket(volume);
}

private void ApplyRiskManagement(ICandleMessage candle)
{
if (Position > 0m)
{
var entryPrice = PositionPrice;
if (entryPrice <= 0m)
return;

var stopPrice = entryPrice - StepsToPrice(StopLossSteps);
var takePrice = entryPrice + StepsToPrice(TakeProfitSteps);

if (StopLossSteps > 0m && candle.LowPrice <= stopPrice)
{
SellMarket(Position);
return;
}

if (TakeProfitSteps > 0m && candle.HighPrice >= takePrice)
{
SellMarket(Position);
}
}
else if (Position < 0m)
{
var entryPrice = PositionPrice;
if (entryPrice <= 0m)
return;

var stopPrice = entryPrice + StepsToPrice(StopLossSteps);
var takePrice = entryPrice - StepsToPrice(TakeProfitSteps);

if (StopLossSteps > 0m && candle.HighPrice >= stopPrice)
{
BuyMarket(Math.Abs(Position));
return;
}

if (TakeProfitSteps > 0m && candle.LowPrice <= takePrice)
{
BuyMarket(Math.Abs(Position));
}
}
}

private decimal AlignVolume(decimal volume)
{
if (Security is null)
return volume;

var step = Security.VolumeStep ?? 0m;
var min = Security.MinVolume ?? 0m;
var max = Security.MaxVolume ?? decimal.MaxValue;

if (step > 0m)
{
var ratio = Math.Round(volume / step, MidpointRounding.AwayFromZero);
if (ratio == 0m && volume > 0m)
ratio = 1m;
volume = ratio * step;
}

if (min > 0m && volume < min)
volume = min;

if (volume > max)
volume = max;

return volume;
}

private decimal StepsToPrice(decimal steps)
{
if (steps == 0m)
return 0m;

var size = _pipSize > 0m ? _pipSize : (_priceStep > 0m ? _priceStep : 1m);
return steps * size;
}

private decimal GetPipSize()
{
var priceStep = Security?.PriceStep ?? 0m;
if (priceStep <= 0m)
return 1m;

var decimals = Security?.Decimals;
if (decimals == 3 || decimals == 5)
return priceStep * 10m;

return priceStep;
}
}
