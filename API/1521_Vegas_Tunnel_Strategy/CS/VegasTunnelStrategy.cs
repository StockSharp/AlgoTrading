using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Vegas Tunnel strategy using multiple EMAs and ATR based stops.
/// </summary>
public class VegasTunnelStrategy : Strategy
{
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<decimal> _riskRewardRatio;
private readonly StrategyParam<bool> _useAtr;
private readonly StrategyParam<int> _atrLength;
private readonly StrategyParam<decimal> _atrMult;

private ExponentialMovingAverage _emaFast = null!;
private ExponentialMovingAverage _emaMedium = null!;
private ExponentialMovingAverage _emaSlow = null!;
private ExponentialMovingAverage _emaTunnel = null!;
private AverageTrueRange _atr = null!;

private decimal _stopPrice;
private decimal _takePrice;

/// <summary>
/// Candle type to process.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Risk/reward ratio for targets.
/// </summary>
public decimal RiskRewardRatio
{
get => _riskRewardRatio.Value;
set => _riskRewardRatio.Value = value;
}

/// <summary>
/// Use ATR for stop calculation.
/// </summary>
public bool UseAtr
{
get => _useAtr.Value;
set => _useAtr.Value = value;
}

/// <summary>
/// ATR length.
/// </summary>
public int AtrLength
{
get => _atrLength.Value;
set => _atrLength.Value = value;
}

/// <summary>
/// ATR multiplier.
/// </summary>
public decimal AtrMult
{
get => _atrMult.Value;
set => _atrMult.Value = value;
}

/// <summary>
/// Initializes a new instance of <see cref="VegasTunnelStrategy"/>.
/// </summary>
public VegasTunnelStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
.SetDisplay("Candle Type", "Timeframe", "General");

_riskRewardRatio = Param(nameof(RiskRewardRatio), 2m)
.SetGreaterThanZero()
.SetDisplay("Risk/Reward", "Risk to reward ratio", "General")
.SetCanOptimize(true);

_useAtr = Param(nameof(UseAtr), true)
.SetDisplay("Use ATR", "Use ATR for stop", "General");

_atrLength = Param(nameof(AtrLength), 14)
.SetGreaterThanZero()
.SetDisplay("ATR Length", "ATR period", "General")
.SetCanOptimize(true);

_atrMult = Param(nameof(AtrMult), 1.5m)
.SetGreaterThanZero()
.SetDisplay("ATR Mult", "ATR multiplier", "General")
.SetCanOptimize(true);
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

_emaFast = default!;
_emaMedium = default!;
_emaSlow = default!;
_emaTunnel = default!;
_atr = default!;
_stopPrice = 0m;
_takePrice = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_emaFast = new ExponentialMovingAverage { Length = 12 };
_emaMedium = new ExponentialMovingAverage { Length = 25 };
_emaSlow = new ExponentialMovingAverage { Length = 144 };
_emaTunnel = new ExponentialMovingAverage { Length = 169 };
_atr = new AverageTrueRange { Length = AtrLength };

var subscription = SubscribeCandles(CandleType);
subscription.Bind(_emaFast, _emaMedium, _emaSlow, _emaTunnel, _atr, ProcessCandle).Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _emaFast);
DrawIndicator(area, _emaMedium);
DrawIndicator(area, _emaSlow);
DrawIndicator(area, _emaTunnel);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal fast, decimal medium, decimal slow, decimal tunnel, decimal atr)
{
if (candle.State != CandleStates.Finished)
return;

var tunnelUp = slow < tunnel;
var tunnelDown = slow > tunnel;

var longCond = candle.ClosePrice > slow && candle.ClosePrice > tunnel && tunnelUp &&
fast > slow && fast > tunnel;
var shortCond = candle.ClosePrice < slow && candle.ClosePrice < tunnel && tunnelDown &&
fast < slow && fast < tunnel;

if (Position > 0)
{
if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
SellMarket(Position);
}
else if (Position < 0)
{
if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
BuyMarket(-Position);
}

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (longCond && Position <= 0)
{
var entry = candle.ClosePrice;
_stopPrice = UseAtr ? entry - AtrMult * atr : slow;
_takePrice = entry + (entry - _stopPrice) * RiskRewardRatio;
var volume = Volume + (Position < 0 ? -Position : 0m);
BuyMarket(volume);
}
else if (shortCond && Position >= 0)
{
var entry = candle.ClosePrice;
_stopPrice = UseAtr ? entry + AtrMult * atr : slow;
_takePrice = entry - (_stopPrice - entry) * RiskRewardRatio;
var volume = Volume + (Position > 0 ? Position : 0m);
SellMarket(volume);
}
}
}

