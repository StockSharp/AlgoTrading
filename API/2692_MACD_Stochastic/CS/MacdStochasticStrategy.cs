using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD strategy with optional stochastic confirmation and timed trading sessions.
/// </summary>
public class MacdStochasticStrategy : Strategy
{
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<bool> _useStochastic;
	private readonly StrategyParam<int> _stochasticBarsToCheck;
	private readonly StrategyParam<int> _stochasticLength;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<int> _noLossStopPips;
	private readonly StrategyParam<int> _whenSetNoLossStopPips;
	private readonly StrategyParam<TimeSpan> _session1Start;
	private readonly StrategyParam<TimeSpan> _session1End;
	private readonly StrategyParam<TimeSpan> _session2Start;
	private readonly StrategyParam<TimeSpan> _session2End;
	private readonly StrategyParam<TimeSpan> _session3Start;
	private readonly StrategyParam<TimeSpan> _session3End;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private Stochastic _stochastic = null!;
	private readonly List<(decimal K, decimal D)> _stochasticHistory = new();
	private decimal _prevMacd;
	private decimal _prevSignal;
	private bool _hasPrevMacd;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _pipSize;
	private DateTimeOffset _lastEntryBarTime;

	/// <summary>
	/// Fast EMA period used inside MACD.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period used inside MACD.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line period of MACD.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Use stochastic oscillator as additional confirmation.
	/// </summary>
	public bool UseStochastic
	{
		get => _useStochastic.Value;
		set => _useStochastic.Value = value;
	}

	/// <summary>
	/// Number of historical bars used for stochastic crossover validation.
	/// </summary>
	public int StochasticBarsToCheck
	{
		get => _stochasticBarsToCheck.Value;
		set => _stochasticBarsToCheck.Value = value;
	}

	/// <summary>
	/// Base length for the stochastic oscillator.
	/// </summary>
	public int StochasticLength
	{
		get => _stochasticLength.Value;
		set => _stochasticLength.Value = value;
	}

	/// <summary>
	/// Smoothing applied to %K line.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// Period used to calculate %D line.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Order volume in lots.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum price move required before updating trailing stop.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneous positions.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Offset applied to break-even stop in pips.
	/// </summary>
	public int NoLossStopPips
	{
		get => _noLossStopPips.Value;
		set => _noLossStopPips.Value = value;
	}

	/// <summary>
	/// Profit required before activating break-even stop in pips.
	/// </summary>
	public int WhenSetNoLossStopPips
	{
		get => _whenSetNoLossStopPips.Value;
		set => _whenSetNoLossStopPips.Value = value;
	}

	/// <summary>
	/// Start time for the first trading session.
	/// </summary>
	public TimeSpan Session1Start
	{
		get => _session1Start.Value;
		set => _session1Start.Value = value;
	}

	/// <summary>
	/// End time for the first trading session.
	/// </summary>
	public TimeSpan Session1End
	{
		get => _session1End.Value;
		set => _session1End.Value = value;
	}

	/// <summary>
	/// Start time for the second trading session.
	/// </summary>
	public TimeSpan Session2Start
	{
		get => _session2Start.Value;
		set => _session2Start.Value = value;
	}

	/// <summary>
	/// End time for the second trading session.
	/// </summary>
	public TimeSpan Session2End
	{
		get => _session2End.Value;
		set => _session2End.Value = value;
	}

	/// <summary>
	/// Start time for the third trading session.
	/// </summary>
	public TimeSpan Session3Start
	{
		get => _session3Start.Value;
		set => _session3Start.Value = value;
	}

	/// <summary>
	/// End time for the third trading session.
	/// </summary>
	public TimeSpan Session3End
	{
		get => _session3End.Value;
		set => _session3End.Value = value;
	}

	/// <summary>
	/// Candle type used for generating signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="MacdStochasticStrategy"/>.
	/// </summary>
	public MacdStochasticStrategy()
	{
		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetDisplay("MACD Fast Period", "Fast EMA length for MACD", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(6, 18, 1);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetDisplay("MACD Slow Period", "Slow EMA length for MACD", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 1);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetDisplay("MACD Signal Period", "Signal line length for MACD", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_useStochastic = Param(nameof(UseStochastic), false)
			.SetDisplay("Use Stochastic Filter", "Enable stochastic confirmation", "Stochastic");

		_stochasticBarsToCheck = Param(nameof(StochasticBarsToCheck), 5)
			.SetDisplay("Stochastic Bars", "History depth for stochastic confirmation", "Stochastic")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(2, 8, 1);

		_stochasticLength = Param(nameof(StochasticLength), 5)
			.SetDisplay("Stochastic Length", "Number of bars for %K calculation", "Stochastic")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(5, 14, 1);

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 3)
			.SetDisplay("Stochastic %K Smoothing", "Smoothing period for %K line", "Stochastic")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(2, 5, 1);

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
			.SetDisplay("Stochastic %D Period", "Smoothing period for %D line", "Stochastic")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(2, 5, 1);

		_volume = Param(nameof(Volume), 1m)
			.SetDisplay("Order Volume", "Trading volume per position", "Trading")
			.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 100)
			.SetDisplay("Stop Loss (pips)", "Initial stop-loss distance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 10);

		_takeProfitPips = Param(nameof(TakeProfitPips), 100)
			.SetDisplay("Take Profit (pips)", "Initial take-profit distance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 10);

		_trailingStopPips = Param(nameof(TrailingStopPips), 0)
			.SetDisplay("Trailing Stop (pips)", "Trailing stop distance", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetDisplay("Trailing Step (pips)", "Minimum move before trailing", "Risk");

		_maxPositions = Param(nameof(MaxPositions), 1)
			.SetDisplay("Max Positions", "Maximum simultaneous positions", "Trading")
			.SetGreaterThanZero();

		_noLossStopPips = Param(nameof(NoLossStopPips), 1)
			.SetDisplay("No Loss Stop (pips)", "Break-even offset for trailing", "Risk");

		_whenSetNoLossStopPips = Param(nameof(WhenSetNoLossStopPips), 25)
			.SetDisplay("Activation Profit (pips)", "Profit before enabling trailing", "Risk");

		_session1Start = Param(nameof(Session1Start), new TimeSpan(8, 15, 0))
			.SetDisplay("Session 1 Start", "Start time of first window", "Sessions");

		_session1End = Param(nameof(Session1End), new TimeSpan(8, 35, 0))
			.SetDisplay("Session 1 End", "End time of first window", "Sessions");

		_session2Start = Param(nameof(Session2Start), new TimeSpan(13, 45, 0))
			.SetDisplay("Session 2 Start", "Start time of second window", "Sessions");

		_session2End = Param(nameof(Session2End), new TimeSpan(14, 42, 0))
			.SetDisplay("Session 2 End", "End time of second window", "Sessions");

		_session3Start = Param(nameof(Session3Start), new TimeSpan(22, 15, 0))
			.SetDisplay("Session 3 Start", "Start time of third window", "Sessions");

		_session3End = Param(nameof(Session3End), new TimeSpan(22, 45, 0))
			.SetDisplay("Session 3 End", "End time of third window", "Sessions");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for analysis", "General");

		ResetState();
	}

	/// <summary>
	/// Securities required by the strategy.
	/// </summary>
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <summary>
	/// Reset cached state when strategy is reset.
	/// </summary>
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	/// <summary>
	/// Start indicator subscriptions and chart visualization.
	/// </summary>
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastPeriod },
				LongMa = { Length = MacdSlowPeriod }
			},
			SignalMa = { Length = MacdSignalPeriod }
		};

		_stochastic = new Stochastic
		{
			Length = StochasticLength,
			KPeriod = StochasticKPeriod,
			DPeriod = StochasticDPeriod
		};

		UpdatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_macd, _stochastic, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Position == 0 && _entryPrice != 0m)
			ResetPositionState();

		if (_pipSize == 0m)
			UpdatePipSize();

		ManagePosition(candle);

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
			return;

		decimal? currentK = null;
		decimal? currentD = null;

		var stochasticTyped = (StochasticValue)stochasticValue;
		if (stochasticTyped.K is decimal kValue && stochasticTyped.D is decimal dValue)
		{
			currentK = kValue;
			currentD = dValue;
			UpdateStochasticHistory(kValue, dValue);
		}

		var allowTrading = IsFormedAndOnlineAndAllowTrading() && Volume > 0m && MaxPositions > 0;
		var macdCrossUp = _hasPrevMacd && _prevMacd <= _prevSignal && macd > signal && macd < 0m && _prevMacd < 0m;
		var macdCrossDown = _hasPrevMacd && _prevMacd >= _prevSignal && macd < signal && macd > 0m && _prevMacd > 0m;

		if (allowTrading && Position == 0 && candle.OpenTime > _lastEntryBarTime && IsWithinTradingSession(candle.OpenTime))
		{
			if (macdCrossUp && PassesStochasticFilter(true, currentK, currentD))
			{
				EnterLong(candle);
			}
			else if (macdCrossDown && PassesStochasticFilter(false, currentK, currentD))
			{
				EnterShort(candle);
			}
		}

		_prevMacd = macd;
		_prevSignal = signal;
		_hasPrevMacd = true;
	}

	private void EnterLong(ICandleMessage candle)
	{
		// Open long position using close price of finished candle.
		BuyMarket(Volume);
		_entryPrice = candle.ClosePrice;
		_stopPrice = StopLossPips > 0 && _pipSize > 0m ? _entryPrice - StopLossPips * _pipSize : 0m;
		_takePrice = TakeProfitPips > 0 && _pipSize > 0m ? _entryPrice + TakeProfitPips * _pipSize : 0m;
		_lastEntryBarTime = candle.OpenTime;
	}

	private void EnterShort(ICandleMessage candle)
	{
		// Open short position using close price of finished candle.
		SellMarket(Volume);
		_entryPrice = candle.ClosePrice;
		_stopPrice = StopLossPips > 0 && _pipSize > 0m ? _entryPrice + StopLossPips * _pipSize : 0m;
		_takePrice = TakeProfitPips > 0 && _pipSize > 0m ? _entryPrice - TakeProfitPips * _pipSize : 0m;
		_lastEntryBarTime = candle.OpenTime;
	}

	private void ManagePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			UpdateLongTrailing(candle);
			CheckLongExits(candle);
		}
		else if (Position < 0)
		{
			UpdateShortTrailing(candle);
			CheckShortExits(candle);
		}
	}

	private void CheckLongExits(ICandleMessage candle)
	{
		// Close long position if stop or take profit levels are reached.
		if (_stopPrice > 0m && candle.LowPrice <= _stopPrice)
		{
			SellMarket(Position);
			ResetPositionState();
			return;
		}

		if (_takePrice > 0m && candle.HighPrice >= _takePrice)
		{
			SellMarket(Position);
			ResetPositionState();
		}
	}

	private void CheckShortExits(ICandleMessage candle)
	{
		// Close short position if stop or take profit levels are reached.
		if (_stopPrice > 0m && candle.HighPrice >= _stopPrice)
		{
			BuyMarket(-Position);
			ResetPositionState();
			return;
		}

		if (_takePrice > 0m && candle.LowPrice <= _takePrice)
		{
			BuyMarket(-Position);
			ResetPositionState();
		}
	}

	private void UpdateLongTrailing(ICandleMessage candle)
	{
		// Move long stop towards break-even based on trailing parameters.
		if (TrailingStopPips <= 0 || _pipSize <= 0m || _stopPrice <= 0m)
			return;

		var profit = candle.ClosePrice - _entryPrice;
		if (profit <= WhenSetNoLossStopPips * _pipSize)
			return;

		var newStop = _stopPrice + TrailingStopPips * _pipSize;
		var minStop = _entryPrice + NoLossStopPips * _pipSize;
		var maxStop = candle.ClosePrice - (TrailingStepPips + TrailingStopPips) * _pipSize;

		if (newStop <= _stopPrice)
			return;

		if (newStop <= minStop)
			return;

		if (newStop >= maxStop)
			return;

		_stopPrice = newStop;
	}

	private void UpdateShortTrailing(ICandleMessage candle)
	{
		// Move short stop towards break-even based on trailing parameters.
		if (TrailingStopPips <= 0 || _pipSize <= 0m)
			return;

		var profit = _entryPrice - candle.ClosePrice;
		if (profit <= WhenSetNoLossStopPips * _pipSize)
			return;

		if (_stopPrice > 0m)
		{
			var newStop = _stopPrice - TrailingStopPips * _pipSize;
			var maxStop = _entryPrice - NoLossStopPips * _pipSize;
			var minStop = candle.ClosePrice + (TrailingStepPips + TrailingStopPips) * _pipSize;

			if (newStop >= _stopPrice)
				return;

			if (newStop >= maxStop)
				return;

			if (newStop <= minStop)
				return;

			_stopPrice = newStop;
		}
		else
		{
			var candidate = _entryPrice - NoLossStopPips * _pipSize;
			var threshold = candle.ClosePrice + WhenSetNoLossStopPips * _pipSize;

			if (candidate <= 0m)
				return;

			if (candidate <= threshold)
				return;

			_stopPrice = candidate;
		}
	}

	private bool PassesStochasticFilter(bool isBuy, decimal? currentK, decimal? currentD)
	{
		// Validate stochastic crossover when the filter is enabled.
		if (!UseStochastic)
			return true;

		if (currentK is null || currentD is null)
			return false;

		var bars = Math.Max(1, StochasticBarsToCheck);
		if (_stochasticHistory.Count < bars)
			return false;

		if (bars <= 1)
			return isBuy ? currentD < currentK : currentD > currentK;

		var (oldK, oldD) = _stochasticHistory[0];
		return isBuy ? currentD < currentK && oldD > oldK : currentD > currentK && oldD < oldK;
	}

	private void UpdateStochasticHistory(decimal k, decimal d)
	{
		// Maintain rolling history for stochastic confirmation.
		var max = Math.Max(1, StochasticBarsToCheck);
		_stochasticHistory.Add((k, d));
		while (_stochasticHistory.Count > max)
			_stochasticHistory.RemoveAt(0);
	}

	private bool IsWithinTradingSession(DateTimeOffset time)
	{
		// Check whether local time is inside any allowed window.
		var tod = time.LocalDateTime.TimeOfDay;
		return IsWithinSession(tod, Session1Start, Session1End)
			|| IsWithinSession(tod, Session2Start, Session2End)
			|| IsWithinSession(tod, Session3Start, Session3End);
	}

	private static bool IsWithinSession(TimeSpan time, TimeSpan start, TimeSpan end)
	{
		if (start == end && start == TimeSpan.Zero)
			return false;

		return start <= end
			? time >= start && time <= end
			: time >= start || time <= end;
	}

	private void UpdatePipSize()
	{
		// Convert pip-based settings to price values using security price step.
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		{
			_pipSize = 0m;
			return;
		}

		var ratio = 1m / priceStep;
		var digits = (int)Math.Round(Math.Log10((double)ratio));
		_pipSize = digits == 3 || digits == 5 ? priceStep * 10m : priceStep;
	}

	private void ResetState()
	{
		// Clear cached values when strategy is reset or initialized.
		_stochasticHistory.Clear();
		_prevMacd = 0m;
		_prevSignal = 0m;
		_hasPrevMacd = false;
		ResetPositionState();
		_pipSize = 0m;
		_lastEntryBarTime = DateTimeOffset.MinValue;
	}

	private void ResetPositionState()
	{
		// Reset position-specific tracking variables.
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
	}
}
