using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the "BounceNumber" MetaTrader indicator that counts how many times price bounces inside a channel before breaking it.
/// The strategy keeps track of the touch statistics and logs the distribution after each completed cycle.
/// </summary>
public class BounceNumberStrategy : Strategy
{
	private readonly StrategyParam<int> _maxHistoryCandles;
	private readonly StrategyParam<int> _channelPoints;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Dictionary<int, int> _bounceDistribution = new();

	private decimal? _channelCenter;
	private int _bounceCount;
	private int _lastTouchDirection;
	private int _candlesInCycle;

	/// <summary>
	/// Maximum number of candles allowed inside one channel cycle before it is forcefully reset.
	/// </summary>
	public int MaxHistoryCandles
	{
		get => _maxHistoryCandles.Value;
		set => _maxHistoryCandles.Value = value;
	}

	/// <summary>
	/// Half-width of the bounce channel expressed in price points.
	/// </summary>
	public int ChannelPoints
	{
		get => _channelPoints.Value;
		set => _channelPoints.Value = value;
	}

	/// <summary>
	/// Candle series that feeds the bounce counter.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Provides read-only access to the accumulated bounce distribution.
	/// </summary>
	public IReadOnlyDictionary<int, int> BounceDistribution => _bounceDistribution;

	/// <summary>
	/// Initializes a new instance of the <see cref="BounceNumberStrategy"/> class.
	/// </summary>
	public BounceNumberStrategy()
	{
		_maxHistoryCandles = Param(nameof(MaxHistoryCandles), 10000)
			.SetGreaterOrEqualZero()
			.SetDisplay("Max History Candles", "Maximum number of candles inspected inside a single channel cycle", "General")
			.SetCanOptimize(true);

		_channelPoints = Param(nameof(ChannelPoints), 300)
			.SetRange(10, 5000)
			.SetDisplay("Channel Half-Width", "Half height of the bounce channel measured in price points", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1)))
			.SetDisplay("Candle Type", "Timeframe used to perform the bounce analysis", "Data");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var priceStep = Security?.PriceStep;

		if (priceStep is null || priceStep.Value <= 0m)
			throw new InvalidOperationException("Security.PriceStep must be configured to convert point values into price offsets.");

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(OnProcessCandle)
			.Start();
	}

	private void OnProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var channelHalf = GetChannelHalfWidth();

		if (channelHalf <= 0m)
			return;

		if (_channelCenter is null)
		{
			ResetChannel(candle.ClosePrice, channelHalf);
			return;
		}

		_candlesInCycle++;

		var center = _channelCenter.Value;
		var upperBand = center + channelHalf;
		var lowerBand = center - channelHalf;
		var breakUpper = center + channelHalf * 2m;
		var breakLower = center - channelHalf * 2m;

		var candleHigh = candle.HighPrice;
		var candleLow = candle.LowPrice;

		var breakoutUp = candleHigh >= breakUpper;
		var breakoutDown = candleLow <= breakLower;

		if (breakoutUp || breakoutDown || (_candlesInCycle >= MaxHistoryCandles && MaxHistoryCandles > 0))
		{
			RegisterBounceResult();
			ResetChannel(candle.ClosePrice, channelHalf);
			return;
		}

		var touchedLower = candleLow <= lowerBand && candleHigh >= lowerBand;
		var touchedUpper = candleHigh >= upperBand && candleLow <= upperBand;

		if (touchedLower && _lastTouchDirection >= 0)
		{
			_bounceCount++;
			_lastTouchDirection = -1;
			LogInfo($"Lower band touch detected. Bounce count increased to {_bounceCount}.");
		}
		else if (touchedUpper && _lastTouchDirection <= 0)
		{
			_bounceCount++;
			_lastTouchDirection = 1;
			LogInfo($"Upper band touch detected. Bounce count increased to {_bounceCount}.");
		}
	}

	private void RegisterBounceResult()
	{
		if (!_bounceDistribution.TryGetValue(_bounceCount, out var occurrences))
			occurrences = 0;

		_bounceDistribution[_bounceCount] = occurrences + 1;

		LogInfo($"Channel cycle finished with {_bounceCount} bounce(s). Total occurrences for this count: {_bounceDistribution[_bounceCount]}.");
	}

	private void ResetChannel(decimal center, decimal channelHalf)
	{
		_channelCenter = center;
		_bounceCount = 0;
		_lastTouchDirection = 0;
		_candlesInCycle = 0;

		LogInfo($"Channel reset around price {center} with half-width {channelHalf}.");
	}

	private decimal GetChannelHalfWidth()
	{
		var priceStep = Security?.PriceStep;

		if (priceStep is null || priceStep.Value <= 0m)
			return 0m;

		return ChannelPoints * priceStep.Value;
	}
}
