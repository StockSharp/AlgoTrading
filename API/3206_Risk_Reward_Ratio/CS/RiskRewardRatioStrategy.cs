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
/// Risk Reward Ratio strategy converted from MetaTrader. Combines stochastic, RSI, momentum and MACD filters
/// together with configurable stop-loss, take-profit, trailing stop and break-even management.
/// </summary>
public class RiskRewardRatioStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<decimal> _rewardRatio;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<bool> _enableBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerPips;
	private readonly StrategyParam<decimal> _breakEvenOffsetPips;
	private readonly StrategyParam<bool> _exitSwitch;

	private LinearWeightedMovingAverage _fastMa = null!;
	private LinearWeightedMovingAverage _slowMa = null!;
	private StochasticOscillator _fastStochastic = null!;
	private StochasticOscillator _slowStochastic = null!;
	private RelativeStrengthIndex _rsi = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;

	private decimal _pointValue;
	private decimal? _longEntryPrice;
	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortEntryPrice;
	private decimal? _shortStop;
	private decimal? _shortTake;

	private decimal? _momentum1;
	private decimal? _momentum2;
	private decimal? _momentum3;

	/// <summary>
	/// Initializes a new instance of the <see cref="RiskRewardRatioStrategy"/> class.
	/// </summary>
	public RiskRewardRatioStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
		.SetDisplay("Trade Volume", "Order volume for every entry", "General")
		.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used for calculations", "Data");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
		.SetDisplay("Fast LWMA", "Length of the fast linear weighted moving average", "Indicators")
		.SetGreaterThanZero();

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
		.SetDisplay("Slow LWMA", "Length of the slow linear weighted moving average", "Indicators")
		.SetGreaterThanZero();

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
		.SetDisplay("Momentum Threshold", "Minimum momentum deviation from 100 to allow trades", "Indicators")
		.SetNotNegative();

		_rewardRatio = Param(nameof(RewardRatio), 2m)
		.SetDisplay("Reward Ratio", "Take-profit to stop-loss ratio", "Risk")
		.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 20m)
		.SetDisplay("Stop Loss (pips)", "Protective stop distance expressed in pips", "Risk")
		.SetGreaterThanZero();

		_maxPositions = Param(nameof(MaxPositions), 10)
		.SetDisplay("Max Positions", "Maximum number of position units allowed", "Risk")
		.SetGreaterThanZero();

		_enableTrailing = Param(nameof(EnableTrailing), true)
		.SetDisplay("Enable Trailing", "Activates trailing stop management", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 40m)
		.SetDisplay("Trailing Stop (pips)", "Distance maintained by the trailing stop", "Risk")
		.SetNotNegative();

		_enableBreakEven = Param(nameof(EnableBreakEven), true)
		.SetDisplay("Enable Break Even", "Moves stop-loss to break-even after a positive move", "Risk");

		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 30m)
		.SetDisplay("Break Even Trigger", "Required profit in pips before moving stop to break-even", "Risk")
		.SetNotNegative();

		_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 30m)
		.SetDisplay("Break Even Offset", "Extra pips added when moving the stop to break-even", "Risk")
		.SetNotNegative();

		_exitSwitch = Param(nameof(ExitSwitch), false)
		.SetDisplay("Exit Switch", "If enabled the strategy closes all positions", "Risk");

		Volume = _tradeVolume.Value;
	}

	/// <summary>
	/// Order volume used for entries.
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
	/// Timeframe used to build candles.
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
	/// Minimum momentum deviation required to open trades.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	/// <summary>
	/// Take-profit to stop-loss ratio.
	/// </summary>
	public decimal RewardRatio
	{
		get => _rewardRatio.Value;
		set => _rewardRatio.Value = value;
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
	/// Maximum number of position units allowed.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Enables trailing stop logic.
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
	/// Enables break-even management.
	/// </summary>
	public bool EnableBreakEven
	{
		get => _enableBreakEven.Value;
		set => _enableBreakEven.Value = value;
	}

	/// <summary>
	/// Required profit before the stop moves to break-even.
	/// </summary>
	public decimal BreakEvenTriggerPips
	{
		get => _breakEvenTriggerPips.Value;
		set => _breakEvenTriggerPips.Value = value;
	}

	/// <summary>
	/// Additional offset added once the stop moves to break-even.
	/// </summary>
	public decimal BreakEvenOffsetPips
	{
		get => _breakEvenOffsetPips.Value;
		set => _breakEvenOffsetPips.Value = value;
	}

	/// <summary>
	/// Emergency switch that forces position liquidation.
	/// </summary>
	public bool ExitSwitch
	{
		get => _exitSwitch.Value;
		set => _exitSwitch.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = _tradeVolume.Value;
		_pointValue = GetPointValue();

		_fastMa = new LinearWeightedMovingAverage { Length = FastMaPeriod };
		_slowMa = new LinearWeightedMovingAverage { Length = SlowMaPeriod };

		_fastStochastic = new StochasticOscillator
		{
			Length = 5,
			K = { Length = 2 },
			D = { Length = 2 }
		};

		_slowStochastic = new StochasticOscillator
		{
			Length = 21,
			K = { Length = 10 },
			D = { Length = 4 }
		};

		_rsi = new RelativeStrengthIndex { Length = 14 };
		_momentum = new Momentum { Length = 14 };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 }
			},
			SignalMa = { Length = 9 }
		};

		ResetState();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_fastStochastic, _slowStochastic, _macd, _momentum, _fastMa, _slowMa, _rsi, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _fastStochastic);
			DrawIndicator(area, _slowStochastic);
			DrawIndicator(area, _rsi);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(
	ICandleMessage candle,
	IIndicatorValue fastStochValue,
	IIndicatorValue slowStochValue,
	IIndicatorValue macdValue,
	IIndicatorValue momentumValue,
	IIndicatorValue fastMaValue,
	IIndicatorValue slowMaValue,
	IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (ExitSwitch && Position != 0m)
		{
			FlattenPosition();
			ResetState();
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var fastStoch = (StochasticOscillatorValue)fastStochValue;
		var slowStoch = (StochasticOscillatorValue)slowStochValue;
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (fastStoch.K is not decimal fastK)
		return;

		if (slowStoch.D is not decimal slowD)
		return;

		if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal macdSignal)
		return;

		if (!momentumValue.IsFinal || !fastMaValue.IsFinal || !slowMaValue.IsFinal || !rsiValue.IsFinal)
		return;

		var momentumRaw = momentumValue.ToDecimal();
		var fastMa = fastMaValue.ToDecimal();
		var slowMa = slowMaValue.ToDecimal();
		var rsi = rsiValue.ToDecimal();

		UpdateMomentumBuffer(momentumRaw);
		ManageOpenPosition(candle);

		var volume = Volume;
		if (volume <= 0m)
		return;

		var maxExposure = volume * MaxPositions;
		var currentExposure = Math.Abs(Position);
		if (currentExposure >= maxExposure)
		return;

		var fastBelowSlow = slowD < fastK;
		var fastAboveSlow = slowD > fastK;
		var rsiBullish = rsi > 50m;
		var rsiBearish = rsi < 50m;
		var maAlignmentBullish = fastMa > slowMa;
		var maAlignmentBearish = fastMa < slowMa;
		var macdBullish = (macdLine > 0m && macdLine > macdSignal) || (macdLine < 0m && macdLine > macdSignal);
		var macdBearish = (macdLine > 0m && macdLine < macdSignal) || (macdLine < 0m && macdLine < macdSignal);
		var momentumOk = IsMomentumQualified();

		if (fastBelowSlow && rsiBullish && maAlignmentBullish && macdBullish && momentumOk && Position <= 0m)
		{
			var entryPrice = candle.ClosePrice;
			BuyMarket(volume);
			ConfigureLongProtection(entryPrice);
		}
		else if (fastAboveSlow && rsiBearish && maAlignmentBearish && macdBearish && momentumOk && Position >= 0m)
		{
			var entryPrice = candle.ClosePrice;
			SellMarket(volume);
			ConfigureShortProtection(entryPrice);
		}
	}

	private void ConfigureLongProtection(decimal entryPrice)
	{
		_longEntryPrice = entryPrice;
		_shortEntryPrice = null;
		_shortStop = null;
		_shortTake = null;

		var distance = StopLossPips * _pointValue;
		if (distance <= 0m)
		{
			_longStop = null;
			_longTake = null;
			return;
		}

		_longStop = entryPrice - distance;
		_longTake = RewardRatio > 0m ? entryPrice + distance * RewardRatio : null;
	}

	private void ConfigureShortProtection(decimal entryPrice)
	{
		_shortEntryPrice = entryPrice;
		_longEntryPrice = null;
		_longStop = null;
		_longTake = null;

		var distance = StopLossPips * _pointValue;
		if (distance <= 0m)
		{
			_shortStop = null;
			_shortTake = null;
			return;
		}

		_shortStop = entryPrice + distance;
		_shortTake = RewardRatio > 0m ? entryPrice - distance * RewardRatio : null;
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position > 0m)
		{
			if (_longTake.HasValue && candle.HighPrice >= _longTake.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetState();
				return;
			}

			if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetState();
				return;
			}

			UpdateLongTrailing(candle);
		}
		else if (Position < 0m)
		{
			if (_shortTake.HasValue && candle.LowPrice <= _shortTake.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetState();
				return;
			}

			if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetState();
				return;
			}

			UpdateShortTrailing(candle);
		}
	}

	private void UpdateLongTrailing(ICandleMessage candle)
	{
		if (_longEntryPrice is null || _pointValue <= 0m)
		return;

		var entry = _longEntryPrice.Value;
		var close = candle.ClosePrice;
		var high = candle.HighPrice;

		if (EnableBreakEven)
		{
			var triggerDistance = BreakEvenTriggerPips * _pointValue;
			if (triggerDistance > 0m && high - entry >= triggerDistance)
			{
				var newStop = entry + BreakEvenOffsetPips * _pointValue;
				if (!_longStop.HasValue || newStop > _longStop.Value)
				_longStop = newStop;
			}
		}

		if (EnableTrailing)
		{
			var trailingDistance = TrailingStopPips * _pointValue;
			if (trailingDistance > 0m && close - entry >= trailingDistance)
			{
				var candidate = close - trailingDistance;
				if (!_longStop.HasValue || candidate > _longStop.Value)
				_longStop = candidate;
			}
		}
	}

	private void UpdateShortTrailing(ICandleMessage candle)
	{
		if (_shortEntryPrice is null || _pointValue <= 0m)
		return;

		var entry = _shortEntryPrice.Value;
		var close = candle.ClosePrice;
		var low = candle.LowPrice;

		if (EnableBreakEven)
		{
			var triggerDistance = BreakEvenTriggerPips * _pointValue;
			if (triggerDistance > 0m && entry - low >= triggerDistance)
			{
				var newStop = entry - BreakEvenOffsetPips * _pointValue;
				if (!_shortStop.HasValue || newStop < _shortStop.Value)
				_shortStop = newStop;
			}
		}

		if (EnableTrailing)
		{
			var trailingDistance = TrailingStopPips * _pointValue;
			if (trailingDistance > 0m && entry - close >= trailingDistance)
			{
				var candidate = close + trailingDistance;
				if (!_shortStop.HasValue || candidate < _shortStop.Value)
				_shortStop = candidate;
			}
		}
	}

	private void UpdateMomentumBuffer(decimal momentum)
	{
		_momentum3 = _momentum2;
		_momentum2 = _momentum1;
		_momentum1 = Math.Abs(momentum - 100m);
	}

	private bool IsMomentumQualified()
	{
		var threshold = MomentumThreshold;
		if (threshold <= 0m)
		return true;

		if (_momentum1.HasValue && _momentum1.Value >= threshold)
		return true;

		if (_momentum2.HasValue && _momentum2.Value >= threshold)
		return true;

		if (_momentum3.HasValue && _momentum3.Value >= threshold)
		return true;

		return false;
	}

	private decimal GetPointValue()
	{
		var security = Security;
		if (security == null)
		return 0m;

		var step = security.PriceStep;
		return step.HasValue && step.Value > 0m ? step.Value : 0m;
	}

	private void ResetState()
	{
		_longEntryPrice = null;
		_longStop = null;
		_longTake = null;
		_shortEntryPrice = null;
		_shortStop = null;
		_shortTake = null;
		_momentum1 = null;
		_momentum2 = null;
		_momentum3 = null;
	}

	private void FlattenPosition()
	{
		var position = Position;
		if (position > 0m)
		{
			SellMarket(Math.Abs(position));
		}
		else if (position < 0m)
		{
			BuyMarket(Math.Abs(position));
		}
	}
}

