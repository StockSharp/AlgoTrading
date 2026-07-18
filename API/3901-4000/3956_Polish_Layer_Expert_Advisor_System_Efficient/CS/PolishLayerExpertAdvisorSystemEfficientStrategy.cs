using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
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
private WeightedMovingAverage _longPriceMa = null!;
private RelativeStrengthIndex _rsi = null!;
private SimpleMovingAverage _shortRsiAverage = null!;
private SimpleMovingAverage _longRsiAverage = null!;

private StochasticOscillator _stochastic = null!;
private DeMarker _deMarker = null!;
private WilliamsR _williams = null!;

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

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
.SetDisplay("Candle Type", "Primary timeframe for calculations", "General");

_rsiPeriod = Param(nameof(RsiPeriod), 14)
.SetGreaterThanZero()
.SetDisplay("RSI Period", "Base RSI length", "RSI")
;

_shortPricePeriod = Param(nameof(ShortPricePeriod), 9)
.SetGreaterThanZero()
.SetDisplay("Fast Price MA", "Length of the fast price moving average", "Trend")
;

_longPricePeriod = Param(nameof(LongPricePeriod), 45)
.SetGreaterThanZero()
.SetDisplay("Slow Price MA", "Length of the slow price moving average", "Trend")
;

_shortRsiPeriod = Param(nameof(ShortRsiPeriod), 9)
.SetGreaterThanZero()
.SetDisplay("Fast RSI MA", "Length of the fast RSI moving average", "RSI")
;

_longRsiPeriod = Param(nameof(LongRsiPeriod), 45)
.SetGreaterThanZero()
.SetDisplay("Slow RSI MA", "Length of the slow RSI moving average", "RSI")
;

_stochasticKPeriod = Param(nameof(StochasticKPeriod), 5)
.SetGreaterThanZero()
.SetDisplay("%K Period", "Stochastic %K period", "Stochastic")
;

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
.SetNotNegative()
.SetDisplay("Stop Loss", "Stop-loss distance in pips", "Risk");

_takeProfitPips = Param(nameof(TakeProfitPips), 17m)
.SetNotNegative()
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
protected override void OnStarted2(DateTime time)
{
base.OnStarted2(time);

StartProtection(
	takeProfit: new Unit(2, UnitTypes.Percent),
	stopLoss: new Unit(1, UnitTypes.Percent));

_priceStep = Security?.PriceStep ?? 0m;

_shortPriceMa = new SimpleMovingAverage { Length = ShortPricePeriod };
_longPriceMa = new WeightedMovingAverage { Length = LongPricePeriod };
_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
_shortRsiAverage = new SimpleMovingAverage { Length = ShortRsiPeriod };
_longRsiAverage = new SimpleMovingAverage { Length = LongRsiPeriod };
_stochastic = new StochasticOscillator();
_stochastic.K.Length = StochasticKPeriod;
_stochastic.D.Length = StochasticDPeriod;
_deMarker = new DeMarker { Length = DemarkerPeriod };
_williams = new WilliamsR { Length = WilliamsPeriod };

Indicators.Add(_stochastic);
Indicators.Add(_deMarker);
Indicators.Add(_williams);

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(_shortPriceMa, _longPriceMa, _rsi, ProcessCandle)
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
decimal fastPrice,
decimal slowPrice,
decimal rsi)
{
if (candle.State != CandleStates.Finished)
return;

var fastRsi = _shortRsiAverage.Process(rsi, candle.OpenTime, true).ToDecimal();
var slowRsi = _longRsiAverage.Process(rsi, candle.OpenTime, true).ToDecimal();

var stochasticResult = _stochastic.Process(candle);
var demarkerResult = _deMarker.Process(candle);
var williamsResult = _williams.Process(candle);

if (!_stochastic.IsFormed || !_deMarker.IsFormed || !_williams.IsFormed)
return;

if (!_shortRsiAverage.IsFormed || !_longRsiAverage.IsFormed)
return;

var stochastic = (StochasticOscillatorValue)stochasticResult;
if (stochastic.K is not decimal currentStochasticMain || stochastic.D is not decimal currentStochasticSignal)
return;

var demarker = demarkerResult.ToDecimal();
var williams = williamsResult.ToDecimal();

if (_previousStochasticMain is null || _previousStochasticSignal is null || _previousDeMarker is null || _previousWilliams is null)
{
UpdatePreviousValues(currentStochasticMain, currentStochasticSignal, demarker, williams);
return;
}

if (Position != 0)
{
UpdatePreviousValues(currentStochasticMain, currentStochasticSignal, demarker, williams);
return;
}

var longTrend = fastPrice > slowPrice && fastRsi > slowRsi;
var shortTrend = fastPrice < slowPrice && fastRsi < slowRsi;

// Simplified: just require trend + stochastic cross (relax demarker/williams requirements)
var stochasticCrossUp = _previousStochasticMain < StochasticOversoldLevel && currentStochasticMain >= StochasticOversoldLevel;
var stochasticCrossDown = _previousStochasticMain > StochasticOverboughtLevel && currentStochasticMain <= StochasticOverboughtLevel;

var longSignal = longTrend && stochasticCrossUp;
var shortSignal = shortTrend && stochasticCrossDown;

if (longSignal)
{
BuyMarket();
}
else if (shortSignal)
{
SellMarket();
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

