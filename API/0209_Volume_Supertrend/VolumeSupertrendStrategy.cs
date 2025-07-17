using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Volume and Supertrend indicators (#209)
	/// </summary>
	public class VolumeSupertrendStrategy : Strategy
	{
		private readonly StrategyParam<int> _volumeAvgPeriod;
		private readonly StrategyParam<int> _supertrendPeriod;
		private readonly StrategyParam<decimal> _supertrendMultiplier;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLossPercent;

		/// <summary>
		/// Volume average period
		/// </summary>
		public int VolumeAvgPeriod
		{
			get => _volumeAvgPeriod.Value;
			set => _volumeAvgPeriod.Value = value;
		}

		/// <summary>
		/// Supertrend ATR period
		/// </summary>
		public int SupertrendPeriod
		{
			get => _supertrendPeriod.Value;
			set => _supertrendPeriod.Value = value;
		}

		/// <summary>
		/// Supertrend multiplier
		/// </summary>
		public decimal SupertrendMultiplier
		{
			get => _supertrendMultiplier.Value;
			set => _supertrendMultiplier.Value = value;
		}

		/// <summary>
		/// Candle type for strategy
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Stop-loss percentage parameter.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public VolumeSupertrendStrategy()
		{
			_volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 20)
				.SetRange(10, 50)
				.SetDisplay("Volume Avg Period", "Period for volume average calculation", "Volume")
				.SetCanOptimize(true);

			_supertrendPeriod = Param(nameof(SupertrendPeriod), 10)
				.SetRange(5, 30)
				.SetDisplay("Supertrend Period", "ATR period for Supertrend", "Supertrend")
				.SetCanOptimize(true);

			_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3m)
				.SetRange(1m, 5m)
				.SetDisplay("Supertrend Multiplier", "Multiplier for Supertrend calculation", "Supertrend")
				.SetCanOptimize(true);

			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetGreaterThanZero()
				.SetDisplay("Stop-loss %", "Stop-loss as percentage of entry price", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1m, 3m, 0.5m);

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

			// Initialize indicators
			var volumeMA = new SimpleMovingAverage { Length = VolumeAvgPeriod };
			
			// Create custom Supertrend indicator - StockSharp doesn't have built-in Supertrend
			var atr = new AverageTrueRange { Length = SupertrendPeriod };
			
			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Current Supertrend state variables
			var supertrendValue = 0m;
			var supertrendDirection = 0; // 1 for up (bullish), -1 for down (bearish)
			
			// Bind indicators to handle each candle
			subscription
				.Bind(atr, (candle, atrValue) =>
				{
					// Calculate volume average
					var volumeValue = volumeMA.Process(candle.TotalVolume, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal();
					
					// Calculate Supertrend
					if (!atr.IsFormed)
						return;
						
					var highPrice = candle.HighPrice;
					var lowPrice = candle.LowPrice;
					var closePrice = candle.ClosePrice;
					
					// Calculate bands
					var multiplier = SupertrendMultiplier;
					var atrAmount = atrValue * multiplier;
					
					var upperBand = ((highPrice + lowPrice) / 2) + atrAmount;
					var lowerBand = ((highPrice + lowPrice) / 2) - atrAmount;
					
					// Initialize Supertrend
					if (supertrendValue == 0 && supertrendDirection == 0)
					{
						supertrendValue = closePrice;
						supertrendDirection = 1;
					}
					
					// Update Supertrend
					if (supertrendDirection == 1) // Previous trend was up
					{
						// Update lower band only - trailing
						supertrendValue = Math.Max(lowerBand, supertrendValue);
						
						// Check for trend reversal
						if (closePrice < supertrendValue)
						{
							supertrendDirection = -1;
							supertrendValue = upperBand;
						}
					}
					else // Previous trend was down
					{
						// Update upper band only - trailing
						supertrendValue = Math.Min(upperBand, supertrendValue);
						
						// Check for trend reversal
						if (closePrice > supertrendValue)
						{
							supertrendDirection = 1;
							supertrendValue = lowerBand;
						}
					}
					
					// Current volume
					var currentVolume = candle.TotalVolume;
					
					// Process trading signals
					ProcessSignals(candle, currentVolume, volumeValue, supertrendValue, supertrendDirection);
				})
				.Start();

			// Enable position protection
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute), // No take-profit
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent) // Stop-loss as percentage
			);

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, volumeMA);
				
				// Create a secondary area for volume
				var volumeArea = CreateChartArea();
				if (volumeArea != null)
				{
					// Use Volume indicator to visualize volume
					var volumeIndicator = new VolumeIndicator();
					DrawIndicator(volumeArea, volumeIndicator);
				}
				
				DrawOwnTrades(area);
			}
		}

		private void ProcessSignals(ICandleMessage candle, decimal currentVolume, decimal volumeAvg, 
			decimal supertrendValue, int supertrendDirection)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var price = candle.ClosePrice;

			// Trading logic:
			// Long: Volume > Avg(Volume) && Price > Supertrend (volume surge with uptrend)
			// Short: Volume > Avg(Volume) && Price < Supertrend (volume surge with downtrend)
			
			var volumeSurge = currentVolume > volumeAvg;
			
			if (volumeSurge && supertrendDirection == 1 && Position <= 0)
			{
				// Buy signal - Volume surge with Supertrend uptrend
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			else if (volumeSurge && supertrendDirection == -1 && Position >= 0)
			{
				// Sell signal - Volume surge with Supertrend downtrend
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}
			// Exit conditions based on Supertrend reversal
			else if (Position > 0 && supertrendDirection == -1)
			{
				// Exit long position when Supertrend turns down
				SellMarket(Position);
			}
			else if (Position < 0 && supertrendDirection == 1)
			{
				// Exit short position when Supertrend turns up
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}
