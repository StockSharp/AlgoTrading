using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Weighted Ichimoku strategy using score from Tenkan/Kijun cross and Kumo breakout.
/// Opens long when score exceeds the buy threshold and closes when score drops below the sell threshold.
/// </summary>
public class WeightedIchimokuStrategy : Strategy
{
private readonly StrategyParam<int> _tenkanPeriod;
private readonly StrategyParam<int> _kijunPeriod;
private readonly StrategyParam<int> _senkouSpanBPeriod;
private readonly StrategyParam<int> _offset;
private readonly StrategyParam<decimal> _buyThreshold;
private readonly StrategyParam<bool> _useSellThreshold;
private readonly StrategyParam<decimal> _sellThreshold;
private readonly StrategyParam<DataType> _candleType;

/// <summary>
/// Tenkan-sen period.
/// </summary>
public int TenkanPeriod
{
get => _tenkanPeriod.Value;
set => _tenkanPeriod.Value = value;
}

/// <summary>
/// Kijun-sen period.
/// </summary>
public int KijunPeriod
{
get => _kijunPeriod.Value;
set => _kijunPeriod.Value = value;
}

/// <summary>
/// Senkou Span B period.
/// </summary>
public int SenkouSpanBPeriod
{
get => _senkouSpanBPeriod.Value;
set => _senkouSpanBPeriod.Value = value;
}

/// <summary>
/// Offset for leading spans.
/// </summary>
public int Offset
{
get => _offset.Value;
set => _offset.Value = value;
}

/// <summary>
/// Buy threshold for combined score.
/// </summary>
public decimal BuyThreshold
{
get => _buyThreshold.Value;
set => _buyThreshold.Value = value;
}

/// <summary>
/// Use sell threshold instead of crossing zero.
/// </summary>
public bool UseSellThreshold
{
get => _useSellThreshold.Value;
set => _useSellThreshold.Value = value;
}

/// <summary>
/// Sell threshold for combined score.
/// </summary>
public decimal SellThreshold
{
get => _sellThreshold.Value;
set => _sellThreshold.Value = value;
}

/// <summary>
/// Candle type to process.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initializes a new instance of <see cref="WeightedIchimokuStrategy"/>.
/// </summary>
public WeightedIchimokuStrategy()
{
_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
.SetGreaterThanZero()
.SetDisplay("Tenkan Period", "Tenkan length", "Ichimoku")
.SetCanOptimize(true);

_kijunPeriod = Param(nameof(KijunPeriod), 26)
.SetGreaterThanZero()
.SetDisplay("Kijun Period", "Kijun length", "Ichimoku")
.SetCanOptimize(true);

_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
.SetGreaterThanZero()
.SetDisplay("Senkou Span B Period", "Span B length", "Ichimoku")
.SetCanOptimize(true);

_offset = Param(nameof(Offset), 26)
.SetGreaterThanZero()
.SetDisplay("Offset", "Leading span offset", "Ichimoku")
.SetCanOptimize(true);

_buyThreshold = Param(nameof(BuyThreshold), 60m)
.SetDisplay("Buy Threshold", "Score to enter long", "General")
.SetCanOptimize(true);

_useSellThreshold = Param(nameof(UseSellThreshold), true)
.SetDisplay("Use Sell Threshold", "Enable sell threshold", "General");

_sellThreshold = Param(nameof(SellThreshold), -49m)
.SetDisplay("Sell Threshold", "Score to exit", "General")
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
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var ichimoku = new Ichimoku
{
Tenkan = { Length = TenkanPeriod },
Kijun = { Length = KijunPeriod },
SenkouB = { Length = SenkouSpanBPeriod }
};

var subscription = SubscribeCandles(CandleType);
subscription.BindEx(ichimoku, ProcessCandle).Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, ichimoku);
DrawOwnTrades(area);
}

StartProtection();
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
{
if (candle.State != CandleStates.Finished)
return;

var ichimokuValue = (IchimokuValue)value;

if (ichimokuValue.Tenkan is not decimal tenkan ||
ichimokuValue.Kijun is not decimal kijun ||
ichimokuValue.SenkouA is not decimal senkouA ||
ichimokuValue.SenkouB is not decimal senkouB)
{
return;
}

var cloudTop = Math.Max(senkouA, senkouB);
var cloudBottom = Math.Min(senkouA, senkouB);

decimal score = 0m;
score += tenkan > kijun ? 25m : tenkan < kijun ? -25m : 0m;
score += candle.ClosePrice > cloudTop ? 30m : candle.ClosePrice < cloudBottom ? -30m : 0m;

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (score >= BuyThreshold && Position <= 0)
{
BuyMarket();
}
else if (Position > 0)
{
var exitScore = UseSellThreshold ? score <= SellThreshold : score <= 0m;
if (exitScore)
SellMarket(Position);
}
}
}
