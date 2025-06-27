using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on MACD with adaptive histogram threshold.
	/// </summary>
	public class MacdAdaptiveHistogramStrategy : Strategy
	{
		private readonly StrategyParam<int> _fastPeriod;
		private readonly StrategyParam<int> _slowPeriod;
		private readonly StrategyParam<int> _signalPeriod;
		private readonly StrategyParam<int> _histogramAvgPeriod;
		private readonly StrategyParam<decimal> _stdDevMultiplier;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// Fast EMA period for MACD.
		/// </summary>
		public int FastPeriod
		{
			get => _fastPeriod.Value;
			set => _fastPeriod.Value = value;
		}

		/// <summary>
		/// Slow EMA period for MACD.
		/// </summary>
		public int SlowPeriod
		{
			get => _slowPeriod.Value;
			set => _slowPeriod.Value = value;
		}

		/// <summary>
		/// Signal line period for MACD.
		/// </summary>
		public int SignalPeriod
		{
			get => _signalPeriod.Value;
			set => _signalPeriod.Value = value;
		}

		/// <summary>
		/// Period for histogram average and standard deviation calculation.
		/// </summary>
		public int HistogramAvgPeriod
		{
			get => _histogramAvgPeriod.Value;
			set => _histogramAvgPeriod.Value = value;
		}

		/// <summary>
		/// Standard deviation multiplier for histogram threshold.
		/// </summary>
		public decimal StdDevMultiplier
		{
			get => _stdDevMultiplier.Value;
			set => _stdDevMultiplier.Value = value;
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
		/// Candle type parameter.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initialize a new instance of <see cref="MacdAdaptiveHistogramStrategy"/>.
		/// </summary>
		public MacdAdaptiveHistogramStrategy()
		{
			_fastPeriod = this.Param(nameof(FastPeriod), 12)
				.SetGreaterThanZero()
				.SetDisplay("Fast Period", "Fast EMA period for MACD", "MACD Settings")
				.SetCanOptimize(true)
				.SetOptimize(8, 16, 2);

			_slowPeriod = this.Param(nameof(SlowPeriod), 26)
				.SetGreaterThanZero()
				.SetDisplay("Slow Period", "Slow EMA period for MACD", "MACD Settings")
				.SetCanOptimize(true)
				.SetOptimize(20, 32, 3);

			_signalPeriod = this.Param(nameof(SignalPeriod), 9)
				.SetGreaterThanZero()
				.SetDisplay("Signal Period", "Signal line period for MACD", "MACD Settings")
				.SetCanOptimize(true)
				.SetOptimize(7, 12, 1);

			_histogramAvgPeriod = this.Param(nameof(HistogramAvgPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Histogram Avg Period", "Period for histogram average calculation", "Strategy Settings")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_stdDevMultiplier = this.Param(nameof(StdDevMultiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("StdDev Multiplier", "Standard deviation multiplier for histogram threshold", "Strategy Settings")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_stopLossPercent = this.Param(nameof(StopLossPercent), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss %", "Stop loss percentage", "Strategy Settings")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_candleType = this.Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles for strategy", "General");
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

			// Create MACD indicator with custom settings
			var macdLine = new MovingAverageConvergenceDivergenceSignal
			{
				Macd =
				{
					ShortMa = { Length = FastPeriod },
					LongMa = { Length = SlowPeriod },
				},
				SignalMa = { Length = SignalPeriod }
			};
			
			// Create indicators for the histogram statistics
			var histAvg = new SimpleMovingAverage { Length = HistogramAvgPeriod };
			var histStdDev = new StandardDeviation { Length = HistogramAvgPeriod };

			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind MACD to subscription
			subscription
				.Bind(macdLine, ProcessCandle)
				.Start();
				
			// Create a special subscription for histogram statistics
			var histogramSubscription = subscription.CopySubscription();
			
			histogramSubscription
				.BindEx(macdLine, macdValue => {
					// Extract the histogram value (MACD - Signal)
					var macd = macdValue.GetValue<Tuple<decimal, decimal, decimal>>().Item1;
					var signal = macdValue.GetValue<Tuple<decimal, decimal, decimal>>().Item2;
					var histogram = macd - signal;
					
					// Process the histogram through the statistics indicators
					var decimalValue = new DecimalIndicatorValue(histogram);
					histAvg.Process(decimalValue);
					histStdDev.Process(decimalValue);
				})
				.Start();

			// Enable position protection with percentage stop-loss
			StartProtection(
				takeProfit: new Unit(0), // We'll handle exits in the strategy logic
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
				useMarketOrders: true
			);

			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, macdLine);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, Tuple<decimal, decimal, decimal> macdValues)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
				
			// Extract MACD values
			var macd = macdValues.Item1;
			var signal = macdValues.Item2;
			var histogram = macd - signal; // Not using Item3 as it might not be available depending on MACD implementation
			
			// Get values from indicators
			var histAvgValue = 0m;
			var histStdDevValue = 0.001m; // Default small value to avoid division by zero
			
			// Get the indicator containers
			var histAvgContainer = Indicators.TryGetByName("HistAvg");
			var histStdDevContainer = Indicators.TryGetByName("HistStdDev");
			
			if (histAvgContainer != null && histAvgContainer.IsFormed)
				histAvgValue = histAvgContainer.GetCurrentValue();
				
			if (histStdDevContainer != null && histStdDevContainer.IsFormed)
				histStdDevValue = histStdDevContainer.GetCurrentValue();
			
			// Calculate adaptive thresholds for histogram
			var upperThreshold = histAvgValue + StdDevMultiplier * histStdDevValue;
			var lowerThreshold = histAvgValue - StdDevMultiplier * histStdDevValue;
			
			// Define entry conditions with adaptive thresholds
			var longEntryCondition = histogram > upperThreshold && Position <= 0;
			var shortEntryCondition = histogram < lowerThreshold && Position >= 0;
			
			// Define exit conditions
			var longExitCondition = histogram < 0 && Position > 0;
			var shortExitCondition = histogram > 0 && Position < 0;

			// Log current values
			LogInfo($"Candle: {candle.OpenTime}, Close: {candle.ClosePrice}, MACD: {macd}, Signal: {signal}, Histogram: {histogram}");
			LogInfo($"Hist Avg: {histAvgValue}, Hist StdDev: {histStdDevValue}, Upper: {upperThreshold}, Lower: {lowerThreshold}");

			// Execute trading logic
			if (longEntryCondition)
			{
				// Calculate position size
				var positionSize = Volume + Math.Abs(Position);
				
				// Enter long position
				BuyMarket(positionSize);
				
				LogInfo($"Long entry: Price={candle.ClosePrice}, Histogram={histogram}, Threshold={upperThreshold}");
			}
			else if (shortEntryCondition)
			{
				// Calculate position size
				var positionSize = Volume + Math.Abs(Position);
				
				// Enter short position
				SellMarket(positionSize);
				
				LogInfo($"Short entry: Price={candle.ClosePrice}, Histogram={histogram}, Threshold={lowerThreshold}");
			}
			else if (longExitCondition)
			{
				// Exit long position
				SellMarket(Math.Abs(Position));
				LogInfo($"Long exit: Price={candle.ClosePrice}, Histogram={histogram}");
			}
			else if (shortExitCondition)
			{
				// Exit short position
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short exit: Price={candle.ClosePrice}, Histogram={histogram}");
			}
		}
	}
}