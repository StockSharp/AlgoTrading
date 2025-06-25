using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on ADX with Volume Breakout.
	/// </summary>
	public class AdxWithVolumeBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _adxPeriod;
		private readonly StrategyParam<decimal> _adxThreshold;
		private readonly StrategyParam<int> _volumeAvgPeriod;
		private readonly StrategyParam<decimal> _volumeThresholdFactor;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// ADX period parameter.
		/// </summary>
		public int AdxPeriod
		{
			get => _adxPeriod.Value;
			set => _adxPeriod.Value = value;
		}

		/// <summary>
		/// ADX threshold parameter.
		/// </summary>
		public decimal AdxThreshold
		{
			get => _adxThreshold.Value;
			set => _adxThreshold.Value = value;
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
		public AdxWithVolumeBreakoutStrategy()
		{
			_adxPeriod = Param(nameof(AdxPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(7, 28, 7);

			_adxThreshold = Param(nameof(AdxThreshold), 25m)
				.SetGreaterThanZero()
				.SetDisplay("ADX Threshold", "Threshold for strong trend identification", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(15, 35, 5);

			_volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Volume Avg Period", "Period for volume moving average", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_volumeThresholdFactor = Param(nameof(VolumeThresholdFactor), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Volume Threshold Factor", "Factor for volume breakout detection", "Indicators")
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

			// Create indicators
			var adx = new AverageDirectionalIndex { Length = AdxPeriod };
			var volumeSma = new SimpleMovingAverage { Length = VolumeAvgPeriod };
			var volumeStdDev = new StandardDeviation { Length = VolumeAvgPeriod };

			// Subscribe to candles and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.BindEx(adx, (candle, adxValue) =>
				{
					// Process volume indicators
					var volumeIndicatorValue = new DecimalIndicatorValue(candle.TotalVolume);
					var volumeAvg = volumeSma.Process(volumeIndicatorValue).GetValue<decimal>();
					var volumeStdDev = volumeStdDev.Process(volumeIndicatorValue).GetValue<decimal>();
					
					// Process the strategy logic
					ProcessStrategy(
						candle,
						adxValue,
						candle.TotalVolume,
						volumeAvg,
						volumeStdDev
					);
				})
				.Start();

			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, adx);
				DrawOwnTrades(area);
			}

			// Setup position protection
			StartProtection(
				takeProfit: new Unit(2, UnitTypes.Percent),
				stopLoss: new Unit(1, UnitTypes.Percent)
			);
		}

		private void ProcessStrategy(ICandleMessage candle, IIndicatorValue adxValue, decimal volume, decimal volumeAvg, decimal volumeStdDev)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Extract values from ADX composite indicator
			var adx = adxValue[0].GetValue<decimal>();	 // ADX value
			var diPlus = adxValue[1].GetValue<decimal>();  // +DI value
			var diMinus = adxValue[2].GetValue<decimal>(); // -DI value
			
			// Check for strong trend
			var isStrongTrend = adx > AdxThreshold;
			
			// Check directional indicators
			var isBullishTrend = diPlus > diMinus;
			var isBearishTrend = diMinus > diPlus;
			
			// Check for volume breakout
			var volumeThreshold = volumeAvg + (VolumeThresholdFactor * volumeStdDev);
			var isVolumeBreakout = volume > volumeThreshold;
			
			// Trading logic - only enter with strong trend and volume breakout
			if (isStrongTrend && isVolumeBreakout)
			{
				if (isBullishTrend && Position <= 0)
				{
					// Strong bullish trend with volume breakout - Go long
					CancelActiveOrders();
					
					// Calculate position size
					var volume = Volume + Math.Abs(Position);
					
					// Enter long position
					BuyMarket(volume);
				}
				else if (isBearishTrend && Position >= 0)
				{
					// Strong bearish trend with volume breakout - Go short
					CancelActiveOrders();
					
					// Calculate position size
					var volume = Volume + Math.Abs(Position);
					
					// Enter short position
					SellMarket(volume);
				}
			}
			
			// Exit logic - when ADX drops below threshold (trend weakens)
			if (adx < 20)
			{
				// Close position on trend weakening
				ClosePosition();
			}
		}
	}
}
