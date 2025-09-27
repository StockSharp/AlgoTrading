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
/// Strategy converted from the MetaTrader "BandOsMA" expert advisor.
/// Combines the MACD histogram (OsMA) with Bollinger Bands and a moving average to trade reversals.
/// </summary>
public class BandOsMaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<IndicatorAppliedPrices> _priceType;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<int> _bollingerShift;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<MovingAverageMethods> _maMethod;

	private MovingAverageConvergenceDivergenceSignal _macd;
	private BollingerBands _bollinger;
	private IIndicator _osmaAverage;

	private readonly List<decimal> _osmaHistory = new();
	private readonly List<decimal> _upperBandHistory = new();
	private readonly List<decimal> _lowerBandHistory = new();
	private readonly List<decimal> _maHistory = new();

	private int _activeSignal;
	private decimal? _pipSize;
	private decimal? _longStop;
	private decimal? _shortStop;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal _trailingStep;

	/// <summary>
	/// Primary candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Trading volume expressed in lots.
	/// </summary>
	public decimal LotSize
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in MetaTrader points. Set to zero to disable protection.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Fast EMA length of the MACD oscillator.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length of the MACD oscillator.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA length of the MACD oscillator.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Applied price mapping that mirrors MetaTrader PRICE_* constants.
	/// </summary>
	public IndicatorAppliedPrices PriceType
	{
		get => _priceType.Value;
		set => _priceType.Value = value;
	}

	/// <summary>
	/// Period used by the Bollinger Bands calculated on the OsMA sequence.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Shift applied to the Bollinger Bands buffers.
	/// </summary>
	public int BollingerShift
	{
		get => _bollingerShift.Value;
		set => _bollingerShift.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for Bollinger Bands.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Period of the moving average that filters the OsMA line.
	/// </summary>
	public int MovingAveragePeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Shift applied to the moving average buffer.
	/// </summary>
	public int MovingAverageShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Moving average method matching the MetaTrader enumeration.
	/// </summary>
	public MovingAverageMethods MovingAverageMethods
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="BandOsMaStrategy"/> with defaults taken from the MetaTrader expert.
	/// </summary>
	public BandOsMaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used for indicator calculations", "General");

		_lotSize = Param(nameof(LotSize), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Lot Size", "Trade volume expressed in lots", "Risk")
		.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
		.SetNotNegative()
		.SetDisplay("Stop Loss (points)", "Protective stop distance in MetaTrader points", "Risk")
		.SetCanOptimize(true);

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
		.SetGreaterThanZero()
		.SetDisplay("MACD Fast", "Fast EMA length used inside MACD", "Indicators")
		.SetCanOptimize(true);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
		.SetGreaterThanZero()
		.SetDisplay("MACD Slow", "Slow EMA length used inside MACD", "Indicators")
		.SetCanOptimize(true);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD Signal", "Signal EMA length used inside MACD", "Indicators")
		.SetCanOptimize(true);

		_priceType = Param(nameof(PriceType), IndicatorAppliedPrices.Typical)
		.SetDisplay("Applied Price", "Price source forwarded to the MACD", "General")
		.SetCanOptimize(true);

		_bollingerPeriod = Param(nameof(BollingerPeriod), 26)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Period", "Number of OsMA samples used for Bollinger Bands", "Indicators")
		.SetCanOptimize(true);

		_bollingerShift = Param(nameof(BollingerShift), 0)
		.SetDisplay("Bollinger Shift", "Shift applied to Bollinger buffers (MetaTrader style)", "Indicators")
		.SetCanOptimize(true);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators")
		.SetCanOptimize(true);

		_maPeriod = Param(nameof(MovingAveragePeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("OsMA MA Period", "Length of the smoothing moving average", "Indicators")
		.SetCanOptimize(true);

		_maShift = Param(nameof(MovingAverageShift), 0)
		.SetDisplay("OsMA MA Shift", "Shift applied to the moving average buffer", "Indicators")
		.SetCanOptimize(true);

		_maMethod = Param(nameof(MovingAverageMethods), MovingAverageMethods.Simple)
		.SetDisplay("OsMA MA Method", "Moving average method applied to the OsMA", "Indicators")
		.SetCanOptimize(true);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_macd = null;
		_bollinger = null;
		_osmaAverage = null;

		_osmaHistory.Clear();
		_upperBandHistory.Clear();
		_lowerBandHistory.Clear();
		_maHistory.Clear();

		_activeSignal = 0;
		_pipSize = null;
		_longStop = null;
		_shortStop = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_trailingStep = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = LotSize;
		_pipSize = Security?.PriceStep ?? 0.0001m;
		if (_pipSize <= 0m)
		{
			_pipSize = 0.0001m;
		}

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = new ExponentialMovingAverage { Length = MacdFastPeriod },
				LongMa = new ExponentialMovingAverage { Length = MacdSlowPeriod }
			},
			SignalMa = new ExponentialMovingAverage { Length = MacdSignalPeriod }
		};

		_bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		_osmaAverage = CreateMovingAverage(MovingAverageMethods, MovingAveragePeriod);

		var subscription = SubscribeCandles(CandleType);
		subscription
		.WhenCandlesFinished(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		if (_macd is null || _bollinger is null || _osmaAverage is null)
		{
			return;
		}

		var price = GetAppliedPrice(candle, PriceType);
		var macdValue = (MovingAverageConvergenceDivergenceSignalValue)_macd.Process(price, candle.CloseTime, true);
		if (!macdValue.IsFinal || macdValue.Histogram is not decimal osma)
		{
			return;
		}

		var bollingerValue = (BollingerBandsValue)_bollinger.Process(new DecimalIndicatorValue(_bollinger, osma, candle.CloseTime));
		if (!bollingerValue.IsFinal || bollingerValue.UpperBand is not decimal upper || bollingerValue.LowerBand is not decimal lower)
		{
			return;
		}

		var maValue = _osmaAverage.Process(new DecimalIndicatorValue(_osmaAverage, osma, candle.CloseTime));
		if (!maValue.IsFinal)
		{
			return;
		}

		var ma = maValue.ToDecimal();

		_osmaHistory.Add(osma);
		_upperBandHistory.Add(upper);
		_lowerBandHistory.Add(lower);
		_maHistory.Add(ma);

		TrimHistory();

		if (!TryGetRecentPair(_osmaHistory, 0, out var currentOsma, out var previousOsma))
		{
			return;
		}

		if (!TryGetRecentPair(_upperBandHistory, BollingerShift, out var currentUpper, out var previousUpper))
		{
			return;
		}

		if (!TryGetRecentPair(_lowerBandHistory, BollingerShift, out var currentLower, out var previousLower))
		{
			return;
		}

		if (!TryGetRecentPair(_maHistory, MovingAverageShift, out var currentMa, out var previousMa))
		{
			return;
		}

		UpdateSignal(currentOsma, previousOsma, currentUpper, previousUpper, currentLower, previousLower, currentMa, previousMa);
		ManagePosition(candle);
	}

	private void UpdateSignal(decimal currentOsma, decimal previousOsma, decimal currentUpper, decimal previousUpper, decimal currentLower, decimal previousLower, decimal currentMa, decimal previousMa)
	{
		if (_activeSignal > 0)
		{
			if (currentOsma >= currentMa && previousOsma < previousMa)
			{
				_activeSignal = 0;
			}
		}
	else if (_activeSignal < 0)
	{
		if (currentOsma <= currentMa && previousOsma > previousMa)
		{
			_activeSignal = 0;
		}
	}

	if (currentOsma <= currentLower && previousOsma > previousLower)
	{
		_activeSignal = 1;
	}
else if (currentOsma >= currentUpper && previousOsma < previousUpper)
{
	_activeSignal = -1;
}
}

private void ManagePosition(ICandleMessage candle)
{
	if (!IsFormedAndOnlineAndAllowTrading())
	{
		return;
	}

	var stopDistance = StopLossPoints > 0m && _pipSize.HasValue ? StopLossPoints * _pipSize.Value : 0m;
	_trailingStep = stopDistance > 0m ? stopDistance / 50m : 0m;

	if (Position > 0m)
	{
		if (_activeSignal != 1)
		{
			SellMarket(Position);
			ResetStops();
		}
	else
	{
		UpdateLongTrailing(candle, stopDistance);
	}
}
else if (Position < 0m)
{
	if (_activeSignal != -1)
	{
		BuyMarket(-Position);
		ResetStops();
	}
else
{
	UpdateShortTrailing(candle, stopDistance);
}
}
else
{
	ResetStops();

	if (_activeSignal == 1)
	{
		BuyMarket(Volume);
		_longEntryPrice = candle.ClosePrice;
		if (stopDistance > 0m)
		{
			_longStop = candle.ClosePrice - stopDistance;
		}
	}
else if (_activeSignal == -1)
{
	SellMarket(Volume);
	_shortEntryPrice = candle.ClosePrice;
	if (stopDistance > 0m)
	{
		_shortStop = candle.ClosePrice + stopDistance;
	}
}
}
}

private void UpdateLongTrailing(ICandleMessage candle, decimal stopDistance)
{
	if (stopDistance <= 0m || !_longEntryPrice.HasValue)
	{
		return;
	}

	_longStop ??= _longEntryPrice.Value - stopDistance;

	var candidate = candle.ClosePrice - stopDistance;
	if (_longStop.Value + _trailingStep < candidate)
	{
		_longStop = candidate;
	}

	if (_longStop.HasValue && candle.LowPrice <= _longStop.Value)
	{
		SellMarket(Position);
		ResetStops();
	}
}

private void UpdateShortTrailing(ICandleMessage candle, decimal stopDistance)
{
	if (stopDistance <= 0m || !_shortEntryPrice.HasValue)
	{
		return;
	}

	_shortStop ??= _shortEntryPrice.Value + stopDistance;

	var candidate = candle.ClosePrice + stopDistance;
	if (_shortStop.Value - _trailingStep > candidate)
	{
		_shortStop = candidate;
	}

	if (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value)
	{
		BuyMarket(-Position);
		ResetStops();
	}
}

private void ResetStops()
{
	_longStop = null;
	_shortStop = null;
	_longEntryPrice = null;
	_shortEntryPrice = null;
}

private void TrimHistory()
{
	var maxLength = Math.Max(Math.Max(BollingerShift, MovingAverageShift), 2) + BollingerPeriod + 5;
	TrimList(_osmaHistory, maxLength);
	TrimList(_upperBandHistory, maxLength);
	TrimList(_lowerBandHistory, maxLength);
	TrimList(_maHistory, maxLength);
}

private static void TrimList(List<decimal> list, int maxLength)
{
	if (maxLength < 0)
	{
		maxLength = 0;
	}

	var extra = list.Count - maxLength;
	if (extra <= 0)
	{
		return;
	}

	list.RemoveRange(0, extra);
}

private static bool TryGetRecentPair(List<decimal> history, int shift, out decimal current, out decimal previous)
{
	current = 0m;
	previous = 0m;

	if (shift < 0)
	{
		shift = 0;
	}

	var currentIndex = history.Count - 1 - shift;
	var previousIndex = history.Count - 2 - shift;
	if (previousIndex < 0 || currentIndex >= history.Count)
	{
		return false;
	}

	current = history[currentIndex];
	previous = history[previousIndex];
	return true;
}

private static decimal GetAppliedPrice(ICandleMessage candle, IndicatorAppliedPrices priceType)
{
	return priceType switch
	{
		IndicatorAppliedPrices.Open => candle.OpenPrice,
		IndicatorAppliedPrices.High => candle.HighPrice,
		IndicatorAppliedPrices.Low => candle.LowPrice,
		IndicatorAppliedPrices.Median => (candle.HighPrice + candle.LowPrice) / 2m,
		IndicatorAppliedPrices.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
		IndicatorAppliedPrices.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m,
		_ => candle.ClosePrice
	};
}

private static IIndicator CreateMovingAverage(MovingAverageMethods method, int length)
{
	return method switch
	{
		MovingAverageMethods.Exponential => new ExponentialMovingAverage { Length = length },
		MovingAverageMethods.Smoothed => new SmoothedMovingAverage { Length = length },
		MovingAverageMethods.LinearWeighted => new WeightedMovingAverage { Length = length },
		_ => new SimpleMovingAverage { Length = length }
	};
}

	public enum IndicatorAppliedPrices
	{
		Close,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted
	}

	/// <summary>
	/// Moving average methods corresponding to MetaTrader's ENUM_MA_METHOD values.
	/// </summary>
	public enum MovingAverageMethods
	{
		Simple,
		Exponential,
		Smoothed,
		LinearWeighted
	}
}
