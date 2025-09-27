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

using StockSharp.Algo.Candles;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Eliot Waves strategy converted from MetaTrader. Combines linear weighted moving averages,
/// momentum deviation, Bollinger Bands and MACD based exit management together with risk
/// controls such as stop-loss, take-profit, trailing stop and break-even rules.
/// </summary>
public class EliotWavesStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<bool> _enableBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerPips;
	private readonly StrategyParam<decimal> _breakEvenOffsetPips;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<bool> _enableExitStrategy;

	private LinearWeightedMovingAverage _fastMa = null!;
	private LinearWeightedMovingAverage _slowMa = null!;
	private Momentum _momentum = null!;
	private BollingerBands _bollinger = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private ICandleMessage _previousCandle;
	private ICandleMessage _previousPreviousCandle;
	private readonly Queue<decimal> _momentumHistory = new();

	private decimal _pipSize;
	private decimal _pointValue;
	private bool _pipWarningIssued;

	private decimal? _longEntryPrice;
	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortEntryPrice;
	private decimal? _shortStop;
	private decimal? _shortTake;

	/// <summary>
	/// Initializes a new instance of the <see cref="EliotWavesStrategy"/> class.
	/// </summary>
	public EliotWavesStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetDisplay("Trade Volume", "Order volume used for each position step", "General")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for all indicators", "Data");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
			.SetDisplay("Fast LWMA", "Length of the fast linear weighted moving average", "Indicators")
			.SetGreaterThanZero();

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
			.SetDisplay("Slow LWMA", "Length of the slow linear weighted moving average", "Indicators")
			.SetGreaterThanZero();

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetDisplay("Momentum Period", "Lookback for the momentum deviation filter", "Indicators")
			.SetGreaterThanZero();

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
			.SetDisplay("Momentum Threshold", "Minimum deviation from 100 required to trade", "Indicators")
			.SetNotNegative();

		_stopLossPips = Param(nameof(StopLossPips), 20m)
			.SetDisplay("Stop Loss (pips)", "Protective stop distance expressed in pips", "Risk")
			.SetNotNegative();

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetDisplay("Take Profit (pips)", "Take-profit distance expressed in pips", "Risk")
			.SetNotNegative();

		_enableTrailing = Param(nameof(EnableTrailing), true)
			.SetDisplay("Enable Trailing", "Activates trailing stop management", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 40m)
			.SetDisplay("Trailing Stop (pips)", "Distance maintained by the trailing stop", "Risk")
			.SetNotNegative();

		_enableBreakEven = Param(nameof(EnableBreakEven), true)
			.SetDisplay("Enable Break Even", "Move stop-loss to break-even after a positive move", "Risk");

		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 30m)
			.SetDisplay("Break Even Trigger (pips)", "Required profit in pips before moving stop to break-even", "Risk")
			.SetNotNegative();

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 30m)
			.SetDisplay("Break Even Offset (pips)", "Additional buffer applied when moving the stop", "Risk")
			.SetNotNegative();

		_maxPositions = Param(nameof(MaxPositions), 10)
			.SetDisplay("Max Positions", "Maximum number of volume steps allowed", "Risk")
			.SetGreaterThanZero();

		_enableExitStrategy = Param(nameof(EnableExitStrategy), false)
			.SetDisplay("Force Exit", "When enabled the strategy closes all open positions", "Risk");

		Volume = _tradeVolume.Value;
	}

	/// <summary>
	/// Trade volume used for entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set
		{
			_tradeVolume.Value = value;
			Volume = value;
		}
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
	/// Fast linear weighted moving average length.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow linear weighted moving average length.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Momentum indicator length.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum deviation from 100 required to accept momentum signals.
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
	/// Enables trailing stop management.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
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
	/// Enables the break-even feature.
	/// </summary>
	public bool EnableBreakEven
	{
		get => _enableBreakEven.Value;
		set => _enableBreakEven.Value = value;
	}

	/// <summary>
	/// Profit threshold before the break-even rule is applied.
	/// </summary>
	public decimal BreakEvenTriggerPips
	{
		get => _breakEvenTriggerPips.Value;
		set => _breakEvenTriggerPips.Value = value;
	}

	/// <summary>
	/// Additional offset when moving the stop to break-even.
	/// </summary>
	public decimal BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	/// <summary>
	/// Maximum number of volume steps allowed.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Forces the strategy to exit all positions when enabled.
	/// </summary>
	public bool EnableExitStrategy
	{
		get => _enableExitStrategy.Value;
		set => _enableExitStrategy.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousCandle = null;
		_previousPreviousCandle = null;
		_momentumHistory.Clear();
		_pipSize = 0m;
		_pointValue = 0m;
		_pipWarningIssued = false;

		ResetLongTargets();
		ResetShortTargets();

		Volume = _tradeVolume.Value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();
		_pointValue = GetPointValue();

		_fastMa = new LinearWeightedMovingAverage
		{
			Length = FastMaPeriod,
			CandlePrice = CandlePrice.Typical
		};

		_slowMa = new LinearWeightedMovingAverage
		{
			Length = SlowMaPeriod,
			CandlePrice = CandlePrice.Typical
		};

		_momentum = new Momentum
		{
			Length = MomentumPeriod
		};

		_bollinger = new BollingerBands
		{
			Length = 20,
			Width = 2m
		};

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			ShortMa = { Length = 12 },
			LongMa = { Length = 26 },
			SignalMa = { Length = 9 }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_fastMa, _slowMa, _momentum, _bollinger, _macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue momentumValue, IIndicatorValue bollingerValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!fastValue.IsFinal || !slowValue.IsFinal || !momentumValue.IsFinal || !bollingerValue.IsFinal || !macdValue.IsFinal)
		{
			StoreCandle(candle);
			return;
		}

		var fast = fastValue.ToDecimal();
		var slow = slowValue.ToDecimal();
		var momentumReading = momentumValue.ToDecimal();

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_momentum.IsFormed || !_bollinger.IsFormed || !_macd.IsFormed)
		{
			UpdateMomentumHistory(momentumReading);
			StoreCandle(candle);
			return;
		}

		var bollinger = (BollingerBandsValue)bollingerValue;
		if (bollinger.UpBand is not decimal upperBand || bollinger.LowBand is not decimal lowerBand || bollinger.MovingAverage is not decimal _)
		{
			StoreCandle(candle);
			return;
		}

		var macd = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macd.Macd is not decimal macdMain || macd.Signal is not decimal macdSignal)
		{
			StoreCandle(candle);
			return;
		}

		UpdateMomentumHistory(momentumReading);

		// Update protective logic for an existing long position.
		ManageLongPosition(candle, lowerBand, macdMain, macdSignal);
		ManageShortPosition(candle, upperBand, macdMain, macdSignal);

		// Allow the operator to liquidate all positions instantly when the switch is active.
		if (EnableExitStrategy)
		{
			ForceExit();
			StoreCandle(candle);
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading() || !EnableTrading)
		{
			StoreCandle(candle);
			return;
		}

		var hasStructure = _previousCandle != null && _previousPreviousCandle != null;
		var canBuyStructure = hasStructure && _previousPreviousCandle!.LowPrice < _previousCandle!.HighPrice;
		var canSellStructure = hasStructure && _previousCandle!.LowPrice < _previousPreviousCandle!.HighPrice;

		var momentumOk = HasMomentumImpulse();
		var targetVolume = GetMaxExposure();

		// Fast LWMA above slow LWMA together with momentum and structure confirmation triggers a long entry attempt.
		if (fast > slow && momentumOk && canBuyStructure)
			TryOpenLong(candle, targetVolume);
		else if (fast < slow && momentumOk && canSellStructure)
			TryOpenShort(candle, targetVolume);

		StoreCandle(candle);
	}

	private void ManageLongPosition(ICandleMessage candle, decimal lowerBand, decimal macdMain, decimal macdSignal)
	{
		if (Position <= 0m)
			return;

		var positionVolume = Position;

		// Price crossed the volatility or MACD filters against the long position.

		if (candle.ClosePrice <= lowerBand || macdMain < macdSignal)
		{
			SellMarket(positionVolume);
			ResetLongTargets();
			return;
		}

		if (_longEntryPrice is not decimal entryPrice)
			return;

		var stopDistance = ConvertPips(StopLossPips);
		if (_longStop == null && stopDistance > 0m)
			// Initialise stop-loss relative to the average entry price.
			_longStop = entryPrice - stopDistance;

		var takeDistance = ConvertPips(TakeProfitPips);
		if (_longTake == null && takeDistance > 0m)
			_longTake = entryPrice + takeDistance;

		if (EnableBreakEven && BreakEvenTriggerPips > 0m)
		{
			var triggerDistance = ConvertPips(BreakEvenTriggerPips);
			if (triggerDistance > 0m && candle.ClosePrice - entryPrice >= triggerDistance)
			{
				var breakEvenPrice = entryPrice + ConvertPips(BreakEvenOffsetPips);
				if (!_longStop.HasValue || _longStop < breakEvenPrice)
					_longStop = breakEvenPrice;
			}
		}

		if (EnableTrailing && TrailingStopPips > 0m)
		{
			var trailingDistance = ConvertPips(TrailingStopPips);
			if (trailingDistance > 0m && candle.ClosePrice - entryPrice >= trailingDistance)
			{
				var candidateStop = candle.ClosePrice - trailingDistance;
				if (!_longStop.HasValue || candidateStop > _longStop.Value)
					_longStop = candidateStop;
			}
		}

		if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
		{
			SellMarket(positionVolume);
			ResetLongTargets();
			return;
		}

		if (_longTake.HasValue && candle.HighPrice >= _longTake.Value)
		{
			SellMarket(positionVolume);
			ResetLongTargets();
		}
	}

	private void ManageShortPosition(ICandleMessage candle, decimal upperBand, decimal macdMain, decimal macdSignal)
	{
		if (Position >= 0m)
			return;

		var positionVolume = Math.Abs(Position);

		// Exit short exposure when volatility or MACD signals turn against it.

		if (candle.ClosePrice >= upperBand || macdMain > macdSignal)
		{
			BuyMarket(positionVolume);
			ResetShortTargets();
			return;
		}

		if (_shortEntryPrice is not decimal entryPrice)
			return;

		var stopDistance = ConvertPips(StopLossPips);
		if (_shortStop == null && stopDistance > 0m)
			// Initialise stop-loss for the short position above the entry.
			_shortStop = entryPrice + stopDistance;

		var takeDistance = ConvertPips(TakeProfitPips);
		if (_shortTake == null && takeDistance > 0m)
			_shortTake = entryPrice - takeDistance;

		if (EnableBreakEven && BreakEvenTriggerPips > 0m)
		{
			var triggerDistance = ConvertPips(BreakEvenTriggerPips);
			if (triggerDistance > 0m && entryPrice - candle.ClosePrice >= triggerDistance)
			{
				var breakEvenPrice = entryPrice - ConvertPips(BreakEvenOffsetPips);
				if (!_shortStop.HasValue || _shortStop > breakEvenPrice)
					_shortStop = breakEvenPrice;
			}
		}

		if (EnableTrailing && TrailingStopPips > 0m)
		{
			var trailingDistance = ConvertPips(TrailingStopPips);
			if (trailingDistance > 0m && entryPrice - candle.ClosePrice >= trailingDistance)
			{
				var candidateStop = candle.ClosePrice + trailingDistance;
				if (!_shortStop.HasValue || candidateStop < _shortStop.Value)
					_shortStop = candidateStop;
			}
		}

		if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
		{
			BuyMarket(positionVolume);
			ResetShortTargets();
			return;
		}

		if (_shortTake.HasValue && candle.LowPrice <= _shortTake.Value)
		{
			BuyMarket(positionVolume);
			ResetShortTargets();
		}
	}

	private void TryOpenLong(ICandleMessage candle, decimal targetVolume)
	{
		if (!EnableTrading)
			return;

		// Close the opposite exposure before attempting to build a new long position.

		if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
			ResetShortTargets();
		}

		var currentLong = Position > 0m ? Position : 0m;
		var remaining = targetVolume - currentLong;
		if (remaining <= 0m)
			return;

		var volumeToBuy = Math.Min(TradeVolume, remaining);
		if (volumeToBuy > 0m)
			BuyMarket(volumeToBuy);
	}

	private void TryOpenShort(ICandleMessage candle, decimal targetVolume)
	{
		if (!EnableTrading)
			return;

		// Close the opposite exposure before attempting to build a new short position.

		if (Position > 0m)
		{
			SellMarket(Position);
			ResetLongTargets();
		}

		var currentShort = Position < 0m ? Math.Abs(Position) : 0m;
		var remaining = targetVolume - currentShort;
		if (remaining <= 0m)
			return;

		var volumeToSell = Math.Min(TradeVolume, remaining);
		if (volumeToSell > 0m)
			SellMarket(volumeToSell);
	}

	private void ForceExit()
	{
		if (Position > 0m)
		{
			SellMarket(Position);
			ResetLongTargets();
		}
		else if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
			ResetShortTargets();
		}
	}

	private bool HasMomentumImpulse()
	{
		if (_momentumHistory.Count == 0)
			return false;

		// Check the last three momentum readings for the required deviation from 100.
		foreach (var value in _momentumHistory)
		{
			if (Math.Abs(100m - value) >= MomentumThreshold)
				return true;
		}

		return false;
	}

	private void UpdateMomentumHistory(decimal momentum)
	{
		_momentumHistory.Enqueue(momentum);

		// Keep only the three most recent values to mirror the original EA.

		while (_momentumHistory.Count > 3)
			_momentumHistory.Dequeue();
	}

	private decimal GetMaxExposure()
	{
		return TradeVolume * MaxPositions;
	}

	private void StoreCandle(ICandleMessage candle)
	{
		_previousPreviousCandle = _previousCandle;
		_previousCandle = candle;
	}

	private decimal ConvertPips(decimal pips)
	{
		if (pips <= 0m)
			return 0m;

		if (_pipSize > 0m)
			return _pipSize * pips;

		if (!_pipWarningIssued)
		{
			LogWarning("Unable to determine pip size from security settings. Using price step as a fallback.");
			_pipWarningIssued = true;
		}

		return _pointValue > 0m ? _pointValue * pips : pips;
	}

	private decimal CalculatePipSize()
	{
		// Derive the pip size from exchange specifications or fall back to the price step.
		var security = Security;
		if (security == null)
			return 0m;

		var step = security.PriceStep ?? security.MinStep ?? 0m;
		if (step <= 0m)
			return 0m;

		var bits = decimal.GetBits(step);
		var scale = (bits[3] >> 16) & 0x7F;

		return scale is 3 or 5 ? step * 10m : step;
	}

	private decimal GetPointValue()
	{
		// Price step is used as a conservative substitute when the pip size is unknown.
		var security = Security;
		if (security == null)
			return 0m;

		var step = security.PriceStep ?? security.MinStep ?? 0m;
		return step > 0m ? step : 0m;
	}

	private void ResetLongTargets()
	{
		_longEntryPrice = null;
		_longStop = null;
		_longTake = null;
	}

	private void ResetShortTargets()
	{
		_shortEntryPrice = null;
		_shortStop = null;
		_shortTake = null;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0m)
		{
			// Update long-side protective targets using the current average entry price.
			_longEntryPrice = PositionPrice;

			if (_longEntryPrice is decimal entryPrice && entryPrice > 0m)
			{
				var stopDistance = ConvertPips(StopLossPips);
				_longStop = stopDistance > 0m ? entryPrice - stopDistance : null;

				var takeDistance = ConvertPips(TakeProfitPips);
				_longTake = takeDistance > 0m ? entryPrice + takeDistance : null;
			}

			_shortEntryPrice = null;
			_shortStop = null;
			_shortTake = null;
		}
		else if (Position < 0m)
		{
			// Update short-side protective targets using the current average entry price.
			_shortEntryPrice = PositionPrice;

			if (_shortEntryPrice is decimal entryPrice && entryPrice > 0m)
			{
				var stopDistance = ConvertPips(StopLossPips);
				_shortStop = stopDistance > 0m ? entryPrice + stopDistance : null;

				var takeDistance = ConvertPips(TakeProfitPips);
				_shortTake = takeDistance > 0m ? entryPrice - takeDistance : null;
			}

			_longEntryPrice = null;
			_longStop = null;
			_longTake = null;
		}
		else
		{
			ResetLongTargets();
			ResetShortTargets();
		}
	}
}

