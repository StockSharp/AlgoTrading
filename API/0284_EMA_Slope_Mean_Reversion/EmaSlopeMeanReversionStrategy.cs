using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// EMA Slope Mean Reversion Strategy - strategy based on mean reversion of exponential moving average slope.
	/// </summary>
	public class EmaSlopeMeanReversionStrategy : Strategy
	{
		private readonly StrategyParam<int> _emaPeriod;
		private readonly StrategyParam<int> _slopeLookback;
		private readonly StrategyParam<decimal> _thresholdMultiplier;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		private decimal _previousEmaValue;
		private decimal _currentSlope;
		private decimal _averageSlope;
		private decimal _slopeStdDev;
		private int _slopeCount;
		private decimal _sumSlopes;
		private decimal _sumSquaredDiff;

		/// <summary>
		/// EMA period.
		/// </summary>
		public int EmaPeriod
		{
			get => _emaPeriod.Value;
			set => _emaPeriod.Value = value;
		}

		/// <summary>
		/// Period for calculating slope statistics.
		/// </summary>
		public int SlopeLookback
		{
			get => _slopeLookback.Value;
			set => _slopeLookback.Value = value;
		}
		
		/// <summary>
		/// Threshold multiplier for standard deviation.
		/// </summary>
		public decimal ThresholdMultiplier
		{
			get => _thresholdMultiplier.Value;
			set => _thresholdMultiplier.Value = value;
		}

		/// <summary>
		/// Stop-loss percentage.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Candle type.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initialize <see cref="EmaSlopeMeanReversionStrategy"/>.
		/// </summary>
		public EmaSlopeMeanReversionStrategy()
		{
			_emaPeriod = Param(nameof(EmaPeriod), 20)
				.SetDisplay("EMA Period", "Exponential Moving Average period", "EMA Settings")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_slopeLookback = Param(nameof(SlopeLookback), 20)
				.SetDisplay("Slope Lookback", "Period for slope statistics", "Slope Settings")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_thresholdMultiplier = Param(nameof(ThresholdMultiplier), 2m)
				.SetDisplay("Threshold Multiplier", "Standard deviation multiplier for entry threshold", "Slope Settings")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

			// Reset variables
			_previousEmaValue = 0;
			_currentSlope = 0;
			_averageSlope = 0;
			_slopeStdDev = 0;
			_slopeCount = 0;
			_sumSlopes = 0;
			_sumSquaredDiff = 0;

			// Create EMA indicator
			var ema = new ExponentialMovingAverage { Length = EmaPeriod };

			// Subscribe to candles and bind indicator
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.Bind(ema, ProcessCandle)
				.Start();

			// Start position protection
			StartProtection(
				new Unit(0, UnitTypes.Absolute), // No take profit (use exit rule instead)
				new Unit(StopLossPercent, UnitTypes.Percent) // Stop loss
			);

			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, ema);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal emaValue)
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Calculate EMA slope only if we have previous EMA value
			if (_previousEmaValue != 0)
			{
				// Calculate current slope
				_currentSlope = emaValue - _previousEmaValue;

				// Update statistics
				_slopeCount++;
				_sumSlopes += _currentSlope;

				// Update average slope
				if (_slopeCount > 0)
					_averageSlope = _sumSlopes / _slopeCount;

				// Calculate sum of squared differences for std dev
				_sumSquaredDiff += (_currentSlope - _averageSlope) * (_currentSlope - _averageSlope);

				// Calculate standard deviation after we have enough samples
				if (_slopeCount >= SlopeLookback)
				{
					_slopeStdDev = (decimal)Math.Sqrt((double)(_sumSquaredDiff / _slopeCount));

					// Remove oldest slope value contribution (simple approximation)
					if (_slopeCount > SlopeLookback)
					{
						_slopeCount = SlopeLookback;
						_sumSlopes = _averageSlope * SlopeLookback;
						_sumSquaredDiff = _slopeStdDev * _slopeStdDev * SlopeLookback;
					}

					// Calculate entry thresholds
					var lowerThreshold = _averageSlope - ThresholdMultiplier * _slopeStdDev;
					var upperThreshold = _averageSlope + ThresholdMultiplier * _slopeStdDev;

					// Trading logic
					if (_currentSlope < lowerThreshold && Position <= 0)
					{
						// Slope is below lower threshold (falling rapidly) - mean reversion buy signal
						BuyMarket(Volume + Math.Abs(Position));
						LogInfo($"BUY Signal: Slope {_currentSlope:F6} < Lower Threshold {lowerThreshold:F6}");
					}
					else if (_currentSlope > upperThreshold && Position >= 0)
					{
						// Slope is above upper threshold (rising rapidly) - mean reversion sell signal
						SellMarket(Volume + Math.Abs(Position));
						LogInfo($"SELL Signal: Slope {_currentSlope:F6} > Upper Threshold {upperThreshold:F6}");
					}
					else if (_currentSlope > _averageSlope && Position > 0)
					{
						// Exit long position when slope returns to average (profit target)
						SellMarket(Position);
						LogInfo($"EXIT LONG: Slope {_currentSlope:F6} returned to average {_averageSlope:F6}");
					}
					else if (_currentSlope < _averageSlope && Position < 0)
					{
						// Exit short position when slope returns to average (profit target)
						BuyMarket(Math.Abs(Position));
						LogInfo($"EXIT SHORT: Slope {_currentSlope:F6} returned to average {_averageSlope:F6}");
					}
				}
			}

			// Save current EMA value for next calculation
			_previousEmaValue = emaValue;
		}
	}
}
