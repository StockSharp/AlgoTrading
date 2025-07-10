using System;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using System.Linq;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Bollinger Bands with K-Means clustering strategy.
	/// Uses Bollinger Bands indicator along with a simple K-Means clustering algorithm
	/// to identify overbought/oversold conditions.
	/// </summary>
	public class BollingerKMeansStrategy : Strategy
	{
		private readonly StrategyParam<int> _bollingerLength;
		private readonly StrategyParam<decimal> _bollingerDeviation;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _kMeansHistoryLength;
		
		private BollingerBands _bollinger;
		private decimal _atrValue;
		
		// Cluster state tracking
		private enum ClusterState
		{
			Oversold,
			Neutral,
			Overbought
		}
		
		private ClusterState _currentClusterState = ClusterState.Neutral;
		private readonly SynchronizedList<decimal> _rsiValues = [];
		private readonly SynchronizedList<decimal> _priceValues = [];
		private readonly SynchronizedList<decimal> _volumeValues = [];
		
		private RelativeStrengthIndex _rsi;
		private AverageTrueRange _atr;

		/// <summary>
		/// Bollinger Bands period.
		/// </summary>
		public int BollingerLength
		{
			get => _bollingerLength.Value;
			set => _bollingerLength.Value = value;
		}

		/// <summary>
		/// Bollinger Bands standard deviation multiplier.
		/// </summary>
		public decimal BollingerDeviation
		{
			get => _bollingerDeviation.Value;
			set => _bollingerDeviation.Value = value;
		}

		/// <summary>
		/// Candle type to use for the strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}
		
		/// <summary>
		/// Length of history for K-Means clustering.
		/// </summary>
		public int KMeansHistoryLength
		{
			get => _kMeansHistoryLength.Value;
			set => _kMeansHistoryLength.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BollingerKMeansStrategy"/>.
		/// </summary>
		public BollingerKMeansStrategy()
		{
			_bollingerLength = Param(nameof(BollingerLength), 20)
				.SetDisplay("Bollinger Length", "Length of the Bollinger Bands indicator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
				.SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
				
			_kMeansHistoryLength = Param(nameof(KMeansHistoryLength), 50)
				.SetDisplay("K-Means History Length", "Length of history for K-Means clustering", "Clustering")
				.SetCanOptimize(true)
				.SetOptimize(30, 100, 10);
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

			_atrValue = default;
			_currentClusterState = ClusterState.Neutral;
			_rsiValues.Clear();
			_priceValues.Clear();
			_volumeValues.Clear();

			// Create indicators
			_bollinger = new BollingerBands
			{
				Length = BollingerLength,
				Width = BollingerDeviation
			};
			
			_rsi = new RelativeStrengthIndex
			{
				Length = 14
			};
			
			_atr = new AverageTrueRange
			{
				Length = 14
			};

			// Create and initialize subscription
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.BindEx(
					_bollinger, 
					_rsi,
					_atr,
					ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _bollinger);
				DrawOwnTrades(area);
			}
			
			// Setup position protection
			StartProtection(
				new Unit(2, UnitTypes.Percent), 
				new Unit(2, UnitTypes.Percent)
			);
		}
		
		private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue rsiValue, IIndicatorValue atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var bollingerTyped = (BollingerBandsValue)bollingerValue;

			// Extract values from indicators
			var bollingerUpper = bollingerTyped.UpBand;
			var bollingerMiddle = bollingerTyped.MovingAverage;
			var bollingerLower = bollingerTyped.LowBand;

			var rsi = rsiValue.ToDecimal();
			_atrValue = atrValue.ToDecimal();
			
			// Update data for clustering
			UpdateClusterData(candle, rsi);
			
			// Calculate K-Means clusters and determine market state
			CalculateClusters();
			
			// Trading logic
			if (candle.ClosePrice < bollingerLower && _currentClusterState == ClusterState.Oversold && Position <= 0)
			{
				// Buy signal - price below lower band and in oversold cluster
				BuyMarket(Volume);
				LogInfo($"Buy Signal: Price below lower band ({bollingerLower:F2}) in oversold cluster");
			}
			else if (candle.ClosePrice > bollingerUpper && _currentClusterState == ClusterState.Overbought && Position >= 0)
			{
				// Sell signal - price above upper band and in overbought cluster
				SellMarket(Volume + Math.Abs(Position));
				LogInfo($"Sell Signal: Price above upper band ({bollingerUpper:F2}) in overbought cluster");
			}
			else if (Position > 0 && candle.ClosePrice > bollingerMiddle)
			{
				// Exit long position when price returns to middle band
				SellMarket(Position);
				LogInfo($"Exit Long: Price returned to middle band ({bollingerMiddle:F2})");
			}
			else if (Position < 0 && candle.ClosePrice < bollingerMiddle)
			{
				// Exit short position when price returns to middle band
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exit Short: Price returned to middle band ({bollingerMiddle:F2})");
			}
		}
		
		private void UpdateClusterData(ICandleMessage candle, decimal rsi)
		{
			// Add current values to the data series
			_priceValues.Add(candle.ClosePrice);
			_rsiValues.Add(rsi);
			_volumeValues.Add(candle.TotalVolume);
			
			// Maintain the desired history length
			while (_priceValues.Count > KMeansHistoryLength)
			{
				_priceValues.RemoveAt(0);
				_rsiValues.RemoveAt(0);
				_volumeValues.RemoveAt(0);
			}
		}
		
		private void CalculateClusters()
		{
			// Only perform clustering when we have enough data
			if (_priceValues.Count < KMeansHistoryLength)
				return;
				
			// Normalize the data (simple min-max normalization)
			var normalizedRsi = _rsiValues.Last() / 100m;  // RSI is already 0-100
			
			// Find min/max for price normalization
			decimal? minPrice = null;
			decimal? maxPrice = null;
			
			foreach (var price in _priceValues)
			{
				if (minPrice == null || price < minPrice.Value)
					minPrice = price;
				if (maxPrice == null || price > maxPrice.Value)
					maxPrice = price;
			}
			
			// Normalize the last price
			decimal normalizedPrice = 0.5m;
			if (minPrice.HasValue && maxPrice.HasValue && maxPrice.Value != minPrice.Value)
			{
				var priceRange = maxPrice.Value - minPrice.Value;
				normalizedPrice = (_priceValues.Last() - minPrice.Value) / priceRange;
			}
			
			// Simple rules-based clustering (simplified K-means approximation)
			// Oversold: Low RSI (< 30) and price near bottom of range
			// Overbought: High RSI (> 70) and price near top of range
			// Neutral: Everything else
			
			if (normalizedRsi < 0.3m && normalizedPrice < 0.3m)
			{
				_currentClusterState = ClusterState.Oversold;
			}
			else if (normalizedRsi > 0.7m && normalizedPrice > 0.7m)
			{
				_currentClusterState = ClusterState.Overbought;
			}
			else
			{
				_currentClusterState = ClusterState.Neutral;
			}
			
			LogInfo($"Cluster State: {_currentClusterState}, Normalized RSI: {normalizedRsi:F2}, Normalized Price: {normalizedPrice:F2}");
		}
	}
}