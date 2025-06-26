using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Strategies
{
	/// <summary>
	/// Strategy that trades on Bollinger Band width breakouts.
	/// When Bollinger Band width increases significantly above its average, 
	/// it enters position in the direction determined by price movement.
	/// </summary>
	public class BollingerWidthBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _bollingerLength;
		private readonly StrategyParam<decimal> _bollingerDeviation;
		private readonly StrategyParam<int> _avgPeriod;
		private readonly StrategyParam<decimal> _multiplier;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _stopMultiplier;
		
		private BollingerBands _bollinger;
		private SimpleMovingAverage _widthAverage;
		private AverageTrueRange _atr;
		
		// Track band width values
		private decimal _lastWidth;
		private decimal _lastAvgWidth;
		
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
		/// Period for width average calculation.
		/// </summary>
		public int AvgPeriod
		{
			get => _avgPeriod.Value;
			set => _avgPeriod.Value = value;
		}
		
		/// <summary>
		/// Standard deviation multiplier for breakout detection.
		/// </summary>
		public decimal Multiplier
		{
			get => _multiplier.Value;
			set => _multiplier.Value = value;
		}
		
		/// <summary>
		/// Candle type for strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}
		
		/// <summary>
		/// Stop-loss ATR multiplier.
		/// </summary>
		public int StopMultiplier
		{
			get => _stopMultiplier.Value;
			set => _stopMultiplier.Value = value;
		}
		
		/// <summary>
		/// Initialize <see cref="BollingerWidthBreakoutStrategy"/>.
		/// </summary>
		public BollingerWidthBreakoutStrategy()
		{
			_bollingerLength = Param(nameof(BollingerLength), 20)
				.SetGreaterThanZero()
				.SetDisplay("Bollinger Length", "Period of the Bollinger Bands indicator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);
				
			_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);
			
			_avgPeriod = Param(nameof(AvgPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Average Period", "Period for Bollinger width average calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);
			
			_multiplier = Param(nameof(Multiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Multiplier", "Standard deviation multiplier for breakout detection", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);
			
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
			
			_stopMultiplier = Param(nameof(StopMultiplier), 2)
				.SetGreaterThanZero()
				.SetDisplay("Stop Multiplier", "ATR multiplier for stop-loss", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1, 5, 1);
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
			
			_lastWidth = 0;
			_lastAvgWidth = 0;
			
			// Create indicators
			_bollinger = new BollingerBands
			{
				Length = BollingerLength,
				Width = BollingerDeviation
			};
			
			_widthAverage = new SimpleMovingAverage { Length = AvgPeriod };
			_atr = new AverageTrueRange { Length = BollingerLength };
			
			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind Bollinger Bands
			subscription
				.Bind(_bollinger, ProcessBollinger)
				.Start();
			
			// Create chart area for visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _bollinger);
				DrawOwnTrades(area);
			}
		}
		
		private void ProcessBollinger(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand)
		{
			if (candle.State != CandleStates.Finished)
				return;
			
			// Process candle through ATR
			var atrValue = _atr.Process(candle);
			var currentAtr = atrValue.ToDecimal();
			
			// Calculate Bollinger Band width
			var width = upperBand - lowerBand;
			
			// Process width through average
			var widthAvgValue = _widthAverage.Process(width, candle.ServerTime, candle.State == CandleStates.Finished);
			var avgWidth = widthAvgValue.ToDecimal();
			
			// For first values, just save and skip
			if (_lastWidth == 0)
			{
				_lastWidth = width;
				_lastAvgWidth = avgWidth;
				return;
			}
			
			// Calculate width standard deviation (simplified approach)
			var stdDev = Math.Abs(width - avgWidth) * 1.5m; // Simplified approximation
			
			// Skip if indicators are not formed yet
			if (!_bollinger.IsFormed || !_widthAverage.IsFormed || !_atr.IsFormed)
			{
				_lastWidth = width;
				_lastAvgWidth = avgWidth;
				return;
			}
			
			// Check if trading is allowed
			if (!IsFormedAndOnlineAndAllowTrading())
			{
				_lastWidth = width;
				_lastAvgWidth = avgWidth;
				return;
			}
			
			// Bollinger width breakout detection
			if (width > avgWidth + Multiplier * stdDev)
			{
				// Determine direction based on price and bands
				var priceDirection = false;
				
				// If price is closer to upper band, go long. If closer to lower band, go short.
				var upperDistance = Math.Abs(candle.ClosePrice - upperBand);
				var lowerDistance = Math.Abs(candle.ClosePrice - lowerBand);
				
				if (upperDistance < lowerDistance)
				{
					// Price is closer to upper band, likely bullish
					priceDirection = true;
				}
				
				// Cancel active orders before placing new ones
				CancelActiveOrders();
				
				// Calculate stop-loss based on current ATR
				var stopOffset = StopMultiplier * currentAtr;
				
				// Trade in the determined direction
				if (priceDirection && Position <= 0)
				{
					// Bullish direction - Buy
					var buyPrice = Security.GetCurrentPrice(Sides.Buy);
					BuyMarket(Volume + Math.Abs(Position));
					
					// Set stop-loss order
					var stopLoss = buyPrice - stopOffset;
					RegisterOrder(this.CreateOrder(Sides.Sell, stopLoss, Math.Abs(Position)));
				}
				else if (!priceDirection && Position >= 0)
				{
					// Bearish direction - Sell
					var sellPrice = Security.GetCurrentPrice(Sides.Sell);
					SellMarket(Volume + Math.Abs(Position));
					
					// Set stop-loss order
					var stopLoss = sellPrice + stopOffset;
					RegisterOrder(this.CreateOrder(Sides.Buy, stopLoss, Math.Abs(Position)));
				}
			}
			// Check for exit condition - width returns to average
			else if ((Position > 0 || Position < 0) && width < avgWidth)
			{
				// Exit position
				ClosePosition();
			}
			
			// Update last values
			_lastWidth = width;
			_lastAvgWidth = avgWidth;
		}
	}
}
