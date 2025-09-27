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
/// Port of the MetaTrader strategy earlyBird1.
/// Builds a morning range, filters direction with RSI, and trades the first breakout.
/// Includes volatility-aware trailing and a configurable session close out.
/// </summary>
public class EarlyBirdRangeBreakoutStrategy : Strategy
{
	private enum TradeDirection
	{
		Both = 0,
		LongOnly = 1,
		ShortOnly = 2,
	}


	private readonly StrategyParam<bool> _enableAutoTrading;
	private readonly StrategyParam<bool> _enableHedging;
	private readonly StrategyParam<int> _directionMode;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingRiskMultiplier;
	private readonly StrategyParam<decimal> _entryBufferPips;
	private readonly StrategyParam<int> _tradingStartHour;
	private readonly StrategyParam<int> _tradingStartMinute;
	private readonly StrategyParam<int> _tradingEndHour;
	private readonly StrategyParam<int> _closingHour;
	private readonly StrategyParam<int> _rangeStartHour;
	private readonly StrategyParam<int> _rangeEndHour;
	private readonly StrategyParam<int> _summerTimeStartDay;
	private readonly StrategyParam<int> _winterTimeStartDay;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _volatilityWindowLength;

	private RelativeStrengthIndex _rsi = null!;

	private readonly Queue<decimal> _volatilityWindow = new();
	private decimal _volatilitySum;

	private DateTime? _currentDay;
	private DateTimeOffset _rangeStartTime;
	private DateTimeOffset _rangeEndTime;
	private DateTimeOffset _tradingStartTime;
	private DateTimeOffset _tradingEndTime;
	private DateTimeOffset _closingTime;

	private decimal? _rangeHigh;
	private decimal? _rangeLow;
	private decimal? _longEntryLevel;
	private decimal? _shortEntryLevel;
	private decimal _rangeWidthPips;
	private bool _rangeReady;

	private bool _longTradedToday;
	private bool _shortTradedToday;

	private decimal _pipSize;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _previousPosition;

	/// <summary>
	/// Initializes a new instance of the strategy with default parameters.
	/// </summary>
	public EarlyBirdRangeBreakoutStrategy()
	{
		_enableAutoTrading = Param(nameof(EnableAutoTrading), true)
		.SetDisplay("Auto Trading", "Enable automatic entries", "Trading")
		.SetCanOptimize(false);

		_enableHedging = Param(nameof(EnableHedging), true)
		.SetDisplay("Allow Hedging", "Permit reversing positions within the session", "Trading")
		.SetCanOptimize(false);

		_directionMode = Param(nameof(DirectionMode), (int)TradeDirection.Both)
		.SetDisplay("Trade Direction", "0 = both, 1 = long only, 2 = short only", "Trading")
		.SetCanOptimize(false);


		_takeProfitPips = Param(nameof(TakeProfitPips), 25m)
		.SetDisplay("Take Profit (pips)", "Maximum profit target distance", "Risk")
		.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 50m)
		.SetDisplay("Stop Loss (pips)", "Protective stop distance", "Risk")
		.SetCanOptimize(true);

		_trailingStopPips = Param(nameof(TrailingStopPips), 15m)
		.SetDisplay("Trailing Trigger (pips)", "Distance required to arm trailing", "Risk")
		.SetCanOptimize(true);

		_trailingRiskMultiplier = Param(nameof(TrailingRiskMultiplier), 1m)
		.SetDisplay("Trailing Risk", "Multiplier applied to average volatility", "Risk")
		.SetCanOptimize(true);

		_entryBufferPips = Param(nameof(EntryBufferPips), 2m)
		.SetDisplay("Entry Buffer (pips)", "Offset added above/below the range boundary", "Range")
		.SetCanOptimize(true);

		_tradingStartHour = Param(nameof(TradingStartHour), 7)
		.SetDisplay("Session Start Hour", "Hour when new trades may begin", "Session")
		.SetCanOptimize(false);

		_tradingStartMinute = Param(nameof(TradingStartMinute), 15)
		.SetDisplay("Session Start Minute", "Minute component for session start", "Session")
		.SetCanOptimize(false);

		_tradingEndHour = Param(nameof(TradingEndHour), 15)
		.SetDisplay("Session End Hour", "Hour that stops accepting new trades", "Session")
		.SetCanOptimize(false);

		_closingHour = Param(nameof(ClosingHour), 17)
		.SetDisplay("Closing Hour", "Hour to force day-trade exits", "Session")
		.SetCanOptimize(false);

		_rangeStartHour = Param(nameof(RangeStartHour), 3)
		.SetDisplay("Range Start Hour", "Hour to begin the overnight range scan", "Range")
		.SetCanOptimize(false);

		_rangeEndHour = Param(nameof(RangeEndHour), 7)
		.SetDisplay("Range End Hour", "Hour to finish the range scan", "Range")
		.SetCanOptimize(false);

		_summerTimeStartDay = Param(nameof(SummerTimeStartDay), 87)
		.SetDisplay("Summer Time Start", "Day-of-year when DST begins", "Calendar")
		.SetCanOptimize(false);

		_winterTimeStartDay = Param(nameof(WinterTimeStartDay), 297)
		.SetDisplay("Winter Time Start", "Day-of-year when DST ends", "Calendar")
		.SetCanOptimize(false);

		_rsiLength = Param(nameof(RsiLength), 14)
		.SetDisplay("RSI Length", "Number of periods for RSI", "Indicators")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(15)))
		.SetDisplay("Candle Type", "Primary timeframe for calculations", "Data")
		.SetCanOptimize(false);
		_volatilityWindowLength = Param(nameof(VolatilityWindowLength), 16)
			.SetGreaterThanZero()
			.SetDisplay("Volatility Window", "Number of bars used to average volatility for trailing decisions", "Risk");
	}

	/// <summary>
	/// Enables automated entries within the trading window.
	/// </summary>
	public bool EnableAutoTrading
	{
		get => _enableAutoTrading.Value;
		set => _enableAutoTrading.Value = value;
	}

	/// <summary>
	/// Allows the strategy to reverse positions even if an opposite trade is still open.
	/// </summary>
	public bool EnableHedging
	{
		get => _enableHedging.Value;
		set => _enableHedging.Value = value;
	}

	/// <summary>
	/// Determines which direction is eligible for new trades.
	/// </summary>
	public int DirectionMode
	{
		get => _directionMode.Value;
		set => _directionMode.Value = value;
	}


	/// <summary>
	/// Maximum profit target distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Protective stop distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Distance in pips required before activating the trailing logic.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the average volatility to enable trailing.
	/// </summary>
	public decimal TrailingRiskMultiplier
	{
		get => _trailingRiskMultiplier.Value;
		set => _trailingRiskMultiplier.Value = value;
	}

	/// <summary>
	/// Offset in pips added to the range high/low when defining entry triggers.
	/// </summary>
	public decimal EntryBufferPips
	{
		get => _entryBufferPips.Value;
		set => _entryBufferPips.Value = value;
	}

	/// <summary>
	/// Hour when new trades may begin (chart time).
	/// </summary>
	public int TradingStartHour
	{
		get => _tradingStartHour.Value;
		set => _tradingStartHour.Value = value;
	}

	/// <summary>
	/// Minute component for the start of the trading session.
	/// </summary>
	public int TradingStartMinute
	{
		get => _tradingStartMinute.Value;
		set => _tradingStartMinute.Value = value;
	}

	/// <summary>
	/// Hour when new trades are no longer accepted.
	/// </summary>
	public int TradingEndHour
	{
		get => _tradingEndHour.Value;
		set => _tradingEndHour.Value = value;
	}

	/// <summary>
	/// Hour used to force a day-trade exit.
	/// </summary>
	public int ClosingHour
	{
		get => _closingHour.Value;
		set => _closingHour.Value = value;
	}

	/// <summary>
	/// Hour when the reference range calculation starts.
	/// </summary>
	public int RangeStartHour
	{
		get => _rangeStartHour.Value;
		set => _rangeStartHour.Value = value;
	}

	/// <summary>
	/// Hour when the reference range calculation ends.
	/// </summary>
	public int RangeEndHour
	{
		get => _rangeEndHour.Value;
		set => _rangeEndHour.Value = value;
	}

	/// <summary>
	/// Day of year when daylight saving time begins.
	/// </summary>
	public int SummerTimeStartDay
	{
		get => _summerTimeStartDay.Value;
		set => _summerTimeStartDay.Value = value;
	}

	/// <summary>
	/// Day of year when daylight saving time ends.
	/// </summary>
	public int WinterTimeStartDay
	{
		get => _winterTimeStartDay.Value;
		set => _winterTimeStartDay.Value = value;
	}

	/// <summary>
	/// RSI period used to filter trade direction.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set
		{
			_rsiLength.Value = value;

			if (_rsi != null)
			_rsi.Length = value;
		}
	}

	/// <summary>
	/// Candle type that drives the strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	/// <summary>
	/// Number of bars used to average recent volatility for trailing calculations.
	/// </summary>
	public int VolatilityWindowLength
	{
		get => _volatilityWindowLength.Value;
		set
		{
			if (_volatilityWindowLength.Value == value)
				return;

			_volatilityWindowLength.Value = value;
			TrimVolatilityWindow();
		}
	}

	private void TrimVolatilityWindow()
	{
		var limit = _volatilityWindowLength.Value;
		if (limit <= 0)
		{
			limit = 1;
			_volatilityWindowLength.Value = limit;
		}

		while (_volatilityWindow.Count > limit)
		{
			_volatilitySum -= _volatilityWindow.Dequeue();
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_rsi = null!;
		_volatilityWindow.Clear();
		_volatilitySum = 0m;

		_currentDay = null;
		_rangeHigh = null;
		_rangeLow = null;
		_longEntryLevel = null;
		_shortEntryLevel = null;
		_rangeWidthPips = 0m;
		_rangeReady = false;
		_longTradedToday = false;
		_shortTradedToday = false;

		_pipSize = 0m;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_previousPosition = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.WhenCandlesFinished(ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		EnsureDailyState(candle.CloseTime);
		UpdatePipSize(candle.ClosePrice);
		UpdateRange(candle);

		var currentRangePips = ComputeRangeInPips(candle);
		var averageVolatility = _volatilityWindow.Count == VolatilityWindowLength ? _volatilitySum / VolatilityWindowLength : 0m;

		var rsiValue = _rsi.Process(candle.OpenPrice, candle.CloseTime, true);
		if (!rsiValue.IsFinal)
		{
			AddVolatilitySample(currentRangePips);
			return;
		}

		var rsi = rsiValue.ToDecimal();

		if (HandleStopAndTargetExits(candle))
		{
			AddVolatilitySample(currentRangePips);
			return;
		}

		if (HandleSessionClosing(candle))
		{
			AddVolatilitySample(currentRangePips);
			return;
		}

		UpdateTrailing(candle, averageVolatility, currentRangePips);

		var allowTrading = EnableAutoTrading && IsBusinessDay(candle.CloseTime.DayOfWeek) && IsWithinTradingWindow(candle.CloseTime);

		if (allowTrading && _rangeReady)
		{
			if (rsi > 50m)
			TryEnterLong(candle);
			else
			TryEnterShort(candle);
		}

		AddVolatilitySample(currentRangePips);
	}

	/// <inheritdoc />
	protected override void OnPositionChanged()
	{
		var previous = _previousPosition;

		base.OnPositionChanged();

		var current = Position;

		if (current == 0m)
		{
			_entryPrice = 0m;
			_stopPrice = 0m;
			_takePrice = 0m;
		}
		else
		{
			_entryPrice = PositionPrice;
			InitializeTargets();

			if (current > 0m && previous <= 0m)
			_longTradedToday = true;
			else if (current < 0m && previous >= 0m)
			_shortTradedToday = true;
		}

		_previousPosition = current;
	}

	private void InitializeTargets()
	{
		var stopOffset = GetStopOffset();
		var takeOffset = GetTakeOffset();

		if (Position > 0m)
		{
			_stopPrice = stopOffset > 0m ? _entryPrice - stopOffset : 0m;
			_takePrice = takeOffset > 0m ? _entryPrice + takeOffset : 0m;
		}
		else if (Position < 0m)
		{
			_stopPrice = stopOffset > 0m ? _entryPrice + stopOffset : 0m;
			_takePrice = takeOffset > 0m ? _entryPrice - takeOffset : 0m;
		}
		else
		{
			_stopPrice = 0m;
			_takePrice = 0m;
		}
	}

	private void EnsureDailyState(DateTimeOffset time)
	{
		var currentDate = time.Date;
		if (_currentDay == null || _currentDay.Value != currentDate)
		ResetDailyState(time);
	}

	private void ResetDailyState(DateTimeOffset time)
	{
		_currentDay = time.Date;
		_longTradedToday = false;
		_shortTradedToday = false;
		_rangeHigh = null;
		_rangeLow = null;
		_longEntryLevel = null;
		_shortEntryLevel = null;
		_rangeWidthPips = 0m;
		_rangeReady = false;

		UpdateSchedule(time);
	}

	private void UpdateSchedule(DateTimeOffset time)
	{
		var offset = time.Offset;
		var date = time.Date;
		var shift = GetDaylightShift(date);

		_rangeStartTime = BuildTime(date, RangeStartHour, 0, shift, offset);
		_rangeEndTime = BuildTime(date, RangeEndHour, 0, shift, offset);
		_tradingStartTime = BuildTime(date, TradingStartHour, TradingStartMinute, shift, offset);
		_tradingEndTime = BuildTime(date, TradingEndHour, 0, shift, offset);
		_closingTime = BuildTime(date, ClosingHour, 0, shift, offset);
	}

	private static DateTimeOffset BuildTime(DateTime date, int hour, int minute, int shift, TimeSpan offset)
	{
		var adjustedHour = hour - shift;
		var adjustedDate = date;

		while (adjustedHour < 0)
		{
			adjustedHour += 24;
			adjustedDate = adjustedDate.AddDays(-1);
		}

		while (adjustedHour >= 24)
		{
			adjustedHour -= 24;
			adjustedDate = adjustedDate.AddDays(1);
		}

		return new DateTimeOffset(adjustedDate.Year, adjustedDate.Month, adjustedDate.Day, adjustedHour, minute, 0, offset);
	}

	private int GetDaylightShift(DateTime date)
	{
		var day = date.DayOfYear;
		var summer = SummerTimeStartDay;
		var winter = WinterTimeStartDay;

		if (summer == winter)
		return 2;

		if (summer < winter)
		{
			if (day >= summer && day <= winter)
			return 2;

			return 1;
		}

		if (day >= summer || day <= winter)
		return 2;

		return 1;
	}

	private void UpdateRange(ICandleMessage candle)
	{
		var candleStart = candle.OpenTime;
		var candleEnd = candle.CloseTime;

		var overlapsRange = candleStart < _rangeEndTime && candleEnd > _rangeStartTime;
		if (overlapsRange)
		{
			if (_rangeHigh == null || candle.HighPrice > _rangeHigh)
			_rangeHigh = candle.HighPrice;

			if (_rangeLow == null || candle.LowPrice < _rangeLow)
			_rangeLow = candle.LowPrice;
		}

		if (!_rangeReady && candleEnd >= _rangeEndTime)
		{
			_rangeReady = _rangeHigh.HasValue && _rangeLow.HasValue;
			if (_rangeReady)
			UpdateEntryLevels();
		}
		else if (_rangeReady)
		{
			UpdateEntryLevels();
		}
	}

	private void UpdateEntryLevels()
	{
		if (!_rangeHigh.HasValue || !_rangeLow.HasValue)
		return;

		var pip = _pipSize > 0m ? _pipSize : GuessPipSize(_rangeHigh.Value);
		var buffer = EntryBufferPips * pip;

		var top = _rangeHigh.Value + buffer;
		var bottom = _rangeLow.Value - buffer;

		_longEntryLevel = top;
		_shortEntryLevel = bottom;

		_rangeWidthPips = pip > 0m ? (top - bottom) / pip : 0m;
	}

	private void UpdatePipSize(decimal price)
	{
		var absolutePrice = Math.Abs(price);
		_pipSize = absolutePrice >= 10m ? 0.01m : 0.0001m;
	}

	private decimal GuessPipSize(decimal referencePrice)
	{
		var absolutePrice = Math.Abs(referencePrice);
		return absolutePrice >= 10m ? 0.01m : 0.0001m;
	}

	private decimal ComputeRangeInPips(ICandleMessage candle)
	{
		if (_pipSize <= 0m)
		return 0m;

		var range = candle.HighPrice - candle.LowPrice;
		return range > 0m ? range / _pipSize : 0m;
	}

	private void AddVolatilitySample(decimal rangePips)
	{
		if (rangePips < 0m)
		rangePips = 0m;

		_volatilityWindow.Enqueue(rangePips);
		_volatilitySum += rangePips;

		if (_volatilityWindow.Count > VolatilityWindowLength)
		_volatilitySum -= _volatilityWindow.Dequeue();
	}

	private decimal GetStopOffset()
	{
		var stopPips = StopLossPips;
		if (stopPips <= 0m)
		return 0m;

		var pip = _pipSize > 0m ? _pipSize : GuessPipSize(_entryPrice);
		return stopPips * pip;
	}

	private decimal GetTakeOffset()
	{
		var takePips = TakeProfitPips;
		if (takePips <= 0m)
		return 0m;

		var candidate = takePips;
		if (StopLossPips > 0m)
		candidate = Math.Min(candidate, StopLossPips);

		if (_rangeWidthPips > 0m)
		candidate = Math.Min(candidate, _rangeWidthPips);

		if (candidate <= 0m)
		return 0m;

		var pip = _pipSize > 0m ? _pipSize : GuessPipSize(_entryPrice);
		return candidate * pip;
	}

	private static bool IsBusinessDay(DayOfWeek dayOfWeek)
	{
		return dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday;
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		return IsWithinWindow(time, _tradingStartTime, _tradingEndTime);
	}

	private bool IsWithinWindow(DateTimeOffset time, DateTimeOffset start, DateTimeOffset end)
	{
		if (end > start)
		return time >= start && time < end;

		return time >= start || time < end;
	}

	private bool IsPastClosingTime(DateTimeOffset time)
	{
		if (_closingTime == default)
		return false;

		if (_closingTime > _tradingStartTime)
		return time >= _closingTime;

		return time >= _closingTime && time < _tradingStartTime;
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		if (!AllowLongEntry())
		return;

		if (_longEntryLevel is not decimal entryPrice)
		return;

		if (candle.HighPrice < entryPrice || candle.LowPrice > entryPrice)
		return;

		var volume = GetBuyVolume();
		if (volume <= 0m)
		return;

		BuyMarket(volume);
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		if (!AllowShortEntry())
		return;

		if (_shortEntryLevel is not decimal entryPrice)
		return;

		if (candle.LowPrice > entryPrice || candle.HighPrice < entryPrice)
		return;

		var volume = GetSellVolume();
		if (volume <= 0m)
		return;

		SellMarket(volume);
	}

	private bool AllowLongEntry()
	{
		if (DirectionMode == (int)TradeDirection.ShortOnly)
		return false;

		if (_longTradedToday)
		return false;

		if (!EnableHedging && Position != 0m)
		return false;

		if (Position > 0m)
		return false;

		return true;
	}

	private bool AllowShortEntry()
	{
		if (DirectionMode == (int)TradeDirection.LongOnly)
		return false;

		if (_shortTradedToday)
		return false;

		if (!EnableHedging && Position != 0m)
		return false;

		if (Position < 0m)
		return false;

		return true;
	}

	private decimal GetBuyVolume()
	{
		var volume = Volume;
		if (Position < 0m)
		volume += Math.Abs(Position);

		return volume;
	}

	private decimal GetSellVolume()
	{
		var volume = Volume;
		if (Position > 0m)
		volume += Position;

		return volume;
	}

	private bool HandleStopAndTargetExits(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			var volume = Position;

			if (_stopPrice > 0m && candle.LowPrice <= _stopPrice)
			{
				SellMarket(volume);
				return true;
			}

			if (_takePrice > 0m && candle.HighPrice >= _takePrice)
			{
				SellMarket(volume);
				return true;
			}
		}
		else if (Position < 0m)
		{
			var volume = Math.Abs(Position);

			if (_stopPrice > 0m && candle.HighPrice >= _stopPrice)
			{
				BuyMarket(volume);
				return true;
			}

			if (_takePrice > 0m && candle.LowPrice <= _takePrice)
			{
				BuyMarket(volume);
				return true;
			}
		}

		return false;
	}

	private bool HandleSessionClosing(ICandleMessage candle)
	{
		if (Position == 0m)
		return false;

		if (!IsPastClosingTime(candle.CloseTime))
		return false;

		if (Position > 0m)
		{
			if (candle.ClosePrice > _entryPrice)
			{
				SellMarket(Position);
				return true;
			}

			if (_takePrice != _entryPrice)
			_takePrice = _entryPrice;
		}
		else if (Position < 0m)
		{
			var volume = Math.Abs(Position);

			if (candle.ClosePrice < _entryPrice)
			{
				BuyMarket(volume);
				return true;
			}

			if (_takePrice != _entryPrice)
			_takePrice = _entryPrice;
		}

		return false;
	}

	private void UpdateTrailing(ICandleMessage candle, decimal averageVolatility, decimal currentRangePips)
	{
		if (Position == 0m)
		return;

		if (TrailingStopPips <= 0m || StopLossPips <= 0m)
		return;

		if (_volatilityWindow.Count < VolatilityWindowLength)
		return;

		if (currentRangePips <= averageVolatility * TrailingRiskMultiplier)
		return;

		var trailingOffset = TrailingStopPips * (_pipSize > 0m ? _pipSize : GuessPipSize(_entryPrice));
		var stopOffset = GetStopOffset();

		if (trailingOffset <= 0m || stopOffset <= 0m)
		return;

		if (Position > 0m)
		{
			var profitDistance = candle.ClosePrice - _entryPrice;
			if (profitDistance < trailingOffset)
			return;

			var candidateStop = candle.ClosePrice - stopOffset;
			if (_stopPrice == 0m || candidateStop > _stopPrice)
			{
				_stopPrice = candidateStop;
				_takePrice = candle.ClosePrice + trailingOffset / 2m;
			}
		}
		else if (Position < 0m)
		{
			var profitDistance = _entryPrice - candle.ClosePrice;
			if (profitDistance < trailingOffset)
			return;

			var candidateStop = candle.ClosePrice + stopOffset;
			if (_stopPrice == 0m || candidateStop < _stopPrice)
			{
				_stopPrice = candidateStop;
				_takePrice = candle.ClosePrice - trailingOffset / 2m;
			}
		}
	}
}
