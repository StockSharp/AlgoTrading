namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that closes existing positions when price reaches specified take-profit or stop-loss levels.
/// This replicates a simple TP/SL panel logic without using pending orders.
/// </summary>
public class TpSlPanelStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPrice;
	private readonly StrategyParam<decimal> _stopLossPrice;

	/// <summary>
	/// Take-profit price. Value of 0 disables the level.
	/// </summary>
	public decimal TakeProfitPrice
	{
		get => _takeProfitPrice.Value;
		set => _takeProfitPrice.Value = value;
	}

	/// <summary>
	/// Stop-loss price. Value of 0 disables the level.
	/// </summary>
	public decimal StopLossPrice
	{
		get => _stopLossPrice.Value;
		set => _stopLossPrice.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public TpSlPanelStrategy()
	{
		_takeProfitPrice = Param(nameof(TakeProfitPrice), 0m)
			.SetDisplay("Take Profit Price", "Price level to take profit", "Trading")
			.SetCanOptimize(false);

		_stopLossPrice = Param(nameof(StopLossPrice), 0m)
			.SetDisplay("Stop Loss Price", "Price level to stop loss", "Trading")
			.SetCanOptimize(false);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, DataType.Ticks)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		// Subscribe to trades for real-time price updates
		SubscribeTrades().Bind(ProcessTrade).Start();

		// Enable protection for position management
		StartProtection();

		base.OnStarted(time);
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		var price = trade.TradePrice ?? 0m;

		if (Position == 0)
			return;

		if (Position > 0)
		{
			if (TakeProfitPrice > 0m && price >= TakeProfitPrice)
				ClosePosition();
			else if (StopLossPrice > 0m && price <= StopLossPrice)
				ClosePosition();
		}
		else
		{
			if (TakeProfitPrice > 0m && price <= TakeProfitPrice)
				ClosePosition();
			else if (StopLossPrice > 0m && price >= StopLossPrice)
				ClosePosition();
		}
	}
}
