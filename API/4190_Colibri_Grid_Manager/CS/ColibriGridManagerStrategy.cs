
namespace StockSharp.Samples.Strategies;

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

/// <summary>
/// Grid management strategy converted from the MetaTrader 4 expert advisor Colibri.mq4.
/// It places layered pending orders, attaches protective exits and enforces a daily loss limit.
/// </summary>
public class ColibriGridManagerStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableGrid;
	private readonly StrategyParam<GridOrderTypes> _orderType;
	private readonly StrategyParam<bool> _allowBuy;
	private readonly StrategyParam<bool> _allowSell;
	private readonly StrategyParam<bool> _useCenterLine;
	private readonly StrategyParam<decimal> _centerPrice;
	private readonly StrategyParam<decimal> _levelSpacingPoints;
	private readonly StrategyParam<int> _levelsCount;
	private readonly StrategyParam<decimal> _buyEntryPrice;
	private readonly StrategyParam<decimal> _sellEntryPrice;
	private readonly StrategyParam<decimal> _stopLossPrice;
	private readonly StrategyParam<decimal> _stopDistancePoints;
	private readonly StrategyParam<decimal> _takeProfitDistancePoints;
	private readonly StrategyParam<bool> _useRiskSizing;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _fixedOrderVolume;
	private readonly StrategyParam<int> _expirationHours;
	private readonly StrategyParam<decimal> _dailyLossLimitPercent;
	private readonly StrategyParam<bool> _closeAllPositions;
	private readonly StrategyParam<bool> _closeLongPositions;
	private readonly StrategyParam<bool> _closeShortPositions;
	private readonly StrategyParam<bool> _cancelOrders;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<GridOrderInfo> _gridOrders = new();
	private readonly List<Order> _protectionOrders = new();

	private decimal _pipSize;
	private decimal _bestBid;
	private decimal _bestAsk;
	private DateTime? _currentDay;
	private decimal _dailyPnLBase;
	private decimal _dayStartEquity;
	private bool _tradingSuspended;

	private sealed class GridOrderInfo
	{
		public required Order EntryOrder { get; init; }
		public required Sides Side { get; init; }
		public required decimal Volume { get; init; }
		public decimal? StopLossPrice { get; init; }
		public decimal? TakeProfitPrice { get; init; }
		public DateTimeOffset? Expiration { get; init; }
		public int LevelIndex { get; init; }
	}

	/// <summary>
	/// Supported entry order types.
	/// </summary>
	public enum GridOrderTypes
	{
		Limit,
		Stop,
		Market,
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public ColibriGridManagerStrategy()
	{
		_enableGrid = Param(nameof(EnableGrid), true)
			.SetDisplay("Enable Grid", "Toggle automatic grid management", "General");

		_orderType = Param(nameof(OrderType), GridOrderTypes.Limit)
			.SetDisplay("Order Type", "Entry order type for the grid", "General");

		_allowBuy = Param(nameof(AllowBuy), true)
			.SetDisplay("Allow Buy", "Place buy-side orders when enabled", "Trading");

		_allowSell = Param(nameof(AllowSell), false)
			.SetDisplay("Allow Sell", "Place sell-side orders when enabled", "Trading");

		_useCenterLine = Param(nameof(UseCenterLine), false)
			.SetDisplay("Use Center Line", "Distribute orders symmetrically around CenterPrice", "Structure");

		_centerPrice = Param(nameof(CenterPrice), 0m)
			.SetDisplay("Center Price", "Manual center price (0 uses market reference)", "Structure");

		_levelSpacingPoints = Param(nameof(LevelSpacingPoints), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Spacing (points)", "Distance between grid levels in points", "Structure");

		_levelsCount = Param(nameof(LevelsCount), 3)
			.SetGreaterThanZero()
			.SetDisplay("Levels", "Number of orders per direction", "Structure")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_buyEntryPrice = Param(nameof(BuyEntryPrice), 0m)
			.SetDisplay("Buy Entry Price", "Reference price for long grids (0 uses market)", "Structure");

		_sellEntryPrice = Param(nameof(SellEntryPrice), 0m)
			.SetDisplay("Sell Entry Price", "Reference price for short grids (0 uses market)", "Structure");

		_stopLossPrice = Param(nameof(StopLossPrice), 0m)
			.SetDisplay("Stop Loss Price", "Absolute protective stop price (0 uses distance)", "Risk");

		_stopDistancePoints = Param(nameof(StopDistancePoints), 80m)
			.SetNotNegative()
			.SetDisplay("Stop Distance (points)", "Distance from entry to stop when StopLossPrice is zero", "Risk");

		_takeProfitDistancePoints = Param(nameof(TakeProfitDistancePoints), 0m)
			.SetNotNegative()
			.SetDisplay("Take Profit Distance (points)", "Distance from entry to profit target (0 uses spacing)", "Risk");

		_useRiskSizing = Param(nameof(UseRiskSizing), true)
			.SetDisplay("Use Risk Sizing", "Adjust volume using RiskPercent and stop distance", "Risk");

		_riskPercent = Param(nameof(RiskPercent), 0.02m)
			.SetNotNegative()
			.SetDisplay("Risk Percent", "Fraction of equity risked per grid direction", "Risk");

		_fixedOrderVolume = Param(nameof(FixedOrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Fixed Volume", "Fallback order volume when risk sizing is disabled", "Trading");

		_expirationHours = Param(nameof(ExpirationHours), 48)
			.SetNotNegative()
			.SetDisplay("Expiration (hours)", "Lifetime of pending orders (0 keeps them indefinitely)", "Trading");

		_dailyLossLimitPercent = Param(nameof(DailyLossLimitPercent), 0.06m)
			.SetNotNegative()
			.SetDisplay("Daily Loss Limit", "Stop trading when daily loss exceeds this equity fraction", "Risk");

		_closeAllPositions = Param(nameof(CloseAllPositions), false)
			.SetDisplay("Close All", "Manually close all positions and cancel orders", "Manual");

		_closeLongPositions = Param(nameof(CloseLongPositions), false)
			.SetDisplay("Close Long", "Close only long positions", "Manual");

		_closeShortPositions = Param(nameof(CloseShortPositions), false)
			.SetDisplay("Close Short", "Close only short positions", "Manual");

		_cancelOrders = Param(nameof(CancelOrders), false)
			.SetDisplay("Cancel Orders", "Cancel outstanding grid and protection orders", "Manual");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for maintenance tasks", "General");
	}

	public bool EnableGrid
	{
		get => _enableGrid.Value;
		set => _enableGrid.Value = value;
	}

	public GridOrderTypes OrderType
	{
		get => _orderType.Value;
		set => _orderType.Value = value;
	}

	public bool AllowBuy
	{
		get => _allowBuy.Value;
		set => _allowBuy.Value = value;
	}

	public bool AllowSell
	{
		get => _allowSell.Value;
		set => _allowSell.Value = value;
	}

	public bool UseCenterLine
	{
		get => _useCenterLine.Value;
		set => _useCenterLine.Value = value;
	}

	public decimal CenterPrice
	{
		get => _centerPrice.Value;
		set => _centerPrice.Value = value;
	}

	public decimal LevelSpacingPoints
	{
		get => _levelSpacingPoints.Value;
		set => _levelSpacingPoints.Value = value;
	}

	public int LevelsCount
	{
		get => _levelsCount.Value;
		set => _levelsCount.Value = value;
	}

	public decimal BuyEntryPrice
	{
		get => _buyEntryPrice.Value;
		set => _buyEntryPrice.Value = value;
	}

	public decimal SellEntryPrice
	{
		get => _sellEntryPrice.Value;
		set => _sellEntryPrice.Value = value;
	}

	public decimal StopLossPrice
	{
		get => _stopLossPrice.Value;
		set => _stopLossPrice.Value = value;
	}

	public decimal StopDistancePoints
	{
		get => _stopDistancePoints.Value;
		set => _stopDistancePoints.Value = value;
	}

	public decimal TakeProfitDistancePoints
	{
		get => _takeProfitDistancePoints.Value;
		set => _takeProfitDistancePoints.Value = value;
	}

	public bool UseRiskSizing
	{
		get => _useRiskSizing.Value;
		set => _useRiskSizing.Value = value;
	}

	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	public decimal FixedOrderVolume
	{
		get => _fixedOrderVolume.Value;
		set => _fixedOrderVolume.Value = value;
	}

	public int ExpirationHours
	{
		get => _expirationHours.Value;
		set => _expirationHours.Value = value;
	}

	public decimal DailyLossLimitPercent
	{
		get => _dailyLossLimitPercent.Value;
		set => _dailyLossLimitPercent.Value = value;
	}

	public bool CloseAllPositions
	{
		get => _closeAllPositions.Value;
		set => _closeAllPositions.Value = value;
	}

	public bool CloseLongPositions
	{
		get => _closeLongPositions.Value;
		set => _closeLongPositions.Value = value;
	}

	public bool CloseShortPositions
	{
		get => _closeShortPositions.Value;
		set => _closeShortPositions.Value = value;
	}

	public bool CancelOrders
	{
		get => _cancelOrders.Value;
		set => _cancelOrders.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
		{
			yield return (Security, CandleType);
			yield return (Security, DataType.OrderBook);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_gridOrders.Clear();
		_protectionOrders.Clear();
		_bestBid = 0m;
		_bestAsk = 0m;
		_pipSize = 0m;
		_currentDay = null;
		_dailyPnLBase = 0m;
		_dayStartEquity = 0m;
		_tradingSuspended = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();
		_bestBid = 0m;
		_bestAsk = 0m;
		_currentDay = time.Date;
		_dailyPnLBase = PnL;
		_dayStartEquity = Portfolio?.CurrentValue ?? 0m;
		_tradingSuspended = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		SubscribeOrderBook()
			.Bind(OnOrderBookReceived)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
		ProcessUpdate();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ProcessUpdate();
	}

	private void OnOrderBookReceived(IOrderBookMessage orderBook)
	{
		var bestBid = orderBook.GetBestBid();
		if (bestBid != null)
			_bestBid = bestBid.Price;

		var bestAsk = orderBook.GetBestAsk();
		if (bestAsk != null)
			_bestAsk = bestAsk.Price;

		ProcessUpdate();
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Order == null)
			return;

		var info = _gridOrders.FirstOrDefault(o => o.EntryOrder == trade.Order);
		if (info != null)
		{
			RegisterProtectionOrders(info, trade);
		}

		ProcessUpdate();
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (Position == 0m)
			CancelProtectionOrders();

		ProcessUpdate();
	}

	/// <inheritdoc />
	protected override void OnPnLChanged(decimal diff)
	{
		base.OnPnLChanged(diff);

		ProcessUpdate();
	}

	private void ProcessUpdate()
	{
		CleanupGridOrders();
		CleanupProtectionOrders();
		CancelExpiredOrders();

		UpdateDailyState(CurrentTime);
		HandleManualCommands();

		if (!EnableGrid)
		{
			CancelGridOrders();
			return;
		}

		if (_tradingSuspended)
			return;

		CheckDailyLoss();

		if (_tradingSuspended)
		{
			CancelGridOrders();
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0m)
			return;

		if (HasActiveGridOrders())
			return;

		BuildGrid();
	}

	private void UpdateDailyState(DateTimeOffset time)
	{
		if (time == default)
			return;

		if (_currentDay == null || time.Date != _currentDay.Value)
		{
			_currentDay = time.Date;
			_dailyPnLBase = PnL;
			_dayStartEquity = Portfolio?.CurrentValue ?? 0m;
			_tradingSuspended = false;
		}
	}

	private void HandleManualCommands()
	{
		if (CloseAllPositions)
		{
			CloseAllImmediate();
			_closeAllPositions.Value = false;
		}

		if (CloseLongPositions)
		{
			CloseLongImmediate();
			_closeLongPositions.Value = false;
		}

		if (CloseShortPositions)
		{
			CloseShortImmediate();
			_closeShortPositions.Value = false;
		}

		if (CancelOrders)
		{
			CancelGridOrders();
			CancelProtectionOrders();
			_cancelOrders.Value = false;
		}
	}

	private void CloseAllImmediate()
	{
		CancelGridOrders();
		CancelProtectionOrders();

		if (Position > 0m)
		{
			SellMarket(Position);
		}
		else if (Position < 0m)
		{
			BuyMarket(-Position);
		}
	}

	private void CloseLongImmediate()
	{
		CancelGridOrders();

		if (Position > 0m)
		{
			SellMarket(Position);
		}
	}

	private void CloseShortImmediate()
	{
		CancelGridOrders();

		if (Position < 0m)
		{
			BuyMarket(-Position);
		}
	}

	private void BuildGrid()
	{
		var directions = new List<Sides>();
		if (AllowBuy)
			directions.Add(Sides.Buy);
		if (AllowSell)
			directions.Add(Sides.Sell);

		if (directions.Count == 0)
			return;

		var spacing = ConvertPoints(LevelSpacingPoints);
		if (OrderType != GridOrderTypes.Market && spacing <= 0m)
			return;

		foreach (var side in directions)
		{
			PlaceGridOrders(side, spacing);
		}
	}

	private void PlaceGridOrders(Sides side, decimal spacing)
	{
		var reference = GetInitialReferencePrice(side);
		if (reference is null || reference.Value <= 0m)
			return;

		var levels = OrderType == GridOrderTypes.Market ? 1 : LevelsCount;
		for (var index = 0; index < levels; index++)
		{
			var entry = CalculateEntryPrice(side, index, spacing, reference.Value);
			if (entry is null || entry.Value <= 0m)
				continue;

			var volume = CalculateOrderVolume(entry.Value, side, levels);
			if (volume <= 0m)
				continue;

			var order = CreateEntryOrder(side, entry.Value, volume);
			if (order == null)
				continue;

			order.Comment = $"Colibri {side} L{index + 1}";

			var stopPrice = GetStopPrice(side, entry.Value);
			var takePrice = GetTakeProfitPrice(side, entry.Value, spacing);

			_gridOrders.Add(new GridOrderInfo
			{
				EntryOrder = order,
				Side = side,
				Volume = volume,
				StopLossPrice = stopPrice,
				TakeProfitPrice = takePrice,
				Expiration = GetExpirationTime(),
				LevelIndex = index,
			});
		}
	}

	private Order CreateEntryOrder(Sides side, decimal price, decimal volume)
	{
		return OrderType switch
		{
			GridOrderTypes.Limit => side == Sides.Buy ? BuyLimit(volume, price) : SellLimit(volume, price),
			GridOrderTypes.Stop => side == Sides.Buy ? BuyStop(volume, price) : SellStop(volume, price),
			GridOrderTypes.Market => side == Sides.Buy ? BuyMarket(volume) : SellMarket(volume),
			_ => null,
		};
	}

	private decimal? CalculateEntryPrice(Sides side, int index, decimal spacing, decimal referencePrice)
	{
		if (OrderType == GridOrderTypes.Market && index > 0)
			return null;

		if (UseCenterLine)
		{
			var center = CenterPrice > 0m ? CenterPrice : referencePrice;
			if (OrderType == GridOrderTypes.Market)
				return center;

			if (OrderType == GridOrderTypes.Limit)
			{
				var halfSpan = spacing * (Math.Max(LevelsCount, 1) - 1) / 2m;
				return side == Sides.Buy
					? center - halfSpan + spacing * index
					: center + halfSpan - spacing * index;
			}

			var offset = spacing * (index + 1);
			return side == Sides.Buy
				? center + offset
				: center - offset;
		}

		var basePrice = side == Sides.Buy
			? (BuyEntryPrice > 0m ? BuyEntryPrice : referencePrice)
			: (SellEntryPrice > 0m ? SellEntryPrice : referencePrice);

		return OrderType switch
		{
			GridOrderTypes.Limit => side == Sides.Buy
				? basePrice - spacing * index
				: basePrice + spacing * index,
			GridOrderTypes.Stop => side == Sides.Buy
				? basePrice + spacing * index
				: basePrice - spacing * index,
			GridOrderTypes.Market => basePrice,
			_ => basePrice,
		};
	}

	private decimal CalculateOrderVolume(decimal entryPrice, Sides side, int levels)
	{
		var volume = FixedOrderVolume;

		if (UseRiskSizing)
		{
			var stop = GetStopPrice(side, entryPrice);
			if (stop is decimal stopPrice)
			{
				var distance = Math.Abs(entryPrice - stopPrice);
				var riskVolume = CalculateRiskVolume(distance, levels);
				if (riskVolume > 0m)
					volume = riskVolume;
			}
		}

		return AdjustVolume(volume);
	}

	private decimal CalculateRiskVolume(decimal stopDistance, int levels)
	{
		if (stopDistance <= 0m)
			return 0m;

		var security = Security;
		if (security == null)
			return 0m;

		var priceStep = security.PriceStep ?? 0m;
		var stepPrice = security.StepPrice ?? 0m;
		if (priceStep <= 0m || stepPrice <= 0m)
			return 0m;

		var equity = Portfolio?.CurrentValue ?? 0m;
		if (equity <= 0m)
			return 0m;

		var riskFraction = RiskPercent;
		if (riskFraction <= 0m)
			return 0m;

		var perLevel = equity * riskFraction / Math.Max(levels, 1);
		if (perLevel <= 0m)
			return 0m;

		var steps = stopDistance / priceStep;
		if (steps <= 0m)
			return 0m;

		var moneyPerUnit = steps * stepPrice;
		if (moneyPerUnit <= 0m)
			return 0m;

		return perLevel / moneyPerUnit;
	}

	private decimal AdjustVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var ratio = Math.Floor(volume / step);
			volume = ratio * step;
		}

		var minVolume = security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			return 0m;

		var maxVolume = security.MaxVolume;
		if (maxVolume != null && volume > maxVolume.Value)
			volume = maxVolume.Value;

		return volume;
	}

	private decimal? GetStopPrice(Sides side, decimal entryPrice)
	{
		if (StopLossPrice > 0m)
			return StopLossPrice;

		var distance = ConvertPoints(StopDistancePoints);
		if (distance <= 0m)
			return null;

		return side == Sides.Buy ? entryPrice - distance : entryPrice + distance;
	}

	private decimal? GetTakeProfitPrice(Sides side, decimal entryPrice, decimal spacing)
	{
		var distance = ConvertPoints(TakeProfitDistancePoints);
		if (distance <= 0m)
			distance = spacing;

		if (distance <= 0m)
			return null;

		return side == Sides.Buy ? entryPrice + distance : entryPrice - distance;
	}

	private decimal? GetInitialReferencePrice(Sides side)
	{
		return OrderType switch
		{
			GridOrderTypes.Limit => side == Sides.Buy ? GetBidPrice() : GetAskPrice(),
			GridOrderTypes.Stop => side == Sides.Buy ? GetAskPrice() : GetBidPrice(),
			GridOrderTypes.Market => GetMidPrice(),
			_ => GetMidPrice(),
		};
	}

	private decimal GetBidPrice()
	{
		if (_bestBid > 0m)
			return _bestBid;

		if (Security?.LastPrice is decimal last && last > 0m)
			return last;

		return 0m;
	}

	private decimal GetAskPrice()
	{
		if (_bestAsk > 0m)
			return _bestAsk;

		if (Security?.LastPrice is decimal last && last > 0m)
			return last;

		return 0m;
	}

	private decimal? GetMidPrice()
	{
		if (_bestBid > 0m && _bestAsk > 0m)
			return (_bestBid + _bestAsk) / 2m;

		if (Security?.LastPrice is decimal last && last > 0m)
			return last;

		return null;
	}

	private decimal ConvertPoints(decimal value)
	{
		if (value == 0m)
			return 0m;

		var pip = _pipSize;
		if (pip <= 0m)
			pip = Security?.PriceStep ?? 0m;

		return pip > 0m ? value * pip : 0m;
	}

	private DateTimeOffset? GetExpirationTime()
	{
		if (ExpirationHours <= 0)
			return null;

		return CurrentTime + TimeSpan.FromHours(ExpirationHours);
	}

	private void RegisterProtectionOrders(GridOrderInfo info, MyTrade trade)
	{
		if (trade.Order.State != OrderStates.Done)
			return;

		if (info.StopLossPrice is decimal stopPrice)
		{
			var stopOrder = info.Side == Sides.Buy
				? SellStop(trade.Trade.Volume, stopPrice)
				: BuyStop(trade.Trade.Volume, stopPrice);

			if (stopOrder != null)
				_protectionOrders.Add(stopOrder);
		}

		if (info.TakeProfitPrice is decimal takePrice)
		{
			var takeOrder = info.Side == Sides.Buy
				? SellLimit(trade.Trade.Volume, takePrice)
				: BuyLimit(trade.Trade.Volume, takePrice);

			if (takeOrder != null)
				_protectionOrders.Add(takeOrder);
		}
	}

	private void CancelGridOrders()
	{
		foreach (var info in _gridOrders)
		{
			var order = info.EntryOrder;
			if (order.State == OrderStates.Active)
				CancelOrder(order);
		}
	}

	private void CancelProtectionOrders()
	{
		foreach (var order in _protectionOrders)
		{
			if (order.State == OrderStates.Active)
				CancelOrder(order);
		}
	}

	private void CleanupGridOrders()
	{
		for (var i = _gridOrders.Count - 1; i >= 0; i--)
		{
			var info = _gridOrders[i];
			if (IsFinalState(info.EntryOrder))
				_gridOrders.RemoveAt(i);
		}
	}

	private void CleanupProtectionOrders()
	{
		for (var i = _protectionOrders.Count - 1; i >= 0; i--)
		{
			var order = _protectionOrders[i];
			if (IsFinalState(order))
				_protectionOrders.RemoveAt(i);
		}
	}

	private void CancelExpiredOrders()
	{
		if (ExpirationHours <= 0)
			return;

		var now = CurrentTime;
		foreach (var info in _gridOrders)
		{
			if (info.Expiration is null)
				continue;

			if (info.EntryOrder.State != OrderStates.Active)
				continue;

			if (now >= info.Expiration.Value)
				CancelOrder(info.EntryOrder);
		}
	}

	private bool HasActiveGridOrders()
	{
		return _gridOrders.Any(o => o.EntryOrder.State == OrderStates.Active || o.EntryOrder.State == OrderStates.Pending);
	}

	private void CheckDailyLoss()
	{
		if (DailyLossLimitPercent <= 0m || _dayStartEquity <= 0m)
			return;

		var totalPnL = CalculateTotalPnL();
		if (totalPnL is null)
			return;

		var dailyPnL = totalPnL.Value - _dailyPnLBase;
		var threshold = -DailyLossLimitPercent * _dayStartEquity;

		if (dailyPnL <= threshold)
		{
			_tradingSuspended = true;
			CloseAllImmediate();
			CancelProtectionOrders();
		}
	}

	private decimal? CalculateTotalPnL()
	{
		var realizedPnL = PnL;

		if (Position == 0m)
			return realizedPnL;

		var priceStep = Security?.PriceStep ?? 0m;
		var stepPrice = Security?.StepPrice ?? 0m;
		if (priceStep <= 0m || stepPrice <= 0m)
			return null;

		var referencePrice = Position > 0m ? GetBidPrice() : GetAskPrice();
		if (referencePrice <= 0m)
			return null;

		var averagePrice = Position.AveragePrice;
		if (averagePrice <= 0m)
			return realizedPnL;

		var diff = referencePrice - averagePrice;
		var steps = diff / priceStep;
		var openPnL = steps * stepPrice * Position;

		return realizedPnL + openPnL;
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep > 0m)
			return priceStep;

		return 0.0001m;
	}

	private static bool IsFinalState(Order order)
	{
		return order.State == OrderStates.Done
			|| order.State == OrderStates.Failed
			|| order.State == OrderStates.Cancelled;
	}
}
