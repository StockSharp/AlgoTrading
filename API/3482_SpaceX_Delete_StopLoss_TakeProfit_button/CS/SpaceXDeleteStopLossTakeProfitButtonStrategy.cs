namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Utility strategy that removes protective stop-loss and take-profit orders from open positions.
/// Mirrors the "DELETE SL_TP" button from the original MetaTrader panel.
/// </summary>
public class SpaceXDeleteStopLossTakeProfitButtonStrategy : Strategy
{
	private readonly StrategyParam<bool> _applyOnStart;
	private readonly StrategyParam<bool> _affectAllSecurities;
	private readonly StrategyParam<bool> _deleteRequest;
	private readonly StrategyParam<int> _pollingIntervalSeconds;

	/// <summary>
	/// Execute the delete action automatically when the strategy starts.
	/// </summary>
	public bool ApplyOnStart
	{
		get => _applyOnStart.Value;
		set => _applyOnStart.Value = value;
	}

	/// <summary>
	/// Process positions from every portfolio security instead of the attached one only.
	/// </summary>
	public bool AffectAllSecurities
	{
		get => _affectAllSecurities.Value;
		set => _affectAllSecurities.Value = value;
	}

	/// <summary>
	/// Manual trigger that emulates the MetaTrader button.
	/// Set to <c>true</c> to schedule a delete action; it resets to <c>false</c> once processed.
	/// </summary>
	public bool DeleteRequest
	{
		get => _deleteRequest.Value;
		set => _deleteRequest.Value = value;
	}

	/// <summary>
	/// Interval in seconds used to poll the manual request flag.
	/// </summary>
	public int PollingIntervalSeconds
	{
		get => _pollingIntervalSeconds.Value;
		set => _pollingIntervalSeconds.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="SpaceXDeleteStopLossTakeProfitButtonStrategy"/>.
	/// </summary>
	public SpaceXDeleteStopLossTakeProfitButtonStrategy()
	{
		_applyOnStart = Param(nameof(ApplyOnStart), true)
			.SetDisplay("Run On Start", "Automatically remove protective orders when the strategy starts.", "General")
			.SetCanOptimize(false);

		_affectAllSecurities = Param(nameof(AffectAllSecurities), true)
			.SetDisplay("All Securities", "Include every open portfolio position instead of only the attached security.", "Scope")
			.SetCanOptimize(false);

		_deleteRequest = Param(nameof(DeleteRequest), false)
			.SetDisplay("Delete Request", "Flip to true to emulate the DELETE SL_TP button.", "Manual Controls")
			.SetCanOptimize(false);

		_pollingIntervalSeconds = Param(nameof(PollingIntervalSeconds), 1)
			.SetDisplay("Polling Interval (s)", "Timer interval that checks the manual request flag.", "General")
			.SetGreaterThanZero();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, default);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		DeleteRequest = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (PollingIntervalSeconds <= 0)
			throw new InvalidOperationException("Polling interval must be positive.");

		Timer.Start(TimeSpan.FromSeconds(PollingIntervalSeconds), OnTimer);

		if (ApplyOnStart)
		{
			ExecuteDeletion();
		}
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		Timer.Stop();

		base.OnStopped();
	}

	private void OnTimer()
	{
		if (!DeleteRequest)
			return;

		ExecuteDeletion();
		DeleteRequest = false;
	}

	private void ExecuteDeletion()
	{
		var securities = CollectTargetSecurities();
		if (securities.Count == 0)
		{
			LogInfo("No open positions were found for the configured scope.");
			return;
		}

		var cancelled = 0;

		foreach (var security in securities)
		{
			cancelled += CancelProtectiveOrders(security);
		}

		if (cancelled == 0)
		{
			LogInfo("No protective orders were active for the targeted positions.");
		}
		else
		{
			LogInfo($"Cancelled {cancelled} protective order(s).");
		}
	}

	private HashSet<Security> CollectTargetSecurities()
	{
		var securities = new HashSet<Security>();

		if (Portfolio is null)
			return securities;

		if (AffectAllSecurities)
		{
			foreach (var position in Portfolio.Positions)
			{
				if (position.CurrentValue == 0m)
					continue;

				if (position.Security is not null)
					securities.Add(position.Security);
			}
		}
		else if (Security is not null)
		{
			var position = Portfolio.Positions.FirstOrDefault(p => p.Security == Security);
			if (position is not null && position.CurrentValue != 0m)
			{
				securities.Add(Security);
			}
		}

		return securities;
	}

	private int CancelProtectiveOrders(Security security)
	{
		var cancelled = 0;

		foreach (var order in Orders)
		{
			if (order.Security != security)
				continue;

			if (!order.State.IsActive())
				continue;

			if (!IsProtectiveOrder(order))
				continue;

			CancelOrder(order);
			cancelled++;
		}

		return cancelled;
	}

	private static bool IsProtectiveOrder(Order order)
	{
		switch (order.Type)
		{
			case OrderTypes.Stop:
			case OrderTypes.TakeProfit:
			case OrderTypes.Conditional:
				return true;
		}

		return order.Condition is not null || order.StopPrice is not null || order.TakeProfit is not null;
	}
}
