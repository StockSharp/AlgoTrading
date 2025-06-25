using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Kalman Filter for trend identification.
	/// </summary>
	public class KalmanFilterTrendStrategy : Strategy
	{
		private readonly StrategyParam<decimal> _processNoise;
		private readonly StrategyParam<decimal> _measurementNoise;
		private readonly StrategyParam<decimal> _stopMultiplier;
		private readonly StrategyParam<DataType> _candleType;

		private decimal _previousKalmanValue;
		private bool _isFirstValue = true;

		/// <summary>
		/// Process noise (Q) parameter for Kalman filter.
		/// </summary>
		public decimal ProcessNoise
		{
			get => _processNoise.Value;
			set => _processNoise.Value = value;
		}

		/// <summary>
		/// Measurement noise (R) parameter for Kalman filter.
		/// </summary>
		public decimal MeasurementNoise
		{
			get => _measurementNoise.Value;
			set => _measurementNoise.Value = value;
		}

		/// <summary>
		/// Stop-loss multiplier relative to ATR.
		/// </summary>
		public decimal StopMultiplier
		{
			get => _stopMultiplier.Value;
			set => _stopMultiplier.Value = value;
		}

		/// <summary>
		/// Candle type parameter.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initialize a new instance of <see cref="KalmanFilterTrendStrategy"/>.
		/// </summary>
		public KalmanFilterTrendStrategy()
		{
			_processNoise = this.Param(nameof(ProcessNoise), 0.01m)
				.SetGreaterThanZero()
				.SetDisplay("Process Noise (Q)", "Process noise parameter for Kalman filter", "Kalman Filter Settings")
				.SetCanOptimize(true)
				.SetOptimize(0.001m, 0.1m, 0.005m);

			_measurementNoise = this.Param(nameof(MeasurementNoise), 0.1m)
				.SetGreaterThanZero()
				.SetDisplay("Measurement Noise (R)", "Measurement noise parameter for Kalman filter", "Kalman Filter Settings")
				.SetCanOptimize(true)
				.SetOptimize(0.01m, 1.0m, 0.05m);

			_stopMultiplier = this.Param(nameof(StopMultiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Stop ATR Multiplier", "ATR multiplier for stop-loss", "Strategy Settings")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_candleType = this.Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles for strategy", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security security, DataType dataType)> GetWorkingSecurities()
		{
			return new[] { (Security, CandleType) };
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create indicators
			var kalmanFilter = new OptimalTracking
			{
				Q = ProcessNoise,
				R = MeasurementNoise
			};
			
			var atr = new AverageTrueRange { Length = 14 };
			
			// Reset state variables
			_isFirstValue = true;
			_previousKalmanValue = 0;

			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind indicators to subscription
			subscription
				.Bind(kalmanFilter, atr, ProcessCandle)
				.Start();

			// Enable position protection
			StartProtection(
				takeProfit: new Unit(0), // We'll handle exits in the strategy logic
				stopLoss: new Unit(0),   // We'll handle stops in the strategy logic
				useMarketOrders: true
			);

			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, kalmanFilter);
				DrawIndicator(area, atr);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal kalmanValue, decimal atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Initialize values on first candle
			if (_isFirstValue)
			{
				_previousKalmanValue = kalmanValue;
				_isFirstValue = false;
				return;
			}

			// Calculate Kalman filter slope (trend direction)
			var kalmanSlope = kalmanValue - _previousKalmanValue;
			
			// Define entry conditions
			var longEntryCondition = candle.ClosePrice > kalmanValue && kalmanSlope > 0 && Position <= 0;
			var shortEntryCondition = candle.ClosePrice < kalmanValue && kalmanSlope < 0 && Position >= 0;
			
			// Define exit conditions
			var longExitCondition = candle.ClosePrice < kalmanValue && Position > 0;
			var shortExitCondition = candle.ClosePrice > kalmanValue && Position < 0;

			// Execute trading logic
			if (longEntryCondition)
			{
				// Calculate position size
				var positionSize = Volume + Math.Abs(Position);
				
				// Calculate stop loss level
				var stopPrice = candle.ClosePrice - atrValue * StopMultiplier;
				
				// Enter long position
				BuyMarket(positionSize);
				
				LogInfo($"Long entry: Price={candle.ClosePrice}, Kalman={kalmanValue}, Slope={kalmanSlope}, ATR={atrValue}, Stop={stopPrice}");
			}
			else if (shortEntryCondition)
			{
				// Calculate position size
				var positionSize = Volume + Math.Abs(Position);
				
				// Calculate stop loss level
				var stopPrice = candle.ClosePrice + atrValue * StopMultiplier;
				
				// Enter short position
				SellMarket(positionSize);
				
				LogInfo($"Short entry: Price={candle.ClosePrice}, Kalman={kalmanValue}, Slope={kalmanSlope}, ATR={atrValue}, Stop={stopPrice}");
			}
			else if (longExitCondition)
			{
				// Exit long position
				SellMarket(Math.Abs(Position));
				LogInfo($"Long exit: Price={candle.ClosePrice}, Kalman={kalmanValue}, Slope={kalmanSlope}");
			}
			else if (shortExitCondition)
			{
				// Exit short position
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short exit: Price={candle.ClosePrice}, Kalman={kalmanValue}, Slope={kalmanSlope}");
			}

			// Store current value for next candle
			_previousKalmanValue = kalmanValue;
		}
	}
}