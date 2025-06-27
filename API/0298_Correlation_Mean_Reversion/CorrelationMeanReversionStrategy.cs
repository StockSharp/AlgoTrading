using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Strategies
{
	/// <summary>
	/// Correlation Mean Reversion strategy.
	/// Trades based on changes in correlation between two securities.
	/// </summary>
	public class CorrelationMeanReversionStrategy : Strategy
	{
		private readonly StrategyParam<Security> _security1;
		private readonly StrategyParam<Security> _security2;
		private readonly StrategyParam<int> _correlationPeriod;
		private readonly StrategyParam<int> _lookbackPeriod;
		private readonly StrategyParam<decimal> _deviationThreshold;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;
		
		// Indicators and data containers
		private SimpleMovingAverage _correlationSma;
		private StandardDeviation _correlationStdDev;
		
		private readonly Queue<decimal> _security1Prices = [];
		private readonly Queue<decimal> _security2Prices = [];
		
		// Current values
		private decimal _currentCorrelation;
		private decimal _averageCorrelation;
		private decimal _correlationStdDeviation;
		private decimal _security1LastPrice;
		private decimal _security2LastPrice;
		private bool _security1Updated;
		private bool _security2Updated;
		
		/// <summary>
		/// First security for correlation calculation.
		/// </summary>
		public Security Security1
		{
			get => _security1.Value;
			set => _security1.Value = value;
		}
		
		/// <summary>
		/// Second security for correlation calculation.
		/// </summary>
		public Security Security2
		{
			get => _security2.Value;
			set => _security2.Value = value;
		}
		
		/// <summary>
		/// Period for correlation calculation.
		/// </summary>
		public int CorrelationPeriod
		{
			get => _correlationPeriod.Value;
			set => _correlationPeriod.Value = value;
		}
		
		/// <summary>
		/// Lookback period for moving average and standard deviation calculation.
		/// </summary>
		public int LookbackPeriod
		{
			get => _lookbackPeriod.Value;
			set => _lookbackPeriod.Value = value;
		}
		
		/// <summary>
		/// Threshold in standard deviations for entry signals.
		/// </summary>
		public decimal DeviationThreshold
		{
			get => _deviationThreshold.Value;
			set => _deviationThreshold.Value = value;
		}
		
		/// <summary>
		/// Stop loss percentage from entry price.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}
		
		/// <summary>
		/// Candle timeframe type for data subscription.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}
		
		/// <summary>
		/// Constructor.
		/// </summary>
		public CorrelationMeanReversionStrategy()
		{
			_security1 = Param<Security>(nameof(Security1))
				.SetDisplay("First Security", "First security for correlation calculation", "Securities");
				
			_security2 = Param<Security>(nameof(Security2))
				.SetDisplay("Second Security", "Second security for correlation calculation", "Securities");
				
			_correlationPeriod = Param(nameof(CorrelationPeriod), 20)
				.SetRange(10, 100)
				.SetDisplay("Correlation Period", "Period for correlation calculation", "Parameters")
				.SetCanOptimize(true);
				
			_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
				.SetRange(10, 100)
				.SetDisplay("Lookback Period", "Period for moving average and standard deviation calculation", "Parameters")
				.SetCanOptimize(true);
				
			_deviationThreshold = Param(nameof(DeviationThreshold), 2.0m)
				.SetRange(1.0m, 3.0m)
				.SetDisplay("Deviation Threshold", "Threshold in standard deviations for entry signals", "Parameters")
				.SetCanOptimize(true);
				
			_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
				.SetRange(0.5m, 5.0m)
				.SetDisplay("Stop Loss", "Stop loss percentage from entry price", "Parameters")
				.SetCanOptimize(true);
				
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Timeframe for candles", "Parameters");
		}
		
		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			if (Security1 != null && Security2 != null)
			{
				yield return (Security1, CandleType);
				yield return (Security2, CandleType);
			}
		}
		
		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);
			
			if (Security1 == null)
				throw new InvalidOperationException("First security is not specified.");
				
			if (Security2 == null)
				throw new InvalidOperationException("Second security is not specified.");
			
			// Initialize indicators
			_correlationSma = new SimpleMovingAverage { Length = LookbackPeriod };
			_correlationStdDev = new StandardDeviation { Length = LookbackPeriod };
			
			// Subscribe to candles for both securities
			var subscription1 = SubscribeCandles(CandleType, false, Security1);
			var subscription2 = SubscribeCandles(CandleType, false, Security2);
			
			// Process data
			subscription1
				.Bind(ProcessSecurity1Candle)
				.Start();
				
			subscription2
				.Bind(ProcessSecurity2Candle)
				.Start();
			
			// Setup visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription1);
				DrawCandles(area, subscription2);
				DrawOwnTrades(area);
			}
			
			// Setup position protection
			StartProtection(
				new Unit(0, UnitTypes.Absolute), // No take profit
				new Unit(StopLossPercent, UnitTypes.Percent), // Stop loss in percent
				false // No trailing stop
			);
		}
		
		private void ProcessSecurity1Candle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;
			
			// Store the last price
			_security1LastPrice = candle.ClosePrice;
			_security1Updated = true;
			
			// Update correlation and check for signals
			CalculateCorrelation(candle.ServerTime, candle.State == CandleStates.Finished);
		}
		
		private void ProcessSecurity2Candle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;
			
			// Store the last price
			_security2LastPrice = candle.ClosePrice;
			_security2Updated = true;
			
			// Update correlation and check for signals
			CalculateCorrelation(candle.ServerTime, candle.State == CandleStates.Finished);
		}
		
		private void CalculateCorrelation(DateTimeOffset time, bool isFinal)
		{
			// Only proceed if both securities have been updated
			if (!_security1Updated || !_security2Updated)
				return;
			
			// Reset flags
			_security1Updated = false;
			_security2Updated = false;
			
			// Add the latest prices to queues
			_security1Prices.Enqueue(_security1LastPrice);
			_security2Prices.Enqueue(_security2LastPrice);
			
			// Keep queue sizes limited to correlation period
			while (_security1Prices.Count > CorrelationPeriod)
			{
				_security1Prices.Dequeue();
				_security2Prices.Dequeue();
			}
			
			// Need sufficient data to calculate correlation
			if (_security1Prices.Count < CorrelationPeriod)
				return;
			
			// Calculate correlation coefficient
			_currentCorrelation = CalculateCorrelationCoefficient(_security1Prices.ToArray(), _security2Prices.ToArray());
			
			// Process indicators
			_averageCorrelation = _correlationSma.Process(_currentCorrelation, time, isFinal).ToDecimal();
			_correlationStdDeviation = _correlationStdDev.Process(_currentCorrelation, time, isFinal).ToDecimal();
			
			// Check for trading signals
			CheckSignal();
		}
		
		private decimal CalculateCorrelationCoefficient(decimal[] series1, decimal[] series2)
		{
			// Need at least two points for correlation
			if (series1.Length < 2 || series1.Length != series2.Length)
				return 0;
			
			// Calculate means
			decimal mean1 = series1.Average();
			decimal mean2 = series2.Average();
			
			decimal sum1 = 0;
			decimal sum2 = 0;
			decimal sum12 = 0;
			
			// Calculate correlation
			for (int i = 0; i < series1.Length; i++)
			{
				decimal diff1 = series1[i] - mean1;
				decimal diff2 = series2[i] - mean2;
				
				sum1 += diff1 * diff1;
				sum2 += diff2 * diff2;
				sum12 += diff1 * diff2;
			}
			
			// Avoid division by zero
			if (sum1 == 0 || sum2 == 0)
				return 0;
			
			return sum12 / (decimal)Math.Sqrt((double)(sum1 * sum2));
		}
		
		private void CheckSignal()
		{
			// Ensure strategy is ready for trading and indicators are formed
			if (!IsFormedAndOnlineAndAllowTrading() || 
				!_correlationSma.IsFormed || 
				!_correlationStdDev.IsFormed)
				return;
			
			// Calculate Z-score for correlation
			var correlationZScore = (_currentCorrelation - _averageCorrelation) / _correlationStdDeviation;
			
			// If we have no position, check for entry signals
			if (Position == 0)
			{
				// Correlation drops below average (securities are less correlated than normal)
				if (correlationZScore < -DeviationThreshold)
				{
					// Long Security1, Short Security2
					BuyMarket(Volume, Security1);
					SellMarket(Volume, Security2);
					
					LogInfo($"LONG {Security1.Code}, SHORT {Security2.Code}: Correlation Z-Score: {correlationZScore:F2}");
				}
				// Correlation rises above average (securities are more correlated than normal)
				else if (correlationZScore > DeviationThreshold)
				{
					// Short Security1, Long Security2
					SellMarket(Volume, Security1);
					BuyMarket(Volume, Security2);
					
					LogInfo($"SHORT {Security1.Code}, LONG {Security2.Code}: Correlation Z-Score: {correlationZScore:F2}");
				}
			}
			// Check for exit signals
			else
			{
				// Exit when correlation returns to average
				if ((Position > 0 && correlationZScore >= 0) || 
					(Position < 0 && correlationZScore <= 0))
				{
					ClosePosition(Security1);
					ClosePosition(Security2);
					
					LogInfo($"CLOSE PAIR: Correlation Z-Score: {correlationZScore:F2}");
				}
			}
		}
	}
}
