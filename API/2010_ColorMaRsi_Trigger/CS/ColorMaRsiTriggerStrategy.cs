using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ColorMaRsi Trigger strategy.
/// Combines fast and slow EMA and RSI to generate trading signals.
/// </summary>
public class ColorMaRsiTriggerStrategy : Strategy
{
private readonly StrategyParam<int> _emaFastLength;
private readonly StrategyParam<int> _emaSlowLength;
private readonly StrategyParam<int> _rsiFastLength;
private readonly StrategyParam<int> _rsiSlowLength;
private readonly StrategyParam<DataType> _candleType;

private ExponentialMovingAverage _emaFast = null!;
private ExponentialMovingAverage _emaSlow = null!;
private RelativeStrengthIndex _rsiFast = null!;
private RelativeStrengthIndex _rsiSlow = null!;
private decimal _prevSignal;

/// <summary>
/// Fast EMA period.
/// </summary>
public int EmaFastLength { get => _emaFastLength.Value; set => _emaFastLength.Value = value; }

/// <summary>
/// Slow EMA period.
/// </summary>
public int EmaSlowLength { get => _emaSlowLength.Value; set => _emaSlowLength.Value = value; }

/// <summary>
/// Fast RSI period.
/// </summary>
public int RsiFastLength { get => _rsiFastLength.Value; set => _rsiFastLength.Value = value; }

/// <summary>
/// Slow RSI period.
/// </summary>
public int RsiSlowLength { get => _rsiSlowLength.Value; set => _rsiSlowLength.Value = value; }

/// <summary>
/// Candle type used for calculations.
/// </summary>
public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

/// <summary>
/// Initializes strategy parameters.
/// </summary>
public ColorMaRsiTriggerStrategy()
{
_emaFastLength = Param(nameof(EmaFastLength), 5)
.SetGreaterThanZero()
.SetDisplay("Fast EMA", "Fast EMA period", "General")
.SetCanOptimize(true)
.SetOptimize(3, 20, 1);

_emaSlowLength = Param(nameof(EmaSlowLength), 10)
.SetGreaterThanZero()
.SetDisplay("Slow EMA", "Slow EMA period", "General")
.SetCanOptimize(true)
.SetOptimize(5, 40, 1);

_rsiFastLength = Param(nameof(RsiFastLength), 3)
.SetGreaterThanZero()
.SetDisplay("Fast RSI", "Fast RSI period", "General")
.SetCanOptimize(true)
.SetOptimize(2, 20, 1);

_rsiSlowLength = Param(nameof(RsiSlowLength), 13)
.SetGreaterThanZero()
.SetDisplay("Slow RSI", "Slow RSI period", "General")
.SetCanOptimize(true)
.SetOptimize(5, 40, 1);

_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
.SetDisplay("Candle Type", "Type of candles", "General");
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
yield return (Security, CandleType);
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();
_prevSignal = 0;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_emaFast = new ExponentialMovingAverage { Length = EmaFastLength };
_emaSlow = new ExponentialMovingAverage { Length = EmaSlowLength };
_rsiFast = new RelativeStrengthIndex { Length = RsiFastLength };
_rsiSlow = new RelativeStrengthIndex { Length = RsiSlowLength };

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(_emaFast, _emaSlow, _rsiFast, _rsiSlow, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _emaFast);
DrawIndicator(area, _emaSlow);
DrawIndicator(area, _rsiFast);
DrawIndicator(area, _rsiSlow);
}
}

private void ProcessCandle(ICandleMessage candle, decimal emaFastValue, decimal emaSlowValue, decimal rsiFastValue, decimal rsiSlowValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!_emaFast.IsFormed || !_emaSlow.IsFormed || !_rsiFast.IsFormed || !_rsiSlow.IsFormed)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var signal = 0m;
if (emaFastValue > emaSlowValue)
signal += 1;
if (emaFastValue < emaSlowValue)
signal -= 1;
if (rsiFastValue > rsiSlowValue)
signal += 1;
if (rsiFastValue < rsiSlowValue)
signal -= 1;

if (signal > 1)
signal = 1;
if (signal < -1)
signal = -1;

if (_prevSignal > 0)
{
if (Position < 0)
BuyMarket(Math.Abs(Position));

if (signal <= 0 && Position <= 0)
BuyMarket(Volume);
}
else if (_prevSignal < 0)
{
if (Position > 0)
SellMarket(Position);

if (signal >= 0 && Position >= 0)
SellMarket(Volume);
}

_prevSignal = signal;
}
}
