using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Collections;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy combining Keltner Channels with a Kalman Filter to identify trends and trade opportunities.
	/// </summary>
	public class KeltnerKalmanStrategy : Strategy
	{
		private readonly StrategyParam<int> _emaPeriod;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<decimal> _atrMultiplier;
		private readonly StrategyParam<decimal> _kalmanProcessNoise;
		private readonly StrategyParam<decimal> _kalmanMeasurementNoise;
		private readonly StrategyParam<DataType> _candleType;

		private ExponentialMovingAverage _ema;
		private AverageTrueRange _atr;

		// Kalman filter parameters
		private decimal _kalmanEstimate;
		private decimal _kalmanError;
		private readonly SynchronizedList<decimal> _prices = [];

		// Saved values for decision making
		private decimal _emaValue;
		private decimal _atrValue;
		private decimal _upperBand;
		private decimal _lowerBand;
		private bool _isLongPosition;
		private bool _isShortPosition;

		/// <summary>
		/// EMA period for Keltner Channel.
		/// </summary>
		public int EmaPeriod
		{
			get => _emaPeriod.Value;
			set => _emaPeriod.Value = value;
		}

		/// <summary>
		/// ATR period for Keltner Channel.
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		/// <summary>
		/// ATR multiplier for Keltner Channel.
		/// </summary>
		public decimal AtrMultiplier
		{
			get => _atrMultiplier.Value;
			set => _atrMultiplier.Value = value;
		}

		/// <summary>
		/// Kalman filter process noise parameter (Q).
		/// </summary>
		public decimal KalmanProcessNoise
		{
			get => _kalmanProcessNoise.Value;
			set => _kalmanProcessNoise.Value = value;
		}

		/// <summary>
		/// Kalman filter measurement noise parameter (R).
		/// </summary>
		public decimal KalmanMeasurementNoise
		{
			get => _kalmanMeasurementNoise.Value;
			set => _kalmanMeasurementNoise.Value = value;
		}

		/// <summary>
		/// Candle type to use for the strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="KeltnerKalmanStrategy"/>.
		/// </summary>
		public KeltnerKalmanStrategy()
		{
			_emaPeriod = Param(nameof(EmaPeriod), 20)
				.SetDisplay("EMA Period", "EMA period for Keltner Channel", "Keltner Channel")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetDisplay("ATR Period", "ATR period for Keltner Channel", "Keltner Channel")
				.SetCanOptimize(true)
				.SetOptimize(10, 20, 2);

			_atrMultiplier = Param(nameof(AtrMultiplier), 2.0m)
				.SetDisplay("ATR Multiplier", "ATR multiplier for Keltner Channel", "Keltner Channel")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 3.0m, 0.5m);

			_kalmanProcessNoise = Param(nameof(KalmanProcessNoise), 0.01m)
				.SetDisplay("Kalman Process Noise (Q)", "Kalman filter process noise parameter", "Kalman Filter")
				.SetCanOptimize(true)
				.SetOptimize(0.001m, 0.1m, 0.005m);

			_kalmanMeasurementNoise = Param(nameof(KalmanMeasurementNoise), 0.1m)
				.SetDisplay("Kalman Measurement Noise (R)", "Kalman filter measurement noise parameter", "Kalman Filter")
				.SetCanOptimize(true)
				.SetOptimize(0.01m, 1.0m, 0.05m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
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

			// Initialize Kalman filter
			_kalmanEstimate = 0;
			_kalmanError = 1;
			_prices.Clear();
			_isLongPosition = false;
			_isShortPosition = false;
			_emaValue = 0;
			_atrValue = 0;
			_upperBand = 0;
			_lowerBand = 0;

			// Create indicators
			_ema = new ExponentialMovingAverage
			{
				Length = EmaPeriod
			};

			_atr = new AverageTrueRange
			{
				Length = AtrPeriod
			};

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);

			subscription
				.Bind(_ema, _atr, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _ema);
				DrawOwnTrades(area);
			}

			// Setup position protection
			StartProtection(
				new Unit(2, UnitTypes.Percent),
				new Unit(2, UnitTypes.Percent)
			);
		}

		private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Save indicator values
			_emaValue = emaValue;
			_atrValue = atrValue;

			// Calculate Keltner Channels
			_upperBand = _emaValue + (_atrValue * AtrMultiplier);
			_lowerBand = _emaValue - (_atrValue * AtrMultiplier);

			// Update Kalman filter
			UpdateKalmanFilter(candle.ClosePrice);

			// Store prices for slope calculation
			_prices.Add(candle.ClosePrice);
			if (_prices.Count > 10)
				_prices.RemoveAt(0);

			// Calculate Kalman slope (trend direction)
			decimal kalmanSlope = CalculateKalmanSlope();

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Trading logic
			// Buy when price is above EMA+k*ATR (upper band) and Kalman filter shows uptrend
			if (candle.ClosePrice > _upperBand && _kalmanEstimate > candle.ClosePrice && kalmanSlope > 0 && Position <= 0)
			{
				BuyMarket(Volume);
				LogInfo($"Buy Signal: Price {candle.ClosePrice:F2} > Upper Band {_upperBand:F2}, Kalman Estimate {_kalmanEstimate:F2}, Kalman Slope {kalmanSlope:F6}");
				_isLongPosition = true;
				_isShortPosition = false;
			}
			// Sell when price is below EMA-k*ATR (lower band) and Kalman filter shows downtrend
			else if (candle.ClosePrice < _lowerBand && _kalmanEstimate < candle.ClosePrice && kalmanSlope < 0 && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				LogInfo($"Sell Signal: Price {candle.ClosePrice:F2} < Lower Band {_lowerBand:F2}, Kalman Estimate {_kalmanEstimate:F2}, Kalman Slope {kalmanSlope:F6}");
				_isLongPosition = false;
				_isShortPosition = true;
			}
			// Exit long position when price falls below EMA
			else if (_isLongPosition && candle.ClosePrice < _emaValue)
			{
				SellMarket(Position);
				LogInfo($"Exit Long: Price {candle.ClosePrice:F2} fell below EMA {_emaValue:F2}");
				_isLongPosition = false;
			}
			// Exit short position when price rises above EMA
			else if (_isShortPosition && candle.ClosePrice > _emaValue)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exit Short: Price {candle.ClosePrice:F2} rose above EMA {_emaValue:F2}");
				_isShortPosition = false;
			}
		}

		private void UpdateKalmanFilter(decimal price)
		{
			// Kalman filter implementation (one-dimensional)
			// Prediction step
			decimal predictedEstimate = _kalmanEstimate;
			decimal predictedError = _kalmanError + KalmanProcessNoise;

			// Update step
			decimal kalmanGain = predictedError / (predictedError + KalmanMeasurementNoise);
			_kalmanEstimate = predictedEstimate + kalmanGain * (price - predictedEstimate);
			_kalmanError = (1 - kalmanGain) * predictedError;

			LogInfo($"Kalman Filter: Price {price:F2}, Estimate {_kalmanEstimate:F2}, Error {_kalmanError:F6}, Gain {kalmanGain:F6}");
		}

		private decimal CalculateKalmanSlope()
		{
			// Need at least a few points to calculate a slope
			if (_prices.Count < 3)
				return 0;

			// Simple linear regression slope calculation
			int n = _prices.Count;
			decimal sumX = 0;
			decimal sumY = 0;
			decimal sumXY = 0;
			decimal sumX2 = 0;

			for (int i = 0; i < n; i++)
			{
				decimal x = i;
				decimal y = _prices[i];

				sumX += x;
				sumY += y;
				sumXY += x * y;
				sumX2 += x * x;
			}

			decimal denominator = n * sumX2 - sumX * sumX;

			if (denominator == 0)
				return 0;

			decimal slope = (n * sumXY - sumX * sumY) / denominator;
			return slope;
		}
	}
}