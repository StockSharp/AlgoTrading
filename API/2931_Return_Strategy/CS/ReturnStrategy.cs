using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid based return strategy converted from the original MQL5 expert.
/// Places symmetric limit orders around the current price and manages trailing exits.
/// </summary>
public class ReturnStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<decimal> _totalProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _distancePips;
	private readonly StrategyParam<decimal> _stepPips;
	private readonly StrategyParam<int> _pendingOrderCount;
	private readonly StrategyParam<int> _expirationHours;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Dictionary<Order, DateTimeOffset?> _pendingOrders = new();

	private bool _shouldCloseAll;
	private decimal? _longStop;
	private decimal? _shortStop;

	/// <summary>
	/// Stop loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Hour to start placing pending orders.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Hour to force all orders and positions to close.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Target profit in pips that triggers a full exit.
	/// </summary>
	public decimal TotalProfitPips
	{
		get => _totalProfitPips.Value;
		set => _totalProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Distance from current price for the first pending order in pips.
	/// </summary>
	public decimal DistancePips
	{
		get => _distancePips.Value;
		set => _distancePips.Value = value;
	}

	/// <summary>
	/// Additional spacing between consecutive orders in pips.
	/// </summary>
	public decimal StepPips
	{
		get => _stepPips.Value;
		set => _stepPips.Value = value;
	}

	/// <summary>
	/// Number of pending buy and sell orders to place.
	/// </summary>
	public int PendingOrderCount
	{
		get => _pendingOrderCount.Value;
		set => _pendingOrderCount.Value = value;
	}

	/// <summary>
	/// Expiration for pending orders in hours. Zero disables expiration.
	/// </summary>
	public int ExpirationHours
	{
		get => _expirationHours.Value;
		set => _expirationHours.Value = value;
	}

	/// <summary>
	/// Fixed volume per pending order. Set to zero to use risk percent sizing.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Risk percent used when order volume is zero.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Candle type for timing logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public ReturnStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 1300m)
			.SetDisplay("Stop Loss Pips", "Stop loss in pips", "Risk");

		_startHour = Param(nameof(StartHour), 21)
			.SetDisplay("Start Hour", "Hour to place pending orders", "Timing");

		_endHour = Param(nameof(EndHour), 2)
			.SetDisplay("End Hour", "Hour to close everything", "Timing");

		_totalProfitPips = Param(nameof(TotalProfitPips), 100m)
			.SetDisplay("Total Profit Pips", "Profit target in pips", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetDisplay("Trailing Stop Pips", "Trailing stop distance", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetDisplay("Trailing Step Pips", "Trailing step distance", "Risk");

		_distancePips = Param(nameof(DistancePips), 25m)
			.SetDisplay("Distance Pips", "Initial distance for limit orders", "Orders");

		_stepPips = Param(nameof(StepPips), 5m)
			.SetDisplay("Step Pips", "Additional spacing between orders", "Orders");

		_pendingOrderCount = Param(nameof(PendingOrderCount), 4)
			.SetDisplay("Pending Orders", "Number of orders per side", "Orders");

		_expirationHours = Param(nameof(ExpirationHours), 4)
			.SetDisplay("Expiration Hours", "Pending order lifetime", "Orders");

		_orderVolume = Param(nameof(OrderVolume), 0m)
			.SetDisplay("Order Volume", "Fixed volume per order", "Orders");

		_riskPercent = Param(nameof(RiskPercent), 5m)
			.SetDisplay("Risk Percent", "Risk percent for position sizing", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for timing", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pendingOrders.Clear();
		_shouldCloseAll = false;
		_longStop = null;
		_shortStop = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (StartHour < 0 || StartHour > 23)
			throw new InvalidOperationException("Start hour must be between 0 and 23.");

		if (EndHour < 0 || EndHour > 23)
			throw new InvalidOperationException("End hour must be between 0 and 23.");

		if (TrailingStopPips > 0m && TrailingStepPips <= 0m)
			throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");

		if (OrderVolume <= 0m && RiskPercent <= 0m)
			throw new InvalidOperationException("Either order volume or risk percent must be positive.");

		if (OrderVolume > 0m && RiskPercent > 0m)
			throw new InvalidOperationException("Use either order volume or risk percent, not both.");

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		CleanupOrders();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		ManageStops(candle);

		if (!_shouldCloseAll && TotalProfitPips > 0m && Position != 0)
		{
			var profit = CalculateOpenProfit(candle.ClosePrice);
			if (profit >= GetTotalProfitTarget())
				_shouldCloseAll = true;
		}

		if (!_shouldCloseAll && ShouldTriggerSessionExit(candle.OpenTime))
			_shouldCloseAll = true;

		if (_shouldCloseAll)
		{
			TryCloseAll();
			return;
		}

		if (candle.OpenTime.Hour == StartHour && !HasActivePendingOrders())
			PlaceGridOrders(candle.ClosePrice);
	}

	private void ManageStops(ICandleMessage candle)
	{
		var stopDistance = GetStopLossDistance();
		var trailingDistance = GetTrailingStopDistance();
		var trailingStep = GetTrailingStepDistance();
		var closePrice = candle.ClosePrice;

		if (Position > 0)
		{
			if (stopDistance > 0m && !_longStop.HasValue)
				_longStop = PositionPrice - stopDistance;

			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
			{
				SellMarket(Math.Abs(Position));
				_longStop = null;
				_shortStop = null;
				return;
			}

			if (trailingDistance > 0m && trailingStep > 0m)
			{
				var entry = PositionPrice;
				var minAdvance = trailingDistance + trailingStep;
				if (closePrice - entry > minAdvance)
				{
					var minStop = closePrice - minAdvance;
					var newStop = closePrice - trailingDistance;
					if (!_longStop.HasValue || _longStop.Value < minStop)
						_longStop = newStop;
				}
			}
		}
		else if (Position < 0)
		{
			if (stopDistance > 0m && !_shortStop.HasValue)
				_shortStop = PositionPrice + stopDistance;

			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
			{
				BuyMarket(Math.Abs(Position));
				_shortStop = null;
				_longStop = null;
				return;
			}

			if (trailingDistance > 0m && trailingStep > 0m)
			{
				var entry = PositionPrice;
				var minAdvance = trailingDistance + trailingStep;
				if (entry - closePrice > minAdvance)
				{
					var maxStop = closePrice + minAdvance;
					var newStop = closePrice + trailingDistance;
					if (!_shortStop.HasValue || _shortStop.Value > maxStop)
						_shortStop = newStop;
				}
			}
		}
	}

	private void PlaceGridOrders(decimal referencePrice)
	{
		if (PendingOrderCount <= 0)
			return;

		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return;

		var distance = GetDistanceOffset();
		var step = GetStepOffset();
		if (distance <= 0m && step <= 0m)
			return;

		var expiration = ExpirationHours > 0
			? CurrentTime + TimeSpan.FromHours(ExpirationHours)
			: (DateTimeOffset?)null;

		for (var i = 0; i < PendingOrderCount; i++)
		{
			var offset = distance + (i + 1) * step;
			if (offset <= 0m)
				continue;

			var buyPrice = referencePrice - offset;
			if (buyPrice > 0m)
			{
				var buyOrder = BuyLimit(buyPrice, volume);
				if (buyOrder != null)
					_pendingOrders[buyOrder] = expiration;
			}

			var sellPrice = referencePrice + offset;
			var sellOrder = SellLimit(sellPrice, volume);
			if (sellOrder != null)
				_pendingOrders[sellOrder] = expiration;
		}
	}

	private void CleanupOrders()
	{
		foreach (var pair in _pendingOrders.ToArray())
		{
			var order = pair.Key;
			if (!IsOrderStillActive(order))
			{
				_pendingOrders.Remove(order);
				continue;
			}

			var expiration = pair.Value;
			if (expiration.HasValue && CurrentTime >= expiration.Value)
				CancelOrder(order);
		}
	}

	private bool HasActivePendingOrders()
	{
		return _pendingOrders.Keys.Any(IsOrderStillActive);
	}

	private static bool IsOrderStillActive(Order order)
	{
		if (order == null)
			return false;

		return order.State == OrderStates.Active;
	}

	private decimal CalculateOrderVolume()
	{
		if (OrderVolume > 0m)
			return OrderVolume;

		if (RiskPercent <= 0m || PendingOrderCount <= 0)
			return 0m;

		var equity = Portfolio?.CurrentValue ?? 0m;
		if (equity <= 0m)
			return 0m;

		var stopDistance = GetStopLossDistance();
		if (stopDistance <= 0m)
			return 0m;

		var stepPrice = Security?.StepPrice ?? 0m;
		var priceStep = Security?.Step ?? 0m;

		decimal riskPerUnit;
		if (stepPrice > 0m && priceStep > 0m)
			riskPerUnit = stopDistance / priceStep * stepPrice;
		else
			riskPerUnit = stopDistance;

		if (riskPerUnit <= 0m)
			return 0m;

		var riskValue = equity * RiskPercent / 100m;
		if (riskValue <= 0m)
			return 0m;

		var perOrderRisk = riskValue / PendingOrderCount;
		return perOrderRisk / riskPerUnit;
	}

	private decimal CalculateOpenProfit(decimal currentPrice)
	{
		if (Position > 0)
			return currentPrice - PositionPrice;

		if (Position < 0)
			return PositionPrice - currentPrice;

		return 0m;
	}

	private void TryCloseAll()
	{
		if (Position != 0)
			ClosePosition();

		foreach (var order in _pendingOrders.Keys.ToArray())
			if (IsOrderStillActive(order))
				CancelOrder(order);

		CleanupOrders();

		if (Position == 0 && !HasActivePendingOrders())
			_shouldCloseAll = false;
	}

	private bool ShouldTriggerSessionExit(DateTimeOffset time)
	{
		if (time.DayOfWeek == DayOfWeek.Friday)
			return true;

		if (EndHour >= 0 && EndHour <= 23 && time.Hour == EndHour)
			return true;

		return false;
	}

	private decimal GetStopLossDistance() => StopLossPips * GetPipSize();

	private decimal GetTrailingStopDistance() => TrailingStopPips * GetPipSize();

	private decimal GetTrailingStepDistance() => TrailingStepPips * GetPipSize();

	private decimal GetDistanceOffset() => DistancePips * GetPipSize();

	private decimal GetStepOffset() => StepPips * GetPipSize();

	private decimal GetTotalProfitTarget() => TotalProfitPips * GetPipSize();

	private decimal GetPipSize()
	{
		var step = Security?.Step ?? 0m;
		if (step <= 0m)
			return 1m;

		var decimals = GetDecimalPlaces(step);
		return decimals == 3 || decimals == 5 ? step * 10m : step;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		var bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 0xFF;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Order != null)
			_pendingOrders.Remove(trade.Order);
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0)
		{
			var stopDistance = GetStopLossDistance();
			_longStop = stopDistance > 0m ? PositionPrice - stopDistance : null;
			_shortStop = null;
		}
		else if (Position < 0)
		{
			var stopDistance = GetStopLossDistance();
			_shortStop = stopDistance > 0m ? PositionPrice + stopDistance : null;
			_longStop = null;
		}
		else
		{
			_longStop = null;
			_shortStop = null;
		}
	}
}
