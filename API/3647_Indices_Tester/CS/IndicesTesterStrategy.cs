using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert advisor "Indices Tester".
/// Implements a time filtered long-only session with daily trade and position limits.
/// </summary>
public class IndicesTesterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionEnd;
	private readonly StrategyParam<TimeSpan> _closeTime;
	private readonly StrategyParam<int> _dailyTradeLimit;
	private readonly StrategyParam<int> _maxOpenPositions;
	private readonly StrategyParam<decimal> _tradeVolume;

	private DateTime _currentDay;
	private int _tradesOpenedToday;

	/// <summary>
	/// Candle type used to drive the strategy clock.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Session start time when new long positions may be opened.
	/// </summary>
	public TimeSpan SessionStart
	{
		get => _sessionStart.Value;
		set => _sessionStart.Value = value;
	}

	/// <summary>
	/// Session end time after which new positions are not allowed.
	/// </summary>
	public TimeSpan SessionEnd
	{
		get => _sessionEnd.Value;
		set => _sessionEnd.Value = value;
	}

	/// <summary>
	/// Time of day when all active positions are closed.
	/// </summary>
	public TimeSpan CloseTime
	{
		get => _closeTime.Value;
		set => _closeTime.Value = value;
	}

	/// <summary>
	/// Maximum number of entries that can be opened during a single trading day.
	/// </summary>
	public int DailyTradeLimit
	{
		get => _dailyTradeLimit.Value;
		set => _dailyTradeLimit.Value = value;
	}

	/// <summary>
	/// Maximum simultaneous long positions measured in trade units.
	/// </summary>
	public int MaxOpenPositions
	{
		get => _maxOpenPositions.Value;
		set => _maxOpenPositions.Value = value;
	}

	/// <summary>
	/// Order volume submitted with every market entry.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="IndicesTesterStrategy"/> class.
	/// </summary>
	public IndicesTesterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe that drives the logic", "General");

		_sessionStart = Param(nameof(SessionStart), new TimeSpan(1, 30, 0))
			.SetDisplay("Session Start", "Time of day when entries become eligible", "Trading");

		_sessionEnd = Param(nameof(SessionEnd), new TimeSpan(1, 35, 0))
			.SetDisplay("Session End", "Time of day when new entries stop", "Trading");

		_closeTime = Param(nameof(CloseTime), new TimeSpan(23, 30, 0))
			.SetDisplay("Close Time", "Time of day used to liquidate open positions", "Risk");

		_dailyTradeLimit = Param(nameof(DailyTradeLimit), 1)
			.SetGreaterThanZero()
			.SetDisplay("Daily Trades", "Maximum number of trades per day", "Risk");

		_maxOpenPositions = Param(nameof(MaxOpenPositions), 1)
			.SetGreaterThanZero()
			.SetDisplay("Open Positions", "Maximum simultaneous long positions", "Risk");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Market order volume for new positions", "Trading");
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

		_currentDay = default;
		_tradesOpenedToday = 0;
	}

	/// <inheritdoc />
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
		// Ignore unfinished candles because the original EA worked on closed data.
		if (candle.State != CandleStates.Finished)
			return;

		// Abort early when infrastructure or trading permissions are not ready.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var candleTime = candle.CloseTime;
		if (_currentDay != candleTime.Date)
		{
			// Reset the intraday counters on the first candle of a new session.
			_currentDay = candleTime.Date;
			_tradesOpenedToday = 0;
		}

		var timeOfDay = candleTime.TimeOfDay;

		// Liquidate open positions once the configured close time is reached.
		if (Position > 0m && timeOfDay >= CloseTime)
		{
			SellMarket(Position);
			return;
		}

		// Only evaluate entries strictly inside the trading window.
		if (timeOfDay <= SessionStart || timeOfDay >= SessionEnd)
			return;

		// Respect the daily trade allowance taken from the original EA.
		if (_tradesOpenedToday >= DailyTradeLimit)
			return;

		// Skip entries when the simultaneous position limit would be exceeded.
		if (GetOpenPositionCount() >= MaxOpenPositions)
			return;

		var volume = TradeVolume;
		if (volume <= 0m)
			return;

		// Submit the market order and immediately update the per-day trade counter.
		BuyMarket(volume);
		_tradesOpenedToday++;
	}

	private int GetOpenPositionCount()
	{
		if (Position == 0m)
			return 0;

		var volume = TradeVolume;
		if (volume <= 0m)
			return 1;

		return (int)Math.Ceiling(Math.Abs(Position) / volume);
	}
}
