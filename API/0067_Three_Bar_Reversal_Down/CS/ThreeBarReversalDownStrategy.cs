using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Three-Bar Reversal Down pattern.
/// This pattern consists of three consecutive bars where:
/// 1. First bar is bullish (close > open)
/// 2. Second bar is bullish with a higher high than the first
/// 3. Third bar is bearish and closes below the low of the second bar
/// </summary>
public class ThreeBarReversalDownStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _requireUptrend;
	private readonly StrategyParam<int> _uptrendLength;

	private readonly Queue<ICandleMessage> _lastThreeCandles;
	private Highest _highestIndicator;

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage above the pattern's high.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Whether to require an uptrend before the pattern.
	/// </summary>
	public bool RequireUptrend
	{
		get => _requireUptrend.Value;
		set => _requireUptrend.Value = value;
	}

	/// <summary>
	/// Number of bars to look back for uptrend confirmation.
	/// </summary>
	public int UptrendLength
	{
		get => _uptrendLength.Value;
		set => _uptrendLength.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ThreeBarReversalDownStrategy"/>.
	/// </summary>
	public ThreeBarReversalDownStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 1.0m)
			.SetRange(0.5m, 3.0m)
			.SetDisplay("Stop Loss %", "Percentage above pattern's high for stop-loss", "Risk Management")
			.SetCanOptimize(true);

		_requireUptrend = Param(nameof(RequireUptrend), true)
			.SetDisplay("Require Uptrend", "Whether to require a prior uptrend", "Pattern Parameters");

		_uptrendLength = Param(nameof(UptrendLength), 5)
			.SetRange(3, 10)
			.SetDisplay("Uptrend Length", "Number of bars to check for uptrend", "Pattern Parameters")
			.SetCanOptimize(true);

		_lastThreeCandles = new Queue<ICandleMessage>(3);
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

		_lastThreeCandles.Clear();
		_highestIndicator = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

// Create highest indicator for uptrend identification
_highestIndicator = new Highest { Length = UptrendLength };

		// Subscribe to candles
		var subscription = SubscribeCandles(CandleType);

		// Bind candle processing with the highest indicator
		subscription
			.Bind(_highestIndicator, ProcessCandle)
			.Start();

		// Enable position protection
		StartProtection(
			new Unit(0, UnitTypes.Absolute), // No take profit (manual exit or on next pattern)
			new Unit(StopLossPercent, UnitTypes.Percent), // Stop loss above pattern's high
			false // No trailing
		);

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highestValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Already in position, no need to search for new patterns
		if (Position < 0)
		{
			UpdateCandleQueue(candle);
			return;
		}

		// Add current candle to the queue and maintain its size
		UpdateCandleQueue(candle);

		// Check if we have enough candles for pattern detection
		if (_lastThreeCandles.Count < 3)
			return;

		// Get the three candles for pattern analysis
		var candles = _lastThreeCandles.ToArray();
		var firstCandle = candles[0];
		var secondCandle = candles[1];
		var thirdCandle = candles[2]; // Current candle

		// Check for Three-Bar Reversal Down pattern:
		// 1. First candle is bullish
		var isFirstBullish = firstCandle.ClosePrice > firstCandle.OpenPrice;

		// 2. Second candle is bullish with a higher high
		var isSecondBullish = secondCandle.ClosePrice > secondCandle.OpenPrice;
		var hasSecondHigherHigh = secondCandle.HighPrice > firstCandle.HighPrice;

		// 3. Third candle is bearish and closes below second candle's low
		var isThirdBearish = thirdCandle.ClosePrice < thirdCandle.OpenPrice;
		var doesThirdCloseBelowSecondLow = thirdCandle.ClosePrice < secondCandle.LowPrice;

		// 4. Check if we're in an uptrend (if required)
		var isInUptrend = !RequireUptrend || IsInUptrend(highestValue);

		// Check if the pattern is complete
		if (isFirstBullish && isSecondBullish && hasSecondHigherHigh && 
			isThirdBearish && doesThirdCloseBelowSecondLow && isInUptrend)
		{
			// Pattern found - take short position
			var patternHigh = Math.Max(secondCandle.HighPrice, thirdCandle.HighPrice);
			var stopLoss = patternHigh * (1 + StopLossPercent / 100);

			SellMarket(Volume);
			LogInfo($"Three-Bar Reversal Down pattern detected at {thirdCandle.OpenTime}");
			LogInfo($"First bar: O={firstCandle.OpenPrice}, C={firstCandle.ClosePrice}, H={firstCandle.HighPrice}");
			LogInfo($"Second bar: O={secondCandle.OpenPrice}, C={secondCandle.ClosePrice}, H={secondCandle.HighPrice}");
			LogInfo($"Third bar: O={thirdCandle.OpenPrice}, C={thirdCandle.ClosePrice}");
			LogInfo($"Stop Loss set at {stopLoss}");
		}
	}

	private void UpdateCandleQueue(ICandleMessage candle)
	{
		_lastThreeCandles.Enqueue(candle);
		while (_lastThreeCandles.Count > 3)
			_lastThreeCandles.Dequeue();
	}

	private bool IsInUptrend(decimal highestValue)
	{
		// If we have the highest indicator value, check if current price is near it
		if (_lastThreeCandles.Count > 0)
		{
			var lastCandle = _lastThreeCandles.Peek();
			return Math.Abs(lastCandle.HighPrice - highestValue) / highestValue < 0.03m;
		}
		
		return false;
	}
}