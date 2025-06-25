using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Momentum Breakout Strategy (245).
	/// Enter when momentum breaks out above/below its average by a certain multiple of standard deviation.
	/// Exit when momentum returns to its average.
	/// </summary>
	public class MomentumBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _momentumPeriod;
		private readonly StrategyParam<int> _averagePeriod;
		private readonly StrategyParam<decimal> _multiplier;
		private readonly StrategyParam<DataType> _candleType;

		private Momentum _momentum;
		private SimpleMovingAverage _momentumAverage;
		private StandardDeviation _momentumStdDev;
		
		private decimal? _currentMomentum;
		private decimal? _momentumAvgValue;
		private decimal? _momentumStdDevValue;

		/// <summary>
		/// Momentum period.
		/// </summary>
		public int MomentumPeriod
		{
			get => _momentumPeriod.Value;
			set => _momentumPeriod.Value = value;
		}

		/// <summary>
		/// Period for momentum average calculation.
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
		/// Initializes a new instance of the <see cref="MomentumBreakoutStrategy"/>.
		/// </summary>
		public MomentumBreakoutStrategy()
		{
			_momentumPeriod = Param(nameof(MomentumPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("Momentum Period", "Period for momentum calculation", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 20, 2);

			_averagePeriod = Param(nameof(AveragePeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Average Period", "Period for momentum average calculation", "Strategy Parameters")
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
			_momentum = new Momentum { Length = MomentumPeriod };
			_momentumAverage = new SimpleMovingAverage { Length = AveragePeriod };
			_momentumStdDev = new StandardDeviation { Length = AveragePeriod };

			// Create candle subscription
			var subscription = SubscribeCandles(CandleType);

			// Create processing chain
			subscription
				.Bind(_momentum, ProcessMomentum)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _momentum);
				DrawOwnTrades(area);
			}

			// Enable position protection
			StartProtection(
				takeProfit: new Unit(5, UnitTypes.Percent),
				stopLoss: new Unit(2, UnitTypes.Percent)
			);
		}

		private void ProcessMomentum(ICandleMessage candle, decimal momentumValue)
		{
			if (candle.State != CandleStates.Finished)
				return;

			// Store the current momentum value
			_currentMomentum = momentumValue;

			// Process momentum through average and standard deviation indicators
			var avgIndicatorValue = _momentumAverage.Process(momentumValue, candle.ServerTime, candle.State == CandleStates.Finished);
			var stdDevIndicatorValue = _momentumStdDev.Process(momentumValue, candle.ServerTime, candle.State == CandleStates.Finished);
			
			_momentumAvgValue = avgIndicatorValue.GetValue<decimal>();
			_momentumStdDevValue = stdDevIndicatorValue.GetValue<decimal>();
			
			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading() || !_momentumAverage.IsFormed || !_momentumStdDev.IsFormed)
				return;

			// Ensure we have all needed values
			if (!_currentMomentum.HasValue || !_momentumAvgValue.HasValue || !_momentumStdDevValue.HasValue)
				return;

			// Calculate bands
			var upperBand = _momentumAvgValue.Value + Multiplier * _momentumStdDevValue.Value;
			var lowerBand = _momentumAvgValue.Value - Multiplier * _momentumStdDevValue.Value;

			LogInfo($"Momentum: {_currentMomentum}, Avg: {_momentumAvgValue}, Upper: {upperBand}, Lower: {lowerBand}");

			// Entry logic - BREAKOUT (not mean reversion)
			if (Position == 0)
			{
				// Long Entry: Momentum breaks above upper band (strong upward momentum)
				if (_currentMomentum.Value > upperBand)
				{
					LogInfo($"Buy Signal - Momentum ({_currentMomentum}) > Upper Band ({upperBand})");
					BuyMarket(Volume);
				}
				// Short Entry: Momentum breaks below lower band (strong downward momentum)
				else if (_currentMomentum.Value < lowerBand)
				{
					LogInfo($"Sell Signal - Momentum ({_currentMomentum}) < Lower Band ({lowerBand})");
					SellMarket(Volume);
				}
			}
			// Exit logic
			else if (Position > 0 && _currentMomentum.Value < _momentumAvgValue.Value)
			{
				// Exit Long: Momentum returned to average
				LogInfo($"Exit Long - Momentum ({_currentMomentum}) < Avg ({_momentumAvgValue})");
				SellMarket(Math.Abs(Position));
			}
			else if (Position < 0 && _currentMomentum.Value > _momentumAvgValue.Value)
			{
				// Exit Short: Momentum returned to average
				LogInfo($"Exit Short - Momentum ({_currentMomentum}) > Avg ({_momentumAvgValue})");
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}
