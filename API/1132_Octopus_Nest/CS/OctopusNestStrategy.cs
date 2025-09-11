using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Octopus Nest strategy based on squeeze breakout and PSAR.
/// </summary>
public class OctopusNestStrategy : Strategy
{
private readonly StrategyParam<int> _emaLength;
private readonly StrategyParam<int> _bbLength;
private readonly StrategyParam<decimal> _bbMultiplier;
private readonly StrategyParam<int> _kcLength;
private readonly StrategyParam<decimal> _kcMultiplier;
private readonly StrategyParam<decimal> _stopMultiplier;
private readonly StrategyParam<int> _lookback;
private readonly StrategyParam<decimal> _rrRatio;
private readonly StrategyParam<decimal> _psarStep;
private readonly StrategyParam<decimal> _psarMax;
private readonly StrategyParam<DataType> _candleType;

private EMA _ema;
private BollingerBands _bollinger;
private KeltnerChannels _keltner;
private ParabolicSar _psar;
private Highest _highest;
private Lowest _lowest;

private decimal _longStop;
private decimal _longTake;
private decimal _shortStop;
private decimal _shortTake;

/// <summary>
/// EMA length.
/// </summary>
public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }

/// <summary>
/// Bollinger Bands length.
/// </summary>
public int BbLength { get => _bbLength.Value; set => _bbLength.Value = value; }

/// <summary>
/// Bollinger Bands multiplier.
/// </summary>
public decimal BbMultiplier { get => _bbMultiplier.Value; set => _bbMultiplier.Value = value; }

/// <summary>
/// Keltner Channels length.
/// </summary>
public int KcLength { get => _kcLength.Value; set => _kcLength.Value = value; }

/// <summary>
/// Keltner Channels multiplier.
/// </summary>
public decimal KcMultiplier { get => _kcMultiplier.Value; set => _kcMultiplier.Value = value; }

/// <summary>
/// Stop multiplier relative to extreme.
/// </summary>
public decimal StopMultiplier { get => _stopMultiplier.Value; set => _stopMultiplier.Value = value; }

/// <summary>
/// Lookback period for extremes.
/// </summary>
public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }

/// <summary>
/// Risk reward ratio.
/// </summary>
public decimal RrRatio { get => _rrRatio.Value; set => _rrRatio.Value = value; }

/// <summary>
/// Parabolic SAR step.
/// </summary>
public decimal PsarStep { get => _psarStep.Value; set => _psarStep.Value = value; }

/// <summary>
/// Parabolic SAR max step.
/// </summary>
public decimal PsarMax { get => _psarMax.Value; set => _psarMax.Value = value; }

/// <summary>
/// Candle type.
/// </summary>
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

public OctopusNestStrategy()
{
_emaLength = Param(nameof(EmaLength), 100).SetGreaterThanZero();
_bbLength = Param(nameof(BbLength), 20).SetGreaterThanZero();
_bbMultiplier = Param(nameof(BbMultiplier), 2m).SetGreaterThanZero();
_kcLength = Param(nameof(KcLength), 20).SetGreaterThanZero();
_kcMultiplier = Param(nameof(KcMultiplier), 1.5m).SetGreaterThanZero();
_stopMultiplier = Param(nameof(StopMultiplier), 0.98m).SetGreaterThanZero();
_lookback = Param(nameof(Lookback), 20).SetGreaterThanZero();
_rrRatio = Param(nameof(RrRatio), 1.125m).SetGreaterThanZero();
_psarStep = Param(nameof(PsarStep), 0.02m).SetGreaterThanZero();
_psarMax = Param(nameof(PsarMax), 0.2m).SetGreaterThanZero();
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
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
_longStop = _longTake = _shortStop = _shortTake = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_ema = new EMA { Length = EmaLength };
_bollinger = new BollingerBands { Length = BbLength, Width = BbMultiplier };
_keltner = new KeltnerChannels { Length = KcLength, Multiplier = KcMultiplier };
_psar = new ParabolicSar { AccelerationStep = PsarStep, AccelerationMax = PsarMax };
_highest = new Highest { Length = Lookback };
_lowest = new Lowest { Length = Lookback };

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx(_ema, _bollinger, _keltner, _psar, _highest, _lowest, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _ema);
DrawIndicator(area, _bollinger);
DrawIndicator(area, _keltner);
DrawIndicator(area, _psar);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue bbValue, IIndicatorValue kcValue, IIndicatorValue psarValue, IIndicatorValue highValue, IIndicatorValue lowValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!_ema.IsFormed || !_bollinger.IsFormed || !_keltner.IsFormed || !_psar.IsFormed || !_highest.IsFormed || !_lowest.IsFormed)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var ema = emaValue.ToDecimal();

var bb = (BollingerBandsValue)bbValue;
if (bb.UpBand is not decimal bbUpper || bb.LowBand is not decimal bbLower)
return;

var kc = (KeltnerChannelsValue)kcValue;
if (kc.Upper is not decimal kcUpper || kc.Lower is not decimal kcLower)
return;

var psar = psarValue.ToDecimal();
var highest = highValue.ToDecimal();
var lowest = lowValue.ToDecimal();

var squeeze = bbUpper < kcUpper && bbLower > kcLower;

var longCond = !squeeze && candle.ClosePrice > ema && candle.ClosePrice > psar;
var shortCond = !squeeze && candle.ClosePrice < ema && candle.ClosePrice < psar;

if (longCond && Position <= 0)
{
BuyMarket(Volume + Math.Abs(Position));
_longStop = lowest * StopMultiplier;
_longTake = candle.ClosePrice + (candle.ClosePrice - _longStop) * RrRatio;
}
else if (shortCond && Position >= 0)
{
SellMarket(Volume + Math.Abs(Position));
_shortStop = highest * (2m - StopMultiplier);
_shortTake = candle.ClosePrice - (_shortStop - candle.ClosePrice) * RrRatio;
}

if (Position > 0)
{
if (candle.LowPrice <= _longStop || candle.HighPrice >= _longTake)
SellMarket(Math.Abs(Position));
}
else if (Position < 0)
{
if (candle.HighPrice >= _shortStop || candle.LowPrice <= _shortTake)
BuyMarket(Math.Abs(Position));
}
}
}
