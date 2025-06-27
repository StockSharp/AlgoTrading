using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Strategies
{
	/// <summary>
	/// Z-Score with Volume Filter strategy.
	/// Trading based on Z-score (standard deviations from the mean) with volume confirmation.
	/// </summary>
	public class ZScoreVolumeFilterStrategy : Strategy
	{
		private readonly StrategyParam<int> _lookbackPeriod;
		private readonly StrategyParam<decimal> _zScoreThreshold;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;
		
		// Technical indicators
		private SimpleMovingAverage _priceSma;
		private StandardDeviation _priceStdDev;
		private SimpleMovingAverage _volumeSma;
		
		// Current data values
		private decimal _currentPrice;
		private decimal _currentVolume;
		private decimal _averagePrice;
		private decimal _priceStdDeviation;
		private decimal _averageVolume;
		
		/// <summary>
		/// Lookback period for calculating moving averages and standard deviation.
		/// </summary>
		public int LookbackPeriod
		{
			get => _lookbackPeriod.Value;
			set => _lookbackPeriod.Value = value;
		}
		
		/// <summary>
		/// Z-Score threshold for entry signals.
		/// </summary>
		public decimal ZScoreThreshold
		{
			get => _zScoreThreshold.Value;
			set => _zScoreThreshold.Value = value;
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
		public ZScoreVolumeFilterStrategy()
		{
			_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
				.SetRange(10, 50)
				.SetDisplay("Lookback Period", "Period for calculating moving averages and standard deviation", "Parameters")
				.SetCanOptimize(true);
				
			_zScoreThreshold = Param(nameof(ZScoreThreshold), 2.0m)
				.SetRange(1.0m, 3.0m)
				.SetDisplay("Z-Score Threshold", "Z-Score threshold for entry signals", "Parameters")
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
			return [(Security, CandleType)];
		}
		
		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);
			
			if (Security == null)
				throw new InvalidOperationException("Security is not specified.");
			
			// Initialize indicators
			_priceSma = new SimpleMovingAverage { Length = LookbackPeriod };
			_priceStdDev = new StandardDeviation { Length = LookbackPeriod };
			_volumeSma = new SimpleMovingAverage { Length = LookbackPeriod };
			
			// Set up candle subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind indicators to process data
			subscription
				.Bind(ProcessCandle)
				.Start();
			
			// Setup visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _priceSma);
				DrawOwnTrades(area);
			}
			
			// Setup position protection
			StartProtection(
				new Unit(0, UnitTypes.Absolute), // No take profit
				new Unit(StopLossPercent, UnitTypes.Percent), // Stop loss in percent
				false // No trailing stop
			);
		}
		
		private void ProcessCandle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;
			
			// Store current values
			_currentPrice = candle.ClosePrice;
			_currentVolume = candle.TotalVolume;
			
			// Process indicators
			_averagePrice = _priceSma.Process(_currentPrice, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal();
			_priceStdDeviation = _priceStdDev.Process(_currentPrice, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal();
			_averageVolume = _volumeSma.Process(_currentVolume, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal();
			
			// Check trading signals
			CheckSignal();
		}
		
		private void CheckSignal()
		{
			// Ensure strategy is ready for trading and indicators are formed
			if (!IsFormedAndOnlineAndAllowTrading() || 
				!_priceSma.IsFormed || 
				!_priceStdDev.IsFormed || 
				!_volumeSma.IsFormed)
				return;
			
			// Calculate Z-score (price in standard deviations from mean)
			var zScore = (_currentPrice - _averagePrice) / _priceStdDeviation;
			
			// Check volume filter - require above average volume for confirmation
			var isHighVolume = _currentVolume > _averageVolume;
			
			// If we have no position, check for entry signals
			if (Position == 0)
			{
				// Long signal: price is below threshold (undervalued) with high volume
				if (zScore < -ZScoreThreshold && isHighVolume)
				{
					BuyMarket(Volume);
					LogInfo($"LONG: Z-Score: {zScore:F2}, Volume: High");
				}
				// Short signal: price is above threshold (overvalued) with high volume
				else if (zScore > ZScoreThreshold && isHighVolume)
				{
					SellMarket(Volume);
					LogInfo($"SHORT: Z-Score: {zScore:F2}, Volume: High");
				}
			}
			// Check for exit signals
			else
			{
				// Exit when price returns to mean
				if ((Position > 0 && zScore >= 0) || 
					(Position < 0 && zScore <= 0))
				{
					ClosePosition();
					LogInfo($"CLOSE: Z-Score: {zScore:F2}");
				}
			}
		}
	}
}
