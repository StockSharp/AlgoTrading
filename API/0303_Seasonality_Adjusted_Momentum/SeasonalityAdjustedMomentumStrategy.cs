using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on momentum indicator adjusted with seasonality strength.
	/// </summary>
	public class SeasonalityAdjustedMomentumStrategy : Strategy
	{
		private readonly StrategyParam<int> _momentumPeriod;
		private readonly StrategyParam<decimal> _seasonalityThreshold;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;
		
		// Dictionary to store seasonality strength values for each month
		private readonly Dictionary<int, decimal> _seasonalStrengthByMonth = new Dictionary<int, decimal>();

		/// <summary>
		/// Period for Momentum indicator.
		/// </summary>
		public int MomentumPeriod
		{
			get => _momentumPeriod.Value;
			set => _momentumPeriod.Value = value;
		}

		/// <summary>
		/// Threshold for seasonality strength.
		/// </summary>
		public decimal SeasonalityThreshold
		{
			get => _seasonalityThreshold.Value;
			set => _seasonalityThreshold.Value = value;
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
		/// Initialize a new instance of <see cref="SeasonalityAdjustedMomentumStrategy"/>.
		/// </summary>
		public SeasonalityAdjustedMomentumStrategy()
		{
			_momentumPeriod = this.Param(nameof(MomentumPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("Momentum Period", "Period for momentum indicator", "Strategy Settings")
				.SetCanOptimize(true)
				.SetOptimize(7, 21, 7);

			_seasonalityThreshold = this.Param(nameof(SeasonalityThreshold), 0.5m)
				.SetGreaterThanZero()
				.SetDisplay("Seasonality Threshold", "Threshold value for seasonality strength", "Strategy Settings")
				.SetCanOptimize(true)
				.SetOptimize(0.3m, 0.7m, 0.1m);

			_stopLossPercent = this.Param(nameof(StopLossPercent), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss %", "Stop loss percentage", "Strategy Settings")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_candleType = this.Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles for strategy", "General");
			
			// Initialize seasonality strength for each month (example data)
			InitializeSeasonalityData();
		}

		private void InitializeSeasonalityData()
		{
			// This is sample data - in a real strategy, this would be calculated from historical data
			// Positive values indicate historically strong months, negative values indicate weak months
			_seasonalStrengthByMonth[1] = 0.8m;  // January
			_seasonalStrengthByMonth[2] = 0.2m;  // February
			_seasonalStrengthByMonth[3] = 0.5m;  // March
			_seasonalStrengthByMonth[4] = 0.7m;  // April
			_seasonalStrengthByMonth[5] = 0.3m;  // May
			_seasonalStrengthByMonth[6] = -0.2m; // June
			_seasonalStrengthByMonth[7] = -0.3m; // July
			_seasonalStrengthByMonth[8] = -0.4m; // August
			_seasonalStrengthByMonth[9] = -0.7m; // September
			_seasonalStrengthByMonth[10] = 0.4m; // October
			_seasonalStrengthByMonth[11] = 0.6m; // November
			_seasonalStrengthByMonth[12] = 0.9m; // December
		}

		/// <inheritdoc />
		public override IEnumerable<(Security security, DataType dataType)> GetWorkingSecurities()
		{
			return [(Security, CandleType)];
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create indicators
			var momentum = new Momentum { Length = MomentumPeriod };
			var momentumAvg = new SimpleMovingAverage { Length = MomentumPeriod };

			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind indicators to subscription
			subscription
				.Bind(momentum, momentumAvg, ProcessCandle)
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
				DrawIndicator(area, momentum);
				DrawIndicator(area, momentumAvg);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal momentumValue, decimal momentumAvgValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Get current month
			var currentMonth = candle.OpenTime.Month;
			
			// Get seasonality strength for current month
			decimal seasonalStrength = 0;
			if (_seasonalStrengthByMonth.TryGetValue(currentMonth, out var strength))
			{
				seasonalStrength = strength;
			}
			
			// Log seasonality data
			LogInfo($"Month: {currentMonth}, Seasonality Strength: {seasonalStrength}, Momentum: {momentumValue}, Avg Momentum: {momentumAvgValue}");
			
			// Define entry conditions with seasonality adjustment
			var longEntryCondition = momentumValue > momentumAvgValue && 
								   seasonalStrength > SeasonalityThreshold && 
								   Position <= 0;
								   
			var shortEntryCondition = momentumValue < momentumAvgValue && 
									seasonalStrength < -SeasonalityThreshold && 
									Position >= 0;
			
			// Define exit conditions
			var longExitCondition = momentumValue < momentumAvgValue && Position > 0;
			var shortExitCondition = momentumValue > momentumAvgValue && Position < 0;

			// Execute trading logic
			if (longEntryCondition)
			{
				// Calculate position size
				var positionSize = Volume + Math.Abs(Position);
				
				// Enter long position
				BuyMarket(positionSize);
				
				LogInfo($"Long entry: Price={candle.ClosePrice}, Momentum={momentumValue}, Avg={momentumAvgValue}, Seasonality={seasonalStrength}");
			}
			else if (shortEntryCondition)
			{
				// Calculate position size
				var positionSize = Volume + Math.Abs(Position);
				
				// Enter short position
				SellMarket(positionSize);
				
				LogInfo($"Short entry: Price={candle.ClosePrice}, Momentum={momentumValue}, Avg={momentumAvgValue}, Seasonality={seasonalStrength}");
			}
			else if (longExitCondition)
			{
				// Exit long position
				SellMarket(Math.Abs(Position));
				LogInfo($"Long exit: Price={candle.ClosePrice}, Momentum={momentumValue}, Avg={momentumAvgValue}");
			}
			else if (shortExitCondition)
			{
				// Exit short position
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short exit: Price={candle.ClosePrice}, Momentum={momentumValue}, Avg={momentumAvgValue}");
			}
		}
	}
}