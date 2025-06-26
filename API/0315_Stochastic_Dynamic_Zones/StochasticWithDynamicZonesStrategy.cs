using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Stochastic Oscillator with Dynamic Overbought/Oversold Zones.
	/// </summary>
	public class StochasticWithDynamicZonesStrategy : Strategy
	{
		private readonly StrategyParam<int> _stochPeriod;
		private readonly StrategyParam<int> _stochKPeriod;
		private readonly StrategyParam<int> _stochDPeriod;
		private readonly StrategyParam<int> _lookbackPeriod;
		private readonly StrategyParam<decimal> _standardDeviationFactor;
		private readonly StrategyParam<DataType> _candleType;

		// Store previous Stochastic value to detect reversals
		private decimal _prevStochK;

		/// <summary>
		/// Stochastic period parameter.
		/// </summary>
		public int StochPeriod
		{
			get => _stochPeriod.Value;
			set => _stochPeriod.Value = value;
		}

		/// <summary>
		/// Stochastic %K period parameter.
		/// </summary>
		public int StochKPeriod
		{
			get => _stochKPeriod.Value;
			set => _stochKPeriod.Value = value;
		}

		/// <summary>
		/// Stochastic %D period parameter.
		/// </summary>
		public int StochDPeriod
		{
			get => _stochDPeriod.Value;
			set => _stochDPeriod.Value = value;
		}

		/// <summary>
		/// Lookback period for dynamic zones calculation.
		/// </summary>
		public int LookbackPeriod
		{
			get => _lookbackPeriod.Value;
			set => _lookbackPeriod.Value = value;
		}

		/// <summary>
		/// Standard deviation factor for dynamic zones.
		/// </summary>
		public decimal StandardDeviationFactor
		{
			get => _standardDeviationFactor.Value;
			set => _standardDeviationFactor.Value = value;
		}

		/// <summary>
		/// Candle type parameter.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public StochasticWithDynamicZonesStrategy()
		{
			_stochPeriod = Param(nameof(StochPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("Stochastic Period", "Period for Stochastic Oscillator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(5, 30, 5);

			_stochKPeriod = Param(nameof(StochKPeriod), 3)
				.SetGreaterThanZero()
				.SetDisplay("Stochastic %K Period", "Smoothing period for %K line", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1, 10, 1);

			_stochDPeriod = Param(nameof(StochDPeriod), 3)
				.SetGreaterThanZero()
				.SetDisplay("Stochastic %D Period", "Smoothing period for %D line", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1, 10, 1);

			_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Lookback Period", "Period for dynamic zones calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 10);

			_standardDeviationFactor = Param(nameof(StandardDeviationFactor), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Standard Deviation Factor", "Factor for dynamic zones calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

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
			base.OnStarted(time);

			// Initialize previous values
			_prevStochK = 50;

			// Create indicators
			var stochastic = new StochasticOscillator
			{
				K = StochKPeriod,
				D = StochDPeriod,
				Length = StochPeriod
			};
			
			var stochSma = new SimpleMovingAverage { Length = LookbackPeriod };
			var stochStdDev = new StandardDeviation { Length = LookbackPeriod };

			// Subscribe to candles and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.BindEx(stochastic, (candle, stochValue) =>
				{
					// Extract %K value
					var stochK = stochValue.ToDecimal();
					
					// Calculate dynamic zones
					var stochKInput = new DecimalIndicatorValue(stochK);
					var stochKAvg = stochSma.Process(stochKInput).ToDecimal();
					var stochKStdDev = stochStdDev.Process(stochKInput).ToDecimal();
					
					var dynamicOversold = stochKAvg - (StandardDeviationFactor * stochKStdDev);
					var dynamicOverbought = stochKAvg + (StandardDeviationFactor * stochKStdDev);
					
					// Process the strategy logic
					ProcessStrategy(candle, stochK, dynamicOversold, dynamicOverbought);
				})
				.Start();

			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, stochastic);
				DrawOwnTrades(area);
			}

			// Setup position protection
			StartProtection(
				takeProfit: new Unit(2, UnitTypes.Percent),
				stopLoss: new Unit(1, UnitTypes.Percent)
			);
		}

		private void ProcessStrategy(ICandleMessage candle, decimal stochK, decimal dynamicOversold, decimal dynamicOverbought)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Check if Stochastic is reversing
			var isReversingUp = stochK > _prevStochK;
			var isReversingDown = stochK < _prevStochK;
			
			// Check if Stochastic is in oversold/overbought zones
			var isOversold = stochK < dynamicOversold;
			var isOverbought = stochK > dynamicOverbought;
			
			// Trading logic
			if (isOversold && isReversingUp && Position <= 0)
			{
				// Oversold condition with upward reversal - Buy signal
				CancelActiveOrders();
				
				// Calculate position size
				var volume = Volume + Math.Abs(Position);
				
				// Enter long position
				BuyMarket(volume);
			}
			else if (isOverbought && isReversingDown && Position >= 0)
			{
				// Overbought condition with downward reversal - Sell signal
				CancelActiveOrders();
				
				// Calculate position size
				var volume = Volume + Math.Abs(Position);
				
				// Enter short position
				SellMarket(volume);
			}
			
			// Exit logic - when Stochastic crosses the middle line (50)
			if ((Position > 0 && stochK > 50) || (Position < 0 && stochK < 50))
			{
				// Close position
				ClosePosition();
			}

			// Update previous Stochastic value
			_prevStochK = stochK;
		}
	}
}
