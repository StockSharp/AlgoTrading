using System;
using System.Collections.Generic;
using Ecng.Common;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// MACD with Sentiment Filter strategy.
	/// Entry condition:
	/// Long: MACD > Signal && Sentiment_Score > Threshold
	/// Short: MACD < Signal && Sentiment_Score < -Threshold
	/// Exit condition:
	/// Long: MACD < Signal
	/// Short: MACD > Signal
	/// </summary>
	public class MacdWithSentimentFilterStrategy : Strategy
	{
		private readonly StrategyParam<int> _macdFast;
		private readonly StrategyParam<int> _macdSlow;
		private readonly StrategyParam<int> _macdSignal;
		private readonly StrategyParam<decimal> _threshold;
		private readonly StrategyParam<decimal> _stopLoss;
		private readonly StrategyParam<DataType> _candleType;
		
		// Sentiment score from external data source (simplified with simulation for this example)
		private decimal _sentimentScore;
		// Last MACD and Signal values stored from the previous candle
		private decimal _prevMacd;
		private decimal _prevSignal;
		
		/// <summary>
		/// MACD Fast period.
		/// </summary>
		public int MacdFast
		{
			get => _macdFast.Value;
			set => _macdFast.Value = value;
		}
		
		/// <summary>
		/// MACD Slow period.
		/// </summary>
		public int MacdSlow
		{
			get => _macdSlow.Value;
			set => _macdSlow.Value = value;
		}
		
		/// <summary>
		/// MACD Signal period.
		/// </summary>
		public int MacdSignal
		{
			get => _macdSignal.Value;
			set => _macdSignal.Value = value;
		}
		
		/// <summary>
		/// Sentiment threshold for entry signal.
		/// </summary>
		public decimal Threshold
		{
			get => _threshold.Value;
			set => _threshold.Value = value;
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
		public MacdWithSentimentFilterStrategy()
		{
			_macdFast = Param(nameof(MacdFast), 12)
				.SetGreaterThanZero()
				.SetDisplay("MACD Fast", "Fast moving average period for MACD", "MACD Settings")
				.SetCanOptimize(true)
				.SetOptimize(8, 20, 1);
				
			_macdSlow = Param(nameof(MacdSlow), 26)
				.SetGreaterThanZero()
				.SetDisplay("MACD Slow", "Slow moving average period for MACD", "MACD Settings")
				.SetCanOptimize(true)
				.SetOptimize(20, 34, 2);
				
			_macdSignal = Param(nameof(MacdSignal), 9)
				.SetGreaterThanZero()
				.SetDisplay("MACD Signal", "Signal line period for MACD", "MACD Settings")
				.SetCanOptimize(true)
				.SetOptimize(5, 13, 1);
				
			_threshold = Param(nameof(Threshold), 0.5m)
				.SetGreaterThanZero()
				.SetDisplay("Sentiment Threshold", "Threshold for sentiment filter", "Sentiment Settings")
				.SetCanOptimize(true)
				.SetOptimize(0.2m, 0.8m, 0.1m);
				
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
			
			// Create MACD indicator
			
			var macd = new MovingAverageConvergenceDivergenceSignal
			{
				Macd =
				{
					ShortMa = { Length = MacdFast },
					LongMa = { Length = MacdSlow },
				},
				SignalMa = { Length = MacdSignal }
			};
			// Initialize sentiment score
			_sentimentScore = 0;
			
			// Subscribe to candles and bind indicator
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(macd, ProcessCandle)
				.Start();
			
			// Create chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, macd);
				DrawOwnTrades(area);
			}
			
			// Enable position protection with stop-loss
			StartProtection(
				new Unit(0), // No take profit
				new Unit(StopLoss, UnitTypes.Percent) // Stop-loss as percentage
			);
		}
		
		/// <summary>
		/// Process each candle and MACD values.
		/// </summary>
		private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;
			
			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Update sentiment score (in a real system this would come from external source)
			UpdateSentimentScore(candle);

			var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
			var macd = macdTyped.Macd;
			var signal = macdTyped.Signal;

			// Store previous MACD values for state tracking
			var prevMacdOverSignal = _prevMacd > _prevSignal;
			var currMacdOverSignal = macd > signal;
			
			// Update previous values for next candle
			_prevMacd = macd;
			_prevSignal = signal;
			
			// First candle, just store values
			if (IsFirstRun())
				return;
			
			// Entry conditions with sentiment filter
			if (prevMacdOverSignal != currMacdOverSignal)
			{
				// MACD crossed above signal with positive sentiment - go long
				if (currMacdOverSignal && _sentimentScore > Threshold && Position <= 0)
				{
					LogInfo("Long signal: MACD crossed above signal with positive sentiment");
					BuyMarket(Volume);
				}
				// MACD crossed below signal with negative sentiment - go short
				else if (!currMacdOverSignal && _sentimentScore < -Threshold && Position >= 0)
				{
					LogInfo("Short signal: MACD crossed below signal with negative sentiment");
					SellMarket(Volume);
				}
			}
			// Exit conditions (without sentiment filter)
			else
			{
				// MACD below signal - exit long position
				if (!currMacdOverSignal && Position > 0)
				{
					LogInfo("Exit long: MACD below signal");
					SellMarket(Math.Abs(Position));
				}
				// MACD above signal - exit short position
				else if (currMacdOverSignal && Position < 0)
				{
					LogInfo("Exit short: MACD above signal");
					BuyMarket(Math.Abs(Position));
				}
			}
		}
		
		/// <summary>
		/// Update sentiment score based on candle data (simulation).
		/// In a real implementation, this would fetch data from an external source.
		/// </summary>
		private void UpdateSentimentScore(ICandleMessage candle)
		{
			// Simple simulation of sentiment based on candle pattern
			// In reality, this would be a call to a sentiment API or database
			
			var bodySize = Math.Abs(candle.ClosePrice - candle.OpenPrice);
			var totalSize = candle.HighPrice - candle.LowPrice;
			
			if (totalSize == 0)
				return;
			
			var bodyRatio = bodySize / totalSize;
			
			// Bullish candle with strong body
			if (candle.ClosePrice > candle.OpenPrice && bodyRatio > 0.7m)
			{
				_sentimentScore = Math.Min(_sentimentScore + 0.2m, 1m);
			}
			// Bearish candle with strong body
			else if (candle.ClosePrice < candle.OpenPrice && bodyRatio > 0.7m)
			{
				_sentimentScore = Math.Max(_sentimentScore - 0.2m, -1m);
			}
			// Add random noise to sentiment
			else
			{
				_sentimentScore += (decimal)((RandomGen.GetDouble() - 0.5) * 0.1);
				_sentimentScore = Math.Max(Math.Min(_sentimentScore, 1m), -1m);
			}
			
			LogInfo($"Updated sentiment score: {_sentimentScore}");
		}
		
		/// <summary>
		/// Check if this is the first run to avoid trading on first candle.
		/// </summary>
		private bool IsFirstRun()
		{
			return _prevMacd == 0 && _prevSignal == 0;
		}
	}
}
