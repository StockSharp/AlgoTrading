using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Automatically applies take profit and stop loss to each trade.
/// </summary>
public class AutostopCyriacStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;

	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	public AutostopCyriacStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 15m)
			.SetGreaterThanZero()
			.SetCanOptimize()
			.SetDisplay("Take Profit", "Take profit in price units", "Protection");

		_stopLoss = Param(nameof(StopLoss), 20m)
			.SetGreaterThanZero()
			.SetCanOptimize()
			.SetDisplay("Stop Loss", "Stop loss in price units", "Protection");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Set up protective orders for all positions
		StartProtection(new Unit(TakeProfit, UnitTypes.Absolute), new Unit(StopLoss, UnitTypes.Absolute));
	}
}
