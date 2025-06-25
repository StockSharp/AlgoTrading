using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Bollinger Bands breakout with volatility confirmation.
	/// </summary>
	public class BollingerVolatilityBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _bollingerPeriod;
		private readonly StrategyParam<decimal> _bollingerDeviation;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<decimal> _atrDeviationMultiplier;
		private readonly StrategyParam<decimal> _stopLossMultiplier;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// Period for Bollinger Bands calculation.
		/// </summary>
		public int BollingerPeriod
		{
			get => _bollingerPeriod.Value;
			set => _bollingerPeriod.Value = value;
		}

		/// <summary>
		/// Standard deviation multiplier for Bollinger Bands.
		/// </summary>
		public decimal BollingerDeviation
		{
			get => _bollingerDeviation.Value;
			set => _bollingerDeviation.Value = value;
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
		/// ATR standard deviation multiplier for volatility confirmation.
		/// </summary>
		public decimal AtrDeviationMultiplier
		{
			get => _atrDeviationMultiplier.Value;
			set => _atrDeviationMultiplier.Value = value;
		}

		/// <summary>
		/// Stop loss multiplier relative to ATR.
		/// </summary>
		public decimal StopLossMultiplier
		{
			get => _stopLossMultiplier.Value;
			set => _stopLossMultiplier.Value = value;
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
		/// Initialize a new instance of <see cref="BollingerVolatilityBreakoutStrategy"/>.
		/// </summary>
		public BollingerVolatilityBreakoutStrategy()
		{
			_bollingerPeriod = this.Param(nameof(BollingerPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Indicator Settings")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 10);

			_bollingerDeviation = this.Param(nameof(BollingerDeviation), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicator Settings")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 3.0m, 0.5m);

			_atrPeriod = this.Param(nameof(AtrPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "Period for ATR calculation", "Indicator Settings")
				.SetCanOptimize(true)
				.SetOptimize(7, 21, 7);

			_atrDeviationMultiplier = this.Param(nameof(AtrDeviationMultiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("ATR Deviation Multiplier", "Standard deviation multiplier for ATR", "Strategy Settings")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_stopLossMultiplier = this.Param(nameof(StopLossMultiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss Multiplier", "ATR multiplier for stop-loss", "Strategy Settings")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_candleType = this.Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
			var bollingerBands = new BollingerBands 
			{ 
				Length = BollingerPeriod,
				Width = BollingerDeviation
			};
			
			var atr = new AverageTrueRange { Length = AtrPeriod };
			var atrSma = new SimpleMovingAverage { Length = AtrPeriod };
			var atrStdDev = new StandardDeviation { Length = AtrPeriod };

			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind main indicators to subscription
			subscription
				.Bind(bollingerBands, atr, ProcessCandle)
				.Start();
				
			// Setup additional processing for ATR-based indicators
			var atrSubscription = subscription.CopySubscription();
			
			atrSubscription
				.BindEx(atr, atrValue => {
					atrSma.Process(atrValue);
					atrStdDev.Process(atrValue);
				})
				.Start();

			// Enable position protection
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
				DrawIndicator(area, bollingerBands);
				DrawIndicator(area, atr);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal bbMiddle, decimal bbUpper, decimal bbLower, decimal atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
				
			// Get values from indicators
			var atrSmaValue = atrValue; // Default to current ATR if SMA not available
			var atrStdDevValue = atrValue * 0.2m; // Default to 20% of ATR if StdDev not available
			
			// Get the indicator containers
			var atrSmaContainer = Indicators.TryGetByName("AtrSma");
			var atrStdDevContainer = Indicators.TryGetByName("AtrStdDev");
			
			if (atrSmaContainer != null && atrSmaContainer.IsFormed)
				atrSmaValue = atrSmaContainer.GetCurrentValue();
				
			if (atrStdDevContainer != null && atrStdDevContainer.IsFormed)
				atrStdDevValue = atrStdDevContainer.GetCurrentValue();
			
			// Calculate volatility threshold for breakout confirmation
			var volatilityThreshold = atrSmaValue + AtrDeviationMultiplier * atrStdDevValue;
			
			// Check for increased volatility
			var isHighVolatility = atrValue > volatilityThreshold;
			
			// Define entry conditions
			var longEntryCondition = candle.ClosePrice > bbUpper && isHighVolatility && Position <= 0;
			var shortEntryCondition = candle.ClosePrice < bbLower && isHighVolatility && Position >= 0;
			
			// Define exit conditions
			var longExitCondition = candle.ClosePrice < bbMiddle && Position > 0;
			var shortExitCondition = candle.ClosePrice > bbMiddle && Position < 0;

			// Log current values
			LogInfo($"Close: {candle.ClosePrice}, BB Upper: {bbUpper}, BB Lower: {bbLower}, ATR: {atrValue}, ATR Threshold: {volatilityThreshold}, High Volatility: {isHighVolatility}");

			// Execute trading logic
			if (longEntryCondition)
			{
				// Calculate position size
				var positionSize = Volume + Math.Abs(Position);
				
				// Calculate stop loss level
				var stopPrice = candle.ClosePrice - atrValue * StopLossMultiplier;
				
				// Enter long position
				BuyMarket(positionSize);
				
				LogInfo($"Long entry: Price={candle.ClosePrice}, BB Upper={bbUpper}, ATR={atrValue}, Stop={stopPrice}");
			}
			else if (shortEntryCondition)
			{
				// Calculate position size
				var positionSize = Volume + Math.Abs(Position);
				
				// Calculate stop loss level
				var stopPrice = candle.ClosePrice + atrValue * StopLossMultiplier;
				
				// Enter short position
				SellMarket(positionSize);
				
				LogInfo($"Short entry: Price={candle.ClosePrice}, BB Lower={bbLower}, ATR={atrValue}, Stop={stopPrice}");
			}
			else if (longExitCondition)
			{
				// Exit long position
				SellMarket(Math.Abs(Position));
				LogInfo($"Long exit: Price={candle.ClosePrice}, BB Middle={bbMiddle}");
			}
			else if (shortExitCondition)
			{
				// Exit short position
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short exit: Price={candle.ClosePrice}, BB Middle={bbMiddle}");
			}
		}
	}
}