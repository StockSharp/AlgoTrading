using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Quoting;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that enters positions when the distance between price and Supertrend
	/// exceeds the average distance plus a multiple of standard deviation
	/// </summary>
	public class SupertrendDistanceBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _supertrendPeriod;
		private readonly StrategyParam<decimal> _supertrendMultiplier;
		private readonly StrategyParam<int> _lookbackPeriod;
		private readonly StrategyParam<decimal> _deviationMultiplier;
		private readonly StrategyParam<DataType> _candleType;

		private SuperTrend _supertrend;
		private AverageTrueRange _atr;
		
		private decimal _avgDistanceLong;
		private decimal _stdDevDistanceLong;
		private decimal _avgDistanceShort;
		private decimal _stdDevDistanceShort;
		
		private decimal _lastLongDistance;
		private decimal _lastShortDistance;
		private int _samplesCount;

		/// <summary>
		/// Supertrend period
		/// </summary>
		public int SupertrendPeriod
		{
			get => _supertrendPeriod.Value;
			set => _supertrendPeriod.Value = value;
		}

		/// <summary>
		/// Supertrend multiplier
		/// </summary>
		public decimal SupertrendMultiplier
		{
			get => _supertrendMultiplier.Value;
			set => _supertrendMultiplier.Value = value;
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
		public SupertrendDistanceBreakoutStrategy()
		{
			_supertrendPeriod = Param(nameof(SupertrendPeriod), 10)
				.SetGreaterThanZero()
				.SetDisplay("Supertrend Period", "Period for Supertrend indicator", "Indicator Parameters")
				.SetCanOptimize(true)
				.SetOptimize(5, 20, 1);

			_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3m)
				.SetGreaterThanZero
				.SetDisplay("Supertrend Multiplier", "Multiplier for Supertrend indicator", "Indicator Parameters")
				.SetCanOptimize(true)
				.SetOptimize(1m, 5m, 0.5m);

			_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Lookback Period", "Period for statistical calculations", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_deviationMultiplier = Param(nameof(DeviationMultiplier), 2m)
				.SetGreaterThanZero
				.SetDisplay("Deviation Multiplier", "Standard deviation multiplier for breakout detection", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(1m, 3m, 0.5m);

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
			_atr = new AverageTrueRange { Length = SupertrendPeriod };
			_supertrend = new SuperTrend { Length = SupertrendPeriod, Multiplier = SupertrendMultiplier };

			_avgDistanceLong = 0;
			_stdDevDistanceLong = 0;
			_avgDistanceShort = 0;
			_stdDevDistanceShort = 0;
			_lastLongDistance = 0;
			_lastShortDistance = 0;
			_samplesCount = 0;

			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(_supertrend, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _supertrend);
				DrawOwnTrades(area);
			}

			// Set up position protection with dynamic stop-loss
			StartProtection(
				takeProfit: null, // We'll handle exits via our strategy logic
				stopLoss: new Unit(2, UnitTypes.Percent) // 2% stop-loss
			);

			base.OnStarted(time);
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue supertrendValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Extract Supertrend value
			decimal supertrendPrice = supertrendValue.ToDecimal();
			
			// Calculate distances
			decimal longDistance = 0;
			decimal shortDistance = 0;
			
			// If price is above Supertrend, calculate distance for long case
			if (candle.ClosePrice > supertrendPrice)
				longDistance = candle.ClosePrice - supertrendPrice;
			// If price is below Supertrend, calculate distance for short case
			else if (candle.ClosePrice < supertrendPrice)
				shortDistance = supertrendPrice - candle.ClosePrice;
			
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
				
				// Exit conditions - when distance returns to average
				if (Position > 0 && longDistance < _avgDistanceLong)
				{
					// Exit long position
					SellMarket(Math.Abs(Position));
					LogInfo($"Exit long: Distance {longDistance} < Avg {_avgDistanceLong}");
				}
				else if (Position < 0 && shortDistance < _avgDistanceShort)
				{
					// Exit short position
					BuyMarket(Math.Abs(Position));
					LogInfo($"Exit short: Distance {shortDistance} < Avg {_avgDistanceShort}");
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
