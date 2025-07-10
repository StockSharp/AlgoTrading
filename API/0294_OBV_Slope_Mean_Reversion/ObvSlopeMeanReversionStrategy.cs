using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// OBV Slope Mean Reversion Strategy.
	/// This strategy trades based on On-Balance Volume (OBV) slope reversions to the mean.
	/// </summary>
	public class ObvSlopeMeanReversionStrategy : Strategy
	{
		private readonly StrategyParam<int> _obvSmaPeriod;
		private readonly StrategyParam<int> _lookbackPeriod;
		private readonly StrategyParam<decimal> _deviationMultiplier;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		private OnBalanceVolume _obv;
		private SimpleMovingAverage _obvSma;
		private decimal _previousObv;
		private decimal _currentObvValue;
		private decimal _currentObvSlope;
		private bool _isFirstCalculation = true;

		private decimal _averageSlope;
		private decimal _slopeStdDev;
		private int _sampleCount;
		private decimal _sumSlopes;
		private decimal _sumSlopesSquared;
		private readonly Queue<decimal> _slopeBuffer = []; // buffer for last N slopes

		/// <summary>
		/// OBV SMA Period.
		/// </summary>
		public int ObvSmaPeriod
		{
			get => _obvSmaPeriod.Value;
			set => _obvSmaPeriod.Value = value;
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
		public ObvSlopeMeanReversionStrategy()
		{
			_obvSmaPeriod = Param(nameof(ObvSmaPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("OBV SMA Period", "Period for OBV SMA", "Indicator Parameters")
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
			// Initialize indicators
			_obv = new OnBalanceVolume();
			_obvSma = new SimpleMovingAverage
			{
				Length = ObvSmaPeriod
			};

			// Initialize statistics variables
			_sampleCount = 0;
			_sumSlopes = 0;
			_sumSlopesSquared = 0;
			_isFirstCalculation = true;
			_slopeBuffer.Clear();

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
				DrawIndicator(area, _obv);
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

			// Process the candle with OBV indicator
			var obvValue = _obv.Process(candle).ToDecimal();
			
			// Process OBV through SMA
			var obvSmaValue = _obvSma.Process(obvValue, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal();
			
			// Skip if OBV SMA is not formed yet
			if (!_obvSma.IsFormed)
				return;
				
			// Save current OBV value
			_currentObvValue = obvValue;

			// Calculate OBV slope
			if (_isFirstCalculation)
			{
				_previousObv = _currentObvValue;
				_isFirstCalculation = false;
				return;
			}

			_currentObvSlope = _currentObvValue - _previousObv;
			_previousObv = _currentObvValue;

			// Update statistics for slope values using a circular buffer
			if (_slopeBuffer.Count == LookbackPeriod)
			{
				var removed = _slopeBuffer.Dequeue();
				_sumSlopes -= removed;
				_sumSlopesSquared -= removed * removed;
			}
			_slopeBuffer.Enqueue(_currentObvSlope);
			_sumSlopes += _currentObvSlope;
			_sumSlopesSquared += _currentObvSlope * _currentObvSlope;
			_sampleCount = _slopeBuffer.Count;

			// We need enough samples to calculate meaningful statistics
			if (_sampleCount < LookbackPeriod)
				return;

			// Calculate statistics
			_averageSlope = _sumSlopes / _sampleCount;
			var variance = (_sumSlopesSquared / _sampleCount) - (_averageSlope * _averageSlope);
			_slopeStdDev = (variance <= 0) ? 0 : (decimal)Math.Sqrt((double)variance);

			// Calculate thresholds for entries
			var longEntryThreshold = _averageSlope - DeviationMultiplier * _slopeStdDev;
			var shortEntryThreshold = _averageSlope + DeviationMultiplier * _slopeStdDev;

			// OBV divergence check (price vs OBV)
			var priceChange = candle.ClosePrice - candle.OpenPrice;
			var obvChangeRelativeToPrice = (priceChange == 0) ? 0 : (_currentObvSlope / Math.Abs(priceChange));

			// Trading logic
			if (_currentObvSlope < longEntryThreshold && Position <= 0)
			{
				// Long entry: OBV slope is significantly lower than average (mean reversion expected)
				// Additional filter: Check for positive price movement to confirm potential reversal
				if (candle.ClosePrice > candle.OpenPrice)
				{
					LogInfo($"OBV slope {_currentObvSlope} below threshold {longEntryThreshold}, entering LONG");
					BuyMarket(Volume + Math.Abs(Position));
				}
			}
			else if (_currentObvSlope > shortEntryThreshold && Position >= 0)
			{
				// Short entry: OBV slope is significantly higher than average (mean reversion expected)
				// Additional filter: Check for negative price movement to confirm potential reversal
				if (candle.ClosePrice < candle.OpenPrice)
				{
					LogInfo($"OBV slope {_currentObvSlope} above threshold {shortEntryThreshold}, entering SHORT");
					SellMarket(Volume + Math.Abs(Position));
				}
			}
			else if (Position > 0 && _currentObvSlope > _averageSlope)
			{
				// Exit long when OBV slope returns to or above average
				LogInfo($"OBV slope {_currentObvSlope} returned to average {_averageSlope}, exiting LONG");
				SellMarket(Math.Abs(Position));
			}
			else if (Position < 0 && _currentObvSlope < _averageSlope)
			{
				// Exit short when OBV slope returns to or below average
				LogInfo($"OBV slope {_currentObvSlope} returned to average {_averageSlope}, exiting SHORT");
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}