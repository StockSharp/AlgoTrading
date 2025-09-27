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

/// <summary>
/// Conversion of the MetaTrader expert "XP Trade Manager" that supervises open trades.
/// The strategy does not open new positions; it manages existing ones by applying stop-loss,
/// take-profit, trailing-stop, and breakeven rules using level1 updates.
/// </summary>
public class XPTradeManagerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<decimal> _breakevenActivationPoints;
	private readonly StrategyParam<decimal> _breakevenLevelPoints;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStartPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<decimal> _trailingDistancePoints;
	private readonly StrategyParam<bool> _trailingEndAtBreakeven;
	private readonly StrategyParam<bool> _stealthMode;

	private decimal? _currentBid;
	private decimal? _currentAsk;

	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;

	private Order _longStopOrder;
	private Order _longTakeOrder;
	private Order _shortStopOrder;
	private Order _shortTakeOrder;
	private Order _longExitOrder;
	private Order _shortExitOrder;

	/// <summary>
	/// Initializes a new instance of the <see cref="XPTradeManagerStrategy"/> class.
	/// </summary>
	public XPTradeManagerStrategy()
	{
		_stopLossPoints = Param(nameof(StopLossPoints), 20m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (points)", "Distance in price points used for the protective stop", "Stops");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 40m)
			.SetNotNegative()
			.SetDisplay("Take Profit (points)", "Distance in price points used for the profit target", "Stops");

		_useBreakEven = Param(nameof(UseBreakEven), true)
			.SetDisplay("Use Breakeven", "Move the stop-loss to breakeven after a configurable gain", "Breakeven");

		_breakevenActivationPoints = Param(nameof(BreakevenActivationPoints), 50m)
			.SetNotNegative()
			.SetDisplay("Breakeven Activation (points)", "Profit in price points required before moving to breakeven", "Breakeven");

		_breakevenLevelPoints = Param(nameof(BreakevenLevelPoints), 10m)
			.SetNotNegative()
			.SetDisplay("Breakeven Level (points)", "Offset in points used once breakeven is triggered", "Breakeven");

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
			.SetDisplay("Use Trailing Stop", "Enable the incremental trailing stop engine", "Trailing");

		_trailingStartPoints = Param(nameof(TrailingStartPoints), 10m)
			.SetNotNegative()
			.SetDisplay("Trailing Start (points)", "Profit in points required before the trailing stop activates", "Trailing");

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Step (points)", "Profit increment in points required to move the stop again", "Trailing");

		_trailingDistancePoints = Param(nameof(TrailingDistancePoints), 15m)
			.SetNotNegative()
			.SetDisplay("Trailing Distance (points)", "Distance between price and trailing stop once active", "Trailing");

		_trailingEndAtBreakeven = Param(nameof(TrailingEndAtBreakeven), false)
			.SetDisplay("End Trailing at Breakeven", "Clamp trailing stop to the breakeven level", "Trailing");

		_stealthMode = Param(nameof(StealthMode), false)
			.SetDisplay("Stealth Mode", "When enabled the strategy closes trades virtually instead of placing protective orders", "Stops");
	}

	/// <summary>
	/// Stop-loss distance expressed in instrument points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in instrument points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Enables the breakeven logic.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Profit in points required before breakeven activates.
	/// </summary>
	public decimal BreakevenActivationPoints
	{
		get => _breakevenActivationPoints.Value;
		set => _breakevenActivationPoints.Value = value;
	}

	/// <summary>
	/// Points kept as buffer when the stop is moved to breakeven.
	/// </summary>
	public decimal BreakevenLevelPoints
	{
		get => _breakevenLevelPoints.Value;
		set => _breakevenLevelPoints.Value = value;
	}

	/// <summary>
	/// Enables the trailing stop manager.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Profit in points required before the trailing engine activates.
	/// </summary>
	public decimal TrailingStartPoints
	{
		get => _trailingStartPoints.Value;
		set => _trailingStartPoints.Value = value;
	}

	/// <summary>
	/// Profit increment (in points) required before the trailing stop moves again.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Distance in points maintained between price and the trailing stop once active.
	/// </summary>
	public decimal TrailingDistancePoints
	{
		get => _trailingDistancePoints.Value;
		set => _trailingDistancePoints.Value = value;
	}

	/// <summary>
	/// When enabled the trailing stop never surpasses the breakeven level.
	/// </summary>
	public bool TrailingEndAtBreakeven
	{
		get => _trailingEndAtBreakeven.Value;
		set => _trailingEndAtBreakeven.Value = value;
	}

	/// <summary>
	/// Controls whether the strategy places visible protective orders or uses virtual management.
	/// </summary>
	public bool StealthMode
	{
		get => _stealthMode.Value;
		set => _stealthMode.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
	return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
	base.OnReseted();

	_currentBid = null;
	_currentAsk = null;

	ResetLongState();
	ResetShortState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	StartProtection();

	SubscribeLevel1()
	.Bind(ProcessLevel1)
	.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
	if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
	_currentBid = (decimal)bid;

	if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
	_currentAsk = (decimal)ask;

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	var pointSize = GetPointSize();
	if (pointSize <= 0m)
	return;

	ManageLongPosition(pointSize);
	ManageShortPosition(pointSize);
	}

	private void ManageLongPosition(decimal pointSize)
	{
	if (Position <= 0m)
	{
	ResetLongState();
	return;
	}

	if (_currentBid is not decimal bid || bid <= 0m)
	return;

	var entryPrice = Position.AveragePrice ?? _longEntryPrice;
	if (entryPrice is not decimal price || price <= 0m)
	return;

	_longEntryPrice = price;

	var stopCandidate = CalculateLongStop(price, bid, pointSize);
	var takeCandidate = CalculateLongTake(price, pointSize);

	_longStopPrice = SelectStopPrice(_longStopPrice, stopCandidate, true);
	_longTakePrice = takeCandidate;

	var volume = Math.Abs(Position);
	if (volume <= 0m)
	return;

	if (StealthMode)
	{
	CancelAndClear(ref _longStopOrder);
	CancelAndClear(ref _longTakeOrder);

	if (_longTakePrice is decimal takePrice && bid >= takePrice)
	{
	TryCloseLong(volume);
	return;
	}

	if (_longStopPrice is decimal stopPrice && bid <= stopPrice)
	TryCloseLong(volume);
	}
	else
	{
	UpdateLongProtectionOrders(volume);

	if (_longTakePrice is decimal takePrice && bid >= takePrice && NeedsExitOrder(_longExitOrder))
	TryCloseLong(volume);

	if (_longStopPrice is decimal stopPrice && bid <= stopPrice && NeedsExitOrder(_longExitOrder))
	TryCloseLong(volume);
	}
	}

	private void ManageShortPosition(decimal pointSize)
	{
	if (Position >= 0m)
	{
	ResetShortState();
	return;
	}

	if (_currentAsk is not decimal ask || ask <= 0m)
	return;

	var entryPrice = Position.AveragePrice ?? _shortEntryPrice;
	if (entryPrice is not decimal price || price <= 0m)
	return;

	_shortEntryPrice = price;

	var stopCandidate = CalculateShortStop(price, ask, pointSize);
	var takeCandidate = CalculateShortTake(price, pointSize);

	_shortStopPrice = SelectStopPrice(_shortStopPrice, stopCandidate, false);
	_shortTakePrice = takeCandidate;

	var volume = Math.Abs(Position);
	if (volume <= 0m)
	return;

	if (StealthMode)
	{
	CancelAndClear(ref _shortStopOrder);
	CancelAndClear(ref _shortTakeOrder);

	if (_shortTakePrice is decimal takePrice && ask <= takePrice)
	{
	TryCloseShort(volume);
	return;
	}

	if (_shortStopPrice is decimal stopPrice && ask >= stopPrice)
	TryCloseShort(volume);
	}
	else
	{
	UpdateShortProtectionOrders(volume);

	if (_shortTakePrice is decimal takePrice && ask <= takePrice && NeedsExitOrder(_shortExitOrder))
	TryCloseShort(volume);

	if (_shortStopPrice is decimal stopPrice && ask >= stopPrice && NeedsExitOrder(_shortExitOrder))
	TryCloseShort(volume);
	}
	}

	private decimal? CalculateLongStop(decimal entryPrice, decimal bid, decimal pointSize)
	{
	decimal? candidate = null;

	if (StopLossPoints > 0m)
	candidate = entryPrice - StopLossPoints * pointSize;

	if (UseTrailingStop && TrailingStepPoints > 0m)
	{
	var profitPoints = (bid - entryPrice) / pointSize;
	if (profitPoints > TrailingStartPoints)
	{
	var multi = (int)decimal.Floor(profitPoints / TrailingStepPoints);
	if (multi > 0)
	{
	var trailing = entryPrice + multi * TrailingStepPoints * pointSize - TrailingDistancePoints * pointSize;
	if (TrailingEndAtBreakeven)
	{
	var breakeven = entryPrice + BreakevenLevelPoints * pointSize;
	if (trailing > breakeven)
	trailing = breakeven;
	}

	if (candidate is null || trailing > candidate)
	candidate = trailing;
	}
	}
	}
	else if (UseBreakEven && BreakevenActivationPoints > 0m)
	{
	var profitPoints = (bid - entryPrice) / pointSize;
	if (profitPoints >= BreakevenActivationPoints)
	{
	var breakeven = entryPrice + BreakevenLevelPoints * pointSize;
	if (candidate is null || breakeven > candidate)
	candidate = breakeven;
	}
	}

	return candidate;
	}

	private decimal? CalculateShortStop(decimal entryPrice, decimal ask, decimal pointSize)
	{
	decimal? candidate = null;

	if (StopLossPoints > 0m)
	candidate = entryPrice + StopLossPoints * pointSize;

	if (UseTrailingStop && TrailingStepPoints > 0m)
	{
	var profitPoints = (entryPrice - ask) / pointSize;
	if (profitPoints > TrailingStartPoints)
	{
	var multi = (int)decimal.Floor(profitPoints / TrailingStepPoints);
	if (multi > 0)
	{
	var trailing = entryPrice - multi * TrailingStepPoints * pointSize + TrailingDistancePoints * pointSize;
	if (TrailingEndAtBreakeven)
	{
	var breakeven = entryPrice - BreakevenLevelPoints * pointSize;
	if (trailing < breakeven)
	trailing = breakeven;
	}

	if (candidate is null || trailing < candidate)
	candidate = trailing;
	}
	}
	}
	else if (UseBreakEven && BreakevenActivationPoints > 0m)
	{
	var profitPoints = (entryPrice - ask) / pointSize;
	if (profitPoints >= BreakevenActivationPoints)
	{
	var breakeven = entryPrice - BreakevenLevelPoints * pointSize;
	if (candidate is null || breakeven < candidate)
	candidate = breakeven;
	}
	}

	return candidate;
	}

	private decimal? CalculateLongTake(decimal entryPrice, decimal pointSize)
	{
	if (TakeProfitPoints <= 0m)
	return null;

	return entryPrice + TakeProfitPoints * pointSize;
	}

	private decimal? CalculateShortTake(decimal entryPrice, decimal pointSize)
	{
	if (TakeProfitPoints <= 0m)
	return null;

	return entryPrice - TakeProfitPoints * pointSize;
	}

	private void UpdateLongProtectionOrders(decimal volume)
	{
	if (StealthMode)
	{
	CancelAndClear(ref _longStopOrder);
	CancelAndClear(ref _longTakeOrder);
	return;
	}

	if (_longStopPrice is decimal stop && stop > 0m)
	ReplaceOrder(ref _longStopOrder, () => SellStop(volume, stop), stop, volume);
	else
	CancelAndClear(ref _longStopOrder);

	if (_longTakePrice is decimal take && take > 0m)
	ReplaceOrder(ref _longTakeOrder, () => SellLimit(volume, take), take, volume);
	else
	CancelAndClear(ref _longTakeOrder);
	}

	private void UpdateShortProtectionOrders(decimal volume)
	{
	if (StealthMode)
	{
	CancelAndClear(ref _shortStopOrder);
	CancelAndClear(ref _shortTakeOrder);
	return;
	}

	if (_shortStopPrice is decimal stop && stop > 0m)
	ReplaceOrder(ref _shortStopOrder, () => BuyStop(volume, stop), stop, volume);
	else
	CancelAndClear(ref _shortStopOrder);

	if (_shortTakePrice is decimal take && take > 0m)
	ReplaceOrder(ref _shortTakeOrder, () => BuyLimit(volume, take), take, volume);
	else
	CancelAndClear(ref _shortTakeOrder);
	}

	private void TryCloseLong(decimal volume)
	{
	if (!NeedsExitOrder(_longExitOrder))
	return;

	_longExitOrder = SellMarket(volume);
	}

	private void TryCloseShort(decimal volume)
	{
	if (!NeedsExitOrder(_shortExitOrder))
	return;

	_shortExitOrder = BuyMarket(volume);
	}

	private static decimal? SelectStopPrice(decimal? previous, decimal? candidate, bool shouldIncrease)
	{
	if (candidate is null)
	return previous;

	if (previous is null)
	return candidate;

	return shouldIncrease
	? Math.Max(previous.Value, candidate.Value)
	: Math.Min(previous.Value, candidate.Value);
	}

	private void ReplaceOrder(ref Order order, Func<Order> factory, decimal price, decimal volume)
	{
	if (order != null)
	{
	if (!IsFinalState(order) && order.State == OrderStates.Active && order.Price == price && order.Volume == volume)
	return;

	CancelAndClear(ref order);
	}

	order = factory();
	}

	private void CancelAndClear(ref Order order)
	{
	if (order == null)
	return;

	if (!IsFinalState(order))
	CancelOrder(order);

	order = null;
	}

	private static bool NeedsExitOrder(Order order)
	{
	return order == null || IsFinalState(order);
	}

	private static bool IsFinalState(Order order)
	{
	return order.State == OrderStates.Done
	|| order.State == OrderStates.Failed
	|| order.State == OrderStates.Cancelled;
	}

	private decimal GetPointSize()
	{
	var priceStep = Security?.PriceStep ?? 0m;
	if (priceStep <= 0m)
	return 0m;

	var decimals = Security?.Decimals ?? 0;
	var multiplier = (decimals == 3 || decimals == 5) ? 10m : 1m;
	return priceStep * multiplier;
	}

	private void ResetLongState()
	{
	_longStopPrice = null;
	_longTakePrice = null;
	_longEntryPrice = null;

	CancelAndClear(ref _longStopOrder);
	CancelAndClear(ref _longTakeOrder);
	CancelAndClear(ref _longExitOrder);
	}

	private void ResetShortState()
	{
	_shortStopPrice = null;
	_shortTakePrice = null;
	_shortEntryPrice = null;

	CancelAndClear(ref _shortStopOrder);
	CancelAndClear(ref _shortTakeOrder);
	CancelAndClear(ref _shortExitOrder);
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
	base.OnPositionChanged(delta);

	if (Position <= 0m)
	_longExitOrder = null;

	if (Position >= 0m)
	_shortExitOrder = null;
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
	base.OnOrderChanged(order);

	if (order == _longStopOrder && IsFinalState(order))
	_longStopOrder = null;
	else if (order == _longTakeOrder && IsFinalState(order))
	_longTakeOrder = null;
	else if (order == _shortStopOrder && IsFinalState(order))
	_shortStopOrder = null;
	else if (order == _shortTakeOrder && IsFinalState(order))
	_shortTakeOrder = null;
	else if (order == _longExitOrder && IsFinalState(order))
	_longExitOrder = null;
	else if (order == _shortExitOrder && IsFinalState(order))
	_shortExitOrder = null;
	}
}

