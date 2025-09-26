using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout straddle that mirrors the "SMC MaxMin" MetaTrader expert.
/// Places stop orders around the previous bar's extremes at a chosen hour
/// and manages protective stop and take-profit levels with trailing updates.
/// </summary>
public class SmcHiloMaxMinStrategy : Strategy
{
	private readonly StrategyParam<int> _setHour;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _minStopDistancePips;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _previousCandle;
	private DateTime? _lastSetupDate;

	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private Order _longStopOrder;
	private Order _longTakeProfitOrder;
	private Order _shortStopOrder;
	private Order _shortTakeProfitOrder;

	private decimal? _bestBid;
	private decimal? _bestAsk;

	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTargetPrice;
	private decimal? _shortTargetPrice;
	private decimal? _pendingLongStop;
	private decimal? _pendingShortStop;
	private decimal? _pendingLongTarget;
	private decimal? _pendingShortTarget;

	private decimal _pipSize;
	private bool _isClosing;


	/// <summary>
	/// Terminal hour when the breakout straddle is placed.
	/// </summary>
	public int SetHour
	{
		get => _setHour.Value;
		set => _setHour.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trailing-stop distance expressed in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum broker stop distance in pips.
	/// </summary>
	public decimal MinStopDistancePips
	{
		get => _minStopDistancePips.Value;
		set => _minStopDistancePips.Value = value;
	}

	/// <summary>
	/// Candle type used to evaluate the hourly session.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters with sensible defaults.
	/// </summary>
	public SmcHiloMaxMinStrategy()
	{

		_setHour = Param(nameof(SetHour), 15)
		.SetDisplay("Trigger Hour", "Terminal hour when pending orders are created", "Timing");

		_takeProfitPips = Param(nameof(TakeProfitPips), 500m)
		.SetGreaterThanOrEqualTo(0m)
		.SetDisplay("Take Profit (pips)", "Distance from entry to the profit target", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 30m)
		.SetGreaterThanOrEqualTo(0m)
		.SetDisplay("Stop Loss (pips)", "Distance from entry to the protective stop", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 30m)
		.SetGreaterThanOrEqualTo(0m)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance that replaces the static stop", "Risk");

		_minStopDistancePips = Param(nameof(MinStopDistancePips), 0m)
		.SetGreaterThanOrEqualTo(0m)
		.SetDisplay("Min Stop Distance (pips)", "Broker minimum stop distance, used to pad breakout levels", "Timing");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Candles that define the hourly breakout window", "Timing");
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

		_previousCandle = null;
		_lastSetupDate = null;

		_buyStopOrder = null;
		_sellStopOrder = null;
		_longStopOrder = null;
		_longTakeProfitOrder = null;
		_shortStopOrder = null;
		_shortTakeProfitOrder = null;

		_bestBid = null;
		_bestAsk = null;

		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTargetPrice = null;
		_shortTargetPrice = null;
		_pendingLongStop = null;
		_pendingShortStop = null;
		_pendingLongTarget = null;
		_pendingShortTarget = null;

		_pipSize = 0m;
		_isClosing = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		UpdatePipSize();

		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription
		.Bind(ProcessCandle)
		.Start();

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdatePipSize();

		var hour = candle.OpenTime.Hour;
		var currentDate = candle.OpenTime.Date;

		if (_previousCandle != null)
		{
			if (_lastSetupDate != currentDate && hour == NormalizeHour(SetHour))
			{
				PlaceStraddle(candle.OpenTime);
			}

			var cancelHour = NormalizeHour(SetHour + 2);
			if (_lastSetupDate == currentDate && hour == cancelHour)
			{
				CancelEntryOrders();
			}
		}

		_previousCandle = candle;
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		var bid = message.TryGetDecimal(Level1Fields.BestBidPrice);
		var ask = message.TryGetDecimal(Level1Fields.BestAskPrice);

		if (bid.HasValue && bid.Value > 0m)
		_bestBid = bid.Value;

		if (ask.HasValue && ask.Value > 0m)
		_bestAsk = ask.Value;

		CleanupInactiveOrders();
		ManageActivePosition();
	}

	private void PlaceStraddle(DateTimeOffset triggerTime)
	{
		if (_previousCandle == null)
		return;

		if (Volume <= 0m)
		return;

		if (Position != 0m)
		return;

		if (IsOrderActive(_buyStopOrder) || IsOrderActive(_sellStopOrder))
		return;

		var previousHigh = _previousCandle.High;
		var previousLow = _previousCandle.Low;

		if (previousHigh <= 0m || previousLow <= 0m)
		return;

		var priceStep = GetPriceStep();
		var minDistance = MinStopDistancePips * _pipSize;
		var ask = _bestAsk ?? _previousCandle.ClosePrice;
		var bid = _bestBid ?? _previousCandle.ClosePrice;

		var longTrigger = previousHigh;
		if (minDistance > 0m && ask > 0m)
		{
			var distance = previousHigh - ask;
			if (distance < minDistance)
			longTrigger += minDistance - distance;
		}

		var shortTrigger = previousLow;
		if (minDistance > 0m && bid > 0m)
		{
			var distance = bid - previousLow;
			if (distance < minDistance)
			shortTrigger -= minDistance - distance;
		}

		longTrigger = NormalizePrice(longTrigger + priceStep);
		shortTrigger = NormalizePrice(shortTrigger - priceStep);

		if (longTrigger > 0m)
		{
			CancelOrderIfActive(ref _buyStopOrder);
			_buyStopOrder = BuyStop(Volume, longTrigger);

			_pendingLongStop = CalculateLongStopPrice();
			_pendingLongTarget = CalculateLongTargetPrice(longTrigger);
		}

		if (shortTrigger > 0m)
		{
			CancelOrderIfActive(ref _sellStopOrder);
			_sellStopOrder = SellStop(Volume, shortTrigger);

			_pendingShortStop = CalculateShortStopPrice();
			_pendingShortTarget = CalculateShortTargetPrice(shortTrigger);
		}

		if (_buyStopOrder != null || _sellStopOrder != null)
		_lastSetupDate = triggerTime.Date;
	}

	private decimal? CalculateLongStopPrice()
	{
		if (_previousCandle == null)
		return null;

		var distance = StopLossPips * _pipSize;
		if (distance <= 0m)
		return null;

		var stop = _previousCandle.Low - distance;
		return stop > 0m ? NormalizePrice(stop) : (decimal?)null;
	}

	private decimal? CalculateShortStopPrice()
	{
		if (_previousCandle == null)
		return null;

		var distance = StopLossPips * _pipSize;
		if (distance <= 0m)
		return null;

		var stop = _previousCandle.High + distance;
		return stop > 0m ? NormalizePrice(stop) : (decimal?)null;
	}

	private decimal? CalculateLongTargetPrice(decimal entryPrice)
	{
		var distance = TakeProfitPips * _pipSize;
		if (distance <= 0m)
		return null;

		var target = entryPrice + distance;
		return target > 0m ? NormalizePrice(target) : (decimal?)null;
	}

	private decimal? CalculateShortTargetPrice(decimal entryPrice)
	{
		var distance = TakeProfitPips * _pipSize;
		if (distance <= 0m)
		return null;

		var target = entryPrice - distance;
		return target > 0m ? NormalizePrice(target) : (decimal?)null;
	}

	private void ManageActivePosition()
	{
		if (Position > 0m)
		{
			EnsureLongProtection();
			UpdateLongTrailing();
		}
		else if (Position < 0m)
		{
			EnsureShortProtection();
			UpdateShortTrailing();
		}
	}

	private void EnsureLongProtection()
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
		return;

		if (_longStopPrice is decimal stop && stop > 0m)
		{
			var normalized = NormalizePrice(stop);
			if (_longStopOrder == null || !ArePricesEqual(_longStopOrder.Price, normalized))
			{
				CancelOrderIfActive(ref _longStopOrder);
				_longStopOrder = SellStop(volume, normalized);
			}
		}
		else
		{
			CancelOrderIfActive(ref _longStopOrder);
		}

		if (_longTargetPrice is decimal target && target > 0m)
		{
			var normalized = NormalizePrice(target);
			if (_longTakeProfitOrder == null || !ArePricesEqual(_longTakeProfitOrder.Price, normalized))
			{
				CancelOrderIfActive(ref _longTakeProfitOrder);
				_longTakeProfitOrder = SellLimit(volume, normalized);
			}
		}
		else
		{
			CancelOrderIfActive(ref _longTakeProfitOrder);
		}
	}

	private void EnsureShortProtection()
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
		return;

		if (_shortStopPrice is decimal stop && stop > 0m)
		{
			var normalized = NormalizePrice(stop);
			if (_shortStopOrder == null || !ArePricesEqual(_shortStopOrder.Price, normalized))
			{
				CancelOrderIfActive(ref _shortStopOrder);
				_shortStopOrder = BuyStop(volume, normalized);
			}
		}
		else
		{
			CancelOrderIfActive(ref _shortStopOrder);
		}

		if (_shortTargetPrice is decimal target && target > 0m)
		{
			var normalized = NormalizePrice(target);
			if (_shortTakeProfitOrder == null || !ArePricesEqual(_shortTakeProfitOrder.Price, normalized))
			{
				CancelOrderIfActive(ref _shortTakeProfitOrder);
				_shortTakeProfitOrder = BuyLimit(volume, normalized);
			}
		}
		else
		{
			CancelOrderIfActive(ref _shortTakeProfitOrder);
		}
	}

	private void UpdateLongTrailing()
	{
		if (TrailingStopPips <= 0m)
		return;

		if (_longEntryPrice is not decimal entry)
		return;

		var bid = _bestBid ?? 0m;
		if (bid <= 0m)
		return;

		var distance = TrailingStopPips * _pipSize;
		if (distance <= 0m)
		return;

		var profit = bid - entry;
		if (profit <= distance)
		return;

		var newStop = NormalizePrice(bid - distance);
		if (_longStopPrice is decimal existing && !IsGreaterThan(newStop, existing))
		return;

		_longStopPrice = newStop;
		EnsureLongProtection();
	}

	private void UpdateShortTrailing()
	{
		if (TrailingStopPips <= 0m)
		return;

		if (_shortEntryPrice is not decimal entry)
		return;

		var ask = _bestAsk ?? 0m;
		if (ask <= 0m)
		return;

		var distance = TrailingStopPips * _pipSize;
		if (distance <= 0m)
		return;

		var profit = entry - ask;
		if (profit <= distance)
		return;

		var newStop = NormalizePrice(ask + distance);
		if (_shortStopPrice is decimal existing && !IsLessThan(newStop, existing))
		return;

		_shortStopPrice = newStop;
		EnsureShortProtection();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order.Security != Security)
		return;

		var tradeVolume = trade.Trade.Volume;
		if (tradeVolume <= 0m)
		return;

		var signedDelta = trade.Order.Side == Sides.Buy ? tradeVolume : -tradeVolume;
		var currentPosition = Position;
		var previousPosition = currentPosition - signedDelta;

		if (currentPosition > 0m && trade.Order.Side == Sides.Buy)
		{
			UpdateLongEntry(previousPosition, trade.Trade.Price, tradeVolume);
		}
		else if (currentPosition < 0m && trade.Order.Side == Sides.Sell)
		{
			UpdateShortEntry(previousPosition, trade.Trade.Price, tradeVolume);
		}
		else
		{
			if (previousPosition > 0m && currentPosition <= 0m)
			ResetLongState();

			if (previousPosition < 0m && currentPosition >= 0m)
			ResetShortState();
		}

		if (trade.Order == _buyStopOrder)
		{
			_buyStopOrder = null;
			CancelOrderIfActive(ref _sellStopOrder);
		}
		else if (trade.Order == _sellStopOrder)
		{
			_sellStopOrder = null;
			CancelOrderIfActive(ref _buyStopOrder);
		}

		if (trade.Order == _longStopOrder || trade.Order == _longTakeProfitOrder)
		{
			ResetLongState();
		}

		if (trade.Order == _shortStopOrder || trade.Order == _shortTakeProfitOrder)
		{
			ResetShortState();
		}

		_isClosing = false;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			ResetLongState();
			ResetShortState();
			_isClosing = false;
		}
	}

	private void UpdateLongEntry(decimal previousPosition, decimal price, decimal tradeVolume)
	{
		var positionBefore = Math.Abs(previousPosition);
		var currentPosition = Math.Abs(Position);

		if (positionBefore <= 0m)
		{
			_longEntryPrice = price;
		}
		else if (_longEntryPrice is decimal existing)
		{
			_longEntryPrice = (existing * positionBefore + price * tradeVolume) / currentPosition;
		}
		else
		{
			_longEntryPrice = price;
		}

		_longStopPrice = _pendingLongStop;
		_longTargetPrice = _pendingLongTarget;

		EnsureLongProtection();
	}

	private void UpdateShortEntry(decimal previousPosition, decimal price, decimal tradeVolume)
	{
		var positionBefore = Math.Abs(previousPosition);
		var currentPosition = Math.Abs(Position);

		if (positionBefore <= 0m)
		{
			_shortEntryPrice = price;
		}
		else if (_shortEntryPrice is decimal existing)
		{
			_shortEntryPrice = (existing * positionBefore + price * tradeVolume) / currentPosition;
		}
		else
		{
			_shortEntryPrice = price;
		}

		_shortStopPrice = _pendingShortStop;
		_shortTargetPrice = _pendingShortTarget;

		EnsureShortProtection();
	}

	private void ResetLongState()
	{
		CancelOrderIfActive(ref _longStopOrder);
		CancelOrderIfActive(ref _longTakeProfitOrder);

		_longEntryPrice = null;
		_longStopPrice = null;
		_longTargetPrice = null;
		_pendingLongStop = null;
		_pendingLongTarget = null;
	}

	private void ResetShortState()
	{
		CancelOrderIfActive(ref _shortStopOrder);
		CancelOrderIfActive(ref _shortTakeProfitOrder);

		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTargetPrice = null;
		_pendingShortStop = null;
		_pendingShortTarget = null;
	}

	private void CancelEntryOrders()
	{
		CancelOrderIfActive(ref _buyStopOrder);
		CancelOrderIfActive(ref _sellStopOrder);
	}

	private void CleanupInactiveOrders()
	{
		CleanupOrder(ref _buyStopOrder);
		CleanupOrder(ref _sellStopOrder);
		CleanupOrder(ref _longStopOrder);
		CleanupOrder(ref _longTakeProfitOrder);
		CleanupOrder(ref _shortStopOrder);
		CleanupOrder(ref _shortTakeProfitOrder);
	}

	private void CleanupOrder(ref Order order)
	{
		if (order == null)
		return;

		if (!IsOrderActive(order))
		order = null;
	}

	private void CancelOrderIfActive(ref Order order)
	{
		if (order == null)
		return;

		if (IsOrderActive(order))
		CancelOrder(order);

		order = null;
	}

	private static bool IsOrderActive(Order order)
	{
		return order != null && (order.State == OrderStates.Active || order.State == OrderStates.Pending);
	}

	private int NormalizeHour(int hour)
	{
		if (hour < 0)
		hour = 0;

		return ((hour % 24) + 24) % 24;
	}

	private void UpdatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return;

		var digits = GetDecimalDigits(step);
		_pipSize = (digits == 3 || digits == 5) ? step * 10m : step;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step > 0m)
		{
			return step;
		}

		if (_pipSize > 0m)
		{
			return _pipSize;
		}

		return 0.0001m;
	}

	private decimal NormalizePrice(decimal price)
	{
		if (price <= 0m)
		{
			return price;
		}

		var step = Security?.PriceStep;
		if (step == null || step.Value <= 0m)
		{
			return price;
		}

		var steps = Math.Round(price / step.Value, MidpointRounding.AwayFromZero);
		return steps * step.Value;
	}

	private static int GetDecimalDigits(decimal value)
	{
		value = Math.Abs(value);
		var digits = 0;

		while (value != Math.Truncate(value) && digits < 10)
		{
			value *= 10m;
			digits++;
		}

		return digits;
	}

	private bool ArePricesEqual(decimal first, decimal second)
	{
		var step = GetPriceStep();
		if (step <= 0m)
		{
			step = 0.0000001m;
		}

		return Math.Abs(first - second) <= step / 2m;
	}

	private bool IsGreaterThan(decimal candidate, decimal reference)
	{
		var step = GetPriceStep();
		return candidate > reference + step / 2m;
	}

	private bool IsLessThan(decimal candidate, decimal reference)
	{
		var step = GetPriceStep();
		return candidate < reference - step / 2m;
	}
}
