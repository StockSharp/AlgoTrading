using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy converted from the MQL Expert NEWS robot.
/// Places symmetrical stop orders around the market price and manages trailing logic.
/// </summary>
public class ExpertNewsStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<int> _trailingStopTicks;
	private readonly StrategyParam<int> _trailingStartTicks;
	private readonly StrategyParam<int> _trailingStepTicks;
	private readonly StrategyParam<bool> _useBreakEven;
	private readonly StrategyParam<int> _breakEvenProfitTicks;
	private readonly StrategyParam<int> _entryOffsetTicks;
	private readonly StrategyParam<int> _orderRefreshSeconds;
	private readonly StrategyParam<int> _minimumStopTicks;

	private decimal? _bestBid;
	private decimal? _bestAsk;
	private Order _entryBuyStopOrder;
	private Order _entrySellStopOrder;
	private DateTimeOffset? _buyOrderLastUpdate;
	private DateTimeOffset? _sellOrderLastUpdate;

	private Order _exitStopOrder;
	private Order _exitTakeProfitOrder;
	private decimal _avgEntryPrice;
	private decimal _signedPosition;
	private decimal _lastStopPrice;

	/// <summary>
	/// Stop loss distance in ticks.
	/// </summary>
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Take profit distance in ticks.
	/// </summary>
	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in ticks.
	/// </summary>
	public int TrailingStopTicks
	{
		get => _trailingStopTicks.Value;
		set => _trailingStopTicks.Value = value;
	}

	/// <summary>
	/// Minimum profit in ticks before the trailing stop activates.
	/// </summary>
	public int TrailingStartTicks
	{
		get => _trailingStartTicks.Value;
		set => _trailingStartTicks.Value = value;
	}

	/// <summary>
	/// Minimum improvement in ticks when updating the trailing stop.
	/// </summary>
	public int TrailingStepTicks
	{
		get => _trailingStepTicks.Value;
		set => _trailingStepTicks.Value = value;
	}

	/// <summary>
	/// Enables break-even protection when true.
	/// </summary>
	public bool UseBreakEven
	{
		get => _useBreakEven.Value;
		set => _useBreakEven.Value = value;
	}

	/// <summary>
	/// Profit buffer in ticks when moving the stop to break-even.
	/// </summary>
	public int BreakEvenProfitTicks
	{
		get => _breakEvenProfitTicks.Value;
		set => _breakEvenProfitTicks.Value = value;
	}

	/// <summary>
	/// Distance in ticks between the current price and new entry stop orders.
	/// </summary>
	public int EntryOffsetTicks
	{
		get => _entryOffsetTicks.Value;
		set => _entryOffsetTicks.Value = value;
	}

	/// <summary>
	/// Seconds between automatic entry order price refresh attempts.
	/// </summary>
	public int OrderRefreshSeconds
	{
		get => _orderRefreshSeconds.Value;
		set => _orderRefreshSeconds.Value = value;
	}

	/// <summary>
	/// Minimum stop distance required by the venue in ticks.
	/// </summary>
	public int MinimumStopTicks
	{
		get => _minimumStopTicks.Value;
		set => _minimumStopTicks.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ExpertNewsStrategy()
	{
		_stopLossTicks = Param(nameof(StopLossTicks), 10)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss Ticks", "Stop loss distance in ticks", "Risk Management");

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 50)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit Ticks", "Take profit distance in ticks", "Risk Management");

		_trailingStopTicks = Param(nameof(TrailingStopTicks), 10)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Stop Ticks", "Trailing stop distance in ticks", "Risk Management");

		_trailingStartTicks = Param(nameof(TrailingStartTicks), 0)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Start Ticks", "Profit needed before trailing activates", "Risk Management");

		_trailingStepTicks = Param(nameof(TrailingStepTicks), 2)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Step Ticks", "Minimum improvement for trailing updates", "Risk Management");

		_useBreakEven = Param(nameof(UseBreakEven), false)
			.SetDisplay("Use Break Even", "Enable break-even stop adjustment", "Risk Management");

		_breakEvenProfitTicks = Param(nameof(BreakEvenProfitTicks), 0)
			.SetGreaterOrEqualZero()
			.SetDisplay("Break Even Profit Ticks", "Profit buffer when moving stop to break-even", "Risk Management");

		_entryOffsetTicks = Param(nameof(EntryOffsetTicks), 10)
			.SetGreaterOrEqualZero()
			.SetDisplay("Entry Offset Ticks", "Distance from price for new stop orders", "Entries");

		_orderRefreshSeconds = Param(nameof(OrderRefreshSeconds), 300)
			.SetGreaterOrEqualZero()
			.SetDisplay("Order Refresh Seconds", "Delay before refreshing pending orders", "Entries");

		_minimumStopTicks = Param(nameof(MinimumStopTicks), 0)
			.SetGreaterOrEqualZero()
			.SetDisplay("Minimum Stop Ticks", "Minimum distance allowed for stops", "Risk Management");

		Volume = 0.1m;
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

		_bestBid = null;
		_bestAsk = null;
		_entryBuyStopOrder = null;
		_entrySellStopOrder = null;
		_buyOrderLastUpdate = null;
		_sellOrderLastUpdate = null;
		_exitStopOrder = null;
		_exitTakeProfitOrder = null;
		_avgEntryPrice = 0m;
		_signedPosition = 0m;
		_lastStopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		var bid = message.TryGetDecimal(Level1Fields.BestBidPrice);
		var ask = message.TryGetDecimal(Level1Fields.BestAskPrice);

		if (bid.HasValue)
			_bestBid = bid.Value;

		if (ask.HasValue)
			_bestAsk = ask.Value;

		if (_bestBid is null || _bestAsk is null || _bestBid <= 0m || _bestAsk <= 0m)
			return;

		var time = message.ServerTime != default ? message.ServerTime : CurrentTime;

		ManagePositionProtection();
		ManageEntryOrders(time);
	}

	private void ManageEntryOrders(DateTimeOffset time)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var priceStep = GetPriceStep();
		if (priceStep <= 0m || Volume <= 0)
			return;

		if (!IsOrderActive(_entryBuyStopOrder))
			_entryBuyStopOrder = null;

		if (!IsOrderActive(_entrySellStopOrder))
			_entrySellStopOrder = null;

		var bid = _bestBid ?? 0m;
		var ask = _bestAsk ?? 0m;

		if (bid <= 0m || ask <= 0m)
			return;

		if (ProcessBuyStop(time, priceStep, ask))
			return;

		ProcessSellStop(time, priceStep, bid);
	}

	private bool ProcessBuyStop(DateTimeOffset time, decimal priceStep, decimal ask)
	{
		if (_signedPosition > 0)
			return false;

		var desiredPrice = ask + EntryOffsetTicks * priceStep;

		if (_entryBuyStopOrder is null)
		{
			_entryBuyStopOrder = BuyStop(Volume, desiredPrice);
			_buyOrderLastUpdate = time;
			return false;
		}

		if (OrderRefreshSeconds <= 0 || !_buyOrderLastUpdate.HasValue)
			return false;

		var interval = TimeSpan.FromSeconds(OrderRefreshSeconds);
		if (time - _buyOrderLastUpdate.Value < interval)
			return false;

		var minDiff = TrailingStepTicks * priceStep;
		if (minDiff <= 0m)
			minDiff = priceStep;

		if (Math.Abs(_entryBuyStopOrder.Price - desiredPrice) <= minDiff)
			return false;

		CancelOrder(_entryBuyStopOrder);
		_entryBuyStopOrder = null;
		_buyOrderLastUpdate = null;
		return true;
	}

	private void ProcessSellStop(DateTimeOffset time, decimal priceStep, decimal bid)
	{
		if (_signedPosition < 0)
			return;

		var desiredPrice = bid - EntryOffsetTicks * priceStep;

		if (_entrySellStopOrder is null)
		{
			_entrySellStopOrder = SellStop(Volume, desiredPrice);
			_sellOrderLastUpdate = time;
			return;
		}

		if (OrderRefreshSeconds <= 0 || !_sellOrderLastUpdate.HasValue)
			return;

		var interval = TimeSpan.FromSeconds(OrderRefreshSeconds);
		if (time - _sellOrderLastUpdate.Value < interval)
			return;

		var minDiff = TrailingStepTicks * priceStep;
		if (minDiff <= 0m)
			minDiff = priceStep;

		if (Math.Abs(_entrySellStopOrder.Price - desiredPrice) <= minDiff)
			return;

		CancelOrder(_entrySellStopOrder);
		_entrySellStopOrder = null;
		_sellOrderLastUpdate = null;
	}

	private void ManagePositionProtection()
	{
		var priceStep = GetPriceStep();
		if (priceStep <= 0m)
			return;

		if (!IsOrderActive(_exitStopOrder))
			_exitStopOrder = null;

		if (!IsOrderActive(_exitTakeProfitOrder))
			_exitTakeProfitOrder = null;

		if (_signedPosition == 0)
		{
			CancelExitOrders();
			return;
		}

		EnsureProtectionOrders(priceStep);
		UpdateTrailingStops(priceStep);
	}

	private void EnsureProtectionOrders(decimal priceStep)
	{
		var volume = Math.Abs(_signedPosition);
		if (volume <= 0)
		{
			CancelExitOrders();
			return;
		}

		var minStopTicks = MinimumStopTicks;
		var stopEnabled = StopLossTicks > 0 && StopLossTicks >= minStopTicks && _avgEntryPrice > 0m;
		var takeEnabled = TakeProfitTicks > 0 && TakeProfitTicks >= minStopTicks && _avgEntryPrice > 0m;

		if (!takeEnabled)
		{
			if (_exitTakeProfitOrder != null)
			CancelOrderSafe(ref _exitTakeProfitOrder);
		}
		else
		{
			var desiredTake = _signedPosition > 0
			? _avgEntryPrice + TakeProfitTicks * priceStep
			: _avgEntryPrice - TakeProfitTicks * priceStep;

			if (_exitTakeProfitOrder == null)
			{
			_exitTakeProfitOrder = _signedPosition > 0
			? SellLimit(volume, desiredTake)
			: BuyLimit(volume, desiredTake);
			}
			else
			{
			var needUpdate = _exitTakeProfitOrder.Volume != volume || Math.Abs(_exitTakeProfitOrder.Price - desiredTake) > priceStep / 2m;
			if (needUpdate)
			{
			CancelOrderSafe(ref _exitTakeProfitOrder);
			_exitTakeProfitOrder = _signedPosition > 0
			? SellLimit(volume, desiredTake)
			: BuyLimit(volume, desiredTake);
			}
			}
		}

		if (!stopEnabled)
		{
			if (_exitStopOrder != null)
			{
			CancelOrderSafe(ref _exitStopOrder);
			_lastStopPrice = 0m;
			}
		}
		else if (_exitStopOrder == null)
		{
			var stopPrice = _signedPosition > 0
			? _avgEntryPrice - StopLossTicks * priceStep
			: _avgEntryPrice + StopLossTicks * priceStep;
			_exitStopOrder = _signedPosition > 0
			? SellStop(volume, stopPrice)
			: BuyStop(volume, stopPrice);
			_lastStopPrice = stopPrice;
		}
		else if (_exitStopOrder.Volume != volume)
		{
			var currentPrice = _exitStopOrder.Price;
			CancelOrderSafe(ref _exitStopOrder);
			_exitStopOrder = _signedPosition > 0
			? SellStop(volume, currentPrice)
			: BuyStop(volume, currentPrice);
			_lastStopPrice = currentPrice;
		}
	}

	private void UpdateTrailingStops(decimal priceStep)
	{
		var volume = Math.Abs(_signedPosition);
		if (volume <= 0 || _avgEntryPrice <= 0m)
			return;

		var bid = _bestBid ?? 0m;
		var ask = _bestAsk ?? 0m;
		var minDistance = MinimumStopTicks * priceStep;

		if (_signedPosition > 0 && bid > 0m)
		{
			var currentStop = GetCurrentStopPrice();
			decimal? candidate = null;
			var tolerance = priceStep / 2m;

			if (UseBreakEven)
			{
			var breakEven = _avgEntryPrice + BreakEvenProfitTicks * priceStep;
			if ((currentStop == 0m || breakEven > currentStop) && bid - breakEven >= minDistance)
			candidate = candidate.HasValue ? Math.Max(candidate.Value, breakEven) : breakEven;
			}

			if (TrailingStopTicks > 0 && bid - _avgEntryPrice >= TrailingStartTicks * priceStep)
			{
			var trailing = bid - TrailingStopTicks * priceStep;
			if (trailing >= _avgEntryPrice && bid - trailing >= minDistance)
			{
			var minStep = TrailingStepTicks * priceStep;
			if (minStep <= 0m)
			minStep = priceStep / 2m;

			if (currentStop == 0m || trailing >= currentStop + minStep)
			{
			candidate = candidate.HasValue ? Math.Max(candidate.Value, trailing) : trailing;
			tolerance = Math.Max(tolerance, minStep);
			}
			}
			}

			if (candidate.HasValue)
			{
			var desiredStop = candidate.Value;
			if (currentStop == 0m || desiredStop > currentStop)
			ApplyStopUpdate(true, volume, currentStop, desiredStop, tolerance);
			}
		}
		else if (_signedPosition < 0 && ask > 0m)
		{
			var currentStop = GetCurrentStopPrice();
			decimal? candidate = null;
			var tolerance = priceStep / 2m;

			if (UseBreakEven)
			{
			var breakEven = _avgEntryPrice - BreakEvenProfitTicks * priceStep;
			if ((currentStop == 0m || breakEven < currentStop) && breakEven >= ask + minDistance)
			candidate = candidate.HasValue ? Math.Min(candidate.Value, breakEven) : breakEven;
			}

			if (TrailingStopTicks > 0 && _avgEntryPrice - ask >= TrailingStartTicks * priceStep)
			{
			var trailing = ask + TrailingStopTicks * priceStep;
			if (trailing <= _avgEntryPrice && trailing >= ask + minDistance)
			{
			var minStep = TrailingStepTicks * priceStep;
			if (minStep <= 0m)
			minStep = priceStep / 2m;

			if (currentStop == 0m || trailing <= currentStop - minStep)
			{
			candidate = candidate.HasValue ? Math.Min(candidate.Value, trailing) : trailing;
			tolerance = Math.Max(tolerance, minStep);
			}
			}
			}

			if (candidate.HasValue)
			{
			var desiredStop = candidate.Value;
			if (currentStop == 0m || desiredStop < currentStop)
			ApplyStopUpdate(false, volume, currentStop, desiredStop, tolerance);
			}
		}
	}

	private void ApplyStopUpdate(bool isLong, decimal volume, decimal currentStop, decimal desiredStop, decimal tolerance)
	{
		if (desiredStop <= 0m)
			return;

		if ((isLong && desiredStop <= currentStop + tolerance && currentStop != 0m) ||
		(!isLong && currentStop != 0m && desiredStop >= currentStop - tolerance))
			return;

		CancelOrderSafe(ref _exitStopOrder);
		_exitStopOrder = isLong
		? SellStop(volume, desiredStop)
		: BuyStop(volume, desiredStop);
		_lastStopPrice = desiredStop;
	}

	private decimal GetCurrentStopPrice()
	{
		if (_exitStopOrder != null)
		return _exitStopOrder.Price;

		return _lastStopPrice;
	}

	private void CancelExitOrders()
	{
		CancelOrderSafe(ref _exitStopOrder);
		CancelOrderSafe(ref _exitTakeProfitOrder);
		_lastStopPrice = 0m;
	}

	private void CancelOrderSafe(ref Order order)
	{
		if (order == null)
			return;

		if (order.State == OrderStates.Active)
		CancelOrder(order);

		order = null;
	}

	private bool IsOrderActive(Order order)
	{
		return order != null && order.State == OrderStates.Active;
	}

	private decimal GetPriceStep()
	{
		return Security?.PriceStep ?? 0m;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null)
			return;

		UpdatePositionState(trade);

		var priceStep = GetPriceStep();
		if (priceStep > 0m)
		EnsureProtectionOrders(priceStep);

		if (_signedPosition == 0)
		CancelExitOrders();
	}

	private void UpdatePositionState(MyTrade trade)
	{
		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;
		var side = trade.Order.Side;
		var signedVolume = side == Sides.Buy ? volume : -volume;

		var prevPosition = _signedPosition;
		var newPosition = prevPosition + signedVolume;

		if (prevPosition == 0)
		{
			_avgEntryPrice = price;
		}
		else if (prevPosition > 0)
		{
			if (side == Sides.Buy)
			{
				if (newPosition > 0)
				_avgEntryPrice = (_avgEntryPrice * prevPosition + price * volume) / newPosition;
				else if (newPosition <= 0)
				_avgEntryPrice = price;
			}
			else
			{
				if (newPosition <= 0)
				_avgEntryPrice = newPosition == 0 ? 0m : price;
			}
		}
		else if (prevPosition < 0)
		{
			if (side == Sides.Sell)
			{
				if (newPosition < 0)
				_avgEntryPrice = (_avgEntryPrice * -prevPosition + price * volume) / -newPosition;
				else if (newPosition >= 0)
				_avgEntryPrice = price;
			}
			else
			{
				if (newPosition >= 0)
				_avgEntryPrice = newPosition == 0 ? 0m : price;
			}
		}

		_signedPosition = newPosition;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			_signedPosition = 0m;
			_avgEntryPrice = 0m;
			CancelExitOrders();
		}
	}
}
