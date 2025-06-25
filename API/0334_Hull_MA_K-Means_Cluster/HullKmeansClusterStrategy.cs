using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Localization;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that trades based on Hull Moving Average direction with K-Means clustering for market state detection.
	/// </summary>
	public class HullKMeansClusterStrategy : Strategy
	{
		private readonly StrategyParam<int> _hullPeriod;
		private readonly StrategyParam<int> _clusterDataLength;
		private readonly StrategyParam<int> _rsiPeriod;
		private readonly StrategyParam<DataType> _candleType;

		private enum MarketState
		{
			Neutral,
			Bullish,
			Bearish
		}

		private decimal _prevHullValue;
		private MarketState _currentMarketState = MarketState.Neutral;

		// Feature data for clustering
		private readonly Queue<decimal> _priceChangeData = new Queue<decimal>();
		private readonly Queue<decimal> _rsiData = new Queue<decimal>();
		private readonly Queue<decimal> _volumeRatioData = new Queue<decimal>();

		private decimal _lastPrice;
		private decimal _avgVolume;

		/// <summary>
		/// Strategy parameter: Hull Moving Average period.
		/// </summary>
		public int HullPeriod
		{
			get => _hullPeriod.Value;
			set => _hullPeriod.Value = value;
		}

		/// <summary>
		/// Strategy parameter: Length of data to use for clustering.
		/// </summary>
		public int ClusterDataLength
		{
			get => _clusterDataLength.Value;
			set => _clusterDataLength.Value = value;
		}

		/// <summary>
		/// Strategy parameter: RSI period for feature calculation.
		/// </summary>
		public int RsiPeriod
		{
			get => _rsiPeriod.Value;
			set => _rsiPeriod.Value = value;
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
		public HullKMeansClusterStrategy()
		{
			_hullPeriod = Param(nameof(HullPeriod), 9)
				.SetGreaterThanZero()
				.SetDisplay("Hull MA Period", "Period for Hull Moving Average", "Indicator Settings");

			_clusterDataLength = Param(nameof(ClusterDataLength), 50)
				.SetGreaterThanZero()
				.SetDisplay("Cluster Data Length", "Number of periods to use for clustering", "Clustering Settings");

			_rsiPeriod = Param(nameof(RsiPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("RSI Period", "Period for RSI calculation as a clustering feature", "Indicator Settings");

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
			_prevHullValue = 0;
			_currentMarketState = MarketState.Neutral;
			_lastPrice = 0;
			_avgVolume = 0;
			
			_priceChangeData.Clear();
			_rsiData.Clear();
			_volumeRatioData.Clear();

			// Create Hull Moving Average indicator
			var hullMa = new HullMovingAverage
			{
				Length = HullPeriod
			};

			// Create RSI indicator for feature calculation
			var rsi = new RelativeStrengthIndex
			{
				Length = RsiPeriod
			};

			// Create subscription for candles
			var subscription = SubscribeCandles(CandleType);

			// Bind indicators to subscription and start
			subscription
				.Bind(hullMa, rsi, ProcessCandle)
				.Start();

			// Add chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, hullMa);
				DrawOwnTrades(area);
			}

			// Start position protection with ATR-based stop-loss
			StartProtection(
				takeProfit: new Unit(0), // No fixed take profit
				stopLoss: new Unit(2, UnitTypes.Absolute) // 2 ATR stop-loss
			);
		}

		private void ProcessCandle(ICandleMessage candle, decimal hullValue, decimal rsiValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Update feature data for clustering
			UpdateFeatureData(candle, rsiValue);

			// Perform K-Means clustering when enough data is collected
			if (_priceChangeData.Count >= ClusterDataLength && 
				_rsiData.Count >= ClusterDataLength && 
				_volumeRatioData.Count >= ClusterDataLength)
			{
				// Perform K-Means clustering for market state detection
				_currentMarketState = DetectMarketState();
				LogInfo($"Current market state: {_currentMarketState}");
			}

			// Check for Hull MA direction change
			bool isHullRising = hullValue > _prevHullValue;

			// Trading logic based on Hull MA direction and market state
			if (isHullRising && _currentMarketState == MarketState.Bullish && Position <= 0)
			{
				// Hull MA rising in bullish market state - Buy signal
				LogInfo($"Buy signal: Hull MA rising ({hullValue} > {_prevHullValue}) in bullish market state");
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (!isHullRising && _currentMarketState == MarketState.Bearish && Position >= 0)
			{
				// Hull MA falling in bearish market state - Sell signal
				LogInfo($"Sell signal: Hull MA falling ({hullValue} < {_prevHullValue}) in bearish market state");
				SellMarket(Volume + Math.Abs(Position));
			}

			// Store Hull MA value for next comparison
			_prevHullValue = hullValue;
			
			// Update last price
			_lastPrice = candle.ClosePrice;
		}

		private void UpdateFeatureData(ICandleMessage candle, decimal rsiValue)
		{
			// Calculate price change percentage
			if (_lastPrice != 0)
			{
				decimal priceChange = (candle.ClosePrice - _lastPrice) / _lastPrice * 100;
				
				// Maintain price change data queue
				_priceChangeData.Enqueue(priceChange);
				if (_priceChangeData.Count > ClusterDataLength)
					_priceChangeData.Dequeue();
			}

			// Maintain RSI data queue
			_rsiData.Enqueue(rsiValue);
			if (_rsiData.Count > ClusterDataLength)
				_rsiData.Dequeue();

			// Calculate volume ratio and maintain queue
			if (_avgVolume == 0)
			{
				_avgVolume = candle.TotalVolume;
			}
			else
			{
				// Exponential smoothing for average volume
				_avgVolume = 0.9m * _avgVolume + 0.1m * candle.TotalVolume;
			}

			decimal volumeRatio = candle.TotalVolume / (_avgVolume == 0 ? 1 : _avgVolume);
			_volumeRatioData.Enqueue(volumeRatio);
			if (_volumeRatioData.Count > ClusterDataLength)
				_volumeRatioData.Dequeue();
		}

		private MarketState DetectMarketState()
		{
			// Simplified implementation of K-Means clustering for market state detection
			// This is a basic approach - a full implementation would use proper K-Means algorithm
			
			// Calculate feature averages to represent cluster centers
			decimal avgPriceChange = _priceChangeData.Average();
			decimal avgRsi = _rsiData.Average();
			decimal avgVolumeRatio = _volumeRatioData.Average();
			
			// Detect market state based on features
			// Higher RSI, positive price change and higher volume -> Bullish
			// Lower RSI, negative price change and higher volume -> Bearish
			// Otherwise -> Neutral
			
			if (avgRsi > 60 && avgPriceChange > 0.1m && avgVolumeRatio > 1.1m)
			{
				return MarketState.Bullish;
			}
			else if (avgRsi < 40 && avgPriceChange < -0.1m && avgVolumeRatio > 1.1m)
			{
				return MarketState.Bearish;
			}
			else
			{
				return MarketState.Neutral;
			}
		}
	}
}
