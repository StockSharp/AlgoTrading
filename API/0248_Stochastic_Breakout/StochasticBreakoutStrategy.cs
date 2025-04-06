using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Stochastic Breakout Strategy.
	/// This strategy identifies breakouts based on the Stochastic oscillator values compared to their historical average.
	/// </summary>
	public class StochasticBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _stochasticPeriod;
		private readonly StrategyParam<int> _kPeriod;
		private readonly StrategyParam<int> _dPeriod;
		private readonly StrategyParam<int> _lookbackPeriod;
		private readonly StrategyParam<decimal> _deviationMultiplier;
		private readonly StrategyParam<DataType> _candleType;
		
		private StochasticOscillator _stochastic;
		private SimpleMovingAverage _stochAverage;
		private StandardDeviation _stochStdDev;
		
		private decimal _prevStochValue;
		private decimal _prevStochAverage;
		private decimal _prevStochStdDev;

		/// <summary>
		/// Stochastic oscillator period.
		/// </summary>
		public int StochasticPeriod
		{
			get => _stochasticPeriod.Value;
			set => _stochasticPeriod.Value = value;
		}

		/// <summary>
		/// Stochastic %K smoothing period.
		/// </summary>
		public int KPeriod
		{
			get => _kPeriod.Value;
			set => _kPeriod.Value = value;
		}

		/// <summary>
		/// Stochastic %D smoothing period.
		/// </summary>
		public int DPeriod
		{
			get => _dPeriod.Value;
			set => _dPeriod.Value = value;
		}

		/// <summary>
		/// Lookback period for calculating the average and standard deviation.
		/// </summary>
		public int LookbackPeriod
		{
			get => _lookbackPeriod.Value;
			set => _lookbackPeriod.Value = value;
		}

		/// <summary>
		/// Deviation multiplier for breakout detection.
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
		public StochasticBreakoutStrategy()
		{
			_stochasticPeriod = Param(nameof(StochasticPeriod), 14)
				.SetDisplayName("Stochastic Period")
				.SetCategory("Stochastic")
				.SetCanOptimize(true)
				.SetOptimize(5, 30, 5);

			_kPeriod = Param(nameof(KPeriod), 3)
				.SetDisplayName("K Period")
				.SetCategory("Stochastic")
				.SetCanOptimize(true)
				.SetOptimize(1, 5, 1);

			_dPeriod = Param(nameof(DPeriod), 3)
				.SetDisplayName("D Period")
				.SetCategory("Stochastic")
				.SetCanOptimize(true)
				.SetOptimize(1, 5, 1);

			_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
				.SetDisplayName("Lookback Period")
				.SetCategory("Breakout")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_deviationMultiplier = Param(nameof(DeviationMultiplier), 2.0m)
				.SetDisplayName("Deviation Multiplier")
				.SetCategory("Breakout")
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
			_stochastic = new StochasticOscillator
			{
				KPeriod = StochasticPeriod,
				KSmoothing = KPeriod,
				DPeriod = DPeriod
			};

			_stochAverage = new SimpleMovingAverage { Length = LookbackPeriod };
			_stochStdDev = new StandardDeviation { Length = LookbackPeriod };
			
			// Reset stored values
			_prevStochValue = 0;
			_prevStochAverage = 0;
			_prevStochStdDev = 0;

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
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
			
			// Start position protection
			StartProtection(
				takeProfit: new Unit(2, UnitTypes.Percent),
				stopLoss: new Unit(2, UnitTypes.Percent)
			);
		}

		private void ProcessStochastic(ICandleMessage candle, IIndicatorValue value)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Get stochastic value (K line)
			var stochValue = value.GetValue<decimal>();
			
			// Calculate average and standard deviation of stochastic
			var stochAvgValue = _stochAverage.Process(new DecimalIndicatorValue(stochValue)).GetValue<decimal>();
			var tempStdDevValue = _stochStdDev.Process(new DecimalIndicatorValue(stochValue)).GetValue<decimal>();
			
			// First values initialization - skip trading decision
			if (_prevStochValue == 0)
			{
				_prevStochValue = stochValue;
				_prevStochAverage = stochAvgValue;
				_prevStochStdDev = tempStdDevValue;
				return;
			}
			
			// Calculate breakout thresholds
			var upperThreshold = _prevStochAverage + _prevStochStdDev * DeviationMultiplier;
			var lowerThreshold = _prevStochAverage - _prevStochStdDev * DeviationMultiplier;
			
			// Trading logic:
			// Buy when stochastic breaks above upper threshold
			if (stochValue > upperThreshold && _prevStochValue <= upperThreshold && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				this.AddInfoLog($"Stochastic breakout UP: {stochValue} > {upperThreshold}. Buying at {candle.ClosePrice}");
			}
			// Sell when stochastic breaks below lower threshold
			else if (stochValue < lowerThreshold && _prevStochValue >= lowerThreshold && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				this.AddInfoLog($"Stochastic breakout DOWN: {stochValue} < {lowerThreshold}. Selling at {candle.ClosePrice}");
			}
			
			// Exit positions when stochastic returns to average
			else if (Position > 0 && stochValue < _prevStochAverage && _prevStochValue >= _prevStochAverage)
			{
				SellMarket(Math.Abs(Position));
				this.AddInfoLog($"Stochastic returned to average: {stochValue} < {_prevStochAverage}. Closing long position at {candle.ClosePrice}");
			}
			else if (Position < 0 && stochValue > _prevStochAverage && _prevStochValue <= _prevStochAverage)
			{
				BuyMarket(Math.Abs(Position));
				this.AddInfoLog($"Stochastic returned to average: {stochValue} > {_prevStochAverage}. Closing short position at {candle.ClosePrice}");
			}
			
			// Store current values for next comparison
			_prevStochValue = stochValue;
			_prevStochAverage = stochAvgValue;
			_prevStochStdDev = tempStdDevValue;
		}
	}
}