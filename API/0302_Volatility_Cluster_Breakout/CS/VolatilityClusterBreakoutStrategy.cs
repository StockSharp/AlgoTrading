using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on breakouts during high volatility clusters.
	/// </summary>
	public class VolatilityClusterBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _priceAvgPeriod;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<decimal> _stdDevMultiplier;
		private readonly StrategyParam<decimal> _stopMultiplier;
		private readonly StrategyParam<DataType> _candleType;
		private SimpleMovingAverage _atrAvg;

		/// <summary>
		/// Period for price average and standard deviation calculation.
		/// </summary>
		public int PriceAvgPeriod
		{
			get => _priceAvgPeriod.Value;
			set => _priceAvgPeriod.Value = value;
		}

		/// <summary>
		/// Period for ATR calculation.
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		/// <summary>
		/// Standard deviation multiplier for breakout threshold.
		/// </summary>
		public decimal StdDevMultiplier
		{
			get => _stdDevMultiplier.Value;
			set => _stdDevMultiplier.Value = value;
		}

		/// <summary>
		/// Stop-loss multiplier relative to ATR.
		/// </summary>
		public decimal StopMultiplier
		{
			get => _stopMultiplier.Value;
			set => _stopMultiplier.Value = value;
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
		/// Initialize a new instance of <see cref="VolatilityClusterBreakoutStrategy"/>.
		/// </summary>
		public VolatilityClusterBreakoutStrategy()
		{
			_priceAvgPeriod = Param(nameof(PriceAvgPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Price Average Period", "Period for calculating price average and standard deviation", "Strategy Settings")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "Period for calculating Average True Range", "Strategy Settings")
				.SetCanOptimize(true)
				.SetOptimize(7, 21, 7);

			_stdDevMultiplier = Param(nameof(StdDevMultiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("StdDev Multiplier", "Multiplier for standard deviation to determine breakout levels", "Strategy Settings")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_stopMultiplier = Param(nameof(StopMultiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Stop ATR Multiplier", "ATR multiplier for stop-loss", "Strategy Settings")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles for strategy", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return [(Security, CandleType)];
		}

		/// <inheritdoc />
		protected override void OnReseted()
		{
			base.OnReseted();
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			_atrAvg = new SimpleMovingAverage { Length = AtrPeriod };

			// Create indicators
			var sma = new SimpleMovingAverage { Length = PriceAvgPeriod };
			var stdDev = new StandardDeviation { Length = PriceAvgPeriod };
			var atr = new AverageTrueRange { Length = AtrPeriod };

			// Create subscription
			var subscription = SubscribeCandles(CandleType);

			// Bind indicators to subscription
			subscription
				.Bind(sma, stdDev, atr, ProcessCandle)
				.Start();

			// Enable position protection with dynamic stops
			StartProtection(
				takeProfit: new Unit(0), // We'll handle exits in the strategy logic
				stopLoss: new Unit(0),   // We'll handle stops in the strategy logic
				useMarketOrders: true
			);

			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, sma);
				DrawIndicator(area, atr);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal stdDevValue, decimal atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			var atrAvgVal = _atrAvg.Process(atrValue, candle.ServerTime, candle.State == CandleStates.Finished);

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Calculate breakout levels
			var upperLevel = smaValue + StdDevMultiplier * stdDevValue;
			var lowerLevel = smaValue - StdDevMultiplier * stdDevValue;
			
			// Check if we're in high volatility cluster
			var isHighVolatility = atrValue > smaValue * 0.01m; // ATR > 1% of price as simplification
			
			// Exit conditions based on volatility
			var exitCondition = !isHighVolatility;

			// Entry conditions
			var longEntryCondition = candle.ClosePrice > upperLevel && isHighVolatility && Position <= 0;
			var shortEntryCondition = candle.ClosePrice < lowerLevel && isHighVolatility && Position >= 0;

			// Execute trading logic
			if (exitCondition)
			{
				// Exit positions when volatility drops
				if (Position > 0)
				{
					SellMarket(Math.Abs(Position));
					LogInfo($"Long exit on volatility drop: Price={candle.ClosePrice}, ATR={atrValue}");
				}
				else if (Position < 0)
				{
					BuyMarket(Math.Abs(Position));
					LogInfo($"Short exit on volatility drop: Price={candle.ClosePrice}, ATR={atrValue}");
				}
			}
			else if (longEntryCondition)
			{
				// Calculate position size
				var positionSize = Volume + Math.Abs(Position);
				
				// Calculate stop loss level
				var stopPrice = candle.ClosePrice - atrValue * StopMultiplier;
				
				// Enter long position
				BuyMarket(positionSize);
				
				LogInfo($"Long entry: Price={candle.ClosePrice}, Upper={upperLevel}, ATR={atrValue}, Stop={stopPrice}");
			}
			else if (shortEntryCondition)
			{
				// Calculate position size
				var positionSize = Volume + Math.Abs(Position);
				
				// Calculate stop loss level
				var stopPrice = candle.ClosePrice + atrValue * StopMultiplier;
				
				// Enter short position
				SellMarket(positionSize);
				
				LogInfo($"Short entry: Price={candle.ClosePrice}, Lower={lowerLevel}, ATR={atrValue}, Stop={stopPrice}");
			}
		}
	}
}