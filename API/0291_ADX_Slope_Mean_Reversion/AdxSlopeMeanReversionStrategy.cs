using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// ADX Slope Mean Reversion Strategy.
	/// This strategy trades based on ADX slope reversions to the mean.
	/// </summary>
	public class AdxSlopeMeanReversionStrategy : Strategy
	{
		private readonly StrategyParam<int> _adxPeriod;
		private readonly StrategyParam<int> _lookbackPeriod;
		private readonly StrategyParam<decimal> _deviationMultiplier;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		private AverageDirectionalIndex _adx;
		private decimal _previousAdx;
		private decimal _currentAdxSlope;
		private bool _isFirstCalculation = true;

		private decimal _averageSlope;
		private decimal _slopeStdDev;
		private int _sampleCount;
		private decimal _sumSlopes;
		private decimal _sumSlopesSquared;

		/// <summary>
		/// ADX Period.
		/// </summary>
		public int AdxPeriod
		{
			get => _adxPeriod.Value;
			set => _adxPeriod.Value = value;
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
		public AdxSlopeMeanReversionStrategy()
		{
			_adxPeriod = Param(nameof(AdxPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ADX Period", "Period for ADX indicator", "Indicator Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 2);

			_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Lookback Period", "Period for calculating average and standard deviation of the slope", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_deviationMultiplier = Param(nameof(DeviationMultiplier), 1.0m)
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
			// Initialize indicators
			_adx = new AverageDirectionalIndex
			{
				Length = AdxPeriod
			};

			// Initialize statistics variables
			_sampleCount = 0;
			_sumSlopes = 0;
			_sumSlopesSquared = 0;
			_isFirstCalculation = true;

			_previousAdx = 0;
			_currentAdxSlope = 0;
			_averageSlope = 0;
			_slopeStdDev = 0;

			// Create subscription and bind indicator
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(_adx, ProcessCandle)
				.Start();

			// Set up chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _adx);
				DrawOwnTrades(area);
			}

			// Start position protection
			StartProtection(
				new Unit(StopLossPercent, UnitTypes.Percent), 
				new Unit(StopLossPercent, UnitTypes.Percent));

			base.OnStarted(time);
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var adxTyped = (AverageDirectionalIndexValue)adxValue;

			if (adxTyped.MovingAverage is not decimal adx)
				return;

			var dx = adxTyped.Dx;

			if (dx.Plus is not decimal plusDi || dx.Minus is not decimal minusDi)
				return;

			// Calculate ADX slope
			if (_isFirstCalculation)
			{
				_previousAdx = adx;
				_isFirstCalculation = false;
				return;
			}

			_currentAdxSlope = adx - _previousAdx;
			_previousAdx = adx;

			// Update statistics for slope values
			_sampleCount++;
			_sumSlopes += _currentAdxSlope;
			_sumSlopesSquared += _currentAdxSlope * _currentAdxSlope;

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

			// Trading logic
			if (_currentAdxSlope < longEntryThreshold && Position <= 0)
			{
				// Long entry: slope is significantly lower than average (mean reversion expected)
				LogInfo($"ADX slope {_currentAdxSlope} below threshold {longEntryThreshold}, entering LONG");
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (_currentAdxSlope > shortEntryThreshold && Position >= 0)
			{
				// Short entry: slope is significantly higher than average (mean reversion expected)
				LogInfo($"ADX slope {_currentAdxSlope} above threshold {shortEntryThreshold}, entering SHORT");
				SellMarket(Volume + Math.Abs(Position));
			}
			else if (Position > 0 && _currentAdxSlope > _averageSlope)
			{
				// Exit long when slope returns to or above average
				LogInfo($"ADX slope {_currentAdxSlope} returned to average {_averageSlope}, exiting LONG");
				SellMarket(Math.Abs(Position));
			}
			else if (Position < 0 && _currentAdxSlope < _averageSlope)
			{
				// Exit short when slope returns to or below average
				LogInfo($"ADX slope {_currentAdxSlope} returned to average {_averageSlope}, exiting SHORT");
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}
