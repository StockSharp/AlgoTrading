using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Time based strategy that opens up to two independent sessions with individual direction and risk management.
/// Supports configurable opening windows, optional forced closing windows, pip based stops and trailing logic.
/// </summary>
public class OpenTimeTwoStrategy : Strategy
{
	private const int SecondsInDay = 24 * 60 * 60;

	private readonly StrategyParam<bool> _useClosingWindowOne;
	private readonly StrategyParam<TimeSpan> _closeWindowOneStart;
	private readonly StrategyParam<bool> _useClosingWindowTwo;
	private readonly StrategyParam<TimeSpan> _closeWindowTwoStart;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<bool> _tradeOnMonday;
	private readonly StrategyParam<bool> _tradeOnTuesday;
	private readonly StrategyParam<bool> _tradeOnWednesday;
	private readonly StrategyParam<bool> _tradeOnThursday;
	private readonly StrategyParam<bool> _tradeOnFriday;
	private readonly StrategyParam<TimeSpan> _intervalOneOpenStart;
	private readonly StrategyParam<TimeSpan> _intervalOneOpenEnd;
	private readonly StrategyParam<TimeSpan> _intervalTwoOpenStart;
	private readonly StrategyParam<TimeSpan> _intervalTwoOpenEnd;
	private readonly StrategyParam<TimeSpan> _duration;
	private readonly StrategyParam<bool> _intervalOneBuy;
	private readonly StrategyParam<bool> _intervalTwoBuy;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossOnePips;
	private readonly StrategyParam<decimal> _takeProfitOnePips;
	private readonly StrategyParam<decimal> _stopLossTwoPips;
	private readonly StrategyParam<decimal> _takeProfitTwoPips;
	private readonly StrategyParam<DataType> _candleType;

	private readonly IntervalState _intervalOne = new();
	private readonly IntervalState _intervalTwo = new();

	private decimal _pipSize;

	/// <summary>
	/// Use closing window for the first interval.
	/// </summary>
	public bool UseClosingWindowOne
	{
		get => _useClosingWindowOne.Value;
		set => _useClosingWindowOne.Value = value;
	}

	/// <summary>
	/// Closing window start time for the first interval.
	/// </summary>
	public TimeSpan CloseWindowOneStart
	{
		get => _closeWindowOneStart.Value;
		set => _closeWindowOneStart.Value = value;
	}

	/// <summary>
	/// Use closing window for the second interval.
	/// </summary>
	public bool UseClosingWindowTwo
	{
		get => _useClosingWindowTwo.Value;
		set => _useClosingWindowTwo.Value = value;
	}

	/// <summary>
	/// Closing window start time for the second interval.
	/// </summary>
	public TimeSpan CloseWindowTwoStart
	{
		get => _closeWindowTwoStart.Value;
		set => _closeWindowTwoStart.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step distance in pips.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Enable trading on Monday.
	/// </summary>
	public bool TradeOnMonday
	{
		get => _tradeOnMonday.Value;
		set => _tradeOnMonday.Value = value;
	}

	/// <summary>
	/// Enable trading on Tuesday.
	/// </summary>
	public bool TradeOnTuesday
	{
		get => _tradeOnTuesday.Value;
		set => _tradeOnTuesday.Value = value;
	}

	/// <summary>
	/// Enable trading on Wednesday.
	/// </summary>
	public bool TradeOnWednesday
	{
		get => _tradeOnWednesday.Value;
		set => _tradeOnWednesday.Value = value;
	}

	/// <summary>
	/// Enable trading on Thursday.
	/// </summary>
	public bool TradeOnThursday
	{
		get => _tradeOnThursday.Value;
		set => _tradeOnThursday.Value = value;
	}

	/// <summary>
	/// Enable trading on Friday.
	/// </summary>
	public bool TradeOnFriday
	{
		get => _tradeOnFriday.Value;
		set => _tradeOnFriday.Value = value;
	}

	/// <summary>
	/// Opening window start for the first interval.
	/// </summary>
	public TimeSpan IntervalOneOpenStart
	{
		get => _intervalOneOpenStart.Value;
		set => _intervalOneOpenStart.Value = value;
	}

	/// <summary>
	/// Opening window end for the first interval.
	/// </summary>
	public TimeSpan IntervalOneOpenEnd
	{
		get => _intervalOneOpenEnd.Value;
		set => _intervalOneOpenEnd.Value = value;
	}

	/// <summary>
	/// Opening window start for the second interval.
	/// </summary>
	public TimeSpan IntervalTwoOpenStart
	{
		get => _intervalTwoOpenStart.Value;
		set => _intervalTwoOpenStart.Value = value;
	}

	/// <summary>
	/// Opening window end for the second interval.
	/// </summary>
	public TimeSpan IntervalTwoOpenEnd
	{
		get => _intervalTwoOpenEnd.Value;
		set => _intervalTwoOpenEnd.Value = value;
	}

	/// <summary>
	/// Extra duration added to each opening and closing window.
	/// </summary>
	public TimeSpan Duration
	{
		get => _duration.Value;
		set => _duration.Value = value;
	}

	/// <summary>
	/// Trade direction for interval one (true for buy, false for sell).
	/// </summary>
	public bool IntervalOneBuy
	{
		get => _intervalOneBuy.Value;
		set => _intervalOneBuy.Value = value;
	}

	/// <summary>
	/// Trade direction for interval two (true for buy, false for sell).
	/// </summary>
	public bool IntervalTwoBuy
	{
		get => _intervalTwoBuy.Value;
		set => _intervalTwoBuy.Value = value;
	}

	/// <summary>
	/// Trade volume for each interval.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance for interval one in pips.
	/// </summary>
	public decimal StopLossOnePips
	{
		get => _stopLossOnePips.Value;
		set => _stopLossOnePips.Value = value;
	}

	/// <summary>
	/// Take profit distance for interval one in pips.
	/// </summary>
	public decimal TakeProfitOnePips
	{
		get => _takeProfitOnePips.Value;
		set => _takeProfitOnePips.Value = value;
	}

	/// <summary>
	/// Stop loss distance for interval two in pips.
	/// </summary>
	public decimal StopLossTwoPips
	{
		get => _stopLossTwoPips.Value;
		set => _stopLossTwoPips.Value = value;
	}

	/// <summary>
	/// Take profit distance for interval two in pips.
	/// </summary>
	public decimal TakeProfitTwoPips
	{
		get => _takeProfitTwoPips.Value;
		set => _takeProfitTwoPips.Value = value;
	}

	/// <summary>
	/// Candle type used as a driver for the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public OpenTimeTwoStrategy()
	{
		_useClosingWindowOne = Param(nameof(UseClosingWindowOne), true)
			.SetDisplay("Close Window #1", "Enable closing window for interval #1", "Closing")
			.SetCanOptimize(true);

		_closeWindowOneStart = Param(nameof(CloseWindowOneStart), new TimeSpan(19, 50, 0))
			.SetDisplay("Close Start #1", "Start time for closing window #1", "Closing");

		_useClosingWindowTwo = Param(nameof(UseClosingWindowTwo), true)
			.SetDisplay("Close Window #2", "Enable closing window for interval #2", "Closing")
			.SetCanOptimize(true);

		_closeWindowTwoStart = Param(nameof(CloseWindowTwoStart), new TimeSpan(23, 20, 0))
			.SetDisplay("Close Start #2", "Start time for closing window #2", "Closing");

		_trailingStopPips = Param(nameof(TrailingStopPips), 30m)
			.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk")
			.SetRange(0m, 500m)
			.SetCanOptimize(true);

		_trailingStepPips = Param(nameof(TrailingStepPips), 3m)
			.SetDisplay("Trailing Step", "Trailing step distance in pips", "Risk")
			.SetRange(0m, 200m)
			.SetCanOptimize(true);

		_tradeOnMonday = Param(nameof(TradeOnMonday), false)
			.SetDisplay("Trade Monday", "Allow trading on Monday", "Schedule")
			.SetCanOptimize(true);

		_tradeOnTuesday = Param(nameof(TradeOnTuesday), false)
			.SetDisplay("Trade Tuesday", "Allow trading on Tuesday", "Schedule")
			.SetCanOptimize(true);

		_tradeOnWednesday = Param(nameof(TradeOnWednesday), false)
			.SetDisplay("Trade Wednesday", "Allow trading on Wednesday", "Schedule")
			.SetCanOptimize(true);

		_tradeOnThursday = Param(nameof(TradeOnThursday), true)
			.SetDisplay("Trade Thursday", "Allow trading on Thursday", "Schedule")
			.SetCanOptimize(true);

		_tradeOnFriday = Param(nameof(TradeOnFriday), false)
			.SetDisplay("Trade Friday", "Allow trading on Friday", "Schedule")
			.SetCanOptimize(true);

		_intervalOneOpenStart = Param(nameof(IntervalOneOpenStart), new TimeSpan(9, 30, 0))
			.SetDisplay("Open Start #1", "Opening window start for interval #1", "Opening");

		_intervalOneOpenEnd = Param(nameof(IntervalOneOpenEnd), new TimeSpan(14, 0, 0))
			.SetDisplay("Open End #1", "Opening window end for interval #1", "Opening");

		_intervalTwoOpenStart = Param(nameof(IntervalTwoOpenStart), new TimeSpan(14, 30, 0))
			.SetDisplay("Open Start #2", "Opening window start for interval #2", "Opening");

		_intervalTwoOpenEnd = Param(nameof(IntervalTwoOpenEnd), new TimeSpan(19, 0, 0))
			.SetDisplay("Open End #2", "Opening window end for interval #2", "Opening");

		_duration = Param(nameof(Duration), TimeSpan.FromSeconds(30))
			.SetDisplay("Window Duration", "Extra duration added to opening/closing windows", "Opening")
			.SetRange(TimeSpan.Zero, TimeSpan.FromHours(1));

		_intervalOneBuy = Param(nameof(IntervalOneBuy), true)
			.SetDisplay("Direction #1", "Trade direction for interval #1 (Buy=true)", "Opening")
			.SetCanOptimize(true);

		_intervalTwoBuy = Param(nameof(IntervalTwoBuy), true)
			.SetDisplay("Direction #2", "Trade direction for interval #2 (Buy=true)", "Opening")
			.SetCanOptimize(true);

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetDisplay("Trade Volume", "Volume for each interval", "Risk")
			.SetRange(0.01m, 100m)
			.SetCanOptimize(true);

		_stopLossOnePips = Param(nameof(StopLossOnePips), 30m)
			.SetDisplay("Stop Loss #1", "Stop loss for interval #1 (pips)", "Risk")
			.SetRange(0m, 1000m)
			.SetCanOptimize(true);

		_takeProfitOnePips = Param(nameof(TakeProfitOnePips), 90m)
			.SetDisplay("Take Profit #1", "Take profit for interval #1 (pips)", "Risk")
			.SetRange(0m, 2000m)
			.SetCanOptimize(true);

		_stopLossTwoPips = Param(nameof(StopLossTwoPips), 10m)
			.SetDisplay("Stop Loss #2", "Stop loss for interval #2 (pips)", "Risk")
			.SetRange(0m, 1000m)
			.SetCanOptimize(true);

		_takeProfitTwoPips = Param(nameof(TakeProfitTwoPips), 35m)
			.SetDisplay("Take Profit #2", "Take profit for interval #2 (pips)", "Risk")
			.SetRange(0m, 2000m)
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Base candle type driving decisions", "General");
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

		ResetInterval(_intervalOne);
		ResetInterval(_intervalTwo);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var decimals = Security?.Decimals ?? 0;
		var adjust = decimals is 3 or 5 ? 10m : 1m;
		var step = Security?.PriceStep ?? 1m;
		_pipSize = step * adjust;

		if (_pipSize <= 0m)
		{
			_pipSize = 1m;
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			return;
		}

		var localTime = candle.OpenTime.ToLocalTime();
		var timeOfDay = localTime.TimeOfDay;

		if (UseClosingWindowOne && IsWithinSimpleWindow(timeOfDay, CloseWindowOneStart, Duration))
		{
			ExitInterval(_intervalOne);
		}

		if (UseClosingWindowTwo && IsWithinSimpleWindow(timeOfDay, CloseWindowTwoStart, Duration))
		{
			ExitInterval(_intervalTwo);
		}

		if (TrailingStopPips > 0m && TrailingStepPips > 0m)
		{
			UpdateTrailingStops(candle);
		}

		CheckRiskControls(_intervalOne, candle);
		CheckRiskControls(_intervalTwo, candle);

		if (!IsTradingDay(localTime.DayOfWeek))
		{
			return;
		}

		var inFirstWindow = IsWithinOpeningWindow(timeOfDay, IntervalOneOpenStart, IntervalOneOpenEnd);
		if (inFirstWindow)
		{
			TryOpenInterval(_intervalOne, IntervalOneBuy, StopLossOnePips, TakeProfitOnePips, candle.ClosePrice);
		}

		var inSecondWindow = IsWithinOpeningWindow(timeOfDay, IntervalTwoOpenStart, IntervalTwoOpenEnd);
		if (inSecondWindow)
		{
			TryOpenInterval(_intervalTwo, IntervalTwoBuy, StopLossTwoPips, TakeProfitTwoPips, candle.ClosePrice);
		}
	}

	private void TryOpenInterval(IntervalState state, bool isBuy, decimal stopLossPips, decimal takeProfitPips, decimal referencePrice)
	{
		if (state.IsActive)
		{
			return;
		}

		if (TradeVolume <= 0m)
		{
			return;
		}

		var direction = isBuy ? 1 : -1;
		var stopDistance = stopLossPips > 0m ? stopLossPips * _pipSize : 0m;
		var takeDistance = takeProfitPips > 0m ? takeProfitPips * _pipSize : 0m;

		decimal? stopPrice = null;
		decimal? takePrice = null;

		if (direction > 0)
		{
			if (stopDistance > 0m)
			{
				stopPrice = referencePrice - stopDistance;
			}

			if (takeDistance > 0m)
			{
				takePrice = referencePrice + takeDistance;
			}
		}
		else
		{
			if (stopDistance > 0m)
			{
				stopPrice = referencePrice + stopDistance;
			}

			if (takeDistance > 0m)
			{
				takePrice = referencePrice - takeDistance;
			}
		}

		state.IsActive = true;
		state.Direction = direction;
		state.EntryPrice = referencePrice;
		state.StopLossPrice = stopPrice;
		state.TakeProfitPrice = takePrice;
		state.TrailingStopPrice = null;

		SyncPosition();
	}

	private void UpdateTrailingStops(ICandleMessage candle)
	{
		var trailingDistance = TrailingStopPips * _pipSize;
		var stepDistance = TrailingStepPips * _pipSize;

		if (trailingDistance <= 0m || stepDistance <= 0m)
		{
			return;
		}

		UpdateTrailingForInterval(_intervalOne, candle, trailingDistance, stepDistance);
		UpdateTrailingForInterval(_intervalTwo, candle, trailingDistance, stepDistance);
	}

	private static void ResetInterval(IntervalState state)
	{
		state.IsActive = false;
		state.Direction = 0;
		state.EntryPrice = 0m;
		state.StopLossPrice = null;
		state.TakeProfitPrice = null;
		state.TrailingStopPrice = null;
	}

	private void UpdateTrailingForInterval(IntervalState state, ICandleMessage candle, decimal trailingDistance, decimal stepDistance)
	{
		if (!state.IsActive)
		{
			return;
		}

		if (state.Direction > 0)
		{
			var profit = candle.ClosePrice - state.EntryPrice;
			if (profit <= trailingDistance + stepDistance)
			{
				return;
			}

			var proposed = candle.ClosePrice - trailingDistance;

			if (state.TrailingStopPrice is null || proposed - state.TrailingStopPrice.Value >= stepDistance)
			{
				state.TrailingStopPrice = state.TrailingStopPrice is null
					? proposed
					: Math.Max(state.TrailingStopPrice.Value, proposed);
			}
		}
		else
		{
			var profit = state.EntryPrice - candle.ClosePrice;
			if (profit <= trailingDistance + stepDistance)
			{
				return;
			}

			var proposed = candle.ClosePrice + trailingDistance;

			if (state.TrailingStopPrice is null || state.TrailingStopPrice.Value - proposed >= stepDistance)
			{
				state.TrailingStopPrice = state.TrailingStopPrice is null
					? proposed
					: Math.Min(state.TrailingStopPrice.Value, proposed);
			}
		}
	}

	private void CheckRiskControls(IntervalState state, ICandleMessage candle)
	{
		if (!state.IsActive)
		{
			return;
		}

		if (state.Direction > 0)
		{
			if (state.StopLossPrice is decimal sl && candle.LowPrice <= sl)
			{
				ExitInterval(state);
				return;
			}

			if (state.TrailingStopPrice is decimal trail && candle.LowPrice <= trail)
			{
				ExitInterval(state);
				return;
			}

			if (state.TakeProfitPrice is decimal tp && candle.HighPrice >= tp)
			{
				ExitInterval(state);
			}
		}
		else
		{
			if (state.StopLossPrice is decimal sl && candle.HighPrice >= sl)
			{
				ExitInterval(state);
				return;
			}

			if (state.TrailingStopPrice is decimal trail && candle.HighPrice >= trail)
			{
				ExitInterval(state);
				return;
			}

			if (state.TakeProfitPrice is decimal tp && candle.LowPrice <= tp)
			{
				ExitInterval(state);
			}
		}
	}

	private void ExitInterval(IntervalState state)
	{
		if (!state.IsActive)
		{
			return;
		}

		ResetInterval(state);
		SyncPosition();
	}

	private void SyncPosition()
	{
		var target = GetTargetPosition();
		var diff = target - Position;

		if (diff == 0m)
		{
			return;
		}

		if (diff > 0m)
		{
			BuyMarket(diff);
		}
		else
		{
			SellMarket(-diff);
		}
	}

	private decimal GetTargetPosition()
	{
		var target = 0m;

		if (_intervalOne.IsActive)
		{
			target += _intervalOne.Direction * TradeVolume;
		}

		if (_intervalTwo.IsActive)
		{
			target += _intervalTwo.Direction * TradeVolume;
		}

		return target;
	}

	private bool IsTradingDay(DayOfWeek day)
	{
		return day switch
		{
			DayOfWeek.Monday => TradeOnMonday,
			DayOfWeek.Tuesday => TradeOnTuesday,
			DayOfWeek.Wednesday => TradeOnWednesday,
			DayOfWeek.Thursday => TradeOnThursday,
			DayOfWeek.Friday => TradeOnFriday,
			_ => false,
		};
	}

	private bool IsWithinOpeningWindow(TimeSpan current, TimeSpan start, TimeSpan end)
	{
		var startSec = ToSeconds(start);
		var endSec = ToSeconds(end);
		var durationSec = ToSeconds(Duration);
		var currentSec = ToSeconds(current);

		if (endSec <= startSec)
		{
			return false;
		}

		var finalEnd = Math.Min(SecondsInDay, endSec + durationSec);
		return currentSec >= startSec && currentSec < finalEnd;
	}

	private bool IsWithinSimpleWindow(TimeSpan current, TimeSpan start, TimeSpan length)
	{
		var startSec = ToSeconds(start);
		var currentSec = ToSeconds(current);
		var lengthSec = Math.Max(0, ToSeconds(length));
		var endSec = startSec + lengthSec;

		if (lengthSec == 0)
		{
			return currentSec == startSec;
		}

		if (endSec <= SecondsInDay)
		{
			return currentSec >= startSec && currentSec < endSec;
		}

		endSec -= SecondsInDay;
		return currentSec >= startSec || currentSec < endSec;
	}

	private static int ToSeconds(TimeSpan time)
	{
		var value = time.TotalSeconds;

		if (value < 0)
		{
			return 0;
		}

		if (value > SecondsInDay)
		{
			return SecondsInDay;
		}

		return (int)Math.Floor(value);
	}

	private sealed class IntervalState
	{
		public bool IsActive;
		public int Direction;
		public decimal EntryPrice;
		public decimal? StopLossPrice;
		public decimal? TakeProfitPrice;
		public decimal? TrailingStopPrice;
	}
}
