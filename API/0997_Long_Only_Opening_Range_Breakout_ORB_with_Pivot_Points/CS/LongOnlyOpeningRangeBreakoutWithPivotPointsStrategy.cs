namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public enum SlType
{
	Percentage,
	PreviousLow
}

/// <summary>
/// Long-only opening range breakout with pivot point trailing stop.
/// </summary>
public class LongOnlyOpeningRangeBreakoutWithPivotPointsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<int> _rangeMinutes;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<SlType> _initialSlType;

	private DateTimeOffset _sessionStartTime;
	private DateTimeOffset _sessionEndTime;
	private decimal? _openingHigh;
	private int _tradesCounter;
	private decimal _entryPrice;
	private decimal _slLong0;
	private decimal _trailLong;
	private decimal _slLong;
	private decimal _prevLow;

	private decimal _r1;
	private decimal _r2;
	private decimal _r3;
	private decimal _r4;
	private decimal _r5;
	private decimal _r0_5;
	private decimal _r1_5;
	private decimal _r2_5;
	private decimal _r3_5;
	private decimal _r4_5;

	/// <summary>
	/// Initializes a new instance of the <see cref="LongOnlyOpeningRangeBreakoutWithPivotPointsStrategy"/> class.
	/// </summary>
	public LongOnlyOpeningRangeBreakoutWithPivotPointsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Working candle type", "General");

		_sessionStart = Param(nameof(SessionStart), new TimeSpan(9, 30, 0))
		.SetDisplay("Session Start", "Opening session start (UTC)", "General");

		_rangeMinutes = Param(nameof(RangeMinutes), 15)
		.SetGreaterThanZero()
		.SetDisplay("Range Minutes", "Opening range duration", "General");

		_maxTrades = Param(nameof(MaxTradesPerDay), 1)
		.SetDisplay("Max Trades", "Maximum trades per day", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 3m)
		.SetDisplay("Stop Loss %", "Initial stop loss percent", "Risk");

		_initialSlType = Param(nameof(InitialSlType), SlType.Percentage)
		.SetDisplay("Initial SL Type", "Initial stop loss type", "Risk");
	}

	/// <summary>
	/// Working candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Opening session start time.
	/// </summary>
	public TimeSpan SessionStart
	{
		get => _sessionStart.Value;
		set => _sessionStart.Value = value;
	}

	/// <summary>
	/// Opening range duration in minutes.
	/// </summary>
	public int RangeMinutes
	{
		get => _rangeMinutes.Value;
		set => _rangeMinutes.Value = value;
	}

	/// <summary>
	/// Maximum trades per day.
	/// </summary>
	public int MaxTradesPerDay
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Initial stop loss percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Initial stop loss type.
	/// </summary>
	public SlType InitialSlType
	{
		get => _initialSlType.Value;
		set => _initialSlType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, TimeSpan.FromDays(1).TimeFrame())];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_openingHigh = null;
		_tradesCounter = 0;
		_trailLong = 0m;
		_slLong = 0m;
		_entryPrice = 0m;
		_prevLow = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sessionStartTime = GetNextSessionStart(time);
		_sessionEndTime = _sessionStartTime.AddMinutes(RangeMinutes);

		SubscribeCandles(CandleType).Bind(ProcessCandle).Start();
		SubscribeCandles(TimeSpan.FromDays(1).TimeFrame()).Bind(ProcessDaily).Start();
	}

	private DateTimeOffset GetNextSessionStart(DateTimeOffset time)
	{
		var start = new DateTimeOffset(time.Date + SessionStart, time.Offset);
		return time <= start ? start : start.AddDays(1);
	}

	private void ProcessDaily(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;

		var pivot = (high + low + close) / 3m;
		var range = high - low;

		_r1 = pivot + pivot - low;
		_r2 = pivot + range;
		_r3 = pivot * 2m + high - 2m * low;
		_r4 = pivot * 3m + high - 3m * low;
		_r5 = pivot * 4m + high - 4m * low;

		_r1_5 = _r1 + Math.Abs(_r1 - _r2) / 2m;
		_r2_5 = _r2 + Math.Abs(_r2 - _r3) / 2m;
		_r3_5 = _r3 + Math.Abs(_r3 - _r4) / 2m;
		_r4_5 = _r4 + Math.Abs(_r4 - _r5) / 2m;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var openTime = candle.OpenTime;

		if (openTime.Date > _sessionStartTime.Date)
		{
			if (Position > 0)
			SellMarket(Math.Abs(Position));

			_openingHigh = null;
			_tradesCounter = 0;
			_trailLong = 0m;
			_slLong = 0m;
			_sessionStartTime = GetNextSessionStart(openTime);
			_sessionEndTime = _sessionStartTime.AddMinutes(RangeMinutes);
		}

		var prevLow = _prevLow;

		if (openTime >= _sessionStartTime && openTime < _sessionEndTime)
		{
			_openingHigh = _openingHigh.HasValue ? Math.Max(_openingHigh.Value, candle.HighPrice) : candle.HighPrice;
			_prevLow = candle.LowPrice;
			return;
		}

		if (!_openingHigh.HasValue)
		{
			_prevLow = candle.LowPrice;
			return;
		}

		_r0_5 = _openingHigh.Value + Math.Abs(_r1 - _openingHigh.Value) / 2m;

		if (Position <= 0 && _tradesCounter < MaxTradesPerDay)
		{
			if (candle.OpenPrice < _openingHigh.Value && candle.HighPrice > _openingHigh.Value && _r1 > _openingHigh.Value)
			{
				_entryPrice = _openingHigh.Value;
				var slPercent = StopLossPercent / 100m;
				_slLong0 = InitialSlType == SlType.Percentage ? _entryPrice * (1m - slPercent) : prevLow;
				BuyMarket(Volume + Math.Abs(Position));
				_tradesCounter++;
				_trailLong = 0m;
				_slLong = _slLong0;
			}
		}

		if (Position > 0)
		{
			var prevTrail = _trailLong;

			if (candle.HighPrice > _r5 && _r4 > prevTrail)
			_trailLong = _r4;
			else if (candle.HighPrice > _r4_5 && _r3_5 > prevTrail)
			_trailLong = _r3_5;
			else if (candle.HighPrice > _r4 && _r3 > prevTrail)
			_trailLong = _r3;
			else if (candle.HighPrice > _r3_5 && _r2_5 > prevTrail)
			_trailLong = _r2_5;
			else if (candle.HighPrice > _r3 && _r2 > prevTrail)
			_trailLong = _r2;
			else if (candle.HighPrice > _r2_5 && _r1_5 > prevTrail)
			_trailLong = _r1_5;
			else if (candle.HighPrice > _r2 && _r1 > prevTrail)
			_trailLong = _r1;
			else if (candle.HighPrice > _r1_5 && _r0_5 > prevTrail)
			_trailLong = _r0_5;
			else if (candle.HighPrice > _r0_5 && _openingHigh.Value > prevTrail)
			_trailLong = _openingHigh.Value;

			_slLong = Math.Max(_slLong0, _trailLong);

			if (candle.LowPrice <= _slLong)
			SellMarket(Math.Abs(Position));
		}

		_prevLow = candle.LowPrice;
	}
}
