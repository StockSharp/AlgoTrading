using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Pairs Trading Strategy with Volatility Filter strategy.
	/// </summary>
	public class PairsTradingVolatilityFilterStrategy : Strategy
	{
		private readonly StrategyParam<Security> _security2;
		private readonly StrategyParam<int> _lookbackPeriod;
		private readonly StrategyParam<decimal> _entryThreshold;
		private readonly StrategyParam<decimal> _exitThreshold;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;
		
		private decimal _currentSpread;
		private decimal _previousSpread;
		private decimal _averageSpread;
		private decimal _standardDeviation;
		private decimal _currentAtr;
		private decimal _averageAtr;
		
		private decimal _volumeRatio = 1; // Default 1:1 ratio
		private decimal _entryPrice;
		
		private decimal _lastPrice1;
		private decimal _lastPrice2;
		
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
			get => Security;
			set => Security = value;
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
		/// The type of candles to use for strategy calculation.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public PairsTradingVolatilityFilterStrategy()
		{
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

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "Data");
		}
		
		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			if (Security1 != null)
				yield return (Security1, CandleType);

			if (Security2 != null)
				yield return (Security2, CandleType);
		}

		/// <inheritdoc />
		protected override void OnReseted()
		{
			base.OnReseted();
			_currentAtr = 0;
			_averageAtr = 0;
			_currentSpread = 0;
			_previousSpread = 0;
			_averageSpread = 0;
			_standardDeviation = 0;
			_entryPrice = 0;
			_lastPrice1 = 0;
			_lastPrice2 = 0;
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
			var subscription1 = SubscribeCandles(CandleType, false, Security1);
			var subscription2 = SubscribeCandles(CandleType, false, Security2);
			
			// Subscribe to ticks for both securities to track last prices
			
			SubscribeTicks(Security1)
				.Bind(tick => _lastPrice1 = tick.Price)
				.Start();

			SubscribeTicks(Security2)
				.Bind(tick => _lastPrice2 = tick.Price)
				.Start();
			
			// Process data and calculate spread
			subscription1
				.Bind(_atr, ProcessSecurity1Candle)
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
			// Use last known prices if available
			var price1 = _lastPrice1;
			var price2 = _lastPrice2;
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
			var atrSmaValue = _atrSma.Process(atrValue, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal();
			_averageAtr = atrSmaValue;
			
			// Check if we have all necessary data to make a trading decision
			CheckSignal();
		}
		
		private void ProcessSecurity2Candle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;

			// Calculate spread (Security1 - Security2 * volumeRatio) using last prices
			decimal price1 = _lastPrice1;
			decimal price2 = _lastPrice2;

			_previousSpread = _currentSpread;
			_currentSpread = price1 - (price2 * _volumeRatio);
			
			// Calculate spread statistics
			var spreadSmaValue = _spreadSma.Process(_currentSpread, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal();
			var stdDevValue = _stdDev.Process(_currentSpread, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal();
			
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

			// Prevent division by zero
			if (_standardDeviation == 0)
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
					BuyMarket(volume1, Security1);
					SellMarket(volume2, Security2);
					
					LogInfo($"LONG SPREAD: {Security1.Code} vs {Security2.Code}, Z-Score: {zScore:F2}, Volatility: Low");
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
					SellMarket(volume1, Security1);
					BuyMarket(volume2, Security2);
					
					LogInfo($"SHORT SPREAD: {Security1.Code} vs {Security2.Code}, Z-Score: {zScore:F2}, Volatility: Low");
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
					LogInfo($"CLOSE SPREAD: {Security1.Code} vs {Security2.Code}, Z-Score: {zScore:F2}");
				}
			}
		}
	}
}
