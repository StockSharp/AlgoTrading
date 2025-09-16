using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD breakout strategy with optional stochastic confirmation and MetaTrader-style trailing.
/// Converted from the "LCS-MACD-Trader" MetaTrader 4 expert advisor.
/// </summary>
public class LcsMacdTraderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<bool> _useStochasticFilter;
	private readonly StrategyParam<int> _barsToCheckStochastic;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSlowing;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _maxOrders;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<int> _trailingActivationPips;
	private readonly StrategyParam<int> _trailingDistancePips;
	private readonly StrategyParam<int> _breakEvenActivationPips;
	private readonly StrategyParam<int> _breakEvenOffsetPips;
	private readonly StrategyParam<TimeSpan> _session1Start;
	private readonly StrategyParam<TimeSpan> _session1End;
	private readonly StrategyParam<TimeSpan> _session2Start;
	private readonly StrategyParam<TimeSpan> _session2End;
	private readonly StrategyParam<TimeSpan> _session3Start;
	private readonly StrategyParam<TimeSpan> _session3End;

	private decimal _pipSize;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longTakeProfitPrice;
	private decimal? _shortTakeProfitPrice;
	private bool _longBreakEvenActivated;
	private bool _shortBreakEvenActivated;
	private decimal? _prevMacdValue;
	private decimal? _prevSignalValue;
	private int _barsSinceDAboveK;
	private int _barsSinceDBelowK;

	/// <summary>
	/// Initializes a new instance of the <see cref="LcsMacdTraderStrategy"/> class.
	/// </summary>
	public LcsMacdTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for calculations", "General");

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period of the MACD", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(6, 18, 2);

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period of the MACD", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 2);

		_signalPeriod = Param(nameof(SignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Period", "Signal line period of the MACD", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 2);

		_useStochasticFilter = Param(nameof(UseStochasticFilter), true)
			.SetDisplay("Use Stochastic", "Require stochastic confirmation", "Filters");

		_barsToCheckStochastic = Param(nameof(BarsToCheckStochastic), 5)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Lookback", "Maximum bars since previous stochastic crossover", "Filters")
			.SetRange(1, 20);

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("%K Period", "Length of the %K line", "Stochastic")
			.SetRange(1, 50);

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("%D Period", "Length of the %D smoothing", "Stochastic")
			.SetRange(1, 50);

		_stochasticSlowing = Param(nameof(StochasticSlowing), 3)
			.SetGreaterThanZero()
			.SetDisplay("Slowing", "Additional smoothing applied to %K", "Stochastic")
			.SetRange(1, 50);

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Lot size used for each entry", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.1m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 100)
			.SetDisplay("Take Profit", "Take-profit distance in pips", "Risk management")
			.SetRange(0, 1000);

		_stopLossPips = Param(nameof(StopLossPips), 100)
			.SetDisplay("Stop Loss", "Stop-loss distance in pips", "Risk management")
			.SetRange(0, 1000);

		_maxOrders = Param(nameof(MaxOrders), 5)
			.SetGreaterThanZero()
			.SetDisplay("Max Orders", "Maximum stacked entries per direction", "Trading")
			.SetRange(1, 20);

		_enableTrailing = Param(nameof(EnableTrailing), false)
			.SetDisplay("Enable Trailing", "Activate MetaTrader-style trailing", "Risk management");

		_trailingActivationPips = Param(nameof(TrailingActivationPips), 50)
			.SetDisplay("Trailing Activation", "Profit (pips) before trailing activates", "Risk management")
			.SetRange(0, 1000);

		_trailingDistancePips = Param(nameof(TrailingDistancePips), 25)
			.SetDisplay("Trailing Distance", "Distance (pips) maintained by the trailing stop", "Risk management")
			.SetRange(0, 1000);

		_breakEvenActivationPips = Param(nameof(BreakEvenActivationPips), 25)
			.SetDisplay("Break-even Trigger", "Profit (pips) required to move stop to break-even", "Risk management")
			.SetRange(0, 1000);

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 1)
			.SetDisplay("Break-even Offset", "Additional pips added above break-even", "Risk management")
			.SetRange(0, 1000);

		_session1Start = Param(nameof(Session1Start), TimeSpan.Parse("08:15"))
			.SetDisplay("Session 1 Start", "First trading window start", "Sessions");

		_session1End = Param(nameof(Session1End), TimeSpan.Parse("08:35"))
			.SetDisplay("Session 1 End", "First trading window end", "Sessions");

		_session2Start = Param(nameof(Session2Start), TimeSpan.Parse("13:45"))
			.SetDisplay("Session 2 Start", "Second trading window start", "Sessions");

		_session2End = Param(nameof(Session2End), TimeSpan.Parse("14:42"))
			.SetDisplay("Session 2 End", "Second trading window end", "Sessions");

		_session3Start = Param(nameof(Session3Start), TimeSpan.Parse("22:15"))
			.SetDisplay("Session 3 Start", "Third trading window start", "Sessions");

		_session3End = Param(nameof(Session3End), TimeSpan.Parse("22:45"))
			.SetDisplay("Session 3 End", "Third trading window end", "Sessions");

		ResetInternalState();
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Fast EMA period of the MACD.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period of the MACD.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// Signal line period of the MACD.
	/// </summary>
	public int SignalPeriod
	{
		get => _signalPeriod.Value;
		set => _signalPeriod.Value = value;
	}

	/// <summary>
	/// Require stochastic confirmation for entries.
	/// </summary>
	public bool UseStochasticFilter
	{
		get => _useStochasticFilter.Value;
		set => _useStochasticFilter.Value = value;
	}

	/// <summary>
	/// Maximum candles since previous opposite stochastic relation.
	/// </summary>
	public int BarsToCheckStochastic
	{
		get => _barsToCheckStochastic.Value;
		set => _barsToCheckStochastic.Value = value;
	}

	/// <summary>
	/// Lookback period of the stochastic %K line.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing length of the %D line.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Additional smoothing applied to %K.
	/// </summary>
	public int StochasticSlowing
	{
		get => _stochasticSlowing.Value;
		set => _stochasticSlowing.Value = value;
	}

	/// <summary>
	/// Volume used for each entry.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Maximum number of stacked entries per direction.
	/// </summary>
	public int MaxOrders
	{
		get => _maxOrders.Value;
		set => _maxOrders.Value = value;
	}

	/// <summary>
	/// Enable MetaTrader-style trailing stop logic.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Profit threshold that activates the trailing stop.
	/// </summary>
	public int TrailingActivationPips
	{
		get => _trailingActivationPips.Value;
		set => _trailingActivationPips.Value = value;
	}

	/// <summary>
	/// Distance maintained by the trailing stop.
	/// </summary>
	public int TrailingDistancePips
	{
		get => _trailingDistancePips.Value;
		set => _trailingDistancePips.Value = value;
	}

	/// <summary>
	/// Profit threshold that moves the stop-loss to break-even.
	/// </summary>
	public int BreakEvenActivationPips
	{
		get => _breakEvenActivationPips.Value;
		set => _breakEvenActivationPips.Value = value;
	}

	/// <summary>
	/// Additional pips added when break-even stop is placed.
	/// </summary>
	public int BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	/// <summary>
	/// Start of the first trading session.
	/// </summary>
	public TimeSpan Session1Start
	{
		get => _session1Start.Value;
		set => _session1Start.Value = value;
	}

	/// <summary>
	/// End of the first trading session.
	/// </summary>
	public TimeSpan Session1End
	{
		get => _session1End.Value;
		set => _session1End.Value = value;
	}

	/// <summary>
	/// Start of the second trading session.
	/// </summary>
	public TimeSpan Session2Start
	{
		get => _session2Start.Value;
		set => _session2Start.Value = value;
	}

	/// <summary>
	/// End of the second trading session.
	/// </summary>
	public TimeSpan Session2End
	{
		get => _session2End.Value;
		set => _session2End.Value = value;
	}

	/// <summary>
	/// Start of the third trading session.
	/// </summary>
	public TimeSpan Session3Start
	{
		get => _session3Start.Value;
		set => _session3Start.Value = value;
	}

	/// <summary>
	/// End of the third trading session.
	/// </summary>
	public TimeSpan Session3End
	{
		get => _session3End.Value;
		set => _session3End.Value = value;
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
		ResetInternalState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastEmaPeriod },
				LongMa = { Length = SlowEmaPeriod },
			},
			SignalMa = { Length = SignalPeriod }
		};

		var stochastic = new StochasticOscillator
		{
			Length = StochasticKPeriod,
			K = { Length = StochasticSlowing },
			D = { Length = StochasticDPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		EnsurePipSize();

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macdLine = macdTyped.Macd;
		var signalLine = macdTyped.Signal;

		decimal currentK = 0m;
		decimal currentD = 0m;
		var hasStochastic = false;

		if (stochasticValue is StochasticOscillatorValue stoch && stoch.K is decimal kValue && stoch.D is decimal dValue)
		{
			hasStochastic = true;
			currentK = kValue;
			currentD = dValue;
			UpdateStochasticCounters(currentK, currentD);
		}
		else
		{
			if (UseStochasticFilter)
				return;
			ResetStochasticCounters();
		}

		HandleRiskManagement(candle);

		var macdCrossUp = false;
		var macdCrossDown = false;

		if (_prevMacdValue is decimal prevMacd && _prevSignalValue is decimal prevSignal)
		{
			macdCrossUp = macdLine > signalLine && prevMacd <= prevSignal && macdLine < 0m && prevMacd < 0m;
			macdCrossDown = macdLine < signalLine && prevMacd >= prevSignal && macdLine > 0m && prevMacd > 0m;
		}

		var timeAllowed = IsWithinTradingSessions(candle.CloseTime);
		var volumeUnit = NormalizeVolume(TradeVolume);
		var maxPosition = MaxOrders * volumeUnit;

		if (volumeUnit <= 0m)
			return;

		var lookbackLimit = Math.Max(1, BarsToCheckStochastic);
		var longConfirmed = !UseStochasticFilter || (hasStochastic && currentD < currentK && _barsSinceDAboveK < lookbackLimit);
		var shortConfirmed = !UseStochasticFilter || (hasStochastic && currentD > currentK && _barsSinceDBelowK < lookbackLimit);

		if (timeAllowed && macdCrossUp && longConfirmed && Position >= 0m && Position < maxPosition)
		{
			var allowedVolume = Math.Min(volumeUnit, maxPosition - Position);
			if (allowedVolume > 0m)
			{
				var previousPosition = Position;
				BuyMarket(allowedVolume);
				InitializeLongPosition(candle.ClosePrice, previousPosition, allowedVolume);
				ResetShortState();
				LogInfo($"Long entry at {candle.ClosePrice:F5}. MACD crossed above signal below zero.");
			}
		}
		else if (timeAllowed && macdCrossDown && shortConfirmed && Position <= 0m && Math.Abs(Position) < maxPosition)
		{
			var allowedVolume = Math.Min(volumeUnit, maxPosition - Math.Abs(Position));
			if (allowedVolume > 0m)
			{
				var previousPosition = Position;
				SellMarket(allowedVolume);
				InitializeShortPosition(candle.ClosePrice, previousPosition, allowedVolume);
				ResetLongState();
				LogInfo($"Short entry at {candle.ClosePrice:F5}. MACD crossed below signal above zero.");
			}
		}

		_prevMacdValue = macdLine;
		_prevSignalValue = signalLine;
	}

	private void HandleRiskManagement(ICandleMessage candle)
	{
		if (Position > 0m && _longEntryPrice is decimal)
		{
			UpdateLongRiskLevels(candle);
			if (ShouldExitLong(candle))
			{
				var volume = Math.Abs(Position);
				SellMarket(volume);
				LogInfo($"Exit long at {candle.ClosePrice:F5} due to risk control.");
				ResetLongState();
			}
		}
		else if (Position <= 0m)
		{
			ResetLongState();
		}

		if (Position < 0m && _shortEntryPrice is decimal)
		{
			UpdateShortRiskLevels(candle);
			if (ShouldExitShort(candle))
			{
				var volume = Math.Abs(Position);
				BuyMarket(volume);
				LogInfo($"Exit short at {candle.ClosePrice:F5} due to risk control.");
				ResetShortState();
			}
		}
		else if (Position >= 0m)
		{
			ResetShortState();
		}
	}

	private bool ShouldExitLong(ICandleMessage candle)
	{
		if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
		{
			LogInfo($"Long stop triggered at {stop:F5}.");
			return true;
		}

		if (_longTakeProfitPrice is decimal target && candle.HighPrice >= target)
		{
			LogInfo($"Long take-profit reached at {target:F5}.");
			return true;
		}

		return false;
	}

	private bool ShouldExitShort(ICandleMessage candle)
	{
		if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
		{
			LogInfo($"Short stop triggered at {stop:F5}.");
			return true;
		}

		if (_shortTakeProfitPrice is decimal target && candle.LowPrice <= target)
		{
			LogInfo($"Short take-profit reached at {target:F5}.");
			return true;
		}

		return false;
	}

	private void UpdateLongRiskLevels(ICandleMessage candle)
	{
		if (_longEntryPrice is not decimal entry)
			return;

		var breakEvenActivation = ConvertPipsToPrice(BreakEvenActivationPips);
		var breakEvenOffset = ConvertPipsToPrice(BreakEvenOffsetPips);

		if (!_longBreakEvenActivated && breakEvenActivation > 0m && candle.HighPrice >= entry + breakEvenActivation)
		{
			var newStop = entry + breakEvenOffset;
			if (_longStopPrice is not decimal current || newStop > current)
			{
				_longStopPrice = newStop;
				LogInfo($"Long break-even activated at {newStop:F5}.");
			}
			_longBreakEvenActivated = true;
		}

		if (!EnableTrailing)
			return;

		var trailingDistance = ConvertPipsToPrice(TrailingDistancePips);
		var trailingActivation = ConvertPipsToPrice(TrailingActivationPips);

		if (trailingDistance <= 0m)
			return;

		if (candle.HighPrice >= entry + trailingActivation)
		{
			var candidate = candle.ClosePrice - trailingDistance;
			if (_longStopPrice is not decimal current || candidate > current)
			{
				_longStopPrice = candidate;
				LogInfo($"Long trailing stop moved to {candidate:F5}.");
			}
		}
	}

	private void UpdateShortRiskLevels(ICandleMessage candle)
	{
		if (_shortEntryPrice is not decimal entry)
			return;

		var breakEvenActivation = ConvertPipsToPrice(BreakEvenActivationPips);
		var breakEvenOffset = ConvertPipsToPrice(BreakEvenOffsetPips);

		if (!_shortBreakEvenActivated && breakEvenActivation > 0m && candle.LowPrice <= entry - breakEvenActivation)
		{
			var newStop = entry - breakEvenOffset;
			if (_shortStopPrice is not decimal current || newStop < current)
			{
				_shortStopPrice = newStop;
				LogInfo($"Short break-even activated at {newStop:F5}.");
			}
			_shortBreakEvenActivated = true;
		}

		if (!EnableTrailing)
			return;

		var trailingDistance = ConvertPipsToPrice(TrailingDistancePips);
		var trailingActivation = ConvertPipsToPrice(TrailingActivationPips);

		if (trailingDistance <= 0m)
			return;

		if (candle.LowPrice <= entry - trailingActivation)
		{
			var candidate = candle.ClosePrice + trailingDistance;
			if (_shortStopPrice is not decimal current || candidate < current)
			{
				_shortStopPrice = candidate;
				LogInfo($"Short trailing stop moved to {candidate:F5}.");
			}
		}
	}

	private void InitializeLongPosition(decimal price, decimal previousPosition, decimal addedVolume)
	{
		var newPosition = previousPosition + addedVolume;
		if (newPosition <= 0m)
			return;

		if (previousPosition > 0m && _longEntryPrice is decimal existing)
		{
			var weightedSum = existing * previousPosition + price * addedVolume;
			_longEntryPrice = weightedSum / newPosition;
		}
		else
		{
			_longEntryPrice = price;
		}

		var stopDistance = ConvertPipsToPrice(StopLossPips);
		_longStopPrice = stopDistance > 0m ? _longEntryPrice.Value - stopDistance : null;

		var targetDistance = ConvertPipsToPrice(TakeProfitPips);
		_longTakeProfitPrice = targetDistance > 0m ? _longEntryPrice.Value + targetDistance : null;

		_longBreakEvenActivated = false;
	}

	private void InitializeShortPosition(decimal price, decimal previousPosition, decimal addedVolume)
	{
		var newPosition = Math.Abs(previousPosition) + addedVolume;
		if (newPosition <= 0m)
			return;

		if (previousPosition < 0m && _shortEntryPrice is decimal existing)
		{
			var weightedSum = existing * Math.Abs(previousPosition) + price * addedVolume;
			_shortEntryPrice = weightedSum / newPosition;
		}
		else
		{
			_shortEntryPrice = price;
		}

		var stopDistance = ConvertPipsToPrice(StopLossPips);
		_shortStopPrice = stopDistance > 0m ? _shortEntryPrice.Value + stopDistance : null;

		var targetDistance = ConvertPipsToPrice(TakeProfitPips);
		_shortTakeProfitPrice = targetDistance > 0m ? _shortEntryPrice.Value - targetDistance : null;

		_shortBreakEvenActivated = false;
	}

	private void ResetInternalState()
	{
		_pipSize = 0m;
		ResetLongState();
		ResetShortState();
		_prevMacdValue = null;
		_prevSignalValue = null;
		ResetStochasticCounters();
	}

	private void ResetLongState()
	{
		if (Position > 0m)
			return;

		_longEntryPrice = null;
		_longStopPrice = null;
		_longTakeProfitPrice = null;
		_longBreakEvenActivated = false;
	}

	private void ResetShortState()
	{
		if (Position < 0m)
			return;

		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
		_shortBreakEvenActivated = false;
	}

	private void UpdateStochasticCounters(decimal currentK, decimal currentD)
	{
		if (currentD > currentK)
		{
			_barsSinceDAboveK = 0;
		}
		else if (_barsSinceDAboveK < int.MaxValue)
		{
			_barsSinceDAboveK++;
		}

		if (currentD < currentK)
		{
			_barsSinceDBelowK = 0;
		}
		else if (_barsSinceDBelowK < int.MaxValue)
		{
			_barsSinceDBelowK++;
		}
	}

	private void ResetStochasticCounters()
	{
		_barsSinceDAboveK = int.MaxValue;
		_barsSinceDBelowK = int.MaxValue;
	}

	private decimal ConvertPipsToPrice(int pips)
	{
		if (pips <= 0)
			return 0m;

		EnsurePipSize();
		return pips * _pipSize;
	}

	private void EnsurePipSize()
	{
		if (_pipSize <= 0m)
			_pipSize = CalculatePipSize();
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			step = 0.0001m;

		var current = step;
		var digits = 0;

		while (current < 1m && digits < 10)
		{
			current *= 10m;
			digits++;
		}

		return digits == 3 || digits == 5 ? step * 10m : step;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var step = Security?.VolumeStep ?? 0m;
		if (step <= 0m)
			return volume;

		var steps = Math.Floor(volume / step);
		if (steps < 1m)
			steps = 1m;

		return steps * step;
	}

	private bool IsWithinTradingSessions(DateTimeOffset time)
	{
		var tod = time.TimeOfDay;
		return IsWithinSession(Session1Start, Session1End, tod)
			|| IsWithinSession(Session2Start, Session2End, tod)
			|| IsWithinSession(Session3Start, Session3End, tod);
	}

	private static bool IsWithinSession(TimeSpan start, TimeSpan end, TimeSpan current)
	{
		if (start == TimeSpan.Zero && end == TimeSpan.Zero)
			return false;

		if (start <= end)
			return current >= start && current <= end;

		return current >= start || current <= end;
	}
}
