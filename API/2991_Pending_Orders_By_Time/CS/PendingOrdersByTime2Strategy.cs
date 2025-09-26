using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Places pending buy and sell stop orders at configured hours and manages trailing exits.
/// </summary>
public class PendingOrdersByTime2Strategy : Strategy
{
	private readonly StrategyParam<int> _openingHour;
	private readonly StrategyParam<int> _closingHour;
	private readonly StrategyParam<decimal> _distanceTicks;
	private readonly StrategyParam<decimal> _stopLossTicks;
	private readonly StrategyParam<decimal> _takeProfitTicks;
	private readonly StrategyParam<decimal> _trailingStopTicks;
	private readonly StrategyParam<decimal> _trailingStepTicks;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _currentDate;
	private bool _ordersPlaced;

	private Order _buyStopOrder;
	private Order _sellStopOrder;
	private Order _protectiveStopOrder;
	private Order _takeProfitOrder;

	private decimal _buyStopLossPrice;
	private decimal _buyTakeProfitPrice;
	private decimal _sellStopLossPrice;
	private decimal _sellTakeProfitPrice;

	private decimal _entryPrice;
	private decimal? _currentStopLevel;

	private decimal _bestBid;
	private decimal _bestAsk;

	/// <summary>
	/// Hour when pending orders are placed.
	/// </summary>
	public int OpeningHour
	{
		get => _openingHour.Value;
		set => _openingHour.Value = value;
	}

	/// <summary>
	/// Hour when pending orders are cancelled and positions closed.
	/// </summary>
	public int ClosingHour
	{
		get => _closingHour.Value;
		set => _closingHour.Value = value;
	}

	/// <summary>
	/// Distance in price steps from bid/ask used to place stop orders.
	/// </summary>
	public decimal DistanceTicks
	{
		get => _distanceTicks.Value;
		set => _distanceTicks.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	/// <summary>
	/// Minimum distance between price and stop-loss used for trailing.
	/// </summary>
	public decimal TrailingStopTicks
	{
		get => _trailingStopTicks.Value;
		set => _trailingStopTicks.Value = value;
	}

	/// <summary>
	/// Trailing step in price steps.
	/// </summary>
	public decimal TrailingStepTicks
	{
		get => _trailingStepTicks.Value;
		set => _trailingStepTicks.Value = value;
	}


	/// <summary>
	/// Candle type used to control the trading session.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="PendingOrdersByTime2Strategy"/>.
	/// </summary>
	public PendingOrdersByTime2Strategy()
	{
		_openingHour = Param(nameof(OpeningHour), 11)
			.SetDisplay("Opening Hour", "Hour when pending orders are placed", "Schedule");

		_closingHour = Param(nameof(ClosingHour), 23)
			.SetDisplay("Closing Hour", "Hour when pending orders are cancelled", "Schedule");

		_distanceTicks = Param(nameof(DistanceTicks), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Distance (ticks)", "Offset from bid/ask to place pending orders", "Orders");

		_stopLossTicks = Param(nameof(StopLossTicks), 35m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (ticks)", "Stop loss distance in price steps", "Risk");

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 85m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (ticks)", "Take profit distance in price steps", "Risk");

		_trailingStopTicks = Param(nameof(TrailingStopTicks), 5m)
			.SetDisplay("Trailing Stop (ticks)", "Minimum distance between price and stop-loss", "Risk");

		_trailingStepTicks = Param(nameof(TrailingStepTicks), 5m)
			.SetDisplay("Trailing Step (ticks)", "Increment for trailing stop updates", "Risk");


		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for session control", "General");
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

		_currentDate = default;
		_ordersPlaced = false;
		_buyStopOrder = null;
		_sellStopOrder = null;
		_protectiveStopOrder = null;
		_takeProfitOrder = null;
		_entryPrice = 0m;
		_currentStopLevel = null;
		_bestBid = 0m;
		_bestAsk = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		SubscribeOrderBook()
			.Bind(depth =>
			{
				_bestBid = depth.GetBestBid()?.Price ?? _bestBid;
				_bestAsk = depth.GetBestAsk()?.Price ?? _bestAsk;
			})
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		UpdateSession(candle);

		ManageTrailing(candle);

		var hour = candle.OpenTime.Hour;

		if (hour == ClosingHour)
		{
			CancelPendingOrders();
			ClosePosition();
			return;
		}

		if (hour == OpeningHour && !_ordersPlaced)
		{
			PlacePendingOrders(candle);
			_ordersPlaced = true;
		}
	}

	private void UpdateSession(ICandleMessage candle)
	{
		var date = candle.OpenTime.Date;

		if (date == _currentDate)
			return;

		_currentDate = date;
		_ordersPlaced = false;
	}

	private void PlacePendingOrders(ICandleMessage candle)
	{
		CancelPendingOrders();

		var step = Security?.PriceStep ?? 1m;
		var distance = DistanceTicks * step;
		var stopLossOffset = StopLossTicks * step;
		var takeProfitOffset = TakeProfitTicks * step;

		var ask = _bestAsk > 0m ? _bestAsk : candle.ClosePrice;
		var bid = _bestBid > 0m ? _bestBid : candle.ClosePrice;

		var buyPrice = ask + distance;
		var sellPrice = bid - distance;

		_buyStopLossPrice = buyPrice - stopLossOffset;
		_buyTakeProfitPrice = buyPrice + takeProfitOffset;

		_sellStopLossPrice = sellPrice + stopLossOffset;
		_sellTakeProfitPrice = sellPrice - takeProfitOffset;

		var volume = Volume;

		if (volume <= 0m)
			return;

		_buyStopOrder = BuyStop(volume, buyPrice);
		_sellStopOrder = SellStop(volume, sellPrice);
	}

	private void ManageTrailing(ICandleMessage candle)
	{
		if (TrailingStopTicks <= 0m || Position == 0m || _entryPrice == 0m)
			return;

		var step = Security?.PriceStep ?? 1m;
		var stopDistance = TrailingStopTicks * step;
		var stepDistance = TrailingStepTicks * step;

		if (Position > 0m)
		{
			var profit = candle.ClosePrice - _entryPrice;

			if (profit < stopDistance + stepDistance)
				return;

			var newStop = candle.ClosePrice - stopDistance;

			if (_currentStopLevel.HasValue && newStop <= _currentStopLevel.Value + step / 2m)
				return;

			UpdateStopOrder(newStop, true);
		}
		else if (Position < 0m)
		{
			var profit = _entryPrice - candle.ClosePrice;

			if (profit < stopDistance + stepDistance)
				return;

			var newStop = candle.ClosePrice + stopDistance;

			if (_currentStopLevel.HasValue && newStop >= _currentStopLevel.Value - step / 2m)
				return;

			UpdateStopOrder(newStop, false);
		}
	}

	private void UpdateStopOrder(decimal newStop, bool isLong)
	{
		var volume = Math.Abs(Position);

		if (volume <= 0m)
			return;

		if (_protectiveStopOrder != null && _protectiveStopOrder.State == OrderStates.Active)
			CancelOrder(_protectiveStopOrder);

		_protectiveStopOrder = isLong
			? SellStop(volume, newStop)
			: BuyStop(volume, newStop);

		_currentStopLevel = newStop;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null)
			return;

		if (trade.Order == _buyStopOrder)
		{
			var tradePrice = trade.Trade.Price;
			var tradeVolume = trade.Trade.Volume;

			_entryPrice = tradePrice;
			_currentStopLevel = _buyStopLossPrice;

			CancelProtectionOrders();

			_protectiveStopOrder = SellStop(tradeVolume, _buyStopLossPrice);
			_takeProfitOrder = SellLimit(tradeVolume, _buyTakeProfitPrice);
		}
		else if (trade.Order == _sellStopOrder)
		{
			var tradePrice = trade.Trade.Price;
			var tradeVolume = trade.Trade.Volume;

			_entryPrice = tradePrice;
			_currentStopLevel = _sellStopLossPrice;

			CancelProtectionOrders();

			_protectiveStopOrder = BuyStop(tradeVolume, _sellStopLossPrice);
			_takeProfitOrder = BuyLimit(tradeVolume, _sellTakeProfitPrice);
		}
		else if (trade.Order == _protectiveStopOrder || trade.Order == _takeProfitOrder)
		{
			CancelProtectionOrders();
			_entryPrice = 0m;
			_currentStopLevel = null;
		}
	}

	private void CancelPendingOrders()
	{
		if (_buyStopOrder != null && _buyStopOrder.State == OrderStates.Active)
			CancelOrder(_buyStopOrder);

		if (_sellStopOrder != null && _sellStopOrder.State == OrderStates.Active)
			CancelOrder(_sellStopOrder);

		_buyStopOrder = null;
		_sellStopOrder = null;
	}

	private void CancelProtectionOrders()
	{
		if (_protectiveStopOrder != null && _protectiveStopOrder.State == OrderStates.Active)
			CancelOrder(_protectiveStopOrder);

		if (_takeProfitOrder != null && _takeProfitOrder.State == OrderStates.Active)
			CancelOrder(_takeProfitOrder);

		_protectiveStopOrder = null;
		_takeProfitOrder = null;
		_currentStopLevel = null;
	}

	private void ClosePosition()
	{
		var volume = Math.Abs(Position);

		if (volume <= 0m)
			return;

		if (Position > 0m)
			SellMarket(volume);
		else
			BuyMarket(volume);

		CancelProtectionOrders();
	}
}
