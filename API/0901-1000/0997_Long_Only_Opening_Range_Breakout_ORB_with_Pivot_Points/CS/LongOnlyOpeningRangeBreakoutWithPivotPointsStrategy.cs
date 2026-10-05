using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long-only opening range breakout with daily pivot points.
/// The opening range is the high of the first RangeMinutes after SessionStart (UTC). After the range closes, a close above the
/// range high goes long when the R1 pivot of the previous day sits above that high, at most MaxTradesPerDay times a day.
/// The initial stop is a percentage below entry or the previous candle low. It trails up to the pivot P, R1 and R2 when price reaches
/// R1, R2 and R3, and at every daily close it is raised to StopLossPercent below that close.
/// </summary>
public class LongOnlyOpeningRangeBreakoutWithPivotPointsStrategy : Strategy
{
	/// <summary>
	/// Initial stop loss types.
	/// </summary>
	public enum SlTypes
	{
		/// <summary>
		/// Percentage below the entry price.
		/// </summary>
		Percentage,

		/// <summary>
		/// Low of the candle before the entry candle.
		/// </summary>
		PreviousLow
	}

	private readonly StrategyParam<int> _rangeMinutes;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<int> _maxTradesPerDay;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<SlTypes> _initialSlType;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime? _currentDay;
	private decimal _dayHigh;
	private decimal _dayLow;
	private decimal _dayClose;
	private decimal? _rangeHigh;
	private int _tradesToday;
	private decimal? _pivot;
	private decimal _r1;
	private decimal _r2;
	private decimal _r3;
	private decimal? _prevLow;
	private decimal? _stopPrice;

	/// <summary>
	/// Length of the opening range in minutes.
	/// </summary>
	public int RangeMinutes
	{
		get => _rangeMinutes.Value;
		set => _rangeMinutes.Value = value;
	}

	/// <summary>
	/// Session start time (UTC).
	/// </summary>
	public TimeSpan SessionStart
	{
		get => _sessionStart.Value;
		set => _sessionStart.Value = value;
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
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Initial stop loss type.
	/// </summary>
	public SlTypes InitialSlType
	{
		get => _initialSlType.Value;
		set => _initialSlType.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public LongOnlyOpeningRangeBreakoutWithPivotPointsStrategy()
	{
		_rangeMinutes = Param(nameof(RangeMinutes), 15)
			.SetGreaterThanZero()
			.SetDisplay("Range Minutes", "Length of the opening range in minutes", "Session");

		_sessionStart = Param(nameof(SessionStart), new TimeSpan(9, 30, 0))
			.SetDisplay("Session Start", "Session start time (UTC)", "Session");

		_maxTradesPerDay = Param(nameof(MaxTradesPerDay), 1)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades Per Day", "Maximum entries per day", "Session");

		_stopLossPercent = Param(nameof(StopLossPercent), 3m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_initialSlType = Param(nameof(InitialSlType), SlTypes.Percentage)
			.SetDisplay("Initial SL Type", "Initial stop loss type", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_currentDay = null;
		_dayHigh = 0m;
		_dayLow = 0m;
		_dayClose = 0m;
		_rangeHigh = null;
		_tradesToday = 0;
		_pivot = null;
		_r1 = 0m;
		_r2 = 0m;
		_r3 = 0m;
		_prevLow = null;
		_stopPrice = null;
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

		var openTime = candle.OpenTime;
		var day = openTime.Date;

		if (_currentDay != day)
		{
			if (_currentDay != null)
				StartNewDay();

			_currentDay = day;
			_dayHigh = candle.HighPrice;
			_dayLow = candle.LowPrice;
		}
		else
		{
			_dayHigh = Math.Max(_dayHigh, candle.HighPrice);
			_dayLow = Math.Min(_dayLow, candle.LowPrice);
		}

		_dayClose = candle.ClosePrice;

		var prevLow = _prevLow;
		_prevLow = candle.LowPrice;

		var timeOfDay = openTime.TimeOfDay;
		var rangeEnd = SessionStart + TimeSpan.FromMinutes(RangeMinutes);

		if (timeOfDay >= SessionStart && timeOfDay < rangeEnd)
		{
			_rangeHigh = _rangeHigh is decimal h ? Math.Max(h, candle.HighPrice) : candle.HighPrice;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			if (_pivot is decimal pivot && _stopPrice is decimal current)
			{
				var level = current;

				if (candle.HighPrice > _r3)
					level = _r2;
				else if (candle.HighPrice > _r2)
					level = _r1;
				else if (candle.HighPrice > _r1)
					level = pivot;

				_stopPrice = Math.Max(current, level);
			}

			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				_stopPrice = null;
			}

			return;
		}

		if (Position != 0 || timeOfDay < rangeEnd || _tradesToday >= MaxTradesPerDay)
			return;

		if (_rangeHigh is not decimal rangeHigh || _pivot is null)
			return;

		if (candle.ClosePrice <= rangeHigh || _r1 <= rangeHigh)
			return;

		BuyMarket(Volume);
		_tradesToday++;

		_stopPrice = InitialSlType == SlTypes.PreviousLow && prevLow is decimal low
			? low
			: candle.ClosePrice * (1m - StopLossPercent / 100m);
	}

	private void StartNewDay()
	{
		var pivot = (_dayHigh + _dayLow + _dayClose) / 3m;
		_pivot = pivot;
		_r1 = 2m * pivot - _dayLow;
		_r2 = pivot + (_dayHigh - _dayLow);
		_r3 = _dayHigh + 2m * (pivot - _dayLow);

		// The stop also trails the daily close.
		if (Position > 0 && _stopPrice is decimal stop)
			_stopPrice = Math.Max(stop, _dayClose * (1m - StopLossPercent / 100m));

		_rangeHigh = null;
		_tradesToday = 0;
	}
}
