using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader strategy "Elderv30aug05v".
/// The algorithm combines hourly MACD momentum with a 15-minute stochastic filter
/// and uses one-minute candles for price confirmation and trailing stop management.
/// </summary>
public class Elderv30aug05vStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _longStopLoss;
	private readonly StrategyParam<decimal> _shortStopLoss;
	private readonly StrategyParam<decimal> _longTrailingStop;
	private readonly StrategyParam<decimal> _shortTrailingStop;
	private readonly StrategyParam<decimal> _longStochasticThreshold;
	private readonly StrategyParam<decimal> _shortStochasticThreshold;
	private readonly StrategyParam<DataType> _baseCandleType;
	private readonly StrategyParam<DataType> _stochasticCandleType;
	private readonly StrategyParam<DataType> _macdCandleType;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _altMacdFastPeriod;
	private readonly StrategyParam<int> _altMacdSlowPeriod;
	private readonly StrategyParam<int> _altMacdSignalPeriod;
	private readonly StrategyParam<int> _stochasticFastKPeriod;
	private readonly StrategyParam<int> _stochasticFastDPeriod;
	private readonly StrategyParam<int> _stochasticFastSmooth;
	private readonly StrategyParam<int> _stochasticSlowKPeriod;
	private readonly StrategyParam<int> _stochasticSlowDPeriod;
	private readonly StrategyParam<int> _stochasticSlowSmooth;

	private MovingAverageConvergenceDivergenceSignal _primaryMacd = null!;
	private MovingAverageConvergenceDivergenceSignal _secondaryMacd = null!;
	private StochasticOscillator _fastStochastic = null!;
	private StochasticOscillator _slowStochastic = null!;

	private decimal? _primaryMacdCurrent;
	private decimal? _primaryMacdPrevious;
	private decimal? _secondaryMacdCurrent;
	private decimal? _secondaryMacdPrevious;
	private decimal? _fastStochasticCurrent;
	private decimal? _fastStochasticPrevious;
	private decimal? _slowStochasticCurrent;
	private decimal? _slowStochasticPrevious;

	private decimal _priceStep;
	private decimal? _entryPrice;
	private decimal? _longStopLevel;
	private decimal? _shortStopLevel;
	private decimal _previousMinuteHigh;
	private decimal _previousMinuteLow;
	private bool _hasPreviousMinute;

	/// <summary>
	/// Trading volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for long positions expressed in points.
	/// </summary>
	public decimal LongStopLoss
	{
		get => _longStopLoss.Value;
		set => _longStopLoss.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for short positions expressed in points.
	/// </summary>
	public decimal ShortStopLoss
	{
		get => _shortStopLoss.Value;
		set => _shortStopLoss.Value = value;
	}

	/// <summary>
	/// Trailing stop distance for long positions expressed in points.
	/// </summary>
	public decimal LongTrailingStop
	{
		get => _longTrailingStop.Value;
		set => _longTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing stop distance for short positions expressed in points.
	/// </summary>
	public decimal ShortTrailingStop
	{
		get => _shortTrailingStop.Value;
		set => _shortTrailingStop.Value = value;
	}

	/// <summary>
	/// Maximum stochastic value to allow long entries.
	/// </summary>
	public decimal LongStochasticThreshold
	{
		get => _longStochasticThreshold.Value;
		set => _longStochasticThreshold.Value = value;
	}

	/// <summary>
	/// Minimum stochastic value to allow short entries.
	/// </summary>
	public decimal ShortStochasticThreshold
	{
		get => _shortStochasticThreshold.Value;
		set => _shortStochasticThreshold.Value = value;
	}

	/// <summary>
	/// One-minute candle type used for price confirmation.
	/// </summary>
	public DataType BaseCandleType
	{
		get => _baseCandleType.Value;
		set => _baseCandleType.Value = value;
	}

	/// <summary>
	/// Fifteen-minute candle type used for stochastic oscillators.
	/// </summary>
	public DataType StochasticCandleType
	{
		get => _stochasticCandleType.Value;
		set => _stochasticCandleType.Value = value;
	}

	/// <summary>
	/// Hourly candle type used for MACD filters.
	/// </summary>
	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="Elderv30aug05vStrategy"/>.
	/// </summary>
	public Elderv30aug05vStrategy()
	{
		_volume = Param(nameof(Volume), 0.1m)
		.SetDisplay("Volume", "Trading volume for entries", "General")
		.SetCanOptimize(true);

		_longStopLoss = Param(nameof(LongStopLoss), 17m)
		.SetDisplay("Long Stop Loss", "Stop-loss for long trades in points", "Risk Management")
		.SetCanOptimize(true);

		_shortStopLoss = Param(nameof(ShortStopLoss), 46m)
		.SetDisplay("Short Stop Loss", "Stop-loss for short trades in points", "Risk Management")
		.SetCanOptimize(true);

		_longTrailingStop = Param(nameof(LongTrailingStop), 18m)
		.SetDisplay("Long Trailing Stop", "Trailing stop distance for long trades", "Risk Management")
		.SetCanOptimize(true);

		_shortTrailingStop = Param(nameof(ShortTrailingStop), 22m)
		.SetDisplay("Short Trailing Stop", "Trailing stop distance for short trades", "Risk Management")
		.SetCanOptimize(true);

		_longStochasticThreshold = Param(nameof(LongStochasticThreshold), 36m)
		.SetDisplay("Long Stochastic Threshold", "Maximum stochastic %K value for long trades", "Indicators")
		.SetCanOptimize(true);

		_shortStochasticThreshold = Param(nameof(ShortStochasticThreshold), 66m)
		.SetDisplay("Short Stochastic Threshold", "Minimum stochastic %K value for short trades", "Indicators")
		.SetCanOptimize(true);

		_baseCandleType = Param(nameof(BaseCandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Base Candle Type", "Primary candle series for trade management", "Data");

		_stochasticCandleType = Param(nameof(StochasticCandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Stochastic Candle Type", "Candle series for stochastic oscillators", "Data");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("MACD Candle Type", "Candle series for MACD filters", "Data");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 13)
		.SetDisplay("Primary MACD Fast", "Fast EMA length for the primary MACD", "Indicators")
		.SetCanOptimize(true);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 30)
		.SetDisplay("Primary MACD Slow", "Slow EMA length for the primary MACD", "Indicators")
		.SetCanOptimize(true);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
		.SetDisplay("Primary MACD Signal", "Signal EMA length for the primary MACD", "Indicators")
		.SetCanOptimize(true);

		_altMacdFastPeriod = Param(nameof(AltMacdFastPeriod), 14)
		.SetDisplay("Secondary MACD Fast", "Fast EMA length for the secondary MACD", "Indicators")
		.SetCanOptimize(true);

		_altMacdSlowPeriod = Param(nameof(AltMacdSlowPeriod), 56)
		.SetDisplay("Secondary MACD Slow", "Slow EMA length for the secondary MACD", "Indicators")
		.SetCanOptimize(true);

		_altMacdSignalPeriod = Param(nameof(AltMacdSignalPeriod), 9)
		.SetDisplay("Secondary MACD Signal", "Signal EMA length for the secondary MACD", "Indicators")
		.SetCanOptimize(true);

		_stochasticFastKPeriod = Param(nameof(StochasticFastKPeriod), 2)
		.SetDisplay("Fast Stochastic %K", "%K length for the fast stochastic", "Indicators")
		.SetCanOptimize(true);

		_stochasticFastDPeriod = Param(nameof(StochasticFastDPeriod), 3)
		.SetDisplay("Fast Stochastic %D", "%D length for the fast stochastic", "Indicators")
		.SetCanOptimize(true);

		_stochasticFastSmooth = Param(nameof(StochasticFastSmooth), 3)
		.SetDisplay("Fast Stochastic Smooth", "Smoothing for the fast stochastic", "Indicators")
		.SetCanOptimize(true);

		_stochasticSlowKPeriod = Param(nameof(StochasticSlowKPeriod), 1)
		.SetDisplay("Slow Stochastic %K", "%K length for the slow stochastic", "Indicators")
		.SetCanOptimize(true);

		_stochasticSlowDPeriod = Param(nameof(StochasticSlowDPeriod), 3)
		.SetDisplay("Slow Stochastic %D", "%D length for the slow stochastic", "Indicators")
		.SetCanOptimize(true);

		_stochasticSlowSmooth = Param(nameof(StochasticSlowSmooth), 3)
		.SetDisplay("Slow Stochastic Smooth", "Smoothing for the slow stochastic", "Indicators")
		.SetCanOptimize(true);
	}

	/// <summary>
	/// Fast EMA length for the primary MACD.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length for the primary MACD.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA length for the primary MACD.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Fast EMA length for the secondary MACD.
	/// </summary>
	public int AltMacdFastPeriod
	{
		get => _altMacdFastPeriod.Value;
		set => _altMacdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length for the secondary MACD.
	/// </summary>
	public int AltMacdSlowPeriod
	{
		get => _altMacdSlowPeriod.Value;
		set => _altMacdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA length for the secondary MACD.
	/// </summary>
	public int AltMacdSignalPeriod
	{
		get => _altMacdSignalPeriod.Value;
		set => _altMacdSignalPeriod.Value = value;
	}

	/// <summary>
	/// %K length for the fast stochastic oscillator.
	/// </summary>
	public int StochasticFastKPeriod
	{
		get => _stochasticFastKPeriod.Value;
		set => _stochasticFastKPeriod.Value = value;
	}

	/// <summary>
	/// %D length for the fast stochastic oscillator.
	/// </summary>
	public int StochasticFastDPeriod
	{
		get => _stochasticFastDPeriod.Value;
		set => _stochasticFastDPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing value for the fast stochastic oscillator.
	/// </summary>
	public int StochasticFastSmooth
	{
		get => _stochasticFastSmooth.Value;
		set => _stochasticFastSmooth.Value = value;
	}

	/// <summary>
	/// %K length for the slow stochastic oscillator.
	/// </summary>
	public int StochasticSlowKPeriod
	{
		get => _stochasticSlowKPeriod.Value;
		set => _stochasticSlowKPeriod.Value = value;
	}

	/// <summary>
	/// %D length for the slow stochastic oscillator.
	/// </summary>
	public int StochasticSlowDPeriod
	{
		get => _stochasticSlowDPeriod.Value;
		set => _stochasticSlowDPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing value for the slow stochastic oscillator.
	/// </summary>
	public int StochasticSlowSmooth
	{
		get => _stochasticSlowSmooth.Value;
		set => _stochasticSlowSmooth.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new (Security, DataType)[]
		{
			(Security, BaseCandleType),
			(Security, StochasticCandleType),
			(Security, MacdCandleType)
		};
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_primaryMacdCurrent = null;
		_primaryMacdPrevious = null;
		_secondaryMacdCurrent = null;
		_secondaryMacdPrevious = null;
		_fastStochasticCurrent = null;
		_fastStochasticPrevious = null;
		_slowStochasticCurrent = null;
		_slowStochasticPrevious = null;
		_entryPrice = null;
		_longStopLevel = null;
		_shortStopLevel = null;
		_previousMinuteHigh = 0m;
		_previousMinuteLow = 0m;
		_hasPreviousMinute = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = GetPriceStep();

		_primaryMacd = CreateMacd(MacdFastPeriod, MacdSlowPeriod, MacdSignalPeriod);
		_secondaryMacd = CreateMacd(AltMacdFastPeriod, AltMacdSlowPeriod, AltMacdSignalPeriod);
		_fastStochastic = CreateStochastic(StochasticFastKPeriod, StochasticFastDPeriod, StochasticFastSmooth);
		_slowStochastic = CreateStochastic(StochasticSlowKPeriod, StochasticSlowDPeriod, StochasticSlowSmooth);

		var baseSubscription = SubscribeCandles(BaseCandleType);
		baseSubscription.Bind(ProcessBaseCandle).Start();

		var stochasticSubscription = SubscribeCandles(StochasticCandleType);
		stochasticSubscription.BindEx(_fastStochastic, _slowStochastic, ProcessStochastic).Start();

		var macdSubscription = SubscribeCandles(MacdCandleType);
		macdSubscription.BindEx(_primaryMacd, _secondaryMacd, ProcessMacd).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, baseSubscription);
			DrawIndicator(area, _primaryMacd);
			DrawIndicator(area, _secondaryMacd);
			DrawIndicator(area, _fastStochastic);
			DrawIndicator(area, _slowStochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessMacd(ICandleMessage candle, IIndicatorValue primaryValue, IIndicatorValue secondaryValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!primaryValue.IsFinal || !secondaryValue.IsFinal)
		return;

		var primary = (MovingAverageConvergenceDivergenceSignalValue)primaryValue;
		if (primary.Macd is decimal primaryMacd)
		{
			_primaryMacdPrevious = _primaryMacdCurrent;
			_primaryMacdCurrent = primaryMacd;
		}

		var secondary = (MovingAverageConvergenceDivergenceSignalValue)secondaryValue;
		if (secondary.Macd is decimal secondaryMacd)
		{
			_secondaryMacdPrevious = _secondaryMacdCurrent;
			_secondaryMacdCurrent = secondaryMacd;
		}
	}

	private void ProcessStochastic(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!fastValue.IsFinal || !slowValue.IsFinal)
		return;

		var fast = (StochasticOscillatorValue)fastValue;
		if (fast.K is decimal fastK)
		{
			_fastStochasticPrevious = _fastStochasticCurrent;
			_fastStochasticCurrent = fastK;
		}

		var slow = (StochasticOscillatorValue)slowValue;
		if (slow.K is decimal slowK)
		{
			_slowStochasticPrevious = _slowStochasticCurrent;
			_slowStochasticCurrent = slowK;
		}
	}

	private void ProcessBaseCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		UpdateTrailingStops(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdatePreviousMinute(candle);
			return;
		}

		var tradeVolume = Volume;
		if (tradeVolume <= 0m)
		{
			UpdatePreviousMinute(candle);
			return;
		}

		if (Position == 0)
		{
			TryEnterPosition(candle, tradeVolume);
		}
		else if (Position > 0)
		{
			CheckLongExit(candle);
		}
		else
		{
			CheckShortExit(candle);
		}

		UpdatePreviousMinute(candle);
	}

	private void TryEnterPosition(ICandleMessage candle, decimal tradeVolume)
	{
		if (!_hasPreviousMinute ||
		_primaryMacdCurrent is not decimal primaryMacd ||
		_primaryMacdPrevious is not decimal primaryPrev ||
		_secondaryMacdCurrent is not decimal secondaryMacd ||
		_secondaryMacdPrevious is not decimal secondaryPrev ||
		_fastStochasticCurrent is not decimal fastK ||
		_fastStochasticPrevious is not decimal fastPrev ||
		_slowStochasticCurrent is not decimal slowK ||
		_slowStochasticPrevious is not decimal slowPrev)
		{
			return;
		}

		var priceStep = _priceStep;
		if (priceStep <= 0m)
		priceStep = 0.0001m;

		var closePrice = candle.ClosePrice;

		var longSignal =
		primaryMacd > primaryPrev &&
		primaryPrev < 0m &&
		fastK < LongStochasticThreshold &&
		fastK > fastPrev &&
		closePrice > _previousMinuteHigh;

		var shortSignal =
		secondaryMacd < secondaryPrev &&
		secondaryPrev > 0m &&
		slowK > ShortStochasticThreshold &&
		slowK < slowPrev &&
		closePrice < _previousMinuteLow;

		if (longSignal)
		{
			BuyMarket(tradeVolume);
			_entryPrice = closePrice;
			_longStopLevel = LongStopLoss > 0m ? closePrice - LongStopLoss * priceStep : null;
			_shortStopLevel = null;
		}
		else if (shortSignal)
		{
			SellMarket(tradeVolume);
			_entryPrice = closePrice;
			_shortStopLevel = ShortStopLoss > 0m ? closePrice + ShortStopLoss * priceStep : null;
			_longStopLevel = null;
		}
	}

	private void CheckLongExit(ICandleMessage candle)
	{
		if (_longStopLevel is decimal stop && candle.LowPrice <= stop)
		{
			SellMarket(Position);
			ResetPositionState();
			return;
		}
	}

	private void CheckShortExit(ICandleMessage candle)
	{
		if (_shortStopLevel is decimal stop && candle.HighPrice >= stop)
		{
			BuyMarket(Math.Abs(Position));
			ResetPositionState();
		}
	}

	private void UpdateTrailingStops(ICandleMessage candle)
	{
		if (_entryPrice is not decimal entry)
		return;

		var priceStep = _priceStep;
		if (priceStep <= 0m)
		priceStep = 0.0001m;

		if (Position > 0)
		{
			if (LongTrailingStop > 0m)
			{
				var distance = LongTrailingStop * priceStep;
				if (candle.ClosePrice - entry > distance)
				{
					var candidate = candle.ClosePrice - distance;
					if (_longStopLevel is not decimal current || candidate > current)
					_longStopLevel = candidate;
				}
			}
		}
		else if (Position < 0)
		{
			if (ShortTrailingStop > 0m)
			{
				var distance = ShortTrailingStop * priceStep;
				if (entry - candle.ClosePrice > distance)
				{
					var candidate = candle.ClosePrice + distance;
					if (_shortStopLevel is not decimal current || candidate < current)
					_shortStopLevel = candidate;
				}
			}
		}
	}

	private void UpdatePreviousMinute(ICandleMessage candle)
	{
		_previousMinuteHigh = candle.HighPrice;
		_previousMinuteLow = candle.LowPrice;
		_hasPreviousMinute = true;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_longStopLevel = null;
		_shortStopLevel = null;
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? 0.0001m;
		return step > 0m ? step : 0.0001m;
	}

	private static MovingAverageConvergenceDivergenceSignal CreateMacd(int fast, int slow, int signal)
	{
		return new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = fast },
				LongMa = { Length = slow }
			},
			SignalMa = { Length = signal }
		};
	}

	private static StochasticOscillator CreateStochastic(int k, int d, int smooth)
	{
		return new StochasticOscillator
		{
			KPeriod = k,
			DPeriod = d,
			Smooth = smooth
		};
	}
}
