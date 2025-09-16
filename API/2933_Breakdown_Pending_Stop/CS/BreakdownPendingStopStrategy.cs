using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Daily breakout strategy that places stop orders around the previous day's range.
/// Mirrors the MetaTrader breakdown expert logic with trailing and risk management controls.
/// </summary>
public class BreakdownPendingStopStrategy : Strategy
{
	private readonly StrategyParam<DataType> _workingCandleType;
	private readonly StrategyParam<decimal> _stopLossTicks;
	private readonly StrategyParam<decimal> _takeProfitTicks;
	private readonly StrategyParam<decimal> _trailingStopTicks;
	private readonly StrategyParam<decimal> _trailingStepTicks;
	private readonly StrategyParam<decimal> _minDistanceTicks;
	private readonly StrategyParam<decimal> _orderVolume;

	private decimal _tickSize;
	private decimal _prevDailyHigh;
	private decimal _prevDailyLow;
	private DateTime _nextOrderDate;
	private DateTime _orderPlacementDate;
	private bool _hasDailyLevels;
	private bool _entryOrdersSubmitted;

	private Order? _buyStopOrder;
	private Order? _sellStopOrder;

	private decimal? _longStopPrice;
	private decimal? _longTakeProfit;
	private decimal? _shortStopPrice;
	private decimal? _shortTakeProfit;
	private decimal? _lastLongEntry;
	private decimal? _lastShortEntry;
	private bool _longExitRequested;
	private bool _shortExitRequested;

	/// <summary>
	/// Working timeframe used for trailing and management.
	/// </summary>
	public DataType WorkingCandleType
	{
		get => _workingCandleType.Value;
		set => _workingCandleType.Value = value;
	}

	/// <summary>
	/// Initial stop loss distance expressed in ticks.
	/// </summary>
	public decimal StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in ticks.
	/// </summary>
	public decimal TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in ticks.
	/// </summary>
	public decimal TrailingStopTicks
	{
		get => _trailingStopTicks.Value;
		set => _trailingStopTicks.Value = value;
	}

	/// <summary>
	/// Additional step required before moving the trailing stop.
	/// </summary>
	public decimal TrailingStepTicks
	{
		get => _trailingStepTicks.Value;
		set => _trailingStepTicks.Value = value;
	}

	/// <summary>
	/// Minimum distance added to the previous day high/low when placing pending orders.
	/// </summary>
	public decimal MinDistanceTicks
	{
		get => _minDistanceTicks.Value;
		set => _minDistanceTicks.Value = value;
	}

	/// <summary>
	/// Order volume used for both stop orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BreakdownPendingStopStrategy"/> class.
	/// </summary>
	public BreakdownPendingStopStrategy()
	{
		_workingCandleType = Param(nameof(WorkingCandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Working Candles", "Intraday candles for trailing logic", "General");

		_stopLossTicks = Param(nameof(StopLossTicks), 50m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (ticks)", "Initial protective stop in ticks", "Risk");

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 50m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (ticks)", "Initial take profit in ticks", "Risk");

		_trailingStopTicks = Param(nameof(TrailingStopTicks), 5m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Stop (ticks)", "Trailing stop distance in ticks", "Risk");

		_trailingStepTicks = Param(nameof(TrailingStepTicks), 5m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Step (ticks)", "Extra profit before trailing stop adjustment", "Risk");

		_minDistanceTicks = Param(nameof(MinDistanceTicks), 25m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Min Distance (ticks)", "Offset added to previous day levels", "Entry");

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume for pending stop orders", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new (Security sec, DataType dt)[]
		{
			(Security, WorkingCandleType),
			(Security, TimeSpan.FromDays(1).TimeFrame()),
		};
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_tickSize = 1m;
		_prevDailyHigh = 0m;
		_prevDailyLow = 0m;
		_nextOrderDate = default;
		_orderPlacementDate = default;
		_hasDailyLevels = false;
		_entryOrdersSubmitted = false;

		_buyStopOrder = null;
		_sellStopOrder = null;

		_longStopPrice = null;
		_longTakeProfit = null;
		_shortStopPrice = null;
		_shortTakeProfit = null;
		_lastLongEntry = null;
		_lastShortEntry = null;
		_longExitRequested = false;
		_shortExitRequested = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tickSize = Security.PriceStep ?? 1m;

		// Subscribe to working timeframe candles for trailing and order placement.
		var workingSubscription = SubscribeCandles(WorkingCandleType);
		workingSubscription
			.Bind(ProcessWorkingCandle)
			.Start();

		// Subscribe to daily candles to collect previous day range levels.
		var dailySubscription = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
		dailySubscription
			.Bind(ProcessDailyCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, workingSubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Store completed day high/low to use on the next session.
		_prevDailyHigh = candle.HighPrice;
		_prevDailyLow = candle.LowPrice;
		_nextOrderDate = candle.OpenTime.Date.AddDays(1);
		_hasDailyLevels = true;

		// Remove leftover pending orders once a new daily bar is confirmed.
		CancelEntryOrders();
	}

	private void ProcessWorkingCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var date = candle.OpenTime.Date;

		// Clear pending orders when a new trading day begins.
		if (_entryOrdersSubmitted && _orderPlacementDate != default && date > _orderPlacementDate)
			CancelEntryOrders();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Deploy new pending orders whenever required levels are available.
		if (_hasDailyLevels && (_nextOrderDate == default || date >= _nextOrderDate) && !_entryOrdersSubmitted)
			PlaceEntryOrders(date);

		ManagePosition(candle);
	}

	private void PlaceEntryOrders(DateTime date)
	{
		if (_prevDailyHigh <= 0m && _prevDailyLow <= 0m)
			return;

		var distance = GetOffset(MinDistanceTicks);
		var buyPrice = _prevDailyHigh + distance;
		var sellPrice = _prevDailyLow - distance;

		_buyStopOrder = BuyStop(OrderVolume, buyPrice);
		_sellStopOrder = SellStop(OrderVolume, sellPrice);

		_entryOrdersSubmitted = true;
		_orderPlacementDate = date;
	}

	private void CancelEntryOrders()
	{
		if (_buyStopOrder != null)
		{
			if (_buyStopOrder.State == OrderStates.Active)
				CancelOrder(_buyStopOrder);
			_buyStopOrder = null;
		}

		if (_sellStopOrder != null)
		{
			if (_sellStopOrder.State == OrderStates.Active)
				CancelOrder(_sellStopOrder);
			_sellStopOrder = null;
		}

		_entryOrdersSubmitted = false;
		_orderPlacementDate = default;
	}

	private void ManagePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var entry = _lastLongEntry ?? (PositionPrice != 0m ? PositionPrice : (decimal?)null) ?? candle.ClosePrice;
			_lastLongEntry ??= entry;

			UpdateLongProtection(candle.ClosePrice, entry);

			if (!_longExitRequested && _longStopPrice is decimal stop && candle.ClosePrice <= stop)
			{
				SellMarket(Position);
				_longExitRequested = true;
				return;
			}

			if (!_longExitRequested && _longTakeProfit is decimal take && candle.ClosePrice >= take)
			{
				SellMarket(Position);
				_longExitRequested = true;
				return;
			}
		}
		else if (Position < 0)
		{
			var entry = _lastShortEntry ?? (PositionPrice != 0m ? PositionPrice : (decimal?)null) ?? candle.ClosePrice;
			_lastShortEntry ??= entry;

			UpdateShortProtection(candle.ClosePrice, entry);

			var volume = Math.Abs(Position);

			if (!_shortExitRequested && _shortStopPrice is decimal stop && candle.ClosePrice >= stop)
			{
				BuyMarket(volume);
				_shortExitRequested = true;
				return;
			}

			if (!_shortExitRequested && _shortTakeProfit is decimal take && candle.ClosePrice <= take)
			{
				BuyMarket(volume);
				_shortExitRequested = true;
			}
		}
	}

	private void UpdateLongProtection(decimal price, decimal entry)
	{
		var stopOffset = GetOffset(StopLossTicks);
		if (_longStopPrice is null && StopLossTicks > 0m)
			_longStopPrice = entry - stopOffset;

		var takeOffset = GetOffset(TakeProfitTicks);
		if (_longTakeProfit is null && TakeProfitTicks > 0m)
			_longTakeProfit = entry + takeOffset;

		if (TrailingStopTicks <= 0m)
			return;

		var trail = GetOffset(TrailingStopTicks);
		var step = GetOffset(TrailingStepTicks);

		if (price - entry <= trail + step)
			return;

		var threshold = price - (trail + step);
		if (_longStopPrice is null || _longStopPrice < threshold)
			_longStopPrice = price - trail;
	}

	private void UpdateShortProtection(decimal price, decimal entry)
	{
		var stopOffset = GetOffset(StopLossTicks);
		if (_shortStopPrice is null && StopLossTicks > 0m)
			_shortStopPrice = entry + stopOffset;

		var takeOffset = GetOffset(TakeProfitTicks);
		if (_shortTakeProfit is null && TakeProfitTicks > 0m)
			_shortTakeProfit = entry - takeOffset;

		if (TrailingStopTicks <= 0m)
			return;

		var trail = GetOffset(TrailingStopTicks);
		var step = GetOffset(TrailingStepTicks);

		if (entry - price <= trail + step)
			return;

		var threshold = price + (trail + step);
		if (_shortStopPrice is null || _shortStopPrice > threshold)
			_shortStopPrice = price + trail;
	}

	private decimal GetOffset(decimal ticks) => ticks * _tickSize;

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var order = trade?.Order;
		if (order == null)
			return;

		if (_buyStopOrder != null && order == _buyStopOrder)
		{
			_buyStopOrder = null;
			_lastLongEntry = PositionPrice != 0m ? PositionPrice : trade.Trade?.Price;
			_longStopPrice = null;
			_longTakeProfit = null;
			_longExitRequested = false;
			_entryOrdersSubmitted = false;
			_orderPlacementDate = default;

			if (_sellStopOrder != null)
			{
				if (_sellStopOrder.State == OrderStates.Active)
					CancelOrder(_sellStopOrder);
				_sellStopOrder = null;
			}
		}
		else if (_sellStopOrder != null && order == _sellStopOrder)
		{
			_sellStopOrder = null;
			_lastShortEntry = PositionPrice != 0m ? PositionPrice : trade.Trade?.Price;
			_shortStopPrice = null;
			_shortTakeProfit = null;
			_shortExitRequested = false;
			_entryOrdersSubmitted = false;
			_orderPlacementDate = default;

			if (_buyStopOrder != null)
			{
				if (_buyStopOrder.State == OrderStates.Active)
					CancelOrder(_buyStopOrder);
				_buyStopOrder = null;
			}
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0)
		{
			_shortStopPrice = null;
			_shortTakeProfit = null;
			_lastShortEntry = null;
			_shortExitRequested = false;
		}
		else if (Position < 0)
		{
			_longStopPrice = null;
			_longTakeProfit = null;
			_lastLongEntry = null;
			_longExitRequested = false;
		}
		else
		{
			_longStopPrice = null;
			_longTakeProfit = null;
			_shortStopPrice = null;
			_shortTakeProfit = null;
			_lastLongEntry = null;
			_lastShortEntry = null;
			_longExitRequested = false;
			_shortExitRequested = false;
		}
	}
}
