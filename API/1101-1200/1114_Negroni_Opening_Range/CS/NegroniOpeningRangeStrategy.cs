using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Negroni opening range strategy.
/// Each day the high and low of the pre-market window (or of the opening range window when UsePreMarketRange is off) form the range.
/// Inside the trading session a close above the range high goes long and a close below the range low goes short, within the allowed
/// Direction and at most MaxTradesPerDay entries a day. Any open position is closed at CloseTime. Times are UTC candle open times.
/// </summary>
public class NegroniOpeningRangeStrategy : Strategy
{
	/// <summary>
	/// Allowed trade directions.
	/// </summary>
	public enum TradeDirection
	{
		/// <summary>
		/// Long entries only.
		/// </summary>
		LongOnly,

		/// <summary>
		/// Short entries only.
		/// </summary>
		ShortOnly,

		/// <summary>
		/// Long and short entries.
		/// </summary>
		LongShort,
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maxTradesPerDay;
	private readonly StrategyParam<TradeDirection> _direction;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionEnd;
	private readonly StrategyParam<TimeSpan> _closeTime;
	private readonly StrategyParam<bool> _usePreMarketRange;
	private readonly StrategyParam<TimeSpan> _preMarketStart;
	private readonly StrategyParam<TimeSpan> _preMarketEnd;
	private readonly StrategyParam<TimeSpan> _openRangeStart;
	private readonly StrategyParam<TimeSpan> _openRangeEnd;

	private DateTime _currentDate;
	private decimal? _rangeHigh;
	private decimal? _rangeLow;
	private int _tradesToday;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Maximum entries per day.
	/// </summary>
	public int MaxTradesPerDay
	{
		get => _maxTradesPerDay.Value;
		set => _maxTradesPerDay.Value = value;
	}

	/// <summary>
	/// Allowed trade direction.
	/// </summary>
	public TradeDirection Direction
	{
		get => _direction.Value;
		set => _direction.Value = value;
	}

	/// <summary>
	/// Start of the trading session.
	/// </summary>
	public TimeSpan SessionStart
	{
		get => _sessionStart.Value;
		set => _sessionStart.Value = value;
	}

	/// <summary>
	/// End of the trading session.
	/// </summary>
	public TimeSpan SessionEnd
	{
		get => _sessionEnd.Value;
		set => _sessionEnd.Value = value;
	}

	/// <summary>
	/// Time when any open position is closed.
	/// </summary>
	public TimeSpan CloseTime
	{
		get => _closeTime.Value;
		set => _closeTime.Value = value;
	}

	/// <summary>
	/// Use the pre-market range instead of the opening range.
	/// </summary>
	public bool UsePreMarketRange
	{
		get => _usePreMarketRange.Value;
		set => _usePreMarketRange.Value = value;
	}

	/// <summary>
	/// Start of the pre-market window.
	/// </summary>
	public TimeSpan PreMarketStart
	{
		get => _preMarketStart.Value;
		set => _preMarketStart.Value = value;
	}

	/// <summary>
	/// End of the pre-market window.
	/// </summary>
	public TimeSpan PreMarketEnd
	{
		get => _preMarketEnd.Value;
		set => _preMarketEnd.Value = value;
	}

	/// <summary>
	/// Start of the opening range window.
	/// </summary>
	public TimeSpan OpenRangeStart
	{
		get => _openRangeStart.Value;
		set => _openRangeStart.Value = value;
	}

	/// <summary>
	/// End of the opening range window.
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
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_maxTradesPerDay = Param(nameof(MaxTradesPerDay), 3)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades Per Day", "Maximum entries per day", "Trading");

		_direction = Param(nameof(Direction), TradeDirection.LongShort)
			.SetDisplay("Direction", "Allowed trade direction", "Trading");

		_sessionStart = Param(nameof(SessionStart), new TimeSpan(9, 30, 0))
			.SetDisplay("Session Start", "Start of the trading session", "Session");

		_sessionEnd = Param(nameof(SessionEnd), new TimeSpan(14, 0, 0))
			.SetDisplay("Session End", "End of the trading session", "Session");

		_closeTime = Param(nameof(CloseTime), new TimeSpan(16, 0, 0))
			.SetDisplay("Close Time", "Time when any open position is closed", "Session");

		_usePreMarketRange = Param(nameof(UsePreMarketRange), true)
			.SetDisplay("Use Pre-Market Range", "Use the pre-market range instead of the opening range", "Range");

		_preMarketStart = Param(nameof(PreMarketStart), new TimeSpan(8, 0, 0))
			.SetDisplay("Pre-Market Start", "Start of the pre-market window", "Range");

		_preMarketEnd = Param(nameof(PreMarketEnd), new TimeSpan(9, 0, 0))
			.SetDisplay("Pre-Market End", "End of the pre-market window", "Range");

		_openRangeStart = Param(nameof(OpenRangeStart), new TimeSpan(9, 5, 0))
			.SetDisplay("Open Range Start", "Start of the opening range window", "Range");

		_openRangeEnd = Param(nameof(OpenRangeEnd), new TimeSpan(9, 30, 0))
			.SetDisplay("Open Range End", "End of the opening range window", "Range");
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
		ResetState();
	}

	private void ResetState()
	{
		_currentDate = default;
		_rangeHigh = null;
		_rangeLow = null;
		_tradesToday = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

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

		var date = candle.OpenTime.Date;
		if (date != _currentDate)
		{
			_currentDate = date;
			_rangeHigh = null;
			_rangeLow = null;
			_tradesToday = 0;
		}

		var tod = candle.OpenTime.TimeOfDay;
		var rangeStart = UsePreMarketRange ? PreMarketStart : OpenRangeStart;
		var rangeEnd = UsePreMarketRange ? PreMarketEnd : OpenRangeEnd;

		if (tod >= rangeStart && tod < rangeEnd)
		{
			_rangeHigh = _rangeHigh is decimal high ? Math.Max(high, candle.HighPrice) : candle.HighPrice;
			_rangeLow = _rangeLow is decimal low ? Math.Min(low, candle.LowPrice) : candle.LowPrice;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (tod >= CloseTime)
		{
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(-Position);
			return;
		}

		if (tod < rangeEnd || tod < SessionStart || tod >= SessionEnd)
			return;

		if (_rangeHigh is not decimal rangeHigh || _rangeLow is not decimal rangeLow || _tradesToday >= MaxTradesPerDay)
			return;

		var close = candle.ClosePrice;
		var allowLong = Direction != TradeDirection.ShortOnly;
		var allowShort = Direction != TradeDirection.LongOnly;

		if (allowLong && close > rangeHigh && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_tradesToday++;
		}
		else if (allowShort && close < rangeLow && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_tradesToday++;
		}
	}
}
