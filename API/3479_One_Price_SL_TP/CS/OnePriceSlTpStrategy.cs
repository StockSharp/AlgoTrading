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

public class OnePriceSlTpStrategy : Strategy
{
	private readonly StrategyParam<decimal> _zenPrice;

	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal _priceStep;

	private Order _stopOrder;
	private Order _takeProfitOrder;

	public OnePriceSlTpStrategy()
	{
		_zenPrice = Param(nameof(ZenPrice), 0m)
		.SetNotNegative()
		.SetDisplay("Target price", "Absolute price level applied to stop loss or take profit depending on the current market side.", "Execution")
		.SetCanOptimize(true);
	}

	public decimal ZenPrice
	{
		get => _zenPrice.Value;
		set
		{
			_zenPrice.Value = value;
			TryUpdateProtection();
		}
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, DataType.Level1)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bestBid = null;
		_bestAsk = null;
		_priceStep = 0m;

		_stopOrder = null;
		_takeProfitOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = GetPriceStep();

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();

		TryUpdateProtection();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		CancelProtectiveOrder(ref _stopOrder);
		CancelProtectiveOrder(ref _takeProfitOrder);
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		TryUpdateProtection();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		TryUpdateProtection();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid)
		_bestBid = bid;

		if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask)
		_bestAsk = ask;

		TryUpdateProtection();
	}

	private void TryUpdateProtection()
	{
		if (ProcessState != ProcessStates.Started)
		return;

		var zen = ZenPrice;
		if (zen <= 0m)
		{
			// Cancel protective orders when the target price is disabled.
			CancelProtectiveOrder(ref _stopOrder);
			CancelProtectiveOrder(ref _takeProfitOrder);
			return;
		}

		var position = Position;
		if (position == 0m)
		{
			// Nothing to protect when there is no active position.
			CancelProtectiveOrder(ref _stopOrder);
			CancelProtectiveOrder(ref _takeProfitOrder);
			return;
		}

		var volume = NormalizeVolume(Math.Abs(position));
		if (volume <= 0m)
		{
			CancelProtectiveOrder(ref _stopOrder);
			CancelProtectiveOrder(ref _takeProfitOrder);
			return;
		}

		var isLong = position > 0m;

		decimal? stopPrice = null;
		decimal? takePrice = null;

		if (isLong)
		{
			if (_bestAsk is decimal ask && zen > ask)
			// Long positions use a limit order above the market as a take profit.
			takePrice = zen;

			if (_bestBid is decimal bid && zen < bid)
			// Long positions use a stop order below the market as a protective stop loss.
			stopPrice = zen;
		}
		else
		{
			if (_bestBid is decimal bid && zen < bid)
			// Short positions place the take profit below the current bid.
			takePrice = zen;

			if (_bestAsk is decimal ask && zen > ask)
			// Short positions raise the stop loss above the current ask.
			stopPrice = zen;
		}

		UpdateStopOrder(stopPrice, volume, isLong);
		UpdateTakeProfitOrder(takePrice, volume, isLong);
	}

	private void UpdateStopOrder(decimal? targetPrice, decimal volume, bool isLong)
	{
		if (targetPrice == null)
		{
			CancelProtectiveOrder(ref _stopOrder);
			return;
		}

		var normalizedPrice = NormalizePrice(targetPrice.Value);
		if (normalizedPrice <= 0m)
		{
			CancelProtectiveOrder(ref _stopOrder);
			return;
		}

		if (_stopOrder == null)
		{
			// Create a new protective stop order aligned with the current position direction.
			_stopOrder = isLong
			? SellStop(volume, normalizedPrice)
			: BuyStop(volume, normalizedPrice);
			return;
		}

		if (_stopOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		{
			_stopOrder = null;
			UpdateStopOrder(targetPrice, volume, isLong);
			return;
		}

		if (_stopOrder.Price != normalizedPrice || _stopOrder.Volume != volume)
		{
			// Re-register the existing stop order to reflect the latest target price.
			ReRegisterOrder(_stopOrder, normalizedPrice, volume);
		}
	}

	private void UpdateTakeProfitOrder(decimal? targetPrice, decimal volume, bool isLong)
	{
		if (targetPrice == null)
		{
			CancelProtectiveOrder(ref _takeProfitOrder);
			return;
		}

		var normalizedPrice = NormalizePrice(targetPrice.Value);
		if (normalizedPrice <= 0m)
		{
			CancelProtectiveOrder(ref _takeProfitOrder);
			return;
		}

		if (_takeProfitOrder == null)
		{
			// Create a new take profit order on the appropriate side of the order book.
			_takeProfitOrder = isLong
			? SellLimit(volume, normalizedPrice)
			: BuyLimit(volume, normalizedPrice);
			return;
		}

		if (_takeProfitOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		{
			_takeProfitOrder = null;
			UpdateTakeProfitOrder(targetPrice, volume, isLong);
			return;
		}

		if (_takeProfitOrder.Price != normalizedPrice || _takeProfitOrder.Volume != volume)
		{
			// Adjust the working take profit order without canceling it on the exchange side.
			ReRegisterOrder(_takeProfitOrder, normalizedPrice, volume);
		}
	}

	private decimal GetPriceStep()
	{
		var security = Security;
		if (security == null)
		return 0m;

		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
		{
			var decimals = security.Decimals;
			if (decimals != null && decimals.Value > 0)
			step = (decimal)Math.Pow(10, -decimals.Value);
		}

		return step;
	}

	private decimal NormalizePrice(decimal price)
	{
		if (_priceStep <= 0m)
		return price;

		var steps = Math.Round(price / _priceStep, MidpointRounding.AwayFromZero);
		return steps * _priceStep;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
		return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Round(volume / step, MidpointRounding.AwayFromZero);
			volume = steps * step;
		}

		var min = security.MinVolume;
		if (min != null && volume < min.Value)
		volume = min.Value;

		var max = security.MaxVolume;
		if (max != null && volume > max.Value)
		volume = max.Value;

		return volume;
	}

	private void CancelProtectiveOrder(ref Order order)
	{
		if (order == null)
		return;

		if (order.State is OrderStates.Active or OrderStates.Pending)
		CancelOrder(order);

		order = null;
	}
}

