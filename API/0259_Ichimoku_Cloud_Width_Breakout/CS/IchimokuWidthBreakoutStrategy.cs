using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that trades on Ichimoku Cloud width breakouts.
	/// When Ichimoku Cloud width increases significantly above its average, 
	/// it enters position in the direction determined by price location relative to the cloud.
	/// </summary>
	public class IchimokuWidthBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _tenkanPeriod;
		private readonly StrategyParam<int> _kijunPeriod;
		private readonly StrategyParam<int> _senkouSpanBPeriod;
		private readonly StrategyParam<int> _avgPeriod;
		private readonly StrategyParam<decimal> _multiplier;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLoss;
		
		private Ichimoku _ichimoku;
		private SimpleMovingAverage _widthAverage;
		
		// Track cloud width values
		private decimal _lastWidth;
		private decimal _lastAvgWidth;
	
		/// <summary>
		/// Tenkan-sen period for Ichimoku.
		/// </summary>
		public int TenkanPeriod
		{
			get => _tenkanPeriod.Value;
			set => _tenkanPeriod.Value = value;
		}
		
		/// <summary>
		/// Kijun-sen period for Ichimoku.
		/// </summary>
		public int KijunPeriod
		{
			get => _kijunPeriod.Value;
			set => _kijunPeriod.Value = value;
		}
		
		/// <summary>
		/// Senkou Span B period for Ichimoku.
		/// </summary>
		public int SenkouSpanBPeriod
		{
			get => _senkouSpanBPeriod.Value;
			set => _senkouSpanBPeriod.Value = value;
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
		/// Initialize <see cref="IchimokuWidthBreakoutStrategy"/>.
		/// </summary>
		public IchimokuWidthBreakoutStrategy()
		{
			_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
				.SetGreaterThanZero()
				.SetDisplay("Tenkan Period", "Period for Tenkan-sen line", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(5, 20, 1);
				
			_kijunPeriod = Param(nameof(KijunPeriod), 26)
				.SetGreaterThanZero()
				.SetDisplay("Kijun Period", "Period for Kijun-sen line", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(20, 40, 2);
				
			_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
				.SetGreaterThanZero()
				.SetDisplay("Senkou Span B Period", "Period for Senkou Span B line", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(40, 80, 4);
				
			_avgPeriod = Param(nameof(AvgPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Average Period", "Period for cloud width average calculation", "Indicators")
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
		protected override void OnReseted()
		{
			base.OnReseted();
			_lastWidth = 0;
			_lastAvgWidth = 0;
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);
			

			// Create indicators
			_ichimoku = new Ichimoku
			{
				Tenkan = { Length = TenkanPeriod },
				Kijun = { Length = KijunPeriod },
				SenkouB = { Length = SenkouSpanBPeriod }
			};
			
			_widthAverage = new SimpleMovingAverage { Length = AvgPeriod };
			
			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind Ichimoku to the candle subscription
			subscription
				.BindEx(_ichimoku, ProcessIchimoku)
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
				DrawIndicator(area, _ichimoku);
				DrawOwnTrades(area);
			}
		}
		
		private void ProcessIchimoku(ICandleMessage candle, IIndicatorValue ichimokuValue)
		{
			if (candle.State != CandleStates.Finished)
				return;
			
			if (!ichimokuValue.IsFinal)
				return;

			// Get current Ichimoku values
			// The structure of values depends on the implementation, this is just an example
			var ichimokuTyped = (IchimokuValue)ichimokuValue;
			
			if (ichimokuTyped.Tenkan is not decimal tenkan)
				return;

			if (ichimokuTyped.Kijun is not decimal kijun)
				return;

			if (ichimokuTyped.SenkouA is not decimal senkouSpanA)
				return;

			if (ichimokuTyped.SenkouB is not decimal senkouSpanB)
				return;

			// Calculate Cloud width (absolute difference between Senkou lines)
			var width = Math.Abs(senkouSpanA - senkouSpanB);
			
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
			if (!_ichimoku.IsFormed || !_widthAverage.IsFormed)
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
			
			// Cloud width breakout detection
			if (width > avgWidth + Multiplier * stdDev)
			{
				// Determine trade direction based on price relative to cloud
				var upperCloud = Math.Max(senkouSpanA, senkouSpanB);
				var lowerCloud = Math.Min(senkouSpanA, senkouSpanB);
				
				var bullish = candle.ClosePrice > upperCloud;
				var bearish = candle.ClosePrice < lowerCloud;
				
				// Cancel active orders before placing new ones
				CancelActiveOrders();
				
				// Only trade if price is clearly outside the cloud
				if (bullish && Position <= 0)
				{
					// Bullish signal - Buy
					BuyMarket(Volume + Math.Abs(Position));
				}
				else if (bearish && Position >= 0)
				{
					// Bearish signal - Sell
					SellMarket(Volume + Math.Abs(Position));
				}
			}
			// Check for exit condition - width returns to average or price enters cloud
			else if ((Position > 0 || Position < 0) && 
					(width < avgWidth || 
					 (candle.ClosePrice > Math.Min(senkouSpanA, senkouSpanB) && 
					  candle.ClosePrice < Math.Max(senkouSpanA, senkouSpanB))))
			{
				// Exit position when cloud width returns to normal or price enters cloud
				ClosePosition();
			}
			
			// Update last values
			_lastWidth = width;
			_lastAvgWidth = avgWidth;
		}
	}
}
