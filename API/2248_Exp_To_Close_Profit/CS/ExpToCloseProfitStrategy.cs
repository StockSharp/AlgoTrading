using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
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
	private decimal _entryPrice;

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
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		SubscribeTicks().Bind(ProcessTrade).Start();
		StartProtection(null, null);
	}

		private void ProcessTrade(ITickTradeMessage trade)
		{
			var price = trade.Price;

		var unrealized = Position * (price - _entryPrice);
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