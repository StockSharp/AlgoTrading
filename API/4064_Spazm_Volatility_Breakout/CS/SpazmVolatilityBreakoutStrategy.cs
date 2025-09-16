using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volatility breakout strategy converted from the MetaTrader 4 expert advisor "Spazm".
/// Tracks swing extremes and reverses direction when price moves beyond an adaptive volatility band.
/// </summary>
public class SpazmVolatilityBreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _volatilityMultiplier;
	private readonly StrategyParam<int> _volatilityPeriod;
	private readonly StrategyParam<bool> _useWeightedVolatility;
	private readonly StrategyParam<bool> _useOpenCloseRange;
	private readonly StrategyParam<decimal> _stopLossMultiplier;
	private readonly StrategyParam<bool> _drawSwingLines;
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
	private (DateTimeOffset time, decimal price)? _lastRecordedHigh;
	private (DateTimeOffset time, decimal price)? _lastRecordedLow;

	/// <summary>
	/// Trade volume used for every market entry.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the averaged volatility to size the breakout threshold.
	/// </summary>
	public decimal VolatilityMultiplier
	{
		get => _volatilityMultiplier.Value;
		set => _volatilityMultiplier.Value = value;
	}

	/// <summary>
	/// Number of candles used when estimating volatility and seeding the initial swings.
	/// </summary>
	public int VolatilityPeriod
	{
		get => _volatilityPeriod.Value;
		set => _volatilityPeriod.Value = value;
	}

	/// <summary>
	/// Enables the linear weighted moving average instead of the simple mean for volatility.
	/// </summary>
	public bool UseWeightedVolatility
	{
		get => _useWeightedVolatility.Value;
		set => _useWeightedVolatility.Value = value;
	}

	/// <summary>
	/// Uses the absolute open-close move as range input instead of the high-low span when enabled.
	/// </summary>
	public bool UseOpenCloseRange
	{
		get => _useOpenCloseRange.Value;
		set => _useOpenCloseRange.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the volatility threshold when computing protective stops.
	/// </summary>
	public decimal StopLossMultiplier
	{
		get => _stopLossMultiplier.Value;
		set => _stopLossMultiplier.Value = value;
	}

	/// <summary>
	/// Enables drawing of trend lines that connect the latest bullish and bearish pivots.
	/// </summary>
	public bool DrawSwingLines
	{
		get => _drawSwingLines.Value;
		set => _drawSwingLines.Value = value;
	}

	/// <summary>
	/// Candle type providing the working data series.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SpazmVolatilityBreakoutStrategy"/> class.
	/// </summary>
	public SpazmVolatilityBreakoutStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetDisplay("Volume", "Order volume for market entries", "Trading")
			.SetGreaterThanZero();

		_volatilityMultiplier = Param(nameof(VolatilityMultiplier), 5m)
			.SetDisplay("Volatility Multiplier", "Multiplier applied to the averaged range", "Trading")
			.SetGreaterThanZero();

		_volatilityPeriod = Param(nameof(VolatilityPeriod), 24)
			.SetDisplay("Volatility Period", "Number of candles used for volatility estimation", "Indicators")
			.SetGreaterThanZero();

		_useWeightedVolatility = Param(nameof(UseWeightedVolatility), false)
			.SetDisplay("Weighted Volatility", "Use linear weighted moving average instead of the simple mean", "Indicators");

		_useOpenCloseRange = Param(nameof(UseOpenCloseRange), false)
			.SetDisplay("Open-Close Range", "Use absolute open-close difference instead of high-low range", "Indicators");

		_stopLossMultiplier = Param(nameof(StopLossMultiplier), 0m)
			.SetDisplay("Stop Loss Multiplier", "Multiplier applied to the threshold for stop calculation", "Risk")
			.SetGreaterOrEqual(0m);

		_drawSwingLines = Param(nameof(DrawSwingLines), true)
			.SetDisplay("Draw Swing Lines", "Draw lines connecting the latest bullish and bearish pivots", "Visualization");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for analysis", "General");
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
		_lastRecordedHigh = null;
		_lastRecordedLow = null;
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

		UpdateExtremeBounds(candle);

		var range = UseOpenCloseRange
			? Math.Abs(candle.OpenPrice - candle.ClosePrice)
			: candle.HighPrice - candle.LowPrice;

		if (range < 0m)
			range = 0m;

		var rangePoints = range / _priceStep;
		var averagePoints = _volatilityIndicator.Process(rangePoints, candle.OpenTime, true).ToDecimal();

		if (!_volatilityIndicator.IsFormed)
			return;

		_threshold = CalculateThreshold(averagePoints);
		if (_threshold <= 0m)
			return;

		if (!_trendInitialized)
		{
			InitializeTrendIfReady();
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		ManageStops(candle);

		var close = candle.ClosePrice;

		if (_isTrendUp)
		{
			if (close < _highestPrice - _threshold)
				SwitchToShort(candle);
		}
		else
		{
			if (close > _lowestPrice + _threshold)
				SwitchToLong(candle);
		}
	}

	private void UpdateExtremeBounds(ICandleMessage candle)
	{
		if (_processedCandles == 1)
		{
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

	private void InitializeTrendIfReady()
	{
		var required = Math.Max(VolatilityPeriod * 3, 1);
		if (_processedCandles < required)
			return;

		_trendInitialized = true;
		_isTrendUp = _lowestTime >= _highestTime;
		_lastRecordedHigh = (_highestTime, _highestPrice);
		_lastRecordedLow = (_lowestTime, _lowestPrice);
	}

	private decimal CalculateThreshold(decimal averagePoints)
	{
		if (averagePoints <= 0m)
			averagePoints = 1m;

		var scaledPoints = averagePoints * VolatilityMultiplier;
		if (scaledPoints <= 0m)
			scaledPoints = 1m;

		return scaledPoints * _priceStep;
	}

	private void ManageStops(ICandleMessage candle)
	{
		if (StopLossMultiplier <= 0m)
		{
			_longStopPrice = null;
			_shortStopPrice = null;
			return;
		}

		if (Position > 0 && _longStopPrice is decimal longStop && candle.LowPrice <= longStop)
		{
			SellMarket(Position);
			_longStopPrice = null;
		}
		else if (Position < 0 && _shortStopPrice is decimal shortStop && candle.HighPrice >= shortStop)
		{
			BuyMarket(Math.Abs(Position));
			_shortStopPrice = null;
		}
	}

	private void SwitchToShort(ICandleMessage candle)
	{
		_lastRecordedHigh = (_highestTime, _highestPrice);

		if (DrawSwingLines && _lastRecordedLow.HasValue && _lastRecordedHigh.HasValue)
		{
			var low = _lastRecordedLow.Value;
			var high = _lastRecordedHigh.Value;
			DrawLine(low.time, low.price, high.time, high.price);
		}

		var volume = Volume + Math.Abs(Position);
		if (volume > 0m)
			SellMarket(volume);

		_isTrendUp = false;
		_lowestPrice = candle.ClosePrice;
		_lowestTime = candle.OpenTime;
		_longStopPrice = null;
		_shortStopPrice = CalculateStopPrice(candle.ClosePrice, false);
	}

	private void SwitchToLong(ICandleMessage candle)
	{
		_lastRecordedLow = (_lowestTime, _lowestPrice);

		if (DrawSwingLines && _lastRecordedLow.HasValue && _lastRecordedHigh.HasValue)
		{
			var low = _lastRecordedLow.Value;
			var high = _lastRecordedHigh.Value;
			DrawLine(low.time, low.price, high.time, high.price);
		}

		var volume = Volume + Math.Abs(Position);
		if (volume > 0m)
			BuyMarket(volume);

		_isTrendUp = true;
		_highestPrice = candle.ClosePrice;
		_highestTime = candle.OpenTime;
		_shortStopPrice = null;
		_longStopPrice = CalculateStopPrice(candle.ClosePrice, true);
	}

	private decimal? CalculateStopPrice(decimal entryPrice, bool isLong)
	{
		if (StopLossMultiplier <= 0m || _threshold <= 0m)
			return null;

		var thresholdPoints = _threshold / _priceStep;
		if (thresholdPoints <= 0m)
			return null;

		var stopPoints = thresholdPoints * StopLossMultiplier;
		if (stopPoints <= 0m)
			return null;

		var offset = stopPoints * _priceStep;
		var minOffset = 3m * _priceStep;
		if (offset < minOffset)
			offset = minOffset;

		var stopPrice = isLong ? entryPrice - offset : entryPrice + offset;
		return stopPrice < 0m ? 0m : stopPrice;
	}
}
