using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Scheduled breakout strategy that places stop orders around the current price and manages trailing exits.
/// </summary>
public class ENewsLuckyStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _distancePips;
	private readonly StrategyParam<TimeSpan> _placementTime;
	private readonly StrategyParam<TimeSpan> _cancelTime;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private Order? _buyStopOrder;
	private Order? _sellStopOrder;
	private Order? _longStopOrder;
	private Order? _longTakeOrder;
	private Order? _shortStopOrder;
	private Order? _shortTakeOrder;
	private decimal? _plannedLongStop;
	private decimal? _plannedLongTarget;
	private decimal? _plannedShortStop;
	private decimal? _plannedShortTarget;
	private decimal? _longStopLevel;
	private decimal? _shortStopLevel;
	private decimal? _longTakePrice;
	private decimal? _shortTakePrice;
	private decimal _prevPosition;
	private DateTime? _lastPlacementDate;
	private DateTime? _lastCancelDate;

	/// <summary>
	/// Order volume used for pending entries.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips relative to the entry order.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips relative to the entry order.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
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
	/// Minimum trailing step in pips before the stop is moved again.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Distance in pips from the current price to place the stop orders.
	/// </summary>
	public decimal DistancePips
	{
		get => _distancePips.Value;
		set => _distancePips.Value = value;
	}

	/// <summary>
	/// Time of day when the pending stop orders are submitted.
	/// </summary>
	public TimeSpan PlacementTime
	{
		get => _placementTime.Value;
		set => _placementTime.Value = value;
	}

	/// <summary>
	/// Time of day when all pending orders are cancelled and open positions are closed.
	/// </summary>
	public TimeSpan CancelTime
	{
		get => _cancelTime.Value;
		set => _cancelTime.Value = value;
	}

	/// <summary>
	/// Candle type used for time tracking and trailing logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ENewsLuckyStrategy"/> class.
	/// </summary>
	public ENewsLuckyStrategy()
	{
		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetDisplay("Stop Loss", "Stop loss in pips", "Trading")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 150m)
			.SetDisplay("Take Profit", "Take profit in pips", "Trading")
			.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 5m)
			.SetDisplay("Trailing Stop", "Trailing distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
			.SetDisplay("Trailing Step", "Minimum trailing step in pips", "Risk");

		_distancePips = Param(nameof(DistancePips), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Entry Distance", "Distance from market in pips", "Trading")
			.SetCanOptimize(true);

		_placementTime = Param(nameof(PlacementTime), new TimeSpan(10, 30, 0))
			.SetDisplay("Placement Time", "Time to place pending orders", "General");

		_cancelTime = Param(nameof(CancelTime), new TimeSpan(22, 30, 0))
			.SetDisplay("Cancel Time", "Time to cancel orders and close", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");
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

		_buyStopOrder = null;
		_sellStopOrder = null;
		_longStopOrder = null;
		_longTakeOrder = null;
		_shortStopOrder = null;
		_shortTakeOrder = null;
		_plannedLongStop = null;
		_plannedLongTarget = null;
		_plannedShortStop = null;
		_plannedShortTarget = null;
		_longStopLevel = null;
		_shortStopLevel = null;
		_longTakePrice = null;
		_shortTakePrice = null;
		_prevPosition = 0m;
		_lastPlacementDate = null;
		_lastCancelDate = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var localTime = candle.CloseTime.ToLocalTime();
		HandleSchedule(localTime, candle);
		HandlePositionTransitions();
		ManageTrailing(candle);
	}

	private void HandleSchedule(DateTimeOffset time, ICandleMessage candle)
	{
		var date = time.Date;
		var tod = time.TimeOfDay;

		if (_lastPlacementDate != date && IsTimeMatch(tod, PlacementTime))
		{
			PlacePendingOrders(candle);
			_lastPlacementDate = date;
		}

		if (_lastCancelDate != date && IsTimeMatch(tod, CancelTime))
		{
			CancelPendingOrders();
			ExitPositions();
			_lastCancelDate = date;
		}
	}

	private static bool IsTimeMatch(TimeSpan actual, TimeSpan target)
	{
		return actual.Hours == target.Hours && actual.Minutes == target.Minutes;
	}

	private void PlacePendingOrders(ICandleMessage candle)
	{
		CancelPendingOrders();

		if (Volume <= 0m || DistancePips <= 0m)
			return;

		var price = candle.ClosePrice;
		var volume = Volume;
		var distance = DistancePips * _pipSize;

		var buyPrice = NormalizePrice(price + distance);
		var sellPrice = NormalizePrice(price - distance);

		if (buyPrice <= 0m || sellPrice <= 0m)
			return;

		_buyStopOrder = BuyStop(volume, buyPrice);
		_sellStopOrder = SellStop(volume, sellPrice);

		_plannedLongStop = StopLossPips > 0m ? NormalizePrice(buyPrice - StopLossPips * _pipSize) : null;
		_plannedLongTarget = TakeProfitPips > 0m ? NormalizePrice(buyPrice + TakeProfitPips * _pipSize) : null;
		_plannedShortStop = StopLossPips > 0m ? NormalizePrice(sellPrice + StopLossPips * _pipSize) : null;
		_plannedShortTarget = TakeProfitPips > 0m ? NormalizePrice(sellPrice - TakeProfitPips * _pipSize) : null;
	}

	private void CancelPendingOrders()
	{
		CancelOrderIfActive(ref _buyStopOrder);
		CancelOrderIfActive(ref _sellStopOrder);
		_plannedLongStop = null;
		_plannedLongTarget = null;
		_plannedShortStop = null;
		_plannedShortTarget = null;
	}

	private void ExitPositions()
	{
		if (Position > 0m)
			SellMarket(Position);
		else if (Position < 0m)
			BuyMarket(-Position);
	}

	private void HandlePositionTransitions()
	{
		var current = Position;

		if (current > 0m && _prevPosition <= 0m)
		{
			OnEnteredLong();
		}
		else if (current < 0m && _prevPosition >= 0m)
		{
			OnEnteredShort();
		}
		else if (current == 0m)
		{
			if (_prevPosition > 0m)
				OnExitedLong();
			else if (_prevPosition < 0m)
				OnExitedShort();
		}

		_prevPosition = current;
	}

	private void OnEnteredLong()
	{
		CancelOrderIfActive(ref _sellStopOrder);

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			volume = Volume;

		if (_plannedLongStop.HasValue)
			UpdateLongStop(_plannedLongStop.Value, volume);
		else if (StopLossPips > 0m)
			UpdateLongStop(NormalizePrice(PositionPrice - StopLossPips * _pipSize), volume);

		if (_plannedLongTarget.HasValue)
			UpdateLongTarget(_plannedLongTarget.Value, volume);
		else if (TakeProfitPips > 0m)
			UpdateLongTarget(NormalizePrice(PositionPrice + TakeProfitPips * _pipSize), volume);

		_plannedLongStop = null;
		_plannedLongTarget = null;
	}

	private void OnEnteredShort()
	{
		CancelOrderIfActive(ref _buyStopOrder);

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			volume = Volume;

		if (_plannedShortStop.HasValue)
			UpdateShortStop(_plannedShortStop.Value, volume);
		else if (StopLossPips > 0m)
			UpdateShortStop(NormalizePrice(PositionPrice + StopLossPips * _pipSize), volume);

		if (_plannedShortTarget.HasValue)
			UpdateShortTarget(_plannedShortTarget.Value, volume);
		else if (TakeProfitPips > 0m)
			UpdateShortTarget(NormalizePrice(PositionPrice - TakeProfitPips * _pipSize), volume);

		_plannedShortStop = null;
		_plannedShortTarget = null;
	}

	private void OnExitedLong()
	{
		CancelOrderIfActive(ref _longStopOrder);
		CancelOrderIfActive(ref _longTakeOrder);
		_longStopLevel = null;
		_longTakePrice = null;
	}

	private void OnExitedShort()
	{
		CancelOrderIfActive(ref _shortStopOrder);
		CancelOrderIfActive(ref _shortTakeOrder);
		_shortStopLevel = null;
		_shortTakePrice = null;
	}

	private void ManageTrailing(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m || _pipSize <= 0m)
			return;

		var distance = TrailingStopPips * _pipSize;
		var step = TrailingStepPips * _pipSize;

		if (Position > 0m && _longStopLevel.HasValue)
		{
			var volume = Math.Abs(Position);
			if (volume <= 0m)
				return;

			var entry = PositionPrice;
			var current = candle.ClosePrice;
			var desired = NormalizePrice(current - distance);
			var moveEnough = desired > _longStopLevel.Value + (step > 0m ? step : 0m);
			var profitable = current - entry > distance + (step > 0m ? step : 0m);

			if (profitable && moveEnough)
				UpdateLongStop(desired, volume);
		}
		else if (Position < 0m && _shortStopLevel.HasValue)
		{
			var volume = Math.Abs(Position);
			if (volume <= 0m)
				return;

			var entry = PositionPrice;
			var current = candle.ClosePrice;
			var desired = NormalizePrice(current + distance);
			var moveEnough = desired < _shortStopLevel.Value - (step > 0m ? step : 0m);
			var profitable = entry - current > distance + (step > 0m ? step : 0m);

			if (profitable && moveEnough)
				UpdateShortStop(desired, volume);
		}
	}

	private void UpdateLongStop(decimal price, decimal volume)
	{
		CancelOrderIfActive(ref _longStopOrder);
		if (volume <= 0m)
			return;

		_longStopLevel = price;
		_longStopOrder = SellStop(volume, price);
	}

	private void UpdateShortStop(decimal price, decimal volume)
	{
		CancelOrderIfActive(ref _shortStopOrder);
		if (volume <= 0m)
			return;

		_shortStopLevel = price;
		_shortStopOrder = BuyStop(volume, price);
	}

	private void UpdateLongTarget(decimal price, decimal volume)
	{
		CancelOrderIfActive(ref _longTakeOrder);
		if (volume <= 0m)
			return;

		_longTakePrice = price;
		_longTakeOrder = SellLimit(volume, price);
	}

	private void UpdateShortTarget(decimal price, decimal volume)
	{
		CancelOrderIfActive(ref _shortTakeOrder);
		if (volume <= 0m)
			return;

		_shortTakePrice = price;
		_shortTakeOrder = BuyLimit(volume, price);
	}

	private void CancelOrderIfActive(ref Order? order)
	{
		if (order == null)
			return;

		if (order.State == OrderStates.Active)
			CancelOrder(order);

		order = null;
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security.PriceStep ?? 1m;
		var decimals = Security.Decimals ?? 0;

		return decimals is 3 or 5 ? priceStep * 10m : priceStep;
	}

	private decimal NormalizePrice(decimal price)
	{
		var step = Security.PriceStep;
		if (step == null || step.Value <= 0m)
			return price;

		var steps = Math.Round(price / step.Value, MidpointRounding.AwayFromZero);
		return steps * step.Value;
	}
}
