namespace StockSharp.Strategies.Samples
{
	using System;
	using System.Collections.Generic;
	
	using Ecng.Common;
	
	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Strategies;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	
	/// <summary>
	/// Strategy that combines moving average and volume indicators to identify
	/// potential trend breakouts with volume confirmation.
	/// </summary>
	public class MaVolumeStrategy : Strategy
	{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _maPeriod;
		private readonly StrategyParam<int> _volumePeriod;
		private readonly StrategyParam<decimal> _volumeThreshold;
		private readonly StrategyParam<decimal> _stopLossPercent;
		
		private SimpleMovingAverage _priceSma;
		private SimpleMovingAverage _volumeSma;
		
		/// <summary>
		/// Data type for candles.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}
		
		/// <summary>
		/// Period for moving average calculation.
		/// </summary>
		public int MaPeriod
		{
			get => _maPeriod.Value;
			set => _maPeriod.Value = value;
		}
		
		/// <summary>
		/// Period for volume moving average calculation.
		/// </summary>
		public int VolumePeriod
		{
			get => _volumePeriod.Value;
			set => _volumePeriod.Value = value;
		}
		
		/// <summary>
		/// Volume threshold multiplier for volume confirmation.
		/// </summary>
		public decimal VolumeThreshold
		{
			get => _volumeThreshold.Value;
			set => _volumeThreshold.Value = value;
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
		/// Initializes a new instance of the <see cref="MaVolumeStrategy"/>.
		/// </summary>
		public MaVolumeStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
						  .SetDisplay("Candle Type", "Type of candles to use", "General");
						  
			_maPeriod = Param(nameof(MaPeriod), 20)
						.SetRange(5, 200)
						.SetDisplay("MA Period", "Period for moving average calculation", "MA Settings")
						.SetCanOptimize(true);
						
			_volumePeriod = Param(nameof(VolumePeriod), 20)
							.SetRange(5, 100)
							.SetDisplay("Volume MA Period", "Period for volume moving average calculation", "Volume Settings")
							.SetCanOptimize(true);
							
			_volumeThreshold = Param(nameof(VolumeThreshold), 1.5m)
							   .SetRange(1.0m, 3.0m)
							   .SetDisplay("Volume Threshold", "Volume threshold multiplier for volume confirmation", "Volume Settings")
							   .SetCanOptimize(true);
							   
			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
							   .SetRange(0.5m, 5m)
							   .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management");
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
			
			// Set up stop loss protection
			StartProtection(
				new Unit(0), // No take profit
				new Unit(StopLossPercent, UnitTypes.Percent) // Stop loss based on parameter
			);
			
			// Initialize indicators
			_priceSma = new SimpleMovingAverage { Length = MaPeriod };
			_volumeSma = new SimpleMovingAverage { Length = VolumePeriod };
			
			// Create candle subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Create custom processor to handle both price and volume indicators
			subscription
				.Do(ProcessCandle)
				.Start();
				
			// Set up chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _priceSma);
				
				// Create volume area for volume indicator
				var volumeArea = CreateChartArea();
				DrawIndicator(volumeArea, _volumeSma);
				
				DrawOwnTrades(area);
			}
		}
		
		/// <summary>
		/// Process incoming candle with manual indicator updates.
		/// </summary>
		/// <param name="candle">Candle to process.</param>
		private void ProcessCandle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;
				
			// Process indicators
			var smaValue = _priceSma.Process(candle).GetValue<decimal>();
			
			// Handle volume
			var volumeCandle = new CandleMessage
			{
				OpenPrice = candle.TotalVolume,
				HighPrice = candle.TotalVolume,
				LowPrice = candle.TotalVolume,
				ClosePrice = candle.TotalVolume,
				TotalVolume = candle.TotalVolume,
				OpenTime = candle.OpenTime,
				State = candle.State
			};
			
			var volumeSmaValue = _volumeSma.Process(volumeCandle).GetValue<decimal>();
			
			if (!IsFormedAndOnlineAndAllowTrading() || !_priceSma.IsFormed || !_volumeSma.IsFormed)
				return;
				
			// Check if current volume is above threshold compared to average volume
			bool volumeConfirmation = candle.TotalVolume > volumeSmaValue * VolumeThreshold;
			
			// Trading logic
			if (volumeConfirmation)
			{
				if (candle.ClosePrice > smaValue && Position <= 0)
				{
					// Price above MA with volume confirmation - Long signal
					BuyMarket(Volume + Math.Abs(Position));
					LogInfo($"Buy signal: Price ({candle.ClosePrice}) above MA ({smaValue:F4}) with volume confirmation ({candle.TotalVolume} > {volumeSmaValue * VolumeThreshold})");
				}
				else if (candle.ClosePrice < smaValue && Position >= 0)
				{
					// Price below MA with volume confirmation - Short signal
					SellMarket(Volume + Math.Abs(Position));
					LogInfo($"Sell signal: Price ({candle.ClosePrice}) below MA ({smaValue:F4}) with volume confirmation ({candle.TotalVolume} > {volumeSmaValue * VolumeThreshold})");
				}
			}
			
			// Exit logic
			if (Position > 0 && candle.ClosePrice < smaValue && volumeConfirmation)
			{
				// Exit long when price crosses below MA with volume confirmation
				SellMarket(Math.Abs(Position));
				LogInfo($"Exit long: Price ({candle.ClosePrice}) crossed below MA ({smaValue:F4}) with volume confirmation");
			}
			else if (Position < 0 && candle.ClosePrice > smaValue && volumeConfirmation)
			{
				// Exit short when price crosses above MA with volume confirmation
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exit short: Price ({candle.ClosePrice}) crossed above MA ({smaValue:F4}) with volume confirmation");
			}
		}
	}
}