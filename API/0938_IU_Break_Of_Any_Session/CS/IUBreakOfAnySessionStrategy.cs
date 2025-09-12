using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout from custom session high or low.
/// Enters once per day when price breaks session range.
/// </summary>
public class IUBreakOfAnySessionStrategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionEnd;
	private readonly StrategyParam<TimeSpan> _entryStart;
	private readonly StrategyParam<TimeSpan> _entryEnd;
	private readonly StrategyParam<TimeSpan> _exitStart;
	private readonly StrategyParam<TimeSpan> _exitEnd;
	private readonly StrategyParam<decimal> _profitFactor;
	private readonly StrategyParam<DataType> _candleType;

	private bool _insideSession;
	private decimal _sessionHigh;
	private decimal _sessionLow;
	private decimal _extendedHigh;
	private decimal _extendedLow;
	private bool _tradeExecuted;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _targetPrice;

	public TimeSpan SessionStart { get => _sessionStart.Value; set => _sessionStart.Value = value; }
	public TimeSpan SessionEnd { get => _sessionEnd.Value; set => _sessionEnd.Value = value; }
	public TimeSpan EntryStart { get => _entryStart.Value; set => _entryStart.Value = value; }
	public TimeSpan EntryEnd { get => _entryEnd.Value; set => _entryEnd.Value = value; }
	public TimeSpan ExitStart { get => _exitStart.Value; set => _exitStart.Value = value; }
	public TimeSpan ExitEnd { get => _exitEnd.Value; set => _exitEnd.Value = value; }
	public decimal ProfitFactor { get => _profitFactor.Value; set => _profitFactor.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public IUBreakOfAnySessionStrategy()
	{
		_sessionStart = Param(nameof(SessionStart), new TimeSpan(9, 15, 0))
			.SetDisplay("Session Start", "Start of custom session", "Session");
		_sessionEnd = Param(nameof(SessionEnd), new TimeSpan(10, 0, 0))
			.SetDisplay("Session End", "End of custom session", "Session");

		_entryStart = Param(nameof(EntryStart), new TimeSpan(9, 15, 0))
			.SetDisplay("Entry Start", "Start time for entries", "Trading");
		_entryEnd = Param(nameof(EntryEnd), new TimeSpan(14, 30, 0))
			.SetDisplay("Entry End", "End time for entries", "Trading");

		_exitStart = Param(nameof(ExitStart), new TimeSpan(14, 45, 0))
			.SetDisplay("Exit Start", "Start time to close all trades", "Trading");
		_exitEnd = Param(nameof(ExitEnd), new TimeSpan(15, 0, 0))
			.SetDisplay("Exit End", "End time to close all trades", "Trading");

		_profitFactor = Param(nameof(ProfitFactor), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Factor", "Risk to reward ratio", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_insideSession = false;
		_sessionHigh = 0m;
		_sessionLow = 0m;
		_extendedHigh = 0m;
		_extendedLow = 0m;
		_tradeExecuted = false;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_targetPrice = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
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

		var t = candle.CloseTime.TimeOfDay;

		var inSession = InTimeRange(t, SessionStart, SessionEnd);
		var inEntry = InTimeRange(t, EntryStart, EntryEnd);
		var inExit = InTimeRange(t, ExitStart, ExitEnd);

		if (!_insideSession && inSession)
		{
			_insideSession = true;
			_sessionHigh = candle.HighPrice;
			_sessionLow = candle.LowPrice;
		}
		else if (_insideSession && inSession)
		{
			_sessionHigh = Math.Max(_sessionHigh, candle.HighPrice);
			_sessionLow = Math.Min(_sessionLow, candle.LowPrice);
		}
		else if (_insideSession && !inSession)
		{
			_insideSession = false;
			_extendedHigh = _sessionHigh;
			_extendedLow = _sessionLow;
		}

		if (inExit)
		{
			if (Position != 0)
				ClosePosition();

			_tradeExecuted = false;
			return;
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _targetPrice)
				ClosePosition();
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _targetPrice)
				ClosePosition();
		}
		else if (!_tradeExecuted && inEntry && _extendedHigh > 0m && _extendedLow > 0m)
		{
			var longSignal = candle.OpenPrice < _extendedHigh && candle.ClosePrice > _extendedHigh;
			var shortSignal = candle.OpenPrice > _extendedLow && candle.ClosePrice < _extendedLow;

			if (longSignal)
			{
				_entryPrice = candle.ClosePrice;
				_stopPrice = candle.LowPrice;
				var risk = _entryPrice - _stopPrice;
				_targetPrice = _entryPrice + risk * ProfitFactor;
				BuyMarket();
				_tradeExecuted = true;
			}
			else if (shortSignal)
			{
				_entryPrice = candle.ClosePrice;
				_stopPrice = candle.HighPrice;
				var risk = _stopPrice - _entryPrice;
				_targetPrice = _entryPrice - risk * ProfitFactor;
				SellMarket();
				_tradeExecuted = true;
			}
		}
	}

	private static bool InTimeRange(TimeSpan time, TimeSpan start, TimeSpan end)
	{
		return start <= end ? time >= start && time <= end : time >= start || time <= end;
	}
}
