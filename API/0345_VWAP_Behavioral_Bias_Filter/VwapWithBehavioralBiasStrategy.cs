using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// VWAP with Behavioral Bias Filter strategy.
	/// Entry condition:
	/// Long: Price < VWAP && Bias_Score < -Threshold (oversold with panic)
	/// Short: Price > VWAP && Bias_Score > Threshold (overbought with euphoria)
	/// Exit condition:
	/// Long: Price > VWAP
	/// Short: Price < VWAP
	/// </summary>
	public class VwapWithBehavioralBiasFilterStrategy : Strategy
	{
		private readonly StrategyParam<decimal> _biasThreshold;
		private readonly StrategyParam<int> _biasWindowSize;
		private readonly StrategyParam<decimal> _stopLoss;
		private readonly StrategyParam<DataType> _candleType;
		
		private VolumeWeightedAveragePrice _vwap;
		private decimal _currentBiasScore;
		
		// Tracks recent price movements for bias calculation
		private readonly Queue<decimal> _recentPriceMovements = [];
		
		// Flags to track positions
		private bool _isLong;
		private bool _isShort;
		
		/// <summary>
		/// Behavioral bias threshold for entry signal.
		/// </summary>
		public decimal BiasThreshold
		{
			get => _biasThreshold.Value;
			set => _biasThreshold.Value = value;
		}
		
		/// <summary>
		/// Window size for behavioral bias calculation.
		/// </summary>
		public int BiasWindowSize
		{
			get => _biasWindowSize.Value;
			set => _biasWindowSize.Value = value;
		}
		
		/// <summary>
		/// Stop loss percentage.
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
		public VwapWithBehavioralBiasFilterStrategy()
		{
			_biasThreshold = Param(nameof(BiasThreshold), 0.5m)
				.SetGreaterThanZero()
				.SetDisplay("Bias Threshold", "Threshold for behavioral bias", "Behavioral Settings")
				.SetCanOptimize(true)
				.SetOptimize(0.3m, 0.7m, 0.1m);
				
			_biasWindowSize = Param(nameof(BiasWindowSize), 20)
				.SetGreaterThanZero()
				.SetDisplay("Bias Window Size", "Window size for behavioral bias calculation", "Behavioral Settings")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);
				
			_stopLoss = Param(nameof(StopLoss), 2m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss (%)", "Stop Loss percentage from entry price", "Risk Management")
				.SetCanOptimize(true)
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
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);
			
			// Initialize flags
			_isLong = false;
			_isShort = false;
			
			// Initialize VWAP indicator
			_vwap = new VolumeWeightedAveragePrice();
			
			// Subscribe to candles and bind indicator
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.Bind(_vwap, ProcessCandle)
				.Start();
			
			// Create chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _vwap);
				DrawOwnTrades(area);
			}
			
			// Enable position protection with stop-loss
			StartProtection(
				new Unit(0),  // No take profit
				new Unit(StopLoss, UnitTypes.Percent) // Stop-loss as percentage
			);
		}
		
		/// <summary>
		/// Process each candle and VWAP value.
		/// </summary>
		private void ProcessCandle(ICandleMessage candle, decimal vwapValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;
			
			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Update behavioral bias score
			UpdateBehavioralBias(candle);
			
			var price = candle.ClosePrice;
			var priceBelowVwap = price < vwapValue;
			var priceAboveVwap = price > vwapValue;
			
			// Trading logic
			
			// Entry conditions
			
			// Long entry: Price below VWAP and negative bias score (panic)
			if (priceBelowVwap && _currentBiasScore < -BiasThreshold && !_isLong && Position <= 0)
			{
				LogInfo($"Long signal: Price {price} < VWAP {vwapValue}, Bias {_currentBiasScore} < -Threshold {-BiasThreshold}");
				BuyMarket(Volume);
				_isLong = true;
				_isShort = false;
			}
			// Short entry: Price above VWAP and positive bias score (euphoria)
			else if (priceAboveVwap && _currentBiasScore > BiasThreshold && !_isShort && Position >= 0)
			{
				LogInfo($"Short signal: Price {price} > VWAP {vwapValue}, Bias {_currentBiasScore} > Threshold {BiasThreshold}");
				SellMarket(Volume);
				_isShort = true;
				_isLong = false;
			}
			
			// Exit conditions
			
			// Exit long: Price rises above VWAP
			if (_isLong && priceAboveVwap && Position > 0)
			{
				LogInfo($"Exit long: Price {price} > VWAP {vwapValue}");
				SellMarket(Math.Abs(Position));
				_isLong = false;
			}
			// Exit short: Price falls below VWAP
			else if (_isShort && priceBelowVwap && Position < 0)
			{
				LogInfo($"Exit short: Price {price} < VWAP {vwapValue}");
				BuyMarket(Math.Abs(Position));
				_isShort = false;
			}
		}
		
		/// <summary>
		/// Update behavioral bias score based on recent price movements.
		/// This is a simplified model of behavioral biases in markets.
		/// </summary>
		private void UpdateBehavioralBias(ICandleMessage candle)
		{
			// Calculate price movement %
			decimal priceChange = 0;
			if (candle.OpenPrice != 0)
			{
				priceChange = (candle.ClosePrice - candle.OpenPrice) / candle.OpenPrice * 100;
			}
			
			// Add to queue
			_recentPriceMovements.Enqueue(priceChange);
			
			// Maintain window size
			while (_recentPriceMovements.Count > BiasWindowSize)
			{
				_recentPriceMovements.Dequeue();
			}
			
			// Not enough data yet
			if (_recentPriceMovements.Count < 5)
			{
				_currentBiasScore = 0;
				return;
			}
			
			// Calculate various components of bias score
			
			// 1. Recent momentum (last 5 candles)
			decimal recentMovement = 0;
			int count = 0;
			foreach (var movement in _recentPriceMovements)
			{
				if (count >= _recentPriceMovements.Count - 5)
				{
					recentMovement += movement;
				}
				count++;
			}
			
			// 2. Overreaction to recent news (volatility of recent moves)
			decimal volatility = 0;
			decimal sum = 0;
			decimal sumSquared = 0;
			
			foreach (var movement in _recentPriceMovements)
			{
				sum += movement;
				sumSquared += movement * movement;
			}
			
			decimal avg = sum / _recentPriceMovements.Count;
			decimal variance = (sumSquared / _recentPriceMovements.Count) - (avg * avg);
			volatility = (decimal)Math.Sqrt((double)Math.Max(0, variance));
			
			// 3. Herding behavior (consecutive moves in same direction)
			decimal previousMove = 0;
			int consecutiveSameDirection = 0;
			int maxConsecutive = 0;
			
			foreach (var movement in _recentPriceMovements)
			{
				if (previousMove != 0 && Math.Sign(movement) == Math.Sign(previousMove))
				{
					consecutiveSameDirection++;
					maxConsecutive = Math.Max(maxConsecutive, consecutiveSameDirection);
				}
				else
				{
					consecutiveSameDirection = 0;
				}
				previousMove = movement;
			}
			
			// 4. Current candle characteristics
			decimal bodySize = Math.Abs(candle.ClosePrice - candle.OpenPrice);
			decimal totalSize = candle.HighPrice - candle.LowPrice;
			decimal bodyRatio = totalSize > 0 ? bodySize / totalSize : 0;
			
			// Combined bias score calculation
			_currentBiasScore = 0;
			
			// Recent momentum component (range -0.5 to 0.5)
			_currentBiasScore += Math.Min(0.5m, Math.Max(-0.5m, recentMovement / 2));
			
			// Volatility component (range -0.3 to 0.3)
			// Higher volatility often indicates panic or euphoria
			_currentBiasScore += Math.Sign(recentMovement) * Math.Min(0.3m, volatility / 10);
			
			// Herding component (range -0.2 to 0.2)
			_currentBiasScore += Math.Sign(recentMovement) * Math.Min(0.2m, maxConsecutive / 10.0m);
			
			// Current candle strength component (range -0.2 to 0.2)
			if (candle.ClosePrice > candle.OpenPrice)
			{
				_currentBiasScore += bodyRatio * 0.2m; // Bullish bias
			}
			else
			{
				_currentBiasScore -= bodyRatio * 0.2m; // Bearish bias
			}
			
			// Ensure score is between -1 and 1
			_currentBiasScore = Math.Max(-1.0m, Math.Min(1.0m, _currentBiasScore));
			
			LogInfo($"Behavioral Bias: {_currentBiasScore}, Components: Momentum={recentMovement/2}, Volatility={volatility/10}, Herding={maxConsecutive/10.0m}, Candle={bodyRatio*0.2m}");
		}
	}
}