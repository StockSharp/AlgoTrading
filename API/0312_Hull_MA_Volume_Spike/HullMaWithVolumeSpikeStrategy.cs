using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Hull Moving Average with Volume Spike detection.
	/// </summary>
	public class HullMaWithVolumeSpikeStrategy : Strategy
	{
		private readonly StrategyParam<int> _hmaPeriod;
		private readonly StrategyParam<int> _volumeAvgPeriod;
		private readonly StrategyParam<decimal> _volumeThresholdFactor;
		private readonly StrategyParam<DataType> _candleType;
		
		// Store previous HMA value to detect direction changes
		private decimal _prevHmaValue;

		/// <summary>
		/// Hull Moving Average period parameter.
		/// </summary>
		public int HmaPeriod
		{
			get => _hmaPeriod.Value;
			set => _hmaPeriod.Value = value;
		}

		/// <summary>
		/// Volume average period parameter.
		/// </summary>
		public int VolumeAvgPeriod
		{
			get => _volumeAvgPeriod.Value;
			set => _volumeAvgPeriod.Value = value;
		}

		/// <summary>
		/// Volume threshold factor parameter.
		/// </summary>
		public decimal VolumeThresholdFactor
		{
			get => _volumeThresholdFactor.Value;
			set => _volumeThresholdFactor.Value = value;
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
		public HullMaWithVolumeSpikeStrategy()
		{
			_hmaPeriod = Param(nameof(HmaPeriod), 9)
				.SetGreaterThanZero()
				.SetDisplay("HMA Period", "Period for Hull Moving Average", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(5, 20, 1);

			_volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Volume Avg Period", "Period for volume moving average", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_volumeThresholdFactor = Param(nameof(VolumeThresholdFactor), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Volume Threshold Factor", "Factor for volume spike detection", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 3.0m, 0.5m);

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
			_prevHmaValue = 0;

			// Create indicators
			var hma = new HullMovingAverage { Length = HmaPeriod };
			var volumeSma = new SimpleMovingAverage { Length = VolumeAvgPeriod };
			var volumeStdDev = new StandardDeviation { Length = VolumeAvgPeriod };

			// Subscribe to candles and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.BindEx(hma, (candle, hmaValue) => 
				{
					// Process volume indicators
					var volumeSmaValue = volumeSma.Process(candle.TotalVolume, candle.ServerTime, candle.State == CandleStates.Finished);
					var volumeStdDevValue = volumeStdDev.Process(candle.TotalVolume, candle.ServerTime, candle.State == CandleStates.Finished);
					
					// Process the strategy logic
					ProcessStrategy(
						candle, 
						hmaValue.ToDecimal(), 
						candle.TotalVolume, 
						volumeSmaValue.ToDecimal(), 
						volumeStdDevValue.ToDecimal()
					);
				})
				.Start();

			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, hma);
				DrawOwnTrades(area);
			}

			// Setup position protection
			StartProtection(
				takeProfit: new Unit(2, UnitTypes.Percent),
				stopLoss: new Unit(1, UnitTypes.Percent)
			);
		}

		private void ProcessStrategy(ICandleMessage candle, decimal? hmaValue, decimal? volume, decimal? volumeAvg, decimal? volumeStdDev)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Skip if it's the first valid candle
			if (_prevHmaValue == 0)
			{
				_prevHmaValue = hmaValue;
				return;
			}

			// Detect HMA direction
			var isHmaRising = hmaValue > _prevHmaValue;
			
			// Check for volume spike
			var volumeThreshold = volumeAvg + (VolumeThresholdFactor * volumeStdDev);
			var isVolumeSpiking = volume > volumeThreshold;
			
			// Trading logic - only enter on HMA direction change with volume spike
			if (isHmaRising && isVolumeSpiking && Position <= 0)
			{
				// Hull MA rising with volume spike - Go long
				CancelActiveOrders();
				
				// Calculate position size
				var positionSize = Volume + Math.Abs(Position);
				
				// Enter long position
				BuyMarket(positionSize);
			}
			else if (!isHmaRising && isVolumeSpiking && Position >= 0)
			{
				// Hull MA falling with volume spike - Go short
				CancelActiveOrders();
				
				// Calculate position size
				var positionSize = Volume + Math.Abs(Position);
				
				// Enter short position
				SellMarket(positionSize);
			}
			
			// Exit logic - HMA direction reversal
			if ((Position > 0 && !isHmaRising) || (Position < 0 && isHmaRising))
			{
				// Close position on HMA direction change
				ClosePosition();
			}

			// Update previous HMA value
			_prevHmaValue = hmaValue;
		}
	}
}
