using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

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

	private readonly List<decimal> _dailyTrueRanges = [];
	private decimal? _previousDailyClose;
	private decimal? _dailyAtr;
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private DateTime _sessionDate;
	private decimal _sessionHigh;
	private decimal _sessionLow;
	private bool _tradedToday;
	private bool _skipToday;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal TriggerPercent { get => _triggerPercent.Value; set => _triggerPercent.Value = value; }

	public RangeFollowerStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Working timeframe used for session boundaries.", "General");
		_triggerPercent = Param(nameof(TriggerPercent), 60m)
			.SetRange(10m, 90m)
			.SetDisplay("Trigger Percent", "Percentage of daily ATR used as the entry/stop distance.", "Signal");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, TimeSpan.FromDays(1).TimeFrame()), (Security, DataType.Level1)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_dailyTrueRanges.Clear();
		_previousDailyClose = null;
		_dailyAtr = null;
		_bestBid = null;
		_bestAsk = null;
		ResetSession(default);
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		SubscribeCandles(TimeSpan.FromDays(1).TimeFrame())
			.Bind(ProcessDailyCandle)
			.Start();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessWorkingCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var trueRange = _previousDailyClose is decimal previous
			? Math.Max(candle.HighPrice - candle.LowPrice,
				Math.Max(Math.Abs(candle.HighPrice - previous), Math.Abs(candle.LowPrice - previous)))
			: candle.HighPrice - candle.LowPrice;

		_dailyTrueRanges.Add(trueRange);
		if (_dailyTrueRanges.Count > 20)
			_dailyTrueRanges.RemoveAt(0);

		if (_dailyTrueRanges.Count == 20)
			_dailyAtr = _dailyTrueRanges.Average();

		_previousDailyClose = candle.ClosePrice;
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid && bid > 0m)
			_bestBid = bid;

		if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask && ask > 0m)
			_bestAsk = ask;

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
		}
		else
		{
			_sessionHigh = Math.Max(_sessionHigh, candle.HighPrice);
			_sessionLow = Math.Min(_sessionLow, candle.LowPrice);
		}

		if (_dailyAtr is decimal atr)
		{
			var trigger = atr * TriggerPercent / 100m;
			if (!_tradedToday && _sessionHigh - _sessionLow >= trigger)
				_skipToday = true;
		}

		if (Position != 0)
			ApplyProtection(candle.HighPrice, candle.LowPrice);

		EvaluateQuote();
	}

	private void EvaluateQuote()
	{
		if (_dailyAtr is not decimal atr || _sessionDate == default || _tradedToday || _skipToday || Position != 0)
			return;

		if (_bestBid is not decimal bid || _bestAsk is not decimal ask || _sessionLow == 0m || _sessionHigh == 0m)
			return;

		var trigger = atr * TriggerPercent / 100m;
		var residual = atr - trigger;

		var longDistance = bid - _sessionLow;
		var shortDistance = _sessionHigh - ask;

		if (longDistance < trigger && shortDistance < trigger)
			return;

		// If both are true on a wide session, take the side with the larger excursion.
		if (longDistance >= shortDistance)
			Enter(Sides.Buy, ask, trigger, residual);
		else
			Enter(Sides.Sell, bid, trigger, residual);
	}

	private void Enter(Sides side, decimal price, decimal trigger, decimal residual)
	{
		if (side == Sides.Buy)
			BuyMarket();
		else
			SellMarket();

		_tradedToday = true;
		_stopPrice = side == Sides.Buy ? price - trigger : price + trigger;
		_takePrice = side == Sides.Buy ? price + residual : price - residual;
	}

	private void ApplyProtection(decimal high, decimal low)
	{
		if (Position > 0 &&
			((_stopPrice is decimal stop && low <= stop) || (_takePrice is decimal take && high >= take)))
			Flatten();
		else if (Position < 0 &&
			((_stopPrice is decimal stop && high >= stop) || (_takePrice is decimal take && low <= take)))
			Flatten();
	}

	private void Flatten()
	{
		if (Position > 0)
			SellMarket(Math.Abs(Position));
		else if (Position < 0)
			BuyMarket(Math.Abs(Position));

		_stopPrice = null;
		_takePrice = null;
	}

	private void ResetSession(DateTime date)
	{
		_sessionDate = date;
		_sessionHigh = 0m;
		_sessionLow = 0m;
		_tradedToday = false;
		_skipToday = false;
		_stopPrice = null;
		_takePrice = null;
	}
}
