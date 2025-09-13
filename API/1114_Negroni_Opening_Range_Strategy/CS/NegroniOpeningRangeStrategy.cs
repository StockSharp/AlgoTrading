using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Negroni opening range breakout strategy.
/// Trades breakouts based on pre-market or opening range with time and direction filters.
/// </summary>
public class NegroniOpeningRangeStrategy : Strategy
{
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<int> _maxTradesPerDay;
private readonly StrategyParam<Sides?> _direction;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionEnd;
	private readonly StrategyParam<TimeSpan> _closeTime;
	private readonly StrategyParam<bool> _usePreMarketRange;
	private readonly StrategyParam<TimeSpan> _preMarketStart;
	private readonly StrategyParam<TimeSpan> _preMarketEnd;
	private readonly StrategyParam<TimeSpan> _openRangeStart;
	private readonly StrategyParam<TimeSpan> _openRangeEnd;

	private int _tradesToday;
	private DateTime _currentDay;
	private decimal _prevClose;
	private decimal? _rangeHigh1;
	private decimal? _rangeLow1;
	private decimal? _rangeHigh2;
	private decimal? _rangeLow2;
	private bool _range1Completed;
	private bool _range2Completed;

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Maximum trades allowed per day.
	/// </summary>
	public int MaxTradesPerDay
	{
		get => _maxTradesPerDay.Value;
		set => _maxTradesPerDay.Value = value;
	}

	/// <summary>
	/// Trading direction.
	/// </summary>
public Sides? Direction
{
	get => _direction.Value;
	set => _direction.Value = value;
}

	/// <summary>
	/// Start of trading session (UTC).
	/// </summary>
	public TimeSpan SessionStart
	{
		get => _sessionStart.Value;
		set => _sessionStart.Value = value;
	}

	/// <summary>
	/// End of trading session (UTC).
	/// </summary>
	public TimeSpan SessionEnd
	{
		get => _sessionEnd.Value;
		set => _sessionEnd.Value = value;
	}

	/// <summary>
	/// Time to close all positions (UTC).
	/// </summary>
	public TimeSpan CloseTime
	{
		get => _closeTime.Value;
		set => _closeTime.Value = value;
	}

	/// <summary>
	/// Use pre-market range instead of opening range.
	/// </summary>
	public bool UsePreMarketRange
	{
		get => _usePreMarketRange.Value;
		set => _usePreMarketRange.Value = value;
	}

	/// <summary>
	/// Start of pre-market range (UTC).
	/// </summary>
	public TimeSpan PreMarketStart
	{
		get => _preMarketStart.Value;
		set => _preMarketStart.Value = value;
	}

	/// <summary>
	/// End of pre-market range (UTC).
	/// </summary>
	public TimeSpan PreMarketEnd
	{
		get => _preMarketEnd.Value;
		set => _preMarketEnd.Value = value;
	}

	/// <summary>
	/// Start of opening range (UTC).
	/// </summary>
	public TimeSpan OpenRangeStart
	{
		get => _openRangeStart.Value;
		set => _openRangeStart.Value = value;
	}

	/// <summary>
	/// End of opening range (UTC).
	/// </summary>
	public TimeSpan OpenRangeEnd
	{
		get => _openRangeEnd.Value;
		set => _openRangeEnd.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public NegroniOpeningRangeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_maxTradesPerDay = Param(nameof(MaxTradesPerDay), 3)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum trades per day", "General");
	 _direction = Param(nameof(Direction), null)
	        .SetDisplay("Direction", "Trading direction", "General");

		_sessionStart = Param(nameof(SessionStart), new TimeSpan(9, 30, 0))
			.SetDisplay("Session Start", "Start time for opening trades", "Time");

		_sessionEnd = Param(nameof(SessionEnd), new TimeSpan(14, 0, 0))
			.SetDisplay("Session End", "End time for opening trades", "Time");

		_closeTime = Param(nameof(CloseTime), new TimeSpan(16, 0, 0))
			.SetDisplay("Close Time", "Time to close positions", "Time");

		_usePreMarketRange = Param(nameof(UsePreMarketRange), true)
			.SetDisplay("Use PreMarket", "Use pre-market range for breakouts", "Range");

		_preMarketStart = Param(nameof(PreMarketStart), new TimeSpan(8, 0, 0))
			.SetDisplay("PreMarket Start", "Start of pre-market range", "Range");

		_preMarketEnd = Param(nameof(PreMarketEnd), new TimeSpan(9, 0, 0))
			.SetDisplay("PreMarket End", "End of pre-market range", "Range");

		_openRangeStart = Param(nameof(OpenRangeStart), new TimeSpan(9, 5, 0))
			.SetDisplay("Open Range Start", "Start of opening range", "Range");

		_openRangeEnd = Param(nameof(OpenRangeEnd), new TimeSpan(9, 30, 0))
			.SetDisplay("Open Range End", "End of opening range", "Range");
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
		_tradesToday = 0;
		_currentDay = default;
		_prevClose = 0m;
		_rangeHigh1 = _rangeLow1 = _rangeHigh2 = _rangeLow2 = null;
		_range1Completed = _range2Completed = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_currentDay = time.Date;

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

		var openTime = candle.OpenTime;
		var time = openTime.TimeOfDay;

		if (openTime.Date != _currentDay)
		{
			_currentDay = openTime.Date;
			_tradesToday = 0;
			_prevClose = 0m;
			_rangeHigh1 = _rangeLow1 = _rangeHigh2 = _rangeLow2 = null;
			_range1Completed = _range2Completed = false;
		}

		if (time >= PreMarketStart && time < PreMarketEnd)
		{
			_rangeHigh2 = _rangeHigh2.HasValue ? Math.Max(_rangeHigh2.Value, candle.HighPrice) : candle.HighPrice;
			_rangeLow2 = _rangeLow2.HasValue ? Math.Min(_rangeLow2.Value, candle.LowPrice) : candle.LowPrice;
		}
		else if (time >= PreMarketEnd && !_range2Completed)
		{
			_range2Completed = true;
		}

		if (time >= OpenRangeStart && time < OpenRangeEnd)
		{
			_rangeHigh1 = _rangeHigh1.HasValue ? Math.Max(_rangeHigh1.Value, candle.HighPrice) : candle.HighPrice;
			_rangeLow1 = _rangeLow1.HasValue ? Math.Min(_rangeLow1.Value, candle.LowPrice) : candle.LowPrice;
		}
		else if (time >= OpenRangeEnd && !_range1Completed)
		{
			_range1Completed = true;
		}

		if (time >= CloseTime && Position != 0)
			ClosePosition();

		var previousClose = _prevClose;
		_prevClose = candle.ClosePrice;

		if (previousClose == 0m)
			return;

		var inTradeTime = time >= SessionStart && time <= SessionEnd;
		if (!inTradeTime || _tradesToday >= MaxTradesPerDay)
			return;

		decimal? high = UsePreMarketRange ? _rangeHigh2 : _rangeHigh1;
		decimal? low = UsePreMarketRange ? _rangeLow2 : _rangeLow1;
		bool rangeReady = UsePreMarketRange ? _range2Completed : _range1Completed;

		if (!rangeReady || !high.HasValue || !low.HasValue)
			return;

		var close = candle.ClosePrice;
	 var allowLong = Direction is null || Direction == Sides.Buy;
	var allowShort = Direction is null || Direction == Sides.Sell;
	 if (allowLong &&
	        Position <= 0 && previousClose <= high.Value && close > high.Value)
	{
	        BuyMarket(Volume + Math.Abs(Position));
	        _tradesToday++;
	}
	else if (allowShort &&
	        Position >= 0 && previousClose >= low.Value && close < low.Value)
	{
	        SellMarket(Volume + Math.Abs(Position));
	        _tradesToday++;
	}
	}
}
