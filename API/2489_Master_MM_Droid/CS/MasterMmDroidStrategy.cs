using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the Master MM Droid strategy with modular money management blocks.
/// </summary>
public class MasterMmDroidStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _timeShiftHours;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<bool> _enableRsiModule;
	private readonly StrategyParam<bool> _enableBoxModule;
	private readonly StrategyParam<bool> _enableWeeklyModule;
	private readonly StrategyParam<bool> _enableGapModule;

	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiLowerLevel;
	private readonly StrategyParam<decimal> _rsiUpperLevel;
	private readonly StrategyParam<int> _rsiMaxEntries;
	private readonly StrategyParam<decimal> _rsiPyramidPoints;
	private readonly StrategyParam<decimal> _rsiStopLossPoints;
	private readonly StrategyParam<decimal> _rsiTrailingPoints;

	private readonly StrategyParam<decimal> _boxEntryPoints;
	private readonly StrategyParam<decimal> _boxTrailingPoints;

	private readonly StrategyParam<decimal> _weeklyEntryPoints;
	private readonly StrategyParam<int> _weeklySetupEndHour;
	private readonly StrategyParam<decimal> _weeklyTrailingPoints;

	private readonly StrategyParam<decimal> _gapStopLossPoints;
	private readonly StrategyParam<decimal> _gapTrailingPoints;

	private RelativeStrengthIndex _rsi = null!;

	private decimal _previousRsi;
	private bool _hasPreviousRsi;
	private decimal? _lastLongEntryPrice;
	private decimal? _lastShortEntryPrice;

	private decimal? _activeStopPrice;
	private decimal _activeTrailingPoints;

	private bool _boxOrdersPlaced;
	private bool _weeklyOrdersPlaced;

	private DateTime _currentWeekDate;
	private decimal _weeklyHigh;
	private decimal _weeklyLow;
	private bool _weeklyTracking;

	private DateTime _currentDay;
	private bool _hasDayData;
	private decimal _dayOpen;
	private decimal _dayHigh;
	private decimal _dayLow;
	private decimal _prevDayHigh;
	private decimal _prevDayLow;
	private bool _pendingGapCheck;

	public MasterMmDroidStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");

		_timeShiftHours = Param(nameof(TimeShiftHours), 2)
			.SetDisplay("Time Shift", "Session shift versus UTC", "Timing");

		_startHour = Param(nameof(StartHour), 0)
			.SetDisplay("Start Hour", "Base hour for the weekly module", "Timing");

		_enableRsiModule = Param(nameof(EnableRsiModule), true)
			.SetDisplay("Enable RSI", "Toggle the RSI money management block", "Modules");

		_enableBoxModule = Param(nameof(EnableBoxModule), true)
			.SetDisplay("Enable Box", "Toggle the breakout box block", "Modules");

		_enableWeeklyModule = Param(nameof(EnableWeeklyModule), true)
			.SetDisplay("Enable Weekly", "Toggle the weekly breakout block", "Modules");

		_enableGapModule = Param(nameof(EnableGapModule), true)
			.SetDisplay("Enable Gap", "Toggle the gap trading block", "Modules");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", string.Empty, "RSI")
			.SetCanOptimize(true);

		_rsiLowerLevel = Param(nameof(RsiLowerLevel), 30m)
			.SetDisplay("RSI Oversold", string.Empty, "RSI")
			.SetCanOptimize(true);

		_rsiUpperLevel = Param(nameof(RsiUpperLevel), 70m)
			.SetDisplay("RSI Overbought", string.Empty, "RSI")
			.SetCanOptimize(true);

		_rsiMaxEntries = Param(nameof(RsiMaxEntries), 3)
			.SetDisplay("RSI Entries", "Maximum pyramiding steps", "RSI");

		_rsiPyramidPoints = Param(nameof(RsiPyramidPoints), 15m)
			.SetDisplay("RSI Pyramid Points", "Price distance in points between entries", "RSI");

		_rsiStopLossPoints = Param(nameof(RsiStopLossPoints), 35m)
			.SetDisplay("RSI Stop Loss", "Initial protective stop in points", "RSI");

		_rsiTrailingPoints = Param(nameof(RsiTrailingPoints), 50m)
			.SetDisplay("RSI Trailing", "Trailing distance in points", "RSI");

		_boxEntryPoints = Param(nameof(BoxEntryPoints), 10m)
			.SetDisplay("Box Offset", "Breakout distance above/below the box", "Box");

		_boxTrailingPoints = Param(nameof(BoxTrailingPoints), 35m)
			.SetDisplay("Box Trailing", "Trailing distance after a box entry", "Box");

		_weeklyEntryPoints = Param(nameof(WeeklyEntryPoints), 15m)
			.SetDisplay("Weekly Offset", "Breakout distance for the weekly orders", "Weekly");

		_weeklySetupEndHour = Param(nameof(WeeklySetupEndHour), 6)
			.SetDisplay("Weekly Setup End", "Hour (session time) to stop placing new weekly orders", "Weekly");

		_weeklyTrailingPoints = Param(nameof(WeeklyTrailingPoints), 100m)
			.SetDisplay("Weekly Trailing", "Trailing distance for weekly trades", "Weekly");

		_gapStopLossPoints = Param(nameof(GapStopLossPoints), 105m)
			.SetDisplay("Gap Stop", "Stop loss distance for gap trades", "Gap");

		_gapTrailingPoints = Param(nameof(GapTrailingPoints), 115m)
			.SetDisplay("Gap Trailing", "Trailing distance for gap trades", "Gap");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int TimeShiftHours
	{
		get => _timeShiftHours.Value;
		set => _timeShiftHours.Value = value;
	}

	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	public bool EnableRsiModule
	{
		get => _enableRsiModule.Value;
		set => _enableRsiModule.Value = value;
	}

	public bool EnableBoxModule
	{
		get => _enableBoxModule.Value;
		set => _enableBoxModule.Value = value;
	}

	public bool EnableWeeklyModule
	{
		get => _enableWeeklyModule.Value;
		set => _enableWeeklyModule.Value = value;
	}

	public bool EnableGapModule
	{
		get => _enableGapModule.Value;
		set => _enableGapModule.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public decimal RsiLowerLevel
	{
		get => _rsiLowerLevel.Value;
		set => _rsiLowerLevel.Value = value;
	}

	public decimal RsiUpperLevel
	{
		get => _rsiUpperLevel.Value;
		set => _rsiUpperLevel.Value = value;
	}

	public int RsiMaxEntries
	{
		get => _rsiMaxEntries.Value;
		set => _rsiMaxEntries.Value = value;
	}

	public decimal RsiPyramidPoints
	{
		get => _rsiPyramidPoints.Value;
		set => _rsiPyramidPoints.Value = value;
	}

	public decimal RsiStopLossPoints
	{
		get => _rsiStopLossPoints.Value;
		set => _rsiStopLossPoints.Value = value;
	}

	public decimal RsiTrailingPoints
	{
		get => _rsiTrailingPoints.Value;
		set => _rsiTrailingPoints.Value = value;
	}

	public decimal BoxEntryPoints
	{
		get => _boxEntryPoints.Value;
		set => _boxEntryPoints.Value = value;
	}

	public decimal BoxTrailingPoints
	{
		get => _boxTrailingPoints.Value;
		set => _boxTrailingPoints.Value = value;
	}

	public decimal WeeklyEntryPoints
	{
		get => _weeklyEntryPoints.Value;
		set => _weeklyEntryPoints.Value = value;
	}

	public int WeeklySetupEndHour
	{
		get => _weeklySetupEndHour.Value;
		set => _weeklySetupEndHour.Value = value;
	}

	public decimal WeeklyTrailingPoints
	{
		get => _weeklyTrailingPoints.Value;
		set => _weeklyTrailingPoints.Value = value;
	}

	public decimal GapStopLossPoints
	{
		get => _gapStopLossPoints.Value;
		set => _gapStopLossPoints.Value = value;
	}

	public decimal GapTrailingPoints
	{
		get => _gapTrailingPoints.Value;
		set => _gapTrailingPoints.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousRsi = default;
		_hasPreviousRsi = false;
		_lastLongEntryPrice = null;
		_lastShortEntryPrice = null;
		_activeStopPrice = null;
		_activeTrailingPoints = 0m;

		_boxOrdersPlaced = false;
		_weeklyOrdersPlaced = false;

		_currentWeekDate = default;
		_weeklyHigh = 0m;
		_weeklyLow = 0m;
		_weeklyTracking = false;

		_currentDay = default;
		_hasDayData = false;
		_dayOpen = 0m;
		_dayHigh = 0m;
		_dayLow = 0m;
		_prevDayHigh = 0m;
		_prevDayLow = 0m;
		_pendingGapCheck = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// RSI indicator reused across modules.
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var shiftedTime = candle.CloseTime.UtcDateTime + TimeSpan.FromHours(TimeShiftHours);

		// Refresh daily aggregates for gap detection.
		UpdateDailyState(candle, shiftedTime);
		// Try to open gap trades before other logic.
		ProcessGapModule(candle);
		// Handle direct RSI signals and pyramiding.
		ProcessRsiModule(candle, rsiValue);
		// Maintain timed breakout boxes.
		ProcessBoxModule(candle, shiftedTime);
		// Evaluate weekly breakout schedule.
		ProcessWeeklyModule(candle, shiftedTime);
		// Maintain trailing protection for active trades.
		UpdateTrailing(candle);
	}

	private void UpdateDailyState(ICandleMessage candle, DateTime shiftedTime)
	{
		// Track daily OHLC values to emulate MT5 data windows.
		var currentDay = shiftedTime.Date;

		if (!_hasDayData || currentDay != _currentDay)
		{
			if (_hasDayData)
			{
				_prevDayHigh = _dayHigh;
				_prevDayLow = _dayLow;
			}

			_currentDay = currentDay;
			_dayOpen = candle.OpenPrice;
			_dayHigh = candle.HighPrice;
			_dayLow = candle.LowPrice;
			_hasDayData = true;
			_pendingGapCheck = true;
		}
		else
		{
			_dayHigh = Math.Max(_dayHigh, candle.HighPrice);
			_dayLow = Math.Min(_dayLow, candle.LowPrice);
		}
	}

	private void ProcessGapModule(ICandleMessage candle)
	{
		// Detect daily gaps and mirror the EA gap entries.
		if (!EnableGapModule || !_pendingGapCheck || _prevDayHigh <= 0m || _prevDayLow <= 0m)
			return;

		var openPrice = _dayOpen;
		var gapStop = GetPriceOffset(GapStopLossPoints);

		if (openPrice < _prevDayLow && Position <= 0)
		{
			BuyMarket(Volume);
			_lastLongEntryPrice = openPrice;
			_lastShortEntryPrice = null;
			_activeStopPrice = openPrice - gapStop;
			_activeTrailingPoints = GapTrailingPoints;
		}
		else if (openPrice > _prevDayHigh && Position >= 0)
		{
			SellMarket(Volume);
			_lastShortEntryPrice = openPrice;
			_lastLongEntryPrice = null;
			_activeStopPrice = openPrice + gapStop;
			_activeTrailingPoints = GapTrailingPoints;
		}

		_pendingGapCheck = false;
	}

	private void ProcessRsiModule(ICandleMessage candle, decimal rsiValue)
	{
		// Manage RSI cross signals together with pyramiding logic.
		if (!EnableRsiModule || !_rsi.IsFormed)
		{
			_previousRsi = rsiValue;
			_hasPreviousRsi = true;
			return;
		}

		if (!_hasPreviousRsi)
		{
			_previousRsi = rsiValue;
			_hasPreviousRsi = true;
			return;
		}

		var rsiCrossUp = _previousRsi <= RsiLowerLevel && rsiValue > RsiLowerLevel;
		var rsiCrossDown = _previousRsi >= RsiUpperLevel && rsiValue < RsiUpperLevel;

		_previousRsi = rsiValue;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var pyramidStep = GetPriceOffset(RsiPyramidPoints);
		var maxPosition = Volume * RsiMaxEntries;

		if (rsiCrossUp && Position <= 0)
		{
			BuyMarket(Volume);
			_lastLongEntryPrice = candle.ClosePrice;
			_lastShortEntryPrice = null;
			_activeStopPrice = candle.ClosePrice - GetPriceOffset(RsiStopLossPoints);
			_activeTrailingPoints = RsiTrailingPoints;
		}
		else if (rsiCrossDown && Position >= 0)
		{
			SellMarket(Volume);
			_lastShortEntryPrice = candle.ClosePrice;
			_lastLongEntryPrice = null;
			_activeStopPrice = candle.ClosePrice + GetPriceOffset(RsiStopLossPoints);
			_activeTrailingPoints = RsiTrailingPoints;
		}
		else if (Position > 0 && Math.Abs(Position) < maxPosition && _lastLongEntryPrice.HasValue)
		{
			if (candle.ClosePrice >= _lastLongEntryPrice.Value + pyramidStep)
			{
				BuyMarket(Volume);
				_lastLongEntryPrice = candle.ClosePrice;
			}
		}
		else if (Position < 0 && Math.Abs(Position) < maxPosition && _lastShortEntryPrice.HasValue)
		{
			if (candle.ClosePrice <= _lastShortEntryPrice.Value - pyramidStep)
			{
				SellMarket(Volume);
				_lastShortEntryPrice = candle.ClosePrice;
			}
		}
	}

	private void ProcessBoxModule(ICandleMessage candle, DateTime shiftedTime)
	{
		// Run the timed breakout box logic and cleanups.
		if (!EnableBoxModule)
			return;

		var hour = shiftedTime.Hour;

		if (hour == NormalizeHour(0) || hour == NormalizeHour(10) || hour == NormalizeHour(16))
		{
			CancelActiveOrders();
			ClosePosition();
			_boxOrdersPlaced = false;
		}
		else if ((hour == NormalizeHour(6) || hour == NormalizeHour(12) || hour == NormalizeHour(20)) && !_boxOrdersPlaced && Position == 0)
		{
			var upper = candle.HighPrice + GetPriceOffset(BoxEntryPoints);
			var lower = candle.LowPrice - GetPriceOffset(BoxEntryPoints);

			BuyStop(Volume, upper);
			SellStop(Volume, lower);

			_boxOrdersPlaced = true;
			_activeTrailingPoints = BoxTrailingPoints;
		}

		if (Position != 0)
			_boxOrdersPlaced = false;
	}

	private void ProcessWeeklyModule(ICandleMessage candle, DateTime shiftedTime)
	{
		// Build weekly breakout orders with Monday ranges.
		if (!EnableWeeklyModule)
			return;

		var day = shiftedTime.DayOfWeek;
		var hour = shiftedTime.Hour;

		if (day == DayOfWeek.Monday)
		{
			if (!_weeklyTracking || shiftedTime.Date != _currentWeekDate)
			{
				_weeklyTracking = true;
				_currentWeekDate = shiftedTime.Date;
				_weeklyHigh = candle.HighPrice;
				_weeklyLow = candle.LowPrice;
				_weeklyOrdersPlaced = false;
			}
			else
			{
				_weeklyHigh = Math.Max(_weeklyHigh, candle.HighPrice);
				_weeklyLow = Math.Min(_weeklyLow, candle.LowPrice);
			}

			if (hour >= NormalizeHour(StartHour) && hour < NormalizeHour(WeeklySetupEndHour) && !_weeklyOrdersPlaced && Position == 0)
			{
				var buyStop = _weeklyHigh + GetPriceOffset(WeeklyEntryPoints);
				var sellStop = _weeklyLow - GetPriceOffset(WeeklyEntryPoints);

				BuyStop(Volume, buyStop);
				SellStop(Volume, sellStop);

				_weeklyOrdersPlaced = true;
				_activeTrailingPoints = WeeklyTrailingPoints;
			}
		}
		else if (day == DayOfWeek.Friday && hour >= NormalizeHour(18))
		{
			CancelActiveOrders();
			ClosePosition();
			_weeklyOrdersPlaced = false;
		}
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		// Move protective stops only in the profitable direction.
		var position = Position;
		if (position == 0)
		{
			_activeStopPrice = null;
			_activeTrailingPoints = 0m;
			return;
		}

		var trailingDistance = GetPriceOffset(_activeTrailingPoints);
		if (trailingDistance <= 0m)
			return;

		var volume = Math.Abs(position);

		if (position > 0)
		{
			var stopPrice = candle.ClosePrice - trailingDistance;
			if (!_activeStopPrice.HasValue || stopPrice > _activeStopPrice.Value)
			{
				_activeStopPrice = stopPrice;
				SellStop(volume, stopPrice);
			}
		}
		else
		{
			var stopPrice = candle.ClosePrice + trailingDistance;
			if (!_activeStopPrice.HasValue || stopPrice < _activeStopPrice.Value)
			{
				_activeStopPrice = stopPrice;
				BuyStop(volume, stopPrice);
			}
		}
	}

	private int NormalizeHour(int hour)
	{
		// Convert legacy EA schedule hours to shifted session time.
		var normalized = hour + TimeShiftHours;
		while (normalized < 0)
			normalized += 24;
		while (normalized >= 24)
			normalized -= 24;
		return normalized;
	}

	private decimal GetPriceOffset(decimal points)
	{
		// Translate point-based settings into actual price offsets.
		var step = TickSize;
		if (step <= 0m)
			step = 0.0001m;
		return points * step;
	}

	private void ClosePosition()
	{
		// Helper to flatten regardless of current direction.
		if (Position > 0)
		{
			SellMarket(Position);
		}
		else if (Position < 0)
		{
			BuyMarket(-Position);
		}
	}
}
