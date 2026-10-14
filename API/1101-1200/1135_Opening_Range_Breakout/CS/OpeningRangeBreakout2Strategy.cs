using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Opening range breakout strategy.
/// The high and low of the candles between RangeStart and RangeEnd form the opening range. After it closes, and only if its width
/// exceeds MinRangePercent of the close, a break above the high goes long and a break below the low goes short. The stop sits
/// Retrace times the range from the breakout level and the target RewardRisk times that distance. Optionally only one trade per
/// day is taken and a stopped-out trade reverses. Everything is flat at DayEnd.
/// </summary>
public class OpeningRangeBreakout2Strategy : Strategy
{
	private readonly StrategyParam<TimeSpan> _rangeStart;
	private readonly StrategyParam<TimeSpan> _rangeEnd;
	private readonly StrategyParam<TimeSpan> _dayEnd;
	private readonly StrategyParam<decimal> _minRangePercent;
	private readonly StrategyParam<decimal> _rewardRisk;
	private readonly StrategyParam<decimal> _retrace;
	private readonly StrategyParam<bool> _oneTradePerDay;
	private readonly StrategyParam<bool> _reverseOnLoss;
	private readonly StrategyParam<DataType> _candleType;

	private DateTime _currentDay;
	private decimal? _rangeHigh;
	private decimal? _rangeLow;
	private bool _rangeValid;
	private bool _tradedToday;
	private bool _reversedToday;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Opening range start time (UTC).
	/// </summary>
	public TimeSpan RangeStart
	{
		get => _rangeStart.Value;
		set => _rangeStart.Value = value;
	}

	/// <summary>
	/// Opening range end time (UTC).
	/// </summary>
	public TimeSpan RangeEnd
	{
		get => _rangeEnd.Value;
		set => _rangeEnd.Value = value;
	}

	/// <summary>
	/// Time when all positions are closed (UTC).
	/// </summary>
	public TimeSpan DayEnd
	{
		get => _dayEnd.Value;
		set => _dayEnd.Value = value;
	}

	/// <summary>
	/// Minimum range width as a percent of the close.
	/// </summary>
	public decimal MinRangePercent
	{
		get => _minRangePercent.Value;
		set => _minRangePercent.Value = value;
	}

	/// <summary>
	/// Target distance in multiples of the stop distance.
	/// </summary>
	public decimal RewardRisk
	{
		get => _rewardRisk.Value;
		set => _rewardRisk.Value = value;
	}

	/// <summary>
	/// Stop distance as a fraction of the range width.
	/// </summary>
	public decimal Retrace
	{
		get => _retrace.Value;
		set => _retrace.Value = value;
	}

	/// <summary>
	/// Take at most one breakout per day.
	/// </summary>
	public bool OneTradePerDay
	{
		get => _oneTradePerDay.Value;
		set => _oneTradePerDay.Value = value;
	}

	/// <summary>
	/// Reverse the position when the stop is hit.
	/// </summary>
	public bool ReverseOnLoss
	{
		get => _reverseOnLoss.Value;
		set => _reverseOnLoss.Value = value;
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
	public OpeningRangeBreakout2Strategy()
	{
		_rangeStart = Param(nameof(RangeStart), new TimeSpan(9, 30, 0))
			.SetDisplay("Range Start", "Opening range start time (UTC)", "Session");

		_rangeEnd = Param(nameof(RangeEnd), new TimeSpan(10, 15, 0))
			.SetDisplay("Range End", "Opening range end time (UTC)", "Session");

		_dayEnd = Param(nameof(DayEnd), new TimeSpan(15, 45, 0))
			.SetDisplay("Day End", "Time when all positions are closed (UTC)", "Session");

		_minRangePercent = Param(nameof(MinRangePercent), 0.35m)
			.SetNotNegative()
			.SetDisplay("Min Range %", "Minimum range width as percent of close", "Trading");

		_rewardRisk = Param(nameof(RewardRisk), 1.1m)
			.SetGreaterThanZero()
			.SetDisplay("Reward/Risk", "Target distance in multiples of the stop distance", "Risk");

		_retrace = Param(nameof(Retrace), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Retrace", "Stop distance as a fraction of the range width", "Risk");

		_oneTradePerDay = Param(nameof(OneTradePerDay), true)
			.SetDisplay("One Trade Per Day", "Take at most one breakout per day", "Trading");

		_reverseOnLoss = Param(nameof(ReverseOnLoss), false)
			.SetDisplay("Reverse On Loss", "Reverse the position when the stop is hit", "Trading");

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
		_currentDay = default;
		ResetDay();
	}

	private void ResetDay()
	{
		_rangeHigh = null;
		_rangeLow = null;
		_rangeValid = false;
		_tradedToday = false;
		_reversedToday = false;
		_stopPrice = 0m;
		_takePrice = 0m;
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

		var openTime = candle.OpenTime.ToUniversalTime();
		var day = openTime.Date;
		var tod = openTime.TimeOfDay;

		if (day != _currentDay)
		{
			_currentDay = day;
			ResetDay();
		}

		if (tod >= RangeStart && tod < RangeEnd)
		{
			_rangeHigh = _rangeHigh is decimal h ? Math.Max(h, candle.HighPrice) : candle.HighPrice;
			_rangeLow = _rangeLow is decimal l ? Math.Min(l, candle.LowPrice) : candle.LowPrice;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (tod >= DayEnd)
		{
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(-Position);

			return;
		}

		if (tod < RangeEnd || _rangeHigh is not decimal high || _rangeLow is not decimal low)
			return;

		var range = high - low;

		if (!_rangeValid)
		{
			// The width filter is checked once, on the first candle after the window closes.
			if (range <= 0 || range < candle.ClosePrice * MinRangePercent / 100m)
			{
				_rangeHigh = null;
				_rangeLow = null;
				return;
			}

			_rangeValid = true;
		}

		var stopDistance = range * Retrace;

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice)
			{
				if (ReverseOnLoss && !_reversedToday)
				{
					SellMarket(Position + Volume);
					SetLevels(_stopPrice, stopDistance, false);
					_reversedToday = true;
				}
				else
				{
					SellMarket(Position);
				}
			}
			else if (candle.HighPrice >= _takePrice)
			{
				SellMarket(Position);
			}

			return;
		}

		if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice)
			{
				if (ReverseOnLoss && !_reversedToday)
				{
					BuyMarket(-Position + Volume);
					SetLevels(_stopPrice, stopDistance, true);
					_reversedToday = true;
				}
				else
				{
					BuyMarket(-Position);
				}
			}
			else if (candle.LowPrice <= _takePrice)
			{
				BuyMarket(-Position);
			}

			return;
		}

		if (OneTradePerDay && _tradedToday)
			return;

		var breakUp = candle.HighPrice > high;
		var breakDown = candle.LowPrice < low;

		// A candle through both boundaries gives no clear direction.
		if (breakUp && !breakDown)
		{
			BuyMarket(Volume);
			SetLevels(high, stopDistance, true);
			_tradedToday = true;
		}
		else if (breakDown && !breakUp)
		{
			SellMarket(Volume);
			SetLevels(low, stopDistance, false);
			_tradedToday = true;
		}
	}

	private void SetLevels(decimal entry, decimal stopDistance, bool isLong)
	{
		var takeDistance = stopDistance * RewardRisk;

		_stopPrice = isLong ? entry - stopDistance : entry + stopDistance;
		_takePrice = isLong ? entry + takeDistance : entry - takeDistance;
	}
}
