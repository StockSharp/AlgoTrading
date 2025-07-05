using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Strategies
{
	/// <summary>
	/// Strategy that trades on ADX breakouts.
	/// When ADX breaks out above its average, it enters position in the direction determined by price.
	/// </summary>
	public class ADXBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _adxPeriod;
		private readonly StrategyParam<int> _avgPeriod;
		private readonly StrategyParam<decimal> _multiplier;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLoss;
		
		private AverageDirectionalIndex _adx;
		private SimpleMovingAverage _adxAverage;
		private decimal _prevAdxValue;
		private decimal _prevAdxAvgValue;
		
		/// <summary>
		/// ADX period.
		/// </summary>
		public int ADXPeriod
		{
			get => _adxPeriod.Value;
			set => _adxPeriod.Value = value;
		}
		
		/// <summary>
		/// Period for ADX average calculation.
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
		/// Initialize <see cref="ADXBreakoutStrategy"/>.
		/// </summary>
		public ADXBreakoutStrategy()
		{
			_adxPeriod = Param(nameof(ADXPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ADX Period", "Period for ADX indicator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 2);
			
			_avgPeriod = Param(nameof(AvgPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Average Period", "Period for ADX average calculation", "Indicators")
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
			
			_prevAdxValue = 0;
			_prevAdxAvgValue = 0;
			
			// Create indicators
			_adx = new AverageDirectionalIndex { Length = ADXPeriod };
			_adxAverage = new SimpleMovingAverage { Length = AvgPeriod };
			
			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			// First bind ADX to the candle subscription
			subscription
				.BindEx(_adx, ProcessAdx)
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
				DrawIndicator(area, _adx);
				DrawOwnTrades(area);
			}
		}
		
		private void ProcessAdx(ICandleMessage candle, IIndicatorValue adxValue)
		{
			if (candle.State != CandleStates.Finished)
				return;
			
			if (!adxValue.IsFinal)
				return;
			
			// Get current ADX value
			if (((AverageDirectionalIndexValue)adxValue).MovingAverage is not decimal currentAdx)
			{
				return;
			}

			// Process ADX through average indicator
			var adxAvgValue = _adxAverage.Process(currentAdx, candle.ServerTime, candle.State == CandleStates.Finished);
			var currentAdxAvg = adxAvgValue.ToDecimal();
			
			// For first values, just save and skip
			if (_prevAdxValue == 0)
			{
				_prevAdxValue = currentAdx;
				_prevAdxAvgValue = currentAdxAvg;
				return;
			}
			
			// Calculate standard deviation of ADX (simplified approach)
			var stdDev = Math.Abs(currentAdx - currentAdxAvg) * 2; // Simplified approximation
			
			// Check if trading is allowed
			if (!IsFormedAndOnlineAndAllowTrading())
			{
				_prevAdxValue = currentAdx;
				_prevAdxAvgValue = currentAdxAvg;
				return;
			}
			
			// ADX breakout detection (ADX increases significantly above its average)
			if (currentAdx > currentAdxAvg + Multiplier * stdDev)
			{
				// Determine direction based on price movement
				var priceDirection = candle.ClosePrice > candle.OpenPrice;
				
				// Cancel active orders before placing new ones
				CancelActiveOrders();
				
				// Trade in the direction of price movement
				if (priceDirection && Position <= 0)
				{
					// Bullish breakout - Buy
					BuyMarket(Volume + Math.Abs(Position));
				}
				else if (!priceDirection && Position >= 0)
				{
					// Bearish breakout - Sell
					SellMarket(Volume + Math.Abs(Position));
				}
			}
			// Check for exit condition - ADX returns to average
			else if ((Position > 0 && currentAdx < currentAdxAvg) || 
					 (Position < 0 && currentAdx < currentAdxAvg))
			{
				// Exit position
				ClosePosition();
			}
			
			// Update previous values
			_prevAdxValue = currentAdx;
			_prevAdxAvgValue = currentAdxAvg;
		}
	}
}
