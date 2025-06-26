using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Stochastic Mean Reversion Strategy.
	/// Enter when Stochastic %K deviates from its average by a certain multiple of standard deviation.
	/// Exit when Stochastic %K returns to its average.
	/// </summary>
	public class StochasticMeanReversionStrategy : Strategy
	{
		private readonly StrategyParam<int> _stochPeriod;
		private readonly StrategyParam<int> _kPeriod;
		private readonly StrategyParam<int> _dPeriod;
		private readonly StrategyParam<int> _averagePeriod;
		private readonly StrategyParam<decimal> _multiplier;
		private readonly StrategyParam<DataType> _candleType;

		private StochasticOscillator _stochastic;
		private SimpleMovingAverage _stochAverage;
		private StandardDeviation _stochStdDev;
		
		private decimal _prevStochKValue;

		/// <summary>
		/// Stochastic period.
		/// </summary>
		public int StochPeriod
		{
			get => _stochPeriod.Value;
			set => _stochPeriod.Value = value;
		}

		/// <summary>
		/// Stochastic %K period.
		/// </summary>
		public int KPeriod
		{
			get => _kPeriod.Value;
			set => _kPeriod.Value = value;
		}

		/// <summary>
		/// Stochastic %D period.
		/// </summary>
		public int DPeriod
		{
			get => _dPeriod.Value;
			set => _dPeriod.Value = value;
		}

		/// <summary>
		/// Period for Stochastic average calculation.
		/// </summary>
		public int AveragePeriod
		{
			get => _averagePeriod.Value;
			set => _averagePeriod.Value = value;
		}

		/// <summary>
		/// Standard deviation multiplier for entry.
		/// </summary>
		public decimal Multiplier
		{
			get => _multiplier.Value;
			set => _multiplier.Value = value;
		}

		/// <summary>
		/// Type of candles to use.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StochasticMeanReversionStrategy"/>.
		/// </summary>
		public StochasticMeanReversionStrategy()
		{
			_stochPeriod = Param(nameof(StochPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("Stochastic Period", "Period for Stochastic calculation", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 20, 2);

			_kPeriod = Param(nameof(KPeriod), 3)
				.SetGreaterThanZero()
				.SetDisplay("K Period", "Period for %K calculation", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(2, 5, 1);

			_dPeriod = Param(nameof(DPeriod), 3)
				.SetGreaterThanZero()
				.SetDisplay("D Period", "Period for %D calculation", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(2, 5, 1);

			_averagePeriod = Param(nameof(AveragePeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Average Period", "Period for Stochastic average calculation", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_multiplier = Param(nameof(Multiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("StdDev Multiplier", "Standard deviation multiplier for entry", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "Strategy Parameters");
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

			// Create indicators
			_stochastic = new() 
			{
				K = { Length = KPeriod },
				D = { Length = DPeriod }
			};
			
			_stochAverage = new SimpleMovingAverage { Length = AveragePeriod };
			_stochStdDev = new StandardDeviation { Length = AveragePeriod };

			// Create candle subscription
			var subscription = SubscribeCandles(CandleType);

			// Bind stochastic to candles
			subscription
				.BindEx(_stochastic, ProcessStochastic)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _stochastic);
				DrawOwnTrades(area);
			}

			// Enable position protection
			StartProtection(
				takeProfit: new Unit(5, UnitTypes.Percent),
				stopLoss: new Unit(2, UnitTypes.Percent)
			);
		}

		private void ProcessStochastic(ICandleMessage candle, IIndicatorValue stochValue)
		{
			if (candle.State != CandleStates.Finished)
				return;

			// Extract %K value from stochastic
			var stochObject = stochValue.GetValue<IIndicatorValue[]>();
			if (stochObject == null || stochObject.Length < 2)
				return;

			decimal stochKValue = stochObject[0].ToDecimal();
			
			// Process Stochastic %K through average and standard deviation indicators
			var stochAvgValue = _stochAverage.Process(stochKValue, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal();
			var stochStdDevValue = _stochStdDev.Process(stochKValue, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal();
			
			// Store previous Stochastic %K value for changes detection
			decimal currentStochKValue = stochKValue;
			
			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading() || !_stochAverage.IsFormed || !_stochStdDev.IsFormed)
			{
				_prevStochKValue = currentStochKValue;
				return;
			}

			// Calculate bands
			var upperBand = stochAvgValue + Multiplier * stochStdDevValue;
			var lowerBand = stochAvgValue - Multiplier * stochStdDevValue;

			LogInfo($"Stoch %K: {currentStochKValue}, Avg: {stochAvgValue}, Upper: {upperBand}, Lower: {lowerBand}");

			// Entry logic
			if (Position == 0)
			{
				// Long Entry: Stochastic %K is below lower band
				if (currentStochKValue < lowerBand)
				{
					LogInfo($"Buy Signal - Stoch %K ({currentStochKValue}) < Lower Band ({lowerBand})");
					BuyMarket(Volume);
				}
				// Short Entry: Stochastic %K is above upper band
				else if (currentStochKValue > upperBand)
				{
					LogInfo($"Sell Signal - Stoch %K ({currentStochKValue}) > Upper Band ({upperBand})");
					SellMarket(Volume);
				}
			}
			// Exit logic
			else if (Position > 0 && currentStochKValue > stochAvgValue)
			{
				// Exit Long: Stochastic %K returned to average
				LogInfo($"Exit Long - Stoch %K ({currentStochKValue}) > Avg ({stochAvgValue})");
				SellMarket(Math.Abs(Position));
			}
			else if (Position < 0 && currentStochKValue < stochAvgValue)
			{
				// Exit Short: Stochastic %K returned to average
				LogInfo($"Exit Short - Stoch %K ({currentStochKValue}) < Avg ({stochAvgValue})");
				BuyMarket(Math.Abs(Position));
			}
			
			_prevStochKValue = currentStochKValue;
		}
	}
}
