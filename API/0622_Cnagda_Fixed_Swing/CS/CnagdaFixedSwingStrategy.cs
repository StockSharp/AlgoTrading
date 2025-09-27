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




public class CnagdaFixedSwingStrategy : Strategy
{
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<TradeLogics> _tradeLogic;
private readonly StrategyParam<int> _swingLookback;
private readonly StrategyParam<int> _riskReward;

private ExponentialMovingAverage _ema34;
private WeightedMovingAverage _wma34;
private SimpleMovingAverage _sma34;
private RelativeStrengthIndex _rsi;
private ExponentialMovingAverage _rsiEma3;
private ExponentialMovingAverage _rsiEma10;
private SimpleMovingAverage _volumeSma;
private Lowest _swingLow;
private Highest _swingHigh;

private decimal _haOpen;
private decimal _prevHaClose;
private int _barIndex;

private decimal _prevMa1;
private decimal _prevMa2;
private decimal _prevRsiEma3;
private decimal _prevRsiEma10;
private bool _prevHighVol;

private decimal? _refHigh;
private decimal? _refLow;
private ScalpStates _scalpState;
private ScalpStates _prevScalpState;

private decimal? _rsiSignalHigh;
private decimal? _rsiSignalLow;
private int? _rsiSignalBar;
private RsiEntryStates _rsiEntryState;
private RsiEntryStates _prevRsiEntryState;

private decimal? _slLong;
private decimal? _tgLong;
private decimal? _slShort;
private decimal? _tgShort;

public CnagdaFixedSwingStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Type of candles for calculations", "General");

_tradeLogic = Param(nameof(Logic), TradeLogics.Rsi)
.SetDisplay("Trade Logic", "RSI or Scalp entry logic", "General");

_swingLookback = Param(nameof(SwingLookback), 34)
.SetGreaterThanZero()
.SetDisplay("Swing Lookback", "Bars used for swing high/low", "Risk");

_riskReward = Param(nameof(RiskReward), 3)
.SetGreaterThanZero()
.SetDisplay("Risk Reward", "Risk/Reward multiple", "Risk");
}

public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

public TradeLogics Logic
{
get => _tradeLogic.Value;
set => _tradeLogic.Value = value;
}

public int SwingLookback
{
get => _swingLookback.Value;
set => _swingLookback.Value = value;
}

public int RiskReward
{
get => _riskReward.Value;
set => _riskReward.Value = value;
}

public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
=> [(Security, CandleType)];

protected override void OnReseted()
{
base.OnReseted();
_ema34 = new() { Length = 34 };
_wma34 = new() { Length = 34 };
_sma34 = new() { Length = 34 };
_rsi = new() { Length = 14 };
_rsiEma3 = new() { Length = 3 };
_rsiEma10 = new() { Length = 10 };
_volumeSma = new() { Length = 20 };
_swingLow = new() { Length = SwingLookback };
_swingHigh = new() { Length = SwingLookback };
_haOpen = 0;
_prevHaClose = 0;
_barIndex = 0;
_prevMa1 = 0;
_prevMa2 = 0;
_prevRsiEma3 = 0;
_prevRsiEma10 = 0;
_prevHighVol = false;
_refHigh = null;
_refLow = null;
_scalpState = ScalpStates.Neutral;
_prevScalpState = ScalpStates.Neutral;
_rsiSignalHigh = null;
_rsiSignalLow = null;
_rsiSignalBar = null;
_rsiEntryState = RsiEntryStates.None;
_prevRsiEntryState = RsiEntryStates.None;
_slLong = null;
_tgLong = null;
_slShort = null;
_tgShort = null;
}

protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var subscription = SubscribeCandles(CandleType);
subscription.Bind(ProcessCandle).Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

_barIndex++;

var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
_haOpen = _barIndex == 1 ? (candle.OpenPrice + candle.ClosePrice) / 2m : (_haOpen + _prevHaClose) / 2m;
var haHigh = Math.Max(candle.HighPrice, Math.Max(_haOpen, haClose));
var haLow = Math.Min(candle.LowPrice, Math.Min(_haOpen, haClose));
_prevHaClose = haClose;

var emaVal = _ema34.Process(haClose, candle.OpenTime, true).ToDecimal();
var wmaVal = _wma34.Process(haClose, candle.OpenTime, true).ToDecimal();
var smaVal = _sma34.Process(haClose, candle.OpenTime, true).ToDecimal();
var rsiVal = _rsi.Process(haClose, candle.OpenTime, true).ToDecimal();
var rsiEma3Val = _rsiEma3.Process(rsiVal, candle.OpenTime, true).ToDecimal();
var rsiEma10Val = _rsiEma10.Process(rsiVal, candle.OpenTime, true).ToDecimal();
var volSmaVal = _volumeSma.Process(candle.TotalVolume, candle.OpenTime, true).ToDecimal();

var swingLowVal = _swingLow.Process(haLow, candle.OpenTime, true).ToDecimal();
var swingHighVal = _swingHigh.Process(haHigh, candle.OpenTime, true).ToDecimal();
decimal? swingLow = _swingLow.IsFormed ? swingLowVal : null;
decimal? swingHigh = _swingHigh.IsFormed ? swingHighVal : null;

var maAvg = (emaVal + wmaVal + smaVal) / 3m;

var buySignal = _prevMa1 < _prevMa2 && emaVal > wmaVal;
var sellSignal = _prevMa1 > _prevMa2 && emaVal < wmaVal;

if (buySignal || sellSignal)
{
_refHigh = haHigh;
_refLow = haLow;
_scalpState = ScalpStates.WaitEntry;
}

if ((_scalpState == ScalpStates.WaitEntry || _scalpState == ScalpStates.Sell) && _refHigh != null && haClose > _refHigh)
_scalpState = ScalpStates.Buy;

if ((_scalpState == ScalpStates.WaitEntry || _scalpState == ScalpStates.Buy) && _refLow != null && haClose < _refLow)
_scalpState = ScalpStates.Sell;

if (_scalpState == ScalpStates.Buy && haClose < maAvg)
{
_scalpState = ScalpStates.Neutral;
_refHigh = null;
_refLow = null;
}
if (_scalpState == ScalpStates.Sell && haClose > maAvg)
{
_scalpState = ScalpStates.Neutral;
_refHigh = null;
_refLow = null;
}

var highVol = candle.TotalVolume > volSmaVal * 1.5m;
var anyHighBar = highVol || _prevHighVol;
var baseRsiCrossBull = _prevRsiEma3 <= _prevRsiEma10 && rsiEma3Val > rsiEma10Val && anyHighBar;
var baseRsiCrossBear = _prevRsiEma3 >= _prevRsiEma10 && rsiEma3Val < rsiEma10Val && anyHighBar;
var rsiCrossBull = baseRsiCrossBull && haClose < maAvg;
var rsiCrossBear = baseRsiCrossBear && haClose > maAvg;

if (rsiCrossBull)
{
_rsiSignalHigh = candle.HighPrice;
_rsiSignalLow = null;
_rsiSignalBar = _barIndex;
_rsiEntryState = RsiEntryStates.WaitEntry;
}
else if (rsiCrossBear)
{
_rsiSignalLow = candle.LowPrice;
_rsiSignalHigh = null;
_rsiSignalBar = _barIndex;
_rsiEntryState = RsiEntryStates.WaitEntry;
}
else if (_rsiSignalBar != null)
{
if (_rsiSignalHigh != null)
{
if (candle.ClosePrice > _rsiSignalHigh && _barIndex > _rsiSignalBar)
{
_rsiEntryState = RsiEntryStates.Buy;
_rsiSignalHigh = null;
_rsiSignalBar = null;
}
else
_rsiEntryState = RsiEntryStates.WaitEntry;
}
else if (_rsiSignalLow != null)
{
if (candle.ClosePrice < _rsiSignalLow && _barIndex > _rsiSignalBar)
{
_rsiEntryState = RsiEntryStates.Sell;
_rsiSignalLow = null;
_rsiSignalBar = null;
}
else
_rsiEntryState = RsiEntryStates.WaitEntry;
}
else
{
_rsiEntryState = RsiEntryStates.None;
}
}
else
{
_rsiEntryState = RsiEntryStates.None;
}

var longCondition = Logic == TradeLogics.Rsi
? _rsiEntryState == RsiEntryStates.Buy && _prevRsiEntryState != RsiEntryStates.Buy
: _scalpState == ScalpStates.Buy && _prevScalpState != ScalpStates.Buy;

var shortCondition = Logic == TradeLogics.Rsi
? _rsiEntryState == RsiEntryStates.Sell && _prevRsiEntryState != RsiEntryStates.Sell
: _scalpState == ScalpStates.Sell && _prevScalpState != ScalpStates.Sell;

var exitLongCondition = Logic == TradeLogics.Rsi
? _rsiEntryState == RsiEntryStates.Sell && _prevRsiEntryState != RsiEntryStates.Sell
: _scalpState == ScalpStates.Sell && _prevScalpState != ScalpStates.Sell;

var exitShortCondition = Logic == TradeLogics.Rsi
? _rsiEntryState == RsiEntryStates.Buy && _prevRsiEntryState != RsiEntryStates.Buy
: _scalpState == ScalpStates.Buy && _prevScalpState != ScalpStates.Buy;

if (longCondition && swingLow != null)
{
_slLong = swingLow;
var entry = candle.ClosePrice;
var risk = entry - _slLong.Value;
_tgLong = entry + risk * RiskReward;
BuyMarket(Volume + Math.Abs(Position));
}

if (shortCondition && swingHigh != null)
{
_slShort = swingHigh;
var entry = candle.ClosePrice;
var risk = _slShort.Value - entry;
_tgShort = entry - risk * RiskReward;
SellMarket(Volume + Math.Abs(Position));
}

if (exitLongCondition && Position > 0)
{
SellMarket(Math.Abs(Position));
_slLong = null;
_tgLong = null;
}

if (exitShortCondition && Position < 0)
{
BuyMarket(Math.Abs(Position));
_slShort = null;
_tgShort = null;
}

if (Position > 0)
{
if (_slLong != null && candle.LowPrice <= _slLong)
{
SellMarket(Math.Abs(Position));
_slLong = null;
_tgLong = null;
}
else if (_tgLong != null && candle.HighPrice >= _tgLong)
{
SellMarket(Math.Abs(Position));
_slLong = null;
_tgLong = null;
}
}
else if (Position < 0)
{
if (_slShort != null && candle.HighPrice >= _slShort)
{
BuyMarket(Math.Abs(Position));
_slShort = null;
_tgShort = null;
}
else if (_tgShort != null && candle.LowPrice <= _tgShort)
{
BuyMarket(Math.Abs(Position));
_slShort = null;
_tgShort = null;
}
}

_prevMa1 = emaVal;
_prevMa2 = wmaVal;
_prevRsiEma3 = rsiEma3Val;
_prevRsiEma10 = rsiEma10Val;
_prevHighVol = highVol;
_prevScalpState = _scalpState;
_prevRsiEntryState = _rsiEntryState;
}

private enum ScalpStates
{
Neutral,
WaitEntry,
Buy,
Sell
}

private enum RsiEntryStates
{
None,
WaitEntry,
Buy,
Sell
}

public enum TradeLogics
{
Rsi,
Scalp
}
}
