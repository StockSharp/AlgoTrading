using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Keltner Channels and RSI indicators.
	/// Enters long when price is below lower Keltner band and RSI is oversold (< 30)
	/// Enters short when price is above upper Keltner band and RSI is overbought (> 70)
	/// </summary>
	public class KeltnerRsiStrategy : Strategy
	{
		private readonly StrategyParam<int> _emaPeriod;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<decimal> _multiplier;
		private readonly StrategyParam<int> _rsiPeriod;
		private readonly StrategyParam<DataType> _candleType;
		
		// Custom implementation of Keltner Channels
		private ExponentialMovingAverage _ema;
		private AverageTrueRange _atr;
		private decimal _upperBand;
		private decimal _lowerBand;

		/// <summary>
		/// EMA period for Keltner Channels
		/// </summary>
		public int EmaPeriod
		{
			get => _emaPeriod.Value;
			set => _emaPeriod.Value = value;
		}

		/// <summary>
		/// ATR period for Keltner Channels
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		/// <summary>
		/// Multiplier for Keltner Channels width
		/// </summary>
		public decimal Multiplier
		{
			get => _multiplier.Value;
			set => _multiplier.Value = value;
		}

		/// <summary>
		/// RSI period
		/// </summary>
		public int RsiPeriod
		{
			get => _rsiPeriod.Value;
			set => _rsiPeriod.Value = value;
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
		public KeltnerRsiStrategy()
		{
			_emaPeriod = Param(nameof(EmaPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("EMA Period", "Period for EMA in Keltner Channels", "Keltner Channels")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "Period for ATR in Keltner Channels", "Keltner Channels")
				.SetCanOptimize(true)
				.SetOptimize(10, 20, 2);

			_multiplier = Param(nameof(Multiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("ATR Multiplier", "ATR multiplier for Keltner Channel width", "Keltner Channels")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 3.0m, 0.5m);

			_rsiPeriod = Param(nameof(RsiPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("RSI Period", "Period for RSI indicator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 20, 2);

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
			_ema = new ExponentialMovingAverage { Length = EmaPeriod };
			_atr = new AverageTrueRange { Length = AtrPeriod };
			var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

			// Subscribe to candles and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.Bind(_ema, _atr, rsi, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				
				// We'll manually draw Keltner Channels as we're calculating them
				
				// Create a separate area for RSI
				var rsiArea = CreateChartArea();
				if (rsiArea != null)
				{
					DrawIndicator(rsiArea, rsi);
				}
				
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal atrValue, decimal rsiValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;
			
			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Calculate Keltner Channels
			_upperBand = emaValue + (Multiplier * atrValue);
			_lowerBand = emaValue - (Multiplier * atrValue);
			
			// Current price (close of the candle)
			var price = candle.ClosePrice;

			// Trading logic
			if (price < _lowerBand && rsiValue < 30 && Position <= 0)
			{
				// Buy signal: price below lower band and RSI oversold
				BuyMarket(Volume + Math.Abs(Position));
				
				// Set stop-loss
				var stopPrice = Math.Min(price - atrValue, _lowerBand - atrValue);
				RegisterOrder(this.CreateOrder(Sides.Sell, stopPrice, Math.Abs(Position + Volume)));
			}
			else if (price > _upperBand && rsiValue > 70 && Position >= 0)
			{
				// Sell signal: price above upper band and RSI overbought
				SellMarket(Volume + Math.Abs(Position));
				
				// Set stop-loss
				var stopPrice = Math.Max(price + atrValue, _upperBand + atrValue);
				RegisterOrder(this.CreateOrder(Sides.Buy, stopPrice, Math.Abs(Position + Volume)));
			}
			// Exit conditions
			else if (price > emaValue && Position < 0)
			{
				// Exit short position when price returns to middle band (EMA)
				BuyMarket(Math.Abs(Position));
			}
			else if (price < emaValue && Position > 0)
			{
				// Exit long position when price returns to middle band (EMA)
				SellMarket(Position);
			}
		}
	}
}