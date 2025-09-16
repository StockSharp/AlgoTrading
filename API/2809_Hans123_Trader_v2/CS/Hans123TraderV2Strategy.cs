using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hans123 Trader v2 breakout strategy converted from the original MQL expert.
/// Places stop orders at recent range extremes and manages trailing protection.
/// </summary>
public class Hans123TraderV2Strategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _maxPendingOrders;
	private readonly StrategyParam<int> _breakoutPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highest;
	private Lowest _lowest;

	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private Order _stopLossOrder;
	private Order _takeProfitOrder;

	private decimal _pendingBuyPrice;
	private decimal _pendingSellPrice;
	private decimal _entryPrice;

	private decimal _pipSize;
	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;
	private decimal _trailingStopDistance;
	private decimal _trailingStepDistance;

	/// <summary>
	/// Volume used for breakout and protection orders.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips. Set to zero to disable.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips. Set to zero to disable.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips. Set to zero to disable.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step in pips required before the stop is moved again.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Session start hour for placing breakout orders.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Session end hour for placing breakout orders.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneously active breakout orders.
	/// </summary>
	public int MaxPendingOrders
	{
		get => _maxPendingOrders.Value;
		set => _maxPendingOrders.Value = value;
	}

	/// <summary>
	/// Lookback length for calculating highs and lows.
	/// </summary>
	public int BreakoutPeriod
	{
		get => _breakoutPeriod.Value;
		set => _breakoutPeriod.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="Hans123TraderV2Strategy"/>.
	/// </summary>
	public Hans123TraderV2Strategy()
	{
		_volume = Param(nameof(Volume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetNotNegative()
		.SetDisplay("Stop Loss (pips)", "Stop distance", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
		.SetNotNegative()
		.SetDisplay("Take Profit (pips)", "Target distance", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 10m)
		.SetNotNegative()
		.SetDisplay("Trailing Stop (pips)", "Trailing distance", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5m)
		.SetNotNegative()
		.SetDisplay("Trailing Step (pips)", "Extra profit before trailing", "Risk");

		_startHour = Param(nameof(StartHour), 6)
		.SetDisplay("Start Hour", "Session start hour", "Session");

		_endHour = Param(nameof(EndHour), 10)
		.SetDisplay("End Hour", "Session end hour", "Session");

		_maxPendingOrders = Param(nameof(MaxPendingOrders), 5)
		.SetGreaterThanZero()
		.SetDisplay("Max Pending", "Max breakout orders", "Trading");

		_breakoutPeriod = Param(nameof(BreakoutPeriod), 80)
		.SetGreaterThanZero()
		.SetDisplay("Breakout Period", "High/low lookback", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Processed candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_buyStopOrder = null;
		_sellStopOrder = null;
		_stopLossOrder = null;
		_takeProfitOrder = null;
		_pendingBuyPrice = 0m;
		_pendingSellPrice = 0m;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (StartHour < 0 || StartHour > 23)
			throw new InvalidOperationException("Start hour must be between 0 and 23.");

		if (EndHour < 0 || EndHour > 23)
			throw new InvalidOperationException("End hour must be between 0 and 23.");

		if (StartHour >= EndHour)
			throw new InvalidOperationException("Start hour must be less than end hour.");

		if (TrailingStopPips > 0m && TrailingStepPips == 0m)
			throw new InvalidOperationException("Trailing step must be greater than zero when trailing is enabled.");

		// Pre-compute pip-based distances once the security metadata is available.
		_pipSize = CalculatePipSize();
		// Keep cached risk distances aligned with the latest parameter values.
		UpdateDistanceCache();

		_highest = new Highest { Length = BreakoutPeriod, CandlePrice = CandlePrice.High };
		_lowest = new Lowest { Length = BreakoutPeriod, CandlePrice = CandlePrice.Low };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_highest, _lowest, ProcessCandle).Start();
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 1m;

		var ratio = 1m / step;
		var digits = (int)Math.Round(Math.Log10((double)ratio));

		if (digits == 3 || digits == 5)
			step *= 10m;

		return step;
	}

	private void UpdateDistanceCache()
	{
		_stopLossDistance = StopLossPips * _pipSize;
		_takeProfitDistance = TakeProfitPips * _pipSize;
		_trailingStopDistance = TrailingStopPips * _pipSize;
		_trailingStepDistance = TrailingStepPips * _pipSize;
	}

	private void ProcessCandle(ICandleMessage candle, decimal breakoutHigh, decimal breakoutLow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateDistanceCache();

		// Update trailing stop orders using the finished candle.
		UpdateTrailing(candle);

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Enforce the intraday trading window from the original strategy.
		var time = candle.OpenTime.TimeOfDay;
		var start = TimeSpan.FromHours(StartHour);
		var end = TimeSpan.FromHours(EndHour);

		if (time < start || time >= end)
			return;

		var minDistance = _pipSize;
		if (minDistance <= 0m)
			minDistance = Security?.PriceStep ?? 0m;
		if (minDistance <= 0m)
			minDistance = 0.0001m;

		var pendingCount = CountActivePendingOrders();

		if (!IsOrderActive(_buyStopOrder) && pendingCount < MaxPendingOrders)
			{
			// Require a minimum distance from the current ask price before sending a buy stop.
			var ask = Security?.BestAsk?.Price ?? candle.ClosePrice;

			if (breakoutHigh > ask + minDistance)
				{
				_buyStopOrder = BuyStop(Volume, breakoutHigh);
				_pendingBuyPrice = breakoutHigh;
				pendingCount++;
			}
		}

		if (!IsOrderActive(_sellStopOrder) && pendingCount < MaxPendingOrders)
			{
			// Require a minimum distance from the current bid price before sending a sell stop.
			var bid = Security?.BestBid?.Price ?? candle.ClosePrice;

			if (breakoutLow < bid - minDistance)
				{
				_sellStopOrder = SellStop(Volume, breakoutLow);
				_pendingSellPrice = breakoutLow;
			}
		}
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		// Nothing to adjust when trailing is disabled or there is no position.
		if (_trailingStopDistance <= 0m || Position == 0m)
			return;

		var volume = Math.Abs(Position);
		// Skip adjustments if the volume is not available yet.
		if (volume <= 0m)
			return;

		var price = candle.ClosePrice;

		if (Position > 0m)
			{
			if (price - _entryPrice <= _trailingStopDistance + _trailingStepDistance)
				return;

			if (_stopLossOrder == null || _stopLossOrder.State != OrderStates.Active)
				return;

			var newStop = price - _trailingStopDistance;
			if (_stopLossOrder.Price >= newStop - _trailingStepDistance)
				return;

			CancelOrder(_stopLossOrder);
			_stopLossOrder = SellStop(volume, newStop);
		}
		else if (Position < 0m)
			{
			if (_entryPrice - price <= _trailingStopDistance + _trailingStepDistance)
				return;

			if (_stopLossOrder == null || _stopLossOrder.State != OrderStates.Active)
				return;

			var newStop = price + _trailingStopDistance;
			if (_stopLossOrder.Price <= newStop + _trailingStepDistance)
				return;

			CancelOrder(_stopLossOrder);
			_stopLossOrder = BuyStop(volume, newStop);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0m && delta > 0m)
			{
			HandleNewPosition(true);
		}
		else if (Position < 0m && delta < 0m)
			{
			HandleNewPosition(false);
		}
		else if (Position == 0m)
			{
			CancelProtectionOrders();
			_entryPrice = 0m;
		}
	}

	private void HandleNewPosition(bool isLong)
	{
		// Cancel opposite breakout orders once a position is opened.
		CancelPendingOrders();
		UpdateDistanceCache();

		// Track the expected entry price to manage stops and trailing logic.
		_entryPrice = isLong ? _pendingBuyPrice : _pendingSellPrice;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			volume = Volume;

		// Remove the old protective orders before placing new ones.
		CancelOrderIfActive(_stopLossOrder);
		CancelOrderIfActive(_takeProfitOrder);
		_stopLossOrder = null;
		_takeProfitOrder = null;

		if (_stopLossDistance > 0m)
			{
			// Recreate the protective stop at the configured distance.
			var stopPrice = isLong ? _entryPrice - _stopLossDistance : _entryPrice + _stopLossDistance;
			_stopLossOrder = isLong ? SellStop(volume, stopPrice) : BuyStop(volume, stopPrice);
		}

		if (_takeProfitDistance > 0m)
			{
			// Register a take-profit order mirroring the MQL behaviour.
			var takePrice = isLong ? _entryPrice + _takeProfitDistance : _entryPrice - _takeProfitDistance;
			_takeProfitOrder = isLong ? SellLimit(volume, takePrice) : BuyLimit(volume, takePrice);
		}

		_pendingBuyPrice = 0m;
		_pendingSellPrice = 0m;
	}

	private void CancelPendingOrders()
	{
		// Drop any pending breakout orders to avoid duplicates.
		CancelOrderIfActive(_buyStopOrder);
		CancelOrderIfActive(_sellStopOrder);

		_buyStopOrder = null;
		_sellStopOrder = null;
		_pendingBuyPrice = 0m;
		_pendingSellPrice = 0m;
	}

	private void CancelProtectionOrders()
	{
		// Remove the old protective orders before placing new ones.
		CancelOrderIfActive(_stopLossOrder);
		CancelOrderIfActive(_takeProfitOrder);

		_stopLossOrder = null;
		_takeProfitOrder = null;
	}

	private void CancelOrderIfActive(Order order)
	{
		if (order != null && order.State == OrderStates.Active)
			CancelOrder(order);
	}

	private int CountActivePendingOrders()
	{
		var count = 0;

		if (IsOrderActive(_buyStopOrder))
			count++;

		if (IsOrderActive(_sellStopOrder))
			count++;

		return count;
	}

	private static bool IsOrderActive(Order order)
	=> order != null && order.State == OrderStates.Active;

	/// <inheritdoc />
	protected override void OnStopped()
	{
		CancelPendingOrders();
		CancelProtectionOrders();

		base.OnStopped();
	}
}
