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
				.SetRange(0.0001m, 1)
				.SetDisplay("Process Noise", "Process noise coefficient for Kalman filter", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(0.001m, 0.1m, 0.005m);

			_measurementNoiseParam = Param(nameof(MeasurementNoise), 0.1m)
				.SetRange(0.0001m, 1)
				.SetDisplay("Measurement Noise", "Measurement noise coefficient for Kalman filter", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(0.01m, 1.0m, 0.1m);

			_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Candle type for strategy", "Common");
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
	}
}