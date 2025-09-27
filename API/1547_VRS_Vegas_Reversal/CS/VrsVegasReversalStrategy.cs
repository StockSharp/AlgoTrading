using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Vegas Reversal strategy based on spike percentage.
/// Enters long on large lower wick and short on large upper wick.
/// Exits when price moves twice the spike length in favor.
/// </summary>
public class VrsVegasReversalStrategy : Strategy
{
    private readonly StrategyParam<decimal> _spikePercent;
    private readonly StrategyParam<DataType> _candleType;

    private decimal _entryPrice;
    private decimal _spikeSize;
    private bool _isLong;

    /// <summary>
    /// Spike percentage relative to close price.
    /// </summary>
    public decimal SpikePercent
    {
	get => _spikePercent.Value;
	set => _spikePercent.Value = value;
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
    /// Initializes a new instance of <see cref="VrsVegasReversalStrategy"/>.
    /// </summary>
    public VrsVegasReversalStrategy()
    {
	_spikePercent = Param(nameof(SpikePercent), 0.025m)
	    .SetGreaterThanZero()
	    .SetDisplay("Spike %", "Spike percentage threshold", "Reversal")
	    .SetCanOptimize(true)
	    .SetOptimize(0.01m, 0.05m, 0.005m);

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
	_entryPrice = 0;
	_spikeSize = 0;
	_isLong = false;
    }

    /// <inheritdoc />
    protected override void OnStarted(DateTimeOffset time)
    {
	base.OnStarted(time);

	var subscription = SubscribeCandles(CandleType);
	subscription
	    .Bind(ProcessCandle)
	    .Start();

	var area = CreateChartArea();
	if (area != null)
	{
	    DrawCandles(area, subscription);
	    DrawOwnTrades(area);
	}
    }

    private void ProcessCandle(ICandleMessage candle)
    {
	if (candle.State != CandleStates.Finished)
	    return;

	var upperSpike = candle.HighPrice - Math.Max(candle.ClosePrice, candle.OpenPrice);
	var lowerSpike = Math.Min(candle.ClosePrice, candle.OpenPrice) - candle.LowPrice;

	var validUpper = upperSpike >= candle.ClosePrice * SpikePercent;
	var validLower = lowerSpike >= candle.ClosePrice * SpikePercent;
	var valid = (validUpper && !validLower) || (validLower && !validUpper);

	var enterLong = valid && validLower;
	var enterShort = valid && validUpper;

	if (enterLong && Position <= 0)
	{
	    _entryPrice = candle.ClosePrice;
	    _spikeSize = lowerSpike;
	    _isLong = true;
	    BuyMarket(Volume + Math.Abs(Position));
	}
	else if (enterShort && Position >= 0)
	{
	    _entryPrice = candle.ClosePrice;
	    _spikeSize = upperSpike;
	    _isLong = false;
	    SellMarket(Volume + Math.Abs(Position));
	}

	if (Position > 0 && _isLong)
	{
	    var target = _entryPrice + _spikeSize * 2m;
	    if (candle.ClosePrice >= target)
		SellMarket(Math.Abs(Position));
	}
	else if (Position < 0 && !_isLong)
	{
	    var target = _entryPrice - _spikeSize * 2m;
	    if (candle.ClosePrice <= target)
		BuyMarket(Math.Abs(Position));
	}
    }
}
