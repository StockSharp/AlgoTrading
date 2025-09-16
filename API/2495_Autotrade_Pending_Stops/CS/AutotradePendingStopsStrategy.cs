using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the MQL Autotrade strategy that places symmetric stop orders around the market.
/// Pending stop entries are refreshed on every candle while no position is open.
/// Positions are closed when the market calms down or when absolute profit/loss thresholds are reached.
/// </summary>
public class AutotradePendingStopsStrategy : Strategy
{
	private readonly StrategyParam<int> _indentTicks;
	private readonly StrategyParam<decimal> _minProfit;
	private readonly StrategyParam<int> _expirationMinutes;
	private readonly StrategyParam<decimal> _absoluteFixation;
	private readonly StrategyParam<int> _stabilizationTicks;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;

	private Order? _buyStopOrder;
	private Order? _sellStopOrder;
	private DateTimeOffset? _buyExpiry;
	private DateTimeOffset? _sellExpiry;

	private decimal _prevOpen;
	private decimal _prevClose;
	private bool _hasPrevCandle;

	private decimal _tickSize = 1m;
	private decimal _tickValue = 1m;

	/// <summary>
	/// Distance in price steps from the current market to the pending stop entries.
	/// </summary>
	public int IndentTicks
	{
		get => _indentTicks.Value;
		set => _indentTicks.Value = value;
	}

	/// <summary>
	/// Minimal profit in account currency required to exit when price action stabilizes.
	/// </summary>
	public decimal MinProfit
	{
		get => _minProfit.Value;
		set => _minProfit.Value = value;
	}

	/// <summary>
	/// Lifetime of pending stop orders in minutes.
	/// </summary>
	public int ExpirationMinutes
	{
		get => _expirationMinutes.Value;
		set => _expirationMinutes.Value = value;
	}

	/// <summary>
	/// Absolute profit or loss that forces the position to close.
	/// </summary>
	public decimal AbsoluteFixation
	{
		get => _absoluteFixation.Value;
		set => _absoluteFixation.Value = value;
	}

	/// <summary>
	/// Maximum size of the previous candle body that is treated as consolidation.
	/// </summary>
	public int StabilizationTicks
	{
		get => _stabilizationTicks.Value;
		set => _stabilizationTicks.Value = value;
	}

	/// <summary>
	/// Order volume used for entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set
		{
			_orderVolume.Value = value;
			Volume = value;
		}
	}

	/// <summary>
	/// Candle type used to drive the strategy logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public AutotradePendingStopsStrategy()
	{
		_indentTicks = Param(nameof(IndentTicks), 12)
		.SetGreaterThanZero()
		.SetDisplay("Indent Ticks", "Distance in ticks between price and pending stop orders", "Entries");

		_minProfit = Param(nameof(MinProfit), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Min Profit", "Minimum profit to close during low volatility", "Risk");

		_expirationMinutes = Param(nameof(ExpirationMinutes), 41)
		.SetGreaterThanZero()
		.SetDisplay("Order Expiration", "Lifetime of pending stops in minutes", "Entries");

		_absoluteFixation = Param(nameof(AbsoluteFixation), 43m)
		.SetGreaterThanZero()
		.SetDisplay("Absolute Fixation", "Profit or loss in currency that forces exit", "Risk");

		_stabilizationTicks = Param(nameof(StabilizationTicks), 25)
		.SetGreaterThanZero()
		.SetDisplay("Stabilization Ticks", "Maximum candle body considered as flat market", "Exits");

		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Default volume for both stop orders", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Time frame that drives order refresh", "General");

		Volume = _orderVolume.Value;
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

		// Reset runtime state when the strategy is reloaded.
		_buyStopOrder = null;
		_sellStopOrder = null;
		_buyExpiry = null;
		_sellExpiry = null;
		_prevOpen = 0m;
		_prevClose = 0m;
		_hasPrevCandle = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = _orderVolume.Value;

		// Cache price step and tick value for fast profit calculations.
		_tickSize = Security.PriceStep ?? 1m;
		_tickValue = Security.StepPrice ?? _tickSize;

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Only act on completed candles to stay aligned with the original MQL logic.
		if (candle.State != CandleStates.Finished)
		return;

		if (!_hasPrevCandle)
		{
			// Store the first candle so that stabilization checks have history.
			_prevOpen = candle.OpenPrice;
			_prevClose = candle.ClosePrice;
			_hasPrevCandle = true;

			EnsurePendingOrders(candle);
			return;
		}

		UpdatePendingOrdersLifetime(candle);

		if (Position == 0)
		{
			// Refresh pending orders as soon as the market is flat.
			EnsurePendingOrders(candle);
		}
		else
		{
			// Manage the active position and close it when required.
			ManageOpenPosition(candle);
		}

		// Keep the previous candle body for stabilization checks on the next bar.
		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
	}

	private void EnsurePendingOrders(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		// Clear stale handles for already processed orders.
		if (_buyStopOrder != null && _buyStopOrder.State != OrderStates.Active)
		{
			_buyStopOrder = null;
			_buyExpiry = null;
		}

		if (_sellStopOrder != null && _sellStopOrder.State != OrderStates.Active)
		{
			_sellStopOrder = null;
			_sellExpiry = null;
		}

		var indent = IndentTicks * _tickSize;
		var buyPrice = candle.ClosePrice + indent;
		var sellPrice = candle.ClosePrice - indent;

		if (_buyStopOrder == null)
		{
			// Place the long stop entry above the market.
			_buyStopOrder = BuyStop(buyPrice, OrderVolume);
			_buyExpiry = candle.CloseTime + TimeSpan.FromMinutes(ExpirationMinutes);
		}

		if (_sellStopOrder == null)
		{
			// Place the short stop entry below the market.
			_sellStopOrder = SellStop(sellPrice, OrderVolume);
			_sellExpiry = candle.CloseTime + TimeSpan.FromMinutes(ExpirationMinutes);
		}
	}

	private void UpdatePendingOrdersLifetime(ICandleMessage candle)
	{
		// Cancel and recreate stops when their lifetime expires.
		if (_buyStopOrder != null && _buyStopOrder.State == OrderStates.Active && _buyExpiry is DateTimeOffset buyExpiry && candle.CloseTime >= buyExpiry)
		{
			CancelStrategyOrder(ref _buyStopOrder, ref _buyExpiry);
		}

		if (_sellStopOrder != null && _sellStopOrder.State == OrderStates.Active && _sellExpiry is DateTimeOffset sellExpiry && candle.CloseTime >= sellExpiry)
		{
			CancelStrategyOrder(ref _sellStopOrder, ref _sellExpiry);
		}
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		var stabilizationLimit = StabilizationTicks * _tickSize;
		var prevBodySize = Math.Abs(_prevClose - _prevOpen);
		var currentVolume = Math.Abs(Position);
		var entryPrice = PositionAvgPrice;

		if (currentVolume <= 0 || entryPrice == 0)
		return;

		var step = _tickSize <= 0 ? 1m : _tickSize;
		var stepValue = _tickValue <= 0 ? step : _tickValue;
		var priceDiff = Position > 0 ? candle.ClosePrice - entryPrice : entryPrice - candle.ClosePrice;
		var profit = priceDiff / step * stepValue * currentVolume;

		var exitByStabilization = profit > MinProfit && prevBodySize <= stabilizationLimit;
		var exitByAbsolute = Math.Abs(profit) >= AbsoluteFixation;

		if (Position > 0)
		{
			if (exitByStabilization || exitByAbsolute)
			{
				// Exit long trades with a market sell and drop the opposite pending order.
				SellMarket(currentVolume);
				CancelStrategyOrder(ref _sellStopOrder, ref _sellExpiry);
			}
		}
		else if (Position < 0)
		{
			if (exitByStabilization || exitByAbsolute)
			{
				// Exit short trades with a market buy and drop the opposite pending order.
				BuyMarket(currentVolume);
				CancelStrategyOrder(ref _buyStopOrder, ref _buyExpiry);
			}
		}
	}

	private void CancelStrategyOrder(ref Order? order, ref DateTimeOffset? expiry)
	{
		if (order == null)
		{
			expiry = null;
			return;
		}

		if (order.State == OrderStates.Active)
		CancelOrder(order);

		order = null;
		expiry = null;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null || trade.Order.Security != Security)
		return;

		if (trade.Order == _buyStopOrder)
		{
			// Long stop filled - drop the handle and cancel the unused sell stop.
			_buyStopOrder = null;
			_buyExpiry = null;
			CancelStrategyOrder(ref _sellStopOrder, ref _sellExpiry);
		}
		else if (trade.Order == _sellStopOrder)
		{
			// Short stop filled - drop the handle and cancel the unused buy stop.
			_sellStopOrder = null;
			_sellExpiry = null;
			CancelStrategyOrder(ref _buyStopOrder, ref _buyExpiry);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position != 0)
		return;

		// When the position is fully closed, ensure no orphaned pending orders remain.
		CancelStrategyOrder(ref _buyStopOrder, ref _buyExpiry);
		CancelStrategyOrder(ref _sellStopOrder, ref _sellExpiry);
	}
}
