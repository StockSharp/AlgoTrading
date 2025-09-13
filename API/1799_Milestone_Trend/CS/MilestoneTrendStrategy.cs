using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Milestone trend strategy.
/// Uses smoothed moving averages to detect trend reversals.
/// Filters entries by ATR and candle spike.
/// </summary>
public class MilestoneTrendStrategy : Strategy
{
private readonly StrategyParam<int> _slowMaPeriod;
private readonly StrategyParam<int> _fastMaPeriod;
private readonly StrategyParam<int> _atrPeriod;
private readonly StrategyParam<decimal> _minTrend;
private readonly StrategyParam<decimal> _maxTrend;
private readonly StrategyParam<decimal> _minRange;
private readonly StrategyParam<decimal> _candleSpike;
private readonly StrategyParam<DataType> _candleType;

private decimal _prevSlow;
private ICandleMessage _prevCandle;

/// <summary>
/// Slow MA period.
/// </summary>
public int SlowMaPeriod
{
get => _slowMaPeriod.Value;
set => _slowMaPeriod.Value = value;
}

/// <summary>
/// Fast MA period.
/// </summary>
public int FastMaPeriod
{
get => _fastMaPeriod.Value;
set => _fastMaPeriod.Value = value;
}

/// <summary>
/// ATR period.
/// </summary>
public int AtrPeriod
{
get => _atrPeriod.Value;
set => _atrPeriod.Value = value;
}

/// <summary>
/// Minimum trend strength.
/// </summary>
public decimal MinTrend
{
get => _minTrend.Value;
set => _minTrend.Value = value;
}

/// <summary>
/// Maximum trend strength.
/// </summary>
public decimal MaxTrend
{
get => _maxTrend.Value;
set => _maxTrend.Value = value;
}

/// <summary>
/// Minimum ATR value to allow trading.
/// </summary>
public decimal MinRange
{
get => _minRange.Value;
set => _minRange.Value = value;
}

/// <summary>
/// Maximum candle body size to avoid spikes.
/// </summary>
public decimal CandleSpike
{
get => _candleSpike.Value;
set => _candleSpike.Value = value;
}

/// <summary>
/// Candle type used by the strategy.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Constructor.
/// </summary>
public MilestoneTrendStrategy()
{
_slowMaPeriod = Param(nameof(SlowMaPeriod), 120)
.SetGreaterThanZero()
.SetCanOptimize()
.SetDisplay("Slow MA Period");

_fastMaPeriod = Param(nameof(FastMaPeriod), 30)
.SetGreaterThanZero()
.SetCanOptimize()
.SetDisplay("Fast MA Period");

_atrPeriod = Param(nameof(AtrPeriod), 14)
.SetGreaterThanZero()
.SetCanOptimize()
.SetDisplay("ATR Period");

_minTrend = Param(nameof(MinTrend), 10m)
.SetCanOptimize()
.SetDisplay("Minimum Trend");

_maxTrend = Param(nameof(MaxTrend), 100m)
.SetCanOptimize()
.SetDisplay("Maximum Trend");

_minRange = Param(nameof(MinRange), 5m)
.SetCanOptimize()
.SetDisplay("Minimum ATR");

_candleSpike = Param(nameof(CandleSpike), 10m)
.SetCanOptimize()
.SetDisplay("Maximum Candle Body");

_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
.SetDisplay("Candle Type");
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
_prevSlow = 0m;
_prevCandle = null;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

StartProtection();

var slowMa = new SMA { Length = SlowMaPeriod };
var fastMa = new SMA { Length = FastMaPeriod };
var atr = new ATR { Length = AtrPeriod };

var subscription = SubscribeCandles(CandleType);

subscription
.Bind(slowMa, fastMa, atr, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, slowMa);
DrawIndicator(area, fastMa);
DrawIndicator(area, atr);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal slowValue, decimal fastValue, decimal atrValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

var trendStrength = slowValue - _prevSlow;

// Skip trading if candle body indicates a spike.
var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
if (body > CandleSpike)
{
_prevSlow = slowValue;
_prevCandle = candle;
return;
}

// Skip trading if ATR is below threshold.
if (atrValue < MinRange)
{
_prevSlow = slowValue;
_prevCandle = candle;
return;
}

bool bullish = false;
bool bearish = false;

if (_prevCandle != null)
{
if (trendStrength < -MinTrend && trendStrength > -MaxTrend && fastValue > slowValue && candle.ClosePrice > candle.OpenPrice && candle.ClosePrice > _prevCandle.HighPrice)
bullish = true;
else if (trendStrength > MinTrend && trendStrength < MaxTrend && fastValue < slowValue && candle.ClosePrice < candle.OpenPrice && candle.ClosePrice < _prevCandle.LowPrice)
bearish = true;
}

if (bullish && Position <= 0)
{
var volume = Volume + Math.Abs(Position);
BuyMarket(volume);
}
else if (bearish && Position >= 0)
{
var volume = Volume + Math.Abs(Position);
SellMarket(volume);
}

_prevSlow = slowValue;
_prevCandle = candle;
}
}

