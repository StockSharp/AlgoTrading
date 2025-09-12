using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified Trend Vanguard strategy.
/// Uses Donchian-style ZigZag to detect trend reversals.
/// </summary>
public class TrendVanguardStrategy : Strategy
{
private readonly StrategyParam<int> _depth;
private readonly StrategyParam<DataType> _candleType;

private decimal _prevUpper;
private decimal _prevLower;
private decimal _prevZigzag;
private decimal _prev2Zigzag;
private decimal _zigzag;
private decimal _val;
private int _osc;
private int _prevOsc;

/// <summary>
/// ZigZag depth.
/// </summary>
public int Depth
{
get => _depth.Value;
set => _depth.Value = value;
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
/// Initializes a new instance of <see cref="TrendVanguardStrategy"/>.
/// </summary>
public TrendVanguardStrategy()
{
_depth = Param(nameof(Depth), 21)
.SetDisplay("ZigZag Depth", "Lookback for highs/lows", "Indicators")
.SetCanOptimize(true);

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
_prevUpper = default;
_prevLower = default;
_prevZigzag = default;
_prev2Zigzag = default;
_zigzag = default;
_val = default;
_osc = default;
_prevOsc = default;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var highest = new Highest { Length = Depth };
var lowest = new Lowest { Length = Depth };

var subscription = SubscribeCandles(CandleType);
subscription.Bind(highest, lowest, ProcessCandle).Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, highest);
DrawIndicator(area, lowest);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal upper, decimal lower)
{
if (candle.State != CandleStates.Finished)
return;

if (_prevZigzag == 0m)
{
_prevZigzag = candle.ClosePrice;
_prevUpper = upper;
_prevLower = lower;
_val = upper - lower;
return;
}

var crossUpper = false;
var crossLower = false;

if (_prevUpper != 0m && _prevLower != 0m && _prev2Zigzag != 0m)
{
crossUpper = Cross(_prev2Zigzag, _prevZigzag, _prevUpper, upper);
crossLower = Cross(_prev2Zigzag, _prevZigzag, _prevLower, lower);
}

_prevOsc = _osc;
if (crossUpper)
_osc = -1;
else if (crossLower)
_osc = 1;

if (_osc != _prevOsc)
_val = upper - lower;

var prevZig = _prevZigzag;
_zigzag = prevZig + _osc * _val / Depth;

if (_osc != _prevOsc && IsFormedAndOnlineAndAllowTrading())
{
if (_osc == 1 && Position <= 0)
BuyMarket();
else if (_osc == -1 && Position >= 0)
SellMarket();
}

_prev2Zigzag = _prevZigzag;
_prevZigzag = _zigzag;
_prevUpper = upper;
_prevLower = lower;
}

private static bool Cross(decimal prevX, decimal currX, decimal prevY, decimal currY)
{
return (prevX < prevY && currX > currY) || (prevX > prevY && currX < currY);
}
}
