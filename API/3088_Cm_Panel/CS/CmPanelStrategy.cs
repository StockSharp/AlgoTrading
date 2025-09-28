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
/// Manual trading panel that recreates the "cm panel" MetaTrader script.
/// It submits pending stop orders with optional stop-loss and take-profit distances when toggled through parameters.
/// </summary>
public class CmPanelStrategy : Strategy
{
	private readonly StrategyParam<decimal> _buyVolume;
	private readonly StrategyParam<decimal> _sellVolume;
	private readonly StrategyParam<int> _buyStopOffsetPoints;
	private readonly StrategyParam<int> _sellStopOffsetPoints;
	private readonly StrategyParam<int> _buyStopLossPoints;
	private readonly StrategyParam<int> _sellStopLossPoints;
	private readonly StrategyParam<int> _buyTakeProfitPoints;
	private readonly StrategyParam<int> _sellTakeProfitPoints;
	private readonly StrategyParam<bool> _placeBuyStop;
	private readonly StrategyParam<bool> _placeSellStop;
	private readonly StrategyParam<bool> _cancelPendingOrders;

	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private Order _longStopLossOrder;
	private Order _longTakeProfitOrder;
	private Order _shortStopLossOrder;
	private Order _shortTakeProfitOrder;

	private decimal? _pendingLongStopLoss;
	private decimal? _pendingLongTakeProfit;
	private decimal? _pendingShortStopLoss;
	private decimal? _pendingShortTakeProfit;

	private bool _isRunning;
	private decimal _priceStep;

	/// <summary>
	/// Initializes the interactive parameters that emulate the MetaTrader panel fields.
	/// </summary>
	public CmPanelStrategy()
	{
		_buyVolume = Param(nameof(BuyVolume), 0.10m)
		.SetGreaterThanZero()
		.SetDisplay("Buy volume", "Volume for buy stop entries", "Trading");

		_sellVolume = Param(nameof(SellVolume), 0.10m)
		.SetGreaterThanZero()
		.SetDisplay("Sell volume", "Volume for sell stop entries", "Trading");

		_buyStopOffsetPoints = Param(nameof(BuyStopOffsetPoints), 100)
		.SetNotNegative()
		.SetDisplay("Buy offset", "Distance above the ask where the buy stop is placed (points)", "Distances");

		_sellStopOffsetPoints = Param(nameof(SellStopOffsetPoints), 100)
		.SetNotNegative()
		.SetDisplay("Sell offset", "Distance below the bid where the sell stop is placed (points)", "Distances");

		_buyStopLossPoints = Param(nameof(BuyStopLossPoints), 100)
		.SetNotNegative()
		.SetDisplay("Buy stop-loss", "Protective stop distance for long trades (points)", "Risk");

		_sellStopLossPoints = Param(nameof(SellStopLossPoints), 100)
		.SetNotNegative()
		.SetDisplay("Sell stop-loss", "Protective stop distance for short trades (points)", "Risk");

		_buyTakeProfitPoints = Param(nameof(BuyTakeProfitPoints), 150)
		.SetNotNegative()
		.SetDisplay("Buy take-profit", "Profit target distance for long trades (points)", "Risk");

		_sellTakeProfitPoints = Param(nameof(SellTakeProfitPoints), 150)
		.SetNotNegative()
		.SetDisplay("Sell take-profit", "Profit target distance for short trades (points)", "Risk");

		_placeBuyStop = Param(nameof(PlaceBuyStop), false)
		.SetDisplay("Place buy stop", "Toggle to submit a buy stop order", "Actions");

		_placeSellStop = Param(nameof(PlaceSellStop), false)
		.SetDisplay("Place sell stop", "Toggle to submit a sell stop order", "Actions");

		_cancelPendingOrders = Param(nameof(CancelPendingOrders), false)
		.SetDisplay("Cancel pending", "Set to true to cancel active pending stops", "Actions");
	}

	/// <summary>
	/// Gets or sets the volume used when placing buy stop orders.
	/// </summary>
	public decimal BuyVolume
	{
		get => _buyVolume.Value;
		set => _buyVolume.Value = value;
	}

	/// <summary>
	/// Gets or sets the volume used when placing sell stop orders.
	/// </summary>
	public decimal SellVolume
	{
		get => _sellVolume.Value;
		set => _sellVolume.Value = value;
	}

	/// <summary>
	/// Gets or sets the distance between the current ask price and the buy stop order in points.
	/// </summary>
	public int BuyStopOffsetPoints
	{
		get => _buyStopOffsetPoints.Value;
		set => _buyStopOffsetPoints.Value = value;
	}

	/// <summary>
	/// Gets or sets the distance between the current bid price and the sell stop order in points.
	/// </summary>
	public int SellStopOffsetPoints
	{
		get => _sellStopOffsetPoints.Value;
		set => _sellStopOffsetPoints.Value = value;
	}

	/// <summary>
	/// Gets or sets the stop-loss distance in points for long positions opened via the buy stop.
	/// </summary>
	public int BuyStopLossPoints
	{
		get => _buyStopLossPoints.Value;
		set => _buyStopLossPoints.Value = value;
	}

	/// <summary>
	/// Gets or sets the stop-loss distance in points for short positions opened via the sell stop.
	/// </summary>
	public int SellStopLossPoints
	{
		get => _sellStopLossPoints.Value;
		set => _sellStopLossPoints.Value = value;
	}

	/// <summary>
	/// Gets or sets the take-profit distance in points for long positions opened via the buy stop.
	/// </summary>
	public int BuyTakeProfitPoints
	{
		get => _buyTakeProfitPoints.Value;
		set => _buyTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Gets or sets the take-profit distance in points for short positions opened via the sell stop.
	/// </summary>
	public int SellTakeProfitPoints
	{
		get => _sellTakeProfitPoints.Value;
		set => _sellTakeProfitPoints.Value = value;
	}

	/// <summary>
	/// Gets or sets the flag used to place a buy stop order.
	/// </summary>
	public bool PlaceBuyStop
	{
		get => _placeBuyStop.Value;
		set
		{
			if (_placeBuyStop.Value == value)
			return;

			_placeBuyStop.Value = value;

			if (value)
			ProcessPlaceBuyStop();
		}
	}

	/// <summary>
	/// Gets or sets the flag used to place a sell stop order.
	/// </summary>
	public bool PlaceSellStop
	{
		get => _placeSellStop.Value;
		set
		{
			if (_placeSellStop.Value == value)
			return;

			_placeSellStop.Value = value;

			if (value)
			ProcessPlaceSellStop();
		}
	}

	/// <summary>
	/// Gets or sets the flag that cancels active pending stop orders.
	/// </summary>
	public bool CancelPendingOrders
	{
		get => _cancelPendingOrders.Value;
		set
		{
			if (_cancelPendingOrders.Value == value)
			return;

			_cancelPendingOrders.Value = value;

			if (value)
			ProcessCancelPendingOrders();
		}
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_isRunning = true;

		var security = Security;
		_priceStep = security?.PriceStep ?? 0m;
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

		if (order == _buyStopOrder)
		{
			var volume = trade.Trade.Volume;

			_buyStopOrder = null;

			if (_pendingLongStopLoss is decimal stopLoss && stopLoss > 0m && _longStopLossOrder == null)
			_longStopLossOrder = SellStop(volume, stopLoss);

			if (_pendingLongTakeProfit is decimal takeProfit && takeProfit > 0m && _longTakeProfitOrder == null)
			_longTakeProfitOrder = SellLimit(volume, takeProfit);

			_pendingLongStopLoss = null;
			_pendingLongTakeProfit = null;
		}
		else if (order == _sellStopOrder)
		{
			var volume = trade.Trade.Volume;

			_sellStopOrder = null;

			if (_pendingShortStopLoss is decimal stopLoss && stopLoss > 0m && _shortStopLossOrder == null)
			_shortStopLossOrder = BuyStop(volume, stopLoss);

			if (_pendingShortTakeProfit is decimal takeProfit && takeProfit > 0m && _shortTakeProfitOrder == null)
			_shortTakeProfitOrder = BuyLimit(volume, takeProfit);

			_pendingShortStopLoss = null;
			_pendingShortTakeProfit = null;
		}
		else if (order == _longStopLossOrder)
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

	/// <inheritdoc />
	protected override void OnOrderReceived(Order order)
	{
		base.OnOrderReceived(order);

		if (order == _buyStopOrder && IsFinal(order))
		{
			_buyStopOrder = null;
			_pendingLongStopLoss = null;
			_pendingLongTakeProfit = null;
		}
		else if (order == _sellStopOrder && IsFinal(order))
		{
			_sellStopOrder = null;
			_pendingShortStopLoss = null;
			_pendingShortTakeProfit = null;
		}
		else if (order == _longStopLossOrder && IsFinal(order))
		{
			_longStopLossOrder = null;
		}
		else if (order == _longTakeProfitOrder && IsFinal(order))
		{
			_longTakeProfitOrder = null;
		}
		else if (order == _shortStopLossOrder && IsFinal(order))
		{
			_shortStopLossOrder = null;
		}
		else if (order == _shortTakeProfitOrder && IsFinal(order))
		{
			_shortTakeProfitOrder = null;
		}
	}

	private void ProcessPlaceBuyStop()
	{
		try
		{
			if (!EnsureReady("place a buy stop order"))
			return;

			var volume = AdjustVolume(BuyVolume);
			if (volume <= 0m)
			{
				LogWarning("Buy volume must be greater than zero and comply with the volume step.");
				return;
			}

			var ask = GetAskPrice();
			if (ask <= 0m)
			{
				LogWarning("Cannot place a buy stop because the ask price is unavailable.");
				return;
			}

			var offset = ConvertPoints(BuyStopOffsetPoints);
			var price = NormalizePrice(ask + offset);
			if (price <= 0m)
			{
				LogWarning("Computed buy stop price is invalid.");
				return;
			}

			CancelOrderIfActive(ref _buyStopOrder);

			_buyStopOrder = BuyStop(volume, price);

			_pendingLongStopLoss = ComputeStopLoss(price, BuyStopLossPoints, true);
			_pendingLongTakeProfit = ComputeTakeProfit(price, BuyTakeProfitPoints, true);
		}
		finally
		{
			_placeBuyStop.Value = false;
		}
	}

	private void ProcessPlaceSellStop()
	{
		try
		{
			if (!EnsureReady("place a sell stop order"))
			return;

			var volume = AdjustVolume(SellVolume);
			if (volume <= 0m)
			{
				LogWarning("Sell volume must be greater than zero and comply with the volume step.");
				return;
			}

			var bid = GetBidPrice();
			if (bid <= 0m)
			{
				LogWarning("Cannot place a sell stop because the bid price is unavailable.");
				return;
			}

			var offset = ConvertPoints(SellStopOffsetPoints);
			var price = NormalizePrice(bid - offset);
			if (price <= 0m)
			{
				LogWarning("Computed sell stop price is invalid.");
				return;
			}

			CancelOrderIfActive(ref _sellStopOrder);

			_sellStopOrder = SellStop(volume, price);

			_pendingShortStopLoss = ComputeStopLoss(price, SellStopLossPoints, false);
			_pendingShortTakeProfit = ComputeTakeProfit(price, SellTakeProfitPoints, false);
		}
		finally
		{
			_placeSellStop.Value = false;
		}
	}

	private void ProcessCancelPendingOrders()
	{
		try
		{
			if (!EnsureReady("cancel pending orders"))
			return;

			CancelOrderIfActive(ref _buyStopOrder);
			CancelOrderIfActive(ref _sellStopOrder);

			_pendingLongStopLoss = null;
			_pendingLongTakeProfit = null;
			_pendingShortStopLoss = null;
			_pendingShortTakeProfit = null;
		}
		finally
		{
			_cancelPendingOrders.Value = false;
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

	private decimal GetAskPrice()
	{
		var security = Security;
		if (security?.BestAsk?.Price is decimal ask && ask > 0m)
		return ask;

		if (security?.LastTick?.Price is decimal last && last > 0m)
		return last;

		return 0m;
	}

	private decimal GetBidPrice()
	{
		var security = Security;
		if (security?.BestBid?.Price is decimal bid && bid > 0m)
		return bid;

		if (security?.LastTick?.Price is decimal last && last > 0m)
		return last;

		return 0m;
	}

	private decimal ConvertPoints(int points)
	{
		if (points == 0)
		return 0m;

		var step = _priceStep;
		if (step <= 0m)
		{
			var security = Security;
			step = security?.PriceStep ?? 0m;
		}

		if (step <= 0m)
		step = 0.0001m;

		return step * points;
	}

	private decimal NormalizePrice(decimal price)
	{
		var security = Security;
		if (security == null)
		return price;

		var normalized = security.ShrinkPrice(price);
		return normalized > 0m ? normalized : price;
	}

	private decimal? ComputeStopLoss(decimal entryPrice, int points, bool isLong)
	{
		if (points <= 0)
		return null;

		var offset = ConvertPoints(points);
		if (offset <= 0m)
		return null;

		var rawPrice = isLong ? entryPrice - offset : entryPrice + offset;
		if (rawPrice <= 0m)
		return null;

		return NormalizePrice(rawPrice);
	}

	private decimal? ComputeTakeProfit(decimal entryPrice, int points, bool isLong)
	{
		if (points <= 0)
		return null;

		var offset = ConvertPoints(points);
		if (offset <= 0m)
		return null;

		var rawPrice = isLong ? entryPrice + offset : entryPrice - offset;
		if (rawPrice <= 0m)
		return null;

		return NormalizePrice(rawPrice);
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
		=> order.State.IsFinal();
}

