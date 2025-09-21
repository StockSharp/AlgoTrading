namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class SampleTrailingstopMt5Strategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;

	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private Order _exitStopOrder;
	private Order _exitTakeProfitOrder;

	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal _signedPosition;
	private decimal _avgEntryPrice;

	public SampleTrailingstopMt5Strategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
		.SetDisplay("Trade volume", "Lot size used for both stop orders.", "General")
		.SetGreaterThanZero();

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 900m)
		.SetDisplay("Take profit (points)", "Distance in points for the protective take-profit. Zero disables it.", "Risk")
		.SetNotNegative();

		_stopLossPoints = Param(nameof(StopLossPoints), 300m)
		.SetDisplay("Stop loss (points)", "Distance in points for the protective stop-loss.", "Risk")
		.SetNotNegative();

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 200m)
		.SetDisplay("Trailing stop (points)", "Trailing distance maintained once the position is profitable. Zero disables trailing.", "Risk")
		.SetNotNegative();
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		CancelOrderSafe(ref _buyStopOrder);
		CancelOrderSafe(ref _sellStopOrder);
		CancelOrderSafe(ref _exitStopOrder);
		CancelOrderSafe(ref _exitTakeProfitOrder);

		_bestBid = null;
		_bestAsk = null;
		_signedPosition = 0m;
		_avgEntryPrice = 0m;
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

		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return;

		ManageEntryOrders(priceStep);
		ManageProtection(priceStep);
		UpdateTrailingStop(priceStep);
	}

	private void ManageEntryOrders(decimal priceStep)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!IsOrderActive(_buyStopOrder))
		_buyStopOrder = null;

		if (!IsOrderActive(_sellStopOrder))
		_sellStopOrder = null;

		var volume = TradeVolume;
		if (volume <= 0m)
		return;

		var bid = _bestBid ?? 0m;
		var ask = _bestAsk ?? 0m;
		if (bid <= 0m || ask <= 0m)
		return;

		var expiration = (CurrentTime == default ? DateTimeOffset.UtcNow : CurrentTime) + TimeSpan.FromDays(1);

		if (_buyStopOrder is null)
		{
			var price = Math.Max(ask, bid + priceStep);
			var stopLoss = StopLossPoints > 0m ? price - StopLossPoints * priceStep : (decimal?)null;
			var takeProfit = TakeProfitPoints > 0m ? price + TakeProfitPoints * priceStep : (decimal?)null;

			_buyStopOrder = BuyStop(volume, price, stopLoss, takeProfit, expirationTime: expiration);
		}

		if (_sellStopOrder is null)
		{
			var price = Math.Min(bid, ask - priceStep);
			var stopLoss = StopLossPoints > 0m ? price + StopLossPoints * priceStep : (decimal?)null;
			var takeProfit = TakeProfitPoints > 0m ? price - TakeProfitPoints * priceStep : (decimal?)null;

			_sellStopOrder = SellStop(volume, price, stopLoss, takeProfit, expirationTime: expiration);
		}
	}

	private void ManageProtection(decimal priceStep)
	{
		if (!IsOrderActive(_exitStopOrder))
		_exitStopOrder = null;

		if (!IsOrderActive(_exitTakeProfitOrder))
		_exitTakeProfitOrder = null;

		var position = _signedPosition;
		if (position == 0m)
		{
			CancelOrderSafe(ref _exitStopOrder);
			CancelOrderSafe(ref _exitTakeProfitOrder);
			return;
		}

		var volume = Math.Abs(position);
		if (volume <= 0m || _avgEntryPrice <= 0m)
		{
			CancelOrderSafe(ref _exitStopOrder);
			CancelOrderSafe(ref _exitTakeProfitOrder);
			return;
		}

		if (TakeProfitPoints > 0m)
		{
			var takePrice = position > 0m
			? _avgEntryPrice + TakeProfitPoints * priceStep
			: _avgEntryPrice - TakeProfitPoints * priceStep;

			if (_exitTakeProfitOrder is null || _exitTakeProfitOrder.Volume != volume || Math.Abs(_exitTakeProfitOrder.Price - takePrice) > priceStep / 2m)
			{
				CancelOrderSafe(ref _exitTakeProfitOrder);
				_exitTakeProfitOrder = position > 0m
				? SellLimit(volume, takePrice)
				: BuyLimit(volume, takePrice);
			}
		}
		else
		{
			CancelOrderSafe(ref _exitTakeProfitOrder);
		}

		if (StopLossPoints > 0m)
		{
			var stopPrice = position > 0m
			? _avgEntryPrice - StopLossPoints * priceStep
			: _avgEntryPrice + StopLossPoints * priceStep;

			if (_exitStopOrder is null || _exitStopOrder.Volume != volume || Math.Abs(_exitStopOrder.Price - stopPrice) > priceStep / 2m)
			{
				CancelOrderSafe(ref _exitStopOrder);
				_exitStopOrder = position > 0m
				? SellStop(volume, stopPrice)
				: BuyStop(volume, stopPrice);
			}
		}
		else
		{
			CancelOrderSafe(ref _exitStopOrder);
		}
	}

	private void UpdateTrailingStop(decimal priceStep)
	{
		var trailingDistance = TrailingStopPoints * priceStep;
		if (trailingDistance <= 0m || _avgEntryPrice <= 0m)
		return;

		var position = _signedPosition;
		if (position == 0m)
		return;

		var bid = _bestBid ?? 0m;
		var ask = _bestAsk ?? 0m;
		var tolerance = Math.Max(priceStep / 2m, priceStep);

		if (position > 0m && bid > 0m && bid - _avgEntryPrice >= trailingDistance)
		{
			var desiredStop = bid - trailingDistance;
			if (desiredStop > _avgEntryPrice)
			{
				if (_exitStopOrder == null || desiredStop > _exitStopOrder.Price + tolerance)
				{
					CancelOrderSafe(ref _exitStopOrder);
					_exitStopOrder = SellStop(Math.Abs(position), desiredStop);
				}
			}
		}
		else if (position < 0m && ask > 0m && _avgEntryPrice - ask >= trailingDistance)
		{
			var desiredStop = ask + trailingDistance;
			if (desiredStop < _avgEntryPrice)
			{
				if (_exitStopOrder == null || desiredStop < _exitStopOrder.Price - tolerance)
				{
					CancelOrderSafe(ref _exitStopOrder);
					_exitStopOrder = BuyStop(Math.Abs(position), desiredStop);
				}
			}
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null || trade.Trade == null)
		return;

		UpdatePositionState(trade);
	}

	private void UpdatePositionState(MyTrade trade)
	{
		var volume = trade.Trade.Volume ?? 0m;
		var price = trade.Trade.Price ?? 0m;
		var side = trade.Order.Side;
		if (volume <= 0m || price <= 0m || side == null)
		return;

		var signedVolume = side == Sides.Buy ? volume : -volume;
		var prevPosition = _signedPosition;
		var newPosition = prevPosition + signedVolume;

		if (prevPosition == 0m)
		{
			_avgEntryPrice = price;
		}
		else if (prevPosition > 0m)
		{
			if (side == Sides.Buy)
			{
				if (newPosition > 0m)
				_avgEntryPrice = (_avgEntryPrice * prevPosition + price * volume) / newPosition;
				else if (newPosition <= 0m)
				_avgEntryPrice = price;
			}
			else
			{
				if (newPosition <= 0m)
				_avgEntryPrice = newPosition == 0m ? 0m : price;
			}
		}
		else if (prevPosition < 0m)
		{
			if (side == Sides.Sell)
			{
				if (newPosition < 0m)
				_avgEntryPrice = (_avgEntryPrice * -prevPosition + price * volume) / -newPosition;
				else if (newPosition >= 0m)
				_avgEntryPrice = price;
			}
			else
			{
				if (newPosition >= 0m)
				_avgEntryPrice = newPosition == 0m ? 0m : price;
			}
		}

		_signedPosition = newPosition;

		if (_signedPosition == 0m)
		{
			_avgEntryPrice = 0m;
			CancelOrderSafe(ref _exitStopOrder);
			CancelOrderSafe(ref _exitTakeProfitOrder);
		}
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order == null)
		return;

		if (order == _buyStopOrder && order.State.IsFinal())
		_buyStopOrder = null;
		else if (order == _sellStopOrder && order.State.IsFinal())
		_sellStopOrder = null;
		else if (order == _exitStopOrder && order.State.IsFinal())
		_exitStopOrder = null;
		else if (order == _exitTakeProfitOrder && order.State.IsFinal())
		_exitTakeProfitOrder = null;
	}

	private static bool IsOrderActive(Order order)
	{
		return order != null && order.State == OrderStates.Active;
	}

	private void CancelOrderSafe(ref Order order)
	{
		if (order == null)
		return;

		if (order.State == OrderStates.Active)
		CancelOrder(order);

		order = null;
	}
}
