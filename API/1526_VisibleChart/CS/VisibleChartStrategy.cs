using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades breakouts of the visible chart range defined by a number of recent bars.
/// </summary>
public class VisibleChartStrategy : Strategy
{
	private readonly StrategyParam<int> _visibleBars;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private int _processed;

	/// <summary>
	/// Number of bars treated as visible.
	/// </summary>
	public int VisibleBars { get => _visibleBars.Value; set => _visibleBars.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public VisibleChartStrategy()
	{
		_visibleBars = Param(nameof(VisibleBars), 100)
			.SetDisplay("Visible Bars", "Number of bars considered visible", "General")
			.SetCanOptimize(true)
			.SetOptimize(20, 200, 20);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to analyze", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highest = new Highest { Length = VisibleBars };
		_lowest = new Lowest { Length = VisibleBars };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _highest);
			DrawIndicator(area, _lowest);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var high = _highest.Process(candle.HighPrice, candle.OpenTime, true).ToDecimal();
		var low = _lowest.Process(candle.LowPrice, candle.OpenTime, true).ToDecimal();

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		_processed++;

		var breakoutUp = candle.ClosePrice >= high && Position <= 0;
		var breakoutDown = candle.ClosePrice <= low && Position >= 0;

		if (breakoutUp)
			BuyMarket(Volume + Math.Abs(Position));
		else if (breakoutDown)
			SellMarket(Volume + Math.Abs(Position));

		LogInfo($"Visible range high: {high}, low: {low}, bars: {_processed}");
	}
}
