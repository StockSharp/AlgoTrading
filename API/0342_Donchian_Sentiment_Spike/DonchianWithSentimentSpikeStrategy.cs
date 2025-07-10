using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using System;
using System.Collections.Generic;
using Ecng.Common;

namespace StockSharp.Samples.Strategies
{
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
			_donchianPeriod = Param(nameof(DonchianPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Donchian Period", "Donchian channel period", "Donchian Settings")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);
				
			_sentimentPeriod = Param(nameof(SentimentPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Sentiment Period", "Sentiment averaging period", "Sentiment Settings")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);
				
			_sentimentMultiplier = Param(nameof(SentimentMultiplier), 2m)
				.SetGreaterThanZero()
				.SetDisplay("Sentiment StdDev Multiplier", "Multiplier for sentiment standard deviation", "Sentiment Settings")
				.SetCanOptimize(true)
				.SetOptimize(1m, 3m, 0.5m);
				
			_stopLoss = Param(nameof(StopLoss), 2m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss (%)", "Stop Loss percentage from entry price", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1m, 3m, 0.5m);
				
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
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
			
			// Initialize flags
			_isLong = false;
			_isShort = false;
			_midChannel = 0;
			_sentimentHistory.Clear();
			_sentimentAverage = 0;
			_sentimentStdDev = 0;
			_currentSentiment = 0;

			// Create Donchian Channel indicator
			var donchian = new DonchianChannels { Length = DonchianPeriod };
			
			// Subscribe to candles and bind indicator
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.BindEx(donchian, ProcessCandle)
				.Start();
			
			// Create chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, donchian);
				DrawOwnTrades(area);
			}
			
			// Enable position protection with stop-loss
			StartProtection(
				new Unit(0),  // No take profit
				new Unit(StopLoss, UnitTypes.Percent) // Stop-loss as percentage
			);
		}
		
		/// <summary>
		/// Process each candle and Donchian Channel values.
		/// </summary>
		private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;
			
			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Update sentiment data (in a real system, this would come from external source)
			UpdateSentiment(candle);

			// Extract Donchian Channel values
			var donchianTyped = (DonchianChannelsValue)donchianValue;
			
			if (donchianTyped.UpperBand is not decimal upperBand ||
				donchianTyped.LowerBand is not decimal lowerBand ||
				donchianTyped.Middle is not decimal middleBand)
			{
				return;
			}
			
			// Store middle band for exit conditions
			_midChannel = middleBand;
			
			// Calculate sentiment thresholds
			var bullishSentimentThreshold = _sentimentAverage + SentimentMultiplier * _sentimentStdDev;
			var bearishSentimentThreshold = _sentimentAverage - SentimentMultiplier * _sentimentStdDev;
			
			var price = candle.ClosePrice;
			
			// Trading logic
			
			// Entry conditions
			
			// Long entry: Price breaks above upper band with positive sentiment spike
			if (price > upperBand && _currentSentiment > bullishSentimentThreshold && !_isLong && Position <= 0)
			{
				LogInfo($"Long signal: Price {price} > Upper Band {upperBand}, Sentiment {_currentSentiment} > Threshold {bullishSentimentThreshold}");
				BuyMarket(Volume);
				_isLong = true;
				_isShort = false;
			}
			// Short entry: Price breaks below lower band with negative sentiment spike
			else if (price < lowerBand && _currentSentiment < bearishSentimentThreshold && !_isShort && Position >= 0)
			{
				LogInfo($"Short signal: Price {price} < Lower Band {lowerBand}, Sentiment {_currentSentiment} < Threshold {bearishSentimentThreshold}");
				SellMarket(Volume);
				_isShort = true;
				_isLong = false;
			}
			
			// Exit conditions
			
			// Exit long: Price falls below middle band
			if (_isLong && price < _midChannel && Position > 0)
			{
				LogInfo($"Exit long: Price {price} < Middle Band {_midChannel}");
				SellMarket(Math.Abs(Position));
				_isLong = false;
			}
			// Exit short: Price rises above middle band
			else if (_isShort && price > _midChannel && Position < 0)
			{
				LogInfo($"Exit short: Price {price} > Middle Band {_midChannel}");
				BuyMarket(Math.Abs(Position));
				_isShort = false;
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
				
				// Add some randomness
				sentiment += (decimal)((RandomGen.GetDouble() - 0.5) * 0.5);
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
}