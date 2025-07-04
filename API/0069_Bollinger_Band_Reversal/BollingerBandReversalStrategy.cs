using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy #69: Bollinger Band Reversal strategy.
	/// The strategy enters long position when price is below the lower Bollinger Band and the candle is bullish,
	/// enters short position when price is above the upper Bollinger Band and the candle is bearish.
	/// </summary>
	public class BollingerBandReversalStrategy : Strategy
	{
		private readonly StrategyParam<int> _bollingerPeriod;
		private readonly StrategyParam<decimal> _bollingerDeviation;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _atrMultiplier;

		private BollingerBands _bollingerBands;
		private AverageTrueRange _atr;

		/// <summary>
		/// Bollinger Bands period.
		/// </summary>
		public int BollingerPeriod
		{
			get => _bollingerPeriod.Value;
			set => _bollingerPeriod.Value = value;
		}

		/// <summary>
		/// Bollinger Bands deviation multiplier.
		/// </summary>
		public decimal BollingerDeviation
		{
			get => _bollingerDeviation.Value;
			set => _bollingerDeviation.Value = value;
		}

		/// <summary>
		/// Candle type.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// ATR multiplier for stop-loss.
		/// </summary>
		public decimal AtrMultiplier
		{
			get => _atrMultiplier.Value;
			set => _atrMultiplier.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public BollingerBandReversalStrategy()
		{
			_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Bollinger Period", "Number of periods for Bollinger Bands", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
				.SetNotNegative()
				.SetDisplay("Bollinger Deviation", "Number of standard deviations for Bollinger Bands", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 3.0m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_atrMultiplier = Param(nameof(AtrMultiplier), 2.0m)
				.SetNotNegative()
				.SetDisplay("ATR Multiplier", "Multiplier for ATR to set stop-loss", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);
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
			_bollingerBands = new BollingerBands
			{
				Length = BollingerPeriod,
				Width = BollingerDeviation
			};

			_atr = new AverageTrueRange
			{
				Length = 14
			};

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(_bollingerBands, _atr, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _bollingerBands);
				DrawOwnTrades(area);
			}

			// Start position protection
			StartProtection(
				takeProfit: new Unit(10, UnitTypes.Percent),
				stopLoss: new Unit(AtrMultiplier, UnitTypes.Absolute)
			);
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue atrValue)
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Get current Bollinger Bands values
			var bollingerTyped = (BollingerBandsValue)bollingerValue;
			var upperBand = bollingerTyped.UpBand;
			var lowerBand = bollingerTyped.LowBand;
			var middleBand = bollingerTyped.MovingAverage;

			// Determine if the candle is bullish or bearish
			var isBullish = candle.ClosePrice > candle.OpenPrice;
			var isBearish = candle.ClosePrice < candle.OpenPrice;

			// Long entry: Price below lower band and bullish candle
			if (candle.ClosePrice < lowerBand && isBullish && Position <= 0)
			{
				// Cancel active orders first
				CancelActiveOrders();
				
				// Enter long position
				BuyMarket(Volume + Math.Abs(Position));
				
				LogInfo($"Long entry: Price {candle.ClosePrice} below lower band {lowerBand} with bullish candle");
			}
			// Short entry: Price above upper band and bearish candle
			else if (candle.ClosePrice > upperBand && isBearish && Position >= 0)
			{
				// Cancel active orders first
				CancelActiveOrders();
				
				// Enter short position
				SellMarket(Volume + Math.Abs(Position));
				
				LogInfo($"Short entry: Price {candle.ClosePrice} above upper band {upperBand} with bearish candle");
			}
			// Long exit: Price above middle band
			else if (candle.ClosePrice > middleBand && Position > 0)
			{
				SellMarket(Math.Abs(Position));
				LogInfo($"Long exit: Price {candle.ClosePrice} above middle band {middleBand}");
			}
			// Short exit: Price below middle band
			else if (candle.ClosePrice < middleBand && Position < 0)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short exit: Price {candle.ClosePrice} below middle band {middleBand}");
			}
		}
	}
}