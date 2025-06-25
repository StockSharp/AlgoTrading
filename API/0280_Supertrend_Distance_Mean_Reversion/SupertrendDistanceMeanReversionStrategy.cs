using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Supertrend Distance Mean Reversion Strategy.
	/// This strategy trades based on the mean reversion of the distance between price and Supertrend indicator.
	/// </summary>
	public class SupertrendDistanceMeanReversionStrategy : Strategy
	{
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<decimal> _multiplier;
		private readonly StrategyParam<int> _lookbackPeriod;
		private readonly StrategyParam<decimal> _deviationMultiplier;
		private readonly StrategyParam<DataType> _candleType;
		
		private AverageTrueRange _atr;
		private SuperTrend _supertrend;
		private SimpleMovingAverage _distanceAverage;
		private StandardDeviation _distanceStdDev;
		
		private decimal _currentDistanceLong;  // Price - Supertrend (for long positions)
		private decimal _currentDistanceShort; // Supertrend - Price (for short positions)
		private decimal _prevDistanceLong;
		private decimal _prevDistanceShort;
		private decimal _prevDistanceAvgLong;
		private decimal _prevDistanceAvgShort;
		private decimal _prevDistanceStdDevLong;
		private decimal _prevDistanceStdDevShort;
		private decimal _supertrendValue;

		/// <summary>
		/// ATR period for Supertrend calculation.
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		/// <summary>
		/// Multiplier for Supertrend calculation.
		/// </summary>
		public decimal Multiplier
		{
			get => _multiplier.Value;
			set => _multiplier.Value = value;
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
		public SupertrendDistanceMeanReversionStrategy()
		{
			_atrPeriod = Param(nameof(AtrPeriod), 10)
				.SetDisplayName("ATR Period")
				.SetCategory("Supertrend")
				.SetCanOptimize(true)
				.SetOptimize(5, 20, 1);

			_multiplier = Param(nameof(Multiplier), 3.0m)
				.SetDisplayName("Multiplier")
				.SetCategory("Supertrend")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 5.0m, 0.5m);

			_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
				.SetDisplayName("Lookback Period")
				.SetCategory("Mean Reversion")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_deviationMultiplier = Param(nameof(DeviationMultiplier), 2.0m)
				.SetDisplayName("Deviation Multiplier")
				.SetCategory("Mean Reversion")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplayName("Candle Type")
				.SetCategory("General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return new[] { (Security, CandleType) };
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Initialize indicators
			_atr = new AverageTrueRange { Length = AtrPeriod };
			_supertrend = new SuperTrend { Length = AtrPeriod, Multiplier = Multiplier };
			
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
			_supertrendValue = 0;

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(_supertrend, ProcessSupertrend)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _supertrend);
				DrawOwnTrades(area);
			}
		}

		private void ProcessSupertrend(ICandleMessage candle, IIndicatorValue value)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Get the Supertrend value
			_supertrendValue = value.GetValue<decimal>();
			
			// Calculate distances
			_currentDistanceLong = candle.ClosePrice - _supertrendValue;
			_currentDistanceShort = _supertrendValue - candle.ClosePrice;
			
			// Calculate averages and standard deviations for both distances
			var longDistanceAvg = _distanceAverage.Process(new DecimalIndicatorValue(_currentDistanceLong)).GetValue<decimal>();
			var longDistanceStdDev = _distanceStdDev.Process(new DecimalIndicatorValue(_currentDistanceLong)).GetValue<decimal>();
			
			var shortDistanceAvg = _distanceAverage.Process(new DecimalIndicatorValue(_currentDistanceShort)).GetValue<decimal>();
			var shortDistanceStdDev = _distanceStdDev.Process(new DecimalIndicatorValue(_currentDistanceShort)).GetValue<decimal>();
			
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
			// For long positions - when price is far above Supertrend (mean reversion to downside)
			if (_currentDistanceLong > longDistanceExtendedThreshold && 
				_prevDistanceLong <= longDistanceExtendedThreshold && 
				Position >= 0 && candle.ClosePrice > _supertrendValue)
			{
				SellMarket(Volume + Math.Abs(Position));
				LogInfo($"Long distance extended: {_currentDistanceLong} > {longDistanceExtendedThreshold}. Selling at {candle.ClosePrice}");
			}
			// For short positions - when price is far below Supertrend (mean reversion to upside)
			else if (_currentDistanceShort > shortDistanceExtendedThreshold && 
					_prevDistanceShort <= shortDistanceExtendedThreshold && 
					Position <= 0 && candle.ClosePrice < _supertrendValue)
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
			else if (Position > 0 && _currentDistanceLong < _prevDistanceAvgLong && _prevDistanceLong >= _prevDistanceAvgLong)
			{
				SellMarket(Math.Abs(Position));
				LogInfo($"Long distance returned to average: {_currentDistanceLong} < {_prevDistanceAvgLong}. Closing long position at {candle.ClosePrice}");
			}
			
			// Use Supertrend as dynamic stop
			else if ((Position > 0 && candle.ClosePrice < _supertrendValue) || 
					(Position < 0 && candle.ClosePrice > _supertrendValue))
			{
				if (Position > 0)
				{
					SellMarket(Math.Abs(Position));
					LogInfo($"Price crossed below Supertrend: {candle.ClosePrice} < {_supertrendValue}. Closing long position at {candle.ClosePrice}");
				}
				else if (Position < 0)
				{
					BuyMarket(Math.Abs(Position));
					LogInfo($"Price crossed above Supertrend: {candle.ClosePrice} > {_supertrendValue}. Closing short position at {candle.ClosePrice}");
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