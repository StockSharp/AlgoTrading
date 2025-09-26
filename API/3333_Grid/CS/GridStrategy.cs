using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid strategy that alternates long and short cycles while building layered limit orders.
/// </summary>
public class GridStrategy : Strategy
{
	private readonly StrategyParam<Sides> _firstTradeSide;
	private readonly StrategyParam<decimal> _startVolume;
	private readonly StrategyParam<decimal> _lotMultiplier;
	private readonly StrategyParam<decimal> _gridStepPoints;
	private readonly StrategyParam<decimal> _targetPoints;
	private readonly StrategyParam<DataType> _candleType;

	private Sides _nextCycleSide;
	private Sides? _activeCycleSide;
	private Order _pendingOrder;
	private decimal _lastOrderVolume;
	private decimal _gridStep;
	private decimal _targetDistance;
	private decimal _positionVolume;
	private decimal _weightedPriceSum;
	private decimal _lastFillPrice;
	private decimal? _takeProfitPrice;

	/// <summary>
	/// Creates strategy parameters.
	/// </summary>
	public GridStrategy()
	{
		_firstTradeSide = Param(nameof(FirstTradeSide), Sides.Buy)
		.SetDisplay("First Trade Side", "Initial direction for the first cycle", "General");

		_startVolume = Param(nameof(StartVolume), 0.01m)
		.SetDisplay("Start Volume", "Lot size of the very first order in a cycle", "Risk")
		.SetCanOptimize(true);

		_lotMultiplier = Param(nameof(LotMultiplier), 1m)
		.SetDisplay("Lot Multiplier", "Multiplier applied to every additional grid order", "Risk")
		.SetCanOptimize(true);

		_gridStepPoints = Param(nameof(GridStepPoints), 400m)
		.SetDisplay("Grid Step (pts)", "Distance between grid levels in points", "Grid")
		.SetCanOptimize(true);

		_targetPoints = Param(nameof(TargetPoints), 100m)
		.SetDisplay("Target (pts)", "Take-profit distance from the average entry price in points", "Grid")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle subscription used for TP monitoring", "General");
	}

	/// <summary>
	/// Direction of the first market order in a new cycle.
	/// </summary>
	public Sides FirstTradeSide
	{
		get => _firstTradeSide.Value;
		set => _firstTradeSide.Value = value;
	}

	/// <summary>
	/// Initial market volume in lots.
	/// </summary>
	public decimal StartVolume
	{
		get => _startVolume.Value;
		set => _startVolume.Value = value;
	}

	/// <summary>
	/// Multiplier for each subsequent grid order.
	/// </summary>
	public decimal LotMultiplier
	{
		get => _lotMultiplier.Value;
		set => _lotMultiplier.Value = value;
	}

	/// <summary>
	/// Distance between grid levels expressed in points.
	/// </summary>
	public decimal GridStepPoints
	{
		get => _gridStepPoints.Value;
		set => _gridStepPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in points.
	/// </summary>
	public decimal TargetPoints
	{
		get => _targetPoints.Value;
		set => _targetPoints.Value = value;
	}

	/// <summary>
	/// Candle type used to monitor price extremes.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetState();
		UpdatePriceMetrics();
		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order == null)
		return;

		if (_pendingOrder != null && ReferenceEquals(order, _pendingOrder) && IsFinalState(order))
		{
			_pendingOrder = null;
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade?.Order.Security != Security)
		return;

		var order = trade.Order;
		var tradeInfo = trade.Trade;
		var volume = tradeInfo?.Volume ?? 0m;
		var price = tradeInfo?.Price ?? order.Price ?? 0m;

		if (volume <= 0m || price <= 0m)
		return;

		var direction = order.Direction;

		if (_activeCycleSide == null)
		{
			_activeCycleSide = direction;
		}

		if (_activeCycleSide == direction)
		{
			// Update averaged entry information for the active cycle.
			_positionVolume += volume;
			_weightedPriceSum += price * volume;
			_lastOrderVolume = volume;
			_lastFillPrice = price;

			UpdateTakeProfit();
			PlaceNextOrderIfNeeded();
		}
		else
		{
			// Closing trades reduce the outstanding exposure.
			_positionVolume -= volume;
			if (_positionVolume <= 0m)
			{
				FinishCycle();
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdatePriceMetrics();

		CheckTakeProfit(candle);
		TryStartNewCycle();
	}

	private void TryStartNewCycle()
	{
		if (_activeCycleSide != null)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (Position != 0m)
		return;

		if (!AreActiveOrdersEmpty())
		return;

		if (StartVolume <= 0m)
		return;

		if (_gridStep <= 0m)
		return;

		StartNewCycle();
	}

	private void StartNewCycle()
	{
		_activeCycleSide = _nextCycleSide;
		_positionVolume = 0m;
		_weightedPriceSum = 0m;
		_takeProfitPrice = null;
		_lastOrderVolume = StartVolume;
		_lastFillPrice = 0m;

		SendMarketOrder(_activeCycleSide.Value, StartVolume);
	}

	private void PlaceNextOrderIfNeeded()
	{
		if (_activeCycleSide == null)
		return;

		if (_gridStep <= 0m)
		return;

		if (!AreActiveOrdersEmpty())
		return;

		var multiplier = LotMultiplier;
		if (multiplier <= 0m)
		return;

		var nextVolume = _lastOrderVolume * multiplier;
		if (nextVolume <= 0m)
		return;

		var price = _activeCycleSide == Sides.Buy
		? _lastFillPrice - _gridStep
		: _lastFillPrice + _gridStep;

		if (price <= 0m)
		return;

		_pendingOrder = _activeCycleSide == Sides.Buy
		? BuyLimit(nextVolume, price)
		: SellLimit(nextVolume, price);
	}

	private void CheckTakeProfit(ICandleMessage candle)
	{
		if (_activeCycleSide == null || _takeProfitPrice is not decimal target)
		return;

		if (_activeCycleSide == Sides.Buy)
		{
			// Exit the long cycle once the candle high reaches the profit target.
			if (candle.HighPrice >= target)
			{
				ClosePosition();
			}
		}
		else
		{
			// Exit the short cycle once the candle low reaches the profit target.
			if (candle.LowPrice <= target)
			{
				ClosePosition();
			}
		}
	}

	private void ClosePosition()
	{
		CancelPendingOrder();

		if (_activeCycleSide == Sides.Buy && Position > 0m)
		{
			SellMarket(Math.Abs(Position));
		}
		else if (_activeCycleSide == Sides.Sell && Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
		}
	}

	private void FinishCycle()
	{
		if (_activeCycleSide == null)
			return;

		CancelPendingOrder();
		CancelActiveOrders();

		_positionVolume = 0m;
		_weightedPriceSum = 0m;
		_takeProfitPrice = null;
		_lastOrderVolume = StartVolume;
		_lastFillPrice = 0m;

		if (_activeCycleSide is Sides previous)
		{
			_nextCycleSide = previous == Sides.Buy ? Sides.Sell : Sides.Buy;
		}

		_activeCycleSide = null;
	}

	private void UpdatePriceMetrics()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return;

		_gridStep = step * GridStepPoints;
		_targetDistance = step * TargetPoints;
	}

	private void UpdateTakeProfit()
	{
		if (_positionVolume <= 0m)
		{
			_takeProfitPrice = null;
			return;
		}

		var distance = _targetDistance;
		if (distance <= 0m)
		{
			_takeProfitPrice = null;
			return;
		}

		var averagePrice = _weightedPriceSum / _positionVolume;
		_takeProfitPrice = _activeCycleSide == Sides.Buy
		? averagePrice + distance
		: averagePrice - distance;
	}

	private void CancelPendingOrder()
	{
		if (_pendingOrder == null)
		return;

		if (!IsFinalState(_pendingOrder))
		{
			CancelOrder(_pendingOrder);
		}

		_pendingOrder = null;
	}

	private void SendMarketOrder(Sides side, decimal volume)
	{
		if (volume <= 0m)
		return;

		if (side == Sides.Buy)
		{
			BuyMarket(volume);
		}
		else
		{
			SellMarket(volume);
		}
	}

	private bool AreActiveOrdersEmpty()
	{
		return ActiveOrders == null || ActiveOrders.Count == 0;
	}

	private void ResetState()
	{
		_pendingOrder = null;
		_activeCycleSide = null;
		_nextCycleSide = FirstTradeSide;
		_lastOrderVolume = StartVolume;
		_positionVolume = 0m;
		_weightedPriceSum = 0m;
		_lastFillPrice = 0m;
		_takeProfitPrice = null;
	}
}
