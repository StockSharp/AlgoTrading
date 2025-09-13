using System;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// VIDYA ProTrend strategy with multi-tier take profit.
/// Uses fast and slow VIDYA with Bollinger filter and optional multi-step exits.
/// </summary>
public class VidyaProTrendMultiTierProfitStrategy : Strategy
{
	private readonly StrategyParam<Sides?> _tradeDirection;
private readonly StrategyParam<int> _fastVidyaLength;
private readonly StrategyParam<int> _slowVidyaLength;
private readonly StrategyParam<decimal> _minSlopeThreshold;
private readonly StrategyParam<int> _bbLength;
private readonly StrategyParam<decimal> _bbMultiplier;

private readonly StrategyParam<bool> _useMultiStepTp;
private readonly StrategyParam<string> _tpDirection;
private readonly StrategyParam<int> _atrLengthTp;
private readonly StrategyParam<decimal> _atrMultiplierTp1;
private readonly StrategyParam<decimal> _atrMultiplierTp2;
private readonly StrategyParam<decimal> _atrMultiplierTp3;
private readonly StrategyParam<decimal> _shortTpPercentMultiplier;
private readonly StrategyParam<decimal> _tpLevelPercent1;
private readonly StrategyParam<decimal> _tpLevelPercent2;
private readonly StrategyParam<decimal> _tpLevelPercent3;
private readonly StrategyParam<decimal> _tpPercent1;
private readonly StrategyParam<decimal> _tpPercent2;
private readonly StrategyParam<decimal> _tpPercent3;
private readonly StrategyParam<decimal> _tpPercentAtr1;
private readonly StrategyParam<decimal> _tpPercentAtr2;
private readonly StrategyParam<decimal> _tpPercentAtr3;
private readonly StrategyParam<DataType> _candleType;

private ChandeMomentumOscillator _cmoFast;
private ChandeMomentumOscillator _cmoSlow;
private LinearRegression _fastSlopeReg;
private LinearRegression _slowSlopeReg;
private BollingerBands _bollinger;
private AverageTrueRange _atrTp;

private decimal? _fastVidya;
private decimal? _slowVidya;
private decimal _entryPrice;
private bool _tpLongPlaced;
private bool _tpShortPlaced;

	public Sides? TradeDirection { get => _tradeDirection.Value; set => _tradeDirection.Value = value; }
public int FastVidyaLength { get => _fastVidyaLength.Value; set => _fastVidyaLength.Value = value; }
public int SlowVidyaLength { get => _slowVidyaLength.Value; set => _slowVidyaLength.Value = value; }
public decimal MinSlopeThreshold { get => _minSlopeThreshold.Value; set => _minSlopeThreshold.Value = value; }
public int BbLength { get => _bbLength.Value; set => _bbLength.Value = value; }
public decimal BbMultiplier { get => _bbMultiplier.Value; set => _bbMultiplier.Value = value; }
public bool UseMultiStepTp { get => _useMultiStepTp.Value; set => _useMultiStepTp.Value = value; }
	public string TpDirection { get => _tpDirection.Value; set => _tpDirection.Value = value; }
public int AtrLengthTp { get => _atrLengthTp.Value; set => _atrLengthTp.Value = value; }
public decimal AtrMultiplierTp1 { get => _atrMultiplierTp1.Value; set => _atrMultiplierTp1.Value = value; }
public decimal AtrMultiplierTp2 { get => _atrMultiplierTp2.Value; set => _atrMultiplierTp2.Value = value; }
public decimal AtrMultiplierTp3 { get => _atrMultiplierTp3.Value; set => _atrMultiplierTp3.Value = value; }
public decimal ShortTpPercentMultiplier { get => _shortTpPercentMultiplier.Value; set => _shortTpPercentMultiplier.Value = value; }
public decimal TpLevelPercent1 { get => _tpLevelPercent1.Value; set => _tpLevelPercent1.Value = value; }
public decimal TpLevelPercent2 { get => _tpLevelPercent2.Value; set => _tpLevelPercent2.Value = value; }
public decimal TpLevelPercent3 { get => _tpLevelPercent3.Value; set => _tpLevelPercent3.Value = value; }
public decimal TpPercent1 { get => _tpPercent1.Value; set => _tpPercent1.Value = value; }
public decimal TpPercent2 { get => _tpPercent2.Value; set => _tpPercent2.Value = value; }
public decimal TpPercent3 { get => _tpPercent3.Value; set => _tpPercent3.Value = value; }
public decimal TpPercentAtr1 { get => _tpPercentAtr1.Value; set => _tpPercentAtr1.Value = value; }
public decimal TpPercentAtr2 { get => _tpPercentAtr2.Value; set => _tpPercentAtr2.Value = value; }
public decimal TpPercentAtr3 { get => _tpPercentAtr3.Value; set => _tpPercentAtr3.Value = value; }
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

public VidyaProTrendMultiTierProfitStrategy()
{
	_tradeDirection = Param(nameof(TradeDirection), (Sides?)null)
	.SetDisplay("Trading Direction", "Allowed directions", "General");

_fastVidyaLength = Param(nameof(FastVidyaLength), 10)
.SetGreaterThanZero()
.SetDisplay("Fast VIDYA Length", "Fast VIDYA period", "General")
.SetCanOptimize(true);

_slowVidyaLength = Param(nameof(SlowVidyaLength), 30)
.SetGreaterThanZero()
.SetDisplay("Slow VIDYA Length", "Slow VIDYA period", "General")
.SetCanOptimize(true);

_minSlopeThreshold = Param(nameof(MinSlopeThreshold), 0.05m)
.SetDisplay("Slope Threshold", "Minimum slope", "General")
.SetCanOptimize(true);

_bbLength = Param(nameof(BbLength), 20)
.SetGreaterThanZero()
.SetDisplay("BB Length", "Bollinger length", "Bollinger")
.SetCanOptimize(true);

_bbMultiplier = Param(nameof(BbMultiplier), 1m)
.SetDisplay("BB Multiplier", "Bollinger width", "Bollinger")
.SetCanOptimize(true);

_useMultiStepTp = Param(nameof(UseMultiStepTp), true)
.SetDisplay("Use Multi-Step TP", "Enable multi-tier TP", "Take Profit");

_tpDirection = Param(nameof(TpDirection), "Both")
.SetDisplay("TP Direction", "Take profit side", "Take Profit");

_atrLengthTp = Param(nameof(AtrLengthTp), 14)
.SetGreaterThanZero()
.SetDisplay("ATR Length", "ATR period for TP", "Take Profit");

_atrMultiplierTp1 = Param(nameof(AtrMultiplierTp1), 2.618m)
.SetDisplay("ATR Multiplier TP1", "ATR multiplier level 1", "Take Profit");
_atrMultiplierTp2 = Param(nameof(AtrMultiplierTp2), 5m)
.SetDisplay("ATR Multiplier TP2", "ATR multiplier level 2", "Take Profit");
_atrMultiplierTp3 = Param(nameof(AtrMultiplierTp3), 10m)
.SetDisplay("ATR Multiplier TP3", "ATR multiplier level 3", "Take Profit");

_shortTpPercentMultiplier = Param(nameof(ShortTpPercentMultiplier), 1.5m)
.SetDisplay("Short TP Multiplier", "Multiplier for short percentages", "Take Profit");

_tpLevelPercent1 = Param(nameof(TpLevelPercent1), 3m)
.SetDisplay("TP Level Percent1", "Percent level 1", "Take Profit");
_tpLevelPercent2 = Param(nameof(TpLevelPercent2), 8m)
.SetDisplay("TP Level Percent2", "Percent level 2", "Take Profit");
_tpLevelPercent3 = Param(nameof(TpLevelPercent3), 17m)
.SetDisplay("TP Level Percent3", "Percent level 3", "Take Profit");

_tpPercent1 = Param(nameof(TpPercent1), 12m)
.SetDisplay("TP Percent1", "Volume percent 1", "Take Profit");
_tpPercent2 = Param(nameof(TpPercent2), 8m)
.SetDisplay("TP Percent2", "Volume percent 2", "Take Profit");
_tpPercent3 = Param(nameof(TpPercent3), 10m)
.SetDisplay("TP Percent3", "Volume percent 3", "Take Profit");

_tpPercentAtr1 = Param(nameof(TpPercentAtr1), 10m)
.SetDisplay("TP ATR Percent1", "ATR volume percent 1", "Take Profit");
_tpPercentAtr2 = Param(nameof(TpPercentAtr2), 10m)
.SetDisplay("TP ATR Percent2", "ATR volume percent 2", "Take Profit");
_tpPercentAtr3 = Param(nameof(TpPercentAtr3), 10m)
.SetDisplay("TP ATR Percent3", "ATR volume percent 3", "Take Profit");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_cmoFast = new ChandeMomentumOscillator { Length = FastVidyaLength };
_cmoSlow = new ChandeMomentumOscillator { Length = SlowVidyaLength };
_fastSlopeReg = new LinearRegression { Length = FastVidyaLength };
_slowSlopeReg = new LinearRegression { Length = SlowVidyaLength };
_bollinger = new BollingerBands { Length = BbLength, Width = BbMultiplier };
_atrTp = new AverageTrueRange { Length = AtrLengthTp };

var sub = SubscribeCandles(CandleType);
sub.WhenNew(ProcessCandle).Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, sub);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

var cmoFastVal = _cmoFast.Process(candle.ClosePrice);
var cmoSlowVal = _cmoSlow.Process(candle.ClosePrice);
var bbVal = (BollingerBandsValue)_bollinger.Process(candle.ClosePrice);
var atrVal = _atrTp.Process(candle);

if (!cmoFastVal.IsFinal || !cmoSlowVal.IsFinal ||
bbVal.UpBand is not decimal upper || bbVal.LowBand is not decimal lower ||
bbVal.MovingAverage is not decimal middle || !atrVal.IsFinal)
return;

var alphaFast = 2m / (FastVidyaLength + 1);
var alphaSlow = 2m / (SlowVidyaLength + 1);
var absFast = Math.Abs(cmoFastVal.GetValue<decimal>());
var absSlow = Math.Abs(cmoSlowVal.GetValue<decimal>());

_fastVidya = (_fastVidya ?? candle.ClosePrice);
_slowVidya = (_slowVidya ?? candle.ClosePrice);
_fastVidya = alphaFast * absFast / 100m * candle.ClosePrice + (1 - alphaFast * absFast / 100m) * _fastVidya.Value;
_slowVidya = alphaSlow * absSlow / 100m * candle.ClosePrice + (1 - alphaSlow * absSlow / 100m) * _slowVidya.Value;

var fastSlopeVal = (LinearRegressionValue)_fastSlopeReg.Process(_fastVidya.Value);
var slowSlopeVal = (LinearRegressionValue)_slowSlopeReg.Process(_slowVidya.Value);
if (fastSlopeVal.LinearRegSlope is not decimal fastSlope || slowSlopeVal.LinearRegSlope is not decimal slowSlope)
return;

var longCondition = candle.ClosePrice > _slowVidya.Value &&
_fastVidya.Value > _slowVidya.Value &&
fastSlope > MinSlopeThreshold &&
slowSlope > MinSlopeThreshold / 2m &&
candle.ClosePrice > upper;

var shortCondition = candle.ClosePrice < _slowVidya.Value &&
_fastVidya.Value < _slowVidya.Value &&
fastSlope < -MinSlopeThreshold &&
slowSlope < -MinSlopeThreshold / 2m &&
candle.ClosePrice < lower;

var exitLongCondition = fastSlope < -MinSlopeThreshold && slowSlope < -MinSlopeThreshold / 2m;
var exitShortCondition = fastSlope > MinSlopeThreshold && slowSlope > MinSlopeThreshold / 2m;

	var allowLong = TradeDirection != Sides.Sell;
	var allowShort = TradeDirection != Sides.Buy;

if (allowLong && longCondition && Position <= 0)
{
var volume = Volume + (Position < 0 ? -Position : 0m);
BuyMarket(volume);
_entryPrice = candle.ClosePrice;
_tpLongPlaced = false;
}
else if (allowLong && exitLongCondition && Position > 0)
{
SellMarket(Position);
_tpLongPlaced = false;
}

if (allowShort && shortCondition && Position >= 0)
{
var volume = Volume + (Position > 0 ? Position : 0m);
SellMarket(volume);
_entryPrice = candle.ClosePrice;
_tpShortPlaced = false;
}
else if (allowShort && exitShortCondition && Position < 0)
{
BuyMarket(-Position);
_tpShortPlaced = false;
}

var tpLongAllowed = TpDirection.Equals("Long", StringComparison.OrdinalIgnoreCase) || TpDirection.Equals("Both", StringComparison.OrdinalIgnoreCase);
var tpShortAllowed = TpDirection.Equals("Short", StringComparison.OrdinalIgnoreCase) || TpDirection.Equals("Both", StringComparison.OrdinalIgnoreCase);

if (UseMultiStepTp && Position > 0 && !_tpLongPlaced && tpLongAllowed)
{
var atr = atrVal.GetValue<decimal>();
var vol = Position;
SellLimit(_entryPrice + AtrMultiplierTp1 * atr, vol * TpPercentAtr1 / 100m);
SellLimit(_entryPrice + AtrMultiplierTp2 * atr, vol * TpPercentAtr2 / 100m);
SellLimit(_entryPrice + AtrMultiplierTp3 * atr, vol * TpPercentAtr3 / 100m);
SellLimit(_entryPrice * (1 + TpLevelPercent1 / 100m), vol * TpPercent1 / 100m);
SellLimit(_entryPrice * (1 + TpLevelPercent2 / 100m), vol * TpPercent2 / 100m);
SellLimit(_entryPrice * (1 + TpLevelPercent3 / 100m), vol * TpPercent3 / 100m);
_tpLongPlaced = true;
}
else if (UseMultiStepTp && Position < 0 && !_tpShortPlaced && tpShortAllowed)
{
var atr = atrVal.GetValue<decimal>();
var vol = -Position;
var pct1 = TpPercent1 * ShortTpPercentMultiplier;
var pct2 = TpPercent2 * ShortTpPercentMultiplier;
var pct3 = TpPercent3 * ShortTpPercentMultiplier;
var pctAtr1 = TpPercentAtr1 * ShortTpPercentMultiplier;
var pctAtr2 = TpPercentAtr2 * ShortTpPercentMultiplier;
var pctAtr3 = TpPercentAtr3 * ShortTpPercentMultiplier;
BuyLimit(_entryPrice - AtrMultiplierTp1 * atr, vol * pctAtr1 / 100m);
BuyLimit(_entryPrice - AtrMultiplierTp2 * atr, vol * pctAtr2 / 100m);
BuyLimit(_entryPrice - AtrMultiplierTp3 * atr, vol * pctAtr3 / 100m);
BuyLimit(_entryPrice * (1 - TpLevelPercent1 / 100m), vol * pct1 / 100m);
BuyLimit(_entryPrice * (1 - TpLevelPercent2 / 100m), vol * pct2 / 100m);
BuyLimit(_entryPrice * (1 - TpLevelPercent3 / 100m), vol * pct3 / 100m);
_tpShortPlaced = true;
}
}
}

