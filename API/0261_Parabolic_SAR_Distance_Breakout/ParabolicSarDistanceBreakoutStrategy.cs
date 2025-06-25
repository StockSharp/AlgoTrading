using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that enters positions when the distance between price and Parabolic SAR
	/// exceeds the average distance plus a multiple of standard deviation
	/// </summary>
	public class ParabolicSarDistanceBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<decimal> _acceleration;
		private readonly StrategyParam<decimal> _maxAcceleration;
		private readonly StrategyParam<int> _lookbackPeriod;
		private readonly StrategyParam<decimal> _deviationMultiplier;
		private readonly StrategyParam<DataType> _candleType;

		private ParabolicSar _parabolicSar;
		
		private decimal _avgDistanceLong;
		private decimal _stdDevDistanceLong;
		private decimal _avgDistanceShort;
		private decimal _stdDevDistanceShort;
		
		private decimal _lastLongDistance;
		private decimal _lastShortDistance;
		private int _samplesCount;

		/// <summary>
		/// Initial acceleration factor for Parabolic SAR
		/// </summary>
		public decimal Acceleration
		{
			get => _acceleration.Value;
			set => _acceleration.Value = value;
		}

		/// <summary>
		/// Maximum acceleration factor for Parabolic SAR
		/// </summary>
		public decimal MaxAcceleration
		{
			get => _maxAcceleration.Value;
			set => _maxAcceleration.Value = value;
		}

		/// <summary>
		/// Lookback period for distance statistics calculation
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
		/// Constructor
		/// </summary>
		public ParabolicSarDistanceBreakoutStrategy()
		{
			_acceleration = Param(nameof(Acceleration), 0.02m)
				.SetGreaterThan(0)
				.SetDisplay("Acceleration", "Initial acceleration factor for Parabolic SAR", "Indicator Parameters")
				.SetCanOptimize(true)
				.SetOptimize(0.01m, 0.05m, 0.01m);

			_maxAcceleration = Param(nameof(MaxAcceleration), 0.2m)
				.SetGreaterThan(0)
				.SetDisplay("Max Acceleration", "Maximum acceleration factor for Parabolic SAR", "Indicator Parameters")
				.SetCanOptimize(true)
				.SetOptimize(0.1m, 0.5m, 0.1m);

			_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Lookback Period", "Period for statistical calculations", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_deviationMultiplier = Param(nameof(DeviationMultiplier), 2m)
				.SetGreaterThan(0)
				.SetDisplay("Deviation Multiplier", "Standard deviation multiplier for breakout detection", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(1m, 3m, 0.5m);

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
			_parabolicSar = new ParabolicSar
			{
				Acceleration = Acceleration,
				MaxAcceleration = MaxAcceleration
			};

			_avgDistanceLong = 0;
			_stdDevDistanceLong = 0;
			_avgDistanceShort = 0;
			_stdDevDistanceShort = 0;
			_lastLongDistance = 0;
			_lastShortDistance = 0;
			_samplesCount = 0;

			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(_parabolicSar, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _parabolicSar);
				DrawOwnTrades(area);
			}

			// Set up position protection using the dynamic Parabolic SAR
			StartProtection(
				takeProfit: null, // We'll handle exits via strategy logic
				stopLoss: null,   // The dynamic SAR will act as our stop
				isStopTrailing: true
			);

			base.OnStarted(time);
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue parabolicSarValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Extract Parabolic SAR value
			decimal sarValue = parabolicSarValue.GetValue<decimal>();
			
			// Calculate distances
			decimal longDistance = 0;
			decimal shortDistance = 0;
			
			// If SAR is below price, it's in uptrend
			if (sarValue < candle.ClosePrice)
				longDistance = candle.ClosePrice - sarValue;
			// If SAR is above price, it's in downtrend
			else if (sarValue > candle.ClosePrice)
				shortDistance = sarValue - candle.ClosePrice;
			
			// Update statistics
			UpdateDistanceStatistics(longDistance, shortDistance);
			
			// Trading logic
			if (_samplesCount >= LookbackPeriod)
			{
				// Long signal: distance exceeds average + k*stddev and we don't have a long position
				if (longDistance > 0 && 
					longDistance > _avgDistanceLong + DeviationMultiplier * _stdDevDistanceLong && 
					Position <= 0)
				{
					// Cancel existing orders
					CancelActiveOrders();
					
					// Enter long position
					var volume = Volume + Math.Abs(Position);
					BuyMarket(volume);
					
					LogInfo($"Long signal: Distance {longDistance} > Avg {_avgDistanceLong} + {DeviationMultiplier}*StdDev {_stdDevDistanceLong}");
				}
				// Short signal: distance exceeds average + k*stddev and we don't have a short position
				else if (shortDistance > 0 && 
						 shortDistance > _avgDistanceShort + DeviationMultiplier * _stdDevDistanceShort && 
						 Position >= 0)
				{
					// Cancel existing orders
					CancelActiveOrders();
					
					// Enter short position
					var volume = Volume + Math.Abs(Position);
					SellMarket(volume);
					
					LogInfo($"Short signal: Distance {shortDistance} > Avg {_avgDistanceShort} + {DeviationMultiplier}*StdDev {_stdDevDistanceShort}");
				}
				
				// Exit conditions - when price crosses SAR
				if (Position > 0 && candle.ClosePrice < sarValue)
				{
					// Exit long position
					SellMarket(Math.Abs(Position));
					LogInfo($"Exit long: Price {candle.ClosePrice} crossed below SAR {sarValue}");
				}
				else if (Position < 0 && candle.ClosePrice > sarValue)
				{
					// Exit short position
					BuyMarket(Math.Abs(Position));
					LogInfo($"Exit short: Price {candle.ClosePrice} crossed above SAR {sarValue}");
				}
			}
			
			// Store current distances for next update
			_lastLongDistance = longDistance;
			_lastShortDistance = shortDistance;
		}
		
		private void UpdateDistanceStatistics(decimal longDistance, decimal shortDistance)
		{
			_samplesCount++;
			
			// Simple calculation of running average and standard deviation
			if (_samplesCount == 1)
			{
				// Initialize with first values
				_avgDistanceLong = longDistance;
				_avgDistanceShort = shortDistance;
				_stdDevDistanceLong = 0;
				_stdDevDistanceShort = 0;
			}
			else
			{
				// Update running average
				decimal oldAvgLong = _avgDistanceLong;
				decimal oldAvgShort = _avgDistanceShort;
				
				_avgDistanceLong = oldAvgLong + (longDistance - oldAvgLong) / _samplesCount;
				_avgDistanceShort = oldAvgShort + (shortDistance - oldAvgShort) / _samplesCount;
				
				// Update running standard deviation using Welford's algorithm
				if (_samplesCount > 1)
				{
					_stdDevDistanceLong = (1 - 1.0m / (_samplesCount - 1)) * _stdDevDistanceLong + 
										   _samplesCount * ((_avgDistanceLong - oldAvgLong) * (_avgDistanceLong - oldAvgLong));
					
					_stdDevDistanceShort = (1 - 1.0m / (_samplesCount - 1)) * _stdDevDistanceShort + 
											_samplesCount * ((_avgDistanceShort - oldAvgShort) * (_avgDistanceShort - oldAvgShort));
				}
				
				// We only need last LookbackPeriod samples
				if (_samplesCount > LookbackPeriod)
				{
					_samplesCount = LookbackPeriod;
				}
			}
			
			// Calculate square root for final standard deviation
			_stdDevDistanceLong = (decimal)Math.Sqrt((double)_stdDevDistanceLong / _samplesCount);
			_stdDevDistanceShort = (decimal)Math.Sqrt((double)_stdDevDistanceShort / _samplesCount);
		}
	}
}
