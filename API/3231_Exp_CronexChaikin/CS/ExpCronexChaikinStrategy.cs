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

using System.Reflection;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Cronex Chaikin oscillator crossover strategy converted from the MetaTrader expert Exp_CronexChaikin.mq5.
/// The strategy rebuilds the accumulation/distribution based Chaikin oscillator, applies Cronex-style double smoothing,
/// and opens or closes positions according to fast/slow line crossovers.
/// </summary>
public class ExpCronexChaikinStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<ChaikinAverageMethods> _chaikinMethod;
	private readonly StrategyParam<int> _chaikinFastPeriod;
	private readonly StrategyParam<int> _chaikinSlowPeriod;
	private readonly StrategyParam<CronexSmoothingMethods> _smoothingMethod;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _phase;
	private readonly StrategyParam<CronexVolumeTypes> _volumeType;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _buyOpenEnabled;
	private readonly StrategyParam<bool> _sellOpenEnabled;
	private readonly StrategyParam<bool> _buyCloseEnabled;
	private readonly StrategyParam<bool> _sellCloseEnabled;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;

	private LengthIndicator<decimal> _chaikinFast = null!;
	private LengthIndicator<decimal> _chaikinSlow = null!;
	private LengthIndicator<decimal> _cronexFast = null!;
	private LengthIndicator<decimal> _cronexSlow = null!;

	private decimal _adValue;
	private decimal[] _fastHistory = Array.Empty<decimal>();
	private decimal[] _slowHistory = Array.Empty<decimal>();
	private int _historyCount;

	/// <summary>
	/// Candle type used to build the Chaikin oscillator.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Moving average method applied to the accumulation/distribution line.
	/// </summary>
	public ChaikinAverageMethods ChaikinMethod
	{
		get => _chaikinMethod.Value;
		set => _chaikinMethod.Value = value;
	}

	/// <summary>
	/// Fast Chaikin averaging period.
	/// </summary>
	public int ChaikinFastPeriod
	{
		get => _chaikinFastPeriod.Value;
		set => _chaikinFastPeriod.Value = Math.Max(1, value);
	}

	/// <summary>
	/// Slow Chaikin averaging period.
	/// </summary>
	public int ChaikinSlowPeriod
	{
		get => _chaikinSlowPeriod.Value;
		set => _chaikinSlowPeriod.Value = Math.Max(1, value);
	}

	/// <summary>
	/// Cronex smoothing method applied to the Chaikin oscillator.
	/// </summary>
	public CronexSmoothingMethods SmoothingMethod
	{
		get => _smoothingMethod.Value;
		set => _smoothingMethod.Value = value;
	}

	/// <summary>
	/// Fast Cronex smoothing period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = Math.Max(1, value);
	}

	/// <summary>
	/// Slow Cronex smoothing period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = Math.Max(1, value);
	}

	/// <summary>
	/// Phase parameter used by Jurik-style smoothers.
	/// </summary>
	public int Phase
	{
		get => _phase.Value;
		set => _phase.Value = Math.Max(-100, Math.Min(100, value));
	}

	/// <summary>
	/// Volume source used in the accumulation/distribution calculation.
	/// </summary>
	public CronexVolumeTypes VolumeSource
	{
		get => _volumeType.Value;
		set => _volumeType.Value = value;
	}

	/// <summary>
	/// Number of completed bars back where the signal must be detected.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = Math.Max(0, value);
	}

	/// <summary>
	/// Enable or disable opening long positions.
	/// </summary>
	public bool BuyOpenEnabled
	{
		get => _buyOpenEnabled.Value;
		set => _buyOpenEnabled.Value = value;
	}

	/// <summary>
	/// Enable or disable opening short positions.
	/// </summary>
	public bool SellOpenEnabled
	{
		get => _sellOpenEnabled.Value;
		set => _sellOpenEnabled.Value = value;
	}

	/// <summary>
	/// Allow closing long positions when a bearish signal appears.
	/// </summary>
	public bool BuyCloseEnabled
	{
		get => _buyCloseEnabled.Value;
		set => _buyCloseEnabled.Value = value;
	}

	/// <summary>
	/// Allow closing short positions when a bullish signal appears.
	/// </summary>
	public bool SellCloseEnabled
	{
		get => _sellCloseEnabled.Value;
		set => _sellCloseEnabled.Value = value;
	}

	/// <summary>
	/// Take-profit distance in points.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = Math.Max(0, value);
	}

	/// <summary>
	/// Stop-loss distance in points.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = Math.Max(0, value);
	}

	/// <summary>
	/// Initializes strategy parameters with defaults matching the original expert advisor.
	/// </summary>
	public ExpCronexChaikinStrategy()
	{
		Volume = 1m;

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromHours(4)))
			.SetDisplay("Indicator Timeframe", "Time frame used for Cronex Chaikin calculations", "General");

		_chaikinMethod = Param(nameof(ChaikinMethod), ChaikinAverageMethods.Exponential)
			.SetDisplay("Chaikin MA", "Moving-average method for the accumulation/distribution line", "Chaikin");

		_chaikinFastPeriod = Param(nameof(ChaikinFastPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Chaikin Fast", "Fast averaging period for Chaikin oscillator", "Chaikin")
			.SetCanOptimize(true);

		_chaikinSlowPeriod = Param(nameof(ChaikinSlowPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Chaikin Slow", "Slow averaging period for Chaikin oscillator", "Chaikin")
			.SetCanOptimize(true);

		_smoothingMethod = Param(nameof(SmoothingMethod), CronexSmoothingMethods.Simple)
			.SetDisplay("Cronex Method", "Smoothing algorithm applied to Chaikin values", "Cronex");

		_fastPeriod = Param(nameof(FastPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Fast Smoothing", "Fast Cronex smoothing length", "Cronex")
			.SetCanOptimize(true);

		_slowPeriod = Param(nameof(SlowPeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("Slow Smoothing", "Slow Cronex smoothing length", "Cronex")
			.SetCanOptimize(true);

		_phase = Param(nameof(Phase), 15)
			.SetDisplay("Phase", "Phase parameter for Jurik-based smoothers", "Cronex")
			.SetCanOptimize(true)
			.SetOptimize(-100, 100, 5);

		_volumeType = Param(nameof(VolumeSource), CronexVolumeTypes.Tick)
			.SetDisplay("Volume Source", "Volume applied inside the accumulation/distribution formula", "Chaikin");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetGreaterOrEqualTo(0)
			.SetDisplay("Signal Bar", "Number of completed bars back for signal evaluation", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0, 3, 1);

		_buyOpenEnabled = Param(nameof(BuyOpenEnabled), true)
			.SetDisplay("Allow Long Entries", "Permit opening long positions", "Trading");

		_sellOpenEnabled = Param(nameof(SellOpenEnabled), true)
			.SetDisplay("Allow Short Entries", "Permit opening short positions", "Trading");

		_buyCloseEnabled = Param(nameof(BuyCloseEnabled), true)
			.SetDisplay("Close Long", "Allow closing long positions on bearish signals", "Trading");

		_sellCloseEnabled = Param(nameof(SellCloseEnabled), true)
			.SetDisplay("Close Short", "Allow closing short positions on bullish signals", "Trading");

		_takeProfit = Param(nameof(TakeProfit), 2000)
			.SetGreaterOrEqualToZero()
			.SetDisplay("Take Profit", "Target distance in points", "Protection")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 1000)
			.SetGreaterOrEqualToZero()
			.SetDisplay("Stop Loss", "Protective stop distance in points", "Protection")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetState();

		_chaikinFast = CreateChaikinAverage(ChaikinMethod, ChaikinFastPeriod);
		_chaikinSlow = CreateChaikinAverage(ChaikinMethod, ChaikinSlowPeriod);
		_cronexFast = CreateCronexSmoother(SmoothingMethod, FastPeriod, Phase);
		_cronexSlow = CreateCronexSmoother(SmoothingMethod, SlowPeriod, Phase);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.CloseTime ?? candle.OpenTime;
		var volume = GetVolume(candle);
		var adIncrement = CalculateAccumulationDistributionIncrement(candle, volume);
		_adValue += adIncrement;

		var fastResult = _chaikinFast.Process(new DecimalIndicatorValue(_chaikinFast, _adValue, time));
		if (fastResult is not DecimalIndicatorValue { IsFinal: true, Value: decimal fastAd })
			return;

		var slowResult = _chaikinSlow.Process(new DecimalIndicatorValue(_chaikinSlow, _adValue, time));
		if (slowResult is not DecimalIndicatorValue { IsFinal: true, Value: decimal slowAd })
			return;

		var chaikinValue = fastAd - slowAd;

		var cronexFastResult = _cronexFast.Process(new DecimalIndicatorValue(_cronexFast, chaikinValue, time));
		if (cronexFastResult is not DecimalIndicatorValue { IsFinal: true, Value: decimal fastValue })
			return;

		var cronexSlowResult = _cronexSlow.Process(new DecimalIndicatorValue(_cronexSlow, fastValue, time));
		if (cronexSlowResult is not DecimalIndicatorValue { IsFinal: true, Value: decimal slowValue })
			return;

		EnsureHistoryCapacity();
		ShiftHistory(fastValue, slowValue);

	var required = SignalBar + 2;
		if (_historyCount < required)
			return;

		var fastCurrent = _fastHistory[SignalBar];
		var slowCurrent = _slowHistory[SignalBar];
		var fastPrevious = _fastHistory[SignalBar + 1];
		var slowPrevious = _slowHistory[SignalBar + 1];

		var bullishNow = fastCurrent > slowCurrent;
		var bearishNow = fastCurrent < slowCurrent;
		var bullishCross = bullishNow && fastPrevious <= slowPrevious;
		var bearishCross = bearishNow && fastPrevious >= slowPrevious;

		if (!bullishNow && !bearishNow)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (bullishNow)
		{
			HandleBullishSignal(candle.ClosePrice, bullishCross);
		}
		else if (bearishNow)
		{
			HandleBearishSignal(candle.ClosePrice, bearishCross);
		}
	}

	private void HandleBullishSignal(decimal price, bool bullishCross)
	{
		var closingVolume = 0m;
		if (SellCloseEnabled && Position < 0m)
			closingVolume = Math.Abs(Position);

		var openingVolume = 0m;
		if (bullishCross && BuyOpenEnabled && Position <= 0m)
		{
			if (Position < 0m && !SellCloseEnabled)
				return;

			openingVolume = Volume;
		}

		ExecuteBuy(price, closingVolume, openingVolume);
	}

	private void HandleBearishSignal(decimal price, bool bearishCross)
	{
		var closingVolume = 0m;
		if (BuyCloseEnabled && Position > 0m)
			closingVolume = Position;

		var openingVolume = 0m;
		if (bearishCross && SellOpenEnabled && Position >= 0m)
		{
			if (Position > 0m && !BuyCloseEnabled)
				return;

			openingVolume = Volume;
		}

		ExecuteSell(price, closingVolume, openingVolume);
	}

	private void ExecuteBuy(decimal price, decimal closingVolume, decimal openingVolume)
	{
		var totalVolume = closingVolume + openingVolume;
		if (totalVolume <= 0m)
			return;

		var resultingPosition = Position + totalVolume;
		BuyMarket(totalVolume);

		if (openingVolume > 0m)
			ApplyProtection(price, resultingPosition);
	}

	private void ExecuteSell(decimal price, decimal closingVolume, decimal openingVolume)
	{
		var totalVolume = closingVolume + openingVolume;
		if (totalVolume <= 0m)
			return;

		var resultingPosition = Position - totalVolume;
		SellMarket(totalVolume);

		if (openingVolume > 0m)
			ApplyProtection(price, resultingPosition);
	}

	private void ApplyProtection(decimal price, decimal resultingPosition)
	{
		if (TakeProfit > 0)
			SetTakeProfit(TakeProfit, price, resultingPosition);

		if (StopLoss > 0)
			SetStopLoss(StopLoss, price, resultingPosition);
	}

	private decimal GetVolume(ICandleMessage candle)
	{
		return VolumeSource switch
		{
			CronexVolumeTypes.Tick => candle.TotalTicks.HasValue ? (decimal)candle.TotalTicks.Value : candle.TotalVolume ?? 0m,
			CronexVolumeTypes.Real => candle.TotalVolume ?? (candle.TotalTicks.HasValue ? (decimal)candle.TotalTicks.Value : 0m),
			_ => candle.TotalVolume ?? 0m,
		};
	}

	private static decimal CalculateAccumulationDistributionIncrement(ICandleMessage candle, decimal volume)
	{
		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;
		var range = high - low;

		if (range <= 0m || volume == 0m)
			return 0m;

		var multiplier = ((close - low) - (high - close)) / range;
		return multiplier * volume;
	}

	private static LengthIndicator<decimal> CreateChaikinAverage(ChaikinAverageMethods method, int length)
	{
		var normalized = Math.Max(1, length);

		return method switch
		{
			ChaikinAverageMethods.Simple => new SimpleMovingAverage { Length = normalized },
			ChaikinAverageMethods.Exponential => new ExponentialMovingAverage { Length = normalized },
			ChaikinAverageMethods.Smoothed => new SmoothedMovingAverage { Length = normalized },
			ChaikinAverageMethods.LinearWeighted => new WeightedMovingAverage { Length = normalized },
			_ => new ExponentialMovingAverage { Length = normalized },
		};
	}

	private static LengthIndicator<decimal> CreateCronexSmoother(CronexSmoothingMethods method, int length, int phase)
	{
		var normalized = Math.Max(1, length);

		return method switch
		{
			CronexSmoothingMethods.Simple => new SimpleMovingAverage { Length = normalized },
			CronexSmoothingMethods.Exponential => new ExponentialMovingAverage { Length = normalized },
			CronexSmoothingMethods.Smoothed => new SmoothedMovingAverage { Length = normalized },
			CronexSmoothingMethods.LinearWeighted => new WeightedMovingAverage { Length = normalized },
			CronexSmoothingMethods.Jjma => CreateJurik(normalized, phase),
			CronexSmoothingMethods.JurX => CreateJurik(normalized, phase),
			CronexSmoothingMethods.ParMa => new ExponentialMovingAverage { Length = normalized },
			CronexSmoothingMethods.T3 => new TripleExponentialMovingAverage { Length = normalized },
			CronexSmoothingMethods.Vidya => new ExponentialMovingAverage { Length = normalized },
			CronexSmoothingMethods.Ama => new KaufmanAdaptiveMovingAverage { Length = normalized },
			_ => new SimpleMovingAverage { Length = normalized },
		};
	}

	private static LengthIndicator<decimal> CreateJurik(int length, int phase)
	{
		var jurik = new JurikMovingAverage { Length = length };
		var property = jurik.GetType().GetProperty("Phase", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (property != null)
		{
			var value = Math.Max(-100, Math.Min(100, phase));
			property.SetValue(jurik, value);
		}

		return jurik;
	}

	private void ResetState()
	{
		_adValue = 0m;
		ResetHistory();
	}

	private void ResetHistory()
	{
		_fastHistory = Array.Empty<decimal>();
		_slowHistory = Array.Empty<decimal>();
		_historyCount = 0;
	}

	private void EnsureHistoryCapacity()
	{
		var required = SignalBar + 2;
		if (_fastHistory.Length == required)
			return;

		_fastHistory = new decimal[required];
		_slowHistory = new decimal[required];
		_historyCount = 0;
	}

	private void ShiftHistory(decimal fastValue, decimal slowValue)
	{
		var length = _fastHistory.Length;
		if (length == 0)
			return;

		for (var i = Math.Min(_historyCount, length - 1); i > 0; i--)
		{
			_fastHistory[i] = _fastHistory[i - 1];
			_slowHistory[i] = _slowHistory[i - 1];
		}

		_fastHistory[0] = fastValue;
		_slowHistory[0] = slowValue;

		if (_historyCount < length)
			_historyCount++;
	}
}

/// <summary>
/// Moving average methods available for the Chaikin oscillator calculation.
/// </summary>
public enum ChaikinAverageMethods
{
	/// <summary>
	/// Simple moving average.
	/// </summary>
	Simple,

	/// <summary>
	/// Exponential moving average.
	/// </summary>
	Exponential,

	/// <summary>
	/// Smoothed moving average (RMA).
	/// </summary>
	Smoothed,

	/// <summary>
	/// Linear weighted moving average.
	/// </summary>
	LinearWeighted,
}

/// <summary>
/// Cronex smoothing algorithms supported by the strategy.
/// </summary>
public enum CronexSmoothingMethods
{
	/// <summary>
	/// Simple moving average (SMA).
	/// </summary>
	Simple,

	/// <summary>
	/// Exponential moving average (EMA).
	/// </summary>
	Exponential,

	/// <summary>
	/// Smoothed moving average (SMMA).
	/// </summary>
	Smoothed,

	/// <summary>
	/// Linear weighted moving average (LWMA).
	/// </summary>
	LinearWeighted,

	/// <summary>
	/// Jurik moving average (JJMA variant).
	/// </summary>
	Jjma,

	/// <summary>
	/// Jurik moving average (JurX variant).
	/// </summary>
	JurX,

	/// <summary>
	/// Parabolic moving average approximation.
	/// </summary>
	ParMa,

	/// <summary>
	/// Tillson T3 moving average.
	/// </summary>
	T3,

	/// <summary>
	/// VIDYA adaptive moving average.
	/// </summary>
	Vidya,

	/// <summary>
	/// Kaufman adaptive moving average (AMA).
	/// </summary>
	Ama,
}

/// <summary>
/// Volume source applied to the accumulation/distribution formula.
/// </summary>
public enum CronexVolumeTypes
{
	/// <summary>
	/// Use tick volume when available.
	/// </summary>
	Tick,

	/// <summary>
	/// Use real volume when available.
	/// </summary>
	Real,
}

