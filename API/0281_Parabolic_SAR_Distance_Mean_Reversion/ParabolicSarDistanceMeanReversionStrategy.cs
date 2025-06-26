using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Parabolic SAR Distance Mean Reversion Strategy.
	/// This strategy trades based on the mean reversion of the distance between price and Parabolic SAR.
	/// </summary>
	public class ParabolicSarDistanceMeanReversionStrategy : Strategy
	{
		private readonly StrategyParam<decimal> _accelerationFactor;
		private readonly StrategyParam<decimal> _accelerationLimit;
		private readonly StrategyParam<int> _lookbackPeriod;
		private readonly StrategyParam<decimal> _deviationMultiplier;
		private readonly StrategyParam<DataType> _candleType;
		
		private ParabolicSar _parabolicSar;
		private SimpleMovingAverage _distanceAverage;
		private StandardDeviation _distanceStdDev;
		
		private decimal _currentDistanceLong;  // Price - SAR (for long positions)
		private decimal _currentDistanceShort; // SAR - Price (for short positions)
		private decimal _prevDistanceLong;
		private decimal _prevDistanceShort;
		private decimal _prevDistanceAvgLong;
		private decimal _prevDistanceAvgShort;
		private decimal _prevDistanceStdDevLong;
		private decimal _prevDistanceStdDevShort;
		private decimal _sarValue;

		/// <summary>
		/// Acceleration factor for Parabolic SAR.
		/// </summary>
		public decimal AccelerationFactor
		{
			get => _accelerationFactor.Value;
			set => _accelerationFactor.Value = value;
		}

		/// <summary>
		/// Acceleration limit for Parabolic SAR.
		/// </summary>
		public decimal AccelerationLimit
		{
			get => _accelerationLimit.Value;
			set => _accelerationLimit.Value = value;
		}

		/// <summary>
		/// Lookback period for calculating the average and standard deviation of distance.
		/// </summary>
		public int LookbackPeriod
		{
			get => _lookbackPeriod.Value;
			set => _lookbackPeriod.Value = value;
		}

		/// <summary>
		/// Deviation multiplier for mean reversion detection.
		/// </summary>
		public decimal DeviationMultiplier
		{
			get => _deviationMultiplier.Value;
			set => _deviationMultiplier.Value = value;
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
		/// Constructor.
		/// </summary>
		public ParabolicSarDistanceMeanReversionStrategy()
		{
			_accelerationFactor = Param(nameof(AccelerationFactor), 0.02m)
				.SetDisplay("Acceleration Factor", "Acceleration factor for Parabolic SAR", "Parabolic SAR")
				.SetCanOptimize(true)
				.SetOptimize(0.01m, 0.05m, 0.01m);

			_accelerationLimit = Param(nameof(AccelerationLimit), 0.2m)
				.SetDisplay("Acceleration Limit", "Acceleration limit for Parabolic SAR", "Parabolic SAR")
				.SetCanOptimize(true)
				.SetOptimize(0.1m, 0.3m, 0.05m);

			_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
				.SetDisplay("Lookback Period", "Lookback period for calculating the average and standard deviation of distance", "Mean Reversion")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_deviationMultiplier = Param(nameof(DeviationMultiplier), 2.0m)
				.SetDisplay("Deviation Multiplier", "Deviation multiplier for mean reversion detection", "Mean Reversion")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Candle type for strategy", "General");
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

			// Initialize indicators
			_parabolicSar = new ParabolicSar
			{
				AccelerationFactor = AccelerationFactor,
				AccelerationLimit = AccelerationLimit
			};
			
			_distanceAverage = new SimpleMovingAverage { Length = LookbackPeriod };
			_distanceStdDev = new StandardDeviation { Length = LookbackPeriod };
			
			// Reset stored values
			_currentDistanceLong = 0;
			_currentDistanceShort = 0;
			_prevDistanceLong = 0;
			_prevDistanceShort = 0;
			_prevDistanceAvgLong = 0;
			_prevDistanceAvgShort = 0;
			_prevDistanceStdDevLong = 0;
			_prevDistanceStdDevShort = 0;
			_sarValue = 0;

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(_parabolicSar, ProcessParabolicSar)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _parabolicSar);
				DrawOwnTrades(area);
			}
		}

		private void ProcessParabolicSar(ICandleMessage candle, IIndicatorValue value)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Get the Parabolic SAR value
			_sarValue = value.ToDecimal();
			
			// Calculate distances
			_currentDistanceLong = candle.ClosePrice - _sarValue;
			_currentDistanceShort = _sarValue - candle.ClosePrice;
			
			// Calculate averages and standard deviations for both distances
			var longDistanceAvg = _distanceAverage.Process(_currentDistanceLong, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal();
			var longDistanceStdDev = _distanceStdDev.Process(_currentDistanceLong, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal();
			
			var shortDistanceAvg = _distanceAverage.Process(_currentDistanceShort, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal();
			var shortDistanceStdDev = _distanceStdDev.Process(_currentDistanceShort, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal();
			
			// Skip the first value
			if (_prevDistanceLong == 0 || _prevDistanceShort == 0)
			{
				_prevDistanceLong = _currentDistanceLong;
				_prevDistanceShort = _currentDistanceShort;
				_prevDistanceAvgLong = longDistanceAvg;
				_prevDistanceAvgShort = shortDistanceAvg;
				_prevDistanceStdDevLong = longDistanceStdDev;
				_prevDistanceStdDevShort = shortDistanceStdDev;
				return;
			}
			
			// Calculate thresholds for long position
			var longDistanceExtendedThreshold = _prevDistanceAvgLong + _prevDistanceStdDevLong * DeviationMultiplier;
			
			// Calculate thresholds for short position
			var shortDistanceExtendedThreshold = _prevDistanceAvgShort + _prevDistanceStdDevShort * DeviationMultiplier;
			
			// Trading logic:
			// For long positions - when price is far above SAR (mean reversion to downside)
			if (_currentDistanceLong > longDistanceExtendedThreshold && 
				_prevDistanceLong <= longDistanceExtendedThreshold && 
				Position >= 0 && candle.ClosePrice > _sarValue)
			{
				SellMarket(Volume + Math.Abs(Position));
				LogInfo($"Long distance extended: {_currentDistanceLong} > {longDistanceExtendedThreshold}. Selling at {candle.ClosePrice}");
			}
			// For short positions - when price is far below SAR (mean reversion to upside)
			else if (_currentDistanceShort > shortDistanceExtendedThreshold && 
					_prevDistanceShort <= shortDistanceExtendedThreshold && 
					Position <= 0 && candle.ClosePrice < _sarValue)
			{
				BuyMarket(Volume + Math.Abs(Position));
				LogInfo($"Short distance extended: {_currentDistanceShort} > {shortDistanceExtendedThreshold}. Buying at {candle.ClosePrice}");
			}
			
			// Exit positions when distance returns to average
			else if (Position < 0 && _currentDistanceShort < _prevDistanceAvgShort && _prevDistanceShort >= _prevDistanceAvgShort)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short distance returned to average: {_currentDistanceShort} < {_prevDistanceAvgShort}. Closing short position at {candle.ClosePrice}");
			}
			else if (Position < 0 && _currentDistanceShort < _prevDistanceAvgShort && _prevDistanceShort >= _prevDistanceAvgShort)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short distance returned to average: {_currentDistanceShort} < {_prevDistanceAvgShort}. Closing short position at {candle.ClosePrice}");
			}
			else if (Position > 0 && _currentDistanceLong < _prevDistanceAvgLong && _prevDistanceLong >= _prevDistanceAvgLong)
			{
				SellMarket(Math.Abs(Position));
				LogInfo($"Long distance returned to average: {_currentDistanceLong} < {_prevDistanceAvgLong}. Closing long position at {candle.ClosePrice}");
			}
			
			// Use Parabolic SAR as dynamic stop
			else if ((Position > 0 && candle.ClosePrice < _sarValue) || 
					(Position < 0 && candle.ClosePrice > _sarValue))
			{
				if (Position > 0)
				{
					SellMarket(Math.Abs(Position));
					LogInfo($"Price crossed below Parabolic SAR: {candle.ClosePrice} < {_sarValue}. Closing long position at {candle.ClosePrice}");
				}
				else if (Position < 0)
				{
					BuyMarket(Math.Abs(Position));
					LogInfo($"Price crossed above Parabolic SAR: {candle.ClosePrice} > {_sarValue}. Closing short position at {candle.ClosePrice}");
				}
			}
			
			// Store current values for next comparison
			_prevDistanceLong = _currentDistanceLong;
			_prevDistanceShort = _currentDistanceShort;
			_prevDistanceAvgLong = longDistanceAvg;
			_prevDistanceAvgShort = shortDistanceAvg;
			_prevDistanceStdDevLong = longDistanceStdDev;
			_prevDistanceStdDevShort = shortDistanceStdDev;
		}
	}
}