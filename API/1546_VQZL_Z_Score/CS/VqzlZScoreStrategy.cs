using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Z-Score strategy based on price deviation from a moving average.
/// Long when z-score crosses above the threshold.
/// Short when z-score crosses below the negative threshold.
/// </summary>
public class VqzlZScoreStrategy : Strategy
{
    private readonly StrategyParam<int> _priceSmoothing;
    private readonly StrategyParam<int> _zLength;
    private readonly StrategyParam<decimal> _threshold;
    private readonly StrategyParam<DataType> _candleType;

    /// <summary>
    /// Period for price smoothing moving average.
    /// </summary>
    public int PriceSmoothing
    {
	get => _priceSmoothing.Value;
	set => _priceSmoothing.Value = value;
    }

    /// <summary>
    /// Lookback length for standard deviation calculation.
    /// </summary>
    public int ZLength
    {
	get => _zLength.Value;
	set => _zLength.Value = value;
    }

    /// <summary>
    /// Z-score threshold for entries.
    /// </summary>
    public decimal Threshold
    {
	get => _threshold.Value;
	set => _threshold.Value = value;
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
    /// Initializes a new instance of <see cref="VqzlZScoreStrategy"/>.
    /// </summary>
    public VqzlZScoreStrategy()
    {
	_priceSmoothing = Param(nameof(PriceSmoothing), 15)
	    .SetGreaterThanZero()
	    .SetDisplay("Price Smoothing", "Length of smoothing moving average", "ZScore")
	    .SetCanOptimize(true)
	    .SetOptimize(5, 50, 5);

	_zLength = Param(nameof(ZLength), 100)
	    .SetGreaterThanZero()
	    .SetDisplay("Z Length", "Lookback for standard deviation", "ZScore")
	    .SetCanOptimize(true)
	    .SetOptimize(50, 200, 10);

	_threshold = Param(nameof(Threshold), 1.64m)
	    .SetGreaterThanZero()
	    .SetDisplay("Z Threshold", "Z-score threshold", "ZScore")
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

	var ma = new SMA { Length = PriceSmoothing };
	var dev = new StandardDeviation { Length = ZLength };

	var subscription = SubscribeCandles(CandleType);

	subscription
	    .Bind(ma, dev, ProcessCandle)
	    .Start();

	var area = CreateChartArea();
	if (area != null)
	{
	    DrawCandles(area, subscription);
	    DrawIndicator(area, ma);
	    DrawIndicator(area, dev);
	    DrawOwnTrades(area);
	}
    }

    private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal devValue)
    {
	if (candle.State != CandleStates.Finished)
	    return;

	if (devValue == 0)
	    return;

	var z = (candle.ClosePrice - maValue) / devValue;

	if (z > Threshold && Position <= 0)
	{
	    BuyMarket(Volume + Math.Abs(Position));
	}
	else if (z < -Threshold && Position >= 0)
	{
	    SellMarket(Volume + Math.Abs(Position));
	}
    }
}
