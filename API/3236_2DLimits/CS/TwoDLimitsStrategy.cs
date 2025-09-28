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
/// Port of the MetaTrader expert advisor 2DLimits_EA_v2.
/// Places directional stop orders above/below the previous daily range when the last two days align in trend.
/// Applies midpoint-based stop-loss and full range take-profit targets once the breakout is triggered.
/// </summary>
public class TwoDLimitsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<ICandleMessage> _recentDailyCandles = new(2);

	private Order _buyEntryOrder;
	private Order _sellEntryOrder;
	private Order _longStopOrder;
	private Order _longTakeOrder;
	private Order _shortStopOrder;
	private Order _shortTakeOrder;

	private decimal? _nextLongStopPrice;
	private decimal? _nextLongTakePrice;
	private decimal? _nextShortStopPrice;
	private decimal? _nextShortTakePrice;

	private DateTime? _activeEntryDay;
	private DateTime? _lastTradeDay;
	private DateTime? _lastCompletedTradeDay;

	private decimal _lastBid;
	private decimal _lastAsk;

	private decimal _previousPosition;


	/// <summary>
	/// Candle type that provides the daily reference range.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public TwoDLimitsStrategy()
	{

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used to read the previous day range.", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_recentDailyCandles.Clear();
		_buyEntryOrder = null;
		_sellEntryOrder = null;
		_longStopOrder = null;
		_longTakeOrder = null;
		_shortStopOrder = null;
		_shortTakeOrder = null;
		_nextLongStopPrice = null;
		_nextLongTakePrice = null;
		_nextShortStopPrice = null;
		_nextShortTakePrice = null;
		_activeEntryDay = null;
		_lastTradeDay = null;
		_lastCompletedTradeDay = null;
		_lastBid = 0m;
		_lastAsk = 0m;
		_previousPosition = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		SubscribeCandles(CandleType)
			.Bind(ProcessDailyCandle)
			.Start();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
			_lastBid = (decimal)bid;

		if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
			_lastAsk = (decimal)ask;
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		CancelEntryOrders();

		_recentDailyCandles.Enqueue(candle);

		while (_recentDailyCandles.Count > 2)
			_recentDailyCandles.Dequeue();

		if (_recentDailyCandles.Count < 2)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
			return;

		var candles = _recentDailyCandles.ToArray();
		var older = candles[0];
		var previous = candles[1];

		var range = previous.HighPrice - previous.LowPrice;
		if (range <= 0m)
			return;

		var middle = (previous.HighPrice + previous.LowPrice) / 2m;
		var upcomingDay = previous.OpenTime.Date.AddDays(1);

		var hasLongBias = previous.HighPrice > older.HighPrice && previous.LowPrice > older.LowPrice;
		var hasShortBias = previous.HighPrice < older.HighPrice && previous.LowPrice < older.LowPrice;

		var volume = Volume;
		if (volume <= 0m)
			return;

		var ask = _lastAsk > 0m ? _lastAsk : (Security?.BestAsk?.Price ?? 0m);
		var bid = _lastBid > 0m ? _lastBid : (Security?.BestBid?.Price ?? 0m);
		var lastTradePrice = Security?.LastTick?.Price ?? 0m;

		if (ask <= 0m && lastTradePrice > 0m)
			ask = lastTradePrice;

		if (bid <= 0m && lastTradePrice > 0m)
			bid = lastTradePrice;

		var anyOrderPlaced = false;

		if (hasLongBias && ask > 0m && ask < middle)
		{
			var entryPrice = RoundPrice(previous.HighPrice);
			var stopPrice = RoundPrice(middle);
			var takeProfitPrice = RoundPrice(previous.HighPrice + range);

			if (entryPrice > 0m && stopPrice > 0m && takeProfitPrice > 0m)
			{
				_buyEntryOrder = BuyStop(volume, entryPrice);
				_nextLongStopPrice = stopPrice;
				_nextLongTakePrice = takeProfitPrice;
				_activeEntryDay = upcomingDay;
				anyOrderPlaced = true;
			}
		}
		else
		{
			_nextLongStopPrice = null;
			_nextLongTakePrice = null;
		}

		if (hasShortBias && bid > 0m && bid > middle)
		{
			var entryPrice = RoundPrice(previous.LowPrice);
			var stopPrice = RoundPrice(middle);
			var takeProfitPrice = RoundPrice(previous.LowPrice - range);

			if (entryPrice > 0m && stopPrice > 0m && takeProfitPrice > 0m)
			{
				_sellEntryOrder = SellStop(volume, entryPrice);
				_nextShortStopPrice = stopPrice;
				_nextShortTakePrice = takeProfitPrice;
				_activeEntryDay = upcomingDay;
				anyOrderPlaced = true;
			}
		}
		else
		{
			_nextShortStopPrice = null;
			_nextShortTakePrice = null;
		}

		if (!anyOrderPlaced)
			_activeEntryDay = null;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade?.Order == null)
			return;

		if (_buyEntryOrder != null && trade.Order == _buyEntryOrder)
		{
			_lastTradeDay = _activeEntryDay ?? trade.Trade.ServerTime.Date;
			CancelOrder(ref _sellEntryOrder);
		}
		else if (_sellEntryOrder != null && trade.Order == _sellEntryOrder)
		{
			_lastTradeDay = _activeEntryDay ?? trade.Trade.ServerTime.Date;
			CancelOrder(ref _buyEntryOrder);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		var position = Position;

		if (position > 0m)
		{
			UpdateLongProtection(position);
		}
		else if (position < 0m)
		{
			UpdateShortProtection(-position);
		}
		else if (_previousPosition != 0m)
		{
			_lastCompletedTradeDay = _lastTradeDay;
			CancelOrder(ref _longStopOrder);
			CancelOrder(ref _longTakeOrder);
			CancelOrder(ref _shortStopOrder);
			CancelOrder(ref _shortTakeOrder);
			_nextLongStopPrice = null;
			_nextLongTakePrice = null;
			_nextShortStopPrice = null;
			_nextShortTakePrice = null;
			_activeEntryDay = null;
		}

		_previousPosition = position;
	}

	/// <inheritdoc />
	protected override void OnOrderReceived(Order order)
	{
		base.OnOrderReceived(order);

		if (order == null)
			return;

		if (order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
		{
			if (order == _buyEntryOrder)
				_buyEntryOrder = null;
			else if (order == _sellEntryOrder)
				_sellEntryOrder = null;
			else if (order == _longStopOrder)
				_longStopOrder = null;
			else if (order == _longTakeOrder)
				_longTakeOrder = null;
			else if (order == _shortStopOrder)
				_shortStopOrder = null;
			else if (order == _shortTakeOrder)
				_shortTakeOrder = null;
		}
	}

	private void UpdateLongProtection(decimal volume)
	{
		CancelOrder(ref _sellEntryOrder);
		_nextShortStopPrice = null;
		_nextShortTakePrice = null;

		if (_nextLongStopPrice is decimal stopPrice && stopPrice > 0m)
		{
			ReplaceOrder(ref _longStopOrder, SellStop(volume, stopPrice));
		}
		else
		{
			CancelOrder(ref _longStopOrder);
		}

		if (_nextLongTakePrice is decimal takeProfit && takeProfit > 0m)
		{
			ReplaceOrder(ref _longTakeOrder, SellLimit(volume, takeProfit));
		}
		else
		{
			CancelOrder(ref _longTakeOrder);
		}
	}

	private void UpdateShortProtection(decimal volume)
	{
		CancelOrder(ref _buyEntryOrder);
		_nextLongStopPrice = null;
		_nextLongTakePrice = null;

		if (_nextShortStopPrice is decimal stopPrice && stopPrice > 0m)
		{
			ReplaceOrder(ref _shortStopOrder, BuyStop(volume, stopPrice));
		}
		else
		{
			CancelOrder(ref _shortStopOrder);
		}

		if (_nextShortTakePrice is decimal takeProfit && takeProfit > 0m)
		{
			ReplaceOrder(ref _shortTakeOrder, BuyLimit(volume, takeProfit));
		}
		else
		{
			CancelOrder(ref _shortTakeOrder);
		}
	}

	private void CancelEntryOrders()
	{
		if (_buyEntryOrder != null)
		{
			if (_buyEntryOrder.State is OrderStates.Active or OrderStates.Pending)
				CancelOrder(_buyEntryOrder);

			_buyEntryOrder = null;
		}

		if (_sellEntryOrder != null)
		{
			if (_sellEntryOrder.State is OrderStates.Active or OrderStates.Pending)
				CancelOrder(_sellEntryOrder);

			_sellEntryOrder = null;
		}

		_nextLongStopPrice = null;
		_nextLongTakePrice = null;
		_nextShortStopPrice = null;
		_nextShortTakePrice = null;
		_activeEntryDay = null;
	}

	private void CancelOrder(ref Order order)
	{
		if (order == null)
			return;

		if (order.State is OrderStates.Active or OrderStates.Pending)
			CancelOrder(order);

		order = null;
	}

	private void ReplaceOrder(ref Order target, Order newOrder)
	{
		if (target != null && target != newOrder)
		{
			if (target.State is OrderStates.Active or OrderStates.Pending)
				CancelOrder(target);
		}

		target = newOrder;
	}

	private decimal RoundPrice(decimal price)
	{
		var step = Security?.PriceStep;

		if (step == null || step.Value <= 0m)
			return price;

		return Math.Round(price / step.Value) * step.Value;
	}
}

