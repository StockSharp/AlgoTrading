namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Port of the MetaTrader expert SampleTrailingstop.
/// Applies trailing stop and take profit management based on broker restrictions.
/// </summary>
public class SampleTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLevelPoints;
	private readonly StrategyParam<decimal> _freezeLevelPoints;

	private Order? _stopOrder;
	private Order? _takeProfitOrder;

	private decimal? _currentStopPrice;
	private decimal? _currentTakePrice;
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal _pointValue;
	private decimal _priceStep;
	private decimal _lastPosition;

	/// <summary>
	/// Trailing distance expressed in price points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Broker stop level expressed in price points.
	/// </summary>
	public decimal StopLevelPoints
	{
		get => _stopLevelPoints.Value;
		set => _stopLevelPoints.Value = value;
	}

	/// <summary>
	/// Broker freeze level expressed in price points.
	/// </summary>
	public decimal FreezeLevelPoints
	{
		get => _freezeLevelPoints.Value;
		set => _freezeLevelPoints.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public SampleTrailingStopStrategy()
	{
		_trailingStopPoints = Param(nameof(TrailingStopPoints), 200m)
			.SetDisplay("Trailing Stop (points)", "Trailing distance in price points.", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 1000m)
			.SetDisplay("Take Profit (points)", "Take profit distance in price points.", "Risk");

		_stopLevelPoints = Param(nameof(StopLevelPoints), 0m)
			.SetDisplay("Stop Level (points)", "Broker stop level restriction in points.", "Risk");

		_freezeLevelPoints = Param(nameof(FreezeLevelPoints), 0m)
			.SetDisplay("Freeze Level (points)", "Broker freeze level restriction in points.", "Risk");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		CancelProtectiveOrder(ref _stopOrder);
		CancelProtectiveOrder(ref _takeProfitOrder);

		_currentStopPrice = null;
		_currentTakePrice = null;
		_bestBid = null;
		_bestAsk = null;
		_pointValue = 0m;
		_priceStep = 0m;
		_lastPosition = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointValue = CalculatePointValue();
		_priceStep = GetPriceStep();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		CancelProtectiveOrder(ref _stopOrder);
		CancelProtectiveOrder(ref _takeProfitOrder);

		base.OnStopped();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			CancelProtectiveOrder(ref _stopOrder);
			CancelProtectiveOrder(ref _takeProfitOrder);
			_currentStopPrice = null;
			_currentTakePrice = null;
		}
		else if (_lastPosition != 0m && Math.Sign(Position) != Math.Sign(_lastPosition))
		{
			// Reset protection when the position flips direction.
			CancelProtectiveOrder(ref _stopOrder);
			CancelProtectiveOrder(ref _takeProfitOrder);
			_currentStopPrice = null;
			_currentTakePrice = null;
		}

		_lastPosition = Position;

		TryUpdateTrailing();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj))
		{
			var bid = ToDecimal(bidObj);
			if (bid > 0m)
				_bestBid = bid;
		}

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj))
		{
			var ask = ToDecimal(askObj);
			if (ask > 0m)
				_bestAsk = ask;
		}

		TryUpdateTrailing();
	}

	private void TryUpdateTrailing()
	{
		if (Position == 0m || _pointValue <= 0m)
			return;

		if (_bestBid is not decimal bid || _bestAsk is not decimal ask)
			return;

		if (Position > 0m)
			UpdateLongTrailing(bid, ask);
		else
			UpdateShortTrailing(bid, ask);
	}

	private void UpdateLongTrailing(decimal bid, decimal ask)
	{
		if (TrailingStopPoints <= 0m)
			return;

		var entryPrice = PositionPrice;
		if (entryPrice is not decimal entry || entry <= 0m)
			return;

		if (bid <= entry)
			return;

		var trailingDistance = PointsToPrice(TrailingStopPoints);
		var stopDistance = PointsToPrice(StopLevelPoints);
		var freezeDistance = PointsToPrice(FreezeLevelPoints);
		var spreadDistance = Math.Max(0m, ask - bid);
		var totalTrailing = trailingDistance + stopDistance + spreadDistance + freezeDistance;

		if (totalTrailing <= 0m)
			return;

		if (ask - entry <= freezeDistance)
			return;

		var minimalDistance = stopDistance + spreadDistance;
		var validBuy = bid - minimalDistance;
		if (validBuy <= 0m)
			return;

		var takeDistance = TakeProfitPoints > 0m ? PointsToPrice(TakeProfitPoints) : 0m;

		if (_currentStopPrice is not decimal current || current <= entry)
		{
			if (bid < entry + totalTrailing)
				return;

			var stopPrice = validBuy;
			var takePrice = takeDistance > 0m ? bid + takeDistance : (decimal?)null;

			ApplyProtectiveOrders(true, stopPrice, takePrice);
			return;
		}

		if (_currentStopPrice <= validBuy)
		{
			var stopPrice = bid - totalTrailing;
			var takePrice = takeDistance > 0m ? bid + takeDistance : (decimal?)null;

			ApplyProtectiveOrders(true, stopPrice, takePrice);
		}
	}

	private void UpdateShortTrailing(decimal bid, decimal ask)
	{
		if (TrailingStopPoints <= 0m)
			return;

		var entryPrice = PositionPrice;
		if (entryPrice is not decimal entry || entry <= 0m)
			return;

		if (ask >= entry)
			return;

		var trailingDistance = PointsToPrice(TrailingStopPoints);
		var stopDistance = PointsToPrice(StopLevelPoints);
		var freezeDistance = PointsToPrice(FreezeLevelPoints);
		var spreadDistance = Math.Max(0m, ask - bid);
		var totalTrailing = trailingDistance + stopDistance + spreadDistance + freezeDistance;

		if (totalTrailing <= 0m)
			return;

		if (entry - bid <= freezeDistance)
			return;

		var minimalDistance = stopDistance + spreadDistance;
		var validSell = ask + minimalDistance;
		if (validSell <= 0m)
			return;

		var takeDistance = TakeProfitPoints > 0m ? PointsToPrice(TakeProfitPoints) : 0m;

		if (_currentStopPrice is not decimal current || current >= entry)
		{
			if (ask > entry - totalTrailing)
				return;

			var stopPrice = validSell;
			var takePrice = takeDistance > 0m ? ask - takeDistance : (decimal?)null;

			ApplyProtectiveOrders(false, stopPrice, takePrice);
			return;
		}

		if (_currentStopPrice >= validSell)
		{
			var stopPrice = ask + totalTrailing;
			var takePrice = takeDistance > 0m ? ask - takeDistance : (decimal?)null;

			ApplyProtectiveOrders(false, stopPrice, takePrice);
		}
	}

	private void ApplyProtectiveOrders(bool isLong, decimal stopPrice, decimal? takePrice)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		volume = NormalizeVolume(volume);
		if (volume <= 0m)
			return;

		var normalizedStop = NormalizePrice(stopPrice);
		var normalizedTake = takePrice is decimal take ? NormalizePrice(take) : (decimal?)null;

		if (normalizedStop <= 0m)
		{
			CancelProtectiveOrder(ref _stopOrder);
			_currentStopPrice = null;
		}
		else
		{
			_currentStopPrice = normalizedStop;
			UpdateStopOrder(isLong, normalizedStop, volume);
		}

		if (normalizedTake is null || normalizedTake <= 0m)
		{
			CancelProtectiveOrder(ref _takeProfitOrder);
			_currentTakePrice = null;
		}
		else
		{
			_currentTakePrice = normalizedTake;
			UpdateTakeProfitOrder(isLong, normalizedTake.Value, volume);
		}
	}

	private void UpdateStopOrder(bool isLong, decimal stopPrice, decimal volume)
	{
		if (_stopOrder == null)
		{
			_stopOrder = isLong
				? SellStop(volume, stopPrice)
				: BuyStop(volume, stopPrice);
			return;
		}

		if (_stopOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		{
			_stopOrder = null;
			UpdateStopOrder(isLong, stopPrice, volume);
			return;
		}

		if (_stopOrder.Price != stopPrice || _stopOrder.Volume != volume)
			ReRegisterOrder(_stopOrder, stopPrice, volume);
	}

	private void UpdateTakeProfitOrder(bool isLong, decimal takePrice, decimal volume)
	{
		if (_takeProfitOrder == null)
		{
			_takeProfitOrder = isLong
				? SellLimit(volume, takePrice)
				: BuyLimit(volume, takePrice);
			return;
		}

		if (_takeProfitOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		{
			_takeProfitOrder = null;
			UpdateTakeProfitOrder(isLong, takePrice, volume);
			return;
		}

		if (_takeProfitOrder.Price != takePrice || _takeProfitOrder.Volume != volume)
			ReRegisterOrder(_takeProfitOrder, takePrice, volume);
	}

	private decimal PointsToPrice(decimal points)
	{
		if (_pointValue <= 0m)
			return 0m;

		return points * _pointValue;
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

		if (step <= 0m)
			step = 0.0001m;

		var multiplier = 1m;
		var digits = security.Decimals;
		if (digits != null && (digits.Value == 3 || digits.Value == 5))
			multiplier = 10m;

		return step * multiplier;
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

	private static decimal ToDecimal(object value)
	{
		return value switch
		{
			null => 0m,
			decimal d => d,
			double dbl => (decimal)dbl,
			int i => i,
			long l => l,
			_ => 0m,
		};
	}

	private void CancelProtectiveOrder(ref Order? order)
	{
		if (order == null)
			return;

		if (order.State is OrderStates.Active or OrderStates.Pending)
			CancelOrder(order);

		order = null;
	}
}
