using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Strategies
{
	/// <summary>
	/// Pairs Trading Strategy with Volatility Filter strategy.
	/// </summary>
	public class PairsTradingVolatilityFilterStrategy : Strategy
	{
		private readonly StrategyParam<Security> _security1;
		private readonly StrategyParam<Security> _security2;
		private readonly StrategyParam<int> _lookbackPeriod;
		private readonly StrategyParam<decimal> _entryThreshold;
		private readonly StrategyParam<decimal> _exitThreshold;
		private readonly StrategyParam<decimal> _stopLossPercent;
		
		private decimal _currentSpread;
		private decimal _previousSpread;
		private decimal _averageSpread;
		private decimal _standardDeviation;
		private decimal _currentAtr;
		private decimal _averageAtr;
		
		private decimal _volumeRatio = 1; // Default 1:1 ratio
		private decimal _entryPrice;
		
		// Indicators
		private AverageTrueRange _atr;
		private StandardDeviation _stdDev;
		private SimpleMovingAverage _spreadSma;
		private SimpleMovingAverage _atrSma;
		
		/// <summary>
		/// First security in the pair.
		/// </summary>
		public Security Security1
		{
			get => _security1.Value;
			set => _security1.Value = value;
		}
		
		/// <summary>
		/// Second security in the pair.
		/// </summary>
		public Security Security2
		{
			get => _security2.Value;
			set => _security2.Value = value;
		}
		
		/// <summary>
		/// Lookback period for moving averages and standard deviation.
		/// </summary>
		public int LookbackPeriod
		{
			get => _lookbackPeriod.Value;
			set => _lookbackPeriod.Value = value;
		}
		
		/// <summary>
		/// Entry threshold in standard deviations.
		/// </summary>
		public decimal EntryThreshold
		{
			get => _entryThreshold.Value;
			set => _entryThreshold.Value = value;
		}
		
		/// <summary>
		/// Exit threshold in standard deviations.
		/// </summary>
		public decimal ExitThreshold
		{
			get => _exitThreshold.Value;
			set => _exitThreshold.Value = value;
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
		/// Constructor.
		/// </summary>
		public PairsTradingVolatilityFilterStrategy()
		{
			_security1 = Param<Security>(nameof(Security1))
				.SetDisplay("First Security", "First security of the pair", "Parameters");
				
			_security2 = Param<Security>(nameof(Security2))
				.SetDisplay("Second Security", "Second security of the pair", "Parameters");
				
			_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
				.SetRange(5, 100)
				.SetDisplay("Lookback Period", "Lookback period for moving averages and standard deviation", "Parameters")
				.SetCanOptimize(true);
				
			_entryThreshold = Param(nameof(EntryThreshold), 2.0m)
				.SetRange(1.0m, 5.0m)
				.SetDisplay("Entry Threshold", "Entry threshold in standard deviations", "Parameters")
				.SetCanOptimize(true);
				
			_exitThreshold = Param(nameof(ExitThreshold), 0.0m)
				.SetRange(0.0m, 1.0m)
				.SetDisplay("Exit Threshold", "Exit threshold in standard deviations", "Parameters")
				.SetCanOptimize(true);
				
			_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
				.SetRange(0.5m, 5.0m)
				.SetDisplay("Stop Loss", "Stop loss percentage from entry price", "Parameters")
				.SetCanOptimize(true);
		}
		
		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			if (Security1 != null && Security2 != null)
			{
				yield return (Security1, DataType.TimeFrame(TimeSpan.FromMinutes(5)));
				yield return (Security2, DataType.TimeFrame(TimeSpan.FromMinutes(5)));
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
			_atr = new AverageTrueRange { Length = LookbackPeriod };
			_spreadSma = new SimpleMovingAverage { Length = LookbackPeriod };
			_atrSma = new SimpleMovingAverage { Length = LookbackPeriod };
			_stdDev = new StandardDeviation { Length = LookbackPeriod };
			
			// Set volume ratio to normalize pair
			_volumeRatio = CalculateVolumeRatio();
			
			// Subscribe to both securities' candles
			var subscription1 = SubscribeCandles(TimeSpan.FromMinutes(5), false, Security1);
			var subscription2 = SubscribeCandles(TimeSpan.FromMinutes(5), false, Security2);
			
			// Process data and calculate spread
			subscription1
				.Bind(_atr, ProcessSecurity1Candle)
				.Start();
				
			subscription2
				.Do(ProcessSecurity2Candle)
				.Start();
			
			// Setup visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription1);
				DrawCandles(area, subscription2);
			}
			
			// Setup position protection
			StartProtection(
				new Unit(0, UnitTypes.Absolute), // No take profit
				new Unit(StopLossPercent, UnitTypes.Percent), // Stop loss in percent
				false // No trailing stop
			);
		}
		
		private decimal CalculateVolumeRatio()
		{
			// Simple ratio based on current prices
			// In a more advanced implementation, this could use beta or cointegration coefficients
			if (Security1 == null || Security2 == null)
				return 1;
				
			var price1 = Security1.GetCurrentPrice();
			var price2 = Security2.GetCurrentPrice();
			
			if (price1 == 0 || price2 == 0)
				return 1;
				
			return price1 / price2;
		}
		
		private void ProcessSecurity1Candle(ICandleMessage candle, decimal atrValue)
		{
			if (candle.State != CandleStates.Finished)
				return;
				
			// Store ATR value for volatility filter
			_currentAtr = atrValue;
			var atrSmaValue = _atrSma.Process(atrValue).GetValue<decimal>();
			_averageAtr = atrSmaValue;
			
			// Check if we have all necessary data to make a trading decision
			CheckSignal();
		}
		
		private void ProcessSecurity2Candle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;
				
			// Calculate spread (Security1 - Security2 * volumeRatio)
			decimal price1 = Security1.GetCurrentPrice();
			decimal price2 = Security2.GetCurrentPrice();
			
			_previousSpread = _currentSpread;
			_currentSpread = price1 - (price2 * _volumeRatio);
			
			// Calculate spread statistics
			var spreadSmaValue = _spreadSma.Process(_currentSpread).GetValue<decimal>();
			var stdDevValue = _stdDev.Process(_currentSpread).GetValue<decimal>();
			
			_averageSpread = spreadSmaValue;
			_standardDeviation = stdDevValue;
			
			// Check if we have all necessary data to make a trading decision
			CheckSignal();
		}
		
		private void CheckSignal()
		{
			// Ensure strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
				
			// Check if indicators are formed
			if (!_spreadSma.IsFormed || !_stdDev.IsFormed || !_atrSma.IsFormed)
				return;
				
			// Calculate Z-score for spread
			var zScore = (_currentSpread - _averageSpread) / _standardDeviation;
			
			// Check volatility filter - only trade in low volatility environment
			var isLowVolatility = _currentAtr < _averageAtr;
			
			// If we have no position, check for entry signals
			if (Position == 0)
			{
				// Long signal: spread is below threshold (undervalued) with low volatility
				if (zScore < -EntryThreshold && isLowVolatility)
				{
					// Long Security1, Short Security2
					var volume1 = Volume;
					var volume2 = Volume * _volumeRatio;
					
					// Record entry price for later reference
					_entryPrice = _currentSpread;
					
					// Execute trades
					BuyMarket(Security1, volume1);
					SellMarket(Security2, volume2);
					
					this.AddInfoLog($"LONG SPREAD: {Security1.Code} vs {Security2.Code}, Z-Score: {zScore:F2}, Volatility: Low");
				}
				// Short signal: spread is above threshold (overvalued) with low volatility
				else if (zScore > EntryThreshold && isLowVolatility)
				{
					// Short Security1, Long Security2
					var volume1 = Volume;
					var volume2 = Volume * _volumeRatio;
					
					// Record entry price for later reference
					_entryPrice = _currentSpread;
					
					// Execute trades
					SellMarket(Security1, volume1);
					BuyMarket(Security2, volume2);
					
					this.AddInfoLog($"SHORT SPREAD: {Security1.Code} vs {Security2.Code}, Z-Score: {zScore:F2}, Volatility: Low");
				}
			}
			// Check for exit signals
			else
			{
				// Exit when spread returns to mean
				if ((Position > 0 && zScore >= ExitThreshold) || 
					(Position < 0 && zScore <= -ExitThreshold))
				{
					ClosePosition();
					this.AddInfoLog($"CLOSE SPREAD: {Security1.Code} vs {Security2.Code}, Z-Score: {zScore:F2}");
				}
			}
		}
	}
}
