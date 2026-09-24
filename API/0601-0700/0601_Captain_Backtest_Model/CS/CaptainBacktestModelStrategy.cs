using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Captain Backtest Model.
/// Builds the morning range, fixes the day's bias on the first range break,
/// waits for a retracement, then enters on a close through the previous candle.
/// </summary>
public class CaptainBacktestModelStrategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _prevRangeStart;
	private readonly StrategyParam<TimeSpan> _prevRangeEnd;
	private readonly StrategyParam<TimeSpan> _takeStart;
	private readonly StrategyParam<TimeSpan> _takeEnd;
	private readonly StrategyParam<TimeSpan> _tradeStart;
	private readonly StrategyParam<TimeSpan> _tradeEnd;
	private readonly StrategyParam<decimal> _risk;
	private readonly StrategyParam<decimal> _reward;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _sessionDate;
	private decimal? _rangeHigh;
	private decimal? _rangeLow;
	private int _bias;
	private bool _retracementSeen;
	private bool _tradedToday;
	private ICandleMessage _previousCandle;
	private decimal _entryPrice;

	public TimeSpan PrevRangeStart { get => _prevRangeStart.Value; set => _prevRangeStart.Value = value; }
	public TimeSpan PrevRangeEnd { get => _prevRangeEnd.Value; set => _prevRangeEnd.Value = value; }
	public TimeSpan TakeStart { get => _takeStart.Value; set => _takeStart.Value = value; }
	public TimeSpan TakeEnd { get => _takeEnd.Value; set => _takeEnd.Value = value; }
	public TimeSpan TradeStart { get => _tradeStart.Value; set => _tradeStart.Value = value; }
	public TimeSpan TradeEnd { get => _tradeEnd.Value; set => _tradeEnd.Value = value; }
	public decimal Risk { get => _risk.Value; set => _risk.Value = value; }
	public decimal Reward { get => _reward.Value; set => _reward.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public CaptainBacktestModelStrategy()
	{
		_prevRangeStart = Param(nameof(PrevRangeStart), new TimeSpan(6, 0, 0))
			.SetDisplay("Previous Range Start", "Start of the range used to establish the daily bias.", "Timing");
		_prevRangeEnd = Param(nameof(PrevRangeEnd), new TimeSpan(10, 0, 0))
			.SetDisplay("Previous Range End", "End of the range used to establish the daily bias.", "Timing");
		_takeStart = Param(nameof(TakeStart), new TimeSpan(10, 0, 0))
			.SetDisplay("Take Start", "Earliest time when a range break may establish the daily bias.", "Timing");
		_takeEnd = Param(nameof(TakeEnd), new TimeSpan(11, 15, 0))
			.SetDisplay("Take End", "Latest time when a range break may establish the daily bias.", "Timing");
		_tradeStart = Param(nameof(TradeStart), new TimeSpan(10, 0, 0))
			.SetDisplay("Trade Start", "Start of the entry window.", "Timing");
		_tradeEnd = Param(nameof(TradeEnd), new TimeSpan(16, 0, 0))
			.SetDisplay("Trade End", "End of the entry window and forced-exit time.", "Timing");
		_risk = Param(nameof(Risk), 25m)
			.SetGreaterThanZero()
			.SetDisplay("Risk", "Fixed stop distance in price points.", "Risk");
		_reward = Param(nameof(Reward), 75m)
			.SetGreaterThanZero()
			.SetDisplay("Reward", "Fixed profit-target distance in price points.", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle series used by the model.", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		ResetSession(default);
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		ResetSession(default);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		var date = candle.OpenTime.Date;
		if (_sessionDate != date)
		{
			if (_sessionDate != default && Position != 0)
				ClosePosition();

			ResetSession(date);
		}

		var time = candle.OpenTime.TimeOfDay;

		// Step 1: collect the 06:00-10:00 range (end is exclusive).
		if (time >= PrevRangeStart && time < PrevRangeEnd)
		{
			_rangeHigh = _rangeHigh is decimal high ? Math.Max(high, candle.HighPrice) : candle.HighPrice;
			_rangeLow = _rangeLow is decimal low ? Math.Min(low, candle.LowPrice) : candle.LowPrice;
			_previousCandle = candle;
			return;
		}

		// Fixed R:R exits. If both levels are touched inside one candle, use the adverse
		// level first because the intrabar path is unknown.
		if (Position > 0 && _entryPrice > 0m)
		{
			if (candle.LowPrice <= _entryPrice - Risk || candle.HighPrice >= _entryPrice + Reward)
			{
				ClosePosition();
				_previousCandle = candle;
				return;
			}
		}
		else if (Position < 0 && _entryPrice > 0m)
		{
			if (candle.HighPrice >= _entryPrice + Risk || candle.LowPrice <= _entryPrice - Reward)
			{
				ClosePosition();
				_previousCandle = candle;
				return;
			}
		}

		// The trade window also acts as the time exit.
		if (time >= TradeEnd)
		{
			if (Position != 0)
				ClosePosition();

			_previousCandle = candle;
			return;
		}

		// Step 2: the first side of the morning range taken between TakeStart and TakeEnd
		// fixes the bias. A candle taking both sides is ambiguous, so no bias is assigned.
		if (_bias == 0 && _rangeHigh is decimal rangeHigh && _rangeLow is decimal rangeLow &&
			time >= TakeStart && time <= TakeEnd)
		{
			var brokeHigh = candle.HighPrice > rangeHigh;
			var brokeLow = candle.LowPrice < rangeLow;

			if (brokeHigh ^ brokeLow)
				_bias = brokeHigh ? 1 : -1;
		}

		// No bias by TakeEnd means no trade for this session.
		if (_bias == 0 || _tradedToday || time < TradeStart || time > TradeEnd || _previousCandle is null)
		{
			_previousCandle = candle;
			return;
		}

		// Step 3: after the bias is known, wait for a pullback. The model's two
		// documented retracement definitions are an opposite-colour close or a sweep
		// of the previous candle's opposite extreme.
		if (_bias > 0)
		{
			if (candle.ClosePrice < candle.OpenPrice || candle.LowPrice < _previousCandle.LowPrice)
				_retracementSeen = true;

			if (_retracementSeen && candle.ClosePrice > _previousCandle.HighPrice)
				Enter(Sides.Buy, candle.ClosePrice);
		}
		else
		{
			if (candle.ClosePrice > candle.OpenPrice || candle.HighPrice > _previousCandle.HighPrice)
				_retracementSeen = true;

			if (_retracementSeen && candle.ClosePrice < _previousCandle.LowPrice)
				Enter(Sides.Sell, candle.ClosePrice);
		}

		_previousCandle = candle;
	}

	private void Enter(Sides side, decimal price)
	{
		if (_tradedToday)
			return;

		_tradedToday = true;
		_entryPrice = price;

		if (side == Sides.Buy)
			BuyMarket();
		else
			SellMarket();
	}

	private void ResetSession(DateTime date)
	{
		_sessionDate = date;
		_rangeHigh = null;
		_rangeLow = null;
		_bias = 0;
		_retracementSeen = false;
		_tradedToday = false;
		_previousCandle = null;
		_entryPrice = 0m;
	}
}
