namespace StockSharp.Samples.Strategies;

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

using StockSharp.Algo;

/// <summary>
/// Recreates the MetaTrader "Close panel" utility by automating the exit rules.
/// </summary>
public class ClosePanelStrategy : Strategy
{
	private readonly StrategyParam<decimal> _lossThreshold;
	private readonly StrategyParam<decimal> _profitThreshold;
	private readonly StrategyParam<bool> _closeLossEnabled;
	private readonly StrategyParam<bool> _closeProfitEnabled;
	private readonly StrategyParam<bool> _closeAllOnStart;

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public ClosePanelStrategy()
	{
		_lossThreshold = Param(nameof(LossThreshold), 30m)
			.SetDisplay("Loss Threshold", "Absolute loss (in money) that forces a position to close", "General")
			.SetCanOptimize(true)
			.SetOptimize(10m, 200m, 10m);

		_profitThreshold = Param(nameof(ProfitThreshold), 90m)
			.SetDisplay("Profit Threshold", "Floating profit (in money) required to secure gains", "General")
			.SetCanOptimize(true)
			.SetOptimize(20m, 400m, 20m);

		_closeLossEnabled = Param(nameof(CloseLossEnabled), false)
			.SetDisplay("Close Losing Positions", "Automatically close positions whose loss exceeds the threshold", "General");

		_closeProfitEnabled = Param(nameof(CloseProfitEnabled), false)
			.SetDisplay("Close Winning Positions", "Automatically close positions whose profit exceeds the threshold", "General");

		_closeAllOnStart = Param(nameof(CloseAllOnStart), false)
			.SetDisplay("Close All On Start", "Flatten every position as soon as the strategy starts", "General");
	}

	/// <summary>
	/// Maximum tolerated unrealized loss for each position, expressed in the portfolio currency.
	/// </summary>
	public decimal LossThreshold
	{
		get => _lossThreshold.Value;
		set => _lossThreshold.Value = value;
	}

	/// <summary>
	/// Floating profit that triggers a protective exit for winning positions.
	/// </summary>
	public decimal ProfitThreshold
	{
		get => _profitThreshold.Value;
		set => _profitThreshold.Value = value;
	}

	/// <summary>
	/// Enables the automatic closing routine for losing positions.
	/// </summary>
	public bool CloseLossEnabled
	{
		get => _closeLossEnabled.Value;
		set => _closeLossEnabled.Value = value;
	}

	/// <summary>
	/// Enables the automatic closing routine for profitable positions.
	/// </summary>
	public bool CloseProfitEnabled
	{
		get => _closeProfitEnabled.Value;
		set => _closeProfitEnabled.Value = value;
	}

	/// <summary>
	/// Sends market orders to flatten every position right after the strategy starts.
	/// </summary>
	public bool CloseAllOnStart
	{
		get => _closeAllOnStart.Value;
		set => _closeAllOnStart.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, DataType.Ticks);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (CloseAllOnStart)
		{
			CloseAllPositions("Close all command triggered on start");
		}

		SubscribeTicks()
			.Bind(ProcessTrade)
			.Start();
	}

	private void ProcessTrade(ITickTradeMessage trade)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		TryClosePositionsByFilters();
	}

	private void TryClosePositionsByFilters()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return;

		foreach (var position in portfolio.Positions)
		{
			var security = position.Security ?? Security;
			if (security == null)
				continue;

			var volume = position.CurrentValue ?? 0m;
			if (volume == 0m)
				continue;

			var profit = position.PnL ?? 0m;

			if (CloseLossEnabled && LossThreshold > 0m && profit <= -LossThreshold)
			{
				SendExitOrder(security, volume, $"loss {profit:0.##} below threshold {LossThreshold:0.##}");
				continue;
			}

			if (CloseProfitEnabled && ProfitThreshold > 0m && profit >= ProfitThreshold)
			{
				SendExitOrder(security, volume, $"profit {profit:0.##} above threshold {ProfitThreshold:0.##}");
			}
		}
	}

	private void CloseAllPositions(string reason)
	{
		var exposures = new Dictionary<Security, decimal>();

		void AddExposure(Security security, decimal volume)
		{
			if (security == null || volume == 0m)
				return;

			if (exposures.TryGetValue(security, out var existing))
				exposures[security] = existing + volume;
			else
				exposures.Add(security, volume);
		}

		AddExposure(Security, Position);

		foreach (var position in Positions)
			AddExposure(position.Security ?? Security, position.CurrentValue ?? 0m);

		var portfolio = Portfolio;
		if (portfolio != null)
		{
			foreach (var position in portfolio.Positions)
				AddExposure(position.Security ?? Security, position.CurrentValue ?? 0m);
		}

		foreach (var pair in exposures)
			SendExitOrder(pair.Key, pair.Value, reason);
	}

	private void SendExitOrder(Security security, decimal volume, string reason)
	{
		if (volume == 0m)
			return;

		var side = volume > 0m ? Sides.Sell : Sides.Buy;
		if (HasActiveExitOrder(security, side))
			return;

		if (volume > 0m)
			SellMarket(volume, security);
		else
			BuyMarket(-volume, security);

		LogInfo($"Closing {security.Id} position {volume:0.####} because {reason}.");
	}

	private bool HasActiveExitOrder(Security security, Sides side)
	{
		foreach (var order in Orders)
		{
			if (order.Security != security)
				continue;

			if (!order.State.IsActive())
				continue;

			if (order.Side == side)
				return true;
		}

		return false;
	}
}

