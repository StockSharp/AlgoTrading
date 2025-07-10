using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Volume Slope Mean Reversion Strategy.
	/// This strategy trades based on volume slope reversions to the mean.
	/// </summary>
	public class VolumeSlopeMeanReversionStrategy : Strategy
	{
		private readonly StrategyParam<int> _volumeMaPeriod;
		private readonly StrategyParam<int> _lookbackPeriod;
		private readonly StrategyParam<decimal> _deviationMultiplier;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		private SimpleMovingAverage _volumeMa;
		private decimal _previousVolumeRatio;
		private decimal _currentVolumeSlope;
		private bool _isFirstCalculation = true;

		private decimal _averageSlope;
		private decimal _slopeStdDev;
		private int _sampleCount;
		private decimal _sumSlopes;
		private decimal _sumSlopesSquared;

		/// <summary>
		/// Volume Moving Average Period.
		/// </summary>
		public int VolumeMaPeriod
		{
			get => _volumeMaPeriod.Value;
			set => _volumeMaPeriod.Value = value;
		}

		/// <summary>
		/// Period for calculating slope average and standard deviation.
		/// </summary>
		public int LookbackPeriod
		{
			get => _lookbackPeriod.Value;
			set => _lookbackPeriod.Value = value;
		}

		/// <summary>
		/// The multiplier for standard deviation to determine entry threshold.
		/// </summary>
		public decimal DeviationMultiplier
		{
			get => _deviationMultiplier.Value;
			set => _deviationMultiplier.Value = value;
		}

		/// <summary>
		/// Stop loss percentage.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
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
		/// Constructor.
		/// </summary>
		public VolumeSlopeMeanReversionStrategy()
		{
			_volumeMaPeriod = Param(nameof(VolumeMaPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Volume MA Period", "Period for Volume Moving Average", "Indicator Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Lookback Period", "Period for calculating average and standard deviation of the slope", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_deviationMultiplier = Param(nameof(DeviationMultiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Deviation Multiplier", "Multiplier for standard deviation to determine entry threshold", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 5.0m, 0.5m);

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
			_previousVolumeRatio = 0;
			_currentVolumeSlope = 0;
			_averageSlope = 0;
			_slopeStdDev = 0;
			_sampleCount = 0;
			_sumSlopes = 0;
			_sumSlopesSquared = 0;
			_isFirstCalculation = true;

			// Initialize indicators
			_volumeMa = new SimpleMovingAverage
			{
				Length = VolumeMaPeriod
			};

			// Initialize statistics variables
			_sampleCount = 0;
			_sumSlopes = 0;
			_sumSlopesSquared = 0;
			_isFirstCalculation = true;

			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(ProcessCandle)
				.Start();

			// Set up chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawOwnTrades(area);
			}

			// Start position protection
			StartProtection(
				new Unit(StopLossPercent, UnitTypes.Percent), 
				new Unit(StopLossPercent, UnitTypes.Percent));

			base.OnStarted(time);
		}

		private void ProcessCandle(ICandleMessage candle)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Process volume through SMA
			var volumeIndicatorValue = _volumeMa.Process(candle.TotalVolume, candle.ServerTime, candle.State == CandleStates.Finished);
			
			// Skip if indicator is not formed yet
			if (!_volumeMa.IsFormed)
				return;
				
			// Calculate volume ratio (current volume / average volume)
			var volumeRatio = candle.TotalVolume / volumeIndicatorValue.ToDecimal();

			// Calculate volume ratio slope
			if (_isFirstCalculation)
			{
				_previousVolumeRatio = volumeRatio;
				_isFirstCalculation = false;
				return;
			}

			_currentVolumeSlope = volumeRatio - _previousVolumeRatio;
			_previousVolumeRatio = volumeRatio;

			// Update statistics for slope values
			_sampleCount++;
			_sumSlopes += _currentVolumeSlope;
			_sumSlopesSquared += _currentVolumeSlope * _currentVolumeSlope;

			// We need enough samples to calculate meaningful statistics
			if (_sampleCount < LookbackPeriod)
				return;

			// If we have more samples than our lookback period, adjust the statistics
			if (_sampleCount > LookbackPeriod)
			{
				// This is a simplified approach - ideally we would keep a circular buffer
				// of the last N slopes for more accurate calculations
				_sampleCount = LookbackPeriod;
			}

			// Calculate statistics
			_averageSlope = _sumSlopes / _sampleCount;
			var variance = (_sumSlopesSquared / _sampleCount) - (_averageSlope * _averageSlope);
			_slopeStdDev = (variance <= 0) ? 0 : (decimal)Math.Sqrt((double)variance);

			// Calculate thresholds for entries
			var longEntryThreshold = _averageSlope - DeviationMultiplier * _slopeStdDev;
			var shortEntryThreshold = _averageSlope + DeviationMultiplier * _slopeStdDev;

			// Determine price direction based on candle
			var isBullishCandle = candle.ClosePrice > candle.OpenPrice;

			// Trading logic - we take into account both volume slope and price direction
			if (_currentVolumeSlope < longEntryThreshold && Position <= 0)
			{
				if (isBullishCandle)
				{
					// Long entry: volume slope is significantly lower than average on a bullish candle
					// This indicates potential for bullish continuation with volume mean reversion
					LogInfo($"Volume slope {_currentVolumeSlope} below threshold {longEntryThreshold} with bullish price, entering LONG");
					BuyMarket(Volume + Math.Abs(Position));
				}
			}
			else if (_currentVolumeSlope > shortEntryThreshold && Position >= 0)
			{
				if (!isBullishCandle)
				{
					// Short entry: volume slope is significantly higher than average on a bearish candle
					// This indicates potential for bearish continuation with volume mean reversion
					LogInfo($"Volume slope {_currentVolumeSlope} above threshold {shortEntryThreshold} with bearish price, entering SHORT");
					SellMarket(Volume + Math.Abs(Position));
				}
			}
			else if (Position > 0 && _currentVolumeSlope > _averageSlope)
			{
				// Exit long when volume slope returns to or above average
				LogInfo($"Volume slope {_currentVolumeSlope} returned to average {_averageSlope}, exiting LONG");
				SellMarket(Math.Abs(Position));
			}
			else if (Position < 0 && _currentVolumeSlope < _averageSlope)
			{
				// Exit short when volume slope returns to or below average
				LogInfo($"Volume slope {_currentVolumeSlope} returned to average {_averageSlope}, exiting SHORT");
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}
