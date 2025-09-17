using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bruno trend-following strategy converted from MetaTrader 5.
/// Combines ADX directional movement, EMA alignment, MACD momentum, Stochastic oscillator and Parabolic SAR slope.
/// Trades only when all filters agree on a single direction and scales the trade volume by a multiplier for every confirming filter.
/// Includes fixed stop-loss, take-profit and trailing-stop management defined in adjusted pips.
/// </summary>
public class BrunoStrategy : Strategy
{
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<decimal> _trailingStepPips;
	private readonly StrategyParam<decimal> _signalMultiplier;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxPositiveThreshold;
	private readonly StrategyParam<decimal> _adxNegativeThreshold;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _stochasticPeriod;
	private readonly StrategyParam<int> _stochasticKsmoothing;
	private readonly StrategyParam<int> _stochasticDsmoothing;
	private readonly StrategyParam<decimal> _stochasticOverbought;
	private readonly StrategyParam<decimal> _stochasticOversold;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _fastEma = null!;
	private EMA _slowEma = null!;
	private MovingAverageConvergenceDivergence _macd = null!;
	private AverageDirectionalIndex _adx = null!;
	private StochasticOscillator _stochastic = null!;
	private ParabolicSar _sar = null!;

	private decimal _pipSize;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal? _previousSar;
	private decimal? _prePreviousSar;

	/// <summary>
	/// Base order volume in lots.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in adjusted pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in adjusted pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing-stop distance expressed in adjusted pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum progress in adjusted pips required before the trailing stop moves again.
	/// </summary>
	public decimal TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Lot multiplier applied whenever a filter supports the active direction.
	/// </summary>
	public decimal SignalMultiplier
	{
		get => _signalMultiplier.Value;
		set => _signalMultiplier.Value = value;
	}

	/// <summary>
	/// ADX calculation period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Minimum +DI value required to amplify long signals.
	/// </summary>
	public decimal AdxPositiveThreshold
	{
		get => _adxPositiveThreshold.Value;
		set => _adxPositiveThreshold.Value = value;
	}

	/// <summary>
	/// Maximum +DI value that still supports short signals (mirrors the original EA logic).
	/// </summary>
	public decimal AdxNegativeThreshold
	{
		get => _adxNegativeThreshold.Value;
		set => _adxNegativeThreshold.Value = value;
	}

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// MACD fast EMA length.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// MACD slow EMA length.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// MACD signal line length.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// %K period of the Stochastic oscillator.
	/// </summary>
	public int StochasticPeriod
	{
		get => _stochasticPeriod.Value;
		set => _stochasticPeriod.Value = value;
	}

	/// <summary>
	/// %K smoothing period for the Stochastic oscillator.
	/// </summary>
	public int StochasticKsmoothing
	{
		get => _stochasticKsmoothing.Value;
		set => _stochasticKsmoothing.Value = value;
	}

	/// <summary>
	/// %D smoothing period for the Stochastic oscillator.
	/// </summary>
	public int StochasticDsmoothing
	{
		get => _stochasticDsmoothing.Value;
		set => _stochasticDsmoothing.Value = value;
	}

	/// <summary>
	/// Overbought threshold for Stochastic %K.
	/// </summary>
	public decimal StochasticOverbought
	{
		get => _stochasticOverbought.Value;
		set => _stochasticOverbought.Value = value;
	}

	/// <summary>
	/// Oversold threshold for Stochastic %K.
	/// </summary>
	public decimal StochasticOversold
	{
		get => _stochasticOversold.Value;
		set => _stochasticOversold.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BrunoStrategy"/> class.
	/// </summary>
	public BrunoStrategy()
	{
		_baseVolume = Param(nameof(BaseVolume), 1m)
		.SetDisplay("Base Volume", "Default lot size used before multipliers", "Trading")
		.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 150m)
		.SetDisplay("Stop Loss (pips)", "Protective stop distance in adjusted pips", "Risk Management")
		.SetNotNegative();

		_takeProfitPips = Param(nameof(TakeProfitPips), 150m)
		.SetDisplay("Take Profit (pips)", "Target distance in adjusted pips", "Risk Management")
		.SetNotNegative();

		_trailingStopPips = Param(nameof(TrailingStopPips), 150m)
		.SetDisplay("Trailing Stop (pips)", "Trailing stop distance in adjusted pips", "Risk Management")
		.SetNotNegative();

		_trailingStepPips = Param(nameof(TrailingStepPips), 150m)
		.SetDisplay("Trailing Step (pips)", "Minimum additional profit before trailing stop moves", "Risk Management")
		.SetNotNegative();

		_signalMultiplier = Param(nameof(SignalMultiplier), 1.6m)
		.SetDisplay("Signal Multiplier", "Volume multiplier applied by every agreeing filter", "Trading")
		.SetGreaterThanZero();

		_adxPeriod = Param(nameof(AdxPeriod), 13)
		.SetDisplay("ADX Period", "Directional movement length", "Indicators")
		.SetGreaterThanZero();

		_adxPositiveThreshold = Param(nameof(AdxPositiveThreshold), 20m)
		.SetDisplay("+DI Threshold", "Minimum +DI value to reinforce long trades", "Indicators")
		.SetRange(0m, 100m);

		_adxNegativeThreshold = Param(nameof(AdxNegativeThreshold), 40m)
		.SetDisplay("+DI Limit", "Upper bound of +DI that still supports shorts", "Indicators")
		.SetRange(0m, 100m);

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 8)
		.SetDisplay("Fast EMA", "Fast exponential moving average length", "Indicators")
		.SetGreaterThanZero();

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 21)
		.SetDisplay("Slow EMA", "Slow exponential moving average length", "Indicators")
		.SetGreaterThanZero();

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 13)
		.SetDisplay("MACD Fast", "Fast EMA length for MACD", "Indicators")
		.SetGreaterThanZero();

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 34)
		.SetDisplay("MACD Slow", "Slow EMA length for MACD", "Indicators")
		.SetGreaterThanZero();

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 8)
		.SetDisplay("MACD Signal", "Signal line smoothing length", "Indicators")
		.SetGreaterThanZero();

		_stochasticPeriod = Param(nameof(StochasticPeriod), 21)
		.SetDisplay("Stochastic %K", "%K period for Stochastic oscillator", "Indicators")
		.SetGreaterThanZero();

		_stochasticKsmoothing = Param(nameof(StochasticKsmoothing), 3)
		.SetDisplay("Stochastic Smoothing", "%K smoothing length", "Indicators")
		.SetGreaterThanZero();

		_stochasticDsmoothing = Param(nameof(StochasticDsmoothing), 3)
		.SetDisplay("Stochastic %D", "%D smoothing length", "Indicators")
		.SetGreaterThanZero();

		_stochasticOverbought = Param(nameof(StochasticOverbought), 80m)
		.SetDisplay("Stochastic Overbought", "Upper %K threshold", "Indicators")
		.SetRange(0m, 100m);

		_stochasticOversold = Param(nameof(StochasticOversold), 20m)
		.SetDisplay("Stochastic Oversold", "Lower %K threshold", "Indicators")
		.SetRange(0m, 100m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used for analysis", "General");
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

		_pipSize = 0m;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_previousSar = null;
		_prePreviousSar = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = BaseVolume;

		_pipSize = CalculatePipSize();
		if (_pipSize <= 0m)
		{
			var step = Security?.PriceStep ?? 0m;
			_pipSize = step > 0m ? step : 0.0001m;
		}

		_fastEma = new EMA { Length = FastEmaPeriod };
		_slowEma = new EMA { Length = SlowEmaPeriod };
		_macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = MacdFastPeriod,
			LongPeriod = MacdSlowPeriod,
			SignalPeriod = MacdSignalPeriod
		};
		_adx = new AverageDirectionalIndex { Length = AdxPeriod };
		_stochastic = new StochasticOscillator
		{
			KPeriod = StochasticPeriod,
			DPeriod = StochasticDsmoothing,
			Slowing = StochasticKsmoothing
		};
		_sar = new ParabolicSar
		{
			AccelerationStep = 0.055m,
			AccelerationMax = 0.21m
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_fastEma, _slowEma, _macd, _adx, _stochastic, _sar, ProcessIndicators)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawIndicator(area, _macd);
			DrawIndicator(area, _adx);
			DrawIndicator(area, _stochastic);
			DrawIndicator(area, _sar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessIndicators(
		ICandleMessage candle,
		IIndicatorValue fastValue,
		IIndicatorValue slowValue,
		IIndicatorValue macdValue,
		IIndicatorValue adxValue,
		IIndicatorValue stochasticValue,
		IIndicatorValue sarValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (CheckProtection(candle))
		{
		UpdateSarHistory(sarValue);
		return;
		}

		UpdateTrailing(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
		UpdateSarHistory(sarValue);
		return;
		}

		if (!fastValue.IsFinal || !slowValue.IsFinal || !macdValue.IsFinal || !adxValue.IsFinal || !stochasticValue.IsFinal || !sarValue.IsFinal)
		{
		UpdateSarHistory(sarValue);
		return;
		}

		var fast = fastValue.ToDecimal();
		var slow = slowValue.ToDecimal();

		if (macdValue is not MovingAverageConvergenceDivergenceValue macdData ||
		macdData.Macd is not decimal macdMain ||
		macdData.Signal is not decimal macdSignal)
		{
		UpdateSarHistory(sarValue);
		return;
		}

		var adxData = (AverageDirectionalIndexValue)adxValue;
		var dx = adxData.Dx;
		if (dx.Plus is not decimal plusDi || dx.Minus is not decimal minusDi)
		{
		UpdateSarHistory(sarValue);
		return;
		}

		var stochastic = (StochasticOscillatorValue)stochasticValue;
		var stochMain = stochastic.K;
		var stochSignal = stochastic.D;

		var baseVolume = BaseVolume;
		var buyVolume = baseVolume;
		var sellVolume = baseVolume;

		if (plusDi > minusDi && plusDi > AdxPositiveThreshold)
		{
			buyVolume *= SignalMultiplier;
		}
		else if (plusDi < minusDi && plusDi < AdxNegativeThreshold)
		{
			sellVolume *= SignalMultiplier;
		}

		if (fast > slow && stochMain > stochSignal && stochMain < StochasticOverbought)
		{
			buyVolume *= SignalMultiplier;
		}
		else if (fast < slow && stochMain < stochSignal && stochMain > StochasticOversold)
		{
			sellVolume *= SignalMultiplier;
		}

		if (macdMain > 0m && macdMain > macdSignal)
		{
			buyVolume *= SignalMultiplier;
		}
		else if (macdMain < 0m && macdMain < macdSignal)
		{
			sellVolume *= SignalMultiplier;
		}

		if (_previousSar.HasValue && _prePreviousSar.HasValue)
		{
			var priorSar = _previousSar.Value;
			var priorSar2 = _prePreviousSar.Value;

			if (fast > slow && priorSar > priorSar2)
			{
				buyVolume *= SignalMultiplier;
			}
			else if (fast < slow && priorSar < priorSar2)
			{
				sellVolume *= SignalMultiplier;
			}
		}

		var buyTriggered = buyVolume > baseVolume;
		var sellTriggered = sellVolume > baseVolume;

		if (buyTriggered && sellTriggered)
		{
		LogInfo("Both directions triggered on the same bar. Signal skipped.");
		UpdateSarHistory(sarValue);
		return;
		}

		if (buyTriggered)
		{
		EnterLong(candle, buyVolume);
		}
		else if (sellTriggered)
		{
		EnterShort(candle, sellVolume);
		}

		UpdateSarHistory(sarValue);
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
		ResetProtection();
		}
	}

	private bool CheckProtection(ICandleMessage candle)
	{
		if (Position > 0m)
		{
		var stopHit = _stopPrice > 0m && candle.LowPrice <= _stopPrice;
		var takeHit = _takePrice > 0m && candle.HighPrice >= _takePrice;

		if (stopHit || takeHit)
		{
			SellMarket(Position);
			LogInfo(stopHit ? "Long stop-loss triggered." : "Long take-profit triggered.");
			ResetProtection();
			return true;
		}
		}
		else if (Position < 0m)
		{
		var stopHit = _stopPrice > 0m && candle.HighPrice >= _stopPrice;
		var takeHit = _takePrice > 0m && candle.LowPrice <= _takePrice;

		if (stopHit || takeHit)
		{
			BuyMarket(Math.Abs(Position));
			LogInfo(stopHit ? "Short stop-loss triggered." : "Short take-profit triggered.");
			ResetProtection();
			return true;
		}
		}

		return false;
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0m || TrailingStepPips < 0m || _pipSize <= 0m || _entryPrice == 0m)
		return;

		var trailingDistance = GetOffset(TrailingStopPips);
		var trailingStep = GetOffset(TrailingStepPips);

		if (Position > 0m)
		{
		var profit = candle.ClosePrice - _entryPrice;
		var threshold = trailingDistance + trailingStep;
		if (profit > threshold)
		{
			var desiredStop = candle.ClosePrice - trailingDistance;
			var minStop = candle.ClosePrice - threshold;
			if (_stopPrice < minStop)
			{
				_stopPrice = desiredStop;
				LogInfo($"Update long trailing stop to {_stopPrice}");
				}
		}
		}
		else if (Position < 0m)
		{
		var profit = _entryPrice - candle.ClosePrice;
		var threshold = trailingDistance + trailingStep;
		if (profit > threshold)
		{
			var desiredStop = candle.ClosePrice + trailingDistance;
			var maxStop = candle.ClosePrice + threshold;
			if (_stopPrice == 0m || _stopPrice > maxStop)
			{
				_stopPrice = desiredStop;
				LogInfo($"Update short trailing stop to {_stopPrice}");
			}
		}
		}
	}

	private void EnterLong(ICandleMessage candle, decimal volume)
	{
		if (volume <= 0m)
		return;

		if (Position < 0m)
		{
		BuyMarket(Math.Abs(Position));
		}

		BuyMarket(volume);
		_entryPrice = candle.ClosePrice;
		_stopPrice = StopLossPips > 0m ? candle.ClosePrice - GetOffset(StopLossPips) : 0m;
		_takePrice = TakeProfitPips > 0m ? candle.ClosePrice + GetOffset(TakeProfitPips) : 0m;
		LogInfo($"Enter long at {_entryPrice} with volume {volume}");
	}

	private void EnterShort(ICandleMessage candle, decimal volume)
	{
		if (volume <= 0m)
		return;

		if (Position > 0m)
		{
		SellMarket(Position);
		}

		SellMarket(volume);
		_entryPrice = candle.ClosePrice;
		_stopPrice = StopLossPips > 0m ? candle.ClosePrice + GetOffset(StopLossPips) : 0m;
		_takePrice = TakeProfitPips > 0m ? candle.ClosePrice - GetOffset(TakeProfitPips) : 0m;
		LogInfo($"Enter short at {_entryPrice} with volume {volume}");
	}

	private void UpdateSarHistory(IIndicatorValue sarValue)
	{
		if (!sarValue.IsFinal)
		return;

		var current = sarValue.ToDecimal();
		_prePreviousSar = _previousSar;
		_previousSar = current;
	}

	private decimal GetOffset(decimal pips)
	{
	return pips > 0m && _pipSize > 0m ? pips * _pipSize : 0m;
	}

	private void ResetProtection()
	{
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
		return 0m;

		var decimals = CountDecimals(step);
		return decimals == 3 || decimals == 5 ? step * 10m : step;
	}

	private static int CountDecimals(decimal value)
	{
		value = Math.Abs(value);
		var decimals = 0;

		while (value != Math.Truncate(value) && decimals < 10)
		{
			value *= 10m;
			decimals++;
		}

		return decimals;
	}
}
