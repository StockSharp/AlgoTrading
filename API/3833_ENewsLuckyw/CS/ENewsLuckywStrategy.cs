namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Time-based straddle strategy converted from the MetaTrader expert advisor e-News-Lucky$.
/// Places pending breakout orders at a configured time, keeps them aligned with the market, and manages trailing exits.
/// </summary>
public class ENewsLuckywStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _distancePips;
	private readonly StrategyParam<bool> _useTrailing;
	private readonly StrategyParam<bool> _profitTrailing;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<TimeSpan> _setOrdersTime;
	private readonly StrategyParam<TimeSpan> _deleteOrdersTime;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private Order _longStopOrder;
	private Order _shortStopOrder;
	private Order _longTakeOrder;
	private Order _shortTakeOrder;
	private decimal? _plannedLongStop;
	private decimal? _plannedLongTarget;
	private decimal? _plannedShortStop;
	private decimal? _plannedShortTarget;
	private decimal? _longTrailingLevel;
	private decimal? _shortTrailingLevel;
	private decimal _previousPosition;
	private DateTime? _lastSetDate;
	private DateTime? _lastDeleteDate;


	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Offset in pips from the current price used to place the breakout orders.
	/// </summary>
	public decimal DistancePips
	{
		get => _distancePips.Value;
		set => _distancePips.Value = value;
	}

	/// <summary>
	/// Enables or disables stop trailing for opened positions.
	/// </summary>
	public bool UseTrailing
	{
		get => _useTrailing.Value;
		set => _useTrailing.Value = value;
	}

	/// <summary>
	/// Starts trailing only after the position reaches a profit equal to the trailing distance.
	/// </summary>
	public bool ProfitTrailing
	{
		get => _profitTrailing.Value;
		set => _profitTrailing.Value = value;
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
	/// Minimal improvement in pips required before the trailing stop is moved again.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Time of day for placing pending stop orders.
	/// </summary>
	public TimeSpan SetOrdersTime
	{
		get => _setOrdersTime.Value;
		set => _setOrdersTime.Value = value;
	}

	/// <summary>
	/// Time of day for cancelling pending orders and closing positions.
	/// </summary>
	public TimeSpan DeleteOrdersTime
	{
		get => _deleteOrdersTime.Value;
		set => _deleteOrdersTime.Value = value;
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
	/// Initializes default parameters.
	/// </summary>
	public ENewsLuckywStrategy()
	{

		_stopLossPips = Param(nameof(StopLossPips), 15m)
		.SetDisplay("Stop Loss", "Stop loss distance in pips", "Risk")
		.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 0m)
		.SetDisplay("Take Profit", "Take profit distance in pips", "Risk")
		.SetCanOptimize(true);

		_distancePips = Param(nameof(DistancePips), 20m)
		.SetDisplay("Entry Distance", "Offset from market price in pips", "Trading")
		.SetGreaterThanZero()
		.SetCanOptimize(true);

		_useTrailing = Param(nameof(UseTrailing), true)
		.SetDisplay("Use Trailing", "Enable stop trailing", "Risk");

		_profitTrailing = Param(nameof(ProfitTrailing), true)
		.SetDisplay("Profit Trailing", "Delay trailing until the trade is profitable", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 25m)
		.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
		.SetDisplay("Trailing Step", "Minimum step for stop adjustments", "Risk");

		_setOrdersTime = Param(nameof(SetOrdersTime), new TimeSpan(10, 30, 0))
		.SetDisplay("Set Time", "Time to place pending orders", "Schedule");

		_deleteOrdersTime = Param(nameof(DeleteOrdersTime), new TimeSpan(22, 30, 0))
		.SetDisplay("Delete Time", "Time to cancel orders and close", "Schedule");

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
		_shortStopOrder = null;
		_longTakeOrder = null;
		_shortTakeOrder = null;
		_plannedLongStop = null;
		_plannedLongTarget = null;
		_plannedShortStop = null;
		_plannedShortTarget = null;
		_longTrailingLevel = null;
		_shortTrailingLevel = null;
		_previousPosition = 0m;
		_lastSetDate = null;
		_lastDeleteDate = null;
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
		HandlePendingMaintenance(candle);
		HandlePositionTransitions();
		ManageTrailing(candle);
	}

	private void HandleSchedule(DateTimeOffset time, ICandleMessage candle)
	{
		var currentDate = time.Date;
		var timeOfDay = time.TimeOfDay;

		if (_lastSetDate != currentDate && IsSameMinute(timeOfDay, SetOrdersTime))
		{
		PlacePendingOrders(candle);
		_lastSetDate = currentDate;
		}

		if (_lastDeleteDate != currentDate && IsSameMinute(timeOfDay, DeleteOrdersTime))
		{
		CancelPendingOrders();
		ExitPositions();
		_lastDeleteDate = currentDate;
		}
	}

	private void HandlePendingMaintenance(ICandleMessage candle)
	{
		if (_buyStopOrder?.State == OrderStates.Active && _sellStopOrder?.State == OrderStates.Active)
		{
		var price = candle.ClosePrice;
		var distance = DistancePips * _pipSize;

		var buyPrice = NormalizePrice(price + distance);
		var sellPrice = NormalizePrice(price - distance);

		if (buyPrice > 0m && sellPrice > 0m)
		{
		ChangeOrder(_buyStopOrder, buyPrice, _buyStopOrder.Volume);
		ChangeOrder(_sellStopOrder, sellPrice, _sellStopOrder.Volume);

		_plannedLongStop = StopLossPips > 0m ? NormalizePrice(buyPrice - StopLossPips * _pipSize) : null;
		_plannedLongTarget = TakeProfitPips > 0m ? NormalizePrice(buyPrice + TakeProfitPips * _pipSize) : null;
		_plannedShortStop = StopLossPips > 0m ? NormalizePrice(sellPrice + StopLossPips * _pipSize) : null;
		_plannedShortTarget = TakeProfitPips > 0m ? NormalizePrice(sellPrice - TakeProfitPips * _pipSize) : null;
		}
		}

		if (Position > 0m)
		{
		CancelOrderIfActive(ref _sellStopOrder);
		}
		else if (Position < 0m)
		{
		CancelOrderIfActive(ref _buyStopOrder);
		}
	}

	private static bool IsSameMinute(TimeSpan actual, TimeSpan target)
	{
		return actual.Hours == target.Hours && actual.Minutes == target.Minutes;
	}

	private void PlacePendingOrders(ICandleMessage candle)
	{
		CancelPendingOrders();

		if (Volume <= 0m || DistancePips <= 0m)
		return;

		var distance = DistancePips * _pipSize;
		var price = candle.ClosePrice;

		var buyPrice = NormalizePrice(price + distance);
		var sellPrice = NormalizePrice(price - distance);

		if (buyPrice <= 0m || sellPrice <= 0m)
		return;

		_buyStopOrder = BuyStop(Volume, buyPrice);
		_sellStopOrder = SellStop(Volume, sellPrice);

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

		if (current > 0m && _previousPosition <= 0m)
		{
		OnEnteredLong();
		}
		else if (current < 0m && _previousPosition >= 0m)
		{
		OnEnteredShort();
		}
		else if (current == 0m)
		{
		if (_previousPosition > 0m)
		OnExitedLong();
		else if (_previousPosition < 0m)
		OnExitedShort();
		}

		_previousPosition = current;
	}

	private void OnEnteredLong()
	{
		CancelOrderIfActive(ref _sellStopOrder);

		var volume = Math.Abs(Position);
		if (volume <= 0m)
		volume = Volume;

		var stopPrice = _plannedLongStop ?? (StopLossPips > 0m ? NormalizePrice(PositionPrice - StopLossPips * _pipSize) : (decimal?)null);
		var takePrice = _plannedLongTarget ?? (TakeProfitPips > 0m ? NormalizePrice(PositionPrice + TakeProfitPips * _pipSize) : (decimal?)null);

		if (stopPrice.HasValue)
		UpdateLongStop(stopPrice.Value, volume);

		if (takePrice.HasValue)
		UpdateLongTake(takePrice.Value, volume);

		_plannedLongStop = null;
		_plannedLongTarget = null;
		_longTrailingLevel = _longStopOrder?.Price;
	}

	private void OnEnteredShort()
	{
		CancelOrderIfActive(ref _buyStopOrder);

		var volume = Math.Abs(Position);
		if (volume <= 0m)
		volume = Volume;

		var stopPrice = _plannedShortStop ?? (StopLossPips > 0m ? NormalizePrice(PositionPrice + StopLossPips * _pipSize) : (decimal?)null);
		var takePrice = _plannedShortTarget ?? (TakeProfitPips > 0m ? NormalizePrice(PositionPrice - TakeProfitPips * _pipSize) : (decimal?)null);

		if (stopPrice.HasValue)
		UpdateShortStop(stopPrice.Value, volume);

		if (takePrice.HasValue)
		UpdateShortTake(takePrice.Value, volume);

		_plannedShortStop = null;
		_plannedShortTarget = null;
		_shortTrailingLevel = _shortStopOrder?.Price;
	}

	private void OnExitedLong()
	{
		CancelOrderIfActive(ref _longStopOrder);
		CancelOrderIfActive(ref _longTakeOrder);
		_longTrailingLevel = null;
	}

	private void OnExitedShort()
	{
		CancelOrderIfActive(ref _shortStopOrder);
		CancelOrderIfActive(ref _shortTakeOrder);
		_shortTrailingLevel = null;
	}

	private void ManageTrailing(ICandleMessage candle)
	{
		if (!UseTrailing || TrailingStopPips <= 0m)
		return;

		var distance = TrailingStopPips * _pipSize;
		var step = TrailingStepPips > 0m ? TrailingStepPips * _pipSize : 0m;

		if (Position > 0m && _longStopOrder != null)
		{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
		return;

		var entryPrice = PositionPrice;
		var currentPrice = candle.ClosePrice;
		var desired = NormalizePrice(currentPrice - distance);
		var profitRequirement = ProfitTrailing ? distance : 0m;
		var currentProfit = currentPrice - entryPrice;

		if (currentProfit >= profitRequirement && desired > _longStopOrder.Price + step)
		{
		UpdateLongStop(desired, volume);
		_longTrailingLevel = desired;
		}
		}
		else if (Position < 0m && _shortStopOrder != null)
		{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
		return;

		var entryPrice = PositionPrice;
		var currentPrice = candle.ClosePrice;
		var desired = NormalizePrice(currentPrice + distance);
		var profitRequirement = ProfitTrailing ? distance : 0m;
		var currentProfit = entryPrice - currentPrice;

		if (currentProfit >= profitRequirement && desired < _shortStopOrder.Price - step)
		{
		UpdateShortStop(desired, volume);
		_shortTrailingLevel = desired;
		}
		}
	}

	private void UpdateLongStop(decimal price, decimal volume)
	{
	CancelOrderIfActive(ref _longStopOrder);
	if (volume <= 0m)
	return;

	_longStopOrder = SellStop(volume, price);
	}

	private void UpdateShortStop(decimal price, decimal volume)
	{
	CancelOrderIfActive(ref _shortStopOrder);
	if (volume <= 0m)
	return;

	_shortStopOrder = BuyStop(volume, price);
	}

	private void UpdateLongTake(decimal price, decimal volume)
	{
	CancelOrderIfActive(ref _longTakeOrder);
	if (volume <= 0m)
	return;

	_longTakeOrder = SellLimit(volume, price);
	}

	private void UpdateShortTake(decimal price, decimal volume)
	{
	CancelOrderIfActive(ref _shortTakeOrder);
	if (volume <= 0m)
	return;

	_shortTakeOrder = BuyLimit(volume, price);
	}

	private void CancelOrderIfActive(ref Order order)
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
