namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class CurrencyStopLossTakeProfitStrategy : Strategy
{
	private sealed class PositionLot
	{
		public PositionLot(Sides direction, decimal volume, decimal entryPrice)
		{
			Direction = direction;
			Volume = volume;
			EntryPrice = entryPrice;
		}

		public Sides Direction { get; }

		public decimal Volume { get; set; }

		public decimal EntryPrice { get; }

		public bool ExitRequested { get; set; }
	}

	private readonly StrategyParam<decimal> _takeProfitCurrency;
	private readonly StrategyParam<decimal> _stopLossCurrency;

	private readonly List<PositionLot> _longLots = new();
	private readonly List<PositionLot> _shortLots = new();
	private readonly Dictionary<Order, PositionLot> _closingOrders = new();

	private decimal? _currentBid;
	private decimal? _currentAsk;
	private decimal? _lastTradePrice;

	public CurrencyStopLossTakeProfitStrategy()
	{
		_takeProfitCurrency = Param(nameof(TakeProfitCurrency), 5m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take profit (currency)", "Profit target measured in account currency for each position.", "Risk")
			.SetCanOptimize(true);

		_stopLossCurrency = Param(nameof(StopLossCurrency), 8m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop loss (currency)", "Maximum tolerable loss in account currency for each position.", "Risk")
			.SetCanOptimize(true);
	}

	public decimal TakeProfitCurrency
	{
		get => _takeProfitCurrency.Value;
		set => _takeProfitCurrency.Value = value;
	}

	public decimal StopLossCurrency
	{
		get => _stopLossCurrency.Value;
		set => _stopLossCurrency.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, DataType.Level1)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_longLots.Clear();
		_shortLots.Clear();
		_closingOrders.Clear();

		_currentBid = null;
		_currentAsk = null;
		_lastTradePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid) && bid is decimal bidPrice)
			_currentBid = bidPrice;

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask) && ask is decimal askPrice)
			_currentAsk = askPrice;

		if (message.Changes.TryGetValue(Level1Fields.LastTradePrice, out var last) && last is decimal lastPrice)
			_lastTradePrice = lastPrice;

		EvaluatePositions();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		RegisterTrade(trade);
		EvaluatePositions();
	}

	private void RegisterTrade(MyTrade trade)
	{
		if (trade.Trade == null)
			return;

		var order = trade.Order;
		if (order == null)
			return;

		var direction = order.Direction;
		if (direction == null)
			return;

		var volume = trade.Trade.Volume ?? 0m;
		if (volume <= 0m)
			return;

		volume = Math.Abs(volume);

		var price = trade.Trade.Price ?? order.Price ?? 0m;
		if (price == 0m)
			return;

		if (_closingOrders.TryGetValue(order, out var closingLot))
		{
			ProcessTrackedClosing(order, closingLot, volume);
			return;
		}

		if (direction == Sides.Buy)
		{
			ProcessShortReductions(volume);

			if (volume > 0m)
			{
				// Store new long exposure opened by the fill.
				_longLots.Add(new PositionLot(Sides.Buy, volume, price));
			}
		}
		else if (direction == Sides.Sell)
		{
			ProcessLongReductions(volume);

			if (volume > 0m)
			{
				// Store new short exposure opened by the fill.
				_shortLots.Add(new PositionLot(Sides.Sell, volume, price));
			}
		}
	}

	private void ProcessTrackedClosing(Order order, PositionLot lot, decimal tradeVolume)
	{
		var reduced = Math.Min(lot.Volume, tradeVolume);
		lot.Volume -= reduced;

		if (lot.Volume <= 0m)
		{
			RemoveLot(lot);
			lot.ExitRequested = false;
			_closingOrders.Remove(order);
		}
	}

	private void ProcessShortReductions(decimal volume)
	{
		for (var i = 0; volume > 0m && i < _shortLots.Count; )
		{
			var lot = _shortLots[i];
			var closed = Math.Min(lot.Volume, volume);

			lot.Volume -= closed;
			volume -= closed;

			if (lot.Volume <= 0m)
			{
				_shortLots.RemoveAt(i);
			}
			else
			{
				lot.ExitRequested = false;
				i++;
			}
		}
	}

	private void ProcessLongReductions(decimal volume)
	{
		for (var i = 0; volume > 0m && i < _longLots.Count; )
		{
			var lot = _longLots[i];
			var closed = Math.Min(lot.Volume, volume);

			lot.Volume -= closed;
			volume -= closed;

			if (lot.Volume <= 0m)
			{
				_longLots.RemoveAt(i);
			}
			else
			{
				lot.ExitRequested = false;
				i++;
			}
		}
	}

	private void RemoveLot(PositionLot lot)
	{
		if (lot.Direction == Sides.Buy)
			_longLots.Remove(lot);
		else
			_shortLots.Remove(lot);
	}

	private void EvaluatePositions()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_longLots.Count > 0)
		{
			var bid = _currentBid ?? _lastTradePrice;
			if (bid is decimal bidPrice)
			{
				for (var i = 0; i < _longLots.Count; i++)
				{
					var lot = _longLots[i];

					if (lot.ExitRequested)
						continue;

					var profit = PriceToMoney(bidPrice - lot.EntryPrice, lot.Volume);

					if (TakeProfitCurrency > 0m && profit >= TakeProfitCurrency)
					{
						RequestExit(lot, Sides.Sell);
						continue;
					}

					var loss = -profit;
					if (StopLossCurrency > 0m && loss >= StopLossCurrency)
					{
						RequestExit(lot, Sides.Sell);
					}
				}
			}
		}

		if (_shortLots.Count > 0)
		{
			var ask = _currentAsk ?? _lastTradePrice;
			if (ask is decimal askPrice)
			{
				for (var i = 0; i < _shortLots.Count; i++)
				{
					var lot = _shortLots[i];

					if (lot.ExitRequested)
						continue;

					var profit = PriceToMoney(lot.EntryPrice - askPrice, lot.Volume);

					if (TakeProfitCurrency > 0m && profit >= TakeProfitCurrency)
					{
						RequestExit(lot, Sides.Buy);
						continue;
					}

					var loss = -profit;
					if (StopLossCurrency > 0m && loss >= StopLossCurrency)
					{
						RequestExit(lot, Sides.Buy);
					}
				}
			}
		}
	}

	private void RequestExit(PositionLot lot, Sides closeSide)
	{
		if (lot.ExitRequested)
			return;

		if (lot.Volume <= 0m)
			return;

		Order? order = closeSide == Sides.Sell
			? SellMarket(lot.Volume)
			: BuyMarket(lot.Volume);

		if (order == null)
			return;

		lot.ExitRequested = true;
		_closingOrders[order] = lot;
	}

	private decimal PriceToMoney(decimal priceDiff, decimal volume)
	{
		if (volume <= 0m || priceDiff == 0m)
			return 0m;

		var security = Security;
		if (security == null)
			return priceDiff * volume;

		var priceStep = security.PriceStep ?? 0m;
		var stepPrice = security.StepPrice ?? 0m;

		if (priceStep <= 0m || stepPrice <= 0m)
			return priceDiff * volume;

		return priceDiff / priceStep * stepPrice * volume;
	}

	/// <inheritdoc />
	protected override void OnOrderRegisterFailed(OrderFail fail)
	{
		base.OnOrderRegisterFailed(fail);

		if (_closingOrders.TryGetValue(fail.Order, out var lot))
		{
			lot.ExitRequested = false;
			_closingOrders.Remove(fail.Order);
		}
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (!_closingOrders.TryGetValue(order, out var lot))
			return;

		if (order.State is OrderStates.Failed or OrderStates.Canceled)
		{
			lot.ExitRequested = false;
			_closingOrders.Remove(order);
		}
		else if (order.State == OrderStates.Done)
		{
			// In case the order completed without generating trades (unlikely), remove the lot.
			if (lot.Volume <= 0m)
			{
				RemoveLot(lot);
			}

			lot.ExitRequested = false;
			_closingOrders.Remove(order);
		}
	}
}
