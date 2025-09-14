using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Closes open positions when profit or loss reaches specified hidden levels.
/// </summary>
public class HiddenSlStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;

	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	public HiddenSlStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 113m)
			.SetDisplay("Take Profit", "Profit target in currency", "General");
		_stopLoss = Param(nameof(StopLoss), -100m)
			.SetDisplay("Stop Loss", "Loss limit in currency (negative value)", "General");
	}

	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Ticks)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		SubscribeTrades()
			.Bind(ProcessTrade)
			.Start();
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		var price = trade.TradePrice;
		if (price is null)
			return;

		var pos = Position;
		if (pos == 0)
			return;

		var entryPrice = Position.AveragePrice;
		if (entryPrice == 0m)
			return;

		var profit = (price.Value - entryPrice) * pos;

		if (profit >= TakeProfit || profit <= StopLoss)
		{
			if (pos > 0)
				SellMarket(pos);
			else
				BuyMarket(-pos);
		}
	}
}
