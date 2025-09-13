using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Four WMA strategy with optional take profit and stop loss.
/// </summary>
public class FourWmaTpSlStrategy : Strategy
{
public enum MaMode
{
Sma,
Ema,
Wma,
Vwma,
Rma
}

public enum AltExitMa
{
LongMa1,
LongMa2,
ShortMa1,
ShortMa2
}

private readonly StrategyParam<int> _longMa1Length;
private readonly StrategyParam<int> _longMa2Length;
private readonly StrategyParam<int> _shortMa1Length;
private readonly StrategyParam<int> _shortMa2Length;
private readonly StrategyParam<MaMode> _maType;
private readonly StrategyParam<bool> _enableTpSl;
private readonly StrategyParam<decimal> _takeProfitPercent;
private readonly StrategyParam<decimal> _stopLossPercent;
private readonly StrategyParam<Sides?> _direction;
private readonly StrategyParam<bool> _enableAltExit;
private readonly StrategyParam<AltExitMa> _altExitMa;
private readonly StrategyParam<DataType> _candleType;

private decimal? _prevLongMa1;
private decimal? _prevLongMa2;
private decimal? _prevShortMa1;
private decimal? _prevShortMa2;

/// <summary>
/// Length for first long moving average.
/// </summary>
public int LongMa1Length { get => _longMa1Length.Value; set => _longMa1Length.Value = value; }

/// <summary>
/// Length for second long moving average.
/// </summary>
public int LongMa2Length { get => _longMa2Length.Value; set => _longMa2Length.Value = value; }

/// <summary>
/// Length for first short moving average.
/// </summary>
public int ShortMa1Length { get => _shortMa1Length.Value; set => _shortMa1Length.Value = value; }

/// <summary>
/// Length for second short moving average.
/// </summary>
public int ShortMa2Length { get => _shortMa2Length.Value; set => _shortMa2Length.Value = value; }

/// <summary>
/// Type of moving average.
/// </summary>
public MaMode MaType { get => _maType.Value; set => _maType.Value = value; }

/// <summary>
/// Enable take profit and stop loss protection.
/// </summary>
public bool EnableTpSl { get => _enableTpSl.Value; set => _enableTpSl.Value = value; }

/// <summary>
/// Take profit percentage.
/// </summary>
public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

/// <summary>
/// Stop loss percentage.
/// </summary>
public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

/// <summary>
/// Allowed trade direction.
/// </summary>
public Sides? Direction { get => _direction.Value; set => _direction.Value = value; }

/// <summary>
/// Enable alternate exit condition.
/// </summary>
public bool EnableAltExit { get => _enableAltExit.Value; set => _enableAltExit.Value = value; }

/// <summary>
/// Moving average used for alternate exit.
/// </summary>
public AltExitMa AltExitMaOption { get => _altExitMa.Value; set => _altExitMa.Value = value; }

/// <summary>
/// Candle type.
/// </summary>
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

/// <summary>
/// Initializes a new instance of the <see cref="FourWmaTpSlStrategy"/> class.
/// </summary>
public FourWmaTpSlStrategy()
{
_longMa1Length = Param(nameof(LongMa1Length), 10)
.SetGreaterThanZero()
.SetDisplay("Long MA1", "Length for first long MA", "Indicators")
.SetCanOptimize(true);

_longMa2Length = Param(nameof(LongMa2Length), 20)
.SetGreaterThanZero()
.SetDisplay("Long MA2", "Length for second long MA", "Indicators")
.SetCanOptimize(true);

_shortMa1Length = Param(nameof(ShortMa1Length), 30)
.SetGreaterThanZero()
.SetDisplay("Short MA1", "Length for first short MA", "Indicators")
.SetCanOptimize(true);

_shortMa2Length = Param(nameof(ShortMa2Length), 40)
.SetGreaterThanZero()
.SetDisplay("Short MA2", "Length for second short MA", "Indicators")
.SetCanOptimize(true);

_maType = Param(nameof(MaType), MaMode.Wma)
.SetDisplay("MA Type", "Type of moving average", "Indicators");

_enableTpSl = Param(nameof(EnableTpSl), true)
.SetDisplay("Enable TP/SL", "Use take profit and stop loss", "Risk");

_takeProfitPercent = Param(nameof(TakeProfitPercent), 1m)
.SetGreaterOrEqualZero()
.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

_stopLossPercent = Param(nameof(StopLossPercent), 1m)
.SetGreaterOrEqualZero()
.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

_direction = Param(nameof(Direction), (Sides?)null)
.SetDisplay("Direction", "Allowed trade direction", "General");

_enableAltExit = Param(nameof(EnableAltExit), false)
.SetDisplay("Enable Alt Exit", "Enable alternate exit", "Risk");

_altExitMa = Param(nameof(AltExitMaOption), AltExitMa.LongMa1)
.SetDisplay("Alt Exit MA", "MA used for alternate exit", "Risk");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
_prevLongMa1 = _prevLongMa2 = _prevShortMa1 = _prevShortMa2 = null;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var longMa1 = CreateMa(MaType, LongMa1Length);
var longMa2 = CreateMa(MaType, LongMa2Length);
var shortMa1 = CreateMa(MaType, ShortMa1Length);
var shortMa2 = CreateMa(MaType, ShortMa2Length);

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(longMa1, longMa2, shortMa1, shortMa2, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, longMa1);
DrawIndicator(area, longMa2);
DrawIndicator(area, shortMa1);
DrawIndicator(area, shortMa2);
DrawOwnTrades(area);
}

if (EnableTpSl)
{
StartProtection(
takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
stopLoss: new Unit(StopLossPercent, UnitTypes.Percent));
}
}

private static LengthIndicator<decimal> CreateMa(MaMode type, int length)
{
return type switch
{
MaMode.Sma => new SimpleMovingAverage { Length = length },
MaMode.Ema => new ExponentialMovingAverage { Length = length },
MaMode.Wma => new WeightedMovingAverage { Length = length },
MaMode.Vwma => new VolumeWeightedMovingAverage { Length = length },
MaMode.Rma => new SmoothedMovingAverage { Length = length },
_ => throw new ArgumentOutOfRangeException(nameof(type))
};
}

private void ProcessCandle(ICandleMessage candle, decimal longMa1, decimal longMa2, decimal shortMa1, decimal shortMa2)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var priceCrossAltExit = false;
if (EnableAltExit)
{
decimal altValue;
switch (AltExitMaOption)
{
case AltExitMa.LongMa1:
altValue = longMa1;
break;
case AltExitMa.LongMa2:
altValue = longMa2;
break;
case AltExitMa.ShortMa1:
altValue = shortMa1;
break;
case AltExitMa.ShortMa2:
altValue = shortMa2;
break;
default:
altValue = longMa1;
break;
}

priceCrossAltExit = candle.ClosePrice < altValue;
}

if (_prevLongMa1.HasValue && _prevLongMa2.HasValue)
{
var longCrossUp = _prevLongMa1 <= _prevLongMa2 && longMa1 > longMa2;
var longCrossDown = _prevLongMa1 >= _prevLongMa2 && longMa1 < longMa2;

if ((Direction is null or Sides.Buy) && Position <= 0 && longCrossUp)
BuyMarket();

if (Position > 0 && (longCrossDown || priceCrossAltExit))
ClosePosition();
}

if (_prevShortMa1.HasValue && _prevShortMa2.HasValue)
{
var shortCrossDown = _prevShortMa1 >= _prevShortMa2 && shortMa1 < shortMa2;
var shortCrossUp = _prevShortMa1 <= _prevShortMa2 && shortMa1 > shortMa2;

if ((Direction is null or Sides.Sell) && Position >= 0 && shortCrossDown)
SellMarket();

if (Position < 0 && (shortCrossUp || priceCrossAltExit))
ClosePosition();
}

_prevLongMa1 = longMa1;
_prevLongMa2 = longMa2;
_prevShortMa1 = shortMa1;
_prevShortMa2 = shortMa2;
}
}

