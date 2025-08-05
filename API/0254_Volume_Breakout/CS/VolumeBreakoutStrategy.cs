using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that trades on volume breakouts.
	/// When volume rises significantly above its average, it enters position in the direction determined by price.
	/// </summary>
	public class VolumeBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _avgPeriod;
		private readonly StrategyParam<decimal> _multiplier;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLoss;
		
		private SimpleMovingAverage _volumeAverage;
		private SimpleMovingAverage _volumeStdDev;
		private decimal _lastAvgVolume;
		private decimal _lastStdDev;
		
		/// <summary>
		/// Period for volume average calculation.
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
		/// Initialize <see cref="VolumeBreakoutStrategy"/>.
		/// </summary>
		public VolumeBreakoutStrategy()
		{
			_avgPeriod = Param(nameof(AvgPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Average Period", "Period for volume average calculation", "Indicators")
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
			_lastAvgVolume = 0;
			_lastStdDev = 0;
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);
			
			
			// Create indicators for volume analysis
			_volumeAverage = new SimpleMovingAverage { Length = AvgPeriod };
			_volumeStdDev = new SimpleMovingAverage { Length = AvgPeriod };
			
			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind candles to processing method
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
				
			// Calculate volume indicators
			var volume = candle.TotalVolume;
			
			// Calculate volume average
			var avgValue = _volumeAverage.Process(volume, candle.ServerTime, candle.State == CandleStates.Finished);
			var avgVolume = avgValue.ToDecimal();
			
			// Calculate standard deviation approximation
			var deviation = Math.Abs(volume - avgVolume);
			var stdDevValue = _volumeStdDev.Process(deviation, candle.ServerTime, candle.State == CandleStates.Finished);
			var stdDev = stdDevValue.ToDecimal();
			
			// Skip the first N candles until we have enough data
			if (!_volumeAverage.IsFormed || !_volumeStdDev.IsFormed)
			{
				_lastAvgVolume = avgVolume;
				_lastStdDev = stdDev;
				return;
			}
			
			// Check if trading is allowed
			if (!IsFormedAndOnlineAndAllowTrading())
			{
				_lastAvgVolume = avgVolume;
				_lastStdDev = stdDev;
				return;
			}
			
			// Volume breakout detection (volume increases significantly above its average)
			if (volume > avgVolume + Multiplier * stdDev)
			{
				// Determine direction based on price movement
				var bullish = candle.ClosePrice > candle.OpenPrice;
				
				// Cancel active orders before placing new ones
				CancelActiveOrders();
				
				// Trade in the direction of price movement
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
			// Check for exit condition - volume returns to average
			else if ((Position > 0 && volume < avgVolume) || 
					 (Position < 0 && volume < avgVolume))
			{
				// Exit position
				ClosePosition();
			}
			
			// Update last values
			_lastAvgVolume = avgVolume;
			_lastStdDev = stdDev;
		}
	}
}
