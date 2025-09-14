namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on MACD turning points and zero line breakdowns.
/// Four modes define how signals are generated.
/// </summary>
public class ColorRsiMacdStrategy : Strategy
{
public enum AlgMode
{
Breakdown,
MacdTwist,
SignalTwist,
MacdDisposition
}

private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<int> _fastPeriod;
private readonly StrategyParam<int> _slowPeriod;
private readonly StrategyParam<int> _signalPeriod;
private readonly StrategyParam<AlgMode> _mode;
private readonly StrategyParam<bool> _buyOpen;
private readonly StrategyParam<bool> _sellOpen;
private readonly StrategyParam<bool> _buyClose;
private readonly StrategyParam<bool> _sellClose;

private decimal? _histPrev;
private decimal? _macdPrev;
private decimal? _macdPrev2;
private decimal? _signalPrev;
private decimal? _signalPrev2;

/// <summary>
/// Data type for candles.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Fast EMA period for MACD.
/// </summary>
public int FastPeriod
{
get => _fastPeriod.Value;
set => _fastPeriod.Value = value;
}

/// <summary>
/// Slow EMA period for MACD.
/// </summary>
public int SlowPeriod
{
get => _slowPeriod.Value;
set => _slowPeriod.Value = value;
}

/// <summary>
/// Signal line period for MACD.
/// </summary>
public int SignalPeriod
{
get => _signalPeriod.Value;
set => _signalPeriod.Value = value;
}

/// <summary>
/// Trading logic mode.
/// </summary>
public AlgMode Mode
{
get => _mode.Value;
set => _mode.Value = value;
}

/// <summary>
/// Allow opening long positions.
/// </summary>
public bool BuyOpen
{
get => _buyOpen.Value;
set => _buyOpen.Value = value;
}

/// <summary>
/// Allow opening short positions.
/// </summary>
public bool SellOpen
{
get => _sellOpen.Value;
set => _sellOpen.Value = value;
}

/// <summary>
/// Allow closing long positions.
/// </summary>
public bool BuyClose
{
get => _buyClose.Value;
set => _buyClose.Value = value;
}

/// <summary>
/// Allow closing short positions.
/// </summary>
public bool SellClose
{
get => _sellClose.Value;
set => _sellClose.Value = value;
}

/// <summary>
/// Initializes a new instance of the <see cref="ColorRsiMacdStrategy"/>.
/// </summary>
public ColorRsiMacdStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");

_fastPeriod = Param(nameof(FastPeriod), 12)
.SetRange(5, 50)
.SetDisplay("Fast Period", "Fast EMA period", "MACD")
.SetCanOptimize(true);

_slowPeriod = Param(nameof(SlowPeriod), 26)
.SetRange(10, 100)
.SetDisplay("Slow Period", "Slow EMA period", "MACD")
.SetCanOptimize(true);

_signalPeriod = Param(nameof(SignalPeriod), 9)
.SetRange(3, 30)
.SetDisplay("Signal Period", "Signal line period", "MACD")
.SetCanOptimize(true);

_mode = Param(nameof(Mode), AlgMode.MacdDisposition)
.SetDisplay("Mode", "Algorithm mode", "Logic");

_buyOpen = Param(nameof(BuyOpen), true)
.SetDisplay("Buy Open", "Allow opening long positions", "Permissions");

_sellOpen = Param(nameof(SellOpen), true)
.SetDisplay("Sell Open", "Allow opening short positions", "Permissions");

_buyClose = Param(nameof(BuyClose), true)
.SetDisplay("Buy Close", "Allow closing long positions", "Permissions");

_sellClose = Param(nameof(SellClose), true)
.SetDisplay("Sell Close", "Allow closing short positions", "Permissions");
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

StartProtection();

var macd = new MovingAverageConvergenceDivergenceSignal
{
Macd =
{
ShortMa = { Length = FastPeriod },
LongMa = { Length = SlowPeriod }
},
SignalMa = { Length = SignalPeriod }
};

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx(macd, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
var macdArea = CreateChartArea();
DrawIndicator(macdArea, macd);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
{
if (candle.State != CandleStates.Finished)
return;

var data = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

if (data.Macd is not decimal macdLine || data.Signal is not decimal signalLine)
return;

var hist = macdLine - signalLine;

switch (Mode)
{
case AlgMode.Breakdown:
if (_histPrev is decimal prevHist)
{
if (prevHist > 0m && hist <= 0m)
{
if (SellClose && Position < 0)
BuyMarket(-Position);

if (BuyOpen && Position <= 0)
BuyMarket(Volume);
}
else if (prevHist < 0m && hist >= 0m)
{
if (BuyClose && Position > 0)
SellMarket(Position);

if (SellOpen && Position >= 0)
SellMarket(Volume);
}
}
_histPrev = hist;
break;

case AlgMode.MacdTwist:
if (_macdPrev2 is decimal m2 && _macdPrev is decimal m1)
{
if (m1 < m2 && macdLine > m1)
{
if (SellClose && Position < 0)
BuyMarket(-Position);

if (BuyOpen && Position <= 0)
BuyMarket(Volume);
}
else if (m1 > m2 && macdLine < m1)
{
if (BuyClose && Position > 0)
SellMarket(Position);

if (SellOpen && Position >= 0)
SellMarket(Volume);
}
}
_macdPrev2 = _macdPrev;
_macdPrev = macdLine;
break;

case AlgMode.SignalTwist:
if (_signalPrev2 is decimal s2 && _signalPrev is decimal s1)
{
if (s1 < s2 && signalLine > s1)
{
if (SellClose && Position < 0)
BuyMarket(-Position);

if (BuyOpen && Position <= 0)
BuyMarket(Volume);
}
else if (s1 > s2 && signalLine < s1)
{
if (BuyClose && Position > 0)
SellMarket(Position);

if (SellOpen && Position >= 0)
SellMarket(Volume);
}
}
_signalPrev2 = _signalPrev;
_signalPrev = signalLine;
break;

case AlgMode.MacdDisposition:
if (_macdPrev is decimal mp && _signalPrev is decimal sp)
{
if (mp > sp && macdLine <= signalLine)
{
if (SellClose && Position < 0)
BuyMarket(-Position);

if (BuyOpen && Position <= 0)
BuyMarket(Volume);
}
else if (mp < sp && macdLine >= signalLine)
{
if (BuyClose && Position > 0)
SellMarket(Position);

if (SellOpen && Position >= 0)
SellMarket(Volume);
}
}
_macdPrev = macdLine;
_signalPrev = signalLine;
break;
}
}
}
