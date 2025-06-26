using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Strategies
{
	/// <summary>
	/// Strategy that trades on ATR volatility breakouts.
	/// When ATR rises significantly above its average, it enters position in the direction determined by price.
	/// </summary>
	public class VolatilityBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<int> _avgPeriod;
		private readonly StrategyParam<decimal> _multiplier;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _stopMultiplier;
		
		private AverageTrueRange _atr;
		private SimpleMovingAverage _atrAverage;
		private decimal _prevAtrValue;
		private decimal _prevAtrAvgValue;
		
		/// <summary>
		/// ATR period.
		/// </summary>
		public int ATRPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}
		
		/// <summary>
		/// Period for ATR average calculation.
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
		/// Initialize <see cref="VolatilityBreakoutStrategy"/>.
		/// </summary>
		public VolatilityBreakoutStrategy()
		{
			_atrPeriod = Param(nameof(ATRPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "Period for ATR indicator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 2);
			
			_avgPeriod = Param(nameof(AvgPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Average Period", "Period for ATR average calculation", "Indicators")
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
			
			_prevAtrValue = 0;
			_prevAtrAvgValue = 0;
			
			// Create indicators
			_atr = new AverageTrueRange { Length = ATRPeriod };
			_atrAverage = new SimpleMovingAverage { Length = AvgPeriod };
			
			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			// First bind ATR to the candle subscription
			subscription
				.BindEx(_atr, ProcessAtr)
				.Start();
				
			// Create chart area for visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _atr);
				DrawOwnTrades(area);
			}
		}
		
		private void ProcessAtr(ICandleMessage candle, IIndicatorValue atrValue)
		{
			if (candle.State != CandleStates.Finished)
				return;
			
			if (!atrValue.IsFinal)
				return;
			
			// Get current ATR value
			var currentAtr = atrValue.GetValue<decimal>();
			
			// Process ATR through average indicator
			var atrAvgValue = _atrAverage.Process(currentAtr, candle.ServerTime, candle.State == CandleStates.Finished);
			var currentAtrAvg = atrAvgValue.GetValue<decimal>();
			
			// For first values, just save and skip
			if (_prevAtrValue == 0)
			{
				_prevAtrValue = currentAtr;
				_prevAtrAvgValue = currentAtrAvg;
				return;
			}
			
			// Calculate standard deviation of ATR (simplified approach)
			var stdDev = Math.Abs(currentAtr - currentAtrAvg) * 1.5m; // Simplified approximation
			
			// Check if trading is allowed
			if (!IsFormedAndOnlineAndAllowTrading())
			{
				_prevAtrValue = currentAtr;
				_prevAtrAvgValue = currentAtrAvg;
				return;
			}
			
			// ATR breakout detection (ATR increases significantly above its average)
			if (currentAtr > currentAtrAvg + Multiplier * stdDev)
			{
				// Determine direction based on price movement
				var priceDirection = candle.ClosePrice > candle.OpenPrice;
				
				// Cancel active orders before placing new ones
				CancelActiveOrders();
				
				// Calculate stop-loss based on current ATR
				var stopOffset = StopMultiplier * currentAtr;
				
				// Trade in the direction of price movement
				if (priceDirection && Position <= 0)
				{
					// Bullish breakout - Buy
					var buyPrice = Security.GetCurrentPrice(Sides.Buy);
					BuyMarket(Volume + Math.Abs(Position));
					
					// Set stop-loss order
					var stopLoss = buyPrice - stopOffset;
					RegisterOrder(this.CreateOrder(Sides.Sell, stopLoss, Math.Abs(Position)));
				}
				else if (!priceDirection && Position >= 0)
				{
					// Bearish breakout - Sell
					var sellPrice = Security.GetCurrentPrice(Sides.Sell);
					SellMarket(Volume + Math.Abs(Position));
					
					// Set stop-loss order
					var stopLoss = sellPrice + stopOffset;
					RegisterOrder(this.CreateOrder(Sides.Buy, stopLoss, Math.Abs(Position)));
				}
			}
			// Check for exit condition - ATR returns to average
			else if ((Position > 0 && currentAtr < currentAtrAvg) || 
					 (Position < 0 && currentAtr < currentAtrAvg))
			{
				// Exit position
				ClosePosition();
			}
			
			// Update previous values
			_prevAtrValue = currentAtr;
			_prevAtrAvgValue = currentAtrAvg;
		}
	}
}
