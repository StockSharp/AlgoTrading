using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volatility breakout strategy converted from the MetaTrader Spasm expert advisor.
/// Tracks directional swings using adaptive thresholds derived from recent volatility.
/// </summary>
public class SpasmStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _volatilityMultiplier;
	private readonly StrategyParam<int> _volatilityPeriod;
	private readonly StrategyParam<bool> _useWeightedVolatility;
	private readonly StrategyParam<bool> _useOpenCloseRange;
	private readonly StrategyParam<decimal> _stopLossFraction;
	private readonly StrategyParam<DataType> _candleType;

	private LengthIndicator<decimal>? _volatilityIndicator;
	private decimal _priceStep;
	private decimal _threshold;
	private decimal _highestPrice;
	private decimal _lowestPrice;
	private DateTimeOffset _highestTime;
	private DateTimeOffset _lowestTime;
	private int _processedCandles;
	private bool _trendInitialized;
	private bool _isTrendUp;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;

	/// <summary>
	/// Order volume used for entries.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the average volatility to build breakout bands.
	/// </summary>
	public decimal VolatilityMultiplier
	{
		get => _volatilityMultiplier.Value;
		set => _volatilityMultiplier.Value = value;
	}

	/// <summary>
	/// Number of candles used to calculate the volatility average.
	/// </summary>
	public int VolatilityPeriod
	{
		get => _volatilityPeriod.Value;
		set => _volatilityPeriod.Value = value;
	}

	/// <summary>
	/// Enables linear weighted averaging of volatility instead of the simple mean.
	/// </summary>
	public bool UseWeightedVolatility
	{
		get => _useWeightedVolatility.Value;
		set => _useWeightedVolatility.Value = value;
	}

	/// <summary>
	/// When enabled uses the absolute open-close move instead of high-low range.
	/// </summary>
	public bool UseOpenCloseRange
	{
		get => _useOpenCloseRange.Value;
		set => _useOpenCloseRange.Value = value;
	}

	/// <summary>
	/// Fraction of the volatility threshold used to derive stop-loss distance.
	/// </summary>
	public decimal StopLossFraction
	{
		get => _stopLossFraction.Value;
		set => _stopLossFraction.Value = value;
	}

	/// <summary>
	/// Candle type that provides the working data series.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SpasmStrategy"/> class.
	/// </summary>
	public SpasmStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetDisplay("Volume", "Order volume for entries", "Trading")
			.SetGreaterThanZero();

		_volatilityMultiplier = Param(nameof(VolatilityMultiplier), 5m)
			.SetDisplay("Volatility Multiplier", "Multiplier applied to average volatility", "Trading")
			.SetGreaterThanZero();

		_volatilityPeriod = Param(nameof(VolatilityPeriod), 24)
			.SetDisplay("Volatility Period", "Number of candles used for volatility calculation", "Indicators")
			.SetGreaterThanZero();

		_useWeightedVolatility = Param(nameof(UseWeightedVolatility), false)
			.SetDisplay("Weighted Volatility", "Use linear weighted averaging instead of the simple mean", "Indicators");

		_useOpenCloseRange = Param(nameof(UseOpenCloseRange), false)
			.SetDisplay("Open-Close Range", "Use absolute open-close movement instead of high-low range", "Indicators");

		_stopLossFraction = Param(nameof(StopLossFraction), 0.5m)
			.SetDisplay("Stop Loss Fraction", "Fraction of volatility used to derive protective stop distance", "Risk")
			.SetGreaterOrEqual(0m)
			.SetLessOrEqual(1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");
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

		_volatilityIndicator = null;
		_priceStep = 0m;
		_threshold = 0m;
		_highestPrice = 0m;
		_lowestPrice = 0m;
		_highestTime = default;
		_lowestTime = default;
		_processedCandles = 0;
		_trendInitialized = false;
		_isTrendUp = false;
		_longStopPrice = null;
		_shortStopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 1m;
		if (_priceStep <= 0m)
			_priceStep = 1m;

		_volatilityIndicator = CreateVolatilityIndicator();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private LengthIndicator<decimal> CreateVolatilityIndicator()
	{
		return UseWeightedVolatility
			? new LinearWeightedMovingAverage { Length = VolatilityPeriod }
			: new SimpleMovingAverage { Length = VolatilityPeriod };
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_volatilityIndicator == null)
			return;

		if (_volatilityIndicator.Length != VolatilityPeriod)
			_volatilityIndicator.Length = VolatilityPeriod;

		_processedCandles++;

		if (!_trendInitialized)
		{
			UpdateInitializationExtremes(candle);

			if (_processedCandles >= VolatilityPeriod * 3)
			{
				// Determine the initial trend direction by comparing the timestamps of the latest extremes.
				_isTrendUp = _highestTime <= _lowestTime;
				_trendInitialized = true;
			}
		}

		var range = UseOpenCloseRange
			? Math.Abs(candle.OpenPrice - candle.ClosePrice)
			: candle.HighPrice - candle.LowPrice;

		// Feed the volatility indicator with the latest range sample.
		var volatilityValue = _volatilityIndicator.Process(range, candle.OpenTime, true).ToDecimal();

		if (!_volatilityIndicator.IsFormed)
			return;

		if (volatilityValue <= 0m)
			volatilityValue = _priceStep;

		_threshold = CalculateThreshold(volatilityValue);

		if (_threshold <= 0m)
			return;

		if (_trendInitialized)
			UpdateDynamicExtremes(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (StopLossFraction <= 0m)
		{
			_longStopPrice = null;
			_shortStopPrice = null;
		}
		else
		{
			CheckStops(candle);
		}

		var close = candle.ClosePrice;

		if (!_isTrendUp && close > _lowestPrice + _threshold)
		{
			// Breakout above the bearish band flips the trend to bullish and opens/rolls a long position.
			BuyMarket(Volume + Math.Abs(Position));
			_isTrendUp = true;
			_highestPrice = close;
			_highestTime = candle.OpenTime;
			_longStopPrice = CalculateStopPrice(close, true);
			_shortStopPrice = null;
		}
		else if (_isTrendUp && close < _highestPrice - _threshold)
		{
			// Breakout below the bullish band flips the trend to bearish and opens/rolls a short position.
			SellMarket(Volume + Math.Abs(Position));
			_isTrendUp = false;
			_lowestPrice = close;
			_lowestTime = candle.OpenTime;
			_shortStopPrice = CalculateStopPrice(close, false);
			_longStopPrice = null;
		}
	}

	private void UpdateInitializationExtremes(ICandleMessage candle)
	{
		if (_processedCandles == 1)
		{
			// Seed the extreme values with the very first finished candle.
			_highestPrice = candle.HighPrice;
			_lowestPrice = candle.LowPrice;
			_highestTime = candle.OpenTime;
			_lowestTime = candle.OpenTime;
			return;
		}

		if (candle.HighPrice >= _highestPrice)
		{
			_highestPrice = candle.HighPrice;
			_highestTime = candle.OpenTime;
		}

		if (candle.LowPrice <= _lowestPrice)
		{
			_lowestPrice = candle.LowPrice;
			_lowestTime = candle.OpenTime;
		}
	}

	private void UpdateDynamicExtremes(ICandleMessage candle)
	{
		// Expand the extremes only when the move exceeds the adaptive threshold.
		if (candle.HighPrice > _highestPrice + _threshold)
		{
			_highestPrice = candle.HighPrice;
			_highestTime = candle.OpenTime;
		}

		if (candle.LowPrice < _lowestPrice - _threshold)
		{
			_lowestPrice = candle.LowPrice;
			_lowestTime = candle.OpenTime;
		}
	}

	private void CheckStops(ICandleMessage candle)
	{
		if (StopLossFraction <= 0m)
			return;

		if (Position > 0 && _longStopPrice is decimal longStop && candle.LowPrice <= longStop)
		{
			// Exit the long position once price pierces the protective stop level.
			SellMarket(Position);
			_longStopPrice = null;
		}
		else if (Position < 0 && _shortStopPrice is decimal shortStop && candle.HighPrice >= shortStop)
		{
			// Exit the short position once price pierces the protective stop level.
			BuyMarket(Math.Abs(Position));
			_shortStopPrice = null;
		}
	}

	private decimal CalculateThreshold(decimal averageRange)
	{
		var points = averageRange / _priceStep;
		if (points <= 0m)
			points = 1m;

		var scaled = points * VolatilityMultiplier;
		if (scaled < 1m)
			scaled = 1m;

		return Math.Floor(scaled) * _priceStep;
	}

	private decimal? CalculateStopPrice(decimal entryPrice, bool isLong)
	{
		if (StopLossFraction <= 0m)
			return null;

		var thresholdPoints = _threshold / _priceStep;
		if (thresholdPoints <= 0m)
			return null;

		var stopPoints = thresholdPoints * StopLossFraction;
		var offset = stopPoints * _priceStep;

		var minOffset = 3m * _priceStep;
		if (offset < minOffset)
			offset = minOffset;

		var stopPrice = isLong ? entryPrice - offset : entryPrice + offset;
		if (stopPrice < 0m)
			stopPrice = 0m;

		return stopPrice;
	}
}
