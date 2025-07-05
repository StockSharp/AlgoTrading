using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using System;
using System.Collections.Generic;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Bollinger Bands and Williams %R indicators.
	/// Enters long when price is at lower band and Williams %R is oversold (< -80)
	/// Enters short when price is at upper band and Williams %R is overbought (> -20)
	/// </summary>
	public class BollingerWilliamsRStrategy : Strategy
	{
		private readonly StrategyParam<int> _bollingerPeriod;
		private readonly StrategyParam<decimal> _bollingerDeviation;
		private readonly StrategyParam<int> _williamsRPeriod;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<decimal> _atrMultiplier;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// Bollinger Bands period
		/// </summary>
		public int BollingerPeriod
		{
			get => _bollingerPeriod.Value;
			set => _bollingerPeriod.Value = value;
		}

		/// <summary>
		/// Bollinger Bands deviation
		/// </summary>
		public decimal BollingerDeviation
		{
			get => _bollingerDeviation.Value;
			set => _bollingerDeviation.Value = value;
		}

		/// <summary>
		/// Williams %R period
		/// </summary>
		public int WilliamsRPeriod
		{
			get => _williamsRPeriod.Value;
			set => _williamsRPeriod.Value = value;
		}
		
		/// <summary>
		/// ATR period for stop-loss calculation
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}
		
		/// <summary>
		/// ATR multiplier for stop-loss
		/// </summary>
		public decimal AtrMultiplier
		{
			get => _atrMultiplier.Value;
			set => _atrMultiplier.Value = value;
		}

		/// <summary>
		/// Candle type for strategy calculation
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public BollingerWilliamsRStrategy()
		{
			_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Bollinger Period", "Period for Bollinger Bands", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(15, 30, 5);

			_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Bollinger Deviation", "Deviation multiplier for Bollinger Bands", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 2.5m, 0.5m);

			_williamsRPeriod = Param(nameof(WilliamsRPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("Williams %R Period", "Period for Williams %R indicator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 20, 2);
				
			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "Period for ATR indicator for stop-loss", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(10, 20, 2);
				
			_atrMultiplier = Param(nameof(AtrMultiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("ATR Multiplier", "Multiplier for ATR-based stop-loss", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 3.0m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Timeframe for strategy", "General");
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
			var bollinger = new BollingerBands
			{
				Length = BollingerPeriod,
				Width = BollingerDeviation
			};

			var williamsR = new WilliamsR { Length = WilliamsRPeriod };
			
			var atr = new AverageTrueRange { Length = AtrPeriod };

			// Subscribe to candles and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.BindEx(bollinger, williamsR, atr, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, bollinger);
				
				// Create a separate area for Williams %R
				var williamsArea = CreateChartArea();
				if (williamsArea != null)
				{
					DrawIndicator(williamsArea, williamsR);
				}
				
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue williamsRValue, IIndicatorValue atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;
			
			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Get additional values from Bollinger Bands
			var bollingerTyped = (BollingerBandsValue)bollingerValue;

			var middleBand = bollingerTyped.MovingAverage; // Middle band is returned by default
			var upperBand = bollingerTyped.UpBand;
			var lowerBand = bollingerTyped.LowBand;
			
			// Current price (close of the candle)
			var price = candle.ClosePrice;

			// Stop-loss size based on ATR
			var stopSize = atrValue.ToDecimal() * AtrMultiplier;

			var williamsRValueDec = williamsRValue.ToDecimal();

			// Trading logic
			if (price <= lowerBand && williamsRValueDec < -80 && Position <= 0)
			{
				// Buy signal: price at/below lower band and Williams %R oversold
				BuyMarket(Volume + Math.Abs(Position));
				
				// Set stop-loss
				var stopPrice = price - stopSize;
				RegisterOrder(CreateOrder(Sides.Sell, stopPrice, Math.Abs(Position + Volume).Max(Volume)));
			}
			else if (price >= upperBand && williamsRValueDec > -20 && Position >= 0)
			{
				// Sell signal: price at/above upper band and Williams %R overbought
				SellMarket(Volume + Math.Abs(Position));
				
				// Set stop-loss
				var stopPrice = price + stopSize;
				RegisterOrder(CreateOrder(Sides.Buy, stopPrice, Math.Abs(Position + Volume).Max(Volume)));
			}
			// Exit conditions
			else if (price >= middleBand && Position < 0)
			{
				// Exit short position when price returns to middle band
				BuyMarket(Math.Abs(Position));
			}
			else if (price <= middleBand && Position > 0)
			{
				// Exit long position when price returns to middle band
				SellMarket(Position);
			}
		}
	}
}