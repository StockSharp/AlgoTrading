using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// MACD Slope Mean Reversion Strategy.
	/// This strategy trades based on MACD histogram slope reversions to the mean.
	/// </summary>
	public class MacdSlopeMeanReversionStrategy : Strategy
	{
		private readonly StrategyParam<int> _fastMacdPeriod;
		private readonly StrategyParam<int> _slowMacdPeriod;
		private readonly StrategyParam<int> _signalMacdPeriod;
		private readonly StrategyParam<int> _lookbackPeriod;
		private readonly StrategyParam<decimal> _deviationMultiplier;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		private MovingAverageConvergenceDivergence _macd;
		private decimal _previousHistogram;
		private decimal _currentHistogramSlope;
		private bool _isFirstCalculation = true;

		private decimal _averageSlope;
		private decimal _slopeStdDev;
		private int _sampleCount;
		private decimal _sumSlopes;
		private decimal _sumSlopesSquared;

		/// <summary>
		/// MACD Fast Period.
		/// </summary>
		public int FastMacdPeriod
		{
			get => _fastMacdPeriod.Value;
			set => _fastMacdPeriod.Value = value;
		}

		/// <summary>
		/// MACD Slow Period.
		/// </summary>
		public int SlowMacdPeriod
		{
			get => _slowMacdPeriod.Value;
			set => _slowMacdPeriod.Value = value;
		}

		/// <summary>
		/// MACD Signal Period.
		/// </summary>
		public int SignalMacdPeriod
		{
			get => _signalMacdPeriod.Value;
			set => _signalMacdPeriod.Value = value;
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
		public MacdSlopeMeanReversionStrategy()
		{
			_fastMacdPeriod = Param(nameof(FastMacdPeriod), 12)
				.SetGreaterThanZero()
				.SetDisplay("MACD Fast", "Fast EMA period for MACD", "Indicator Parameters")
				.SetCanOptimize(true)
				.SetOptimize(8, 20, 2);

			_slowMacdPeriod = Param(nameof(SlowMacdPeriod), 26)
				.SetGreaterThanZero()
				.SetDisplay("MACD Slow", "Slow EMA period for MACD", "Indicator Parameters")
				.SetCanOptimize(true)
				.SetOptimize(20, 40, 2);

			_signalMacdPeriod = Param(nameof(SignalMacdPeriod), 9)
				.SetGreaterThanZero()
				.SetDisplay("MACD Signal", "Signal line period for MACD", "Indicator Parameters")
				.SetCanOptimize(true)
				.SetOptimize(5, 15, 2);

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
			return new[] { (Security, CandleType) };
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			// Initialize indicators
			_macd = new MovingAverageConvergenceDivergence
			{
				FastMa = new ExponentialMovingAverage { Length = FastMacdPeriod },
				SlowMa = new ExponentialMovingAverage { Length = SlowMacdPeriod },
				SignalMa = new ExponentialMovingAverage { Length = SignalMacdPeriod }
			};

			// Initialize statistics variables
			_sampleCount = 0;
			_sumSlopes = 0;
			_sumSlopesSquared = 0;
			_isFirstCalculation = true;

			// Create subscription and bind indicator
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(_macd, ProcessCandle)
				.Start();

			// Set up chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _macd);
				DrawOwnTrades(area);
			}

			// Start position protection
			StartProtection(
				new Unit(StopLossPercent, UnitTypes.Percent), 
				new Unit(StopLossPercent, UnitTypes.Percent));

			base.OnStarted(time);
		}

		private void ProcessCandle(ICandleMessage candle, decimal macdValue, decimal signalValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Calculate MACD histogram
			var histogram = macdValue - signalValue;

			// Calculate MACD histogram slope
			if (_isFirstCalculation)
			{
				_previousHistogram = histogram;
				_isFirstCalculation = false;
				return;
			}

			_currentHistogramSlope = histogram - _previousHistogram;
			_previousHistogram = histogram;

			// Update statistics for slope values
			_sampleCount++;
			_sumSlopes += _currentHistogramSlope;
			_sumSlopesSquared += _currentHistogramSlope * _currentHistogramSlope;

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
			if (_currentHistogramSlope < longEntryThreshold && Position <= 0)
			{
				// Long entry: slope is significantly lower than average (mean reversion expected)
				LogInfo($"MACD histogram slope {_currentHistogramSlope} below threshold {longEntryThreshold}, entering LONG");
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (_currentHistogramSlope > shortEntryThreshold && Position >= 0)
			{
				// Short entry: slope is significantly higher than average (mean reversion expected)
				LogInfo($"MACD histogram slope {_currentHistogramSlope} above threshold {shortEntryThreshold}, entering SHORT");
				SellMarket(Volume + Math.Abs(Position));
			}
			else if (Position > 0 && _currentHistogramSlope > _averageSlope)
			{
				// Exit long when slope returns to or above average
				LogInfo($"MACD histogram slope {_currentHistogramSlope} returned to average {_averageSlope}, exiting LONG");
				SellMarket(Math.Abs(Position));
			}
			else if (Position < 0 && _currentHistogramSlope < _averageSlope)
			{
				// Exit short when slope returns to or below average
				LogInfo($"MACD histogram slope {_currentHistogramSlope} returned to average {_averageSlope}, exiting SHORT");
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}
