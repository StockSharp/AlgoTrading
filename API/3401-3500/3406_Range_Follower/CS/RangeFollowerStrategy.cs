using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Daily ATR range follower using live best bid/ask and one entry per session.
/// </summary>
public class RangeFollowerStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _triggerPercent;

	private decimal? _dailyAtr;
	private decimal? _sessionAtr;
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private DateTime _sessionDate;
	private decimal _sessionHigh;
	private decimal _sessionLow;
	private bool _tradedToday;
	private bool _skipToday;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private Unit _takeDistance;
	private Unit _stopDistance;
	private Order _entryOrder;
	private Order _exitOrder;
	private Sides _entrySide;
	private decimal _entryVolume;
	private decimal _entryValue;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal TriggerPercent { get => _triggerPercent.Value; set => _triggerPercent.Value = value; }

	public RangeFollowerStrategy()
	{
		Volume = 0.1m;
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Working timeframe used for session boundaries.", "General");
		_triggerPercent = Param(nameof(TriggerPercent), 60m)
			.SetRange(10m, 90m)
			.SetDisplay("Trigger Percent", "Percentage of daily ATR used as the entry/stop distance.", "Signal");
		Trades.TradeAdded += ProcessTrade;
		OrderRegistering += order =>
		{
			if (Position != 0m && order.Side == (Position > 0m ? Sides.Sell : Sides.Buy))
				_exitOrder = order;
		};
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, TimeSpan.FromDays(1).TimeFrame()), (Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_dailyAtr = null;
		_bestBid = null;
		_bestAsk = null;
		_takeDistance = _stopDistance = null;
		_entryOrder = _exitOrder = null;
		_entrySide = default;
		_entryVolume = _entryValue = 0m;
		ResetSession(default);
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var atr = new AverageTrueRange { Length = 20 };
		SubscribeCandles(TimeSpan.FromDays(1).TimeFrame())
			.BindEx(atr, ProcessDailyCandle)
			.Start();

		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ProcessLevel1).Start();
		}

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessWorkingCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDailyCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished || !value.IsFormed)
			return;
		_dailyAtr = value.GetValue<decimal>();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid && bid > 0m)
			_bestBid = bid;

		if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask && ask > 0m)
			_bestAsk = ask;

		if (_bestBid is decimal currentBid && _bestAsk is decimal currentAsk)
		{
			if (_sessionDate == message.ServerTime.Date)
			{
				_sessionHigh = Math.Max(_sessionHigh, Math.Max(currentBid, currentAsk));
				_sessionLow = Math.Min(_sessionLow, Math.Min(currentBid, currentAsk));
			}
			if (Position > 0m)
				ApplyProtection(currentBid, currentBid);
			else if (Position < 0m)
				ApplyProtection(currentAsk, currentAsk);
		}

		EvaluateQuote();
	}

	private void ProcessWorkingCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var date = candle.OpenTime.Date;
		if (_sessionDate != date)
		{
			if (_sessionDate != default && Position != 0)
				Flatten();

			ResetSession(date);
			_sessionHigh = candle.HighPrice;
			_sessionLow = candle.LowPrice;
			_sessionAtr = _dailyAtr;
			if (_sessionAtr is decimal initialAtr)
				_skipToday = _sessionHigh - _sessionLow > initialAtr * TriggerPercent / 100m;
		}
		else
		{
			_sessionHigh = Math.Max(_sessionHigh, candle.HighPrice);
			_sessionLow = Math.Min(_sessionLow, candle.LowPrice);
		}

		if (Position != 0)
			ApplyProtection(candle.HighPrice, candle.LowPrice);

		EvaluateQuote();
	}

	private void EvaluateQuote()
	{
		if (_sessionAtr is not decimal atr || _sessionDate == default || CurrentTime.Date != _sessionDate || _tradedToday || _skipToday || Position != 0)
			return;
		if (IsPending(_entryOrder) || IsPending(_exitOrder) || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (_bestBid is not decimal bid || _bestAsk is not decimal ask || _sessionLow == 0m || _sessionHigh == 0m)
			return;

		var trigger = atr * TriggerPercent / 100m;
		var residual = atr - trigger;

		var longDistance = bid - _sessionLow;
		var shortDistance = _sessionHigh - ask;

		if (longDistance <= trigger && shortDistance <= trigger)
			return;

		// If both are true on a wide session, take the side with the larger excursion.
		if (longDistance >= shortDistance)
			Enter(Sides.Buy, trigger, residual);
		else
			Enter(Sides.Sell, trigger, residual);
	}

	private static bool IsPending(Order order)
		=> order is not null && order.State is not (OrderStates.Done or OrderStates.Failed);

	private void Enter(Sides side, decimal trigger, decimal residual)
	{
		if (_takeDistance is null)
		{
			_takeDistance = new Unit(residual);
			_stopDistance = new Unit(trigger);
			StartProtection(_takeDistance, _stopDistance, useMarketOrders: true, isLocalStop: true);
		}
		else
		{
			// The native position controller retains these Unit objects. Reconfigure only while flat.
			_takeDistance.Value = residual;
			_stopDistance.Value = trigger;
		}
		_tradedToday = true;
		_entrySide = side;
		_entryVolume = _entryValue = 0m;
		_entryOrder = side == Sides.Buy ? BuyMarket() : SellMarket();
	}

	private void ProcessTrade(MyTrade trade)
	{
		if (trade.Order.Side == _entrySide)
		{
			_entryVolume += trade.Trade.Volume;
			_entryValue += trade.Trade.Price * trade.Trade.Volume;
		}
		else if (_entryVolume > 0m)
		{
			var closed = Math.Min(_entryVolume, trade.Trade.Volume);
			_entryValue -= _entryValue / _entryVolume * closed;
			_entryVolume -= closed;
		}
		if (_entryVolume > 0m)
		{
			var entry = _entryValue / _entryVolume;
			var direction = _entrySide == Sides.Buy ? 1m : -1m;
			_stopPrice = entry - direction * _stopDistance.Value;
			_takePrice = entry + direction * _takeDistance.Value;
		}
		else
		{
			_entryValue = 0m;
			_stopPrice = _takePrice = null;
		}
	}

	private void ApplyProtection(decimal high, decimal low)
	{
		if (Position > 0 &&
			((_stopPrice is decimal longStop && low <= longStop) || (_takePrice is decimal longTake && high >= longTake)))
			Flatten();
		else if (Position < 0 &&
			((_stopPrice is decimal shortStop && high >= shortStop) || (_takePrice is decimal shortTake && low <= shortTake)))
			Flatten();
	}

	private void Flatten()
	{
		if (IsPending(_exitOrder))
			return;
		if (Position > 0)
			_exitOrder = SellMarket(Math.Abs(Position));
		else if (Position < 0)
			_exitOrder = BuyMarket(Math.Abs(Position));
	}

	private void ResetSession(DateTime date)
	{
		_sessionDate = date;
		_sessionAtr = null;
		_sessionHigh = 0m;
		_sessionLow = 0m;
		_tradedToday = false;
		_skipToday = false;
		_stopPrice = null;
		_takePrice = null;
	}
}
