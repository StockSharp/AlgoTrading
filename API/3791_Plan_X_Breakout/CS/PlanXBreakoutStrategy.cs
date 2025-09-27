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
/// Breakout strategy that monitors the 15-minute session open and applies pip-based trailing stops.
/// </summary>
public class PlanXBreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _longTargetPips;
	private readonly StrategyParam<int> _shortTargetPips;
	private readonly StrategyParam<int> _initialStopPips;
	private readonly StrategyParam<int> _trailTriggerPips;
	private readonly StrategyParam<int> _trailStepPips;
	private readonly StrategyParam<decimal> _sessionStartHour;
	private readonly StrategyParam<decimal> _sessionEndHour;
	private readonly StrategyParam<DataType> _candleType;

	private TimeSpan _timeFrame;
	private decimal _pipSize;
	private DateTime? _sessionDate;
	private decimal? _sessionAnchorClose;

	private decimal? _longStop;
	private decimal _longEntryPrice;

	private decimal? _shortStop;
	private decimal _shortEntryPrice;

	/// <summary>
	/// Default market order volume.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Minimum long breakout distance relative to the session anchor, in pips.
	/// </summary>
	public int LongTargetPips
	{
		get => _longTargetPips.Value;
		set => _longTargetPips.Value = value;
	}

	/// <summary>
	/// Minimum short breakout distance relative to the session anchor, in pips.
	/// </summary>
	public int ShortTargetPips
	{
		get => _shortTargetPips.Value;
		set => _shortTargetPips.Value = value;
	}

	/// <summary>
	/// Initial stop-loss offset applied after entering a position, in pips.
	/// </summary>
	public int InitialStopPips
	{
		get => _initialStopPips.Value;
		set => _initialStopPips.Value = value;
	}

	/// <summary>
	/// Distance the price must move before the trailing stop is advanced, in pips.
	/// </summary>
	public int TrailTriggerPips
	{
		get => _trailTriggerPips.Value;
		set => _trailTriggerPips.Value = value;
	}

	/// <summary>
	/// Increment applied to the trailing stop whenever it is advanced, in pips.
	/// </summary>
	public int TrailStepPips
	{
		get => _trailStepPips.Value;
		set => _trailStepPips.Value = value;
	}

	/// <summary>
	/// Session start hour expressed as decimal hours (e.g., 11.5 = 11:30).
	/// </summary>
	public decimal SessionStartHour
	{
		get => _sessionStartHour.Value;
		set => _sessionStartHour.Value = value;
	}

	/// <summary>
	/// Session end hour expressed as decimal hours.
	/// </summary>
	public decimal SessionEndHour
	{
		get => _sessionEndHour.Value;
		set => _sessionEndHour.Value = value;
	}

	/// <summary>
	/// Candle type used to drive the breakout evaluation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="PlanXBreakoutStrategy"/> with default parameters.
	/// </summary>
	public PlanXBreakoutStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Default market order volume", "Trading")
			.SetCanOptimize(true);

		_longTargetPips = Param(nameof(LongTargetPips), 25)
			.SetDisplay("Long Target (pips)", "Distance above the anchor that triggers a long entry", "Entries")
			.SetCanOptimize(true);

		_shortTargetPips = Param(nameof(ShortTargetPips), 20)
			.SetDisplay("Short Target (pips)", "Distance below the anchor that triggers a short entry", "Entries")
			.SetCanOptimize(true);

		_initialStopPips = Param(nameof(InitialStopPips), 25)
			.SetDisplay("Initial Stop (pips)", "Initial stop-loss distance from the entry price", "Risk")
			.SetCanOptimize(true);

		_trailTriggerPips = Param(nameof(TrailTriggerPips), 10)
			.SetDisplay("Trail Trigger (pips)", "Advance the trailing stop once price moves this distance", "Risk")
			.SetCanOptimize(true);

		_trailStepPips = Param(nameof(TrailStepPips), 5)
			.SetDisplay("Trail Step (pips)", "Increment applied when the trailing stop is updated", "Risk")
			.SetCanOptimize(true);

		_sessionStartHour = Param(nameof(SessionStartHour), 11m)
			.SetDisplay("Session Start", "Hour that defines the anchor candle", "Session")
			.SetCanOptimize(true);

		_sessionEndHour = Param(nameof(SessionEndHour), 15m)
			.SetDisplay("Session End", "Hour that terminates breakout checks", "Session")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series analyzed by the strategy", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;
		_timeFrame = GetTimeFrame();
		UpdatePipSize();
		ValidateSessionWindow();

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

		if (_pipSize <= 0m)
			UpdatePipSize();

		Volume = TradeVolume;

		var sessionStart = GetSessionTime(SessionStartHour);
		var sessionEnd = GetSessionTime(SessionEndHour);
		if (sessionEnd <= sessionStart)
			return;

		var candleDate = candle.OpenTime.Date;
		if (_sessionDate != candleDate)
		{
			_sessionDate = candleDate;
			_sessionAnchorClose = null;
		}

		if (IsAnchorCandle(candle, sessionStart))
			_sessionAnchorClose = candle.ClosePrice;

		if (ManageExistingPosition(candle))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0m || _sessionAnchorClose is not decimal anchorClose)
			return;

		var openTimeOfDay = candle.OpenTime.TimeOfDay;
		var firstTradingTime = sessionStart + _timeFrame;

		if (openTimeOfDay < firstTradingTime || openTimeOfDay >= sessionEnd)
			return;

		var longBreakoutLevel = anchorClose + LongTargetPips * _pipSize;
		var shortBreakoutLevel = anchorClose - ShortTargetPips * _pipSize;

		if (candle.ClosePrice > longBreakoutLevel)
		{
			EnterLong(candle.ClosePrice);
		}
		else if (candle.ClosePrice < shortBreakoutLevel)
		{
			EnterShort(candle.ClosePrice);
		}
	}

	private bool ManageExistingPosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			return HandleLongPosition(candle);
		}

		if (Position < 0m)
		{
			return HandleShortPosition(candle);
		}

		ResetLongState();
		ResetShortState();
		return false;
	}

	private bool HandleLongPosition(ICandleMessage candle)
	{
		if (_longStop is not decimal)
			return false;

		UpdateLongTrailing(candle);

		if (_longStop is decimal updatedStop && candle.LowPrice <= updatedStop)
		{
			SellMarket(Position);
			ResetLongState();
			return true;
		}

		return false;
	}

	private bool HandleShortPosition(ICandleMessage candle)
	{
		if (_shortStop is not decimal)
			return false;

		UpdateShortTrailing(candle);

		if (_shortStop is decimal updatedStop && candle.HighPrice >= updatedStop)
		{
			BuyMarket(Math.Abs(Position));
			ResetShortState();
			return true;
		}

		return false;
	}

	private void EnterLong(decimal closePrice)
	{
		if (_pipSize <= 0m)
			return;

		_longEntryPrice = closePrice;
		_longStop = closePrice - InitialStopPips * _pipSize;
		_shortStop = null;

		BuyMarket();
	}

	private void EnterShort(decimal closePrice)
	{
		if (_pipSize <= 0m)
			return;

		_shortEntryPrice = closePrice;
		_shortStop = closePrice + InitialStopPips * _pipSize;
		_longStop = null;

		SellMarket();
	}

	private void UpdateLongTrailing(ICandleMessage candle)
	{
		if (_longStop is not decimal stop)
			return;

		var trigger = TrailTriggerPips * _pipSize;
		var step = TrailStepPips * _pipSize;
		if (trigger <= 0m || step <= 0m)
			return;

		var priceAdvance = candle.ClosePrice - _longEntryPrice;
		if (stop < _longEntryPrice)
		{
			if (priceAdvance >= trigger)
				_longStop = _longEntryPrice + step;
			return;
		}

		var buffer = candle.ClosePrice - stop;
		if (buffer >= trigger)
			_longStop = stop + step;
	}

	private void UpdateShortTrailing(ICandleMessage candle)
	{
		if (_shortStop is not decimal stop)
			return;

		var trigger = TrailTriggerPips * _pipSize;
		var step = TrailStepPips * _pipSize;
		if (trigger <= 0m || step <= 0m)
			return;

		var priceAdvance = _shortEntryPrice - candle.ClosePrice;
		if (stop > _shortEntryPrice)
		{
			if (priceAdvance >= trigger)
				_shortStop = _shortEntryPrice - step;
			return;
		}

		var buffer = stop - candle.ClosePrice;
		if (buffer >= trigger)
			_shortStop = stop - step;
	}

	private void ResetLongState()
	{
		_longStop = null;
		_longEntryPrice = 0m;
	}

	private void ResetShortState()
	{
		_shortStop = null;
		_shortEntryPrice = 0m;
	}

	private bool IsAnchorCandle(ICandleMessage candle, TimeSpan sessionStart)
	{
		var openTime = candle.OpenTime.TimeOfDay;
		return Math.Abs((openTime - sessionStart).TotalMinutes) < 0.001;
	}

	private void ValidateSessionWindow()
	{
		var start = GetSessionTime(SessionStartHour);
		var end = GetSessionTime(SessionEndHour);
		if (end <= start)
			throw new InvalidOperationException("Session end must be greater than session start.");
	}

	private TimeSpan GetTimeFrame()
	{
		if (CandleType.Arg is not TimeSpan frame)
			throw new InvalidOperationException("The candle type must define a time frame.");

		if (frame <= TimeSpan.Zero)
			throw new InvalidOperationException("The candle time frame must be positive.");

		return frame;
	}

	private void UpdatePipSize()
	{
		var security = Security;
		if (security == null)
		{
			_pipSize = 0m;
			return;
		}

		var step = security.PriceStep ?? 0m;
		if (step <= 0m)
		{
			_pipSize = 0m;
			return;
		}

		var decimals = security.Decimals;
		var multiplier = decimals is 3 or 5 ? 10m : 1m;
		_pipSize = step * multiplier;
	}

	private static TimeSpan GetSessionTime(decimal hourValue)
	{
		var hours = (int)Math.Truncate(hourValue);
		var minutes = (int)Math.Round((hourValue - hours) * 60m);

		if (minutes < 0)
			minutes = 0;
		else if (minutes > 59)
			minutes = 59;

		if (hours < 0)
			hours = 0;
		else if (hours > 23)
			hours = 23;

		return new TimeSpan(hours, minutes, 0);
	}
}

