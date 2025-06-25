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
	/// RSI Slope Mean Reversion Strategy - strategy based on mean reversion of RSI slope.
	/// </summary>
	public class RsiSlopeMeanReversionStrategy : Strategy
	{
		private readonly StrategyParam<int> _rsiPeriod;
		private readonly StrategyParam<int> _slopeLookback;
		private readonly StrategyParam<decimal> _thresholdMultiplier;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		private decimal _previousRsiValue;
		private decimal _currentSlope;
		private decimal _averageSlope;
		private decimal _slopeStdDev;
		private int _slopeCount;
		private decimal _sumSlopes;
		private decimal _sumSquaredDiff;

		/// <summary>
		/// RSI period.
		/// </summary>
		public int RsiPeriod
		{
			get => _rsiPeriod.Value;
			set => _rsiPeriod.Value = value;
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
		/// Initialize <see cref="RsiSlopeMeanReversionStrategy"/>.
		/// </summary>
		public RsiSlopeMeanReversionStrategy()
		{
			_rsiPeriod = Param(nameof(RsiPeriod), 14)
				.SetDisplayName("RSI Period")
				.SetDescription("Relative Strength Index period")
				.SetCategory("RSI Settings")
				.SetCanOptimize(true)
				.SetOptimize(5, 30, 5);

			_slopeLookback = Param(nameof(SlopeLookback), 20)
				.SetDisplayName("Slope Lookback")
				.SetDescription("Period for slope statistics")
				.SetCategory("Slope Settings")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_thresholdMultiplier = Param(nameof(ThresholdMultiplier), 2m)
				.SetDisplayName("Threshold Multiplier")
				.SetDescription("Standard deviation multiplier for entry threshold")
				.SetCategory("Slope Settings")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetDisplayName("Stop Loss %")
				.SetDescription("Stop loss as percentage of entry price")
				.SetCategory("Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplayName("Candle Type")
				.SetDescription("Type of candles to use")
				.SetCategory("General");
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

			// Reset variables
			_previousRsiValue = 0;
			_currentSlope = 0;
			_averageSlope = 0;
			_slopeStdDev = 0;
			_slopeCount = 0;
			_sumSlopes = 0;
			_sumSquaredDiff = 0;

			// Create RSI indicator
			var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

			// Subscribe to candles and bind indicator
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.Bind(rsi, ProcessCandle)
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
				DrawIndicator(area, rsi);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Calculate RSI slope only if we have previous RSI value
			if (_previousRsiValue != 0)
			{
				// Calculate current slope
				_currentSlope = rsiValue - _previousRsiValue;

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
						// Slope is below lower threshold (RSI falling rapidly) - mean reversion buy signal
						BuyMarket(Volume + Math.Abs(Position));
						LogInfo($"BUY Signal: RSI Slope {_currentSlope:F6} < Lower Threshold {lowerThreshold:F6}");
					}
					else if (_currentSlope > upperThreshold && Position >= 0)
					{
						// Slope is above upper threshold (RSI rising rapidly) - mean reversion sell signal
						SellMarket(Volume + Math.Abs(Position));
						LogInfo($"SELL Signal: RSI Slope {_currentSlope:F6} > Upper Threshold {upperThreshold:F6}");
					}
					else if (_currentSlope > _averageSlope && Position > 0)
					{
						// Exit long position when slope returns to average (profit target)
						SellMarket(Position);
						LogInfo($"EXIT LONG: RSI Slope {_currentSlope:F6} returned to average {_averageSlope:F6}");
					}
					else if (_currentSlope < _averageSlope && Position < 0)
					{
						// Exit short position when slope returns to average (profit target)
						BuyMarket(Math.Abs(Position));
						LogInfo($"EXIT SHORT: RSI Slope {_currentSlope:F6} returned to average {_averageSlope:F6}");
					}
				}
			}

			// Save current RSI value for next calculation
			_previousRsiValue = rsiValue;
		}
	}
}
