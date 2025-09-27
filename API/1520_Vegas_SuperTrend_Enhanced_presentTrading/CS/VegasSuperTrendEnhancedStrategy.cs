using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SuperTrend adjusted by Vegas channel.
/// Opens trades when trend direction flips.
/// </summary>
public class VegasSuperTrendEnhancedStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _vegasWindow;
	private readonly StrategyParam<decimal> _superTrendMultiplier;
	private readonly StrategyParam<decimal> _volatilityAdjustment;
	private readonly StrategyParam<Sides?> _tradeDirection;

	private SimpleMovingAverage _vegasMa = null!;
	private StandardDeviation _vegasStd = null!;
	private AverageTrueRange _atr = null!;

	private decimal? _prevUpper;
	private decimal? _prevLower;
	private int _trend = 1;

/// <summary>
/// Candle type to process.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
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
/// Vegas moving average window.
/// </summary>
public int VegasWindow
{
get => _vegasWindow.Value;
set => _vegasWindow.Value = value;
}

/// <summary>
/// Base SuperTrend multiplier.
/// </summary>
public decimal SuperTrendMultiplier
{
get => _superTrendMultiplier.Value;
set => _superTrendMultiplier.Value = value;
}

/// <summary>
/// Adjustment factor based on Vegas channel width.
/// </summary>
public decimal VolatilityAdjustment
{
get => _volatilityAdjustment.Value;
set => _volatilityAdjustment.Value = value;
}

/// <summary>
/// Allowed trade direction (Long, Short, Both).
/// </summary>
	public Sides? TradeDirection
	{
	    get => _tradeDirection.Value;
	    set => _tradeDirection.Value = value;
	}

/// <summary>
/// Initializes a new instance of <see cref="VegasSuperTrendEnhancedStrategy"/>.
/// </summary>
public VegasSuperTrendEnhancedStrategy()
{
	    _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
	        .SetDisplay("Candle Type", "Timeframe", "General");

	    _atrPeriod = Param(nameof(AtrPeriod), 10)
	        .SetGreaterThanZero()
	        .SetDisplay("ATR Period", "ATR length", "General")
	        .SetCanOptimize(true);

	    _vegasWindow = Param(nameof(VegasWindow), 100)
	        .SetGreaterThanZero()
	        .SetDisplay("Vegas Window", "Vegas moving average length", "General")
	        .SetCanOptimize(true);

	    _superTrendMultiplier = Param(nameof(SuperTrendMultiplier), 5m)
	        .SetGreaterThanZero()
	        .SetDisplay("Base Multiplier", "SuperTrend base multiplier", "General")
	        .SetCanOptimize(true);

	    _volatilityAdjustment = Param(nameof(VolatilityAdjustment), 5m)
	        .SetDisplay("Volatility Adjustment", "Multiplier adjustment factor", "General")
	        .SetCanOptimize(true);

	    _tradeDirection = Param(nameof(TradeDirection), (Sides?)null)
	        .SetDisplay("Direction", "Trade direction", "General");
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

_vegasMa = default!;
_vegasStd = default!;
_atr = default!;
_prevUpper = default;
_prevLower = default;
_trend = 1;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_vegasMa = new SimpleMovingAverage { Length = VegasWindow };
_vegasStd = new StandardDeviation { Length = VegasWindow };
_atr = new AverageTrueRange { Length = AtrPeriod };

var subscription = SubscribeCandles(CandleType);
subscription.Bind(_vegasMa, _vegasStd, _atr, ProcessCandle).Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _vegasMa);
DrawIndicator(area, _vegasStd);
DrawIndicator(area, _atr);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, decimal vegasMa, decimal vegasStd, decimal atr)
{
if (candle.State != CandleStates.Finished)
return;

var vegasUpper = vegasMa + vegasStd;
var vegasLower = vegasMa - vegasStd;
var channelWidth = vegasUpper - vegasLower;
var adjustedMultiplier = SuperTrendMultiplier + VolatilityAdjustment * (vegasMa == 0 ? 0 : channelWidth / vegasMa);
var hlc3 = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
var upper = hlc3 - adjustedMultiplier * atr;
var lower = hlc3 + adjustedMultiplier * atr;

var prevUpper = _prevUpper ?? upper;
var prevLower = _prevLower ?? lower;
var prevTrend = _trend;

_trend = candle.ClosePrice > prevLower ? 1 : candle.ClosePrice < prevUpper ? -1 : _trend;

_prevUpper = upper;
_prevLower = lower;

if (!IsFormedAndOnlineAndAllowTrading())
return;

		var allowLong = TradeDirection != Sides.Sell;
		var allowShort = TradeDirection != Sides.Buy;

var longSignal = _trend == 1 && prevTrend != 1;
var shortSignal = _trend == -1 && prevTrend != -1;

if (longSignal && allowLong && Position <= 0)
{
var volume = Volume + (Position < 0 ? -Position : 0m);
BuyMarket(volume);
}
else if (shortSignal && allowShort && Position >= 0)
{
var volume = Volume + (Position > 0 ? Position : 0m);
SellMarket(volume);
}
}
}

