using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Enhanced Range Filter strategy with ATR based TP/SL.
/// Entry when price breaks filter and passes volume, RSI and EMA checks.
/// Exit when price hits ATR based stop loss or take profit.
/// </summary>
public class EnhancedRangeFilterAtrTpSlStrategy : Strategy
{
private readonly StrategyParam<int> _period;
private readonly StrategyParam<decimal> _multiplier;
private readonly StrategyParam<int> _atrLength;
private readonly StrategyParam<int> _rsiLength;
private readonly StrategyParam<int> _rsiOverbought;
private readonly StrategyParam<int> _rsiOversold;
private readonly StrategyParam<int> _emaFastLength;
private readonly StrategyParam<int> _emaSlowLength;
private readonly StrategyParam<bool> _volumeFilter;
private readonly StrategyParam<decimal> _riskReward;
private readonly StrategyParam<decimal> _rangingThreshold;
private readonly StrategyParam<decimal> _atrMultiplierSl;
private readonly StrategyParam<decimal> _atrMultiplierTp;
private readonly StrategyParam<DataType> _candleType;

private ExponentialMovingAverage _emaFast;
private ExponentialMovingAverage _emaSlow;
private AverageTrueRange _atr;
private RelativeStrengthIndex _rsi;
private SimpleMovingAverage _volumeSma;
private Highest _highest;
private Lowest _lowest;

private ExponentialMovingAverage _avrng;
private ExponentialMovingAverage _smooth;

private decimal _prevPrice;
private decimal _filter;
private decimal _upward;
private decimal _downward;
private int _condIni;
private bool _isFirst = true;

private decimal _stopLoss;
private decimal _takeProfit;

/// <summary>
/// Sampling period for range calculation.
/// </summary>
public int Period
{
get => _period.Value;
set => _period.Value = value;
}

/// <summary>
/// Range multiplier.
/// </summary>
public decimal Multiplier
{
get => _multiplier.Value;
set => _multiplier.Value = value;
}

/// <summary>
/// ATR period.
/// </summary>
public int AtrLength
{
get => _atrLength.Value;
set => _atrLength.Value = value;
}

/// <summary>
/// RSI period.
/// </summary>
public int RsiLength
{
get => _rsiLength.Value;
set => _rsiLength.Value = value;
}

/// <summary>
/// RSI overbought level.
/// </summary>
public int RsiOverbought
{
get => _rsiOverbought.Value;
set => _rsiOverbought.Value = value;
}

/// <summary>
/// RSI oversold level.
/// </summary>
public int RsiOversold
{
get => _rsiOversold.Value;
set => _rsiOversold.Value = value;
}

/// <summary>
/// Fast EMA length.
/// </summary>
public int EmaFastLength
{
get => _emaFastLength.Value;
set => _emaFastLength.Value = value;
}

/// <summary>
/// Slow EMA length.
/// </summary>
public int EmaSlowLength
{
get => _emaSlowLength.Value;
set => _emaSlowLength.Value = value;
}

/// <summary>
/// Enable volume filter.
/// </summary>
public bool VolumeFilter
{
get => _volumeFilter.Value;
set => _volumeFilter.Value = value;
}

/// <summary>
/// Risk/reward ratio.
/// </summary>
public decimal RiskReward
{
get => _riskReward.Value;
set => _riskReward.Value = value;
}

/// <summary>
/// Ranging market threshold.
/// </summary>
public decimal RangingThreshold
{
get => _rangingThreshold.Value;
set => _rangingThreshold.Value = value;
}

/// <summary>
/// ATR multiplier for stop loss.
/// </summary>
public decimal AtrMultiplierSl
{
get => _atrMultiplierSl.Value;
set => _atrMultiplierSl.Value = value;
}

/// <summary>
/// ATR multiplier for take profit.
/// </summary>
public decimal AtrMultiplierTp
{
get => _atrMultiplierTp.Value;
set => _atrMultiplierTp.Value = value;
}

/// <summary>
/// Candle type to use.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initializes a new instance of the strategy.
/// </summary>
public EnhancedRangeFilterAtrTpSlStrategy()
{
_period = Param(nameof(Period), 100)
.SetGreaterThanZero()
.SetDisplay("Sampling Period", "Range filter sampling period", "Range Filter")
.SetCanOptimize(true)
.SetOptimize(50, 200, 25);

_multiplier = Param(nameof(Multiplier), 3m)
.SetGreaterThanZero()
.SetDisplay("Range Multiplier", "Range multiplier", "Range Filter")
.SetCanOptimize(true)
.SetOptimize(1m, 5m, 1m);

_atrLength = Param(nameof(AtrLength), 14)
.SetGreaterThanZero()
.SetDisplay("ATR Length", "ATR calculation period", "Indicators")
.SetCanOptimize(true)
.SetOptimize(7, 28, 7);

_rsiLength = Param(nameof(RsiLength), 14)
.SetGreaterThanZero()
.SetDisplay("RSI Length", "RSI period", "Indicators")
.SetCanOptimize(true)
.SetOptimize(7, 28, 7);

_rsiOverbought = Param(nameof(RsiOverbought), 65)
.SetGreaterThanZero()
.SetDisplay("RSI Overbought", "Overbought level", "Indicators")
.SetCanOptimize(true)
.SetOptimize(60, 80, 5);

_rsiOversold = Param(nameof(RsiOversold), 35)
.SetGreaterThanZero()
.SetDisplay("RSI Oversold", "Oversold level", "Indicators")
.SetCanOptimize(true)
.SetOptimize(20, 40, 5);

_emaFastLength = Param(nameof(EmaFastLength), 9)
.SetGreaterThanZero()
.SetDisplay("Fast EMA", "Fast EMA length", "Trend")
.SetCanOptimize(true)
.SetOptimize(5, 20, 5);

_emaSlowLength = Param(nameof(EmaSlowLength), 21)
.SetGreaterThanZero()
.SetDisplay("Slow EMA", "Slow EMA length", "Trend")
.SetCanOptimize(true)
.SetOptimize(20, 50, 5);

_volumeFilter = Param(nameof(VolumeFilter), true)
.SetDisplay("Enable Volume Filter", "Use volume confirmation", "Filters");

_riskReward = Param(nameof(RiskReward), 2m)
.SetGreaterThanZero()
.SetDisplay("Risk/Reward", "Risk reward ratio", "Risk")
.SetCanOptimize(true)
.SetOptimize(1m, 3m, 0.5m);

_rangingThreshold = Param(nameof(RangingThreshold), 0.5m)
.SetGreaterThanZero()
.SetDisplay("Ranging Threshold", "ATR ratio threshold", "Filters")
.SetCanOptimize(true)
.SetOptimize(0.3m, 0.8m, 0.1m);

_atrMultiplierSl = Param(nameof(AtrMultiplierSl), 1.5m)
.SetGreaterThanZero()
.SetDisplay("ATR SL Mult", "ATR multiplier for stop loss", "Risk")
.SetCanOptimize(true)
.SetOptimize(1m, 3m, 0.5m);

_atrMultiplierTp = Param(nameof(AtrMultiplierTp), 3m)
.SetGreaterThanZero()
.SetDisplay("ATR TP Mult", "ATR multiplier for take profit", "Risk")
.SetCanOptimize(true)
.SetOptimize(1m, 4m, 0.5m);

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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

_prevPrice = 0m;
_filter = 0m;
_upward = 0m;
_downward = 0m;
_condIni = 0;
_isFirst = true;
_stopLoss = 0m;
_takeProfit = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_emaFast = new ExponentialMovingAverage { Length = EmaFastLength };
_emaSlow = new ExponentialMovingAverage { Length = EmaSlowLength };
_atr = new AverageTrueRange { Length = AtrLength };
_rsi = new RelativeStrengthIndex { Length = RsiLength };
_volumeSma = new SimpleMovingAverage { Length = 20 };
_highest = new Highest { Length = 14 };
_lowest = new Lowest { Length = 14 };

_avrng = new ExponentialMovingAverage { Length = Period };
_smooth = new ExponentialMovingAverage { Length = Period * 2 - 1 };

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx([
_emaFast,
_emaSlow,
_atr,
_rsi,
_volumeSma,
_highest,
_lowest
], ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _emaFast);
DrawIndicator(area, _emaSlow);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var emaFast = values[0].ToDecimal();
var emaSlow = values[1].ToDecimal();
var atr = values[2].ToDecimal();
var rsi = values[3].ToDecimal();
var avgVolume = values[4].ToDecimal();
var highest = values[5].ToDecimal();
var lowest = values[6].ToDecimal();

var price = candle.ClosePrice;

var avrng = _avrng.Process(Math.Abs(price - _prevPrice)).ToDecimal();
var smooth = _smooth.Process(avrng).ToDecimal();
var smrng = smooth * Multiplier;

var prevFilt = _filter;

if (_isFirst)
{
_filter = price;
_isFirst = false;
}
else
{
if (price > prevFilt)
_filter = price - smrng < prevFilt ? prevFilt : price - smrng;
else
_filter = price + smrng > prevFilt ? prevFilt : price + smrng;
}

if (_filter > prevFilt)
{
_upward++;
_downward = 0m;
}
else if (_filter < prevFilt)
{
_downward++;
_upward = 0m;
}

var longCond = (price > _filter && price > _prevPrice && _upward > 0) || (price > _filter && price < _prevPrice && _upward > 0);
var shortCond = (price < _filter && price < _prevPrice && _downward > 0) || (price < _filter && price > _prevPrice && _downward > 0);

var prevCond = _condIni;
_condIni = longCond ? 1 : shortCond ? -1 : _condIni;

var longCondition = longCond && prevCond == -1;
var shortCondition = shortCond && prevCond == 1;

if (longCondition)
{
_stopLoss = price - atr * AtrMultiplierSl;
_takeProfit = price + atr * AtrMultiplierTp * RiskReward;
}
else if (shortCondition)
{
_stopLoss = price + atr * AtrMultiplierSl;
_takeProfit = price - atr * AtrMultiplierTp * RiskReward;
}

var volumeOk = candle.TotalVolume >= avgVolume * 1.2m || !VolumeFilter;
var rsiOk = (longCondition && rsi < RsiOverbought) || (shortCondition && rsi > RsiOversold);
var trendOk = (longCondition && emaFast > emaSlow) || (shortCondition && emaFast < emaSlow);

var priceRange = highest - lowest;
var atrRatio = priceRange != 0m ? atr / (priceRange / 14m) : 0m;
var isRanging = atrRatio < RangingThreshold;

var finalLong = longCondition && volumeOk && rsiOk && trendOk && !isRanging;
var finalShort = shortCondition && volumeOk && rsiOk && trendOk && !isRanging;

if (finalLong && Position <= 0)
BuyMarket(Volume);
else if (finalShort && Position >= 0)
SellMarket(Volume);

if (Position > 0)
{
if (price <= _stopLoss || price >= _takeProfit)
SellMarket(Math.Abs(Position));
}
else if (Position < 0)
{
if (price >= _stopLoss || price <= _takeProfit)
BuyMarket(Math.Abs(Position));
}

_prevPrice = price;
}
}
