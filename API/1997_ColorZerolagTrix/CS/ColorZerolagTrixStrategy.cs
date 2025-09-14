using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Color Zerolag TRIX Strategy.
/// Combines five TRIX indicators with different periods and weights.
/// </summary>
public class ColorZerolagTrixStrategy : Strategy
{
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<int> _smoothing;
private readonly StrategyParam<decimal> _factor1;
private readonly StrategyParam<int> _period1;
private readonly StrategyParam<decimal> _factor2;
private readonly StrategyParam<int> _period2;
private readonly StrategyParam<decimal> _factor3;
private readonly StrategyParam<int> _period3;
private readonly StrategyParam<decimal> _factor4;
private readonly StrategyParam<int> _period4;
private readonly StrategyParam<decimal> _factor5;
private readonly StrategyParam<int> _period5;
private readonly StrategyParam<bool> _buyPosOpen;
private readonly StrategyParam<bool> _sellPosOpen;
private readonly StrategyParam<bool> _buyPosClose;
private readonly StrategyParam<bool> _sellPosClose;

private Trix _trix1;
private Trix _trix2;
private Trix _trix3;
private Trix _trix4;
private Trix _trix5;

private decimal _slowLine;
private decimal _prevFast;
private decimal _prevSlow;
private bool _hasPrev;

/// <summary>
/// Candle type for strategy.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Smoothing factor for slow line.
/// </summary>
public int Smoothing
{
get => _smoothing.Value;
set => _smoothing.Value = value;
}

/// <summary>
/// Weight for first TRIX.
/// </summary>
public decimal Factor1
{
get => _factor1.Value;
set => _factor1.Value = value;
}

/// <summary>
/// Period for first TRIX.
/// </summary>
public int TrixPeriod1
{
get => _period1.Value;
set => _period1.Value = value;
}

/// <summary>
/// Weight for second TRIX.
/// </summary>
public decimal Factor2
{
get => _factor2.Value;
set => _factor2.Value = value;
}

/// <summary>
/// Period for second TRIX.
/// </summary>
public int TrixPeriod2
{
get => _period2.Value;
set => _period2.Value = value;
}

/// <summary>
/// Weight for third TRIX.
/// </summary>
public decimal Factor3
{
get => _factor3.Value;
set => _factor3.Value = value;
}

/// <summary>
/// Period for third TRIX.
/// </summary>
public int TrixPeriod3
{
get => _period3.Value;
set => _period3.Value = value;
}

/// <summary>
/// Weight for fourth TRIX.
/// </summary>
public decimal Factor4
{
get => _factor4.Value;
set => _factor4.Value = value;
}

/// <summary>
/// Period for fourth TRIX.
/// </summary>
public int TrixPeriod4
{
get => _period4.Value;
set => _period4.Value = value;
}

/// <summary>
/// Weight for fifth TRIX.
/// </summary>
public decimal Factor5
{
get => _factor5.Value;
set => _factor5.Value = value;
}

/// <summary>
/// Period for fifth TRIX.
/// </summary>
public int TrixPeriod5
{
get => _period5.Value;
set => _period5.Value = value;
}

/// <summary>
/// Allow opening of long positions.
/// </summary>
public bool BuyPosOpen
{
get => _buyPosOpen.Value;
set => _buyPosOpen.Value = value;
}

/// <summary>
/// Allow opening of short positions.
/// </summary>
public bool SellPosOpen
{
get => _sellPosOpen.Value;
set => _sellPosOpen.Value = value;
}

/// <summary>
/// Allow closing of long positions.
/// </summary>
public bool BuyPosClose
{
get => _buyPosClose.Value;
set => _buyPosClose.Value = value;
}

/// <summary>
/// Allow closing of short positions.
/// </summary>
public bool SellPosClose
{
get => _sellPosClose.Value;
set => _sellPosClose.Value = value;
}

/// <summary>
/// Initializes a new instance of <see cref="ColorZerolagTrixStrategy"/>.
/// </summary>
public ColorZerolagTrixStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");

_smoothing = Param(nameof(Smoothing), 15)
.SetDisplay("Smoothing", "Smoothing factor", "Indicator")
.SetCanOptimize(true);

_factor1 = Param(nameof(Factor1), 0.05m)
.SetDisplay("Factor 1", "Weight for first TRIX", "Indicator")
.SetCanOptimize(true);

_period1 = Param(nameof(TrixPeriod1), 8)
.SetDisplay("TRIX Period 1", "Period for first TRIX", "Indicator")
.SetCanOptimize(true);

_factor2 = Param(nameof(Factor2), 0.10m)
.SetDisplay("Factor 2", "Weight for second TRIX", "Indicator")
.SetCanOptimize(true);

_period2 = Param(nameof(TrixPeriod2), 21)
.SetDisplay("TRIX Period 2", "Period for second TRIX", "Indicator")
.SetCanOptimize(true);

_factor3 = Param(nameof(Factor3), 0.16m)
.SetDisplay("Factor 3", "Weight for third TRIX", "Indicator")
.SetCanOptimize(true);

_period3 = Param(nameof(TrixPeriod3), 34)
.SetDisplay("TRIX Period 3", "Period for third TRIX", "Indicator")
.SetCanOptimize(true);

_factor4 = Param(nameof(Factor4), 0.26m)
.SetDisplay("Factor 4", "Weight for fourth TRIX", "Indicator")
.SetCanOptimize(true);

_period4 = Param(nameof(TrixPeriod4), 55)
.SetDisplay("TRIX Period 4", "Period for fourth TRIX", "Indicator")
.SetCanOptimize(true);

_factor5 = Param(nameof(Factor5), 0.43m)
.SetDisplay("Factor 5", "Weight for fifth TRIX", "Indicator")
.SetCanOptimize(true);

_period5 = Param(nameof(TrixPeriod5), 89)
.SetDisplay("TRIX Period 5", "Period for fifth TRIX", "Indicator")
.SetCanOptimize(true);

_buyPosOpen = Param(nameof(BuyPosOpen), true)
.SetDisplay("Buy Position Open", "Allow opening long positions", "Trade");

_sellPosOpen = Param(nameof(SellPosOpen), true)
.SetDisplay("Sell Position Open", "Allow opening short positions", "Trade");

_buyPosClose = Param(nameof(BuyPosClose), true)
.SetDisplay("Buy Position Close", "Allow closing long positions", "Trade");

_sellPosClose = Param(nameof(SellPosClose), true)
.SetDisplay("Sell Position Close", "Allow closing short positions", "Trade");
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

StartProtection();

_trix1 = new Trix { Length = TrixPeriod1 };
_trix2 = new Trix { Length = TrixPeriod2 };
_trix3 = new Trix { Length = TrixPeriod3 };
_trix4 = new Trix { Length = TrixPeriod4 };
_trix5 = new Trix { Length = TrixPeriod5 };

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(_trix1, _trix2, _trix3, _trix4, _trix5, Process)
.Start();
}

private void Process(ICandleMessage candle, decimal trix1, decimal trix2, decimal trix3, decimal trix4, decimal trix5)
{
if (candle.State != CandleStates.Finished)
return;

var fast = Factor1 * trix1 + Factor2 * trix2 + Factor3 * trix3 + Factor4 * trix4 + Factor5 * trix5;
var smoothConst = (Smoothing - 1m) / Smoothing;
_slowLine = fast / Smoothing + _slowLine * smoothConst;

if (!_hasPrev)
{
_prevFast = fast;
_prevSlow = _slowLine;
_hasPrev = true;
return;
}

var crossDown = _prevFast > _prevSlow && fast < _slowLine;
var crossUp = _prevFast < _prevSlow && fast > _slowLine;

if (crossDown)
{
if ((Position < 0 && SellPosClose) || (Position <= 0 && BuyPosOpen))
BuyMarket();
}
else if (crossUp)
{
if ((Position > 0 && BuyPosClose) || (Position >= 0 && SellPosOpen))
SellMarket();
}

_prevFast = fast;
_prevSlow = _slowLine;
}
}

