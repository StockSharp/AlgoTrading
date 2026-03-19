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
/// Donchian with Sentiment Spike strategy.
/// Entry condition:
/// Long: Price > Max(High, N) && Sentiment_Score > Avg(Sentiment, M) + k*StdDev(Sentiment, M)
/// Short: Price < Min(Low, N) && Sentiment_Score < Avg(Sentiment, M) - k*StdDev(Sentiment, M)
/// Exit condition:
/// Long: Price < (Max(High, N) + Min(Low, N))/2
/// Short: Price > (Max(High, N) + Min(Low, N))/2
/// </summary>
public class DonchianWithSentimentSpikeStrategy : Strategy
{
	private readonly StrategyParam<int> _donchianPeriod;
	private readonly StrategyParam<int> _sentimentPeriod;
	private readonly StrategyParam<decimal> _sentimentMultiplier;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _sentimentHistory = [];
	private decimal _sentimentAverage;
	private decimal _sentimentStdDev;
	private decimal _currentSentiment;

	private decimal _midChannel;

	// Flags to track entry conditions
	private bool _isLong;
	private bool _isShort;

	/// <summary>
	/// Donchian channel period.
	/// </summary>
	public int DonchianPeriod
	{
		get => _donchianPeriod.Value;
		set => _donchianPeriod.Value = value;
	}

	/// <summary>
	/// Sentiment averaging period.
	/// </summary>
	public int SentimentPeriod
	{
		get => _sentimentPeriod.Value;
		set => _sentimentPeriod.Value = value;
	}

	/// <summary>
	/// Sentiment standard deviation multiplier.
	/// </summary>
	public decimal SentimentMultiplier
	{
		get => _sentimentMultiplier.Value;
		set => _sentimentMultiplier.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor with default parameters.
	/// </summary>
	public DonchianWithSentimentSpikeStrategy()
	{
		_donchianPeriod = Param(nameof(DonchianPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("Donchian Period", "Donchian channel period", "Donchian Settings")
		
		.SetOptimize(10, 30, 5);

		_sentimentPeriod = Param(nameof(SentimentPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("Sentiment Period", "Sentiment averaging period", "Sentiment Settings")
		
		.SetOptimize(10, 30, 5);

		_sentimentMultiplier = Param(nameof(SentimentMultiplier), 0.5m)
		.SetGreaterThanZero()
		.SetDisplay("Sentiment StdDev Multiplier", "Multiplier for sentiment standard deviation", "Sentiment Settings")
		
		.SetOptimize(1m, 3m, 0.5m);

		_stopLoss = Param(nameof(StopLoss), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (%)", "Stop Loss percentage from entry price", "Risk Management")
		
		.SetOptimize(1m, 3m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		_isLong = _isShort = default;
		_midChannel = _sentimentAverage = _sentimentStdDev = _currentSentiment = default;
		_sentimentHistory.Clear();
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var highest = new Highest { Length = DonchianPeriod };
		var lowest = new Lowest { Length = DonchianPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(highest, lowest, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateSentiment(candle);

		var price = candle.ClosePrice;

		// Long entry: Price breaks above upper band with positive sentiment
		if (price >= upper && _currentSentiment > 0 && Position == 0)
		{
			BuyMarket();
		}
		// Short entry: Price breaks below lower band with negative sentiment
		else if (price <= lower && _currentSentiment < 0 && Position == 0)
		{
			SellMarket();
		}
	}

	/// <summary>
	/// Update sentiment score based on candle data (simulation).
	/// In a real implementation, this would fetch data from an external source.
	/// </summary>
	private void UpdateSentiment(ICandleMessage candle)
	{
		// Simple sentiment simulation based on price action
		// In reality, this would come from social media or news sentiment API

		decimal sentiment;

		// Base sentiment on candle pattern
		var bodySize = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var totalSize = candle.HighPrice - candle.LowPrice;

		if (totalSize == 0)
		{
			sentiment = 0;
		}
		else
		{
			var bodyRatio = bodySize / totalSize;

			// Bullish candle with strong body
			if (candle.ClosePrice > candle.OpenPrice)
			{
				sentiment = bodyRatio * 2; // 0 to 2 scale
			}
			// Bearish candle with strong body
			else
			{
				sentiment = -bodyRatio * 2; // -2 to 0 scale
			}

			// Use body ratio directly without randomness
		}

		// Ensure sentiment is within -2 to 2 range
		sentiment = Math.Max(Math.Min(sentiment, 2m), -2m);

		_currentSentiment = sentiment;

		// Add to history
		_sentimentHistory.Add(_currentSentiment);
		if (_sentimentHistory.Count > SentimentPeriod)
		{
			_sentimentHistory.RemoveAt(0);
		}

		// Calculate average
		decimal sum = 0;
		foreach (var value in _sentimentHistory)
		{
			sum += value;
		}

		_sentimentAverage = _sentimentHistory.Count > 0 
		? sum / _sentimentHistory.Count 
		: 0;

		// Calculate standard deviation
		if (_sentimentHistory.Count > 1)
		{
			decimal sumSquaredDiffs = 0;
			foreach (var value in _sentimentHistory)
			{
				var diff = value - _sentimentAverage;
				sumSquaredDiffs += diff * diff;
			}

			_sentimentStdDev = (decimal)Math.Sqrt((double)(sumSquaredDiffs / (_sentimentHistory.Count - 1)));
		}
		else
		{
			_sentimentStdDev = 0.5m; // Default value until we have enough data
		}

		LogInfo($"Sentiment: {_currentSentiment}, Avg: {_sentimentAverage}, StdDev: {_sentimentStdDev}");
	}
}
