using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Strategies
{
	/// <summary>
	/// Strategy that trades on Keltner Channel width breakouts.
	/// When Keltner Channel width increases significantly above its average, 
	/// it enters position in the direction determined by price movement.
	/// </summary>
	public class KeltnerWidthBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _emaPeriod;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<decimal> _atrMultiplier;
		private readonly StrategyParam<int> _avgPeriod;
		private readonly StrategyParam<decimal> _multiplier;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _stopMultiplier;
		
		private ExponentialMovingAverage _ema;
		private AverageTrueRange _atr;
		private SimpleMovingAverage _widthAverage;
		
		// Track channel width values
		private decimal _lastWidth;
		private decimal _lastAvgWidth;
		
		// Track EMA and ATR values to calculate channel
		private decimal _currentEma;
		private decimal _currentAtr;
		
		/// <summary>
		/// EMA period for Keltner Channel.
		/// </summary>
		public int EMAPeriod
		{
			get => _emaPeriod.Value;
			set => _emaPeriod.Value = value;
		}
		
		/// <summary>
		/// ATR period for Keltner Channel.
		/// </summary>
		public int ATRPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}
		
		/// <summary>
		/// ATR multiplier for Keltner Channel.
		/// </summary>
		public decimal ATRMultiplier
		{
			get => _atrMultiplier.Value;
			set => _atrMultiplier.Value = value;
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
		/// Initialize <see cref="KeltnerWidthBreakoutStrategy"/>.
		/// </summary>
		public KeltnerWidthBreakoutStrategy()
		{
			_emaPeriod = Param(nameof(EMAPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("EMA Period", "Period of EMA for Keltner Channel", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);
				
			_atrPeriod = Param(nameof(ATRPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "Period of ATR for Keltner Channel", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 2);
			
			_atrMultiplier = Param(nameof(ATRMultiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("ATR Multiplier", "Multiplier for ATR in Keltner Channel", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);
			
			_avgPeriod = Param(nameof(AvgPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Average Period", "Period for Keltner width average calculation", "Indicators")
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
			return new[] { (Security, CandleType) };
		}
		
		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);
			
			_lastWidth = 0;
			_lastAvgWidth = 0;
			_currentEma = 0;
			_currentAtr = 0;
			
			// Create indicators
			_ema = new ExponentialMovingAverage { Length = EMAPeriod };
			_atr = new AverageTrueRange { Length = ATRPeriod };
			_widthAverage = new SimpleMovingAverage { Length = AvgPeriod };
			
			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind to candle processing
			subscription
				.Bind(ProcessCandle)
				.Start();
			
			// Create chart area for visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawOwnTrades(area);
			}
		}
		
		private void ProcessCandle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;
			
			// Process candle through EMA and ATR
			var emaValue = _ema.Process(candle);
			var atrValue = _atr.Process(candle);
			
			_currentEma = emaValue.GetValue<decimal>();
			_currentAtr = atrValue.GetValue<decimal>();
			
			// Calculate Keltner Channel boundaries
			var upperBand = _currentEma + ATRMultiplier * _currentAtr;
			var lowerBand = _currentEma - ATRMultiplier * _currentAtr;
			
			// Calculate Channel width
			var width = upperBand - lowerBand;
			
			// Process width through average
			var widthAvgValue = _widthAverage.Process(new DecimalIndicatorValue(width));
			var avgWidth = widthAvgValue.GetValue<decimal>();
			
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
			if (!_ema.IsFormed || !_atr.IsFormed || !_widthAverage.IsFormed)
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
			
			// Keltner width breakout detection
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
				var stopOffset = StopMultiplier * _currentAtr;
				
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
