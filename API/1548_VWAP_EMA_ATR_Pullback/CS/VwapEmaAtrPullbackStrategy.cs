using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Combines fast and slow EMAs with VWAP pullback and ATR filter.
/// Enters in trend direction when price pulls back to VWAP.
/// Takes profit at VWAP plus/minus ATR multiplier.
/// </summary>
public class VwapEmaAtrPullbackStrategy : Strategy
{
    private readonly StrategyParam<int> _fastEmaLength;
    private readonly StrategyParam<int> _slowEmaLength;
    private readonly StrategyParam<int> _atrLength;
    private readonly StrategyParam<decimal> _atrMultiplier;
    private readonly StrategyParam<DataType> _candleType;

    /// <summary>
    /// Fast EMA period.
    /// </summary>
    public int FastEmaLength
    {
	get => _fastEmaLength.Value;
	set => _fastEmaLength.Value = value;
    }

    /// <summary>
    /// Slow EMA period.
    /// </summary>
    public int SlowEmaLength
    {
	get => _slowEmaLength.Value;
	set => _slowEmaLength.Value = value;
    }

    /// <summary>
    /// ATR period length.
    /// </summary>
    public int AtrLength
    {
	get => _atrLength.Value;
	set => _atrLength.Value = value;
    }

    /// <summary>
    /// ATR multiplier for trend filter and exits.
    /// </summary>
    public decimal AtrMultiplier
    {
	get => _atrMultiplier.Value;
	set => _atrMultiplier.Value = value;
    }

    /// <summary>
    /// Candle type for calculations.
    /// </summary>
    public DataType CandleType
    {
	get => _candleType.Value;
	set => _candleType.Value = value;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="VwapEmaAtrPullbackStrategy"/>.
    /// </summary>
    public VwapEmaAtrPullbackStrategy()
    {
	_fastEmaLength = Param(nameof(FastEmaLength), 30)
	    .SetGreaterThanZero()
	    .SetDisplay("Fast EMA", "Fast EMA length", "Trend")
	    .SetCanOptimize(true)
	    .SetOptimize(10, 60, 5);

	_slowEmaLength = Param(nameof(SlowEmaLength), 200)
	    .SetGreaterThanZero()
	    .SetDisplay("Slow EMA", "Slow EMA length", "Trend")
	    .SetCanOptimize(true)
	    .SetOptimize(100, 300, 20);

	_atrLength = Param(nameof(AtrLength), 14)
	    .SetGreaterThanZero()
	    .SetDisplay("ATR Length", "ATR period", "Volatility")
	    .SetCanOptimize(true)
	    .SetOptimize(5, 30, 1);

	_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
	    .SetGreaterThanZero()
	    .SetDisplay("ATR Mult", "ATR multiplier", "Volatility")
	    .SetCanOptimize(true)
	    .SetOptimize(1m, 3m, 0.5m);

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
    }

    /// <inheritdoc />
    protected override void OnStarted(DateTimeOffset time)
    {
	base.OnStarted(time);

	var emaFast = new EMA { Length = FastEmaLength };
	var emaSlow = new EMA { Length = SlowEmaLength };
	var atr = new AverageTrueRange { Length = AtrLength };
	var vwap = new VolumeWeightedMovingAverage();

	var subscription = SubscribeCandles(CandleType);
	subscription
	    .Bind(vwap, emaFast, emaSlow, atr, ProcessCandle)
	    .Start();

	var area = CreateChartArea();
	if (area != null)
	{
	    DrawCandles(area, subscription);
	    DrawIndicator(area, vwap);
	    DrawIndicator(area, emaFast);
	    DrawIndicator(area, emaSlow);
	    DrawOwnTrades(area);
	}
    }

    private void ProcessCandle(ICandleMessage candle, decimal vwapValue, decimal emaFastValue, decimal emaSlowValue, decimal atrValue)
    {
	if (candle.State != CandleStates.Finished)
	    return;

	var uptrend = emaFastValue > emaSlowValue && (emaFastValue - emaSlowValue) > atrValue * AtrMultiplier;
	var downtrend = emaFastValue < emaSlowValue && (emaSlowValue - emaFastValue) > atrValue * AtrMultiplier;

	var longEntry = uptrend && candle.ClosePrice < vwapValue;
	var shortEntry = downtrend && candle.ClosePrice > vwapValue;

	if (longEntry && Position <= 0)
	{
	    BuyMarket(Volume + Math.Abs(Position));
	}
	else if (shortEntry && Position >= 0)
	{
	    SellMarket(Volume + Math.Abs(Position));
	}

	var longTarget = vwapValue + atrValue * AtrMultiplier;
	var shortTarget = vwapValue - atrValue * AtrMultiplier;

	if (Position > 0 && candle.ClosePrice >= longTarget)
	    SellMarket(Math.Abs(Position));
	else if (Position < 0 && candle.ClosePrice <= shortTarget)
	    BuyMarket(Math.Abs(Position));
    }
}
