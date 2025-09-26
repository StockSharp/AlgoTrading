namespace StockSharp.Samples.Strategies;

using System;
using System.Reflection;
using System.Threading;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Utility strategy that closes open positions and cancels pending orders based on filters.
/// </summary>
public class CloseDeleteEaStrategy : Strategy
{
	private static readonly PropertyInfo PositionStrategyIdProperty = typeof(Position).GetProperty("StrategyId");
	private static readonly PropertyInfo OrderStrategyIdProperty = typeof(Order).GetProperty("StrategyId");

	private readonly StrategyParam<bool> _closeBuyPositions;
	private readonly StrategyParam<bool> _closeSellPositions;
	private readonly StrategyParam<bool> _closeMarketPositions;
	private readonly StrategyParam<bool> _cancelPendingOrders;
	private readonly StrategyParam<bool> _closeOnlyProfitable;
	private readonly StrategyParam<bool> _closeOnlyLosing;
	private readonly StrategyParam<bool> _applyToCurrentSecurity;
	private readonly StrategyParam<string> _targetStrategyId;
	private readonly StrategyParam<TimeSpan> _timerInterval;

	private Timer _timer;
	private int _isProcessing;
	private bool _hasWork;

	/// <summary>
	/// Determines whether long positions should be closed.
	/// </summary>
	public bool CloseBuyPositions
	{
		get => _closeBuyPositions.Value;
		set => _closeBuyPositions.Value = value;
	}

	/// <summary>
	/// Determines whether short positions should be closed.
	/// </summary>
	public bool CloseSellPositions
	{
		get => _closeSellPositions.Value;
		set => _closeSellPositions.Value = value;
	}

	/// <summary>
	/// Determines whether market positions should be flattened.
	/// </summary>
	public bool CloseMarketPositions
	{
		get => _closeMarketPositions.Value;
		set => _closeMarketPositions.Value = value;
	}

	/// <summary>
	/// Determines whether pending orders should be canceled.
	/// </summary>
	public bool CancelPendingOrders
	{
		get => _cancelPendingOrders.Value;
		set => _cancelPendingOrders.Value = value;
	}

	/// <summary>
	/// Allows closing only profitable positions.
	/// </summary>
	public bool CloseOnlyProfitable
	{
		get => _closeOnlyProfitable.Value;
		set => _closeOnlyProfitable.Value = value;
	}

	/// <summary>
	/// Allows closing only losing positions.
	/// </summary>
	public bool CloseOnlyLosing
	{
		get => _closeOnlyLosing.Value;
		set => _closeOnlyLosing.Value = value;
	}

	/// <summary>
	/// Limits the action to the current security when enabled.
	/// </summary>
	public bool ApplyToCurrentSecurity
	{
		get => _applyToCurrentSecurity.Value;
		set => _applyToCurrentSecurity.Value = value;
	}

	/// <summary>
	/// Filters positions and orders by strategy identifier (empty value matches all).
	/// </summary>
	public string TargetStrategyId
	{
		get => _targetStrategyId.Value;
		set => _targetStrategyId.Value = value;
	}

	/// <summary>
	/// Interval used by the management timer.
	/// </summary>
	public TimeSpan TimerInterval
	{
		get => _timerInterval.Value;
		set => _timerInterval.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="CloseDeleteEaStrategy"/>.
	/// </summary>
	public CloseDeleteEaStrategy()
	{
		_closeBuyPositions = Param(nameof(CloseBuyPositions), true)
			.SetDisplay("Close Buy Positions", "Close long exposure that matches the filters", "General")
			.SetCanOptimize(false);

		_closeSellPositions = Param(nameof(CloseSellPositions), true)
			.SetDisplay("Close Sell Positions", "Close short exposure that matches the filters", "General")
			.SetCanOptimize(false);

		_closeMarketPositions = Param(nameof(CloseMarketPositions), true)
			.SetDisplay("Close Market Positions", "Send market orders to flatten exposure", "General")
			.SetCanOptimize(false);

		_cancelPendingOrders = Param(nameof(CancelPendingOrders), true)
			.SetDisplay("Cancel Pending Orders", "Cancel working orders that match the filters", "General")
			.SetCanOptimize(false);

		_closeOnlyProfitable = Param(nameof(CloseOnlyProfitable), false)
			.SetDisplay("Close Only Profitable", "Close positions only when PnL is non-negative", "Filters")
			.SetCanOptimize(false);

		_closeOnlyLosing = Param(nameof(CloseOnlyLosing), false)
			.SetDisplay("Close Only Losing", "Close positions only when PnL is non-positive", "Filters")
			.SetCanOptimize(false);

		_applyToCurrentSecurity = Param(nameof(ApplyToCurrentSecurity), true)
			.SetDisplay("Current Security Only", "Restrict actions to the strategy security", "Scope")
			.SetCanOptimize(false);

		_targetStrategyId = Param(nameof(TargetStrategyId), string.Empty)
			.SetDisplay("Target Strategy Id", "Filter by strategy id (leave empty for all)", "Scope")
			.SetCanOptimize(false);

		_timerInterval = Param(nameof(TimerInterval), TimeSpan.FromMilliseconds(500))
			.SetDisplay("Timer Interval", "Frequency of the management loop", "General")
			.SetCanOptimize(false);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Portfolio == null)
			throw new InvalidOperationException("Portfolio must be assigned before starting the strategy.");

		if (TimerInterval <= TimeSpan.Zero)
			throw new InvalidOperationException("Timer interval must be a positive value.");

		_timer = new Timer(_ => ProcessManagement(), null, TimeSpan.Zero, TimerInterval);
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		_timer?.Dispose();
		_timer = null;

		base.OnStopped();
	}

	private void ProcessManagement()
	{
		if (Interlocked.Exchange(ref _isProcessing, 1) == 1)
			return;

		try
		{
			var portfolio = Portfolio;
			if (portfolio == null)
				return;

			var referenceSecurity = Security;
			var applyCurrent = ApplyToCurrentSecurity && referenceSecurity != null;
			var filter = TargetStrategyId;
			var filterEnabled = !string.IsNullOrWhiteSpace(filter);

			var hasPendingWork = false;
			var startedWork = false;

			if (CloseMarketPositions)
			{
				foreach (var position in portfolio.Positions)
				{
					var security = position.Security ?? referenceSecurity;
					if (applyCurrent && !Equals(security, referenceSecurity))
						continue;

					if (!MatchesStrategyFilter(position, filterEnabled, filter))
						continue;

					var volume = position.CurrentValue ?? 0m;
					if (volume == 0m)
						continue;

					if (!ShouldClosePosition(volume, position))
						continue;

					if (security == null)
						continue;

					hasPendingWork = true;

					var side = volume > 0m ? Sides.Sell : Sides.Buy;
					if (HasActiveExitOrder(security, side))
						continue;

					if (volume > 0m)
					{
						SellMarket(volume, security);
					}
					else
					{
						BuyMarket(-volume, security);
					}

					startedWork = true;
				}
			}

			if (CancelPendingOrders)
			{
				foreach (var order in Orders)
				{
					if (!order.State.IsActive())
						continue;

					var security = order.Security ?? referenceSecurity;
					if (applyCurrent && !Equals(security, referenceSecurity))
						continue;

					if (!MatchesStrategyFilter(order, filterEnabled, filter))
						continue;

					CancelOrder(order);
					hasPendingWork = true;
					startedWork = true;
				}
			}

			if (startedWork && !_hasWork)
			{
				LogInfo("Close/delete cycle started.");
				_hasWork = true;
			}

			if (!hasPendingWork && _hasWork)
			{
				LogInfo("Close/delete cycle finished. Stopping strategy.");
				_hasWork = false;
				Stop();
			}
			else if (!hasPendingWork)
			{
				_hasWork = false;
			}
		}
		catch (Exception error)
		{
			LogError($"Failed to process management loop: {error.Message}");
		}
		finally
		{
			Interlocked.Exchange(ref _isProcessing, 0);
		}
	}

	private bool ShouldClosePosition(decimal volume, Position position)
	{
		if (volume > 0m && !CloseBuyPositions)
			return false;

		if (volume < 0m && !CloseSellPositions)
			return false;

		var profit = position.PnL ?? 0m;

		if (CloseOnlyProfitable && profit < 0m)
			return false;

		if (CloseOnlyLosing && profit > 0m)
			return false;

		return true;
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

	private static bool MatchesStrategyFilter(Position position, bool filterEnabled, string filter)
	{
		if (!filterEnabled)
			return true;

		var strategyId = TryGetStrategyId(position);
		return string.Equals(strategyId, filter, StringComparison.Ordinal);
	}

	private static bool MatchesStrategyFilter(Order order, bool filterEnabled, string filter)
	{
		if (!filterEnabled)
			return true;

		var strategyId = TryGetStrategyId(order);
		return string.Equals(strategyId, filter, StringComparison.Ordinal);
	}

	private static string TryGetStrategyId(Position position)
	{
		if (PositionStrategyIdProperty == null)
			return null;

		var value = PositionStrategyIdProperty.GetValue(position);
		return value?.ToString();
	}

	private static string TryGetStrategyId(Order order)
	{
		if (OrderStrategyIdProperty == null)
			return null;

		var value = OrderStrategyIdProperty.GetValue(order);
		return value?.ToString();
	}
}
