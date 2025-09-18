namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class BadOrdersStrategy : Strategy
{
	private readonly StrategyParam<decimal> _distancePoints;

	private Order? _stopOrder;
	private decimal _bestBid;
	private bool _hasBid;
	private decimal _pointValue;

	public decimal DistancePoints
	{
		get => _distancePoints.Value;
		set => _distancePoints.Value = value;
	}

	public BadOrdersStrategy()
	{
		_distancePoints = Param(nameof(DistancePoints), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Distance (points)", "Offset in points applied around the current bid", "General");
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_stopOrder = null;
		_bestBid = 0m;
		_hasBid = false;
		_pointValue = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointValue = CalculatePointValue();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue))
		{
			var bid = (decimal)bidValue;
			if (bid > 0m)
			{
				_bestBid = bid;
				_hasBid = true;
			}
		}

		if (_hasBid)
			ManageOrders();
	}

	private void ManageOrders()
	{
		if (_pointValue <= 0m)
			return;

		if (Position != 0 && !HasClosingOrder())
			ClosePosition();

		var upperPrice = NormalizePrice(_bestBid + _pointValue * DistancePoints);
		var lowerPrice = NormalizePrice(_bestBid - _pointValue * DistancePoints);

		if (upperPrice <= 0m || lowerPrice <= 0m)
			return;

		if (_stopOrder == null)
		{
			_stopOrder = BuyStop(Volume, upperPrice);
			return;
		}

		if (_stopOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		{
			_stopOrder = null;
			ManageOrders();
			return;
		}

		if (_stopOrder.Price != lowerPrice)
			ReRegisterOrder(_stopOrder, lowerPrice, Volume);
	}

	private bool HasClosingOrder()
	{
		if (Position > 0)
		{
			foreach (var order in ActiveOrders)
			{
				if (order.Direction == Sides.Sell)
					return true;
			}
		}
		else if (Position < 0)
		{
			foreach (var order in ActiveOrders)
			{
				if (order.Direction == Sides.Buy)
					return true;
			}
		}

		return false;
	}

	private decimal NormalizePrice(decimal price)
	{
		var security = Security;
		if (security == null)
			return price;

		return security.ShrinkPrice(price);
	}

	private decimal CalculatePointValue()
	{
		var security = Security;
		if (security == null)
			return 0.0001m;

		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
		{
			var decimals = security.Decimals;
			if (decimals != null && decimals.Value > 0)
				step = (decimal)Math.Pow(10, -decimals.Value);
		}

		return step > 0m ? step : 0.0001m;
	}
}
