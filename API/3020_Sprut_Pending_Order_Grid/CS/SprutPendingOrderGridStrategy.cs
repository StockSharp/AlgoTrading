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
/// Pending order grid inspired by the Sprut MetaTrader expert advisor.
/// </summary>
public class SprutPendingOrderGridStrategy : Strategy
{
	private readonly StrategyParam<int> _countOrders;
	private readonly StrategyParam<decimal> _firstBuyStop;
	private readonly StrategyParam<decimal> _firstBuyLimit;
	private readonly StrategyParam<decimal> _firstSellStop;
	private readonly StrategyParam<decimal> _firstSellLimit;
	private readonly StrategyParam<decimal> _deltaFirstBuyStop;
	private readonly StrategyParam<decimal> _deltaFirstBuyLimit;
	private readonly StrategyParam<decimal> _deltaFirstSellStop;
	private readonly StrategyParam<decimal> _deltaFirstSellLimit;
	private readonly StrategyParam<bool> _useBuyStop;
	private readonly StrategyParam<bool> _useBuyLimit;
	private readonly StrategyParam<bool> _useSellStop;
	private readonly StrategyParam<bool> _useSellLimit;
	private readonly StrategyParam<decimal> _stepStop;
	private readonly StrategyParam<decimal> _stepLimit;
	private readonly StrategyParam<decimal> _volumeStop;
	private readonly StrategyParam<decimal> _volumeLimit;
	private readonly StrategyParam<decimal> _coefficientStop;
	private readonly StrategyParam<decimal> _coefficientLimit;
	private readonly StrategyParam<decimal> _profitClose;
	private readonly StrategyParam<decimal> _lossClose;
	private readonly StrategyParam<int> _expirationMinutes;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<GridOrderInfo> _gridOrders = new();
	private readonly List<Order> _protectionOrders = new();

	private decimal _bestBid;
	private decimal _bestAsk;
	private decimal _pipSize;
	private bool _closeAllRequested;

	private sealed class GridOrderInfo
	{
		public required Order EntryOrder { get; init; }
		public required Sides Side { get; init; }
		public required decimal Volume { get; init; }
		public decimal? StopLossPrice { get; init; }
		public decimal? TakeProfitPrice { get; init; }
		public DateTimeOffset? Expiration { get; init; }
	}

	/// <summary>
	/// Number of pending orders created for each enabled direction.
	/// </summary>
	public int CountOrders
	{
		get => _countOrders.Value;
		set => _countOrders.Value = value;
	}

	/// <summary>
	/// Explicit price for the first buy stop order.
	/// </summary>
	public decimal FirstBuyStop
	{
		get => _firstBuyStop.Value;
		set => _firstBuyStop.Value = value;
	}

	/// <summary>
	/// Explicit price for the first buy limit order.
	/// </summary>
	public decimal FirstBuyLimit
	{
		get => _firstBuyLimit.Value;
		set => _firstBuyLimit.Value = value;
	}

	/// <summary>
	/// Explicit price for the first sell stop order.
	/// </summary>
	public decimal FirstSellStop
	{
		get => _firstSellStop.Value;
		set => _firstSellStop.Value = value;
	}

	/// <summary>
	/// Explicit price for the first sell limit order.
	/// </summary>
	public decimal FirstSellLimit
	{
		get => _firstSellLimit.Value;
		set => _firstSellLimit.Value = value;
	}

	/// <summary>
	/// Offset for the first buy stop when price is not set explicitly (in pips).
	/// </summary>
	public decimal DeltaFirstBuyStop
	{
		get => _deltaFirstBuyStop.Value;
		set => _deltaFirstBuyStop.Value = value;
	}

	/// <summary>
	/// Offset for the first buy limit when price is not set explicitly (in pips).
	/// </summary>
	public decimal DeltaFirstBuyLimit
	{
		get => _deltaFirstBuyLimit.Value;
		set => _deltaFirstBuyLimit.Value = value;
	}

	/// <summary>
	/// Offset for the first sell stop when price is not set explicitly (in pips).
	/// </summary>
	public decimal DeltaFirstSellStop
	{
		get => _deltaFirstSellStop.Value;
		set => _deltaFirstSellStop.Value = value;
	}

	/// <summary>
	/// Offset for the first sell limit when price is not set explicitly (in pips).
	/// </summary>
	public decimal DeltaFirstSellLimit
	{
		get => _deltaFirstSellLimit.Value;
		set => _deltaFirstSellLimit.Value = value;
	}

	/// <summary>
	/// Enable creation of buy stop orders.
	/// </summary>
	public bool UseBuyStop
	{
		get => _useBuyStop.Value;
		set => _useBuyStop.Value = value;
	}

	/// <summary>
	/// Enable creation of buy limit orders.
	/// </summary>
	public bool UseBuyLimit
	{
		get => _useBuyLimit.Value;
		set => _useBuyLimit.Value = value;
	}

	/// <summary>
	/// Enable creation of sell stop orders.
	/// </summary>
	public bool UseSellStop
	{
		get => _useSellStop.Value;
		set => _useSellStop.Value = value;
	}

	/// <summary>
	/// Enable creation of sell limit orders.
	/// </summary>
	public bool UseSellLimit
	{
		get => _useSellLimit.Value;
		set => _useSellLimit.Value = value;
	}

	/// <summary>
	/// Distance between stop orders (in pips).
	/// </summary>
	public decimal StepStop
	{
		get => _stepStop.Value;
		set => _stepStop.Value = value;
	}

	/// <summary>
	/// Distance between limit orders (in pips).
	/// </summary>
	public decimal StepLimit
	{
		get => _stepLimit.Value;
		set => _stepLimit.Value = value;
	}

	/// <summary>
	/// Base volume for stop orders.
	/// </summary>
	public decimal VolumeStop
	{
		get => _volumeStop.Value;
		set => _volumeStop.Value = value;
	}

	/// <summary>
	/// Base volume for limit orders.
	/// </summary>
	public decimal VolumeLimit
	{
		get => _volumeLimit.Value;
		set => _volumeLimit.Value = value;
	}

	/// <summary>
	/// Multiplier that scales stop order volumes for each additional layer.
	/// </summary>
	public decimal CoefficientStop
	{
		get => _coefficientStop.Value;
		set => _coefficientStop.Value = value;
	}

	/// <summary>
	/// Multiplier that scales limit order volumes for each additional layer.
	/// </summary>
	public decimal CoefficientLimit
	{
		get => _coefficientLimit.Value;
		set => _coefficientLimit.Value = value;
	}

	/// <summary>
	/// Profit target that triggers a full reset of positions and orders.
	/// </summary>
	public decimal ProfitClose
	{
		get => _profitClose.Value;
		set => _profitClose.Value = value;
	}

	/// <summary>
	/// Loss threshold that triggers a full reset of positions and orders.
	/// </summary>
	public decimal LossClose
	{
		get => _lossClose.Value;
		set => _lossClose.Value = value;
	}

	/// <summary>
	/// Lifetime of pending orders in minutes (0 disables expiration).
	/// </summary>
	public int ExpirationMinutes
	{
		get => _expirationMinutes.Value;
		set => _expirationMinutes.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Candle type used for periodic checks.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SprutPendingOrderGridStrategy"/> class.
	/// </summary>
	public SprutPendingOrderGridStrategy()
	{
		_countOrders = Param(nameof(CountOrders), 5)
		.SetGreaterThanZero()
		.SetDisplay("Count Orders", "Number of pending orders for each enabled direction", "Orders");

		_firstBuyStop = Param(nameof(FirstBuyStop), 0m)
		.SetDisplay("First Buy Stop", "Manual price for the first buy stop (0 uses automatic offset)", "Orders");

		_firstBuyLimit = Param(nameof(FirstBuyLimit), 0m)
		.SetDisplay("First Buy Limit", "Manual price for the first buy limit (0 uses automatic offset)", "Orders");

		_firstSellStop = Param(nameof(FirstSellStop), 0m)
		.SetDisplay("First Sell Stop", "Manual price for the first sell stop (0 uses automatic offset)", "Orders");

		_firstSellLimit = Param(nameof(FirstSellLimit), 0m)
		.SetDisplay("First Sell Limit", "Manual price for the first sell limit (0 uses automatic offset)", "Orders");

		_deltaFirstBuyStop = Param(nameof(DeltaFirstBuyStop), 15m)
		.SetNotNegative()
		.SetDisplay("Delta Buy Stop", "Offset in pips added to the best ask when placing the first buy stop", "Orders");

		_deltaFirstBuyLimit = Param(nameof(DeltaFirstBuyLimit), 15m)
		.SetNotNegative()
		.SetDisplay("Delta Buy Limit", "Offset in pips subtracted from the best bid when placing the first buy limit", "Orders");

		_deltaFirstSellStop = Param(nameof(DeltaFirstSellStop), 15m)
		.SetNotNegative()
		.SetDisplay("Delta Sell Stop", "Offset in pips subtracted from the best bid when placing the first sell stop", "Orders");

		_deltaFirstSellLimit = Param(nameof(DeltaFirstSellLimit), 15m)
		.SetNotNegative()
		.SetDisplay("Delta Sell Limit", "Offset in pips added to the best ask when placing the first sell limit", "Orders");

		_useBuyStop = Param(nameof(UseBuyStop), false)
		.SetDisplay("Use Buy Stop", "Enable buy stop grid", "Orders");

		_useBuyLimit = Param(nameof(UseBuyLimit), false)
		.SetDisplay("Use Buy Limit", "Enable buy limit grid", "Orders");

		_useSellStop = Param(nameof(UseSellStop), false)
		.SetDisplay("Use Sell Stop", "Enable sell stop grid", "Orders");

		_useSellLimit = Param(nameof(UseSellLimit), false)
		.SetDisplay("Use Sell Limit", "Enable sell limit grid", "Orders");

		_stepStop = Param(nameof(StepStop), 50m)
		.SetNotNegative()
		.SetDisplay("Stop Step", "Distance in pips between sequential stop orders", "Orders");

		_stepLimit = Param(nameof(StepLimit), 50m)
		.SetNotNegative()
		.SetDisplay("Limit Step", "Distance in pips between sequential limit orders", "Orders");

		_volumeStop = Param(nameof(VolumeStop), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Volume", "Base volume for stop orders", "Volume");

		_volumeLimit = Param(nameof(VolumeLimit), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Limit Volume", "Base volume for limit orders", "Volume");

		_coefficientStop = Param(nameof(CoefficientStop), 1.6m)
		.SetGreaterOrEqual(1m)
		.SetDisplay("Stop Coefficient", "Multiplier applied to each additional stop order", "Volume");

		_coefficientLimit = Param(nameof(CoefficientLimit), 1.6m)
		.SetGreaterOrEqual(1m)
		.SetDisplay("Limit Coefficient", "Multiplier applied to each additional limit order", "Volume");

		_profitClose = Param(nameof(ProfitClose), 10m)
		.SetDisplay("Profit Close", "Close all positions when total profit reaches this value", "Risk");

		_lossClose = Param(nameof(LossClose), -100m)
		.SetDisplay("Loss Close", "Close all positions when total profit drops below this value", "Risk");

		_expirationMinutes = Param(nameof(ExpirationMinutes), 60)
		.SetNotNegative()
		.SetDisplay("Expiration", "Lifetime of pending orders in minutes", "Orders");

		_stopLoss = Param(nameof(StopLoss), 50m)
		.SetNotNegative()
		.SetDisplay("Stop Loss", "Distance in pips for protective stop orders (0 disables)", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 0m)
		.SetNotNegative()
		.SetDisplay("Take Profit", "Distance in pips for protective take profit orders (0 disables)", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Candles used for periodic maintenance", "General");
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

		_gridOrders.Clear();
		_protectionOrders.Clear();
		_bestBid = 0m;
		_bestAsk = 0m;
		_pipSize = 0m;
		_closeAllRequested = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

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
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
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

		if (Position == 0)
		CancelProtectionOrders();

		ProcessUpdate();
	}

	private void ProcessUpdate()
	{
		CleanupGridOrders();
		CleanupProtectionOrders();
		CancelExpiredOrders();

		if (_closeAllRequested)
		{
			HandleCloseAllRequest();
			return;
		}

		CheckProfitTargets();

		if (_closeAllRequested)
		{
			HandleCloseAllRequest();
			return;
		}

		TryPlaceGrid();
	}

	private void TryPlaceGrid()
	{
		if (CountOrders <= 0)
		return;

		if (!UseBuyStop && !UseBuyLimit && !UseSellStop && !UseSellLimit)
		return;

		if (Position != 0)
		return;

		if (_gridOrders.Any(o => o.EntryOrder.State == OrderStates.Active))
		return;

		var ask = GetAskPrice();
		var bid = GetBidPrice();

		var stopStep = ConvertPips(StepStop);
		var limitStep = ConvertPips(StepLimit);
		var stopLossOffset = ConvertPips(StopLoss);
		var takeProfitOffset = ConvertPips(TakeProfit);
		var deltaBuyStop = ConvertPips(DeltaFirstBuyStop);
		var deltaBuyLimit = ConvertPips(DeltaFirstBuyLimit);
		var deltaSellStop = ConvertPips(DeltaFirstSellStop);
		var deltaSellLimit = ConvertPips(DeltaFirstSellLimit);

		for (var index = 0; index < CountOrders; index++)
		{
			if (UseBuyStop && ask > 0m)
			{
				var price = FirstBuyStop != 0m ? FirstBuyStop : ask + deltaBuyStop + stopStep * index;
				PlaceGridOrder(price, stopLossOffset, takeProfitOffset, index, CoefficientStop, VolumeStop, Sides.Buy, true);
			}

			if (UseBuyLimit && bid > 0m)
			{
				var price = FirstBuyLimit != 0m ? FirstBuyLimit : bid - deltaBuyLimit - limitStep * index;
				PlaceGridOrder(price, stopLossOffset, takeProfitOffset, index, CoefficientLimit, VolumeLimit, Sides.Buy, false);
			}

			if (UseSellStop && bid > 0m)
			{
				var price = FirstSellStop != 0m ? FirstSellStop : bid - deltaSellStop - stopStep * index;
				PlaceGridOrder(price, stopLossOffset, takeProfitOffset, index, CoefficientStop, VolumeStop, Sides.Sell, true);
			}

			if (UseSellLimit && ask > 0m)
			{
				var price = FirstSellLimit != 0m ? FirstSellLimit : ask + deltaSellLimit + limitStep * index;
				PlaceGridOrder(price, stopLossOffset, takeProfitOffset, index, CoefficientLimit, VolumeLimit, Sides.Sell, false);
			}
		}
	}

	private void PlaceGridOrder(decimal price, decimal stopLossOffset, decimal takeProfitOffset, int index, decimal coefficient, decimal baseVolume, Sides side, bool isStopOrder)
	{
		if (price <= 0m)
		return;

		var volume = CalculateOrderVolume(index, coefficient, baseVolume);
		if (volume <= 0m)
		return;

		Order order = null;
		if (isStopOrder)
		{
			order = side == Sides.Buy
			? BuyStop(volume, price)
			: SellStop(volume, price);
		}
		else
		{
			order = side == Sides.Buy
			? BuyLimit(volume, price)
			: SellLimit(volume, price);
		}

		if (order == null)
		return;

		var stopPrice = StopLoss > 0m
		? side == Sides.Buy ? price - stopLossOffset : price + stopLossOffset
		: (decimal?)null;

		if (stopPrice is <= 0m)
		stopPrice = null;

		var takePrice = TakeProfit > 0m
		? side == Sides.Buy ? price + takeProfitOffset : price - takeProfitOffset
		: (decimal?)null;

		if (takePrice is <= 0m)
		takePrice = null;

		_gridOrders.Add(new GridOrderInfo
		{
			EntryOrder = order,
			Side = side,
			Volume = volume,
			StopLossPrice = stopPrice,
			TakeProfitPrice = takePrice,
			Expiration = GetExpirationTime()
		});
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

	private void CancelExpiredOrders()
	{
		if (ExpirationMinutes <= 0)
		return;

		var now = CurrentTime;

		foreach (var info in _gridOrders)
		{
			if (info.Expiration is null)
			continue;

			if (now >= info.Expiration && info.EntryOrder.State == OrderStates.Active)
			CancelOrder(info.EntryOrder);
		}
	}

	private void CleanupGridOrders()
	{
		for (var i = _gridOrders.Count - 1; i >= 0; i--)
		{
			if (IsFinalState(_gridOrders[i].EntryOrder))
			_gridOrders.RemoveAt(i);
		}
	}

	private void CleanupProtectionOrders()
	{
		for (var i = _protectionOrders.Count - 1; i >= 0; i--)
		{
			var order = _protectionOrders[i];
			if (order == null || IsFinalState(order))
			_protectionOrders.RemoveAt(i);
		}
	}

	private void CancelProtectionOrders()
	{
		foreach (var order in _protectionOrders)
		{
			if (order != null && order.State == OrderStates.Active)
			CancelOrder(order);
		}
	}

	private void HandleCloseAllRequest()
	{
		if (Position > 0)
		SellMarket(Math.Abs(Position));
		else if (Position < 0)
		BuyMarket(Math.Abs(Position));

		foreach (var info in _gridOrders)
		{
			if (info.EntryOrder.State == OrderStates.Active)
			CancelOrder(info.EntryOrder);
		}

		CancelProtectionOrders();
		CleanupGridOrders();
		CleanupProtectionOrders();

		if (Position == 0 && !_gridOrders.Any(o => o.EntryOrder.State == OrderStates.Active) && _protectionOrders.Count == 0)
		_closeAllRequested = false;
	}

	private void CheckProfitTargets()
	{
		var totalPnL = CalculateTotalPnL();
		if (totalPnL is null)
		return;

		if (ProfitClose > 0m && totalPnL.Value >= ProfitClose)
		{
		_closeAllRequested = true;
		return;
		}

		if (LossClose < 0m && totalPnL.Value <= LossClose)
		_closeAllRequested = true;
	}

	private decimal? CalculateTotalPnL()
	{
		var realizedPnL = PnL;

		if (Position == 0)
		return realizedPnL;

		var priceStep = Security.PriceStep ?? 0m;
		var stepPrice = Security.StepPrice ?? 0m;
		if (priceStep == 0m || stepPrice == 0m)
		return null;

		var referencePrice = GetReferencePrice();
		if (referencePrice is null)
		return null;

		var averagePrice = Position.AveragePrice;
		if (averagePrice == 0m)
		return realizedPnL;

		var diff = referencePrice.Value - averagePrice;
		var steps = diff / priceStep;
		var openPnL = steps * stepPrice * Position;

		return realizedPnL + openPnL;
	}

	private decimal? GetReferencePrice()
	{
		if (Position > 0)
		{
			if (_bestBid > 0m)
			return _bestBid;
		}
		else if (Position < 0)
		{
			if (_bestAsk > 0m)
			return _bestAsk;
		}

		if (_bestBid > 0m && _bestAsk > 0m)
		return (_bestBid + _bestAsk) / 2m;

		if (Security.LastPrice is decimal last && last > 0m)
		return last;

		return null;
	}

	private decimal ConvertPips(decimal value)
	{
		if (value == 0m)
		return 0m;

		var pip = _pipSize;
		if (pip <= 0m)
		pip = Security.PriceStep ?? 0.0001m;

		return value * pip;
	}

	private decimal CalculateOrderVolume(int index, decimal coefficient, decimal baseVolume)
	{
		var volume = index == 0 || coefficient == 1m
		? baseVolume
		: baseVolume * index * coefficient;

		return AdjustVolume(volume);
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

	private DateTimeOffset? GetExpirationTime()
	{
		if (ExpirationMinutes <= 0)
		return null;

		return CurrentTime + TimeSpan.FromMinutes(ExpirationMinutes);
	}

	private decimal GetAskPrice()
	{
		if (_bestAsk > 0m)
		return _bestAsk;

		if (Security.LastPrice is decimal last && last > 0m)
		return last;

		return 0m;
	}

	private decimal GetBidPrice()
	{
		if (_bestBid > 0m)
		return _bestBid;

		if (Security.LastPrice is decimal last && last > 0m)
		return last;

		return 0m;
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security.PriceStep ?? 0m;
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

