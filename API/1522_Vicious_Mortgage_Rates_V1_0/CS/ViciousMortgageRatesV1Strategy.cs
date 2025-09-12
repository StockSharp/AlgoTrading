using System;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Vicious Mortgage Rates strategy.
/// Combines four volatility indexes and trades on EMA cross of their product.
/// </summary>
public class ViciousMortgageRatesV1Strategy : Strategy
{
private readonly StrategyParam<int> _fastLength;
private readonly StrategyParam<int> _slowLength;
private readonly StrategyParam<Security> _security2;
private readonly StrategyParam<Security> _security3;
private readonly StrategyParam<Security> _security4;
private readonly StrategyParam<DataType> _candleType;

private ExponentialMovingAverage _fastEma;
private ExponentialMovingAverage _slowEma;

private decimal _close1;
private decimal _close2;
private decimal _close3;
private decimal _close4;
private bool _ready2;
private bool _ready3;
private bool _ready4;

public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
public Security Security2 { get => _security2.Value; set => _security2.Value = value; }
public Security Security3 { get => _security3.Value; set => _security3.Value = value; }
public Security Security4 { get => _security4.Value; set => _security4.Value = value; }
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

public ViciousMortgageRatesV1Strategy()
{
_fastLength = Param(nameof(FastLength), 8)
.SetGreaterThanZero()
.SetDisplay("Fast EMA", "Fast EMA length", "General")
.SetCanOptimize(true);

_slowLength = Param(nameof(SlowLength), 21)
.SetGreaterThanZero()
.SetDisplay("Slow EMA", "Slow EMA length", "General")
.SetCanOptimize(true);

_security2 = Param(nameof(Security2), new Security())
.SetDisplay("Symbol 2", "Second volatility index", "Securities");
_security3 = Param(nameof(Security3), new Security())
.SetDisplay("Symbol 3", "Third volatility index", "Securities");
_security4 = Param(nameof(Security4), new Security())
.SetDisplay("Symbol 4", "Fourth volatility index", "Securities");

_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
.SetDisplay("Candle Type", "Timeframe for all securities", "General");
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_fastEma = new ExponentialMovingAverage { Length = FastLength };
_slowEma = new ExponentialMovingAverage { Length = SlowLength };

var mainSub = SubscribeCandles(CandleType);
mainSub.WhenNew(ProcessMain).Start();

SubscribeComponent(Security2, v => { _close2 = v; _ready2 = true; });
SubscribeComponent(Security3, v => { _close3 = v; _ready3 = true; });
SubscribeComponent(Security4, v => { _close4 = v; _ready4 = true; });

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, mainSub);
DrawOwnTrades(area);
}
}

private void SubscribeComponent(Security security, Action<decimal> setter)
{
if (security == null)
return;

var sub = SubscribeCandles(CandleType, false, security);
sub.WhenNew(candle =>
{
if (candle.State != CandleStates.Finished)
return;
setter(candle.ClosePrice);
}).Start();
}

private void ProcessMain(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

_close1 = candle.ClosePrice;

if (!(_ready2 && _ready3 && _ready4))
return;

var product = _close1 * _close2 * _close3 * _close4;

var fastValue = _fastEma.Process(product);
var slowValue = _slowEma.Process(product);

if (!fastValue.IsFinal || !slowValue.IsFinal)
return;

var fast = fastValue.GetValue<decimal>();
var slow = slowValue.GetValue<decimal>();

if (fast > slow && Position <= 0)
{
var volume = Volume + (Position < 0 ? -Position : 0m);
BuyMarket(volume);
}
else if (fast < slow && Position >= 0)
{
var volume = Volume + (Position > 0 ? Position : 0m);
SellMarket(volume);
}
}
}

