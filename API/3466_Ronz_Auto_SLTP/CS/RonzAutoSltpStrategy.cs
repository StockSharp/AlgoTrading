namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Port of the "Ronz Auto SLTP" trade management expert.
/// Automatically assigns stop-loss, take-profit, profit lock, and trailing rules to open positions.
/// </summary>
public class RonzAutoSltpStrategy : Strategy
{
	private static readonly Level1Fields? StopLevelField = TryGetField("StopLevel")
	?? TryGetField("MinStopPrice")
	?? TryGetField("StopPrice")
	?? TryGetField("StopDistance");

	private static readonly Level1Fields? FreezeLevelField = TryGetField("FreezeLevel")
	?? TryGetField("FreezePrice")
	?? TryGetField("FreezeDistance");

	private readonly StrategyParam<bool> _manageAllSecurities;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<bool> _useServerStops;
	private readonly StrategyParam<bool> _enableLockProfit;
	private readonly StrategyParam<int> _lockProfitAfterPips;
	private readonly StrategyParam<int> _profitLockPips;
	private readonly StrategyParam<TrailingMode> _trailingMode;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<bool> _enableAlerts;

	// Track protective state per security so that trailing rules remain independent.
	private readonly Dictionary<Security, SecurityState> _securityStates = new();
	// Remember which securities already have active Level1 subscriptions.
	private readonly HashSet<Security> _subscribed = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="RonzAutoSltpStrategy"/> class.
	/// </summary>
	public RonzAutoSltpStrategy()
	{
		_manageAllSecurities = Param(nameof(ManageAllSecurities), true)
		.SetDisplay("Manage All Securities", "Track every open position in the portfolio instead of only the attached symbol.", "General");

		_takeProfitPips = Param(nameof(TakeProfitPips), 550)
		.SetNotNegative()
		.SetDisplay("Take Profit (pips)", "Distance in MetaTrader pips used for the take-profit target.", "Risk")
		.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 350)
		.SetNotNegative()
		.SetDisplay("Stop Loss (pips)", "Distance in MetaTrader pips used for the protective stop-loss.", "Risk")
		.SetCanOptimize(true);

		_useServerStops = Param(nameof(UseServerStops), true)
		.SetDisplay("Use Server Stops", "Send actual stop and limit orders instead of closing positions virtually.", "Execution");

		_enableLockProfit = Param(nameof(EnableLockProfit), true)
		.SetDisplay("Enable Profit Lock", "Raise the stop to a positive level after the lock threshold is reached.", "Trailing");

		_lockProfitAfterPips = Param(nameof(LockProfitAfterPips), 100)
		.SetNotNegative()
		.SetDisplay("Lock After (pips)", "Profit in pips required before the stop-lock engages.", "Trailing")
		.SetCanOptimize(true);

		_profitLockPips = Param(nameof(ProfitLockPips), 60)
		.SetNotNegative()
		.SetDisplay("Locked Profit (pips)", "Profit preserved once the lock is active.", "Trailing")
		.SetCanOptimize(true);

		_trailingMode = Param(nameof(TrailingStopMode), TrailingMode.Classic)
		.SetDisplay("Trailing Mode", "Style of the trailing stop used after the lock threshold.", "Trailing");

		_trailingStopPips = Param(nameof(TrailingStopPips), 50)
		.SetNotNegative()
		.SetDisplay("Trailing Distance (pips)", "Distance maintained by the trailing stop when active.", "Trailing")
		.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 10)
		.SetNotNegative()
		.SetDisplay("Trailing Step (pips)", "Increment applied by step-based trailing modes.", "Trailing")
		.SetCanOptimize(true);

		_enableAlerts = Param(nameof(EnableAlerts), false)
		.SetDisplay("Enable Alerts", "Log informational messages when virtual protection closes a position.", "General");
	}

	/// <summary>
	/// Manage positions for every security in the portfolio.
	/// </summary>
	public bool ManageAllSecurities
	{
		get => _manageAllSecurities.Value;
		set => _manageAllSecurities.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in MetaTrader pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in MetaTrader pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Place real stop and limit orders on the broker side.
	/// </summary>
	public bool UseServerStops
	{
		get => _useServerStops.Value;
		set => _useServerStops.Value = value;
	}

	/// <summary>
	/// Enable the profit-locking behaviour.
	/// </summary>
	public bool EnableLockProfit
	{
		get => _enableLockProfit.Value;
		set => _enableLockProfit.Value = value;
	}

	/// <summary>
	/// Profit threshold that enables the lock logic.
	/// </summary>
	public int LockProfitAfterPips
	{
		get => _lockProfitAfterPips.Value;
		set => _lockProfitAfterPips.Value = value;
	}

	/// <summary>
	/// Profit preserved once the lock threshold is reached.
	/// </summary>
	public int ProfitLockPips
	{
		get => _profitLockPips.Value;
		set => _profitLockPips.Value = value;
	}

	/// <summary>
	/// Trailing stop style.
	/// </summary>
	public TrailingMode TrailingStopMode
	{
		get => _trailingMode.Value;
		set => _trailingMode.Value = value;
	}

	/// <summary>
	/// Trailing distance in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Step size used by step-based trailing modes.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Write informative alerts when virtual protection closes a trade.
	/// </summary>
	public bool EnableAlerts
	{
		get => _enableAlerts.Value;
		set => _enableAlerts.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
		{
			yield return (Security, DataType.Level1);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		foreach (var state in _securityStates.Values)
		{
			state.Dispose(this);
		}

		_securityStates.Clear();
		_subscribed.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		if (Security != null)
		{
			SubscribeToSecurity(Security);
		}

		if (ManageAllSecurities)
		{
			EvaluateAllPositions();
		}
		else
		{
			EvaluateSecurity(Security);
		}
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		foreach (var state in _securityStates.Values)
		{
			state.Dispose(this);
		}

		_securityStates.Clear();
		_subscribed.Clear();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var security = trade?.Order.Security ?? Security;
		if (security != null)
		{
			SubscribeToSecurity(security);
			EvaluateSecurity(security);
		}

		if (ManageAllSecurities)
		{
			EvaluateAllPositions();
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (ManageAllSecurities)
		{
			EvaluateAllPositions();
		}
		else
		{
			EvaluateSecurity(Security);
		}
	}

	// Subscribe to Level1 data and allocate a state container for the requested security.
	private void SubscribeToSecurity(Security security)
	{
		if (security == null)
		{
			return;
		}

		if (!_securityStates.ContainsKey(security))
		{
			_securityStates.Add(security, new SecurityState(security));
		}

		if (_subscribed.Contains(security))
		{
			return;
		}

		SubscribeLevel1(security)
		.Bind(message => ProcessLevel1(security, message))
		.Start();

		_subscribed.Add(security);
	}

	private void ProcessLevel1(Security security, Level1ChangeMessage message)
	{
		if (!_securityStates.TryGetValue(security, out var state))
		{
			return;
		}

		state.UpdateLevel1(message);

		if (ManageAllSecurities)
		{
			EvaluateAllPositions();
		}
		else
		{
			EvaluateSecurity(security);
		}
	}

	private void EvaluateAllPositions()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
		{
			return;
		}

		foreach (var position in portfolio.Positions.ToArray())
		{
			var security = position.Security;
			if (security == null)
			continue;

			SubscribeToSecurity(security);
			EvaluateSingleSecurity(position, security);
		}
	}

	private void EvaluateSecurity(Security security)
	{
		if (security == null)
		{
			return;
		}

		var portfolio = Portfolio;
		if (portfolio == null)
		{
			return;
		}

		var position = portfolio.Positions.FirstOrDefault(p => p.Security == security);
		EvaluateSingleSecurity(position, security);
	}

	// Core routine that evaluates stops, takes, and trailing logic for a single security.
	private void EvaluateSingleSecurity(Position position, Security security)
	{
		if (!_securityStates.TryGetValue(security, out var state))
		return;

		state.RefreshScale();

		// Determine the MetaTrader-style point value used for pip calculations.
		var point = state.PointValue;
		if (point <= 0m)
		point = 0.0001m;

		var priceStep = state.PriceStep;

		var volume = position?.CurrentValue ?? 0m;
		if (volume == 0m)
		{
			state.Reset(this);
			return;
		}

		var isLong = volume > 0m;
		var absVolume = state.NormalizeVolume(Math.Abs(volume));
		// Abort if the normalized volume cannot be traded on the venue.
		if (absVolume <= 0m)
		{
			state.Reset(this);
			return;
		}

		var entryPrice = position?.AveragePrice ?? 0m;
		if (entryPrice <= 0m)
		return;

		var currentPrice = state.GetReferencePrice(isLong);
		if (currentPrice == null)
		return;

		// Translate the floating profit into pips to reuse the MQ5 thresholds.
		var profitPips = point > 0m
		? (isLong ? (currentPrice.Value - entryPrice) / point : (entryPrice - currentPrice.Value) / point)
		: 0m;

		var minDistance = state.GetMinimalDistance();

		decimal? baseStop = null;
		if (StopLossPips > 0)
		{
			var stopDistance = StopLossPips * point + minDistance;
			baseStop = isLong
			? entryPrice - stopDistance
			: entryPrice + stopDistance;
		}

		decimal? takePrice = null;
		if (TakeProfitPips > 0)
		{
			var takeDistance = TakeProfitPips * point + minDistance;
			takePrice = isLong
			? entryPrice + takeDistance
			: entryPrice - takeDistance;
		}

		var previousStop = state.GetStoredStop(isLong) ?? baseStop ?? entryPrice;

		var candidates = new List<decimal>();
		// Start with the previously applied stop so that we never loosen existing protection.
		if (previousStop > 0m)
		candidates.Add(previousStop);
		if (baseStop != null)
		candidates.Add(baseStop.Value);

		if (EnableLockProfit && LockProfitAfterPips > 0 && ProfitLockPips > 0 && profitPips >= LockProfitAfterPips)
		{
			var lockPrice = isLong
			? entryPrice + ProfitLockPips * point
			: entryPrice - ProfitLockPips * point;
			candidates.Add(lockPrice);
		}

		var trailingRequired = TrailingStopPips > 0 &&
		((LockProfitAfterPips == 0) || (profitPips >= LockProfitAfterPips + TrailingStopPips));
		if (trailingRequired)
		{
			var trailing = CalculateTrailingStop(state, isLong, currentPrice.Value, previousStop, point, priceStep);
			if (trailing != null)
			candidates.Add(trailing.Value);
		}

		decimal? finalStop = null;
		if (candidates.Count > 0)
		{
			finalStop = isLong ? candidates.Max() : candidates.Min();
			finalStop = state.NormalizePrice(finalStop.Value);

			var buffer = point;
			if (buffer <= 0m)
			buffer = priceStep;

			if (buffer > 0m)
			{
				if (isLong && finalStop.Value >= currentPrice.Value)
				finalStop = currentPrice.Value - buffer;
				else if (!isLong && finalStop.Value <= currentPrice.Value)
				finalStop = currentPrice.Value + buffer;
			}
		}

		if (finalStop != null)
		{
			if (isLong && finalStop.Value < entryPrice - StopLossPips * point)
			finalStop = entryPrice - StopLossPips * point;
			else if (!isLong && finalStop.Value > entryPrice + StopLossPips * point)
			finalStop = entryPrice + StopLossPips * point;
		}

		UpdateProtection(security, state, isLong, absVolume, finalStop, takePrice, currentPrice.Value);
	}

	// Apply either server-side or virtual protection depending on the user preference.
	private void UpdateProtection(Security security, SecurityState state, bool isLong, decimal volume, decimal? stopPrice, decimal? takePrice, decimal currentPrice)
	{
		if (UseServerStops)
		{
			UpdateStopOrder(security, state, isLong, volume, stopPrice);
			UpdateTakeOrder(security, state, isLong, volume, takePrice);
		}
		else
		{
			state.StoreVirtualLevels(isLong, stopPrice, takePrice);

			if (stopPrice != null && ShouldTriggerStop(isLong, currentPrice, stopPrice.Value))
			{
				ClosePosition(security, volume, isLong, true);
				return;
			}

			if (takePrice != null && ShouldTriggerTake(isLong, currentPrice, takePrice.Value))
			ClosePosition(security, volume, isLong, false);
		}
	}

	private void UpdateStopOrder(Security security, SecurityState state, bool isLong, decimal volume, decimal? targetPrice)
	{
		state.StoreVirtualLevels(isLong, targetPrice, state.GetStoredTake(isLong));

		if (targetPrice == null || targetPrice <= 0m)
		{
			state.CancelStop(this);
			return;
		}

		var normalized = state.NormalizePrice(targetPrice.Value);
		if (normalized <= 0m)
		{
			state.CancelStop(this);
			return;
		}

		var order = state.StopOrder;
		if (order == null || order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		{
			state.CancelStop(this);
			order = isLong
			? SellStop(volume, normalized, security)
			: BuyStop(volume, normalized, security);
			state.StopOrder = order;
		}
		else if (order.Price != normalized || order.Volume != volume)
		{
			ReRegisterOrder(order, normalized, volume);
		}

		state.StoreStop(isLong, normalized);
	}

	private void UpdateTakeOrder(Security security, SecurityState state, bool isLong, decimal volume, decimal? targetPrice)
	{
		state.StoreVirtualLevels(isLong, state.GetStoredStop(isLong), targetPrice);

		if (targetPrice == null || targetPrice <= 0m)
		{
			state.CancelTake(this);
			return;
		}

		var normalized = state.NormalizePrice(targetPrice.Value);
		if (normalized <= 0m)
		{
			state.CancelTake(this);
			return;
		}

		var order = state.TakeProfitOrder;
		if (order == null || order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		{
			state.CancelTake(this);
			order = isLong
			? SellLimit(volume, normalized, security)
			: BuyLimit(volume, normalized, security);
			state.TakeProfitOrder = order;
		}
		else if (order.Price != normalized || order.Volume != volume)
		{
			ReRegisterOrder(order, normalized, volume);
		}

		state.StoreTake(isLong, normalized);
	}

	private void ClosePosition(Security security, decimal volume, bool isLong, bool byStop)
	{
		if (isLong)
		SellMarket(volume, security);
		else
		BuyMarket(volume, security);

		if (EnableAlerts)
		{
			var reason = byStop ? "virtual stop" : "virtual take-profit";
			LogInfo($"{security.Id}: Position closed by {reason}.");
		}
	}

	private static bool ShouldTriggerStop(bool isLong, decimal currentPrice, decimal stopPrice)
	{
		return isLong ? currentPrice <= stopPrice : currentPrice >= stopPrice;
	}

	private static bool ShouldTriggerTake(bool isLong, decimal currentPrice, decimal takePrice)
	{
		return isLong ? currentPrice >= takePrice : currentPrice <= takePrice;
	}

	// Recreate the MQ5 trailing stop algorithms using current price information.
	private decimal? CalculateTrailingStop(SecurityState state, bool isLong, decimal currentPrice, decimal previousStop, decimal point, decimal priceStep)
	{
		var distance = TrailingStopPips * point;
		if (distance <= 0m)
		return null;

		var existingStop = previousStop;
		if (existingStop <= 0m)
		existingStop = isLong ? currentPrice - distance : currentPrice + distance;

		var movement = isLong ? currentPrice - existingStop : existingStop - currentPrice;
		if (movement < distance)
		return null;

		var stepDistance = TrailingStepPips * point;

		decimal candidate;
		switch (TrailingStopMode)
		{
			case TrailingMode.None:
			return null;
			case TrailingMode.Classic:
			candidate = isLong ? currentPrice - distance : currentPrice + distance;
			break;
			case TrailingMode.StepDistance:
			{
				var adjusted = distance - stepDistance;
				if (adjusted <= 0m)
				adjusted = distance;
				candidate = isLong ? currentPrice - adjusted : currentPrice + adjusted;
				break;
			}
			case TrailingMode.StepByStep:
			{
				if (stepDistance <= 0m)
				return null;
				candidate = isLong ? existingStop + stepDistance : existingStop - stepDistance;
				break;
			}
			default:
			return null;
		}

		if (priceStep > 0m)
		{
			var steps = Math.Round(candidate / priceStep, MidpointRounding.AwayFromZero);
			candidate = steps * priceStep;
		}

		return candidate;
	}

	private static Level1Fields? TryGetField(string name)
	{
		return Enum.TryParse(name, out Level1Fields field) ? field : null;
	}

	private static decimal? ToDecimal(object value)
	{
		return value switch
		{
			decimal dec => dec,
			double dbl => (decimal)dbl,
			float fl => (decimal)fl,
			long l => l,
			int i => i,
			short s => s,
			byte b => b,
			null => null,
			IConvertible convertible => Convert.ToDecimal(convertible, CultureInfo.InvariantCulture),
			_ => null,
		};
	}

	private static decimal CalculatePointValue(Security security)
	{
		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
		{
			var decimals = security.Decimals;
			if (decimals != null && decimals.Value > 0)
			step = (decimal)Math.Pow(10, -decimals.Value);
		}

		if (step <= 0m)
		step = 0.0001m;

		var multiplier = 1m;
		var digits = security.Decimals;
		if (digits != null && (digits.Value == 3 || digits.Value == 5))
		multiplier = 10m;

		return step * multiplier;
	}

	private static decimal GetPriceStep(Security security)
	{
		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
		{
			var decimals = security.Decimals;
			if (decimals != null && decimals.Value > 0)
			step = (decimal)Math.Pow(10, -decimals.Value);
		}

		return step;
	}

	/// <summary>
	/// Trailing stop options that match the MetaTrader implementation.
	/// </summary>
	public enum TrailingMode
	{
		None,
		Classic,
		StepDistance,
		StepByStep,
	}

	// Holds cached market data and working orders for a single security.
	private sealed class SecurityState
	{
		private readonly Security _security;

		public SecurityState(Security security)
		{
			_security = security;
			RefreshScale();
		}

		public decimal? BestBid { get; private set; }
		public decimal? BestAsk { get; private set; }
		public decimal? StopLevel { get; private set; }
		public decimal? FreezeLevel { get; private set; }
		public decimal PointValue { get; private set; }
		public decimal PriceStep { get; private set; }
		public decimal VolumeStep { get; private set; }
		public decimal? MinVolume { get; private set; }
		public decimal? MaxVolume { get; private set; }
		public Order StopOrder { get; set; }
		public Order TakeProfitOrder { get; set; }
		public decimal? VirtualLongStop { get; private set; }
		public decimal? VirtualShortStop { get; private set; }
		public decimal? VirtualLongTake { get; private set; }
		public decimal? VirtualShortTake { get; private set; }

		public void UpdateLevel1(Level1ChangeMessage message)
		{
			if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid)
			BestBid = bid;

			if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask)
			BestAsk = ask;

			if (StopLevelField is Level1Fields stopField && message.Changes.TryGetValue(stopField, out var stopValue))
			StopLevel = ToDecimal(stopValue);

			if (FreezeLevelField is Level1Fields freezeField && message.Changes.TryGetValue(freezeField, out var freezeValue))
			FreezeLevel = ToDecimal(freezeValue);
		}

		public void RefreshScale()
		{
			PriceStep = GetPriceStep(_security);
			PointValue = CalculatePointValue(_security);
			VolumeStep = _security.VolumeStep ?? 0m;
			MinVolume = _security.MinVolume;
			MaxVolume = _security.MaxVolume;
		}

		public decimal NormalizePrice(decimal price)
		{
			var step = PriceStep;
			if (step <= 0m)
			return price;

			var steps = Math.Round(price / step, MidpointRounding.AwayFromZero);
			return steps * step;
		}

		public decimal NormalizeVolume(decimal volume)
		{
			var step = VolumeStep;
			if (step > 0m)
			{
				var steps = Math.Round(volume / step, MidpointRounding.AwayFromZero);
				volume = steps * step;
			}

			if (MinVolume is decimal min && volume < min)
			volume = min;

			if (MaxVolume is decimal max && volume > max)
			volume = max;

			return volume;
		}

		public decimal? GetReferencePrice(bool isLong)
		{
			if (isLong)
			return BestBid ?? _security.LastTrade?.Price;

			return BestAsk ?? _security.LastTrade?.Price;
		}

		public decimal GetMinimalDistance()
		{
			var level = Math.Max(StopLevel ?? 0m, FreezeLevel ?? 0m);

			if (level <= 0m && BestBid != null && BestAsk != null)
			{
				var spread = Math.Abs(BestAsk.Value - BestBid.Value);
				level = spread > 0m ? spread * 3m : 0m;
			}

			return level > 0m ? level * 1.1m : 0m;
		}

		public void StoreStop(bool isLong, decimal? value)
		{
			if (isLong)
			VirtualLongStop = value;
			else
			VirtualShortStop = value;
		}

		public void StoreTake(bool isLong, decimal? value)
		{
			if (isLong)
			VirtualLongTake = value;
			else
			VirtualShortTake = value;
		}

		public void StoreVirtualLevels(bool isLong, decimal? stop, decimal? take)
		{
			StoreStop(isLong, stop);
			StoreTake(isLong, take);
		}

		public decimal? GetStoredStop(bool isLong)
		{
			return isLong ? VirtualLongStop : VirtualShortStop;
		}

		public decimal? GetStoredTake(bool isLong)
		{
			return isLong ? VirtualLongTake : VirtualShortTake;
		}

		public void CancelStop(RonzAutoSltpStrategy owner)
		{
			if (StopOrder == null)
			return;

			if (StopOrder.State is OrderStates.Active or OrderStates.Pending)
			owner.CancelOrder(StopOrder);

			StopOrder = null;
		}

		public void CancelTake(RonzAutoSltpStrategy owner)
		{
			if (TakeProfitOrder == null)
			return;

			if (TakeProfitOrder.State is OrderStates.Active or OrderStates.Pending)
			owner.CancelOrder(TakeProfitOrder);

			TakeProfitOrder = null;
		}

		public void Reset(RonzAutoSltpStrategy owner)
		{
			CancelStop(owner);
			CancelTake(owner);
			VirtualLongStop = null;
			VirtualShortStop = null;
			VirtualLongTake = null;
			VirtualShortTake = null;
		}

		public void Dispose(RonzAutoSltpStrategy owner)
		{
			Reset(owner);
		}
	}
}
