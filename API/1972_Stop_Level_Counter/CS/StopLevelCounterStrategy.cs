using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Utility strategy that calculates potential profit for buy and sell orders at a specified price level.
/// </summary>
public class StopLevelCounterStrategy : Strategy
{
	private readonly StrategyParam<decimal> _level;
	private readonly StrategyParam<decimal> _volume;

	/// <summary>
	/// Price level used for profit calculation.
	/// </summary>
	public decimal Level
	{
		get => _level.Value;
		set => _level.Value = value;
	}

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public StopLevelCounterStrategy()
	{
		_level = Param(nameof(Level), 0m)
			.SetDisplay("Stop Level", "Price level for profit calculation", "General");

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize level with current bid if not set.
		if (Level == 0m && Security.BestBid != null)
			Level = Security.BestBid.Price;

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			var end = time + TimeSpan.FromDays(30);
			DrawLine(time, Level, end, Level);
		}
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (!level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj) ||
			!level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj))
			return;

		if (bidObj is not decimal bid || askObj is not decimal ask)
			return;

		var step = Security.PriceStep ?? 1m;
		var stepPrice = Security.StepPrice ?? step;

		var buyProfit = (Level - ask) / step * stepPrice * Volume;
		var sellProfit = (bid - Level) / step * stepPrice * Volume;

		AddInfoLog($"Level {Level:F2} Buy {buyProfit:F2} {Security.Currency} Sell {sellProfit:F2} {Security.Currency}");
	}
}
