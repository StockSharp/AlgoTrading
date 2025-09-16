using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MaCrossoverMultiTimeframeStrategy : Strategy
{
	private readonly StrategyParam<TradeDirectionOption> _allowedDirection;
	private readonly StrategyParam<bool> _closeOnCross;
	private readonly StrategyParam<MovingAverageTypeOption> _maType;
	private readonly StrategyParam<int> _currentPeriod;
	private readonly StrategyParam<int> _previousPeriodAdd;
	private readonly StrategyParam<int> _currentShift;
	private readonly StrategyParam<int> _previousShift;
	private readonly StrategyParam<DataType> _currentCandleType;
	private readonly StrategyParam<DataType> _previousCandleType;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _trailingStopPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DayOfWeek> _startDay;
	private readonly StrategyParam<DayOfWeek> _endDay;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _endTime;
	private readonly StrategyParam<bool> _closeOnMinEquity;
	private readonly StrategyParam<decimal> _minimumEquityPercent;

	private LengthIndicator<decimal> _currentMaIndicator;
	private LengthIndicator<decimal> _previousMaIndicator;

	private readonly Queue<decimal> _currentShiftBuffer = new();
	private readonly Queue<decimal> _previousShiftBuffer = new();

	private decimal? _currentMaValue;
	private decimal? _previousMaValue;
	private bool? _wasCurrentAbovePrevious;

	private decimal _entryPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;
	private decimal _previousPosition;
	private decimal? _initialPortfolioValue;

	/// <summary>
	/// Allowed trade direction.
	/// </summary>
	public TradeDirectionOption AllowedDirection
	{
		get => _allowedDirection.Value;
		set => _allowedDirection.Value = value;
	}

	/// <summary>
	/// Close opposite positions when a crossover happens.
	/// </summary>
	public bool ClosePositionsOnCross
	{
		get => _closeOnCross.Value;
		set => _closeOnCross.Value = value;
	}

	/// <summary>
	/// Moving average calculation type.
	/// </summary>
	public MovingAverageTypeOption MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Period for the current timeframe moving average.
	/// </summary>
	public int CurrentMaPeriod
	{
		get => _currentPeriod.Value;
		set => _currentPeriod.Value = value;
	}

	/// <summary>
	/// Additional length added to the previous moving average.
	/// </summary>
	public int PreviousPeriodAddition
	{
		get => _previousPeriodAdd.Value;
		set => _previousPeriodAdd.Value = value;
	}

	/// <summary>
	/// Shift applied to the current moving average.
	/// </summary>
	public int CurrentShift
	{
		get => _currentShift.Value;
		set => _currentShift.Value = value;
	}

	/// <summary>
	/// Shift applied to the previous moving average.
	/// </summary>
	public int PreviousShift
	{
		get => _previousShift.Value;
		set => _previousShift.Value = value;
	}

	/// <summary>
	/// Candle type for the current moving average.
	/// </summary>
	public DataType CurrentCandleType
	{
		get => _currentCandleType.Value;
		set => _currentCandleType.Value = value;
	}

	/// <summary>
	/// Candle type for the previous moving average.
	/// </summary>
	public DataType PreviousCandleType
	{
		get => _previousCandleType.Value;
		set => _previousCandleType.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage relative to the entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Trailing stop percentage.
	/// </summary>
	public decimal TrailingStopPercent
	{
		get => _trailingStopPercent.Value;
		set => _trailingStopPercent.Value = value;
	}

	/// <summary>
	/// Take-profit percentage relative to the entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// First trading day of the schedule.
	/// </summary>
	public DayOfWeek StartDay
	{
		get => _startDay.Value;
		set => _startDay.Value = value;
	}

	/// <summary>
	/// Last trading day of the schedule.
	/// </summary>
	public DayOfWeek EndDay
	{
		get => _endDay.Value;
		set => _endDay.Value = value;
	}

	/// <summary>
	/// Start time of the trading window.
	/// </summary>
	public TimeSpan StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// End time of the trading window.
	/// </summary>
	public TimeSpan EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	/// <summary>
	/// Close all positions when the equity guard is triggered.
	/// </summary>
	public bool ClosePositionsOnMinEquity
	{
		get => _closeOnMinEquity.Value;
		set => _closeOnMinEquity.Value = value;
	}

	/// <summary>
	/// Minimum equity percentage relative to the initial portfolio value.
	/// </summary>
	public decimal MinimumEquityPercent
	{
		get => _minimumEquityPercent.Value;
		set => _minimumEquityPercent.Value = value;
	}

	/// <summary>
	/// Period calculated for the previous moving average.
	/// </summary>
	public int PreviousMaPeriod => Math.Max(1, CurrentMaPeriod + PreviousPeriodAddition);

	/// <summary>
	/// Initializes the strategy parameters.
	/// </summary>
	public MaCrossoverMultiTimeframeStrategy()
	{
		Volume = 1;

		_allowedDirection = Param(nameof(AllowedDirection), TradeDirectionOption.LongAndShort)
			.SetDisplay("Trade Direction", "Allowed direction for opening positions", "Trading");

		_closeOnCross = Param(nameof(ClosePositionsOnCross), true)
			.SetDisplay("Close on Cross", "Close existing opposite positions when moving averages cross", "Trading");

		_maType = Param(nameof(MaType), MovingAverageTypeOption.Exponential)
			.SetDisplay("MA Type", "Moving average calculation method", "Indicators");

		_currentPeriod = Param(nameof(CurrentMaPeriod), 42)
			.SetGreaterThanZero()
			.SetDisplay("Current MA Period", "Length of the faster moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 120, 5);

		_previousPeriodAdd = Param(nameof(PreviousPeriodAddition), 10)
			.SetGreaterOrEqual(0)
			.SetDisplay("Previous MA Extra Length", "Additional length added to the slower moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0, 50, 5);

		_currentShift = Param(nameof(CurrentShift), 0)
			.SetGreaterOrEqual(0)
			.SetDisplay("Current MA Shift", "Number of bars to shift the faster moving average", "Indicators");

		_previousShift = Param(nameof(PreviousShift), 2)
			.SetGreaterOrEqual(0)
			.SetDisplay("Previous MA Shift", "Number of bars to shift the slower moving average", "Indicators");

		_currentCandleType = Param(nameof(CurrentCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Current Candle", "Timeframe used for the faster moving average", "Data");

		_previousCandleType = Param(nameof(PreviousCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Previous Candle", "Timeframe used for the slower moving average", "Data");

		_stopLossPercent = Param(nameof(StopLossPercent), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss %", "Stop-loss percentage from the entry price", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 10m, 1m);

		_trailingStopPercent = Param(nameof(TrailingStopPercent), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Stop %", "Trailing stop percentage applied to the best price", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 10m, 1m);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit %", "Take-profit percentage from the entry price", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 20m, 1m);

		_startDay = Param(nameof(StartDay), DayOfWeek.Monday)
			.SetDisplay("Start Day", "First day when trading is allowed", "Schedule");

		_endDay = Param(nameof(EndDay), DayOfWeek.Friday)
			.SetDisplay("End Day", "Last day when trading is allowed", "Schedule");

		_startTime = Param(nameof(StartTime), TimeSpan.Zero)
			.SetDisplay("Start Time", "Daily time when the strategy begins trading", "Schedule");

		_endTime = Param(nameof(EndTime), new TimeSpan(23, 59, 0))
			.SetDisplay("End Time", "Daily time when the strategy stops opening new trades", "Schedule");

		_closeOnMinEquity = Param(nameof(ClosePositionsOnMinEquity), true)
			.SetDisplay("Close on Equity Guard", "Close positions when equity drops below the threshold", "Risk");

		_minimumEquityPercent = Param(nameof(MinimumEquityPercent), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Minimum Equity %", "Minimum equity percentage relative to the initial value", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 100m, 5m);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
			yield break;

		yield return (Security, CurrentCandleType);
		if (!Equals(PreviousCandleType, CurrentCandleType))
			yield return (Security, PreviousCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_currentMaIndicator?.Reset();
		_previousMaIndicator?.Reset();

		_currentShiftBuffer.Clear();
		_previousShiftBuffer.Clear();

		_currentMaValue = null;
		_previousMaValue = null;
		_wasCurrentAbovePrevious = null;

		ResetPositionState();
		_previousPosition = 0m;
		_initialPortfolioValue = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_initialPortfolioValue = Portfolio?.CurrentValue;
		// Remember the starting equity for the guard logic.

		_currentMaIndicator = CreateMovingAverage(MaType, CurrentMaPeriod);
		_previousMaIndicator = CreateMovingAverage(MaType, PreviousMaPeriod);

		var currentSubscription = SubscribeCandles(CurrentCandleType);
		// Bind the fast moving average to the current timeframe.
		currentSubscription.Bind(_currentMaIndicator, OnCurrentCandle).Start();

		var previousSubscription = SubscribeCandles(PreviousCandleType);
		// Bind the slow moving average to the configured timeframe.
		previousSubscription.Bind(_previousMaIndicator, OnPreviousCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, currentSubscription);
			DrawIndicator(area, _currentMaIndicator);
			DrawIndicator(area, _previousMaIndicator);
			DrawOwnTrades(area);
		}
	}

	private void OnCurrentCandle(ICandleMessage candle, decimal maValue)
	{
		// Process only completed candles to avoid premature reactions.
		if (candle.State != CandleStates.Finished)
			return;

		if (!_currentMaIndicator.IsFormed)
			return;

		var shifted = ApplyShift(CurrentShift, _currentShiftBuffer, maValue);
		if (shifted == null)
			return;

		_currentMaValue = shifted;

		if (!CheckFreeEquityGuard())
			return;

		ManagePosition(candle);
		TryProcessSignal(candle);
	}

	private void OnPreviousCandle(ICandleMessage candle, decimal maValue)
	{
		// Update the reference moving average from the second timeframe.
		if (candle.State != CandleStates.Finished)
			return;

		if (!_previousMaIndicator.IsFormed)
			return;

		var shifted = ApplyShift(PreviousShift, _previousShiftBuffer, maValue);
		if (shifted == null)
			return;

		_previousMaValue = shifted;
	}

	private void TryProcessSignal(ICandleMessage candle)
	{
		// Ensure that both moving averages are available and trading is allowed.
		if (_currentMaValue == null || _previousMaValue == null)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!IsWithinTradingWindow(candle.OpenTime))
			return;

		var isCurrentAbove = _currentMaValue.Value > _previousMaValue.Value;

		if (_wasCurrentAbovePrevious == null)
		{
			_wasCurrentAbovePrevious = isCurrentAbove;
			return;
		}

		if (_wasCurrentAbovePrevious == isCurrentAbove)
			return;

		if (isCurrentAbove)
		{
			HandleBullishCross(candle);
		}
		else
		{
			HandleBearishCross(candle);
		}

		_wasCurrentAbovePrevious = isCurrentAbove;
	}

	private void HandleBullishCross(ICandleMessage candle)
	{
		// Prevent duplicate entries and respect direction filters.
		if (!IsLongAllowed())
			return;

		if (Position > 0)
			return;

		var volume = Volume;
		if (volume <= 0m)
			volume = 1m;

		if (Position < 0)
		{
			if (!ClosePositionsOnCross)
				return;

			volume += Math.Abs(Position);
		}

		BuyMarket(volume);
		LogInfo($"Bullish crossover detected at {candle.ClosePrice:0.#####}. Fast MA = {_currentMaValue:0.#####}, Slow MA = {_previousMaValue:0.#####}.");
	}

	private void HandleBearishCross(ICandleMessage candle)
	{
		// Prevent duplicate entries and respect direction filters.
		if (!IsShortAllowed())
			return;

		if (Position < 0)
			return;

		var volume = Volume;
		if (volume <= 0m)
			volume = 1m;

		if (Position > 0)
		{
			if (!ClosePositionsOnCross)
				return;

			volume += Math.Abs(Position);
		}

		SellMarket(volume);
		LogInfo($"Bearish crossover detected at {candle.ClosePrice:0.#####}. Fast MA = {_currentMaValue:0.#####}, Slow MA = {_previousMaValue:0.#####}.");
	}

	private void ManagePosition(ICandleMessage candle)
	{
		// Translate percentage-based risk settings into market exits.
		if (Position == 0 || _entryPrice <= 0m)
			return;

		var stopLoss = StopLossPercent / 100m;
		var takeProfit = TakeProfitPercent / 100m;
		var trailing = TrailingStopPercent / 100m;
		var closePrice = candle.ClosePrice;

		if (Position > 0)
		{
			if (closePrice > _highestPrice)
				_highestPrice = closePrice;

			if (stopLoss > 0m)
			{
				var stopPrice = _entryPrice * (1m - stopLoss);
				if (closePrice <= stopPrice)
				{
					SellMarket(Math.Abs(Position));
					LogInfo($"Long stop-loss triggered at {closePrice:0.#####}. Entry {_entryPrice:0.#####}, stop {stopPrice:0.#####}.");
					return;
				}
			}

			if (takeProfit > 0m)
			{
				var targetPrice = _entryPrice * (1m + takeProfit);
				if (closePrice >= targetPrice)
				{
					SellMarket(Math.Abs(Position));
					LogInfo($"Long take-profit triggered at {closePrice:0.#####}. Entry {_entryPrice:0.#####}, target {targetPrice:0.#####}.");
					return;
				}
			}

			if (trailing > 0m && _highestPrice > 0m)
			{
				var trailingPrice = _highestPrice * (1m - trailing);
				if (closePrice <= trailingPrice)
				{
					SellMarket(Math.Abs(Position));
					LogInfo($"Long trailing stop triggered at {closePrice:0.#####}. Trail {trailingPrice:0.#####}.");
					return;
				}
			}
		}
		else if (Position < 0)
		{
			if (_lowestPrice == 0m || closePrice < _lowestPrice)
				_lowestPrice = closePrice;

			if (stopLoss > 0m)
			{
				var stopPrice = _entryPrice * (1m + stopLoss);
				if (closePrice >= stopPrice)
				{
					BuyMarket(Math.Abs(Position));
					LogInfo($"Short stop-loss triggered at {closePrice:0.#####}. Entry {_entryPrice:0.#####}, stop {stopPrice:0.#####}.");
					return;
				}
			}

			if (takeProfit > 0m)
			{
				var targetPrice = _entryPrice * (1m - takeProfit);
				if (closePrice <= targetPrice)
				{
					BuyMarket(Math.Abs(Position));
					LogInfo($"Short take-profit triggered at {closePrice:0.#####}. Entry {_entryPrice:0.#####}, target {targetPrice:0.#####}.");
					return;
				}
			}

			if (trailing > 0m && _lowestPrice > 0m)
			{
				var trailingPrice = _lowestPrice * (1m + trailing);
				if (closePrice >= trailingPrice)
				{
					BuyMarket(Math.Abs(Position));
					LogInfo($"Short trailing stop triggered at {closePrice:0.#####}. Trail {trailingPrice:0.#####}.");
					return;
				}
			}
		}
	}

	private bool CheckFreeEquityGuard()
	{
		// Abort new trades if the equity guard has been triggered.
		var threshold = MinimumEquityPercent;
		if (threshold <= 0m)
			return true;

		if (_initialPortfolioValue == null || _initialPortfolioValue <= 0m)
			return true;

		var currentValue = Portfolio?.CurrentValue;
		if (currentValue == null)
			return true;

		var minimumEquity = _initialPortfolioValue.Value * (threshold / 100m);
		if (currentValue.Value > minimumEquity)
			return true;

		LogInfo($"Equity guard triggered. Current value {currentValue.Value:0.##}, minimum allowed {minimumEquity:0.##}.");

		if (ClosePositionsOnMinEquity && Position != 0)
		{
			CloseAllPositions();
		}

		return false;
	}

	private void CloseAllPositions()
	{
		// Exit using market orders because the protection stays hidden.
		if (Position > 0)
			SellMarket(Math.Abs(Position));
		else if (Position < 0)
			BuyMarket(Math.Abs(Position));
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var day = time.DayOfWeek;
		var startDay = StartDay;
		var endDay = EndDay;

		var withinDays = startDay <= endDay
			? day >= startDay && day <= endDay
			: day >= startDay || day <= endDay;

		if (!withinDays)
			return false;

		var startTime = StartTime;
		var endTime = EndTime;
		var timeOfDay = time.TimeOfDay;

		return startTime <= endTime
			? timeOfDay >= startTime && timeOfDay <= endTime
			: timeOfDay >= startTime || timeOfDay <= endTime;
	}

	private static decimal? ApplyShift(int shift, Queue<decimal> buffer, decimal value)
	{
		// Maintain a small buffer to emulate the MQL shift parameter.
		if (shift <= 0)
		{
			buffer.Clear();
			return value;
		}

		buffer.Enqueue(value);

		while (buffer.Count > shift + 1)
			buffer.Dequeue();

		return buffer.Count == shift + 1 ? buffer.Peek() : null;
	}

	private static LengthIndicator<decimal> CreateMovingAverage(MovingAverageTypeOption type, int length)
	{
		return type switch
		{
			MovingAverageTypeOption.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageTypeOption.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageTypeOption.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageTypeOption.Weighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	private bool IsLongAllowed() => AllowedDirection != TradeDirectionOption.ShortOnly;

	private bool IsShortAllowed() => AllowedDirection != TradeDirectionOption.LongOnly;

	private void ResetPositionState()
	{
		_entryPrice = 0m;
		_highestPrice = 0m;
		_lowestPrice = 0m;
		_previousPosition = 0m;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		// Update the average entry price once fills arrive.
		base.OnNewMyTrade(trade);

		if (trade.Order.Security != Security)
			return;

		var currentPosition = Position;

		if (_previousPosition <= 0m && currentPosition > 0m)
		{
			_entryPrice = trade.Trade.Price;
			_highestPrice = trade.Trade.Price;
			_lowestPrice = trade.Trade.Price;
		}
		else if (_previousPosition >= 0m && currentPosition < 0m)
		{
			_entryPrice = trade.Trade.Price;
			_highestPrice = trade.Trade.Price;
			_lowestPrice = trade.Trade.Price;
		}

		if (currentPosition > 0m && trade.Order.Direction == Sides.Buy)
		{
			var totalVolume = Math.Abs(currentPosition);
			var previousVolume = Math.Abs(_previousPosition > 0m ? _previousPosition : 0m);
			var tradeVolume = trade.Trade.Volume;
			if (totalVolume > 0m)
			{
				var weighted = (_entryPrice * previousVolume) + (trade.Trade.Price * tradeVolume);
				_entryPrice = weighted / totalVolume;
			}

			if (trade.Trade.Price > _highestPrice)
				_highestPrice = trade.Trade.Price;
		}
		else if (currentPosition < 0m && trade.Order.Direction == Sides.Sell)
		{
			var totalVolume = Math.Abs(currentPosition);
			var previousVolume = Math.Abs(_previousPosition < 0m ? _previousPosition : 0m);
			var tradeVolume = trade.Trade.Volume;
			if (totalVolume > 0m)
			{
				var weighted = (_entryPrice * previousVolume) + (trade.Trade.Price * tradeVolume);
				_entryPrice = weighted / totalVolume;
			}

			if (_lowestPrice == 0m || trade.Trade.Price < _lowestPrice)
				_lowestPrice = trade.Trade.Price;
		}

		_previousPosition = currentPosition;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
			ResetPositionState();
	}

	public enum TradeDirectionOption
	{
		LongOnly,
		ShortOnly,
		LongAndShort
	}

	public enum MovingAverageTypeOption
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted
	}
}