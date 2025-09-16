using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Closes all open positions once a profit target is reached.
/// </summary>
public class ExpToCloseProfitStrategy : Strategy
{
	private readonly StrategyParam<decimal> _maxProfit;
	private bool _triggered;

	/// <summary>
	/// Profit threshold that triggers position closing.
	/// </summary>
	public decimal MaxProfit
	{
		get => _maxProfit.Value;
		set => _maxProfit.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="ExpToCloseProfitStrategy"/>.
	/// </summary>
	public ExpToCloseProfitStrategy()
	{
		_maxProfit = Param(nameof(MaxProfit), 1000m)
			.SetDisplay("Max Profit", "Profit target to close all positions", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, DataType.Ticks)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeTrades().Bind(ProcessTrade).Start();
		StartProtection();
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		var price = trade.TradePrice;

		if (price is null)
			return;

		var unrealized = Position * (price.Value - PositionPrice);
		var totalProfit = PnL + unrealized;

		LogInfo($"Profit = {totalProfit:0.##}; MaxProfit = {MaxProfit:0.##};");

		if (!_triggered && totalProfit >= MaxProfit)
			_triggered = true;

		if (_triggered)
		{
			ClosePositions();

			if (Position == 0)
				_triggered = false;
		}
	}

	private void ClosePositions()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);
	}
}
