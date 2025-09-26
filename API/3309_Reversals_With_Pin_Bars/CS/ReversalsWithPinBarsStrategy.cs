
using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader strategy "Reversals With Pin Bars".
/// Detects pin bar reversals on the active timeframe while aligning with higher timeframe momentum,
/// linear weighted moving averages, and MACD direction filters.
/// Includes configurable trailing-stop and break-even logic expressed in pips.
/// </summary>
public class ReversalsWithPinBarsStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<bool> _enableBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerPips;
	private readonly StrategyParam<decimal> _breakEvenOffsetPips;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherCandleType;
	private readonly StrategyParam<DataType> _macdCandleType;

	private LinearWeightedMovingAverage _fastMa = null!;
	private LinearWeightedMovingAverage _slowMa = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergence _macd = null!;

	private decimal? _fastMaValue;
	private decimal? _slowMaValue;
	private readonly Queue<decimal> _momentumDistance = new(3);
	private decimal? _macdMain;
	private decimal? _macdSignal;

	private decimal _pipSize;

	private ICandleMessage _previousPrimaryCandle;

	private decimal? _longStopPrice;
	private decimal? _longTakeProfit;
	private bool _longBreakEvenActive;

	private decimal? _shortStopPrice;
	private decimal? _shortTakeProfit;
	private bool _shortBreakEvenActive;

	/// <summary>
	/// Trading volume aligned to the instrument step.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Maximum number of sequential entries allowed per direction.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
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
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Enables trailing-stop management.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Trailing-stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Enables moving the stop-loss to break even.
	/// </summary>
	public bool EnableBreakEven
	{
		get => _enableBreakEven.Value;
		set => _enableBreakEven.Value = value;
	}

	/// <summary>
	/// Profit distance in pips required before activating the break-even stop.
	/// </summary>
	public decimal BreakEvenTriggerPips
	{
		get => _breakEvenTriggerPips.Value;
		set => _breakEvenTriggerPips.Value = value;
	}

	/// <summary>
	/// Additional buffer in pips applied to the break-even stop.
	/// </summary>
	public decimal BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	/// <summary>
	/// Fast linear weighted moving average length (higher timeframe).
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow linear weighted moving average length (higher timeframe).
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Momentum calculation length.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum momentum distance from the neutral 100 level.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	/// <summary>
	/// Fast EMA period used by MACD.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period used by MACD.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA period used by MACD.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Primary candle type that drives entries.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle type used for MA and momentum filters.
	/// </summary>
	public DataType HigherCandleType
	{
		get => _higherCandleType.Value;
		set => _higherCandleType.Value = value;
	}

	/// <summary>
	/// Candle type used for MACD evaluation.
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	public ReversalsWithPinBarsStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume", "Trading");

		_maxTrades = Param(nameof(MaxTrades), 3)
		.SetGreaterThanZero()
		.SetDisplay("Max Trades", "Maximum simultaneous entries", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 20m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss", "Stop-loss distance (pips)", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit", "Take-profit distance (pips)", "Risk");

		_enableTrailing = Param(nameof(EnableTrailing), true)
		.SetDisplay("Trailing", "Enable trailing stop", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 40m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing Pips", "Trailing distance (pips)", "Risk");

		_enableBreakEven = Param(nameof(EnableBreakEven), true)
		.SetDisplay("Break Even", "Enable break-even stop", "Risk");

		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 30m)
		.SetGreaterThanZero()
		.SetDisplay("BE Trigger", "Profit before break-even (pips)", "Risk");

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 5m)
		.SetDisplay("BE Offset", "Break-even offset (pips)", "Risk");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
		.SetGreaterThanZero()
		.SetDisplay("Fast LWMA", "Fast LWMA period", "Filters");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
		.SetGreaterThanZero()
		.SetDisplay("Slow LWMA", "Slow LWMA period", "Filters");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("Momentum", "Momentum period", "Filters");

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
		.SetGreaterThanZero()
		.SetDisplay("Momentum Th", "Minimum |momentum-100|", "Filters");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "MACD fast EMA", "Filters");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "MACD slow EMA", "Filters");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "MACD signal EMA", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Primary TF", "Primary candle type", "General");

		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Higher TF", "Higher timeframe candle", "General");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("MACD TF", "MACD candle type", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var seen = new HashSet<DataType>();
		if (Security != null)
		{
			if (seen.Add(CandleType))
				yield return (Security, CandleType);
			if (seen.Add(HigherCandleType))
				yield return (Security, HigherCandleType);
			if (seen.Add(MacdCandleType))
				yield return (Security, MacdCandleType);
		}
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = AlignVolume(TradeVolume);

		_fastMa = new LinearWeightedMovingAverage { Length = FastMaPeriod, CandlePrice = CandlePrice.Typical };
		_slowMa = new LinearWeightedMovingAverage { Length = SlowMaPeriod, CandlePrice = CandlePrice.Typical };
		_momentum = new Momentum { Length = MomentumPeriod };
		_macd = new MovingAverageConvergenceDivergence
		{
			FastLength = MacdFastPeriod,
			SlowLength = MacdSlowPeriod,
			SignalLength = MacdSignalPeriod
		};

		_pipSize = GetPipSize();

		var primarySubscription = SubscribeCandles(CandleType);
		primarySubscription.Bind(ProcessPrimaryCandle).Start();

		var higherSubscription = SubscribeCandles(HigherCandleType);
		higherSubscription.Bind(_fastMa, _slowMa, _momentum, ProcessHigherValues);
		higherSubscription.Start();

		var macdSubscription = SubscribeCandles(MacdCandleType);
		macdSubscription.BindEx(_macd, ProcessMacd).Start();
	}

	private void ProcessHigherValues(ICandleMessage candle, decimal fastMa, decimal slowMa, decimal momentum)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_fastMaValue = fastMa;
		_slowMaValue = slowMa;

		var distance = Math.Abs(momentum - 100m);
		if (_momentumDistance.Count == 3)
			_momentumDistance.Dequeue();
		_momentumDistance.Enqueue(distance);
	}

	private void ProcessMacd(ICandleMessage candle, IIndicatorValue value)
	{
		if (!value.IsFinal)
			return;

		if (value is MovingAverageConvergenceDivergenceSignalValue macdSignalValue)
		{
			_macdMain = macdSignalValue.Macd;
			_macdSignal = macdSignalValue.Signal;
		}
		else if (value is MovingAverageConvergenceDivergenceValue macdValue)
		{
			_macdMain = macdValue.Macd;
			_macdSignal = macdValue.Signal;
		}
	}

	private void ProcessPrimaryCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		ManageOpenPositions(candle);

		if (_previousPrimaryCandle is null)
		{
			_previousPrimaryCandle = candle;
			return;
		}

		if (!CanEvaluateEntry())
		{
			_previousPrimaryCandle = candle;
			return;
		}

		var prev = _previousPrimaryCandle;
		_previousPrimaryCandle = candle;

		var range = prev.HighPrice - prev.LowPrice;
		if (range <= 0m)
			return;

		var isBearish = prev.ClosePrice < prev.OpenPrice;
		var upperShadowBear = prev.HighPrice - prev.OpenPrice;
		var upperShadowBull = prev.HighPrice - prev.ClosePrice;
		var lowerShadowBull = prev.ClosePrice - prev.LowPrice;
		var lowerShadowBear = prev.OpenPrice - prev.LowPrice;

		var bullishPinBar = (isBearish && range > 0m && upperShadowBear / range >= 0.5m)
		|| (!isBearish && range > 0m && upperShadowBull / range >= 0.5m);
		var bearishPinBar = (isBearish && range > 0m && lowerShadowBull / range >= 0.5m)
		|| (!isBearish && range > 0m && lowerShadowBear / range >= 0.5m);

		var maBullish = _fastMaValue is decimal fast && _slowMaValue is decimal slow && fast > slow;
		var maBearish = _fastMaValue is decimal fastMa && _slowMaValue is decimal slowMa && fastMa < slowMa;
		var momentumOk = _momentumDistance.Any(d => d >= MomentumThreshold);
		var macdBullish = _macdMain is decimal macd && _macdSignal is decimal signal && macd > signal;
		var macdBearish = _macdMain is decimal macdMain && _macdSignal is decimal macdSignal && macdMain < macdSignal;

		if (bullishPinBar && maBullish && momentumOk && macdBullish)
			TryEnterLong(candle);
		else if (bearishPinBar && maBearish && momentumOk && macdBearish)
			TryEnterShort(candle);
	}

	private bool CanEvaluateEntry()
	{
		return _fastMaValue.HasValue && _slowMaValue.HasValue && _momentumDistance.Count > 0 && _macdMain.HasValue && _macdSignal.HasValue;
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		var remaining = Volume * MaxTrades - Math.Max(Position, 0m);
		var volume = AlignVolume(Math.Min(Volume, remaining));
		if (volume <= 0m)
			return;

		BuyMarket(volume);

		var entryPrice = candle.ClosePrice;
		_longStopPrice = entryPrice - StopLossPips * _pipSize;
		_longTakeProfit = entryPrice + TakeProfitPips * _pipSize;
		_longBreakEvenActive = false;
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		var remaining = Volume * MaxTrades - Math.Max(-Position, 0m);
		var volume = AlignVolume(Math.Min(Volume, remaining));
		if (volume <= 0m)
			return;

		SellMarket(volume);

		var entryPrice = candle.ClosePrice;
		_shortStopPrice = entryPrice + StopLossPips * _pipSize;
		_shortTakeProfit = entryPrice - TakeProfitPips * _pipSize;
		_shortBreakEvenActive = false;
	}

	private void ManageOpenPositions(ICandleMessage candle)
	{
		if (_pipSize <= 0m)
			return;

		if (Position > 0m)
		{
			var entryPrice = PositionPrice ?? candle.ClosePrice;

			if (EnableBreakEven && !_longBreakEvenActive && entryPrice > 0m)
			{
				var profit = candle.ClosePrice - entryPrice;
				if (profit >= BreakEvenTriggerPips * _pipSize)
				{
					_longStopPrice = entryPrice + BreakEvenOffsetPips * _pipSize;
					_longBreakEvenActive = true;
				}
			}

			if (EnableTrailing)
			{
				var trail = candle.ClosePrice - TrailingStopPips * _pipSize;
				if (_longStopPrice is null || trail > _longStopPrice.Value)
					_longStopPrice = trail;
			}

			var stopHit = _longStopPrice is decimal stop && candle.LowPrice <= stop;
			var takeHit = _longTakeProfit is decimal take && candle.HighPrice >= take;
			if (stopHit || takeHit)
			{
				SellMarket(Position);
				ResetLongState();
			}
		}
		else if (Position < 0m)
		{
			var entryPrice = PositionPrice ?? candle.ClosePrice;

			if (EnableBreakEven && !_shortBreakEvenActive && entryPrice > 0m)
			{
				var profit = entryPrice - candle.ClosePrice;
				if (profit >= BreakEvenTriggerPips * _pipSize)
				{
					_shortStopPrice = entryPrice - BreakEvenOffsetPips * _pipSize;
					_shortBreakEvenActive = true;
				}
			}

			if (EnableTrailing)
			{
				var trail = candle.ClosePrice + TrailingStopPips * _pipSize;
				if (_shortStopPrice is null || trail < _shortStopPrice.Value)
					_shortStopPrice = trail;
			}

			var stopHit = _shortStopPrice is decimal stop && candle.HighPrice >= stop;
			var takeHit = _shortTakeProfit is decimal take && candle.LowPrice <= take;
			if (stopHit || takeHit)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
			}
		}
		else
		{
			ResetLongState();
			ResetShortState();
		}
	}

	private void ResetLongState()
	{
		_longStopPrice = null;
		_longTakeProfit = null;
		_longBreakEvenActive = false;
	}

	private void ResetShortState()
	{
		_shortStopPrice = null;
		_shortTakeProfit = null;
		_shortBreakEvenActive = false;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep;
		if (step is null || step == 0m)
			return 0.0001m;

		return step.Value;
	}
}
