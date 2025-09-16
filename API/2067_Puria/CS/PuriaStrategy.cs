using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Puria strategy based on three moving averages and MACD filter.
/// Enters long when fast EMA is above two slow LWMAs and MACD is positive.
/// Enters short when fast EMA is below two slow LWMAs and MACD is negative.
/// Applies fixed take-profit and stop-loss and allows only one trade per direction.
/// </summary>
public class PuriaStrategy : Strategy
{
private readonly StrategyParam<decimal> _stopLoss;
private readonly StrategyParam<decimal> _takeProfit;
private readonly StrategyParam<int> _ma1Period;
private readonly StrategyParam<int> _ma2Period;
private readonly StrategyParam<int> _ma3Period;
private readonly StrategyParam<DataType> _candleType;

private LinearWeightedMovingAverage? _ma75;
private LinearWeightedMovingAverage? _ma85;
private ExponentialMovingAverage? _ma5;
private MovingAverageConvergenceDivergence? _macd;

private decimal _prevMa75;
private decimal _prevMa85;
private decimal _prevMa5;
private decimal _prevClose;
private decimal _prevMacd;
private bool _initialized;
private bool _canBuy;
private bool _canSell;

/// <summary>
/// Constructor.
/// </summary>
public PuriaStrategy()
{
_stopLoss = Param(nameof(StopLoss), 14m)
.SetGreaterThanZero()
.SetDisplay("Stop Loss", "Stop loss in price points", "General")
.SetCanOptimize();

_takeProfit = Param(nameof(TakeProfit), 15m)
.SetGreaterThanZero()
.SetDisplay("Take Profit", "Take profit in price points", "General")
.SetCanOptimize();

_ma1Period = Param(nameof(Ma1Period), 75)
.SetGreaterThanZero()
.SetDisplay("MA1 Period", "LWMA period for low price", "Moving Averages")
.SetCanOptimize();

_ma2Period = Param(nameof(Ma2Period), 85)
.SetGreaterThanZero()
.SetDisplay("MA2 Period", "Second LWMA period for low price", "Moving Averages")
.SetCanOptimize();

_ma3Period = Param(nameof(Ma3Period), 5)
.SetGreaterThanZero()
.SetDisplay("MA3 Period", "EMA period for close price", "Moving Averages")
.SetCanOptimize();

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type", "Timeframe for strategy", "General");
}

/// <summary>
/// Stop loss distance in price points.
/// </summary>
public decimal StopLoss
{
get => _stopLoss.Value;
set => _stopLoss.Value = value;
}

/// <summary>
/// Take profit distance in price points.
/// </summary>
public decimal TakeProfit
{
get => _takeProfit.Value;
set => _takeProfit.Value = value;
}

/// <summary>
/// Period of first LWMA based on low price.
/// </summary>
public int Ma1Period
{
get => _ma1Period.Value;
set => _ma1Period.Value = value;
}

/// <summary>
/// Period of second LWMA based on low price.
/// </summary>
public int Ma2Period
{
get => _ma2Period.Value;
set => _ma2Period.Value = value;
}

/// <summary>
/// Period of fast EMA based on close price.
/// </summary>
public int Ma3Period
{
get => _ma3Period.Value;
set => _ma3Period.Value = value;
}

/// <summary>
/// Candle type used for calculations.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
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

_initialized = false;
_prevMa75 = 0m;
_prevMa85 = 0m;
_prevMa5 = 0m;
_prevClose = 0m;
_prevMacd = 0m;
_canBuy = true;
_canSell = true;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

StartProtection(
takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
stopLoss: new Unit(StopLoss, UnitTypes.Absolute)
);

_ma75 = new LinearWeightedMovingAverage
{
Length = Ma1Period,
CandlePrice = CandlePrice.Low
};

_ma85 = new LinearWeightedMovingAverage
{
Length = Ma2Period,
CandlePrice = CandlePrice.Low
};

_ma5 = new ExponentialMovingAverage
{
Length = Ma3Period
};

_macd = new MovingAverageConvergenceDivergence
{
ShortMa = { Length = 15 },
LongMa = { Length = 26 }
};

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx(_ma75, _ma85, _ma5, _macd, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _ma75);
DrawIndicator(area, _ma85);
DrawIndicator(area, _ma5);
DrawIndicator(area, _macd);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal ma75Value, decimal ma85Value, decimal ma5Value, IIndicatorValue macdValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!_ma75!.IsFormed || !_ma85!.IsFormed || !_ma5!.IsFormed || !_macd!.IsFormed)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var macd = (MovingAverageConvergenceDivergenceValue)macdValue;
var macdLine = macd.Macd;

if (!_initialized)
{
_prevMa75 = ma75Value;
_prevMa85 = ma85Value;
_prevMa5 = ma5Value;
_prevClose = candle.ClosePrice;
_prevMacd = macdLine;
_initialized = true;
return;
}

var buySignal = _prevMa5 > _prevMa75 && _prevMa5 > _prevMa85 && _prevClose > _prevMa5 && _prevMacd > 0m;
var sellSignal = _prevMa5 < _prevMa75 && _prevMa5 < _prevMa85 && _prevClose < _prevMa5 && _prevMacd < 0m;

if (buySignal && Position <= 0 && _canBuy)
{
BuyMarket(Volume + Math.Abs(Position));
_canBuy = false;
_canSell = true;
}
else if (sellSignal && Position >= 0 && _canSell)
{
SellMarket(Volume + Math.Abs(Position));
_canSell = false;
_canBuy = true;
}

_prevMa75 = ma75Value;
_prevMa85 = ma85Value;
_prevMa5 = ma5Value;
_prevClose = candle.ClosePrice;
_prevMacd = macdLine;
}
}
