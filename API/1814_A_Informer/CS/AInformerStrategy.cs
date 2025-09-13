using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Utility strategy that attaches stop loss and take profit to existing orders and logs profit.
/// </summary>
public class AInformerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	/// <summary>
	/// Stop loss distance in points.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance in points.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public AInformerStrategy()
	{
		_stopLoss = Param(nameof(StopLoss), 300m)
			.SetDisplay("Stop Loss", "Stop loss distance in points", "Risk")
			.SetCanOptimize();

		_takeProfit = Param(nameof(TakeProfit), 1000m)
			.SetDisplay("Take Profit", "Take profit distance in points", "Risk")
			.SetCanOptimize();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, default(DataType))];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Enable position protection with take profit and stop loss.
		StartProtection(
			new Unit(TakeProfit, UnitTypes.Point),
			new Unit(StopLoss, UnitTypes.Point));
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		// Calculate profit in USD.
		var profitUsd = PnL;

		// Convert profit to points using security parameters.
		var stepPrice = Security?.StepPrice ?? 1m;
		var priceStep = Security?.PriceStep ?? 1m;
		var profitPoints = stepPrice > 0 ? profitUsd / stepPrice * priceStep : 0m;

		// Log current profit information.
		this.LogInfo($"Profit: {profitUsd:0.##} USD ({profitPoints:0.##} points)");
	}
}
