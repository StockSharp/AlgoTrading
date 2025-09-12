using System;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Uptrick X PineIndicators: Z-Score Flow Strategy.
/// </summary>
public class UptrickXPineIndicatorsZScoreFlowStrategy : Strategy
{
public enum TradeMode
{
Standard,
ZeroCross,
TrendReversal
}

private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<int> _zScorePeriod;
private readonly StrategyParam<int> _emaTrendLen;
private readonly StrategyParam<int> _rsiLen;
private readonly StrategyParam<int> _rsiEmaLen;
private readonly StrategyParam<decimal> _zBuyLevel;
private readonly StrategyParam<decimal> _zSellLevel;
private readonly StrategyParam<int> _cooldownBars;
private readonly StrategyParam<int> _slopeIndex;
private readonly StrategyParam<bool> _enableLong;
private readonly StrategyParam<bool> _enableShort;
private readonly StrategyParam<TradeMode> _tradeMode;

private SMA _basis;
private StandardDeviation _stdev;
private EMA _emaTrend;
private RSI _rsi;
private EMA _rsiEma;
private Shift _basisShift;

private bool _canBuy = true;
private bool _canSell = true;
private decimal _prevRsiEma;
private decimal _prevZscore;
private bool _prevBullish;
private bool _prevBearish;
private int? _lastBuyBar;
private int? _lastSellBar;
private int _barIndex;

/// <summary>
/// Candle type for calculations.
/// </summary>
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

/// <summary>
/// Z-score period length.
/// </summary>
public int ZScorePeriod { get => _zScorePeriod.Value; set => _zScorePeriod.Value = value; }

/// <summary>
/// EMA trend filter length.
/// </summary>
public int EmaTrendLen { get => _emaTrendLen.Value; set => _emaTrendLen.Value = value; }

/// <summary>
/// RSI length.
/// </summary>
public int RsiLen { get => _rsiLen.Value; set => _rsiLen.Value = value; }

/// <summary>
/// EMA of RSI length.
/// </summary>
public int RsiEmaLen { get => _rsiEmaLen.Value; set => _rsiEmaLen.Value = value; }

/// <summary>
/// Z-score buy threshold.
/// </summary>
public decimal ZBuyLevel { get => _zBuyLevel.Value; set => _zBuyLevel.Value = value; }

/// <summary>
/// Z-score sell threshold.
/// </summary>
public decimal ZSellLevel { get => _zSellLevel.Value; set => _zSellLevel.Value = value; }

/// <summary>
/// Cooldown bars between opposite trades.
/// </summary>
public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

/// <summary>
/// Bars back for slope comparison.
/// </summary>
public int SlopeIndex { get => _slopeIndex.Value; set => _slopeIndex.Value = value; }

/// <summary>
/// Enable long trades.
/// </summary>
public bool EnableLong { get => _enableLong.Value; set => _enableLong.Value = value; }

/// <summary>
/// Enable short trades.
/// </summary>
public bool EnableShort { get => _enableShort.Value; set => _enableShort.Value = value; }

/// <summary>
/// Trade execution mode.
/// </summary>
public TradeMode Mode { get => _tradeMode.Value; set => _tradeMode.Value = value; }

public UptrickXPineIndicatorsZScoreFlowStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");

_zScorePeriod = Param(nameof(ZScorePeriod), 100)
.SetGreaterThanZero()
.SetDisplay("Z-Score Period", "Period for z-score", "Indicators");

_emaTrendLen = Param(nameof(EmaTrendLen), 50)
.SetGreaterThanZero()
.SetDisplay("EMA Trend", "EMA trend length", "Indicators");

_rsiLen = Param(nameof(RsiLen), 14)
.SetGreaterThanZero()
.SetDisplay("RSI Length", "RSI period", "Indicators");

_rsiEmaLen = Param(nameof(RsiEmaLen), 8)
.SetGreaterThanZero()
.SetDisplay("RSI EMA Length", "EMA of RSI length", "Indicators");

_zBuyLevel = Param(nameof(ZBuyLevel), -2m)
.SetDisplay("Z-Score Buy", "Buy threshold", "Strategy");

_zSellLevel = Param(nameof(ZSellLevel), 2m)
.SetDisplay("Z-Score Sell", "Sell threshold", "Strategy");

_cooldownBars = Param(nameof(CooldownBars), 10)
.SetGreaterThanZero()
.SetDisplay("Cooldown", "Bars between trades", "Strategy");

_slopeIndex = Param(nameof(SlopeIndex), 30)
.SetGreaterThanZero()
.SetDisplay("Slope Index", "Bars for slope", "Strategy");

_enableLong = Param(nameof(EnableLong), true)
.SetDisplay("Enable Long", "Allow long trades", "Trading");

_enableShort = Param(nameof(EnableShort), true)
.SetDisplay("Enable Short", "Allow short trades", "Trading");

_tradeMode = Param(nameof(Mode), TradeMode.Standard)
.SetDisplay("Trade Mode", "Execution mode", "Trading");
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();
_canBuy = true;
_canSell = true;
_prevRsiEma = 0m;
_prevZscore = 0m;
_prevBullish = false;
_prevBearish = false;
_lastBuyBar = null;
_lastSellBar = null;
_barIndex = 0;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_basis = new SMA { Length = ZScorePeriod };
_stdev = new StandardDeviation { Length = ZScorePeriod };
_emaTrend = new EMA { Length = EmaTrendLen };
_rsi = new RSI { Length = RsiLen };
_rsiEma = new EMA { Length = RsiEmaLen };
_basisShift = new Shift { Length = SlopeIndex };

var subscription = SubscribeCandles(CandleType);
subscription.Bind(ProcessCandle).Start();
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

_barIndex++;

var basisValue = _basis.Process(candle).ToDecimal();
var stdevValue = _stdev.Process(candle).ToDecimal();
var emaTrend = _emaTrend.Process(candle).ToDecimal();
var rsiValue = _rsi.Process(candle).ToDecimal();
var rsiEmaValue = _rsiEma.Process(rsiValue, candle.OpenTime, true).ToDecimal();
var basisShift = _basisShift.Process(basisValue, candle.OpenTime, true).ToDecimal();

var zscore = stdevValue == 0 ? 0 : (candle.ClosePrice - basisValue) / stdevValue;

var isUptrend = candle.ClosePrice > emaTrend;
var isDowntrend = candle.ClosePrice < emaTrend;

if (rsiEmaValue > 30)
_canBuy = true;
if (rsiEmaValue < 70)
_canSell = true;

var rsiEmaSlope = rsiEmaValue - _prevRsiEma;
_prevRsiEma = rsiEmaValue;

var rsiBuyConfirm = rsiEmaValue < 30 && rsiEmaSlope > 0 && _canBuy;
var rsiSellConfirm = rsiEmaValue > 70 && rsiEmaSlope < 0 && _canSell;

var rawBuy = zscore < ZBuyLevel && isDowntrend && rsiBuyConfirm;
var rawSell = zscore > ZSellLevel && isUptrend && rsiSellConfirm;

if (rawBuy)
_canBuy = false;
if (rawSell)
_canSell = false;

var buySignal = rawBuy && (_lastSellBar is null || _barIndex - _lastSellBar > CooldownBars);
var sellSignal = rawSell && (_lastBuyBar is null || _barIndex - _lastBuyBar > CooldownBars);

if (buySignal)
_lastBuyBar = _barIndex;
if (sellSignal)
_lastSellBar = _barIndex;

var bullishCondition = basisValue > basisShift;
var bearishCondition = basisValue < basisShift;

var trendBullishReversal = _prevBearish && bullishCondition;
var trendBearishReversal = _prevBullish && bearishCondition;

_prevBullish = bullishCondition;
_prevBearish = bearishCondition;

switch (Mode)
{
case TradeMode.Standard:
StandardMode(buySignal, sellSignal);
break;
case TradeMode.ZeroCross:
ZeroCrossMode(zscore);
break;
case TradeMode.TrendReversal:
TrendReversalMode(trendBullishReversal, trendBearishReversal);
break;
}

_prevZscore = zscore;
}

private void StandardMode(bool buySignal, bool sellSignal)
{
if (EnableShort && buySignal && Position < 0)
BuyMarket(Math.Abs(Position));
if (EnableLong && buySignal && Position <= 0)
BuyMarket();

if (EnableLong && sellSignal && Position > 0)
SellMarket(Position);
if (EnableShort && sellSignal && Position >= 0)
SellMarket();
}

private void ZeroCrossMode(decimal zscore)
{
var crossUp = _prevZscore <= 0 && zscore > 0;
var crossDown = _prevZscore >= 0 && zscore < 0;

if (crossUp)
{
if (EnableShort && Position < 0)
BuyMarket(Math.Abs(Position));
if (EnableLong && Position <= 0)
BuyMarket();
}

if (crossDown)
{
if (EnableLong && Position > 0)
SellMarket(Position);
if (EnableShort && Position >= 0)
SellMarket();
}
}

private void TrendReversalMode(bool bullishReversal, bool bearishReversal)
{
if (bullishReversal)
{
if (EnableShort && Position < 0)
BuyMarket(Math.Abs(Position));
if (EnableLong && Position <= 0)
BuyMarket();
}

if (bearishReversal)
{
if (EnableLong && Position > 0)
SellMarket(Position);
if (EnableShort && Position >= 0)
SellMarket();
}
}
}
