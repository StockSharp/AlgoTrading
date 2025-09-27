
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
/// Manual trading helper inspired by the "Peter Panel" MetaTrader add-on.
/// The strategy exposes interactive parameters that mimic the original buttons and price lines.
/// </summary>
public class PeterPanelStrategy : Strategy
{
	private readonly StrategyParam<decimal> _entryLevel;
	private readonly StrategyParam<decimal> _takeProfitLevel;
	private readonly StrategyParam<decimal> _stopLossLevel;
	private readonly StrategyParam<bool> _buyMarketCommand;
	private readonly StrategyParam<bool> _buyStopCommand;
	private readonly StrategyParam<bool> _buyLimitCommand;
	private readonly StrategyParam<bool> _sellMarketCommand;
	private readonly StrategyParam<bool> _sellStopCommand;
	private readonly StrategyParam<bool> _sellLimitCommand;
	private readonly StrategyParam<bool> _modifyCommand;
	private readonly StrategyParam<bool> _closeCommand;
	private readonly StrategyParam<bool> _resetCommand;

	private decimal? _lastBid;
	private decimal? _lastAsk;
	private decimal _priceStep;
	private bool _isRunning;

	private Order _buyMarketOrder;
	private Order _sellMarketOrder;
	private Order _buyStopOrder;
	private Order _buyLimitOrder;
	private Order _sellStopOrder;
	private Order _sellLimitOrder;
	private Order _longStopLossOrder;
	private Order _longTakeProfitOrder;
	private Order _shortStopLossOrder;
	private Order _shortTakeProfitOrder;

	/// <summary>
	/// Initializes PeterPanelStrategy.
	/// </summary>
	public PeterPanelStrategy()
	{

		_entryLevel = Param(nameof(EntryLevel), 0m)
			.SetDisplay("Entry price", "Price level used for pending orders (aqua line)", "Levels");

		_takeProfitLevel = Param(nameof(TakeProfitLevel), 0m)
			.SetDisplay("Take-profit", "Green line price used for protective targets", "Levels");

		_stopLossLevel = Param(nameof(StopLossLevel), 0m)
			.SetDisplay("Stop-loss", "Red line price used for protective stops", "Levels");

		_buyMarketCommand = Param(nameof(BuyMarketCommand), false)
			.SetDisplay("Buy market", "Set to true to send a market buy order", "Actions");

		_buyStopCommand = Param(nameof(BuyStopCommand), false)
			.SetDisplay("Buy stop", "Set to true to place a buy stop at the entry line", "Actions");

		_buyLimitCommand = Param(nameof(BuyLimitCommand), false)
			.SetDisplay("Buy limit", "Set to true to place a buy limit at the entry line", "Actions");

		_sellMarketCommand = Param(nameof(SellMarketCommand), false)
			.SetDisplay("Sell market", "Set to true to send a market sell order", "Actions");

		_sellStopCommand = Param(nameof(SellStopCommand), false)
			.SetDisplay("Sell stop", "Set to true to place a sell stop at the entry line", "Actions");

		_sellLimitCommand = Param(nameof(SellLimitCommand), false)
			.SetDisplay("Sell limit", "Set to true to place a sell limit at the entry line", "Actions");

		_modifyCommand = Param(nameof(ModifyCommand), false)
			.SetDisplay("Modify", "Reapply entry, stop-loss and take-profit levels to active orders", "Actions");

		_closeCommand = Param(nameof(CloseCommand), false)
			.SetDisplay("Close", "Cancel pending orders and flatten the position", "Actions");

		_resetCommand = Param(nameof(ResetCommand), false)
			.SetDisplay("Reset lines", "Recalculate entry, stop-loss and take-profit levels around the mid price", "Actions");
	}


	/// <summary>
	/// Gets or sets the price used for pending orders.
	/// </summary>
	public decimal EntryLevel
	{
		get => _entryLevel.Value;
		set => _entryLevel.Value = value;
	}

	/// <summary>
	/// Gets or sets the take-profit level.
	/// </summary>
	public decimal TakeProfitLevel
	{
		get => _takeProfitLevel.Value;
		set => _takeProfitLevel.Value = value;
	}

	/// <summary>
	/// Gets or sets the stop-loss level.
	/// </summary>
	public decimal StopLossLevel
	{
		get => _stopLossLevel.Value;
		set => _stopLossLevel.Value = value;
	}

	/// <summary>
	/// Triggers a market buy order when set to true.
	/// </summary>
	public bool BuyMarketCommand
	{
		get => _buyMarketCommand.Value;
		set
		{
			if (_buyMarketCommand.Value == value)
				return;

			_buyMarketCommand.Value = value;

			if (value)
				ProcessBuyMarket();
		}
	}

	/// <summary>
	/// Triggers a buy stop order when set to true.
	/// </summary>
	public bool BuyStopCommand
	{
		get => _buyStopCommand.Value;
		set
		{
			if (_buyStopCommand.Value == value)
				return;

			_buyStopCommand.Value = value;

			if (value)
				ProcessBuyStop();
		}
	}

	/// <summary>
	/// Triggers a buy limit order when set to true.
	/// </summary>
	public bool BuyLimitCommand
	{
		get => _buyLimitCommand.Value;
		set
		{
			if (_buyLimitCommand.Value == value)
				return;

			_buyLimitCommand.Value = value;

			if (value)
				ProcessBuyLimit();
		}
	}

	/// <summary>
	/// Triggers a market sell order when set to true.
	/// </summary>
	public bool SellMarketCommand
	{
		get => _sellMarketCommand.Value;
		set
		{
			if (_sellMarketCommand.Value == value)
				return;

			_sellMarketCommand.Value = value;

			if (value)
				ProcessSellMarket();
		}
	}

	/// <summary>
	/// Triggers a sell stop order when set to true.
	/// </summary>
	public bool SellStopCommand
	{
		get => _sellStopCommand.Value;
		set
		{
			if (_sellStopCommand.Value == value)
				return;

			_sellStopCommand.Value = value;

			if (value)
				ProcessSellStop();
		}
	}

	/// <summary>
	/// Triggers a sell limit order when set to true.
	/// </summary>
	public bool SellLimitCommand
	{
		get => _sellLimitCommand.Value;
		set
		{
			if (_sellLimitCommand.Value == value)
				return;

			_sellLimitCommand.Value = value;

			if (value)
				ProcessSellLimit();
		}
	}

	/// <summary>
	/// Applies the configured levels to existing orders.
	/// </summary>
	public bool ModifyCommand
	{
		get => _modifyCommand.Value;
		set
		{
			if (_modifyCommand.Value == value)
				return;

			_modifyCommand.Value = value;

			if (value)
				ProcessModify();
		}
	}

	/// <summary>
	/// Cancels pending orders and closes the position.
	/// </summary>
	public bool CloseCommand
	{
		get => _closeCommand.Value;
		set
		{
			if (_closeCommand.Value == value)
				return;

			_closeCommand.Value = value;

			if (value)
				ProcessClose();
		}
	}

	/// <summary>
	/// Resets the price levels around the current mid price.
	/// </summary>
	public bool ResetCommand
	{
		get => _resetCommand.Value;
		set
		{
			if (_resetCommand.Value == value)
				return;

			_resetCommand.Value = value;

			if (value)
				ProcessReset();
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastBid = null;
		_lastAsk = null;
		_priceStep = 0m;
		_isRunning = false;

		_buyMarketOrder = null;
		_sellMarketOrder = null;
		_buyStopOrder = null;
		_buyLimitOrder = null;
		_sellStopOrder = null;
		_sellLimitOrder = null;
		_longStopLossOrder = null;
		_longTakeProfitOrder = null;
		_shortStopLossOrder = null;
		_shortTakeProfitOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var security = Security;
		if (security != null)
		{
			_priceStep = security.PriceStep ?? 0m;
		}

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		_isRunning = true;
	}

	/// <inheritdoc />
	protected override void OnStopped(DateTimeOffset time)
	{
		base.OnStopped(time);

		_isRunning = false;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var order = trade.Order;
		if (order == null)
			return;

		if (order == _buyMarketOrder || order == _buyStopOrder || order == _buyLimitOrder)
		{
			ClearEntryReference(order);
			RefreshProtection();
		}
		else if (order == _sellMarketOrder || order == _sellStopOrder || order == _sellLimitOrder)
		{
			ClearEntryReference(order);
			RefreshProtection();
		}
		else if (order == _longStopLossOrder || order == _longTakeProfitOrder || order == _shortStopLossOrder || order == _shortTakeProfitOrder)
		{
			HandleProtectionExecution(order);
		}
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order == _buyMarketOrder && IsFinal(order))
			_buyMarketOrder = null;
		else if (order == _sellMarketOrder && IsFinal(order))
			_sellMarketOrder = null;
		else if (order == _buyStopOrder && IsFinal(order))
			_buyStopOrder = null;
		else if (order == _buyLimitOrder && IsFinal(order))
			_buyLimitOrder = null;
		else if (order == _sellStopOrder && IsFinal(order))
			_sellStopOrder = null;
		else if (order == _sellLimitOrder && IsFinal(order))
			_sellLimitOrder = null;
		else if (order == _longStopLossOrder && IsFinal(order))
			_longStopLossOrder = null;
		else if (order == _longTakeProfitOrder && IsFinal(order))
			_longTakeProfitOrder = null;
		else if (order == _shortStopLossOrder && IsFinal(order))
			_shortStopLossOrder = null;
		else if (order == _shortTakeProfitOrder && IsFinal(order))
			_shortTakeProfitOrder = null;
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue))
			_lastBid = Convert.ToDecimal(bidValue);

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue))
			_lastAsk = Convert.ToDecimal(askValue);
	}

	private void ProcessBuyMarket()
	{
		try
		{
			if (!EnsureReady("send a market buy"))
				return;

			if (!TryGetVolume(out var volume))
				return;

			if (!TryGetQuotePrices(out _, out _))
			{
				LogWarning("Cannot send a buy market order because quotes are unavailable.");
				return;
			}

			_buyMarketOrder = BuyMarket(volume);
		}
		finally
		{
			_buyMarketCommand.Value = false;
		}
	}

	private void ProcessSellMarket()
	{
		try
		{
			if (!EnsureReady("send a market sell"))
				return;

			if (!TryGetVolume(out var volume))
				return;

			if (!TryGetQuotePrices(out _, out _))
			{
				LogWarning("Cannot send a sell market order because quotes are unavailable.");
				return;
			}

			_sellMarketOrder = SellMarket(volume);
		}
		finally
		{
			_sellMarketCommand.Value = false;
		}
	}

	private void ProcessBuyStop()
	{
		try
		{
			if (!EnsureReady("place a buy stop"))
				return;

			if (!TryGetVolume(out var volume))
				return;

			var price = NormalizePrice(EntryLevel);
			if (price <= 0m)
			{
				LogWarning("Entry level must be positive to place a buy stop order.");
				return;
			}

			CancelOrderIfActive(ref _buyStopOrder);
			_buyStopOrder = BuyStop(volume, price);
		}
		finally
		{
			_buyStopCommand.Value = false;
		}
	}

	private void ProcessBuyLimit()
	{
		try
		{
			if (!EnsureReady("place a buy limit"))
				return;

			if (!TryGetVolume(out var volume))
				return;

			var price = NormalizePrice(EntryLevel);
			if (price <= 0m)
			{
				LogWarning("Entry level must be positive to place a buy limit order.");
				return;
			}

			CancelOrderIfActive(ref _buyLimitOrder);
			_buyLimitOrder = BuyLimit(volume, price);
		}
		finally
		{
			_buyLimitCommand.Value = false;
		}
	}

	private void ProcessSellStop()
	{
		try
		{
			if (!EnsureReady("place a sell stop"))
				return;

			if (!TryGetVolume(out var volume))
				return;

			var price = NormalizePrice(EntryLevel);
			if (price <= 0m)
			{
				LogWarning("Entry level must be positive to place a sell stop order.");
				return;
			}

			CancelOrderIfActive(ref _sellStopOrder);
			_sellStopOrder = SellStop(volume, price);
		}
		finally
		{
			_sellStopCommand.Value = false;
		}
	}

	private void ProcessSellLimit()
	{
		try
		{
			if (!EnsureReady("place a sell limit"))
				return;

			if (!TryGetVolume(out var volume))
				return;

			var price = NormalizePrice(EntryLevel);
			if (price <= 0m)
			{
				LogWarning("Entry level must be positive to place a sell limit order.");
				return;
			}

			CancelOrderIfActive(ref _sellLimitOrder);
			_sellLimitOrder = SellLimit(volume, price);
		}
		finally
		{
			_sellLimitCommand.Value = false;
		}
	}

	private void ProcessModify()
	{
		try
		{
			if (!EnsureReady("modify active orders"))
				return;

			RefreshProtection();
			ReapplyPendingOrders();
		}
		finally
		{
			_modifyCommand.Value = false;
		}
	}

	private void ProcessClose()
	{
		try
		{
			if (!EnsureReady("close positions"))
				return;

			CancelPendingOrders();
			CancelProtectionOrders();

			var position = Position;
			if (position > 0m)
				SellMarket(position);
			else if (position < 0m)
				BuyMarket(Math.Abs(position));
		}
		finally
		{
			_closeCommand.Value = false;
		}
	}

	private void ProcessReset()
	{
		try
		{
			if (!EnsureReady("reset price levels"))
				return;

			if (!TryGetQuotePrices(out var bid, out var ask))
			{
				LogWarning("Cannot reset lines because quotes are unavailable.");
				return;
			}

			var mid = (bid + ask) / 2m;
			var spread = Math.Abs(ask - bid);
			var step = _priceStep;
			if (step <= 0m)
				step = Security?.PriceStep ?? 0m;

			if (spread <= 0m)
				spread = step > 0m ? step : Math.Max(0.0001m, mid * 0.001m);

			var distance = spread * 5m;
			var entry = NormalizePrice(mid);
			if (entry <= 0m)
				entry = mid;

			EntryLevel = entry;
			TakeProfitLevel = NormalizePrice(entry + distance);

			var stop = NormalizePrice(entry - distance);
			StopLossLevel = stop > 0m ? stop : NormalizePrice(Math.Max(entry - distance, step > 0m ? entry - step * 5m : entry * 0.99m));

			LogInfo($"Lines reset around {EntryLevel:0.#####} with TP {TakeProfitLevel:0.#####} and SL {StopLossLevel:0.#####}.");
		}
		finally
		{
			_resetCommand.Value = false;
		}
	}

	private bool EnsureReady(string action)
	{
		if (!_isRunning)
		{
			LogWarning($"Cannot {action} because the strategy is not running.");
			return false;
		}

		if (Portfolio == null)
		{
			LogWarning($"Cannot {action} because portfolio is not assigned.");
			return false;
		}

		if (Security == null)
		{
			LogWarning($"Cannot {action} because security is not assigned.");
			return false;
		}

		return true;
	}

	private bool TryGetVolume(out decimal volume)
	{
		volume = AdjustVolume(Volume);
		if (volume <= 0m)
		{
			LogWarning("Configured volume is not valid for the current security.");
			return false;
		}

		return true;
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var ratio = Math.Floor(volume / step);
			volume = ratio * step;
		}

		var minVolume = security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			return 0m;

		var maxVolume = security.MaxVolume;
		if (maxVolume != null && volume > maxVolume.Value)
			volume = maxVolume.Value;

		return volume;
	}

	private decimal NormalizePrice(decimal price)
	{
		var security = Security;
		if (security == null)
			return price;

		var normalized = security.ShrinkPrice(price);
		return normalized > 0m ? normalized : price;
	}

	private bool TryGetQuotePrices(out decimal bid, out decimal ask)
	{
		var security = Security;
		bid = _lastBid ?? security?.BestBid?.Price ?? 0m;
		ask = _lastAsk ?? security?.BestAsk?.Price ?? 0m;

		if (bid <= 0m && ask <= 0m)
		{
			var last = security?.LastTrade?.Price ?? 0m;
			if (last <= 0m)
			return false;

			bid = last;
			ask = last;
		}
		else
		{
			if (bid <= 0m)
				bid = ask;
			if (ask <= 0m)
				ask = bid;
		}

		return bid > 0m && ask > 0m;
	}

	private void RefreshProtection()
	{
		CancelProtectionOrders();

		var position = Position;
		if (position > 0m)
		{
			PlaceLongProtection(position);
		}
		else if (position < 0m)
		{
			PlaceShortProtection(Math.Abs(position));
		}
	}

	private void PlaceLongProtection(decimal volume)
	{
		var stop = NormalizePrice(StopLossLevel);
		if (stop > 0m)
			_longStopLossOrder = SellStop(volume, stop);

		var take = NormalizePrice(TakeProfitLevel);
		if (take > 0m)
			_longTakeProfitOrder = SellLimit(volume, take);
	}

	private void PlaceShortProtection(decimal volume)
	{
		var stop = NormalizePrice(TakeProfitLevel);
		if (stop > 0m)
			_shortStopLossOrder = BuyStop(volume, stop);

		var take = NormalizePrice(StopLossLevel);
		if (take > 0m)
			_shortTakeProfitOrder = BuyLimit(volume, take);
	}

	private void ReapplyPendingOrders()
	{
		var price = NormalizePrice(EntryLevel);
		if (price <= 0m)
		{
			LogWarning("Entry level must be positive to modify pending orders.");
			return;
		}

		ReplacePendingOrder(ref _buyStopOrder, price, BuyStop);
		ReplacePendingOrder(ref _buyLimitOrder, price, BuyLimit);
		ReplacePendingOrder(ref _sellStopOrder, price, SellStop);
		ReplacePendingOrder(ref _sellLimitOrder, price, SellLimit);
	}

	private void ReplacePendingOrder(ref Order order, decimal price, Func<decimal, decimal, Order> submit)
	{
		var existing = order;
		if (existing == null)
			return;

		var volume = existing.Volume ?? AdjustVolume(Volume);
		if (volume <= 0m)
		{
			LogWarning("Pending order volume is invalid; skipping replacement.");
			return;
		}

		if (existing.State == OrderStates.Active)
			CancelOrder(existing);

		order = submit(volume, price);
	}

	private void CancelPendingOrders()
	{
		CancelOrderIfActive(ref _buyStopOrder);
		CancelOrderIfActive(ref _buyLimitOrder);
		CancelOrderIfActive(ref _sellStopOrder);
		CancelOrderIfActive(ref _sellLimitOrder);
	}

	private void CancelProtectionOrders()
	{
		CancelOrderIfActive(ref _longStopLossOrder);
		CancelOrderIfActive(ref _longTakeProfitOrder);
		CancelOrderIfActive(ref _shortStopLossOrder);
		CancelOrderIfActive(ref _shortTakeProfitOrder);
	}

	private void HandleProtectionExecution(Order order)
	{
		if (order == _longStopLossOrder)
		{
			CancelOrderIfActive(ref _longTakeProfitOrder);
			_longStopLossOrder = null;
		}
		else if (order == _longTakeProfitOrder)
		{
			CancelOrderIfActive(ref _longStopLossOrder);
			_longTakeProfitOrder = null;
		}
		else if (order == _shortStopLossOrder)
		{
			CancelOrderIfActive(ref _shortTakeProfitOrder);
			_shortStopLossOrder = null;
		}
		else if (order == _shortTakeProfitOrder)
		{
			CancelOrderIfActive(ref _shortStopLossOrder);
			_shortTakeProfitOrder = null;
		}
	}

	private void ClearEntryReference(Order order)
	{
		if (order == _buyMarketOrder)
			_buyMarketOrder = null;
		else if (order == _sellMarketOrder)
			_sellMarketOrder = null;
		else if (order == _buyStopOrder)
			_buyStopOrder = null;
		else if (order == _buyLimitOrder)
			_buyLimitOrder = null;
		else if (order == _sellStopOrder)
			_sellStopOrder = null;
		else if (order == _sellLimitOrder)
			_sellLimitOrder = null;
	}

	private void CancelOrderIfActive(ref Order order)
	{
		if (order == null)
			return;

		if (order.State == OrderStates.Active)
			CancelOrder(order);

		order = null;
	}

	private static bool IsFinal(Order order)
	=> order.State == OrderStates.Done || order.State == OrderStates.Failed || order.State == OrderStates.Canceled || order.State == OrderStates.Stopped;
}

