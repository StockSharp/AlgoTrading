using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Renko live chart emulation strategy.
/// </summary>
public class RenkoLiveChartStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _brickSize;
	private readonly StrategyParam<int> _brickOffset;
	private readonly StrategyParam<bool> _showWicks;

	private decimal _renkoPrice;
	/// <summary>
	/// Type of input candles.
	/// </summary>
	public DataType CandleType
	{
	    get => _candleType.Value;
	    set => _candleType.Value = value;
	}

	/// <summary>
	/// Renko brick size.
	/// </summary>
	public decimal BrickSize
	{
	    get => _brickSize.Value;
	    set => _brickSize.Value = value;
	}

	/// <summary>
	/// Renko brick offset in number of bricks.
	/// </summary>
	public int BrickOffset
	{
	    get => _brickOffset.Value;
	    set => _brickOffset.Value = value;
	}

	/// <summary>
	/// Show wicks on chart.
	/// </summary>
	public bool ShowWicks
	{
	    get => _showWicks.Value;
	    set => _showWicks.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RenkoLiveChartStrategy"/>.
	/// </summary>
	public RenkoLiveChartStrategy()
	{
	    _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
	        .SetDisplay("Candle Type", "Working candle timeframe", "General");

	    _brickSize = Param(nameof(BrickSize), 2.5m)
	        .SetGreaterThanZero()
	        .SetDisplay("Brick Size", "Renko brick size", "General")
	        .SetCanOptimize(true);

	    _brickOffset = Param(nameof(BrickOffset), 0)
	        .SetDisplay("Brick Offset", "Offset in bricks", "General");

	    _showWicks = Param(nameof(ShowWicks), true)
	        .SetDisplay("Show Wicks", "Show wicks on chart", "General");
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
	    _renkoPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	    base.OnStarted(time);

	    var subscription = SubscribeCandles(CandleType);
	    subscription.Bind(ProcessCandle).Start();

	    StartProtection();

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

	    var close = candle.ClosePrice;
	    var size = BrickSize;

	    if (_renkoPrice == 0m)
	    {
	        _renkoPrice = close + BrickOffset * size;
	        return;
	    }

	    var diff = close - _renkoPrice;
	    if (Math.Abs(diff) < size)
	        return;

	    var direction = Math.Sign(diff);
	    _renkoPrice += direction * size;

	    if (direction > 0 && Position <= 0)
	        BuyMarket(Volume + Math.Abs(Position));
	    else if (direction < 0 && Position >= 0)
	        SellMarket(Volume + Math.Abs(Position));

	}
}
