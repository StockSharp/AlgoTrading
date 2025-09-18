using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ported version of the Hoop Master pending breakout strategy.
/// Places symmetric stop orders above and below the market with optional martingale sizing.
/// </summary>
public class HoopMasterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _indentPips;

	private decimal _pipSize;
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal? _lastTradePrice;
	private decimal? _lastClosePrice;

	private Order? _buyStopOrder;
	private Order? _sellStopOrder;
	private Order? _stopLossOrder;
	private Order? _takeProfitOrder;

	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;

	/// <summary>
	/// Initializes a new instance of <see cref="HoopMasterStrategy"/>.
	/// </summary>
	public HoopMasterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetDisplay("Order Volume", "Volume for entry orders", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 5m, 0.1m);

		_stopLossPips = Param(nameof(StopLossPips), 25)
			.SetDisplay("Stop Loss (pips)", "Initial stop loss in pips", "Protection")
			.SetGreaterOrEqual(0)
			.SetCanOptimize(true)
			.SetOptimize(5, 80, 5);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetDisplay("Take Profit (pips)", "Initial take profit in pips", "Protection")
			.SetGreaterOrEqual(0)
			.SetCanOptimize(true)
			.SetOptimize(10, 120, 5);

		_trailingStopPips = Param(nameof(TrailingStopPips), 0)
			.SetDisplay("Trailing Stop (pips)", "Trailing distance in pips", "Protection")
			.SetGreaterOrEqual(0)
			.SetCanOptimize(true)
			.SetOptimize(0, 120, 5);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetDisplay("Trailing Step (pips)", "Minimal step to move trailing stop", "Protection")
			.SetGreaterOrEqual(0)
			.SetCanOptimize(true)
			.SetOptimize(1, 40, 1);

		_indentPips = Param(nameof(IndentPips), 15)
			.SetDisplay("Indent (pips)", "Distance for pending stops", "Trading")
			.SetGreaterOrEqual(0)
			.SetCanOptimize(true)
			.SetOptimize(1, 50, 1);
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Base volume for the breakout orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop loss expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Distance for trailing stop calculations.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimal movement required to advance the trailing stop.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Distance from the market to place stop orders.
	/// </summary>
	public int IndentPips
	{
		get => _indentPips.Value;
		set => _indentPips.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
			(Security, CandleType),
			(Security, DataType.Level1)
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bestBid = null;
		_bestAsk = null;
		_lastTradePrice = null;
		_lastClosePrice = null;

		CancelEntryOrders();
		CancelProtectionOrders();

		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
			throw new InvalidOperationException("Security is not specified.");

		if (Portfolio == null)
			throw new InvalidOperationException("Portfolio is not specified.");

		_pipSize = CalculatePipSize();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
			_bestBid = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
			_bestAsk = (decimal)ask;

		if (level1.Changes.TryGetValue(Level1Fields.LastTradePrice, out var last))
			_lastTradePrice = (decimal)last;

		UpdateTrailing();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_lastClosePrice = candle.ClosePrice;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		UpdateTrailing(candle.ClosePrice);

		if (Position != 0m)
			return;

		ReplaceEntryOrders(OrderVolume);
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			CancelProtectionOrders();
			_stopLossPrice = null;
			_takeProfitPrice = null;
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var order = trade.Order;
		if (order == null || order.Security != Security)
			return;

		if (order == _buyStopOrder)
		{
			_buyStopOrder = null;
			HandleEntryFill(Sides.Buy, trade);
			return;
		}

		if (order == _sellStopOrder)
		{
			_sellStopOrder = null;
			HandleEntryFill(Sides.Sell, trade);
			return;
		}

		if (order == _stopLossOrder)
		{
			_stopLossOrder = null;
			_stopLossPrice = null;
			return;
		}

		if (order == _takeProfitOrder)
		{
			_takeProfitOrder = null;
			_takeProfitPrice = null;
		}
	}

	private void HandleEntryFill(Sides side, MyTrade trade)
	{
		var entryPrice = trade.Trade.Price;
		var volume = Math.Abs(Position);

		if (volume <= 0m)
			volume = OrderVolume;

		CancelProtectionOrders();
		EnsureProtectiveOrders(side, entryPrice, volume);

		var martingaleVolume = Math.Max(OrderVolume * 2m, OrderVolume);
		ReplaceEntryOrders(martingaleVolume);
	}

	private void ReplaceEntryOrders(decimal volume)
	{
		CancelEntryOrders();

		if (volume <= 0m)
			return;

		if (!TryGetMarketPrices(out var ask, out var bid))
			return;

		var indent = Math.Max(0, IndentPips) * _pipSize;

		var buyPriceBase = ask > 0m ? ask : bid;
		var sellPriceBase = bid > 0m ? bid : ask;

		var buyPrice = AlignPrice(buyPriceBase + indent);
		var sellPrice = AlignPrice(sellPriceBase - indent);

		if (buyPrice > 0m)
			_buyStopOrder = BuyStop(volume, buyPrice);

		if (sellPrice > 0m)
			_sellStopOrder = SellStop(volume, sellPrice);
	}

	private void EnsureProtectiveOrders(Sides side, decimal entryPrice, decimal volume)
	{
		CancelProtectionOrders();

		var stopDistance = StopLossPips * _pipSize;
		var takeDistance = TakeProfitPips * _pipSize;

		if (stopDistance > 0m && volume > 0m)
		{
			var stopPrice = side == Sides.Buy
				? AlignPrice(entryPrice - stopDistance)
				: AlignPrice(entryPrice + stopDistance);

			if (stopPrice > 0m)
			{
				_stopLossOrder = side == Sides.Buy
					? SellStop(volume, stopPrice)
					: BuyStop(volume, stopPrice);
				_stopLossPrice = stopPrice;
			}
		}
		else
		{
			_stopLossPrice = null;
		}

		if (takeDistance > 0m && volume > 0m)
		{
			var takePrice = side == Sides.Buy
				? AlignPrice(entryPrice + takeDistance)
				: AlignPrice(entryPrice - takeDistance);

			if (takePrice > 0m)
			{
				_takeProfitOrder = side == Sides.Buy
					? SellLimit(volume, takePrice)
					: BuyLimit(volume, takePrice);
				_takeProfitPrice = takePrice;
			}
		}
		else
		{
			_takeProfitPrice = null;
		}
	}

	private void UpdateTrailing(decimal? fallbackPrice = null)
	{
		if (TrailingStopPips <= 0 || TrailingStepPips <= 0)
			return;

		if (Position == 0m)
			return;

		var trailingDistance = TrailingStopPips * _pipSize;
		var trailingStep = TrailingStepPips * _pipSize;

		if (Position > 0m)
		{
			var price = GetLongMarketPrice(fallbackPrice);
			if (price == null)
				return;

			var desiredStop = AlignPrice(price.Value - trailingDistance);
			if (desiredStop <= 0m)
				return;

			if (_stopLossPrice is decimal current && desiredStop - current < trailingStep)
				return;

			MoveStop(Sides.Sell, desiredStop);
		}
		else if (Position < 0m)
		{
			var price = GetShortMarketPrice(fallbackPrice);
			if (price == null)
				return;

			var desiredStop = AlignPrice(price.Value + trailingDistance);
			if (desiredStop <= 0m)
				return;

			if (_stopLossPrice is decimal current && current - desiredStop < trailingStep)
				return;

			MoveStop(Sides.Buy, desiredStop);
		}
	}

	private decimal? GetLongMarketPrice(decimal? fallback)
	{
		if (_bestBid.HasValue && _bestBid.Value > 0m)
			return _bestBid.Value;

		if (fallback.HasValue && fallback.Value > 0m)
			return fallback.Value;

		if (_lastTradePrice.HasValue && _lastTradePrice.Value > 0m)
			return _lastTradePrice.Value;

		if (_lastClosePrice.HasValue && _lastClosePrice.Value > 0m)
			return _lastClosePrice.Value;

		return null;
	}

	private decimal? GetShortMarketPrice(decimal? fallback)
	{
		if (_bestAsk.HasValue && _bestAsk.Value > 0m)
			return _bestAsk.Value;

		if (fallback.HasValue && fallback.Value > 0m)
			return fallback.Value;

		if (_lastTradePrice.HasValue && _lastTradePrice.Value > 0m)
			return _lastTradePrice.Value;

		if (_lastClosePrice.HasValue && _lastClosePrice.Value > 0m)
			return _lastClosePrice.Value;

		return null;
	}

	private void MoveStop(Sides side, decimal price)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		CancelOrderIfActive(ref _stopLossOrder);

		_stopLossOrder = side == Sides.Sell
			? SellStop(volume, price)
			: BuyStop(volume, price);
		_stopLossPrice = price;
	}

	private bool TryGetMarketPrices(out decimal ask, out decimal bid)
	{
		ask = _bestAsk ?? _lastTradePrice ?? _lastClosePrice ?? 0m;
		bid = _bestBid ?? _lastTradePrice ?? _lastClosePrice ?? 0m;

		if (ask <= 0m && bid <= 0m)
			return false;

		if (ask <= 0m)
			ask = bid;

		if (bid <= 0m)
			bid = ask;

		return ask > 0m && bid > 0m;
	}

	private decimal AlignPrice(decimal price)
	{
		var step = Security?.PriceStep;
		if (step == null || step.Value <= 0m)
			return price;

		var steps = Math.Round(price / step.Value, MidpointRounding.AwayFromZero);
		return steps * step.Value;
	}

	private decimal CalculatePipSize()
	{
		var priceStep = Security?.PriceStep ?? 1m;
		var decimals = Security?.Decimals ?? 0;

		return decimals is 3 or 5 ? priceStep * 10m : priceStep;
	}

	private void CancelEntryOrders()
	{
		CancelOrderIfActive(ref _buyStopOrder);
		CancelOrderIfActive(ref _sellStopOrder);
	}

	private void CancelProtectionOrders()
	{
		CancelOrderIfActive(ref _stopLossOrder);
		CancelOrderIfActive(ref _takeProfitOrder);
	}

	private void CancelOrderIfActive(ref Order? order)
	{
		if (order == null)
			return;

		if (order.State == OrderStates.Active)
			CancelOrder(order);

		order = null;
	}
}
