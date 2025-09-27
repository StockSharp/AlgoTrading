using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// BreakOut15 strategy converted from MetaTrader 4.
/// Implements moving average crossover filtering combined with breakout entries and tiered trailing stops.
/// </summary>
public class BreakOut15Strategy : Strategy
{
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<decimal> _tradeSizePercent;
	private readonly StrategyParam<decimal> _fixedVolume;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<TrailingStopMode> _trailingStopType;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _level1TriggerPips;
	private readonly StrategyParam<decimal> _level1StopPips;
	private readonly StrategyParam<decimal> _level2TriggerPips;
	private readonly StrategyParam<decimal> _level2StopPips;
	private readonly StrategyParam<decimal> _level3TriggerPips;
	private readonly StrategyParam<decimal> _level3TrailingPips;
	private readonly StrategyParam<MovingAverageMethod> _fastMethod;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _fastShift;
	private readonly StrategyParam<AppliedPrice> _fastPriceType;
	private readonly StrategyParam<MovingAverageMethod> _slowMethod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _slowShift;
	private readonly StrategyParam<int> _signalBarShift;
	private readonly StrategyParam<AppliedPrice> _slowPriceType;
	private readonly StrategyParam<decimal> _breakoutLevelPips;
	private readonly StrategyParam<bool> _useTimeLimit;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _stopHour;
	private readonly StrategyParam<bool> _useFridayClose;
	private readonly StrategyParam<int> _fridayCloseHour;
	private readonly StrategyParam<decimal> _minimumEquity;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _fastHistory = new();
	private readonly List<decimal> _slowHistory = new();

	private IndicatorBase<decimal> _fastIndicator = null!;
	private IndicatorBase<decimal> _slowIndicator = null!;
	private decimal? _pendingLongBreakout;
	private decimal? _pendingShortBreakout;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakeProfitPrice;
	private decimal? _shortTakeProfitPrice;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;


	/// <summary>
	/// Initializes a new instance of the <see cref="BreakOut15Strategy"/> class.
	/// </summary>
	public BreakOut15Strategy()
	{
		_useMoneyManagement = Param(nameof(UseMoneyManagement), true)
			.SetDisplay("Use Money Management", "Enable dynamic volume calculation", "Trading");

		_tradeSizePercent = Param(nameof(TradeSizePercent), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Percent", "Account percent allocated per trade", "Trading");

		_fixedVolume = Param(nameof(FixedVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Fixed Volume", "Fallback position size when money management is disabled", "Trading");

		_maxVolume = Param(nameof(MaxVolume), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Max Volume", "Upper bound for calculated volume", "Trading");

		_takeProfitPips = Param(nameof(TakeProfitPips), 0m)
			.SetDisplay("Take Profit (pips)", "Profit target distance in pips", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 60m)
			.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk");

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
			.SetDisplay("Use Trailing Stop", "Activate trailing stop logic", "Risk");

		_trailingStopType = Param(nameof(TrailingStopType), TrailingStopMode.Delayed)
			.SetDisplay("Trailing Stop Type", "Select trailing stop behavior", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 45m)
			.SetDisplay("Trailing Stop (pips)", "Distance used by the delayed trailing stop", "Risk");

		_level1TriggerPips = Param(nameof(Level1TriggerPips), 20m)
			.SetDisplay("Level 1 Trigger", "Price move required for the first trailing adjustment", "Risk");

		_level1StopPips = Param(nameof(Level1StopPips), 15m)
			.SetDisplay("Level 1 Stop", "Stop distance applied after the first trigger", "Risk");

		_level2TriggerPips = Param(nameof(Level2TriggerPips), 30m)
			.SetDisplay("Level 2 Trigger", "Price move required for the second trailing adjustment", "Risk");

		_level2StopPips = Param(nameof(Level2StopPips), 20m)
			.SetDisplay("Level 2 Stop", "Stop distance applied after the second trigger", "Risk");

		_level3TriggerPips = Param(nameof(Level3TriggerPips), 40m)
			.SetDisplay("Level 3 Trigger", "Price move required before switching to dynamic trailing", "Risk");

		_level3TrailingPips = Param(nameof(Level3TrailingPips), 20m)
			.SetDisplay("Level 3 Trailing", "Distance maintained once the third level is active", "Risk");

		_fastMethod = Param(nameof(FastMethod), MovingAverageMethod.Exponential)
			.SetDisplay("Fast MA Method", "Calculation method for the fast average", "Indicators");

		_fastPeriod = Param(nameof(FastPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Period", "Length of the fast moving average", "Indicators");

		_fastShift = Param(nameof(FastShift), 0)
			.SetDisplay("Fast MA Shift", "Bar shift applied to the fast average", "Indicators");

		_fastPriceType = Param(nameof(FastPriceType), AppliedPrice.Close)
			.SetDisplay("Fast MA Price", "Applied price for the fast average", "Indicators");

		_slowMethod = Param(nameof(SlowMethod), MovingAverageMethod.Exponential)
			.SetDisplay("Slow MA Method", "Calculation method for the slow average", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 80)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Period", "Length of the slow moving average", "Indicators");

		_slowShift = Param(nameof(SlowShift), 0)
			.SetDisplay("Slow MA Shift", "Bar shift applied to the slow average", "Indicators");
		_signalBarShift = Param(nameof(SignalBarShift), 1)
			.SetDisplay("Signal Bar Shift", "Offset applied when evaluating breakout signals", "Indicators")
			.SetNotNegative();

		_slowPriceType = Param(nameof(SlowPriceType), AppliedPrice.Close)
			.SetDisplay("Slow MA Price", "Applied price for the slow average", "Indicators");

		_breakoutLevelPips = Param(nameof(BreakoutLevelPips), 35m)
			.SetGreaterThanZero()
			.SetDisplay("Breakout Level (pips)", "Distance added to create the breakout entry price", "Trading");

		_useTimeLimit = Param(nameof(UseTimeLimit), true)
			.SetDisplay("Use Trading Hours", "Restrict trading to a time window", "Session");

		_startHour = Param(nameof(StartHour), 7)
			.SetDisplay("Start Hour", "Hour of day when trading is allowed", "Session");

		_stopHour = Param(nameof(StopHour), 16)
			.SetDisplay("Stop Hour", "Hour of day when new trades are blocked", "Session");

		_useFridayClose = Param(nameof(UseFridayClose), false)
			.SetDisplay("Friday Close All", "Close positions late on Friday", "Session");

		_fridayCloseHour = Param(nameof(FridayCloseHour), 20)
			.SetDisplay("Friday Close Hour", "Hour when positions are liquidated on Friday", "Session");

		_minimumEquity = Param(nameof(MinimumEquity), 800m)
			.SetDisplay("Minimum Equity", "Disable trading when equity falls below this level", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used by the strategy", "Data");
	}

	/// <summary>
	/// Toggle money management logic.
	/// </summary>
	public bool UseMoneyManagement
	{
		get => _useMoneyManagement.Value;
		set => _useMoneyManagement.Value = value;
	}

	/// <summary>
	/// Risk allocation in percent when money management is enabled.
	/// </summary>
	public decimal TradeSizePercent
	{
		get => _tradeSizePercent.Value;
		set => _tradeSizePercent.Value = Math.Max(0.01m, value);
	}

	/// <summary>
	/// Fixed fallback volume.
	/// </summary>
	public decimal FixedVolume
	{
		get => _fixedVolume.Value;
		set => _fixedVolume.Value = Math.Max(0.0001m, value);
	}

	/// <summary>
	/// Maximum allowable position size.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = Math.Max(0.0001m, value);
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = Math.Max(0m, value);
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = Math.Max(0m, value);
	}

	/// <summary>
	/// Indicates whether trailing stops are enabled.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing stop behavior type.
	/// </summary>
	public TrailingStopMode TrailingStopType
	{
		get => _trailingStopType.Value;
		set => _trailingStopType.Value = value;
	}

	/// <summary>
	/// Trailing stop distance used by the delayed mode.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = Math.Max(0m, value);
	}

	/// <summary>
	/// First level trigger distance in pips.
	/// </summary>
	public decimal Level1TriggerPips
	{
		get => _level1TriggerPips.Value;
		set => _level1TriggerPips.Value = Math.Max(0m, value);
	}

	/// <summary>
	/// Stop offset applied after the first trigger.
	/// </summary>
	public decimal Level1StopPips
	{
		get => _level1StopPips.Value;
		set => _level1StopPips.Value = Math.Max(0m, value);
	}

	/// <summary>
	/// Second level trigger distance in pips.
	/// </summary>
	public decimal Level2TriggerPips
	{
		get => _level2TriggerPips.Value;
		set => _level2TriggerPips.Value = Math.Max(0m, value);
	}

	/// <summary>
	/// Stop offset applied after the second trigger.
	/// </summary>
	public decimal Level2StopPips
	{
		get => _level2StopPips.Value;
		set => _level2StopPips.Value = Math.Max(0m, value);
	}

	/// <summary>
	/// Third level trigger distance in pips.
	/// </summary>
	public decimal Level3TriggerPips
	{
		get => _level3TriggerPips.Value;
		set => _level3TriggerPips.Value = Math.Max(0m, value);
	}

	/// <summary>
	/// Trailing distance applied after the third trigger.
	/// </summary>
	public decimal Level3TrailingPips
	{
		get => _level3TrailingPips.Value;
		set => _level3TrailingPips.Value = Math.Max(0m, value);
	}

	/// <summary>
	/// Moving average method for the fast line.
	/// </summary>
	public MovingAverageMethod FastMethod
	{
		get => _fastMethod.Value;
		set => _fastMethod.Value = value;
	}

	/// <summary>
	/// Period for the fast moving average.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = Math.Max(1, value);
	}

	/// <summary>
	/// Shift for the fast moving average output.
	/// </summary>
	public int FastShift
	{
		get => _fastShift.Value;
		set => _fastShift.Value = Math.Max(0, value);
	}

	/// <summary>
	/// Applied price for the fast moving average.
	/// </summary>
	public AppliedPrice FastPriceType
	{
		get => _fastPriceType.Value;
		set => _fastPriceType.Value = value;
	}

	/// <summary>
	/// Moving average method for the slow line.
	/// </summary>
	public MovingAverageMethod SlowMethod
	{
		get => _slowMethod.Value;
		set => _slowMethod.Value = value;
	}

	/// <summary>
	/// Period for the slow moving average.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = Math.Max(1, value);
	}

	/// <summary>
	/// Shift for the slow moving average output.
	/// </summary>
	public int SlowShift
	{
		get => _slowShift.Value;
		set => _slowShift.Value = Math.Max(0, value);
	}
	/// <summary>
	/// Additional bar shift applied when reading indicator values.
	/// </summary>
	public int SignalBarShift
	{
		get => _signalBarShift.Value;
		set => _signalBarShift.Value = value;
	}

	/// <summary>
	/// Applied price for the slow moving average.
	/// </summary>
	public AppliedPrice SlowPriceType
	{
		get => _slowPriceType.Value;
		set => _slowPriceType.Value = value;
	}

	/// <summary>
	/// Breakout distance above/below the market.
	/// </summary>
	public decimal BreakoutLevelPips
	{
		get => _breakoutLevelPips.Value;
		set => _breakoutLevelPips.Value = Math.Max(0m, value);
	}

	/// <summary>
	/// Enable or disable the trading schedule.
	/// </summary>
	public bool UseTimeLimit
	{
		get => _useTimeLimit.Value;
		set => _useTimeLimit.Value = value;
	}

	/// <summary>
	/// Trading start hour.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = Math.Clamp(value, 0, 23);
	}

	/// <summary>
	/// Trading end hour.
	/// </summary>
	public int StopHour
	{
		get => _stopHour.Value;
		set => _stopHour.Value = Math.Clamp(value, 0, 24);
	}

	/// <summary>
	/// Enable automatic Friday position liquidation.
	/// </summary>
	public bool UseFridayClose
	{
		get => _useFridayClose.Value;
		set => _useFridayClose.Value = value;
	}

	/// <summary>
	/// Friday closing hour.
	/// </summary>
	public int FridayCloseHour
	{
		get => _fridayCloseHour.Value;
		set => _fridayCloseHour.Value = Math.Clamp(value, 0, 23);
	}

	/// <summary>
	/// Minimum portfolio equity required for trading.
	/// </summary>
	public decimal MinimumEquity
	{
		get => _minimumEquity.Value;
		set => _minimumEquity.Value = Math.Max(0m, value);
	}

	/// <summary>
	/// Candle type used for signal calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_fastHistory.Clear();
		_slowHistory.Clear();
		_pendingLongBreakout = null;
		_pendingShortBreakout = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakeProfitPrice = null;
		_shortTakeProfitPrice = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastIndicator = CreateMovingAverage(FastMethod, FastPeriod);
		_slowIndicator = CreateMovingAverage(SlowMethod, SlowPeriod);

		_fastHistory.Clear();
		_slowHistory.Clear();
		_pendingLongBreakout = null;
		_pendingShortBreakout = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longTakeProfitPrice = null;
		_shortTakeProfitPrice = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastIndicator);
			DrawIndicator(area, _slowIndicator);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fastInput = GetAppliedPrice(candle, FastPriceType);
		var slowInput = GetAppliedPrice(candle, SlowPriceType);

		var fastValue = ProcessIndicator(_fastIndicator, fastInput, candle.OpenTime);
		var slowValue = ProcessIndicator(_slowIndicator, slowInput, candle.OpenTime);

		if (fastValue is null || slowValue is null)
			return;

		var maxFastHistory = FastShift + SignalBarShift + 3;
		var maxSlowHistory = SlowShift + SignalBarShift + 3;
		UpdateHistory(_fastHistory, fastValue.Value, maxFastHistory);
		UpdateHistory(_slowHistory, slowValue.Value, maxSlowHistory);

		var fastSignal = GetShiftedValue(_fastHistory, FastShift + SignalBarShift);
		var slowSignal = GetShiftedValue(_slowHistory, SlowShift + SignalBarShift);
		if (fastSignal is null || slowSignal is null)
			return;

		var fastSignalValue = fastSignal.Value;
		var slowSignalValue = slowSignal.Value;

		var currentTime = candle.OpenTime;
		var hour = currentTime.Hour;
		var isTradingTime = !UseTimeLimit || (hour >= StartHour && hour < StopHour);

		if (UseFridayClose && currentTime.DayOfWeek == DayOfWeek.Friday && hour >= FridayCloseHour)
		{
			ForceFlat();
			return;
		}

		HandleOpenPositions(candle, fastSignalValue, slowSignalValue);
		HandlePendingBreakouts(candle, isTradingTime, fastSignalValue, slowSignalValue);
	}

	private void HandlePendingBreakouts(ICandleMessage candle, bool isTradingTime, decimal fastSignal, decimal slowSignal)
	{
		var step = GetPriceStep();
		if (step <= 0m)
			return;

		if (_pendingLongBreakout is decimal longBreak)
		{
			var cancel = !isTradingTime || ShouldExitLong(fastSignal, slowSignal);
			if (cancel)
			{
				_pendingLongBreakout = null;
			}
			else if (candle.HighPrice >= longBreak && Position == 0 && IsFormedAndOnlineAndAllowTrading() && IsEquityEnough())
			{
				OpenLongPosition(longBreak, step);
				_pendingLongBreakout = null;
				_pendingShortBreakout = null;
			}
		}

		if (_pendingShortBreakout is decimal shortBreak)
		{
			var cancel = !isTradingTime || ShouldExitShort(fastSignal, slowSignal);
			if (cancel)
			{
				_pendingShortBreakout = null;
			}
			else if (candle.LowPrice <= shortBreak && Position == 0 && IsFormedAndOnlineAndAllowTrading() && IsEquityEnough())
			{
				OpenShortPosition(shortBreak, step);
				_pendingLongBreakout = null;
				_pendingShortBreakout = null;
			}
		}

		if (Position != 0 || !isTradingTime || !IsEquityEnough())
			return;

		if (ShouldEnterLong(fastSignal, slowSignal))
		{
			_pendingLongBreakout = candle.ClosePrice + BreakoutLevelPips * step;
			_pendingShortBreakout = null;
		}
		else if (ShouldEnterShort(fastSignal, slowSignal))
		{
			_pendingShortBreakout = candle.ClosePrice - BreakoutLevelPips * step;
			_pendingLongBreakout = null;
		}
	}

private void HandleOpenPositions(ICandleMessage candle, decimal fastSignal, decimal slowSignal)
{
var step = GetPriceStep();
if (step <= 0m)
return;

if (Position > 0)
{
SyncLongEntry(step);
UpdateLongProtection(candle, step);

var exit = ShouldExitLong(fastSignal, slowSignal);
var stopHit = _longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value;
var takeHit = _longTakeProfitPrice.HasValue && candle.HighPrice >= _longTakeProfitPrice.Value;

			if (exit || stopHit || takeHit)
			{
				SellMarket(Position);
				ResetLongState();
				_pendingLongBreakout = null;
			}
		}
else if (Position < 0)
{
SyncShortEntry(step);
UpdateShortProtection(candle, step);

var exit = ShouldExitShort(fastSignal, slowSignal);
var stopHit = _shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value;
var takeHit = _shortTakeProfitPrice.HasValue && candle.LowPrice <= _shortTakeProfitPrice.Value;

			if (exit || stopHit || takeHit)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				_pendingShortBreakout = null;
			}
		}
	}

	private void OpenLongPosition(decimal entryPrice, decimal step)
	{
		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return;

		BuyMarket(volume);
		_longEntryPrice = entryPrice;
		_longStopPrice = StopLossPips > 0m ? entryPrice - StopLossPips * step : null;
		_longTakeProfitPrice = TakeProfitPips > 0m ? entryPrice + TakeProfitPips * step : null;
	}

	private void OpenShortPosition(decimal entryPrice, decimal step)
	{
		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return;

		SellMarket(volume);
		_shortEntryPrice = entryPrice;
		_shortStopPrice = StopLossPips > 0m ? entryPrice + StopLossPips * step : null;
		_shortTakeProfitPrice = TakeProfitPips > 0m ? entryPrice - TakeProfitPips * step : null;
	}

	private void SyncLongEntry(decimal step)
	{
		if (PositionPrice is decimal positionPrice && positionPrice != 0m)
		{
			if (_longEntryPrice is null || _longEntryPrice.Value != positionPrice)
			{
				_longEntryPrice = positionPrice;
				_longStopPrice = StopLossPips > 0m ? positionPrice - StopLossPips * step : null;
				_longTakeProfitPrice = TakeProfitPips > 0m ? positionPrice + TakeProfitPips * step : null;
			}
		}
	}

	private void SyncShortEntry(decimal step)
	{
		if (PositionPrice is decimal positionPrice && positionPrice != 0m)
		{
			if (_shortEntryPrice is null || _shortEntryPrice.Value != positionPrice)
			{
				_shortEntryPrice = positionPrice;
				_shortStopPrice = StopLossPips > 0m ? positionPrice + StopLossPips * step : null;
				_shortTakeProfitPrice = TakeProfitPips > 0m ? positionPrice - TakeProfitPips * step : null;
			}
		}
	}

	private void UpdateLongProtection(ICandleMessage candle, decimal step)
	{
		if (!UseTrailingStop || _longEntryPrice is null)
			return;

		var entry = _longEntryPrice.Value;
		var current = candle.ClosePrice;

		switch (TrailingStopType)
		{
			case TrailingStopMode.Immediate:
				ApplyLongImmediateTrailing(current, step);
				break;
			case TrailingStopMode.Delayed:
				ApplyLongDelayedTrailing(current, step, entry);
				break;
			case TrailingStopMode.MultiLevel:
				ApplyLongMultiLevelTrailing(entry, current, step);
				break;
		}
	}

	private void ApplyLongImmediateTrailing(decimal currentPrice, decimal step)
	{
		var distance = StopLossPips * step;
		if (distance <= 0m)
			return;

		var newStop = currentPrice - distance;
		if (_longStopPrice is null || newStop > _longStopPrice.Value)
			_longStopPrice = newStop;
	}

	private void ApplyLongDelayedTrailing(decimal currentPrice, decimal step, decimal entry)
	{
		var distance = TrailingStopPips * step;
		if (distance <= 0m)
			return;

		if (currentPrice - entry <= distance)
			return;

		var newStop = currentPrice - distance;
		if (_longStopPrice is null || newStop > _longStopPrice.Value)
			_longStopPrice = newStop;
	}

	private void ApplyLongMultiLevelTrailing(decimal entry, decimal currentPrice, decimal step)
	{
		var level1 = Level1TriggerPips * step;
		var level2 = Level2TriggerPips * step;
		var level3 = Level3TriggerPips * step;

		if (level1 > 0m && currentPrice - entry > level1)
		{
			var stop = entry + (Level1TriggerPips - Level1StopPips) * step;
			if (_longStopPrice is null || stop > _longStopPrice.Value)
				_longStopPrice = stop;
		}

		if (level2 > 0m && currentPrice - entry > level2)
		{
			var stop = entry + (Level2TriggerPips - Level2StopPips) * step;
			if (_longStopPrice is null || stop > _longStopPrice.Value)
				_longStopPrice = stop;
		}

		if (level3 > 0m && currentPrice - entry > level3)
		{
			var stop = currentPrice - Level3TrailingPips * step;
			if (_longStopPrice is null || stop > _longStopPrice.Value)
				_longStopPrice = stop;
		}
	}

	private void UpdateShortProtection(ICandleMessage candle, decimal step)
	{
		if (!UseTrailingStop || _shortEntryPrice is null)
			return;

		var entry = _shortEntryPrice.Value;
		var current = candle.ClosePrice;

		switch (TrailingStopType)
		{
			case TrailingStopMode.Immediate:
				ApplyShortImmediateTrailing(current, step);
				break;
			case TrailingStopMode.Delayed:
				ApplyShortDelayedTrailing(current, step, entry);
				break;
			case TrailingStopMode.MultiLevel:
				ApplyShortMultiLevelTrailing(entry, current, step);
				break;
		}
	}

	private void ApplyShortImmediateTrailing(decimal currentPrice, decimal step)
	{
		var distance = StopLossPips * step;
		if (distance <= 0m)
			return;

		var newStop = currentPrice + distance;
		if (_shortStopPrice is null || newStop < _shortStopPrice.Value)
			_shortStopPrice = newStop;
	}

	private void ApplyShortDelayedTrailing(decimal currentPrice, decimal step, decimal entry)
	{
		var distance = TrailingStopPips * step;
		if (distance <= 0m)
			return;

		if (entry - currentPrice <= distance)
			return;

		var newStop = currentPrice + distance;
		if (_shortStopPrice is null || newStop < _shortStopPrice.Value)
			_shortStopPrice = newStop;
	}

	private void ApplyShortMultiLevelTrailing(decimal entry, decimal currentPrice, decimal step)
	{
		var level1 = Level1TriggerPips * step;
		var level2 = Level2TriggerPips * step;
		var level3 = Level3TriggerPips * step;

		if (level1 > 0m && entry - currentPrice > level1)
		{
			var stop = entry - (Level1TriggerPips - Level1StopPips) * step;
			if (_shortStopPrice is null || stop < _shortStopPrice.Value)
				_shortStopPrice = stop;
		}

		if (level2 > 0m && entry - currentPrice > level2)
		{
			var stop = entry - (Level2TriggerPips - Level2StopPips) * step;
			if (_shortStopPrice is null || stop < _shortStopPrice.Value)
				_shortStopPrice = stop;
		}

		if (level3 > 0m && entry - currentPrice > level3)
		{
			var stop = currentPrice + Level3TrailingPips * step;
			if (_shortStopPrice is null || stop < _shortStopPrice.Value)
				_shortStopPrice = stop;
		}
	}

	private void ResetLongState()
	{
		_longStopPrice = null;
		_longTakeProfitPrice = null;
		_longEntryPrice = null;
	}

	private void ResetShortState()
	{
		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
		_shortEntryPrice = null;
	}

	private void ForceFlat()
	{
		if (Position > 0)
		{
			SellMarket(Position);
			ResetLongState();
		}
		else if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			ResetShortState();
		}

		_pendingLongBreakout = null;
		_pendingShortBreakout = null;
	}

	private static bool ShouldEnterLong(decimal fast, decimal slow)
	{
		return fast > slow;
	}

	private static bool ShouldEnterShort(decimal fast, decimal slow)
	{
		return fast < slow;
	}

	private static bool ShouldExitLong(decimal fast, decimal slow)
	{
		return fast < slow;
	}

	private static bool ShouldExitShort(decimal fast, decimal slow)
	{
		return fast > slow;
	}

	private decimal CalculateOrderVolume()
	{
		decimal lot;

		if (UseMoneyManagement)
		{
			var equity = GetPortfolioEquity();
			if (equity > 0m)
			{
				var raw = Math.Floor(equity * TradeSizePercent / 10000m) / 10m;
				lot = raw >= 1m ? Math.Floor(raw) : 1m;
			}
			else
			{
				lot = FixedVolume;
			}
		}
		else
		{
			lot = FixedVolume;
		}

		if (lot >= 1m)
			lot = Math.Floor(lot);
		else
			lot = 1m;

		return Math.Min(lot, MaxVolume);
	}

	private bool IsEquityEnough()
	{
		var equity = GetPortfolioEquity();
		return equity >= MinimumEquity;
	}

	private decimal GetPortfolioEquity()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return 0m;

		if (portfolio.CurrentValue != null)
			return portfolio.CurrentValue.Value;

		if (portfolio.BeginValue != null)
			return portfolio.BeginValue.Value;

		return 0m;
	}

	private decimal GetPriceStep()
	{
		return Security?.PriceStep ?? 0m;
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPrice priceType)
	{
		return priceType switch
		{
			AppliedPrice.Close => candle.ClosePrice,
			AppliedPrice.Open => candle.OpenPrice,
			AppliedPrice.High => candle.HighPrice,
			AppliedPrice.Low => candle.LowPrice,
			AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPrice.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}

	private static decimal? ProcessIndicator(IndicatorBase<decimal> indicator, decimal input, DateTimeOffset time)
	{
		var value = indicator.Process(new DecimalIndicatorValue(indicator, input)
		{
			Time = time,
			IsFinal = true
		});

		if (!value.IsFinal)
			return null;

		return value.GetValue<decimal>();
	}

	private static void UpdateHistory(List<decimal> history, decimal value, int maxLength)
	{
		history.Add(value);
		while (history.Count > maxLength)
		{
			history.RemoveAt(0);
		}
	}

	private static decimal? GetShiftedValue(List<decimal> history, int shift)
	{
		var index = history.Count - 1 - shift;
		return index >= 0 ? history[index] : null;
	}

	private static IndicatorBase<decimal> CreateMovingAverage(MovingAverageMethod method, int length)
	{
		return method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageMethod.LinearWeighted => new WeightedMovingAverage { Length = length },
			MovingAverageMethod.LeastSquares => new LinearRegression { Length = length },
			_ => new SimpleMovingAverage { Length = length }
		};
	}

	/// <summary>
	/// Supported moving average calculation methods.
	/// </summary>
	public enum MovingAverageMethod
	{
		/// <summary>Simple moving average.</summary>
		Simple,
		/// <summary>Exponential moving average.</summary>
		Exponential,
		/// <summary>Smoothed moving average.</summary>
		Smoothed,
		/// <summary>Linear weighted moving average.</summary>
		LinearWeighted,
		/// <summary>Least squares moving average.</summary>
		LeastSquares
	}

	/// <summary>
	/// Price selection compatible with MetaTrader applied price modes.
	/// </summary>
	public enum AppliedPrice
	{
		/// <summary>Use the closing price of the candle.</summary>
		Close,
		/// <summary>Use the opening price of the candle.</summary>
		Open,
		/// <summary>Use the highest price of the candle.</summary>
		High,
		/// <summary>Use the lowest price of the candle.</summary>
		Low,
		/// <summary>Use the median price (high + low) / 2.</summary>
		Median,
		/// <summary>Use the typical price (high + low + close) / 3.</summary>
		Typical,
		/// <summary>Use the weighted price (high + low + 2 * close) / 4.</summary>
		Weighted
	}

	/// <summary>
	/// Trailing stop configurations available in the strategy.
	/// </summary>
	public enum TrailingStopMode
	{
		/// <summary>Adjust stops as soon as price moves by the stop-loss distance.</summary>
		Immediate,
		/// <summary>Wait for a predefined profit distance before trailing.</summary>
		Delayed,
		/// <summary>Use multi-level trailing with break-even and profit locking tiers.</summary>
		MultiLevel
	}
}
