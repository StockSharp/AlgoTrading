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
/// Port of the MetaTrader expert "Standard Deviation Channel".
/// Trades breakouts of a volatility channel confirmed by LWMA trend, momentum, and MACD filters.
/// Implements money-management rules including fixed stops, break-even jumps, and trailing exits.
/// </summary>
public class StandardDeviationChannelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<decimal> _deviationMultiplier;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _breakEvenTriggerPips;
	private readonly StrategyParam<decimal> _breakEvenOffsetPips;
	private readonly StrategyParam<decimal> _trailingStartPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<decimal> _maxPositionUnits;

	private LinearWeightedMovingAverage _channelBasis = null!;
	private StandardDeviation _channelDeviation = null!;
	private LinearWeightedMovingAverage _fastTrend = null!;
	private LinearWeightedMovingAverage _slowTrend = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private readonly Queue<decimal> _momentumDiffHistory = new(3);

	private decimal? _previousChannelBasis;
	private decimal? _previousUpperChannel;
	private decimal? _previousLowerChannel;

	private decimal? _macdLine;
	private decimal? _macdSignal;

	private decimal? _lastClose;
	private decimal? _priceStep;

	private decimal? _longStop;
	private decimal? _longTarget;
	private decimal? _longTrailing;
	private bool _longBreakEvenArmed;

	private decimal? _shortStop;
	private decimal? _shortTarget;
	private decimal? _shortTrailing;
	private bool _shortBreakEvenArmed;

	/// <summary>
	/// Initializes strategy parameters with sensible defaults based on the original expert advisor.
	/// </summary>
	public StandardDeviationChannelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used for all indicators.", "General");

		_volume = Param(nameof(TradeVolume), 1m)
		.SetDisplay("Volume", "Base order volume.", "Trading")
		.SetGreaterThanZero();

		_trendLength = Param(nameof(TrendLength), 50)
		.SetDisplay("Trend Length", "Channel lookback period.", "Channel")
		.SetGreaterThanZero();

		_deviationMultiplier = Param(nameof(DeviationMultiplier), 1.5m)
		.SetDisplay("Deviation Mult", "Standard deviation multiplier for channel width.", "Channel")
		.SetGreaterThanZero();

		_fastMaLength = Param(nameof(FastMaLength), 6)
		.SetDisplay("Fast LWMA", "Fast trend filter length.", "Trend")
		.SetGreaterThanZero();

		_slowMaLength = Param(nameof(SlowMaLength), 85)
		.SetDisplay("Slow LWMA", "Slow trend filter length.", "Trend")
		.SetGreaterThanZero();

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
		.SetDisplay("Momentum Period", "Momentum lookback window.", "Momentum")
		.SetGreaterThanZero();

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
		.SetDisplay("Momentum Threshold", "Minimum distance from the neutral 100 level.", "Momentum")
		.SetGreaterThanZero();

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
		.SetDisplay("Take Profit", "Fixed take-profit distance in pips.", "Risk")
		.SetGreaterThan(0m);

		_stopLossPips = Param(nameof(StopLossPips), 20m)
		.SetDisplay("Stop Loss", "Fixed stop-loss distance in pips.", "Risk")
		.SetGreaterThan(0m);

		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 30m)
		.SetDisplay("Break-Even Trigger", "Profit needed before arming the break-even stop.", "Risk")
		.SetGreaterThanOrEqual(0m);

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 5m)
		.SetDisplay("Break-Even Offset", "Extra distance added when jumping the stop to break-even.", "Risk")
		.SetGreaterThanOrEqual(0m);

		_trailingStartPips = Param(nameof(TrailingStartPips), 40m)
		.SetDisplay("Trailing Start", "Profit required before trailing activates.", "Risk")
		.SetGreaterThanOrEqual(0m);

		_trailingStepPips = Param(nameof(TrailingStepPips), 20m)
		.SetDisplay("Trailing Step", "Distance maintained by the trailing stop.", "Risk")
		.SetGreaterThanOrEqual(0m);

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
		.SetDisplay("MACD Fast", "Fast EMA length for MACD.", "MACD")
		.SetGreaterThanZero();

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
		.SetDisplay("MACD Slow", "Slow EMA length for MACD.", "MACD")
		.SetGreaterThanZero();

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
		.SetDisplay("MACD Signal", "Signal line length for MACD.", "MACD")
		.SetGreaterThanZero();

		_maxPositionUnits = Param(nameof(MaxPositionUnits), 1m)
		.SetDisplay("Max Position", "Maximum absolute net position size.", "Trading")
		.SetGreaterThanZero();
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Base order volume submitted when a new signal appears.
	/// </summary>
	public decimal TradeVolume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Lookback length used by the standard deviation channel.
	/// </summary>
	public int TrendLength
	{
		get => _trendLength.Value;
		set => _trendLength.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier controlling the channel width.
	/// </summary>
	public decimal DeviationMultiplier
	{
		get => _deviationMultiplier.Value;
		set => _deviationMultiplier.Value = value;
	}

	/// <summary>
	/// Fast LWMA trend filter length.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Slow LWMA trend filter length.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Momentum indicator lookback period.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum deviation of momentum from the neutral 100 level.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Profit needed before the stop is moved to break-even.
	/// </summary>
	public decimal BreakEvenTriggerPips
	{
		get => _breakEvenTriggerPips.Value;
		set => _breakEvenTriggerPips.Value = value;
	}

	/// <summary>
	/// Extra distance applied when placing the break-even stop.
	/// </summary>
	public decimal BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	/// <summary>
	/// Profit required before the trailing stop becomes active.
	/// </summary>
	public decimal TrailingStartPips
	{
		get => _trailingStartPips.Value;
		set => _trailingStartPips.Value = value;
	}

	/// <summary>
	/// Distance maintained by the trailing stop.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Fast EMA length for the MACD filter.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length for the MACD filter.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal line length for the MACD filter.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Maximum absolute net position maintained by the strategy.
	/// </summary>
	public decimal MaxPositionUnits
	{
		get => _maxPositionUnits.Value;
		set => _maxPositionUnits.Value = value;
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

		ResetState();
		Volume = TradeVolume;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep;

		_channelBasis = new LinearWeightedMovingAverage
		{
			Length = TrendLength,
			CandlePrice = CandlePrice.Typical,
		};

		_channelDeviation = new StandardDeviation
		{
			Length = TrendLength,
			CandlePrice = CandlePrice.Typical,
		};

		_fastTrend = new LinearWeightedMovingAverage
		{
			Length = FastMaLength,
			CandlePrice = CandlePrice.Typical,
		};

		_slowTrend = new LinearWeightedMovingAverage
		{
			Length = SlowMaLength,
			CandlePrice = CandlePrice.Typical,
		};

		_momentum = new Momentum
		{
			Length = MomentumPeriod,
		};

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastPeriod },
				LongMa = { Length = MacdSlowPeriod },
			},
			SignalMa = { Length = MacdSignalPeriod },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _channelBasis);
			DrawIndicator(area, _channelDeviation);
			DrawIndicator(area, _fastTrend);
			DrawIndicator(area, _slowTrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_lastClose = candle.ClosePrice;

		var basisValue = _channelBasis.Process(candle);
		var deviationValue = _channelDeviation.Process(candle);
		var fastValue = _fastTrend.Process(candle);
		var slowValue = _slowTrend.Process(candle);
		var momentumValue = _momentum.Process(candle);
		var macdValue = _macd.Process(candle);

		if (macdValue is MovingAverageConvergenceDivergenceSignalValue macdTyped)
		{
			_macdLine = macdTyped.Macd as decimal?;
			_macdSignal = macdTyped.Signal as decimal?;
		}

		if (!_channelBasis.IsFormed || !_channelDeviation.IsFormed || !_fastTrend.IsFormed || !_slowTrend.IsFormed || !_momentum.IsFormed || !_macd.IsFormed)
		{
			CacheChannel(basisValue.ToDecimal(), deviationValue.ToDecimal());
			UpdateMomentumHistory(momentumValue.ToDecimal());
			return;
		}

		var basis = basisValue.ToDecimal();
		var deviation = deviationValue.ToDecimal();
		var fast = fastValue.ToDecimal();
		var slow = slowValue.ToDecimal();
		var momentum = momentumValue.ToDecimal();

		UpdateMomentumHistory(momentum);

		if (_macdLine is not decimal macdLine || _macdSignal is not decimal macdSignal)
		{
			CacheChannel(basis, deviation);
			return;
		}

		var upper = basis + DeviationMultiplier * deviation;
		var lower = basis - DeviationMultiplier * deviation;

		ManagePosition(candle, upper, lower);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			CacheChannel(basis, deviation);
			return;
		}

		if (TradeVolume <= 0m || MaxPositionUnits <= 0m)
		{
			CacheChannel(basis, deviation);
			return;
		}

		var recentMomentumOk = _momentumDiffHistory.Any(v => v >= MomentumThreshold);

		var channelSlopeUp = _previousUpperChannel is decimal prevUpper && upper > prevUpper;
		var channelSlopeDown = _previousLowerChannel is decimal prevLower && lower < prevLower;

		var canOpenLong = Position < MaxPositionUnits && Position <= 0m;
		var canOpenShort = -Position < MaxPositionUnits && Position >= 0m;

		if (canOpenLong && fast > slow && recentMomentumOk && macdLine >= macdSignal && channelSlopeUp && candle.ClosePrice >= upper)
		{
			var volume = TradeVolume + Math.Max(0m, -Position);
			if (volume > 0m)
			{
				BuyMarket(volume);
				LogInfo($"Long entry: close {candle.ClosePrice:0.#####} above channel upper {upper:0.#####}.");
			}
		}
		else if (canOpenShort && fast < slow && recentMomentumOk && macdLine <= macdSignal && channelSlopeDown && candle.ClosePrice <= lower)
		{
			var volume = TradeVolume + Math.Max(0m, Position);
			if (volume > 0m)
			{
				SellMarket(volume);
				LogInfo($"Short entry: close {candle.ClosePrice:0.#####} below channel lower {lower:0.#####}.");
			}
		}

		CacheChannel(basis, deviation);
	}

	private void ManagePosition(ICandleMessage candle, decimal upper, decimal lower)
	{
		if (Position > 0m)
		{
			var entry = PositionPrice ?? _lastClose;
			if (entry is null)
			return;

			EnsureLongTargets(entry.Value);

			var breakEvenTrigger = GetPriceOffset(BreakEvenTriggerPips);
			if (!_longBreakEvenArmed && breakEvenTrigger > 0m && candle.HighPrice >= entry + breakEvenTrigger)
			{
				var offset = GetPriceOffset(BreakEvenOffsetPips);
				_longStop = entry + offset;
				_longBreakEvenArmed = true;
				LogInfo("Long break-even armed.");
			}

			var trailingStart = GetPriceOffset(TrailingStartPips);
			var trailingStep = GetPriceOffset(TrailingStepPips);
			if (trailingStart > 0m && trailingStep > 0m && candle.HighPrice >= entry + trailingStart)
			{
				var candidate = candle.ClosePrice - trailingStep;
				if (_longTrailing is null || candidate > _longTrailing.Value)
				_longTrailing = candidate;
			}

			if (_longTrailing is decimal trailing && candle.LowPrice <= trailing)
			{
				ClosePosition();
				LogInfo("Long trailing stop hit.");
				ResetPositionState();
				return;
			}

			if (_longStop is decimal stop && candle.LowPrice <= stop)
			{
				ClosePosition();
				LogInfo("Long stop-loss triggered.");
				ResetPositionState();
				return;
			}

			if (_longTarget is decimal target && candle.HighPrice >= target)
			{
				ClosePosition();
				LogInfo("Long take-profit reached.");
				ResetPositionState();
				return;
			}

			if (_previousChannelBasis is decimal prevBasis && candle.ClosePrice < prevBasis && upper <= (_previousUpperChannel ?? upper))
			{
				ClosePosition();
				LogInfo("Long exit: price fell back inside channel.");
				ResetPositionState();
			}
		}
		else if (Position < 0m)
		{
			var entry = PositionPrice ?? _lastClose;
			if (entry is null)
			return;

			EnsureShortTargets(entry.Value);

			var breakEvenTrigger = GetPriceOffset(BreakEvenTriggerPips);
			if (!_shortBreakEvenArmed && breakEvenTrigger > 0m && candle.LowPrice <= entry - breakEvenTrigger)
			{
				var offset = GetPriceOffset(BreakEvenOffsetPips);
				_shortStop = entry - offset;
				_shortBreakEvenArmed = true;
				LogInfo("Short break-even armed.");
			}

			var trailingStart = GetPriceOffset(TrailingStartPips);
			var trailingStep = GetPriceOffset(TrailingStepPips);
			if (trailingStart > 0m && trailingStep > 0m && candle.LowPrice <= entry - trailingStart)
			{
				var candidate = candle.ClosePrice + trailingStep;
				if (_shortTrailing is null || candidate < _shortTrailing.Value)
				_shortTrailing = candidate;
			}

			if (_shortTrailing is decimal trailing && candle.HighPrice >= trailing)
			{
				ClosePosition();
				LogInfo("Short trailing stop hit.");
				ResetPositionState();
				return;
			}

			if (_shortStop is decimal stop && candle.HighPrice >= stop)
			{
				ClosePosition();
				LogInfo("Short stop-loss triggered.");
				ResetPositionState();
				return;
			}

			if (_shortTarget is decimal target && candle.LowPrice <= target)
			{
				ClosePosition();
				LogInfo("Short take-profit reached.");
				ResetPositionState();
				return;
			}

			if (_previousChannelBasis is decimal prevBasis && candle.ClosePrice > prevBasis && lower >= (_previousLowerChannel ?? lower))
			{
				ClosePosition();
				LogInfo("Short exit: price rejected channel breakout.");
				ResetPositionState();
			}
		}
		else
		{
			ResetPositionState();
		}
	}

	private void EnsureLongTargets(decimal entry)
	{
		if (_longStop is null)
		{
			var stopDistance = GetPriceOffset(StopLossPips);
			if (stopDistance > 0m)
			_longStop = entry - stopDistance;
		}

		if (_longTarget is null)
		{
			var targetDistance = GetPriceOffset(TakeProfitPips);
			if (targetDistance > 0m)
			_longTarget = entry + targetDistance;
		}
	}

	private void EnsureShortTargets(decimal entry)
	{
		if (_shortStop is null)
		{
			var stopDistance = GetPriceOffset(StopLossPips);
			if (stopDistance > 0m)
			_shortStop = entry + stopDistance;
		}

		if (_shortTarget is null)
		{
			var targetDistance = GetPriceOffset(TakeProfitPips);
			if (targetDistance > 0m)
			_shortTarget = entry - targetDistance;
		}
	}

	private void CacheChannel(decimal basis, decimal deviation)
	{
		_previousChannelBasis = basis;
		_previousUpperChannel = basis + DeviationMultiplier * deviation;
		_previousLowerChannel = basis - DeviationMultiplier * deviation;
	}

	private void UpdateMomentumHistory(decimal momentum)
	{
		var distance = Math.Abs(momentum - 100m);
		_momentumDiffHistory.Enqueue(distance);
		while (_momentumDiffHistory.Count > 3)
		_momentumDiffHistory.Dequeue();
	}

	private void ResetState()
	{
		_previousChannelBasis = null;
		_previousUpperChannel = null;
		_previousLowerChannel = null;
		_macdLine = null;
		_macdSignal = null;
		_lastClose = null;
		ResetPositionState();
		_momentumDiffHistory.Clear();
	}

	private void ResetPositionState()
	{
		_longStop = null;
		_longTarget = null;
		_longTrailing = null;
		_longBreakEvenArmed = false;
		_shortStop = null;
		_shortTarget = null;
		_shortTrailing = null;
		_shortBreakEvenArmed = false;
	}

	private decimal GetPriceOffset(decimal pips)
	{
		if (pips <= 0m)
		return 0m;

		var step = _priceStep ?? Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return 0m;

		return step * pips;
	}
}

