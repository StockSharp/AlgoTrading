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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that reproduces the Turbo Scaler grid pending order logic from MQL5.
/// Creates buy and sell stop grids, manages break-even and trailing stops and
/// closes all positions based on floating equity targets.
/// </summary>
public class TurboScalerGridStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _breakevenTriggerPoints;
	private readonly StrategyParam<int> _breakevenOffsetPoints;
	private readonly StrategyParam<int> _trailPoints;
	private readonly StrategyParam<decimal> _trailMultiplier;
	private readonly StrategyParam<decimal> _buyStopLossPrice;
	private readonly StrategyParam<decimal> _sellStopLossPrice;
	private readonly StrategyParam<decimal> _buyStopEntry;
	private readonly StrategyParam<decimal> _sellStopEntry;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _pendingQuantity;
	private readonly StrategyParam<int> _pendingStepPoints;
	private readonly StrategyParam<DataType> _triggerCandleType;
	private readonly StrategyParam<bool> _pendingPriceTrigger;
	private readonly StrategyParam<bool> _pendingConditionTrigger;
	private readonly StrategyParam<decimal> _orderBuyBlockStart;
	private readonly StrategyParam<decimal> _orderBuyBlockEnd;
	private readonly StrategyParam<decimal> _orderSellBlockStart;
	private readonly StrategyParam<decimal> _orderSellBlockEnd;
	private readonly StrategyParam<decimal> _maxFloatLoss;
	private readonly StrategyParam<decimal> _equityBreakeven;
	private readonly StrategyParam<decimal> _equityTrigger;
	private readonly StrategyParam<decimal> _equityTrail;

	private decimal _pipSize;
	private decimal _stopLossOffset;
	private decimal _breakevenTriggerOffset;
	private decimal _breakevenOffset;
	private decimal _trailOffset;
	private decimal _equityScale;
	private decimal _tradeVolume;

	private decimal? _bestBid;
	private decimal? _bestAsk;

	private decimal? _triggerPrevClose;
	private decimal? _triggerPrev2Close;
	private decimal? _triggerPrevLow;
	private decimal? _triggerPrev2Low;
	private decimal? _triggerPrevHigh;
	private decimal? _triggerPrev2High;

	private decimal? _m30PrevClose;
	private decimal? _m30Prev2Close;
	private decimal? _m30PrevHigh;
	private decimal? _m30PrevLow;

	private decimal? _h2PrevClose;
	private decimal? _h2Prev2Close;

	private decimal? _dailyLow;
	private decimal? _dailyHigh;
	private decimal? _dailyOpen;

	private decimal? _equityLockLevel;

	private Order _longStopOrder;
	private Order _shortStopOrder;

	private readonly List<Order> _buyStopOrders = new();
	private readonly List<Order> _sellStopOrders = new();

	/// <summary>
	/// Initializes a new instance of <see cref="TurboScalerGridStrategy"/>.
	/// </summary>
	public TurboScalerGridStrategy()
	{
		_stopLossPoints = Param(nameof(StopLossPoints), 100)
			.SetDisplay("Stop Loss Points", "Initial stop loss distance in points", "Risk")
			.SetNotNegative();

		_breakevenTriggerPoints = Param(nameof(BreakevenTriggerPoints), 130)
			.SetDisplay("Break-even Trigger", "Distance in points that activates break-even", "Risk")
			.SetNotNegative();

		_breakevenOffsetPoints = Param(nameof(BreakevenOffsetPoints), 30)
			.SetDisplay("Break-even Offset", "Offset in points added to the entry price", "Risk")
			.SetNotNegative();

		_trailPoints = Param(nameof(TrailPoints), 250)
			.SetDisplay("Trail Points", "Trailing distance in points", "Risk")
			.SetNotNegative();

		_trailMultiplier = Param(nameof(TrailMultiplier), 1.1m)
			.SetDisplay("Trail Multiplier", "Multiplier applied before trailing is updated", "Risk")
			.SetGreaterThan(1m);

		_buyStopLossPrice = Param(nameof(BuyStopLossPrice), 0m)
			.SetDisplay("Buy Stop Loss Price", "Fixed stop loss price for long positions", "Orders")
			.SetGreaterThanOrEqual(0m);

		_sellStopLossPrice = Param(nameof(SellStopLossPrice), 0m)
			.SetDisplay("Sell Stop Loss Price", "Fixed stop loss price for short positions", "Orders")
			.SetGreaterThanOrEqual(0m);

		_buyStopEntry = Param(nameof(BuyStopEntry), 0m)
			.SetDisplay("Buy Stop Entry", "Base price for buy stop grid", "Orders")
			.SetGreaterThanOrEqual(0m);

		_sellStopEntry = Param(nameof(SellStopEntry), 0m)
			.SetDisplay("Sell Stop Entry", "Base price for sell stop grid", "Orders")
			.SetGreaterThanOrEqual(0m);

		_orderVolume = Param(nameof(OrderVolume), 0.01m)
			.SetDisplay("Order Volume", "Volume for each pending order", "Orders")
			.SetGreaterThan(0m);

		_pendingQuantity = Param(nameof(PendingQuantity), 3)
			.SetDisplay("Pending Quantity", "Number of pending orders in the grid", "Orders")
			.SetNotNegative();

		_pendingStepPoints = Param(nameof(PendingStepPoints), 15)
			.SetDisplay("Pending Step", "Distance between pending orders in points", "Orders")
			.SetNotNegative();

		_triggerCandleType = Param(nameof(TriggerCandleType), TimeSpan.FromMinutes(10).TimeFrame())
			.SetDisplay("Trigger Candle", "Candle series used for price based triggers", "Market Data");

		_pendingPriceTrigger = Param(nameof(PendingPriceTrigger), true)
			.SetDisplay("Price Trigger", "Enable price proximity trigger", "Market Data");

		_pendingConditionTrigger = Param(nameof(PendingConditionTrigger), false)
			.SetDisplay("Condition Trigger", "Enable multi timeframe condition trigger", "Market Data");

		_orderBuyBlockStart = Param(nameof(OrderBuyBlockStart), 0m)
			.SetDisplay("Buy Block Start", "Upper boundary for buy condition block", "Blocks");

		_orderBuyBlockEnd = Param(nameof(OrderBuyBlockEnd), 0m)
			.SetDisplay("Buy Block End", "Lower boundary for buy condition block", "Blocks");

		_orderSellBlockStart = Param(nameof(OrderSellBlockStart), 0m)
			.SetDisplay("Sell Block Start", "Lower boundary for sell condition block", "Blocks");

		_orderSellBlockEnd = Param(nameof(OrderSellBlockEnd), 0m)
			.SetDisplay("Sell Block End", "Upper boundary for sell condition block", "Blocks");

		_maxFloatLoss = Param(nameof(MaxFloatLoss), 3m)
			.SetDisplay("Max Floating Loss", "Maximum allowed floating loss in base currency units", "Equity")
			.SetGreaterThanOrEqual(0m);

		_equityBreakeven = Param(nameof(EquityBreakeven), 2.5m)
			.SetDisplay("Equity Break-even", "Equity level maintained after trigger", "Equity")
			.SetGreaterThanOrEqual(0m);

		_equityTrigger = Param(nameof(EquityTrigger), 7m)
			.SetDisplay("Equity Trigger", "Equity profit that activates the lock line", "Equity")
			.SetGreaterThanOrEqual(0m);

		_equityTrail = Param(nameof(EquityTrail), 10m)
			.SetDisplay("Equity Trail", "Distance used to trail the equity lock", "Equity")
			.SetGreaterThanOrEqual(0m);
	}

	/// <summary>
	/// Initial stop loss distance in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Break-even trigger distance in points.
	/// </summary>
	public int BreakevenTriggerPoints
	{
		get => _breakevenTriggerPoints.Value;
		set => _breakevenTriggerPoints.Value = value;
	}

	/// <summary>
	/// Break-even offset in points.
	/// </summary>
	public int BreakevenOffsetPoints
	{
		get => _breakevenOffsetPoints.Value;
		set => _breakevenOffsetPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in points.
	/// </summary>
	public int TrailPoints
	{
		get => _trailPoints.Value;
		set => _trailPoints.Value = value;
	}

	/// <summary>
	/// Multiplier for the trailing stop update.
	/// </summary>
	public decimal TrailMultiplier
	{
		get => _trailMultiplier.Value;
		set => _trailMultiplier.Value = value;
	}

	/// <summary>
	/// Fixed buy stop loss price (optional).
	/// </summary>
	public decimal BuyStopLossPrice
	{
		get => _buyStopLossPrice.Value;
		set => _buyStopLossPrice.Value = value;
	}

	/// <summary>
	/// Fixed sell stop loss price (optional).
	/// </summary>
	public decimal SellStopLossPrice
	{
		get => _sellStopLossPrice.Value;
		set => _sellStopLossPrice.Value = value;
	}

	/// <summary>
	/// Base price for buy stop grid.
	/// </summary>
	public decimal BuyStopEntry
	{
		get => _buyStopEntry.Value;
		set => _buyStopEntry.Value = value;
	}

	/// <summary>
	/// Base price for sell stop grid.
	/// </summary>
	public decimal SellStopEntry
	{
		get => _sellStopEntry.Value;
		set => _sellStopEntry.Value = value;
	}

	/// <summary>
	/// Volume used for each pending order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Number of pending orders allowed in the grid.
	/// </summary>
	public int PendingQuantity
	{
		get => _pendingQuantity.Value;
		set => _pendingQuantity.Value = value;
	}

	/// <summary>
	/// Distance between pending orders in points.
	/// </summary>
	public int PendingStepPoints
	{
		get => _pendingStepPoints.Value;
		set => _pendingStepPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for price based triggering.
	/// </summary>
	public DataType TriggerCandleType
	{
		get => _triggerCandleType.Value;
		set => _triggerCandleType.Value = value;
	}

	/// <summary>
	/// Enables the price proximity trigger.
	/// </summary>
	public bool PendingPriceTrigger
	{
		get => _pendingPriceTrigger.Value;
		set => _pendingPriceTrigger.Value = value;
	}

	/// <summary>
	/// Enables the multi timeframe condition trigger.
	/// </summary>
	public bool PendingConditionTrigger
	{
		get => _pendingConditionTrigger.Value;
		set => _pendingConditionTrigger.Value = value;
	}

	/// <summary>
	/// Upper limit for buy block filtering.
	/// </summary>
	public decimal OrderBuyBlockStart
	{
		get => _orderBuyBlockStart.Value;
		set => _orderBuyBlockStart.Value = value;
	}

	/// <summary>
	/// Lower limit for buy block filtering.
	/// </summary>
	public decimal OrderBuyBlockEnd
	{
		get => _orderBuyBlockEnd.Value;
		set => _orderBuyBlockEnd.Value = value;
	}

	/// <summary>
	/// Lower limit for sell block filtering.
	/// </summary>
	public decimal OrderSellBlockStart
	{
		get => _orderSellBlockStart.Value;
		set => _orderSellBlockStart.Value = value;
	}

	/// <summary>
	/// Upper limit for sell block filtering.
	/// </summary>
	public decimal OrderSellBlockEnd
	{
		get => _orderSellBlockEnd.Value;
		set => _orderSellBlockEnd.Value = value;
	}

	/// <summary>
	/// Maximum tolerated floating loss in currency units.
	/// </summary>
	public decimal MaxFloatLoss
	{
		get => _maxFloatLoss.Value;
		set => _maxFloatLoss.Value = value;
	}

	/// <summary>
	/// Equity level maintained after the trigger fires.
	/// </summary>
	public decimal EquityBreakeven
	{
		get => _equityBreakeven.Value;
		set => _equityBreakeven.Value = value;
	}

	/// <summary>
	/// Equity profit that activates the lock line.
	/// </summary>
	public decimal EquityTrigger
	{
		get => _equityTrigger.Value;
		set => _equityTrigger.Value = value;
	}

	/// <summary>
	/// Distance used to trail the equity lock.
	/// </summary>
	public decimal EquityTrail
	{
		get => _equityTrail.Value;
		set => _equityTrail.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, TriggerCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bestBid = null;
		_bestAsk = null;

		_triggerPrevClose = null;
		_triggerPrev2Close = null;
		_triggerPrevLow = null;
		_triggerPrev2Low = null;
		_triggerPrevHigh = null;
		_triggerPrev2High = null;

		_m30PrevClose = null;
		_m30Prev2Close = null;
		_m30PrevHigh = null;
		_m30PrevLow = null;

		_h2PrevClose = null;
		_h2Prev2Close = null;

		_dailyLow = null;
		_dailyHigh = null;
		_dailyOpen = null;

		_equityLockLevel = null;

		CancelOrder(ref _longStopOrder);
		CancelOrder(ref _shortStopOrder);

		_buyStopOrders.Clear();
		_sellStopOrders.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = Security?.PriceStep ?? 0.0001m;
		if (Security?.Decimals is 3 or 5)
			_pipSize *= 10m;

		_stopLossOffset = StopLossPoints * _pipSize;
		_breakevenTriggerOffset = BreakevenTriggerPoints * _pipSize;
		_breakevenOffset = BreakevenOffsetPoints * _pipSize;
		_trailOffset = TrailPoints * _pipSize;

		_tradeVolume = OrderVolume > 0m ? OrderVolume : Security?.MinVolume ?? 1m;
		_equityScale = _tradeVolume * 100m;

		var triggerSubscription = SubscribeCandles(TriggerCandleType);
		triggerSubscription
			.Bind(ProcessTriggerCandle)
			.Start();

		var m30Subscription = SubscribeCandles(TimeSpan.FromMinutes(30).TimeFrame());
		m30Subscription
			.Bind(ProcessM30Candle)
			.Start();

		var h2Subscription = SubscribeCandles(TimeSpan.FromHours(2).TimeFrame());
		h2Subscription
			.Bind(ProcessH2Candle)
			.Start();

		var dailySubscription = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
		dailySubscription
			.Bind(ProcessDailyCandle)
			.Start();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, triggerSubscription);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			CancelOrder(ref _longStopOrder);
			CancelOrder(ref _shortStopOrder);
			_equityLockLevel = null;
			return;
		}

		if (Position > 0m)
			CancelOrder(ref _shortStopOrder);
		else if (Position < 0m)
			CancelOrder(ref _longStopOrder);
	}

	private void ProcessTriggerCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_triggerPrev2Close = _triggerPrevClose;
		_triggerPrevClose = candle.ClosePrice;
		_triggerPrev2Low = _triggerPrevLow;
		_triggerPrevLow = candle.LowPrice;
		_triggerPrev2High = _triggerPrevHigh;
		_triggerPrevHigh = candle.HighPrice;
	}

	private void ProcessM30Candle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_m30Prev2Close = _m30PrevClose;
		_m30PrevClose = candle.ClosePrice;
		_m30PrevHigh = candle.HighPrice;
		_m30PrevLow = candle.LowPrice;
	}

	private void ProcessH2Candle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_h2Prev2Close = _h2PrevClose;
		_h2PrevClose = candle.ClosePrice;
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_dailyLow = candle.LowPrice;
		_dailyHigh = candle.HighPrice;
		_dailyOpen = candle.OpenPrice;
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue))
			_bestBid = (decimal)bidValue!;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue))
			_bestAsk = (decimal)askValue!;

		var currentTime = level1.ServerTime != default ? level1.ServerTime : CurrentTime;
		if (currentTime == default)
			return;

		CleanupPendingLists();

		var totalPnL = PnLManager?.PnL ?? 0m;
		HandleEquityProtection(totalPnL);

		ManagePositionStops();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		TryPlacePendingOrders();
	}

	private void ManagePositionStops()
	{
		if (Position == 0m)
			return;

		if (Position > 0m)
			UpdateLongProtection();
		else
			UpdateShortProtection();
	}

	private void UpdateLongProtection()
	{
		if (Position <= 0m)
			return;

		var volume = Position.Abs();
		if (volume <= 0m)
			return;

		var entryPrice = Position.AveragePrice;
		var desiredStop = entryPrice - _stopLossOffset;

		if (BuyStopLossPrice > 0m)
			desiredStop = BuyStopLossPrice;

		if (_bestBid is decimal bid && bid > 0m)
		{
			if (_breakevenTriggerOffset > 0m && bid >= entryPrice + _breakevenTriggerOffset)
				desiredStop = Math.Max(desiredStop, entryPrice + _breakevenOffset);

			if (_trailOffset > 0m && _longStopOrder is Order active && active.Price > entryPrice)
			{
				var threshold = active.Price + _trailOffset * TrailMultiplier;
				if (bid > threshold)
					desiredStop = Math.Max(desiredStop, bid - _trailOffset);
			}
		}

		UpdateOrRegisterOrder(ref _longStopOrder, Sides.Sell, OrderTypes.Stop, desiredStop, volume);
	}

	private void UpdateShortProtection()
	{
		if (Position >= 0m)
			return;

		var volume = Position.Abs();
		if (volume <= 0m)
			return;

		var entryPrice = Position.AveragePrice;
		var desiredStop = entryPrice + _stopLossOffset;

		if (SellStopLossPrice > 0m)
			desiredStop = SellStopLossPrice;

		if (_bestAsk is decimal ask && ask > 0m)
		{
			if (_breakevenTriggerOffset > 0m && ask <= entryPrice - _breakevenTriggerOffset)
				desiredStop = Math.Min(desiredStop, entryPrice - _breakevenOffset);

			if (_trailOffset > 0m && _shortStopOrder is Order active && active.Price < entryPrice)
			{
				var threshold = active.Price - _trailOffset * TrailMultiplier;
				if (ask < threshold)
					desiredStop = Math.Min(desiredStop, ask + _trailOffset);
			}
		}

		UpdateOrRegisterOrder(ref _shortStopOrder, Sides.Buy, OrderTypes.Stop, desiredStop, volume);
	}

	private void TryPlacePendingOrders()
	{
		if (PendingQuantity <= 0)
			return;

		var spread = 0m;
		if (_bestBid is decimal bid && _bestAsk is decimal ask && ask > bid)
			spread = ask - bid;

		if (PendingPriceTrigger)
			TryPriceTriggerOrders(spread);

		if (PendingConditionTrigger)
			TryConditionTriggerOrders(spread);
	}

	private void TryPriceTriggerOrders(decimal spread)
	{
		if (_bestBid is not decimal bid || _bestAsk is not decimal ask)
			return;

		var count = ActivePendingCount();
		if (count >= PendingQuantity || Position != 0m)
			return;

		if (BuyStopEntry > 0m && _triggerPrevClose.HasValue && _triggerPrevLow.HasValue && _triggerPrev2Low.HasValue)
		{
			var minLow = BuyStopEntry - spread;
			if (ask < BuyStopEntry - spread * 3m && _triggerPrevClose.Value >= BuyStopEntry &&
			_triggerPrevLow.Value >= minLow && _triggerPrev2Low.Value >= minLow)
			{
				RegisterGridOrder(true, BuyStopEntry + spread, count);
				count = ActivePendingCount();
			}
		}

		if (count >= PendingQuantity)
			return;

		if (SellStopEntry > 0m && _triggerPrevClose.HasValue && _triggerPrevHigh.HasValue && _triggerPrev2High.HasValue)
		{
			var maxHigh = SellStopEntry + spread;
			if (bid > SellStopEntry + spread * 3m && _triggerPrevClose.Value <= SellStopEntry &&
			_triggerPrevHigh.Value <= maxHigh && _triggerPrev2High.Value <= maxHigh)
			{
				RegisterGridOrder(false, SellStopEntry - spread, count);
			}
		}
	}

	private void TryConditionTriggerOrders(decimal spread)
	{
		var count = ActivePendingCount();
		if (count >= PendingQuantity || Position != 0m)
			return;

		var fifty = CalculateFiftyLevel();

		if (BuyStopEntry > 0m && fifty.HasValue && _bestAsk is decimal ask)
		{
			var blockValid = OrderBuyBlockStart < OrderBuyBlockEnd && _dailyLow.HasValue && _dailyOpen.HasValue;
			if (blockValid && _dailyLow.Value < OrderBuyBlockStart && _dailyLow.Value > OrderBuyBlockEnd &&
			_dailyOpen.Value > OrderBuyBlockStart &&
			_h2Prev2Close.HasValue && _h2PrevClose.HasValue && _h2Prev2Close.Value < _h2PrevClose.Value &&
			_m30Prev2Close.HasValue && _m30PrevClose.HasValue && _m30Prev2Close.Value > _m30PrevClose.Value &&
			ask < fifty.Value - spread * 4m)
			{
				RegisterGridOrder(true, fifty.Value + spread, count);
				count = ActivePendingCount();
			}
		}

		if (count >= PendingQuantity)
			return;

		if (SellStopEntry > 0m && fifty.HasValue && _bestBid is decimal bid)
		{
			var blockValid = OrderSellBlockStart > OrderSellBlockEnd && _dailyHigh.HasValue && _dailyOpen.HasValue;
			if (blockValid && _dailyHigh.Value > OrderSellBlockStart && _dailyHigh.Value < OrderSellBlockEnd &&
			_dailyOpen.Value < OrderSellBlockStart &&
			_h2Prev2Close.HasValue && _h2PrevClose.HasValue && _h2Prev2Close.Value > _h2PrevClose.Value &&
			_m30Prev2Close.HasValue && _m30PrevClose.HasValue && _m30Prev2Close.Value < _m30PrevClose.Value &&
			bid > fifty.Value + spread * 3m)
			{
				RegisterGridOrder(false, fifty.Value - spread, count);
			}
		}
	}

	private decimal? CalculateFiftyLevel()
	{
		if (!_m30PrevHigh.HasValue || !_m30PrevLow.HasValue)
			return null;

		var range = _m30PrevHigh.Value - _m30PrevLow.Value;
		return _m30PrevLow.Value + range / 2m;
	}

	private void RegisterGridOrder(bool isBuy, decimal basePrice, int existingCount)
	{
		var step = PendingStepPoints * _pipSize;
		var price = basePrice + (isBuy ? 1 : -1) * step * existingCount;
		if (price <= 0m)
			return;

		var volume = _tradeVolume;
		if (volume <= 0m)
			return;

		var order = isBuy ? BuyStop(volume, price) : SellStop(volume, price);
		if (order == null)
			return;

		if (isBuy)
			_buyStopOrders.Add(order);
		else
			_sellStopOrders.Add(order);
	}

	private int ActivePendingCount()
	{
		var count = 0;

		for (var i = 0; i < _buyStopOrders.Count; i++)
		{
			var order = _buyStopOrders[i];
			if (order.State == OrderStates.Active)
				count++;
		}

		for (var i = 0; i < _sellStopOrders.Count; i++)
		{
			var order = _sellStopOrders[i];
			if (order.State == OrderStates.Active)
				count++;
		}

		return count;
	}

	private void CleanupPendingLists()
	{
		for (var i = _buyStopOrders.Count - 1; i >= 0; i--)
		{
			var order = _buyStopOrders[i];
			if (!order.State.IsActive())
				_buyStopOrders.RemoveAt(i);
		}

		for (var i = _sellStopOrders.Count - 1; i >= 0; i--)
		{
			var order = _sellStopOrders[i];
			if (!order.State.IsActive())
				_sellStopOrders.RemoveAt(i);
		}
	}

	private void HandleEquityProtection(decimal totalPnL)
	{
		var lossThreshold = -ConvertToEquity(MaxFloatLoss);
		if (MaxFloatLoss > 0m && totalPnL <= lossThreshold && totalPnL < 0m)
		{
			CloseAllPositions();
			_equityLockLevel = null;
			return;
		}

		var triggerLevel = ConvertToEquity(EquityTrigger);
		var breakevenLevel = ConvertToEquity(EquityBreakeven);
		var trailStep = ConvertToEquity(EquityTrail);

		if (EquityTrigger > 0m && totalPnL >= triggerLevel && _equityLockLevel == null)
		{
			_equityLockLevel = breakevenLevel;
			LogInfo($"Equity trigger activated at {totalPnL:F2}, lock set to {_equityLockLevel:F2}.");
		}

		if (_equityLockLevel.HasValue && trailStep > 0m && totalPnL > _equityLockLevel.Value + trailStep * 2m)
		{
			_equityLockLevel = totalPnL - trailStep;
			LogInfo($"Equity lock trailed to {_equityLockLevel:F2}.");
		}

		if (_equityLockLevel.HasValue && totalPnL <= _equityLockLevel.Value && totalPnL > 0m &&
		_equityLockLevel.Value > breakevenLevel && totalPnL > breakevenLevel)
		{
			CloseAllPositions();
			_equityLockLevel = null;
		}
	}

	private decimal ConvertToEquity(decimal value)
	{
		return value * _equityScale;
	}

	private void CloseAllPositions()
	{
		if (Position > 0m)
		{
			SellMarket(Position);
		}
		else if (Position < 0m)
		{
			BuyMarket(-Position);
		}

		CancelOrder(ref _longStopOrder);
		CancelOrder(ref _shortStopOrder);

		CancelPendingOrders(_buyStopOrders);
		CancelPendingOrders(_sellStopOrders);
	}

	private void CancelPendingOrders(List<Order> orders)
	{
		for (var i = 0; i < orders.Count; i++)
		{
			var order = orders[i];
			if (order.State == OrderStates.Active)
				CancelOrder(order);
		}

		orders.Clear();
	}

	private void UpdateOrRegisterOrder(ref Order order, Sides side, OrderTypes type, decimal price, decimal volume)
	{
		if (price <= 0m || volume <= 0m)
		{
			CancelOrder(ref order);
			return;
		}

		if (order != null && order.State == OrderStates.Active && order.Price == price && order.Volume == volume && order.Type == type && order.Direction == side)
			return;

		CancelOrder(ref order);

		order = type switch
		{
			OrderTypes.Stop when side == Sides.Sell => SellStop(volume, price),
			OrderTypes.Stop when side == Sides.Buy => BuyStop(volume, price),
			OrderTypes.Limit when side == Sides.Sell => SellLimit(volume, price),
			OrderTypes.Limit when side == Sides.Buy => BuyLimit(volume, price),
			_ => throw new InvalidOperationException("Unsupported order configuration."),
		};
	}

	private void CancelOrder(ref Order order)
	{
		if (order != null && order.State == OrderStates.Active)
			CancelOrder(order);

		order = null;
	}
}

