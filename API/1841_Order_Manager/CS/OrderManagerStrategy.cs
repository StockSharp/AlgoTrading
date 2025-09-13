using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Manages existing portfolio positions by closing them when profit or loss reach
/// defined percentages of the account balance.
/// </summary>
public class OrderManagerStrategy : Strategy
{
	private readonly StrategyParam<bool> _manageAllSecurities;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPercent;

	/// <summary>
	/// Manage positions for all securities in the portfolio.
	/// </summary>
	public bool ManageAllSecurities
	{
		get => _manageAllSecurities.Value;
		set => _manageAllSecurities.Value = value;
	}

	/// <summary>
	/// Enable stop-loss processing.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Maximum tolerated loss as a fraction of the account balance.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Enable take-profit processing.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Desired profit target as a fraction of the account balance.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Initializes parameters with default values.
	/// </summary>
	public OrderManagerStrategy()
	{
		_manageAllSecurities = Param(nameof(ManageAllSecurities), false)
			.SetDisplay("Manage All Securities", "Monitor all portfolio positions", "General");

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Use Stop Loss", "Close positions exceeding loss threshold", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 0.02m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Loss fraction of account balance", "Risk");

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
			.SetDisplay("Use Take Profit", "Close positions reaching profit goal", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 0.06m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Profit fraction of account balance", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, DataType.Ticks)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();
		SubscribeTrades().Bind(OnTrade).Start();
	}

	private void OnTrade(ExecutionMessage trade)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		CheckPositions();
	}

	private void CheckPositions()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return;

		var balance = portfolio.CurrentValue ?? 0m;
		if (balance <= 0)
			return;

		foreach (var position in portfolio.Positions)
		{
			if (!ManageAllSecurities && position.Security != Security)
				continue;

			var currentPrice = position.Security.LastTrade?.Price;
			if (currentPrice == null)
				continue;

			var volume = GetPositionValue(position.Security, Portfolio) ?? 0m;
			if (volume == 0)
				continue;

			var profit = (currentPrice.Value - position.AveragePrice) * volume;
			var risk = profit > 0 ? profit / balance : Math.Abs(profit) / balance;

			var shouldClose = (profit < 0 && UseStopLoss && risk > StopLossPercent) ||
			(profit > 0 && UseTakeProfit && risk >= TakeProfitPercent);

			if (!shouldClose)
				continue;

			if (volume > 0)
				SellMarket(volume, position.Security);
			else
				BuyMarket(Math.Abs(volume), position.Security);
		}
	}
}
