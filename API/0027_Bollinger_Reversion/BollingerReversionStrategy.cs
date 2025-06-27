using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Bollinger Bands mean reversion.
	/// </summary>
	public class BollingerReversionStrategy : Strategy
	{
		private readonly StrategyParam<int> _bollingerPeriod;
		private readonly StrategyParam<decimal> _bollingerDeviation;
		private readonly StrategyParam<decimal> _atrMultiplier;
		private readonly StrategyParam<DataType> _candleType;

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
		/// ATR multiplier for stop-loss.
		/// </summary>
		public decimal AtrMultiplier
		{
			get => _atrMultiplier.Value;
			set => _atrMultiplier.Value = value;
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
		/// Initializes a new instance of the <see cref="BollingerReversionStrategy"/>.
		/// </summary>
		public BollingerReversionStrategy()
		{
			_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
				.SetRange(10, 50)
				.SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Indicators")
				.SetCanOptimize(true);

			_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
				.SetRange(1m, 3m)
				.SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators")
				.SetCanOptimize(true);

			_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
				.SetRange(1m, 5m)
				.SetDisplay("ATR Multiplier", "Multiplier for ATR stop loss", "Risk Management")
				.SetCanOptimize(true);

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
			var bollingerBands = new BollingerBands
			{
				Length = BollingerPeriod,
				Width = BollingerDeviation
			};
			
			var atr = new AverageTrueRange { Length = 14 };

			// Subscribe to candles
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(bollingerBands, atr, ProcessCandle)
				.Start();

			// Enable position protection with ATR-based stop loss
			StartProtection(
				takeProfit: null,
				stopLoss: new Unit(AtrMultiplier, UnitTypes.Absolute),
				useMarketOrders: true
			);

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, bollingerBands);
				DrawIndicator(area, atr);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, BollingerBandsValue bbValue, decimal atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Get Bollinger Bands values
			decimal middle = bbValue.Middle;
			decimal upper = bbValue.Upper;
			decimal lower = bbValue.Lower;
			decimal closePrice = candle.ClosePrice;

			// Entry logic
			if (closePrice < lower && Position <= 0)
			{
				// Buy when price falls below lower band
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				LogInfo($"Buy signal: Price {closePrice} below lower band {lower:F2}");
			}
			else if (closePrice > upper && Position >= 0)
			{
				// Sell when price rises above upper band
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				LogInfo($"Sell signal: Price {closePrice} above upper band {upper:F2}");
			}

			// Exit logic
			if (Position > 0 && closePrice > middle)
			{
				// Exit long position when price returns to middle band
				SellMarket(Math.Abs(Position));
				LogInfo($"Exiting long position: Price {closePrice} returned to middle band {middle:F2}");
			}
			else if (Position < 0 && closePrice < middle)
			{
				// Exit short position when price returns to middle band
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exiting short position: Price {closePrice} returned to middle band {middle:F2}");
			}
		}
	}
}