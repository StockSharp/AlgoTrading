using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Strategies
{
	/// <summary>
	/// Strategy that trades on Donchian Channel width breakouts.
	/// When Donchian Channel width increases significantly above its average, 
	/// it enters position in the direction determined by price movement.
	/// </summary>
	public class DonchianWidthBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _donchianPeriod;
		private readonly StrategyParam<int> _avgPeriod;
		private readonly StrategyParam<decimal> _multiplier;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLoss;
		
		private Highest _highest;
		private Lowest _lowest;
		private SimpleMovingAverage _widthAverage;
		
		// Track channel width values
		private decimal _lastWidth;
		private decimal _lastAvgWidth;
		
		/// <summary>
		/// Donchian Channel period.
		/// </summary>
		public int DonchianPeriod
		{
			get => _donchianPeriod.Value;
			set => _donchianPeriod.Value = value;
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
		/// Stop-loss percentage.
		/// </summary>
		public decimal StopLoss
		{
			get => _stopLoss.Value;
			set => _stopLoss.Value = value;
		}
		
		/// <summary>
		/// Initialize <see cref="DonchianWidthBreakoutStrategy"/>.
		/// </summary>
		public DonchianWidthBreakoutStrategy()
		{
			_donchianPeriod = Param(nameof(DonchianPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Donchian Period", "Period for the Donchian Channel", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);
				
			_avgPeriod = Param(nameof(AvgPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Average Period", "Period for width average calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);
			
			_multiplier = Param(nameof(Multiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Multiplier", "Standard deviation multiplier for breakout detection", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);
			
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
			
			_stopLoss = Param(nameof(StopLoss), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss %", "Stop Loss percentage", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 5.0m, 0.5m);
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
			
			// Create indicators for Donchian Channel components
			_highest = new Highest { Length = DonchianPeriod };
			_lowest = new Lowest { Length = DonchianPeriod };
			_widthAverage = new SimpleMovingAverage { Length = AvgPeriod };
			
			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind to candle processing
			subscription
				.Bind(ProcessCandle)
				.Start();
				
			// Enable stop loss protection
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute),
				stopLoss: new Unit(StopLoss, UnitTypes.Percent)
			);
			
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
			
			// Process candle through Highest and Lowest indicators
			var highestValue = _highest.Process(candle).GetValue<decimal>();
			var lowestValue = _lowest.Process(candle).GetValue<decimal>();
			
			// Calculate Donchian Channel width
			var width = highestValue - lowestValue;
			
			// Process width through average
			var widthAvgValue = _widthAverage.Process(width, candle.ServerTime, candle.State == CandleStates.Finished);
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
			if (!_highest.IsFormed || !_lowest.IsFormed || !_widthAverage.IsFormed)
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
			
			// Donchian Channel width breakout detection
			if (width > avgWidth + Multiplier * stdDev)
			{
				// Determine direction based on price and channel
				var middleChannel = (highestValue + lowestValue) / 2;
				var bullish = candle.ClosePrice > middleChannel;
				
				// Cancel active orders before placing new ones
				CancelActiveOrders();
				
				// Trade in the direction determined by price position in the channel
				if (bullish && Position <= 0)
				{
					// Bullish breakout - Buy
					BuyMarket(Volume + Math.Abs(Position));
				}
				else if (!bullish && Position >= 0)
				{
					// Bearish breakout - Sell
					SellMarket(Volume + Math.Abs(Position));
				}
			}
			// Check for exit condition - width returns to average
			else if ((Position > 0 || Position < 0) && width < avgWidth)
			{
				// Exit position when channel width returns to normal
				ClosePosition();
			}
			
			// Update last values
			_lastWidth = width;
			_lastAvgWidth = avgWidth;
		}
	}
}
