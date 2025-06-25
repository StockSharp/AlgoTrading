using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on VWAP (Volume Weighted Average Price) Slope breakout
	/// Enters positions when the slope of VWAP exceeds average slope plus a multiple of standard deviation
	/// </summary>
	public class VwapSlopeBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _lookbackPeriod;
		private readonly StrategyParam<decimal> _deviationMultiplier;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLossPercent;

		private VolumeWeightedMovingAverage _vwap;
		
		private decimal _prevVwapValue;
		private decimal _currentSlope;
		private decimal _avgSlope;
		private decimal _stdDevSlope;
		private decimal[] _slopes;
		private int _currentIndex;
		private bool _isInitialized;

		/// <summary>
		/// Lookback period for slope statistics calculation
		/// </summary>
		public int LookbackPeriod
		{
			get => _lookbackPeriod.Value;
			set => _lookbackPeriod.Value = value;
		}

		/// <summary>
		/// Standard deviation multiplier for breakout detection
		/// </summary>
		public decimal DeviationMultiplier
		{
			get => _deviationMultiplier.Value;
			set => _deviationMultiplier.Value = value;
		}

		/// <summary>
		/// Candle type
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Stop loss percentage
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public VwapSlopeBreakoutStrategy()
		{
			_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Lookback Period", "Period for slope statistics calculation", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_deviationMultiplier = Param(nameof(DeviationMultiplier), 2m)
				.SetGreaterThan(0)
				.SetDisplay("Deviation Multiplier", "Standard deviation multiplier for breakout detection", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(1m, 3m, 0.5m);
				
			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetGreaterThan(0)
				.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management");

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
			// VolumeWeightedMovingAverage is used as VWAP indicator
			_vwap = new VolumeWeightedMovingAverage();
			
			_prevVwapValue = 0;
			_currentSlope = 0;
			_avgSlope = 0;
			_stdDevSlope = 0;
			_slopes = new decimal[LookbackPeriod];
			_currentIndex = 0;
			_isInitialized = false;

			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(_vwap, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _vwap);
				DrawOwnTrades(area);
			}

			// Set up position protection
			StartProtection(
				takeProfit: null, // We'll handle exits via strategy logic
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
			);

			base.OnStarted(time);
		}

		private void ProcessCandle(ICandleMessage candle, decimal vwapValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Initialize on first valid value
			if (!_isInitialized)
			{
				_prevVwapValue = vwapValue;
				_isInitialized = true;
				return;
			}
			
			// Calculate current slope (simple difference for now)
			_currentSlope = vwapValue - _prevVwapValue;
			
			// Store slope in array and update index
			_slopes[_currentIndex] = _currentSlope;
			_currentIndex = (_currentIndex + 1) % LookbackPeriod;
			
			// Calculate statistics once we have enough data
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
				
			CalculateStatistics();
			
			// Trading logic
			if (Math.Abs(_avgSlope) > 0)  // Avoid division by zero
			{
				// Long signal: slope exceeds average + k*stddev (slope is positive and we don't have a long position)
				if (_currentSlope > 0 && 
					_currentSlope > _avgSlope + DeviationMultiplier * _stdDevSlope && 
					Position <= 0)
				{
					// Cancel existing orders
					CancelActiveOrders();
					
					// Enter long position
					var volume = Volume + Math.Abs(Position);
					BuyMarket(volume);
					
					LogInfo($"Long signal: VWAP Slope {_currentSlope} > Avg {_avgSlope} + {DeviationMultiplier}*StdDev {_stdDevSlope}");
				}
				// Short signal: slope exceeds average + k*stddev in negative direction (slope is negative and we don't have a short position)
				else if (_currentSlope < 0 && 
						 _currentSlope < _avgSlope - DeviationMultiplier * _stdDevSlope && 
						 Position >= 0)
				{
					// Cancel existing orders
					CancelActiveOrders();
					
					// Enter short position
					var volume = Volume + Math.Abs(Position);
					SellMarket(volume);
					
					LogInfo($"Short signal: VWAP Slope {_currentSlope} < Avg {_avgSlope} - {DeviationMultiplier}*StdDev {_stdDevSlope}");
				}
				
				// Exit conditions - when slope returns to average
				if (Position > 0 && _currentSlope < _avgSlope)
				{
					// Exit long position
					SellMarket(Math.Abs(Position));
					LogInfo($"Exit long: VWAP Slope {_currentSlope} < Avg {_avgSlope}");
				}
				else if (Position < 0 && _currentSlope > _avgSlope)
				{
					// Exit short position
					BuyMarket(Math.Abs(Position));
					LogInfo($"Exit short: VWAP Slope {_currentSlope} > Avg {_avgSlope}");
				}
			}
			
			// Store current VWAP value for next slope calculation
			_prevVwapValue = vwapValue;
		}
		
		private void CalculateStatistics()
		{
			// Reset statistics
			_avgSlope = 0;
			decimal sumSquaredDiffs = 0;
			
			// Calculate average
			for (int i = 0; i < LookbackPeriod; i++)
			{
				_avgSlope += _slopes[i];
			}
			_avgSlope /= LookbackPeriod;
			
			// Calculate standard deviation
			for (int i = 0; i < LookbackPeriod; i++)
			{
				decimal diff = _slopes[i] - _avgSlope;
				sumSquaredDiffs += diff * diff;
			}
			
			_stdDevSlope = (decimal)Math.Sqrt((double)(sumSquaredDiffs / LookbackPeriod));
		}
	}
}