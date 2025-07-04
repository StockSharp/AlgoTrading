using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on RSI with dynamic overbought/oversold levels.
	/// </summary>
	public class RsiDynamicOverboughtOversoldStrategy : Strategy
	{
		private readonly StrategyParam<int> _rsiPeriod;
		private readonly StrategyParam<int> _movingAvgPeriod;
		private readonly StrategyParam<decimal> _stdDevMultiplier;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;
		private SimpleMovingAverage _rsiSma;
		private StandardDeviation _rsiStdDev;

		/// <summary>
		/// Period for RSI calculation.
		/// </summary>
		public int RsiPeriod
		{
			get => _rsiPeriod.Value;
			set => _rsiPeriod.Value = value;
		}

		/// <summary>
		/// Period for moving average and standard deviation calculation.
		/// </summary>
		public int MovingAvgPeriod
		{
			get => _movingAvgPeriod.Value;
			set => _movingAvgPeriod.Value = value;
		}

		/// <summary>
		/// Multiplier for standard deviation to define dynamic levels.
		/// </summary>
		public decimal StdDevMultiplier
		{
			get => _stdDevMultiplier.Value;
			set => _stdDevMultiplier.Value = value;
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
		/// Initialize a new instance of <see cref="RsiDynamicOverboughtOversoldStrategy"/>.
		/// </summary>
		public RsiDynamicOverboughtOversoldStrategy()
		{
			_rsiPeriod = Param(nameof(RsiPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("RSI Period", "Period for RSI calculation", "Indicator Settings")
				.SetCanOptimize(true)
				.SetOptimize(7, 21, 7);

			_movingAvgPeriod = Param(nameof(MovingAvgPeriod), 50)
				.SetGreaterThanZero()
				.SetDisplay("MA Period", "Period for moving average of RSI and price", "Indicator Settings")
				.SetCanOptimize(true)
				.SetOptimize(20, 100, 10);

			_stdDevMultiplier = Param(nameof(StdDevMultiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("StdDev Multiplier", "Multiplier for standard deviation to define overbought/oversold levels", "Strategy Settings")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss %", "Stop loss percentage", "Strategy Settings")
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
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create indicators
			var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
			_rsiSma = new SimpleMovingAverage { Length = MovingAvgPeriod };
			_rsiStdDev = new StandardDeviation { Length = MovingAvgPeriod };
			var priceSma = new SimpleMovingAverage { Length = MovingAvgPeriod };

			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Create RSI and price SMA processing
			subscription
				.Bind(rsi, priceSma, ProcessCandle)
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
				DrawIndicator(area, rsi);
				DrawIndicator(area, priceSma);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal priceSmaValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var smaValue = _rsiSma.Process(rsiValue, candle.ServerTime, candle.State == CandleStates.Finished);
			var stdDevValue = _rsiStdDev.Process(rsiValue, candle.ServerTime, candle.State == CandleStates.Finished);

			// Get values from indicators
			var rsiSmaValue = smaValue.ToDecimal();
			var rsiStdDevValue = stdDevValue.ToDecimal();
			
			// Get the indicator containers using container names
			
			// Calculate dynamic overbought/oversold levels
			var dynamicOverbought = rsiSmaValue + StdDevMultiplier * rsiStdDevValue;
			var dynamicOversold = rsiSmaValue - StdDevMultiplier * rsiStdDevValue;
			
			// Make sure levels are within RSI range (0-100)
			dynamicOverbought = Math.Min(dynamicOverbought, 90m);
			dynamicOversold = Math.Max(dynamicOversold, 10m);
			
			// Log current values
			LogInfo($"RSI: {rsiValue}, MA: {priceSmaValue}, Dynamic Overbought: {dynamicOverbought}, Dynamic Oversold: {dynamicOversold}");
			
			// Define entry conditions
			var longEntryCondition = rsiValue < dynamicOversold && candle.ClosePrice > priceSmaValue && Position <= 0;
			var shortEntryCondition = rsiValue > dynamicOverbought && candle.ClosePrice < priceSmaValue && Position >= 0;
			
			// Define exit conditions
			var longExitCondition = rsiValue > 50 && Position > 0;
			var shortExitCondition = rsiValue < 50 && Position < 0;

			// Execute trading logic
			if (longEntryCondition)
			{
				// Calculate position size
				var positionSize = Volume + Math.Abs(Position);
				
				// Enter long position
				BuyMarket(positionSize);
				
				LogInfo($"Long entry: Price={candle.ClosePrice}, RSI={rsiValue}, Oversold={dynamicOversold}");
			}
			else if (shortEntryCondition)
			{
				// Calculate position size
				var positionSize = Volume + Math.Abs(Position);
				
				// Enter short position
				SellMarket(positionSize);
				
				LogInfo($"Short entry: Price={candle.ClosePrice}, RSI={rsiValue}, Overbought={dynamicOverbought}");
			}
			else if (longExitCondition)
			{
				// Exit long position
				SellMarket(Math.Abs(Position));
				LogInfo($"Long exit: Price={candle.ClosePrice}, RSI={rsiValue}");
			}
			else if (shortExitCondition)
			{
				// Exit short position
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short exit: Price={candle.ClosePrice}, RSI={rsiValue}");
			}
		}
	}
}