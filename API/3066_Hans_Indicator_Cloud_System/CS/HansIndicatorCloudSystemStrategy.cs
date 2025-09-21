namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

public class HansIndicatorCloudSystemStrategy : Strategy
{
	private static readonly TimeSpan Period1Start = TimeSpan.FromHours(4);
	private static readonly TimeSpan Period1End = TimeSpan.FromHours(8);
	private static readonly TimeSpan Period2End = TimeSpan.FromHours(12);

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<int> _localTimeZone;
	private readonly StrategyParam<int> _destinationTimeZone;
	private readonly StrategyParam<decimal> _pipsForEntry;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;
	private readonly StrategyParam<decimal> _tradeVolume;

	private readonly List<int> _colorHistory = new();
	private DayState? _currentDay;
	private TimeSpan _timeShift;

	public HansIndicatorCloudSystemStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Primary timeframe analysed by the strategy.", "General");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetNotNegative()
			.SetDisplay("Signal bar", "Historical bar index inspected for colour changes.", "Signals");

		_localTimeZone = Param(nameof(LocalTimeZone), 0)
			.SetDisplay("Local timezone", "Broker/server timezone used by the raw candles (hours).", "Time zones");

		_destinationTimeZone = Param(nameof(DestinationTimeZone), 4)
			.SetDisplay("Destination timezone", "Target timezone for Hans ranges (hours).", "Time zones");

		_pipsForEntry = Param(nameof(PipsForEntry), 100m)
			.SetNotNegative()
			.SetDisplay("Breakout buffer", "Extra price steps added above/below the session ranges.", "Indicator");

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Enable long entries", "Allow opening new long positions when an upper breakout appears.", "Trading");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Enable short entries", "Allow opening new short positions when a lower breakout appears.", "Trading");

		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Enable long exits", "Allow closing existing longs on a bearish breakout.", "Trading");

		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Enable short exits", "Allow closing existing shorts on a bullish breakout.", "Trading");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade volume", "Order size used for every new position.", "Trading");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	public int LocalTimeZone
	{
		get => _localTimeZone.Value;
		set => _localTimeZone.Value = value;
	}

	public int DestinationTimeZone
	{
		get => _destinationTimeZone.Value;
		set => _destinationTimeZone.Value = value;
	}

	public decimal PipsForEntry
	{
		get => _pipsForEntry.Value;
		set => _pipsForEntry.Value = value;
	}

	public bool BuyPosOpen
	{
		get => _buyPosOpen.Value;
		set => _buyPosOpen.Value = value;
	}

	public bool SellPosOpen
	{
		get => _sellPosOpen.Value;
		set => _sellPosOpen.Value = value;
	}

	public bool BuyPosClose
	{
		get => _buyPosClose.Value;
		set => _buyPosClose.Value = value;
	}

	public bool SellPosClose
	{
		get => _sellPosClose.Value;
		set => _sellPosClose.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume; // Keep the default Strategy volume aligned with the configured trade size.

		_timeShift = TimeSpan.FromHours(DestinationTimeZone - LocalTimeZone);
		_currentDay = null;
		_colorHistory.Clear();

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

		var color = CalculateColor(candle);
		_colorHistory.Add(color); // Store Hans indicator colour codes for historical lookups.

		var maxHistory = Math.Max(5, SignalBar + 3);
		if (_colorHistory.Count > maxHistory)
			_colorHistory.RemoveAt(0); // Keep just enough history for signal evaluation.

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Align the history pointer with the requested SignalBar offset.
		var targetIndex = _colorHistory.Count - 1 - SignalBar;
		if (targetIndex <= 0)
			return;

		// Evaluate the Hans indicator codes for breakout conditions.
		var col0 = _colorHistory[targetIndex];
		var col1 = _colorHistory[targetIndex - 1];

		var bullishBreakout = col1 == 0 || col1 == 1;
		var bearishBreakout = col1 == 3 || col1 == 4;

		// Prepare trading decisions that mimic TradeAlgorithms.mqh helper flags.
		var shouldCloseShort = SellPosClose && bullishBreakout;
		var shouldOpenLong = BuyPosOpen && bullishBreakout && col0 != 0 && col0 != 1;
		var shouldCloseLong = BuyPosClose && bearishBreakout;
		var shouldOpenShort = SellPosOpen && bearishBreakout && col0 != 3 && col0 != 4;

		// Close existing long positions before handling new entries.
		if (shouldCloseLong && Position > 0)
		{
			var volume = Position;
			if (volume > 0)
				SellMarket(volume);
		}

		// Close existing short positions before handling new entries.
		if (shouldCloseShort && Position < 0)
		{
			var volume = Math.Abs(Position);
			if (volume > 0)
				BuyMarket(volume);
		}

		// Flatten any opposite exposure before opening a fresh long trade.
		if (shouldOpenLong && Position <= 0 && TradeVolume > 0)
		{
			if (Position < 0)
			{
				var covering = Math.Abs(Position);
				if (covering > 0)
					BuyMarket(covering);
			}

			BuyMarket(TradeVolume);
		}

		// Flatten any opposite exposure before opening a fresh short trade.
		if (shouldOpenShort && Position >= 0 && TradeVolume > 0)
		{
			if (Position > 0)
			{
				var covering = Position;
				if (covering > 0)
					SellMarket(covering);
			}

			SellMarket(TradeVolume);
		}
	}

	private int CalculateColor(ICandleMessage candle)
	{
		var shiftedTime = candle.OpenTime + _timeShift;
		var day = shiftedTime.Date;

		// Build or reset the daily session state after applying the timezone shift.
		if (_currentDay == null || _currentDay.Date != day)
			_currentDay = new DayState(day);

		UpdateSessionExtremes(_currentDay, candle, shiftedTime.TimeOfDay);

		var zone = GetActiveZone(_currentDay);
		if (zone == null)
			return 2;

		var (upper, lower) = zone.Value;
		var close = candle.ClosePrice;
		var open = candle.OpenPrice;

		// The Hans indicator paints breakout candles with colour codes 0/1 (bullish) and 3/4 (bearish).
		if (close > upper)
			return close >= open ? 0 : 1;

		if (close < lower)
			return close <= open ? 4 : 3;

		return 2;
	}

	// Track the two Hans sessions (04:00-08:00 and 08:00-12:00 target time) and their high/low ranges.
	private void UpdateSessionExtremes(DayState dayState, ICandleMessage candle, TimeSpan localTime)
	{
		if (localTime >= Period1Start && localTime < Period1End)
		{
			// First session: update running high/low.
			dayState.Period1Seen = true;
			dayState.Period1High = dayState.Period1High.HasValue
				? Math.Max(dayState.Period1High.Value, candle.HighPrice)
				: candle.HighPrice;
			dayState.Period1Low = dayState.Period1Low.HasValue
				? Math.Min(dayState.Period1Low.Value, candle.LowPrice)
				: candle.LowPrice;
		}
		else if (localTime >= Period1End && localTime < Period2End)
		{
			// Second session: finalise the first zone and accumulate the second zone.
			if (!dayState.Period1Closed && dayState.Period1Seen)
				dayState.Period1Closed = true;

			dayState.Period2Seen = true;
			dayState.Period2High = dayState.Period2High.HasValue
				? Math.Max(dayState.Period2High.Value, candle.HighPrice)
				: candle.HighPrice;
			dayState.Period2Low = dayState.Period2Low.HasValue
				? Math.Min(dayState.Period2Low.Value, candle.LowPrice)
				: candle.LowPrice;
		}
		else
		{
			// After the monitored windows we just lock the zones if they received data.
			if (!dayState.Period1Closed && dayState.Period1Seen && localTime >= Period1End)
				dayState.Period1Closed = true;

			if (!dayState.Period2Closed && dayState.Period2Seen && localTime >= Period2End)
				dayState.Period2Closed = true;
		}

		if (localTime >= Period2End && dayState.Period2Seen)
			dayState.Period2Closed = true;
	}

	// Prefer the second session range when available, otherwise fall back to the first session.
	private (decimal upper, decimal lower)? GetActiveZone(DayState dayState)
	{
		var entryOffset = GetEntryOffset();
		if (dayState.Period2Closed && dayState.Period2High.HasValue && dayState.Period2Low.HasValue)
		{
			return (
				dayState.Period2High.Value + entryOffset,
				dayState.Period2Low.Value - entryOffset);
		}

		if (dayState.Period1Closed && dayState.Period1High.HasValue && dayState.Period1Low.HasValue)
		{
			return (
				dayState.Period1High.Value + entryOffset,
				dayState.Period1Low.Value - entryOffset);
		}

		return null;
	}

	// Convert the buffer measured in points into absolute price units.
	private decimal GetEntryOffset()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0)
			step = 1m;

		return PipsForEntry * step;
	}

	// Container for daily session statistics.
	private sealed class DayState
	{
		public DayState(DateTime date)
		{
			Date = date;
		}

		public DateTime Date { get; }

		public decimal? Period1High { get; set; }
		public decimal? Period1Low { get; set; }
		public bool Period1Seen { get; set; }
		public bool Period1Closed { get; set; }

		public decimal? Period2High { get; set; }
		public decimal? Period2Low { get; set; }
		public bool Period2Seen { get; set; }
		public bool Period2Closed { get; set; }
	}
}
