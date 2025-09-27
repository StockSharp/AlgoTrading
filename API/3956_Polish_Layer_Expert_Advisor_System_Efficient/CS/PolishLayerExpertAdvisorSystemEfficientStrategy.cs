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
/// Polish Layer Expert Advisor System Efficient strategy converted from MQL4.
/// Combines price and RSI moving averages with Stochastic, DeMarker, and Williams %R confirmations.
/// </summary>
public class PolishLayerExpertAdvisorSystemEfficientStrategy : Strategy
{
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<int> _rsiPeriod;
private readonly StrategyParam<int> _shortPricePeriod;
private readonly StrategyParam<int> _longPricePeriod;
private readonly StrategyParam<int> _shortRsiPeriod;
private readonly StrategyParam<int> _longRsiPeriod;
private readonly StrategyParam<int> _stochasticKPeriod;
private readonly StrategyParam<int> _stochasticDPeriod;
private readonly StrategyParam<int> _stochasticSlowing;
private readonly StrategyParam<int> _demarkerPeriod;
private readonly StrategyParam<int> _williamsPeriod;
private readonly StrategyParam<decimal> _stochasticOversoldLevel;
private readonly StrategyParam<decimal> _stochasticOverboughtLevel;
private readonly StrategyParam<decimal> _demarkerBuyLevel;
private readonly StrategyParam<decimal> _demarkerSellLevel;
private readonly StrategyParam<decimal> _williamsBuyLevel;
private readonly StrategyParam<decimal> _williamsSellLevel;
private readonly StrategyParam<decimal> _stopLossPips;
private readonly StrategyParam<decimal> _takeProfitPips;

private SimpleMovingAverage _shortPriceMa = null!;
private LinearWeightedMovingAverage _longPriceMa = null!;
private RelativeStrengthIndex _rsi = null!;
private SimpleMovingAverage _shortRsiAverage = null!;
private SimpleMovingAverage _longRsiAverage = null!;
private StochasticOscillator _stochastic = null!;
private DeMarker _deMarker = null!;
private WilliamsPercentRange _williams = null!;

private decimal? _previousStochasticMain;
private decimal? _previousStochasticSignal;
private decimal? _previousDeMarker;
private decimal? _previousWilliams;

private decimal? _longStopPrice;
private decimal? _longTakePrice;
private decimal? _shortStopPrice;
private decimal? _shortTakePrice;

private decimal _priceStep;


/// <summary>
/// Candle type to process.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// RSI calculation period.
/// </summary>
public int RsiPeriod
{
get => _rsiPeriod.Value;
set => _rsiPeriod.Value = value;
}

/// <summary>
/// Period for the fast price moving average.
/// </summary>
public int ShortPricePeriod
{
get => _shortPricePeriod.Value;
set => _shortPricePeriod.Value = value;
}

/// <summary>
/// Period for the slow price moving average (LWMA).
/// </summary>
public int LongPricePeriod
{
get => _longPricePeriod.Value;
set => _longPricePeriod.Value = value;
}

/// <summary>
/// Length of the short RSI smoothing average.
/// </summary>
public int ShortRsiPeriod
{
get => _shortRsiPeriod.Value;
set => _shortRsiPeriod.Value = value;
}

/// <summary>
/// Length of the long RSI smoothing average.
/// </summary>
public int LongRsiPeriod
{
get => _longRsiPeriod.Value;
set => _longRsiPeriod.Value = value;
}

/// <summary>
/// Stochastic %K period.
/// </summary>
public int StochasticKPeriod
{
get => _stochasticKPeriod.Value;
set => _stochasticKPeriod.Value = value;
}

/// <summary>
/// Stochastic %D period.
/// </summary>
public int StochasticDPeriod
{
get => _stochasticDPeriod.Value;
set => _stochasticDPeriod.Value = value;
}

/// <summary>
/// Stochastic slowing factor.
/// </summary>
public int StochasticSlowing
{
get => _stochasticSlowing.Value;
set => _stochasticSlowing.Value = value;
}

/// <summary>
/// DeMarker period.
/// </summary>
public int DemarkerPeriod
{
get => _demarkerPeriod.Value;
set => _demarkerPeriod.Value = value;
}

/// <summary>
/// Williams %R period.
/// </summary>
public int WilliamsPeriod
{
get => _williamsPeriod.Value;
set => _williamsPeriod.Value = value;
}

/// <summary>
/// Stochastic oversold level.
/// </summary>
public decimal StochasticOversoldLevel
{
get => _stochasticOversoldLevel.Value;
set => _stochasticOversoldLevel.Value = value;
}

/// <summary>
/// Stochastic overbought level.
/// </summary>
public decimal StochasticOverboughtLevel
{
get => _stochasticOverboughtLevel.Value;
set => _stochasticOverboughtLevel.Value = value;
}

/// <summary>
/// DeMarker level for long setups.
/// </summary>
public decimal DemarkerBuyLevel
{
get => _demarkerBuyLevel.Value;
set => _demarkerBuyLevel.Value = value;
}

/// <summary>
/// DeMarker level for short setups.
/// </summary>
public decimal DemarkerSellLevel
{
get => _demarkerSellLevel.Value;
set => _demarkerSellLevel.Value = value;
}

/// <summary>
/// Williams %R threshold that confirms longs.
/// </summary>
public decimal WilliamsBuyLevel
{
get => _williamsBuyLevel.Value;
set => _williamsBuyLevel.Value = value;
}

/// <summary>
/// Williams %R threshold that confirms shorts.
/// </summary>
public decimal WilliamsSellLevel
{
get => _williamsSellLevel.Value;
set => _williamsSellLevel.Value = value;
}

/// <summary>
/// Stop-loss distance expressed in pips.
/// </summary>
public decimal StopLossPips
{
get => _stopLossPips.Value;
set => _stopLossPips.Value = value;
}

/// <summary>
/// Take-profit distance expressed in pips.
/// </summary>
public decimal TakeProfitPips
{
get => _takeProfitPips.Value;
set => _takeProfitPips.Value = value;
}

/// <summary>
/// Initializes strategy parameters.
/// </summary>
public PolishLayerExpertAdvisorSystemEfficientStrategy()
{

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
.SetDisplay("Candle Type", "Primary timeframe for calculations", "General");

_rsiPeriod = Param(nameof(RsiPeriod), 14)
.SetGreaterThanZero()
.SetDisplay("RSI Period", "Base RSI length", "RSI")
.SetCanOptimize(true);

_shortPricePeriod = Param(nameof(ShortPricePeriod), 9)
.SetGreaterThanZero()
.SetDisplay("Fast Price MA", "Length of the fast price moving average", "Trend")
.SetCanOptimize(true);

_longPricePeriod = Param(nameof(LongPricePeriod), 45)
.SetGreaterThanZero()
.SetDisplay("Slow Price MA", "Length of the slow price moving average", "Trend")
.SetCanOptimize(true);

_shortRsiPeriod = Param(nameof(ShortRsiPeriod), 9)
.SetGreaterThanZero()
.SetDisplay("Fast RSI MA", "Length of the fast RSI moving average", "RSI")
.SetCanOptimize(true);

_longRsiPeriod = Param(nameof(LongRsiPeriod), 45)
.SetGreaterThanZero()
.SetDisplay("Slow RSI MA", "Length of the slow RSI moving average", "RSI")
.SetCanOptimize(true);

_stochasticKPeriod = Param(nameof(StochasticKPeriod), 5)
.SetGreaterThanZero()
.SetDisplay("%K Period", "Stochastic %K period", "Stochastic")
.SetCanOptimize(true);

_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
.SetGreaterThanZero()
.SetDisplay("%D Period", "Stochastic %D period", "Stochastic");

_stochasticSlowing = Param(nameof(StochasticSlowing), 3)
.SetGreaterThanZero()
.SetDisplay("Slowing", "Stochastic slowing factor", "Stochastic");

_demarkerPeriod = Param(nameof(DemarkerPeriod), 14)
.SetGreaterThanZero()
.SetDisplay("DeMarker Period", "DeMarker averaging period", "DeMarker");

_williamsPeriod = Param(nameof(WilliamsPeriod), 14)
.SetGreaterThanZero()
.SetDisplay("Williams %R Period", "Williams %R lookback", "Williams %R");

_stochasticOversoldLevel = Param(nameof(StochasticOversoldLevel), 19m)
.SetDisplay("%K Oversold", "Oversold level for %K", "Thresholds");

_stochasticOverboughtLevel = Param(nameof(StochasticOverboughtLevel), 81m)
.SetDisplay("%K Overbought", "Overbought level for %K", "Thresholds");

_demarkerBuyLevel = Param(nameof(DemarkerBuyLevel), 0.35m)
.SetDisplay("DeMarker Buy Level", "Minimum DeMarker value for longs", "Thresholds");

_demarkerSellLevel = Param(nameof(DemarkerSellLevel), 0.63m)
.SetDisplay("DeMarker Sell Level", "Maximum DeMarker value for shorts", "Thresholds");

_williamsBuyLevel = Param(nameof(WilliamsBuyLevel), -81m)
.SetDisplay("Williams Buy Level", "Williams %R level for longs", "Thresholds");

_williamsSellLevel = Param(nameof(WilliamsSellLevel), -19m)
.SetDisplay("Williams Sell Level", "Williams %R level for shorts", "Thresholds");

_stopLossPips = Param(nameof(StopLossPips), 7777m)
.SetGreaterThanOrEqual(0m)
.SetDisplay("Stop Loss", "Stop-loss distance in pips", "Risk");

_takeProfitPips = Param(nameof(TakeProfitPips), 17m)
.SetGreaterThanOrEqual(0m)
.SetDisplay("Take Profit", "Take-profit distance in pips", "Risk");
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

_previousStochasticMain = null;
_previousStochasticSignal = null;
_previousDeMarker = null;
_previousWilliams = null;

ResetProtectionLevels();
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

StartProtection();

_priceStep = Security?.PriceStep ?? 0m;

_shortPriceMa = new SimpleMovingAverage { Length = ShortPricePeriod };
_longPriceMa = new LinearWeightedMovingAverage { Length = LongPricePeriod };
_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
_shortRsiAverage = new SimpleMovingAverage { Length = ShortRsiPeriod };
_longRsiAverage = new SimpleMovingAverage { Length = LongRsiPeriod };
_stochastic = new StochasticOscillator
{
KPeriod = StochasticKPeriod,
DPeriod = StochasticDPeriod,
Slowing = StochasticSlowing
};
_deMarker = new DeMarker { Length = DemarkerPeriod };
_williams = new WilliamsPercentRange { Length = WilliamsPeriod };

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx(_shortPriceMa, _longPriceMa, _rsi, _stochastic, _deMarker, _williams, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _shortPriceMa);
DrawIndicator(area, _longPriceMa);
DrawIndicator(area, _rsi);
DrawIndicator(area, _stochastic);
DrawIndicator(area, _deMarker);
DrawIndicator(area, _williams);
DrawOwnTrades(area);
}
}

private void ProcessCandle(
ICandleMessage candle,
IIndicatorValue shortPriceValue,
IIndicatorValue longPriceValue,
IIndicatorValue rsiValue,
IIndicatorValue stochasticValue,
IIndicatorValue demarkerValue,
IIndicatorValue williamsValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!shortPriceValue.IsFinal || !longPriceValue.IsFinal || !rsiValue.IsFinal || !stochasticValue.IsFinal || !demarkerValue.IsFinal || !williamsValue.IsFinal)
return;

var fastPrice = shortPriceValue.ToDecimal();
var slowPrice = longPriceValue.ToDecimal();
var rsi = rsiValue.ToDecimal();

var fastRsi = _shortRsiAverage.Process(new DecimalIndicatorValue(_shortRsiAverage, rsi, candle.OpenTime)).ToDecimal();
var slowRsi = _longRsiAverage.Process(new DecimalIndicatorValue(_longRsiAverage, rsi, candle.OpenTime)).ToDecimal();

var stochastic = (StochasticOscillatorValue)stochasticValue;
if (stochastic.K is not decimal currentStochasticMain || stochastic.D is not decimal currentStochasticSignal)
return;

var demarker = demarkerValue.ToDecimal();
var williams = williamsValue.ToDecimal();

if (ManageOpenPosition(candle))
{
UpdatePreviousValues(currentStochasticMain, currentStochasticSignal, demarker, williams);
return;
}

if (!IsFormedAndOnlineAndAllowTrading())
{
UpdatePreviousValues(currentStochasticMain, currentStochasticSignal, demarker, williams);
return;
}

if (!_shortPriceMa.IsFormed || !_longPriceMa.IsFormed || !_shortRsiAverage.IsFormed || !_longRsiAverage.IsFormed || !_stochastic.IsFormed || !_deMarker.IsFormed || !_williams.IsFormed)
{
UpdatePreviousValues(currentStochasticMain, currentStochasticSignal, demarker, williams);
return;
}

if (_previousStochasticMain is null || _previousStochasticSignal is null || _previousDeMarker is null || _previousWilliams is null)
{
UpdatePreviousValues(currentStochasticMain, currentStochasticSignal, demarker, williams);
return;
}

var longTrend = fastPrice > slowPrice && fastRsi > slowRsi;
var shortTrend = fastPrice < slowPrice && fastRsi < slowRsi;

var stochasticCrossUp = _previousStochasticMain < StochasticOversoldLevel && currentStochasticMain >= StochasticOversoldLevel &&
_previousStochasticMain < _previousStochasticSignal && currentStochasticMain >= currentStochasticSignal;

var stochasticCrossDown = _previousStochasticMain > StochasticOverboughtLevel && currentStochasticMain <= StochasticOverboughtLevel &&
_previousStochasticMain > _previousStochasticSignal && currentStochasticMain <= currentStochasticSignal;

var demarkerCrossUp = _previousDeMarker < DemarkerBuyLevel && demarker >= DemarkerBuyLevel;
var demarkerCrossDown = _previousDeMarker > DemarkerSellLevel && demarker <= DemarkerSellLevel;

var williamsCrossUp = _previousWilliams < WilliamsBuyLevel && williams >= WilliamsBuyLevel;
var williamsCrossDown = _previousWilliams > WilliamsSellLevel && williams <= WilliamsSellLevel;

var longSignal = Position == 0 && longTrend && stochasticCrossUp && demarkerCrossUp && williamsCrossUp;
var shortSignal = Position == 0 && shortTrend && stochasticCrossDown && demarkerCrossDown && williamsCrossDown;

if (longSignal)
{
TryEnterPosition(Sides.Buy, candle.ClosePrice);
}
else if (shortSignal)
{
TryEnterPosition(Sides.Sell, candle.ClosePrice);
}

UpdatePreviousValues(currentStochasticMain, currentStochasticSignal, demarker, williams);
}

private void TryEnterPosition(Sides side, decimal entryPrice)
{
var volume = Volume;
if (volume <= 0m)
return;

if (side == Sides.Buy)
{
BuyMarket(volume);
SetProtectionLevels(entryPrice, true);
}
else
{
SellMarket(volume);
SetProtectionLevels(entryPrice, false);
}
}

private bool ManageOpenPosition(ICandleMessage candle)
{
if (Position > 0)
{
if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
{
SellMarket(Position);
ResetProtectionLevels();
return true;
}

if (_longTakePrice is decimal take && candle.HighPrice >= take)
{
SellMarket(Position);
ResetProtectionLevels();
return true;
}
}
else if (Position < 0)
{
var shortVolume = Math.Abs(Position);
if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
{
BuyMarket(shortVolume);
ResetProtectionLevels();
return true;
}

if (_shortTakePrice is decimal take && candle.LowPrice <= take)
{
BuyMarket(shortVolume);
ResetProtectionLevels();
return true;
}
}

return false;
}

private void SetProtectionLevels(decimal entryPrice, bool isLong)
{
if (_priceStep <= 0m)
{
ResetProtectionLevels();
return;
}

var stopOffset = StopLossPips > 0m ? StopLossPips * _priceStep : 0m;
var takeOffset = TakeProfitPips > 0m ? TakeProfitPips * _priceStep : 0m;

if (isLong)
{
_longStopPrice = stopOffset > 0m ? entryPrice - stopOffset : null;
_longTakePrice = takeOffset > 0m ? entryPrice + takeOffset : null;
_shortStopPrice = null;
_shortTakePrice = null;
}
else
{
_shortStopPrice = stopOffset > 0m ? entryPrice + stopOffset : null;
_shortTakePrice = takeOffset > 0m ? entryPrice - takeOffset : null;
_longStopPrice = null;
_longTakePrice = null;
}
}

private void ResetProtectionLevels()
{
_longStopPrice = null;
_longTakePrice = null;
_shortStopPrice = null;
_shortTakePrice = null;
}

private void UpdatePreviousValues(decimal stochasticMain, decimal stochasticSignal, decimal demarker, decimal williams)
{
_previousStochasticMain = stochasticMain;
_previousStochasticSignal = stochasticSignal;
_previousDeMarker = demarker;
_previousWilliams = williams;
}
}

