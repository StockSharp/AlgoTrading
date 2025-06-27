using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that trades based on VWAP with Hidden Markov Model for market state detection.
	/// </summary>
	public class VwapHiddenMarkovModelStrategy : Strategy
	{
		private readonly StrategyParam<int> _hmmDataLength;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		private enum MarketState
		{
			Neutral,
			Bullish,
			Bearish
		}

		private MarketState _currentMarketState = MarketState.Neutral;
		
		// Feature data for HMM
		private readonly Queue<decimal> _priceData = [];
		private readonly Queue<decimal> _volumeData = [];
		
		// Transition probabilities
		private readonly decimal[,] _transitionMatrix = new decimal[3, 3]
		{
			{ 0.8m, 0.1m, 0.1m }, // Neutral -> Neutral, Bullish, Bearish
			{ 0.2m, 0.7m, 0.1m }, // Bullish -> Neutral, Bullish, Bearish
			{ 0.2m, 0.1m, 0.7m }  // Bearish -> Neutral, Bullish, Bearish
		};

		/// <summary>
		/// Strategy parameter: Length of data to use for HMM.
		/// </summary>
		public int HmmDataLength
		{
			get => _hmmDataLength.Value;
			set => _hmmDataLength.Value = value;
		}

		/// <summary>
		/// Strategy parameter: Stop-loss percentage.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Strategy parameter: Candle type.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public VwapHiddenMarkovModelStrategy()
		{
			_hmmDataLength = Param(nameof(HmmDataLength), 100)
				.SetGreaterThanZero()
				.SetDisplay("HMM Data Length", "Number of periods to use for HMM", "HMM Settings");

			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetGreaterThanZero
				.SetDisplay("Stop Loss %", "Stop Loss percentage from entry price", "Risk Management");

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

			// Reset state variables
			_currentMarketState = MarketState.Neutral;
			_priceData.Clear();
			_volumeData.Clear();

			// Create Vwap indicator
			var vwap = new VolumeWeightedAveragePrice();

			// Create subscription for candles
			var subscription = SubscribeCandles(CandleType);

			// Bind VWAP indicator to subscription and start
			subscription
				.Bind(vwap, ProcessVwap)
				.Start();

			// Add chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, vwap);
				DrawOwnTrades(area);
			}

			// Start position protection with percentage-based stop-loss
			StartProtection(
				takeProfit: new Unit(0), // No fixed take profit
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
			);
		}

		private void ProcessVwap(ICandleMessage candle, decimal vwapValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Update data for HMM
			UpdateHmmData(candle);

			// Run HMM algorithm when enough data is collected
			if (_priceData.Count >= HmmDataLength && _volumeData.Count >= HmmDataLength)
			{
				// Update current market state using HMM
				_currentMarketState = RunHmm();
				
				// Log market state updates periodically
				if (candle.OpenTime.Second == 0 && candle.OpenTime.Minute % 15 == 0)
				{
					LogInfo($"Current market state: {_currentMarketState}");
				}
			}

			// Trading logic based on VWAP and HMM state
			if (_currentMarketState == MarketState.Bullish && candle.ClosePrice > vwapValue && Position <= 0)
			{
				// Price above VWAP in bullish state - Buy signal
				LogInfo($"Buy signal: Price ({candle.ClosePrice}) above VWAP ({vwapValue}) in bullish state");
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (_currentMarketState == MarketState.Bearish && candle.ClosePrice < vwapValue && Position >= 0)
			{
				// Price below VWAP in bearish state - Sell signal
				LogInfo($"Sell signal: Price ({candle.ClosePrice}) below VWAP ({vwapValue}) in bearish state");
				SellMarket(Volume + Math.Abs(Position));
			}
		}

		private void UpdateHmmData(ICandleMessage candle)
		{
			// Add price data to queue
			_priceData.Enqueue(candle.ClosePrice);
			if (_priceData.Count > HmmDataLength)
				_priceData.Dequeue();

			// Add volume data to queue
			_volumeData.Enqueue(candle.TotalVolume);
			if (_volumeData.Count > HmmDataLength)
				_volumeData.Dequeue();
		}

		private MarketState RunHmm()
		{
			// This is a simplified implementation of Hidden Markov Model
			// A full implementation would use Baum-Welch algorithm for training and Viterbi algorithm for decoding
			
			// Convert data to observations
			var observations = GenerateObservations();
			
			// Decode the most likely state sequence (simplified Viterbi)
			var states = SimplifiedViterbi(observations);
			
			// Return the most recent state
			return states.Last();
		}

		private List<int> GenerateObservations()
		{
			// Generate observation sequence from price and volume data
			// This is a simplified approach - in a real implementation, we would
			// use more sophisticated techniques to generate observations
			
			var result = new List<int>();
			var prices = _priceData.ToArray();
			var volumes = _volumeData.ToArray();
			
			for (int i = 1; i < prices.Length; i++)
			{
				decimal priceChange = (prices[i] - prices[i - 1]) / prices[i - 1];
				decimal volumeRatio = volumes[i] / Math.Max(1, volumes[i - 1]);
				
				// Classify observation:
				// 0: Price down, low volume
				// 1: Price down, high volume
				// 2: Price up, low volume
				// 3: Price up, high volume
				
				int observation;
				if (priceChange < 0)
					observation = volumeRatio > 1.1m ? 1 : 0;
				else
					observation = volumeRatio > 1.1m ? 3 : 2;
				
				result.Add(observation);
			}
			
			return result;
		}

		private List<MarketState> SimplifiedViterbi(List<int> observations)
		{
			// This is a very simplified version of the Viterbi algorithm
			// For a real implementation, proper HMM libraries should be used
			
			// Emission probabilities: P(observation | state)
			var emissionMatrix = new decimal[3, 4]
			{
				{ 0.3m, 0.2m, 0.3m, 0.2m }, // Neutral -> obs0, obs1, obs2, obs3
				{ 0.1m, 0.1m, 0.3m, 0.5m }, // Bullish -> obs0, obs1, obs2, obs3
				{ 0.5m, 0.3m, 0.1m, 0.1m }  // Bearish -> obs0, obs1, obs2, obs3
			};
			
			// Initialize with equal probabilities for each state
			var currentStateProbabilities = new decimal[3] { 1m / 3, 1m / 3, 1m / 3 };
			var stateSequence = new List<MarketState>();
			
			// Process each observation
			foreach (var obs in observations)
			{
				var newProbabilities = new decimal[3];
				
				// Calculate new state probabilities based on observation and transition matrix
				for (int newState = 0; newState < 3; newState++)
				{
					decimal totalProb = 0;
					
					for (int oldState = 0; oldState < 3; oldState++)
					{
						totalProb += currentStateProbabilities[oldState] * 
							_transitionMatrix[oldState, newState] * 
							emissionMatrix[newState, obs];
					}
					
					newProbabilities[newState] = totalProb;
				}
				
				// Normalize probabilities
				decimal sum = newProbabilities.Sum();
				if (sum > 0)
				{
					for (int i = 0; i < 3; i++)
						newProbabilities[i] /= sum;
				}
				
				// Find most likely state
				int maxIndex = 0;
				for (int i = 1; i < 3; i++)
				{
					if (newProbabilities[i] > newProbabilities[maxIndex])
						maxIndex = i;
				}
				
				// Add state to sequence
				stateSequence.Add((MarketState)maxIndex);
				
				// Update current probabilities
				currentStateProbabilities = newProbabilities;
			}
			
			return stateSequence;
		}
	}
}
