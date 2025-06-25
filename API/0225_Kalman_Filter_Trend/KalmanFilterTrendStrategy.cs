using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Kalman Filter Trend strategy.
	/// Uses a custom Kalman Filter indicator to track price trend.
	/// </summary>
	public class KalmanFilterTrendStrategy : Strategy
	{
		private readonly StrategyParam<decimal> _processNoiseParam;
		private readonly StrategyParam<decimal> _measurementNoiseParam;
		private readonly StrategyParam<DataType> _candleTypeParam;

		private KalmanFilter _kalmanFilter;
		private AverageTrueRange _atr;

		/// <summary>
		/// Process noise coefficient for Kalman filter.
		/// </summary>
		public decimal ProcessNoise
		{
			get => _processNoiseParam.Value;
			set => _processNoiseParam.Value = value;
		}

		/// <summary>
		/// Measurement noise coefficient for Kalman filter.
		/// </summary>
		public decimal MeasurementNoise
		{
			get => _measurementNoiseParam.Value;
			set => _measurementNoiseParam.Value = value;
		}

		/// <summary>
		/// Candle type for strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleTypeParam.Value;
			set => _candleTypeParam.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public KalmanFilterTrendStrategy()
		{
			_processNoiseParam = Param(nameof(ProcessNoise), 0.01m)
				.SetGreaterThan(0.0001m)
				.SetDisplay("Process Noise", "Process noise coefficient for Kalman filter", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(0.001m, 0.1m, 0.005m);

			_measurementNoiseParam = Param(nameof(MeasurementNoise), 0.1m)
				.SetGreaterThan(0.0001m)
				.SetDisplay("Measurement Noise", "Measurement noise coefficient for Kalman filter", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(0.01m, 1.0m, 0.1m);

			_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Candle type for strategy", "Common");
		}

		/// <summary>
		/// Returns working securities.
		/// </summary>
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return [(Security, CandleType)];
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create indicators
			_kalmanFilter = new KalmanFilter 
			{ 
				ProcessNoise = ProcessNoise,
				MeasurementNoise = MeasurementNoise 
			};
			
			_atr = new AverageTrueRange { Length = 14 };

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(_kalmanFilter, _atr, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _kalmanFilter);
				DrawOwnTrades(area);
			}
			
			// Enable position protection
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit
				stopLoss: new Unit(2, UnitTypes.Absolute) // Stop loss at 2*ATR
			);
		}

		private void ProcessCandle(ICandleMessage candle, decimal kalmanValue, decimal atrValue)
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Calculate trend direction
			var trend = candle.ClosePrice > kalmanValue ? 1 : -1;
			
			// Trading logic based on price position relative to Kalman filter
			if (trend > 0 && Position <= 0)
			{
				// Buy when price is above Kalman filter (uptrend)
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (trend < 0 && Position >= 0)
			{
				// Sell when price is below Kalman filter (downtrend)
				SellMarket(Volume + Math.Abs(Position));
			}
		}
		
		/// <summary>
		/// Custom Kalman Filter indicator implementation.
		/// </summary>
		private class KalmanFilter : BaseIndicator
		{
			private decimal _estimate;
			private decimal _errorCovariance;
			private bool _isInitialized;
			
			/// <summary>
			/// Process noise coefficient.
			/// </summary>
			public decimal ProcessNoise { get; set; } = 0.01m;
			
			/// <summary>
			/// Measurement noise coefficient.
			/// </summary>
			public decimal MeasurementNoise { get; set; } = 0.1m;
			
			/// <summary>
			/// Create a new instance of KalmanFilter.
			/// </summary>
			public KalmanFilter()
			{
				_estimate = 0;
				_errorCovariance = 1;
				_isInitialized = false;
			}
			
			/// <summary>
			/// Process input value.
			/// </summary>
			protected override IIndicatorValue OnProcess(IIndicatorValue input)
			{
				var candle = input.GetValue<ICandleMessage>();
				
				if (!_isInitialized)
				{
					_estimate = candle.ClosePrice;
					_isInitialized = true;
					return new DecimalIndicatorValue(this, _estimate);
				}
				
				// Prediction phase
				var prioriEstimate = _estimate;
				var prioriErrorCovariance = _errorCovariance + ProcessNoise;
				
				// Update phase
				var kalmanGain = prioriErrorCovariance / (prioriErrorCovariance + MeasurementNoise);
				_estimate = prioriEstimate + kalmanGain * (candle.ClosePrice - prioriEstimate);
				_errorCovariance = (1 - kalmanGain) * prioriErrorCovariance;
				
				return new DecimalIndicatorValue(this, _estimate);
			}
			
			/// <summary>
			/// The indicator is always formed.
			/// </summary>
			public override bool IsFormed => _isInitialized;
			
			/// <summary>
			/// Reset the indicator to initial state.
			/// </summary>
			public override void Reset()
			{
				base.Reset();
				_estimate = 0;
				_errorCovariance = 1;
				_isInitialized = false;
			}
			
			/// <summary>
			/// Name of the indicator.
			/// </summary>
			public override string Name => "Kalman Filter";
		}
	}
}