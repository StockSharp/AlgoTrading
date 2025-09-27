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
/// Daily breakout strategy converted from the MetaTrader 5 "Daily range" expert advisor.
/// Tracks the highest and lowest prices within a sliding window and trades breakouts with range-based stops.
/// </summary>
public class DailyRangeStrategy : Strategy
{
	private readonly StrategyParam<DailyRangeCalculation> _rangeMode;
	private readonly StrategyParam<int> _slidingWindowDays;
	private readonly StrategyParam<decimal> _stopLossCoefficient;
	private readonly StrategyParam<decimal> _takeProfitCoefficient;
	private readonly StrategyParam<decimal> _offsetCoefficient;
	private readonly StrategyParam<int> _maxPositionsPerDay;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<DataType> _candleType;

	private readonly LinkedList<DayRangeStats> _recentDays = new();

	private DayRangeStats _currentDayStats;
	private bool _rangeCalculatedForDay;
	private decimal? _upperBoundary;
	private decimal? _lowerBoundary;
	private decimal? _dailyRange;
	private int _buyCount;
	private int _sellCount;

	/// <summary>
	/// Calculation method used for the daily range.
	/// </summary>
	public DailyRangeCalculation RangeMode
	{
		get => _rangeMode.Value;
		set => _rangeMode.Value = value;
	}

	/// <summary>
	/// Number of calendar days considered when calculating the range.
	/// </summary>
	public int SlidingWindowDays
	{
		get => _slidingWindowDays.Value;
		set => _slidingWindowDays.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the daily range to calculate the stop-loss distance.
	/// </summary>
	public decimal StopLossCoefficient
	{
		get => _stopLossCoefficient.Value;
		set => _stopLossCoefficient.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the daily range to calculate the take-profit distance.
	/// </summary>
	public decimal TakeProfitCoefficient
	{
		get => _takeProfitCoefficient.Value;
		set => _takeProfitCoefficient.Value = value;
	}

	/// <summary>
	/// Additional offset applied to the breakout levels.
	/// </summary>
	public decimal OffsetCoefficient
	{
		get => _offsetCoefficient.Value;
		set => _offsetCoefficient.Value = value;
	}

	/// <summary>
	/// Maximum number of entries allowed per direction during a single trading day.
	/// </summary>
	public int MaxPositionsPerDay
	{
		get => _maxPositionsPerDay.Value;
		set => _maxPositionsPerDay.Value = value;
	}

	/// <summary>
	/// Time of day when a fresh breakout range should be calculated.
	/// </summary>
	public TimeSpan StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DailyRangeStrategy"/>.
	/// </summary>
	public DailyRangeStrategy()
	{
		_rangeMode = Param(nameof(RangeMode), DailyRangeCalculation.HighestLowest)
			.SetDisplay("Range Mode", "Daily range calculation method", "General")
			.SetCanOptimize(true);

		_slidingWindowDays = Param(nameof(SlidingWindowDays), 3)
			.SetGreaterThanZero()
			.SetDisplay("Sliding Window", "Number of calendar days to analyse", "General")
			.SetCanOptimize(true);

		_stopLossCoefficient = Param(nameof(StopLossCoefficient), 0.03m)
			.SetDisplay("Stop Loss Coeff.", "Stop-loss multiplier applied to the daily range", "Risk Management")
			.SetNotNegative()
			.SetCanOptimize(true);

		_takeProfitCoefficient = Param(nameof(TakeProfitCoefficient), 0.05m)
			.SetDisplay("Take Profit Coeff.", "Take-profit multiplier applied to the daily range", "Risk Management")
			.SetNotNegative()
			.SetCanOptimize(true);

		_offsetCoefficient = Param(nameof(OffsetCoefficient), 0.01m)
			.SetDisplay("Offset Coeff.", "Additional offset applied to breakout levels", "General")
			.SetNotNegative()
			.SetCanOptimize(true);

		_maxPositionsPerDay = Param(nameof(MaxPositionsPerDay), 3)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades Per Day", "Maximum number of entries allowed per direction each day", "Risk Management")
			.SetCanOptimize(true);

		_startTime = Param(nameof(StartTime), new TimeSpan(10, 5, 0))
			.SetDisplay("Start Time", "Time of day when a new range is computed", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for range calculation and trading", "General");
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

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetState();

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenNew(ProcessCandle).Start();

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

		var day = candle.OpenTime.Date;

		if (_currentDayStats == null || _currentDayStats.Date != day)
			StartNewDay(day);

		_currentDayStats.Update(candle);

		if (!_rangeCalculatedForDay && candle.OpenTime.TimeOfDay >= StartTime)
		{
			if (TryCalculateRange(out var highest, out var lowest, out var range))
			{
				_dailyRange = range;
				_upperBoundary = highest + range * OffsetCoefficient;
				_lowerBoundary = lowest - range * OffsetCoefficient;
				_rangeCalculatedForDay = true;
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_dailyRange is not decimal currentRange || _upperBoundary is not decimal upper || _lowerBoundary is not decimal lower)
			return;

		if (TryHandleExits(candle, currentRange))
			return;

		TryHandleEntries(candle, upper, lower);
	}

	private void StartNewDay(DateTime day)
	{
		_currentDayStats = new DayRangeStats(day);
		_recentDays.AddLast(_currentDayStats);

		while (_recentDays.Count > SlidingWindowDays)
			_recentDays.RemoveFirst();

		_rangeCalculatedForDay = false;
		_buyCount = 0;
		_sellCount = 0;
	}

	private void ResetState()
	{
		_recentDays.Clear();
		_currentDayStats = null;
		_rangeCalculatedForDay = false;
		_dailyRange = null;
		_upperBoundary = null;
		_lowerBoundary = null;
		_buyCount = 0;
		_sellCount = 0;
	}

	private bool TryCalculateRange(out decimal highest, out decimal lowest, out decimal range)
	{
		highest = decimal.MinValue;
		lowest = decimal.MaxValue;
		decimal? previousClose = null;
		decimal diffSum = 0m;
		var diffCount = 0;

		foreach (var stats in _recentDays)
		{
			if (!stats.HasData)
				continue;

			if (stats.High > highest)
				highest = stats.High;

			if (stats.Low < lowest)
				lowest = stats.Low;

			if (previousClose is decimal prev)
			{
				diffSum += Math.Abs(stats.LastClose - prev);
				diffCount++;
			}

			previousClose = stats.LastClose;
		}

		if (highest == decimal.MinValue || lowest == decimal.MaxValue)
		{
			range = 0m;
			return false;
		}

		range = RangeMode == DailyRangeCalculation.HighestLowest || diffCount == 0
			? highest - lowest
			: diffSum / diffCount;

		return true;
	}

	private bool TryHandleExits(ICandleMessage candle, decimal currentRange)
	{
		var positionVolume = Math.Abs(Position);
		if (positionVolume == 0m)
			return false;

		var averagePrice = Position.AveragePrice ?? candle.ClosePrice;

		var stopDistance = StopLossCoefficient > 0m ? currentRange * StopLossCoefficient : 0m;
		var takeDistance = TakeProfitCoefficient > 0m ? currentRange * TakeProfitCoefficient : 0m;

		if (Position > 0m)
		{
			var stopPrice = stopDistance > 0m ? averagePrice - stopDistance : (decimal?)null;
			var takePrice = takeDistance > 0m ? averagePrice + takeDistance : (decimal?)null;

			var stopHit = stopPrice.HasValue && candle.LowPrice <= stopPrice.Value;
			var takeHit = takePrice.HasValue && candle.HighPrice >= takePrice.Value;

			if (stopHit || takeHit)
			{
				SellMarket(positionVolume);
				return true;
			}
		}
		else
		{
			var stopPrice = stopDistance > 0m ? averagePrice + stopDistance : (decimal?)null;
			var takePrice = takeDistance > 0m ? averagePrice - takeDistance : (decimal?)null;

			var stopHit = stopPrice.HasValue && candle.HighPrice >= stopPrice.Value;
			var takeHit = takePrice.HasValue && candle.LowPrice <= takePrice.Value;

			if (stopHit || takeHit)
			{
				BuyMarket(positionVolume);
				return true;
			}
		}

		return false;
	}

	private void TryHandleEntries(ICandleMessage candle, decimal upper, decimal lower)
	{
		if (candle.ClosePrice >= upper && _buyCount < MaxPositionsPerDay)
		{
			var volume = Volume;
			if (Position < 0m)
				volume += Math.Abs(Position);

			if (volume > 0m)
			{
				BuyMarket(volume);
				_buyCount++;
			}
		}
		else if (candle.ClosePrice <= lower && _sellCount < MaxPositionsPerDay)
		{
			var volume = Volume;
			if (Position > 0m)
				volume += Math.Abs(Position);

			if (volume > 0m)
			{
				SellMarket(volume);
				_sellCount++;
			}
		}
	}

	/// <summary>
	/// Range calculation modes.
	/// </summary>
	public enum DailyRangeCalculation
	{
		/// <summary>
		/// Use the distance between the highest high and the lowest low within the window.
		/// </summary>
		HighestLowest,

		/// <summary>
		/// Use the average absolute change between consecutive daily closing prices.
		/// </summary>
		CloseToClose
	}

	private sealed class DayRangeStats
	{
		public DayRangeStats(DateTime date)
		{
			Date = date;
		}

		public DateTime Date { get; }
		public decimal High { get; private set; } = decimal.MinValue;
		public decimal Low { get; private set; } = decimal.MaxValue;
		public decimal LastClose { get; private set; }
		public bool HasData { get; private set; }

		public void Update(ICandleMessage candle)
		{
			if (!HasData)
			{
				High = candle.HighPrice;
				Low = candle.LowPrice;
				HasData = true;
			}
			else
			{
				if (candle.HighPrice > High)
					High = candle.HighPrice;

				if (candle.LowPrice < Low)
					Low = candle.LowPrice;
			}

			LastClose = candle.ClosePrice;
		}
	}
}

