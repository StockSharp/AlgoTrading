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
/// Utility strategy that mirrors the Jim's Close Orders MetaTrader script.
/// Immediately closes existing positions according to the selected profit filter.
/// </summary>
public class JimsCloseOrdersStrategy : Strategy
{
	private enum CloseMode
	{
		All,
		Positive,
		Negative
	}

	private readonly StrategyParam<bool> _closeOpenOrders;
	private readonly StrategyParam<bool> _closeOrdersWithPlusProfit;
	private readonly StrategyParam<bool> _closeOrdersWithMinusProfit;

	/// <summary>
	/// Close all open positions regardless of their profit.
	/// </summary>
	public bool CloseOpenOrders
	{
		get => _closeOpenOrders.Value;
		set => _closeOpenOrders.Value = value;
	}

	/// <summary>
	/// Close only positions that currently show profit.
	/// </summary>
	public bool CloseOrdersWithPlusProfit
	{
		get => _closeOrdersWithPlusProfit.Value;
		set => _closeOrdersWithPlusProfit.Value = value;
	}

	/// <summary>
	/// Close only positions that currently show a loss.
	/// </summary>
	public bool CloseOrdersWithMinusProfit
	{
		get => _closeOrdersWithMinusProfit.Value;
		set => _closeOrdersWithMinusProfit.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="JimsCloseOrdersStrategy"/> class.
	/// </summary>
	public JimsCloseOrdersStrategy()
	{
		_closeOpenOrders = Param(nameof(CloseOpenOrders), true)
			.SetDisplay("Close All", "Close every open position", "General");

		_closeOrdersWithPlusProfit = Param(nameof(CloseOrdersWithPlusProfit), false)
			.SetDisplay("Close Profitable", "Close only positions with positive unrealized profit", "General");

		_closeOrdersWithMinusProfit = Param(nameof(CloseOrdersWithMinusProfit), false)
			.SetDisplay("Close Losing", "Close only positions with negative unrealized profit", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var portfolio = Portfolio;
		if (portfolio == null)
		{
			LogWarning("Portfolio is not assigned. Nothing to close.");
			Stop();
			return;
		}

		var mode = DetermineMode();
		if (mode == null)
		{
			LogWarning("Select exactly one closing option before starting the strategy.");
			Stop();
			return;
		}

		ClosePositions(portfolio, mode.Value);

		Stop();
	}

	private CloseMode? DetermineMode()
	{
		var selected = 0;
		CloseMode mode = CloseMode.All;

		if (CloseOpenOrders)
		{
			selected++;
			mode = CloseMode.All;
		}

		if (CloseOrdersWithPlusProfit)
		{
			selected++;
			mode = CloseMode.Positive;
		}

		if (CloseOrdersWithMinusProfit)
		{
			selected++;
			mode = CloseMode.Negative;
		}

		return selected == 1 ? mode : (CloseMode?)null;
	}

	private void ClosePositions(Portfolio portfolio, CloseMode mode)
	{
		// Create a snapshot because the collection can change while orders are being sent.
		var positions = portfolio.Positions.ToArray();

		foreach (var position in positions)
		{
			if (position == null)
				continue;

			ProcessPosition(position, mode);
		}
	}

	private void ProcessPosition(Position position, CloseMode mode)
	{
		var signedVolume = position.CurrentValue ?? 0m;
		if (signedVolume == 0m)
			return;

		if (mode != CloseMode.All)
		{
			var profit = EstimateProfit(position, signedVolume);
			if (profit == null)
			{
				LogWarning($"Skipping {position.Security?.Id} because no market price is available.");
				return;
			}

			if (mode == CloseMode.Positive && profit < 0m)
				return;

			if (mode == CloseMode.Negative && profit > 0m)
				return;
		}

		var security = position.Security;
		if (security == null)
		{
			LogWarning("Encountered a position without an associated security.");
			return;
		}

		ClosePosition(security);
	}

	private decimal? EstimateProfit(Position position, decimal signedVolume)
	{
		var security = position.Security;
		if (security == null)
			return null;

		// Use bid for long exits and ask for short exits to mirror MT4 logic.
		decimal? closePrice;
		if (signedVolume > 0m)
			closePrice = security.BestBid?.Price ?? security.LastTrade?.Price;
		else
			closePrice = security.BestAsk?.Price ?? security.LastTrade?.Price;

		if (closePrice == null)
			return null;

		return (closePrice.Value - position.AveragePrice) * signedVolume;
	}
}
