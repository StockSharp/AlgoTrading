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
/// Momentum based day trading strategy converted from the "Day Trading" MQL expert.
/// </summary>
public class DayTradingStrategy : Strategy
{
	private readonly StrategyParam<int> _pullbackLookback;

	private readonly Queue<bool> _pullbackBelowMa20 = new();
	private readonly Queue<bool> _pullbackAboveMa20 = new();
	private readonly Queue<bool> _ema100BullQueue = new();
	private readonly Queue<bool> _ema100BearQueue = new();
	private readonly Queue<decimal> _momentumDeviationQueue = new();

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _macdCandleType;
	private readonly StrategyParam<int> _trendConfirmationCount;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<int> _maxPositions;

	private decimal? _longStopPrice;
	private decimal? _longTakeProfitPrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakeProfitPrice;
	private decimal? _pipSize;

	private decimal? _macdMain;
	private decimal? _macdSignal;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public DayTradingStrategy()
	{

		_pullbackLookback = Param(nameof(PullbackLookback), 3)
			.SetGreaterThanZero()
			.SetDisplay("Pullback Lookback", "Candles stored for recent pullback checks", "Trend");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(30).TimeFrame())
			.SetDisplay("MACD Candle Type", "Higher timeframe for MACD", "General");

		_trendConfirmationCount = Param(nameof(TrendConfirmationCount), 10)
			.SetGreaterThanZero()
			.SetDisplay("Trend Confirmation", "Candles required above/below EMA100", "Trend");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Momentum indicator period", "Momentum");

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
			.SetDisplay("Momentum Threshold", "Minimum absolute deviation from 100", "Momentum");

		_stopLossPips = Param(nameof(StopLossPips), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss Pips", "Stop distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit Pips", "Target distance in pips", "Risk");

		_maxPositions = Param(nameof(MaxPositions), 10)
			.SetGreaterThanZero()
			.SetDisplay("Max Positions", "Maximum number of base lots", "Risk");
	}


	/// <summary>
	/// Primary candle type used for trading decisions.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Candle type for the higher timeframe MACD confirmation.
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	/// <summary>
	/// Number of candles that must stay on one side of EMA100.
	/// </summary>
	public int TrendConfirmationCount
	{
		get => _trendConfirmationCount.Value;
		set => _trendConfirmationCount.Value = value;
	}

	/// <summary>
	/// Momentum indicator period.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum absolute momentum deviation from 100.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Maximum number of base lots that can be accumulated per direction.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Number of candles tracked for pullback evaluation.
	/// </summary>
	public int PullbackLookback
	{
		get => _pullbackLookback.Value;
		set => _pullbackLookback.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
			(Security, CandleType),
			(Security, MacdCandleType)
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pullbackBelowMa20.Clear();
		_pullbackAboveMa20.Clear();
		_ema100BullQueue.Clear();
		_ema100BearQueue.Clear();
		_momentumDeviationQueue.Clear();

		_longStopPrice = null;
		_longTakeProfitPrice = null;
		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
		_pipSize = null;
		_macdMain = null;
		_macdSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ema20 = new ExponentialMovingAverage
		{
			Length = 20,
			CandlePrice = CandlePrice.Close
		};

		var ema60 = new ExponentialMovingAverage
		{
			Length = 60,
			CandlePrice = CandlePrice.Close
		};

		var ema100 = new ExponentialMovingAverage
		{
			Length = 100,
			CandlePrice = CandlePrice.Median
		};

		var momentum = new Momentum
		{
			Length = MomentumPeriod,
			CandlePrice = CandlePrice.Close
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ema20, ema60, ema100, momentum, ProcessMainCandle)
			.Start();

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 }
			},
			SignalMa = { Length = 9 }
		};

		SubscribeCandles(MacdCandleType)
			.BindEx(macd, ProcessMacdCandle)
			.Start();

		StartProtection();
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal ema20, decimal ema60, decimal ema100, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		CheckProtectiveLevels(candle);

		var trendCount = TrendConfirmationCount;
		var hasBullTrend = AllTrue(_ema100BullQueue, trendCount);
		var hasBearTrend = AllTrue(_ema100BearQueue, trendCount);
		var hasBullPullback = HasAnyTrue(_pullbackBelowMa20);
		var hasBearPullback = HasAnyTrue(_pullbackAboveMa20);
		var hasMomentumImpulse = HasMomentumImpulse();
		var structureBullish = ema20 > ema60 && ema60 > ema100;
		var structureBearish = ema20 < ema60 && ema60 < ema100;
		var macdRelation = GetMacdRelation();

		if (IsFormedAndOnlineAndAllowTrading())
		{
			if (macdRelation == MacdRelation.Bullish && hasBullTrend && structureBullish && hasBullPullback && hasMomentumImpulse)
			{
				TryEnterLong(candle);
			}
			else if (macdRelation == MacdRelation.Bearish && hasBearTrend && structureBearish && hasBearPullback && hasMomentumImpulse)
			{
				TryEnterShort(candle);
			}
		}

		UpdateQueues(candle, ema20, ema100, momentumValue, trendCount);
	}

	private void ProcessMacdCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (!macdValue.IsFinal || candle.State != CandleStates.Finished)
			return;

		var typed = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (typed.Macd is not decimal macd || typed.Signal is not decimal signal)
			return;

		_macdMain = macd;
		_macdSignal = signal;
	}

	private void TryEnterLong(ICandleMessage candle)
	{
		var baseVolume = CalculateEntryVolume();
		if (baseVolume <= 0m)
			return;

		var totalVolume = baseVolume;

		if (Position < 0m)
			totalVolume += Math.Abs(Position);

		if (totalVolume <= 0m)
			return;

		BuyMarket(totalVolume);

		var stopDistance = ConvertPipsToPrice(StopLossPips);
		var targetDistance = ConvertPipsToPrice(TakeProfitPips);
		var entryPrice = candle.ClosePrice;

		_longStopPrice = stopDistance > 0m ? entryPrice - stopDistance : null;
		_longTakeProfitPrice = targetDistance > 0m ? entryPrice + targetDistance : null;

		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
	}

	private void TryEnterShort(ICandleMessage candle)
	{
		var baseVolume = CalculateEntryVolume();
		if (baseVolume <= 0m)
			return;

		var totalVolume = baseVolume;

		if (Position > 0m)
			totalVolume += Math.Abs(Position);

		if (totalVolume <= 0m)
			return;

		SellMarket(totalVolume);

		var stopDistance = ConvertPipsToPrice(StopLossPips);
		var targetDistance = ConvertPipsToPrice(TakeProfitPips);
		var entryPrice = candle.ClosePrice;

		_shortStopPrice = stopDistance > 0m ? entryPrice + stopDistance : null;
		_shortTakeProfitPrice = targetDistance > 0m ? entryPrice - targetDistance : null;

		_longStopPrice = null;
		_longTakeProfitPrice = null;
	}

	private void CheckProtectiveLevels(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetLongRiskLevels();
				return;
			}

			if (_longTakeProfitPrice is decimal target && candle.HighPrice >= target)
			{
				SellMarket(Position);
				ResetLongRiskLevels();
			}
		}
		else if (Position < 0m)
		{
			if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(-Position);
				ResetShortRiskLevels();
				return;
			}

			if (_shortTakeProfitPrice is decimal target && candle.LowPrice <= target)
			{
				BuyMarket(-Position);
				ResetShortRiskLevels();
			}
		}
	}

	private void ResetLongRiskLevels()
	{
		_longStopPrice = null;
		_longTakeProfitPrice = null;
	}

	private void ResetShortRiskLevels()
	{
		_shortStopPrice = null;
		_shortTakeProfitPrice = null;
	}

	private void UpdateQueues(ICandleMessage candle, decimal ema20, decimal ema100, decimal momentumValue, int trendCount)
	{
		var momentumDeviation = Math.Abs(momentumValue - 100m);

		EnqueueLimited(_pullbackBelowMa20, candle.LowPrice < ema20, PullbackLookback);
		EnqueueLimited(_pullbackAboveMa20, candle.LowPrice > ema20, PullbackLookback);
		EnqueueLimited(_ema100BullQueue, candle.LowPrice > ema100, trendCount);
		EnqueueLimited(_ema100BearQueue, candle.HighPrice < ema100, trendCount);
		EnqueueLimited(_momentumDeviationQueue, momentumDeviation, PullbackLookback);
	}

	private decimal CalculateEntryVolume()
	{
		var baseVolume = Volume;
		if (baseVolume <= 0m)
			return 0m;

		var maxVolume = baseVolume * MaxPositions;
		if (maxVolume <= 0m)
			return 0m;

		var currentExposure = Math.Abs(Position);
		var remaining = maxVolume - currentExposure;
		if (remaining <= 0m)
			return 0m;

		var desired = Math.Min(baseVolume, remaining);
		return NormalizeVolume(desired);
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

	private decimal ConvertPipsToPrice(decimal pips)
	{
		if (pips <= 0m)
			return 0m;

		EnsurePipSize();

		return (_pipSize ?? 0m) * pips;
	}

	private void EnsurePipSize()
	{
		if (_pipSize is decimal value && value > 0m)
			return;

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

		_pipSize = digits is 3 or 5 ? step * 10m : step;
	}

	private bool HasMomentumImpulse()
	{
		if (_momentumDeviationQueue.Count < PullbackLookback)
			return false;

		foreach (var deviation in _momentumDeviationQueue)
		{
			if (deviation >= MomentumThreshold)
			return true;
		}

		return false;
	}

	private static bool HasAnyTrue(Queue<bool> queue)
	{
		if (queue.Count < PullbackLookback)
			return false;

		foreach (var value in queue)
		{
			if (value)
			return true;
		}

		return false;
	}

	private static bool AllTrue(Queue<bool> queue, int requiredCount)
	{
		if (requiredCount <= 0 || queue.Count < requiredCount)
			return false;

		foreach (var value in queue)
		{
			if (!value)
			return false;
		}

		return true;
	}

	private static void EnqueueLimited(Queue<bool> queue, bool value, int capacity)
	{
		if (capacity <= 0)
		{
			queue.Clear();
			return;
		}

		queue.Enqueue(value);

		while (queue.Count > capacity)
		{
			queue.Dequeue();
		}
	}

	private static void EnqueueLimited(Queue<decimal> queue, decimal value, int capacity)
	{
		if (capacity <= 0)
		{
			queue.Clear();
			return;
		}

		queue.Enqueue(value);

		while (queue.Count > capacity)
		{
			queue.Dequeue();
		}
	}

	private MacdRelation GetMacdRelation()
	{
		if (_macdMain is not decimal macd || _macdSignal is not decimal signal)
			return MacdRelation.None;

		if (macd > signal)
			return MacdRelation.Bullish;

		if (macd < signal)
			return MacdRelation.Bearish;

		return MacdRelation.None;
	}

	private enum MacdRelation
	{
		None,
		Bullish,
		Bearish
	}
}

