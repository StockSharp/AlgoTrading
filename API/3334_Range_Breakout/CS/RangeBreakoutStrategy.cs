using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy using the recent high-low range.
/// Trades only when the range width stays below the configured threshold.
/// </summary>
public class RangeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<int> _rangePeriod;
	private readonly StrategyParam<decimal> _maxRangePoints;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;

	private Highest _highest = null!;
	private Lowest _lowest = null!;

	/// <summary>
	/// Number of candles to look back for the breakout range.
	/// </summary>
	public int RangePeriod
	{
		get => _rangePeriod.Value;
		set => _rangePeriod.Value = value;
	}

	/// <summary>
	/// Maximum allowed range in points to accept a breakout.
	/// </summary>
	public decimal MaxRangePoints
	{
		get => _maxRangePoints.Value;
		set => _maxRangePoints.Value = value;
	}

	/// <summary>
	/// Candle type to use for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Trading volume for market orders.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RangeBreakoutStrategy"/> class.
	/// </summary>
	public RangeBreakoutStrategy()
	{
		_rangePeriod = Param(nameof(RangePeriod), 10)
			.SetDisplay("Range Period", "Number of candles forming the range", "General")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_maxRangePoints = Param(nameof(MaxRangePoints), 300m)
			.SetDisplay("Max Range", "Upper bound for the range size in points", "General")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for breakout detection", "General");

		_volume = Param(nameof(Volume), 0.1m)
			.SetDisplay("Volume", "Order size for entries", "Trading")
			.SetGreaterThanZero();

		_stopLossPoints = Param(nameof(StopLossPoints), 500m)
			.SetDisplay("Stop Loss", "Stop loss distance in points", "Risk")
			.SetGreaterThanZero();

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 1000m)
			.SetDisplay("Take Profit", "Take profit distance in points", "Risk")
			.SetGreaterThanZero();
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

		_highest = new Highest { Length = RangePeriod };
		_lowest = new Lowest { Length = RangePeriod };

		var takeProfitUnit = TakeProfitPoints > 0 ? new Unit(TakeProfitPoints, UnitTypes.Step) : default(Unit?);
		var stopLossUnit = StopLossPoints > 0 ? new Unit(StopLossPoints, UnitTypes.Step) : default(Unit?);

		StartProtection(takeProfit: takeProfitUnit, stopLoss: stopLossUnit);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_highest, _lowest, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _highest, "Highest");
			DrawIndicator(area, _lowest, "Lowest");
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var priceStep = Security?.PriceStep;
		if (priceStep <= 0)
			return;

		var rangePoints = (highestValue - lowestValue) / priceStep.Value;
		if (rangePoints > MaxRangePoints)
			return;

		if (Position != 0)
			return;

		var closePrice = candle.ClosePrice;

		if (closePrice >= highestValue)
		{
			// Breakout above the range opens a long position.
			BuyMarket(Volume);
		}
		else if (closePrice <= lowestValue)
		{
			// Breakdown below the range opens a short position.
			SellMarket(Volume);
		}
	}
}
