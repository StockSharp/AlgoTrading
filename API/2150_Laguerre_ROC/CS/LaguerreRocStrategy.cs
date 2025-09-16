using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Laguerre rate of change oscillator.
/// The oscillator is calculated using a four-stage Laguerre filter
/// applied to a standard rate of change indicator.
/// </summary>
public class LaguerreRocStrategy : Strategy
{
private readonly StrategyParam<int> _period;
private readonly StrategyParam<decimal> _gamma;
private readonly StrategyParam<decimal> _upLevel;
private readonly StrategyParam<decimal> _downLevel;
private readonly StrategyParam<DataType> _candleType;

private RateOfChange? _roc;
private decimal _l0;
private decimal _l1;
private decimal _l2;
private decimal _l3;
private bool _isFirst = true;
private int _prevColor = 2;

/// <summary>
/// Rate of change lookback period.
/// </summary>
public int Period
{
get => _period.Value;
set => _period.Value = value;
}

/// <summary>
/// Laguerre smoothing factor.
/// </summary>
public decimal Gamma
{
get => _gamma.Value;
set => _gamma.Value = value;
}

/// <summary>
/// Overbought level.
/// </summary>
public decimal UpLevel
{
get => _upLevel.Value;
set => _upLevel.Value = value;
}

/// <summary>
/// Oversold level.
/// </summary>
public decimal DownLevel
{
get => _downLevel.Value;
set => _downLevel.Value = value;
}

/// <summary>
/// Candle type.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initializes a new instance of <see cref="LaguerreRocStrategy"/>.
/// </summary>
public LaguerreRocStrategy()
{
_period = Param(nameof(Period), 5)
.SetGreaterThanZero()
.SetDisplay("Period", "Rate of change lookback", "Indicators")
.SetCanOptimize(true);

_gamma = Param(nameof(Gamma), 0.5m)
.SetRange(0.1m, 0.9m)
.SetDisplay("Gamma", "Laguerre smoothing factor", "Indicators")
.SetCanOptimize(true);

_upLevel = Param(nameof(UpLevel), 0.75m)
.SetRange(0.1m, 0.9m)
.SetDisplay("Up Level", "Overbought threshold", "Indicators")
.SetCanOptimize(true);

_downLevel = Param(nameof(DownLevel), 0.25m)
.SetRange(0.1m, 0.9m)
.SetDisplay("Down Level", "Oversold threshold", "Indicators")
.SetCanOptimize(true);

_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
return [(Security, CandleType)];
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_roc = new RateOfChange { Length = Period };

var subscription = SubscribeCandles(CandleType);
subscription
 .Bind(_roc, ProcessCandle)
 .Start();

var area = CreateChartArea();
if (area != null)
{
 DrawCandles(area, subscription);
 DrawIndicator(area, _roc);
 DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal rocValue)
{
if (candle.State != CandleStates.Finished)
 return;

if (_roc == null || !_roc.IsFormed)
 return;

decimal l0, l1, l2, l3;

if (_isFirst)
{
 l0 = l1 = l2 = l3 = rocValue;
 _isFirst = false;
}
else
{
 l0 = (1 - Gamma) * rocValue + Gamma * _l0;
 l1 = -Gamma * l0 + _l0 + Gamma * _l1;
 l2 = -Gamma * l1 + _l1 + Gamma * _l2;
 l3 = -Gamma * l2 + _l2 + Gamma * _l3;
}

var cu = 0m;
var cd = 0m;

if (l0 >= l1) cu += l0 - l1; else cd += l1 - l0;
if (l1 >= l2) cu += l1 - l2; else cd += l2 - l1;
if (l2 >= l3) cu += l2 - l3; else cd += l3 - l2;

var denom = cu + cd;
var lroc = denom != 0m ? cu / denom : 0m;

int color = 2;
if (lroc > UpLevel) color = 4;
else if (lroc > 0.5m) color = 3;
if (lroc < DownLevel) color = 0;
else if (lroc < 0.5m) color = 1;

// Close positions based on color change
if (_prevColor > 2 && color <= 2 && Position < 0)
 BuyMarket(Math.Abs(Position));
if (_prevColor < 2 && color >= 2 && Position > 0)
 SellMarket(Math.Abs(Position));

// Open positions on transitions out of extreme zones
if (_prevColor == 4 && color < 4 && Position <= 0)
 BuyMarket();
if (_prevColor == 0 && color > 0 && Position >= 0)
 SellMarket();

_prevColor = color;

_l0 = l0;
_l1 = l1;
_l2 = l2;
_l3 = l3;
}

