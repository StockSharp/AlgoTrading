using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Manual training panel that mirrors the MQL Training expert advisor.
/// It exposes boolean parameters that work like the draggable buttons from MetaTrader.
/// </summary>
public class TrainingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<bool> _requestBuy;
	private readonly StrategyParam<bool> _requestSell;
	private readonly StrategyParam<bool> _closeBuy;
	private readonly StrategyParam<bool> _closeSell;

	private Order _entryOrder;
	private Order _stopLossOrder;
	private Order _takeProfitOrder;

	private DateTimeOffset _lastStatusUpdate;

	/// <summary>
	/// Initializes a new instance of the <see cref="TrainingStrategy"/> class.
	/// </summary>
	public TrainingStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetDisplay("Order Volume", "Contracts traded for each manual action", "Trading")
		.SetGreaterThanZero();

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 30)
		.SetDisplay("Take Profit (points)", "Distance in price steps for the take-profit", "Risk")
		.SetGreaterThanOrEqualTo(0);

		_stopLossPoints = Param(nameof(StopLossPoints), 30)
		.SetDisplay("Stop Loss (points)", "Distance in price steps for the stop-loss", "Risk")
		.SetGreaterThanOrEqualTo(0);

		_requestBuy = Param(nameof(RequestBuy), false)
		.SetDisplay("Request Buy", "Set to true to submit a market buy", "Manual Controls");

		_requestSell = Param(nameof(RequestSell), false)
		.SetDisplay("Request Sell", "Set to true to submit a market sell", "Manual Controls");

		_closeBuy = Param(nameof(CloseBuy), false)
		.SetDisplay("Close Long", "Set to true to flatten an open long", "Manual Controls");

		_closeSell = Param(nameof(CloseSell), false)
		.SetDisplay("Close Short", "Set to true to flatten an open short", "Manual Controls");
	}

	/// <summary>
	/// Contracts traded by each manual request.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Protective take-profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Protective stop-loss distance expressed in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Set to true to request a market buy.
	/// </summary>
	public bool RequestBuy
	{
		get => _requestBuy.Value;
		set => _requestBuy.Value = value;
	}

	/// <summary>
	/// Set to true to request a market sell.
	/// </summary>
	public bool RequestSell
	{
		get => _requestSell.Value;
		set => _requestSell.Value = value;
	}

	/// <summary>
	/// Set to true to close the current long position.
	/// </summary>
	public bool CloseBuy
	{
		get => _closeBuy.Value;
		set => _closeBuy.Value = value;
	}

	/// <summary>
	/// Set to true to close the current short position.
	/// </summary>
	public bool CloseSell
	{
		get => _closeSell.Value;
		set => _closeSell.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Ticks)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_entryOrder = null;
		_stopLossOrder = null;
		_takeProfitOrder = null;
		_lastStatusUpdate = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		// The timer mirrors the MQL Control() loop that polled label positions.
		Timer.Start(TimeSpan.FromMilliseconds(250), ProcessRequests);

		AddInfoLog("Training strategy started. Toggle the manual parameters to send orders.");
	}

	private void ProcessRequests()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (OrderVolume <= 0m)
			return;

		Volume = OrderVolume;

		// Execute pending manual open requests.
		if (RequestBuy && TryEnterLong())
			RequestBuy = false;

		if (RequestSell && TryEnterShort())
			RequestSell = false;

		// Execute pending manual close requests.
		if (CloseBuy)
		{
			if (TryCloseLong() || Position <= 0m)
				CloseBuy = false;
		}

		if (CloseSell)
		{
			if (TryCloseShort() || Position >= 0m)
				CloseSell = false;
		}

		UpdateStatus();
	}

	private bool TryEnterLong()
	{
		var volume = OrderVolume;
		if (volume <= 0m)
			return false;

		CancelProtectionOrders();

		var totalVolume = volume + Math.Max(0m, -Position);
		var order = BuyMarket(totalVolume);
		if (order == null)
			return false;

		_entryOrder = order;

		AddInfoLog($"Submitted market buy for {totalVolume} contracts.");
		return true;
	}

	private bool TryEnterShort()
	{
		var volume = OrderVolume;
		if (volume <= 0m)
			return false;

		CancelProtectionOrders();

		var totalVolume = volume + Math.Max(0m, Position);
		var order = SellMarket(totalVolume);
		if (order == null)
			return false;

		_entryOrder = order;

		AddInfoLog($"Submitted market sell for {totalVolume} contracts.");
		return true;
	}

	private bool TryCloseLong()
	{
		var volume = Math.Max(0m, Position);
		if (volume <= 0m)
			return false;

		CancelProtectionOrders();

		var order = SellMarket(volume);
		if (order == null)
			return false;

		AddInfoLog($"Closing long position with market sell of {volume} contracts.");
		return true;
	}

	private bool TryCloseShort()
	{
		var volume = Math.Max(0m, -Position);
		if (volume <= 0m)
			return false;

		CancelProtectionOrders();

		var order = BuyMarket(volume);
		if (order == null)
			return false;

		AddInfoLog($"Closing short position with market buy of {volume} contracts.");
		return true;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var order = trade.Order;
		if (order == null)
			return;

		if (order == _entryOrder)
		{
			_entryOrder = null;

			var price = trade.Trade?.Price ?? trade.Price ?? order.Price ?? 0m;
			if (price > 0m)
			{
				CreateProtection(order.Direction == Sides.Buy, price);
			}
		}
		else if (order == _stopLossOrder)
		{
			_stopLossOrder = null;
			CancelOrderSafe(ref _takeProfitOrder);
			AddInfoLog("Stop-loss filled, clearing take-profit.");
		}
		else if (order == _takeProfitOrder)
		{
			_takeProfitOrder = null;
			CancelOrderSafe(ref _stopLossOrder);
			AddInfoLog("Take-profit filled, clearing stop-loss.");
		}

		UpdateStatus();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		// When the net position reaches zero, cancel leftover protection orders.
		if (Position == 0m)
			CancelProtectionOrders();
	}

	private void CreateProtection(bool isLong, decimal entryPrice)
	{
		if (Security?.PriceStep == null)
		{
			AddWarningLog("Price step is not defined; protection orders cannot be created.");
			return;
		}

		if (StopLossPoints <= 0 && TakeProfitPoints <= 0)
			return;

		CancelProtectionOrders();

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			volume = OrderVolume;
		if (volume <= 0m)
			return;

		var step = Security.PriceStep.Value;
		var stopOffset = StopLossPoints * step;
		var takeOffset = TakeProfitPoints * step;

		decimal? stopPrice = null;
		decimal? takePrice = null;

		if (isLong)
		{
			if (StopLossPoints > 0)
			{
				var raw = entryPrice - stopOffset;
				stopPrice = Security.ShrinkPrice(raw);
				_stopLossOrder = SellStop(price: stopPrice.Value, volume: volume);
			}

			if (TakeProfitPoints > 0)
			{
				var raw = entryPrice + takeOffset;
				takePrice = Security.ShrinkPrice(raw);
				_takeProfitOrder = SellLimit(price: takePrice.Value, volume: volume);
			}
		}
		else
		{
			if (StopLossPoints > 0)
			{
				var raw = entryPrice + stopOffset;
				stopPrice = Security.ShrinkPrice(raw);
				_stopLossOrder = BuyStop(price: stopPrice.Value, volume: volume);
			}

			if (TakeProfitPoints > 0)
			{
				var raw = entryPrice - takeOffset;
				takePrice = Security.ShrinkPrice(raw);
				_takeProfitOrder = BuyLimit(price: takePrice.Value, volume: volume);
			}
		}

		AddInfoLog($"Protection updated. Stop={(stopPrice ?? 0m):0.#####}; Take={(takePrice ?? 0m):0.#####}.");
	}

	private void CancelProtectionOrders()
	{
		CancelOrderSafe(ref _stopLossOrder);
		CancelOrderSafe(ref _takeProfitOrder);
	}

	private void CancelOrderSafe(ref Order order)
	{
		if (order == null)
			return;

		if (order.State == OrderStates.Active)
			CancelOrder(order);

		order = null;
	}

	private void UpdateStatus()
	{
		var now = CurrentTime;
		if (_lastStatusUpdate != default && (now - _lastStatusUpdate) < TimeSpan.FromSeconds(5))
			return;

		var balance = Portfolio?.CurrentValue ?? 0m;
		var realized = PnL;
		AddInfoLog($"Balance={balance:0.##}; PnL={realized:0.##}; Position={Position:0.##}.");

		_lastStatusUpdate = now;
	}
}
