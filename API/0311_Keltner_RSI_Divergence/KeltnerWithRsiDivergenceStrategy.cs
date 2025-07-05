using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Keltner Channel and RSI Divergence.
	/// </summary>
	public class KeltnerWithRsiDivergenceStrategy : Strategy
	{
		private readonly StrategyParam<int> _emaPeriod;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<decimal> _atrMultiplier;
		private readonly StrategyParam<int> _rsiPeriod;
		private readonly StrategyParam<DataType> _candleType;

		// Store previous values to detect divergence
		private decimal _prevRsi;
		private decimal _prevPrice;

		/// <summary>
		/// EMA period parameter.
		/// </summary>
		public int EmaPeriod
		{
			get => _emaPeriod.Value;
			set => _emaPeriod.Value = value;
		}

		/// <summary>
		/// ATR period parameter.
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		/// <summary>
		/// ATR multiplier parameter.
		/// </summary>
		public decimal AtrMultiplier
		{
			get => _atrMultiplier.Value;
			set => _atrMultiplier.Value = value;
		}

		/// <summary>
		/// RSI period parameter.
		/// </summary>
		public int RsiPeriod
		{
			get => _rsiPeriod.Value;
			set => _rsiPeriod.Value = value;
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
		public KeltnerWithRsiDivergenceStrategy()
		{
			_emaPeriod = Param(nameof(EmaPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("EMA Period", "Period for EMA calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(7, 28, 7);

			_atrMultiplier = Param(nameof(AtrMultiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("ATR Multiplier", "Multiplier for ATR to set channel width", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_rsiPeriod = Param(nameof(RsiPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(7, 28, 7);

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

			// Initialize previous values
			_prevRsi = 50;
			_prevPrice = 0;

			// Create indicators
			var ema = new ExponentialMovingAverage { Length = EmaPeriod };
			var atr = new AverageTrueRange { Length = AtrPeriod };
			var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

			// Subscribe to candles and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.Bind(ema, atr, rsi, ProcessCandle)
				.Start();

			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, ema);
				DrawIndicator(area, rsi);
				DrawOwnTrades(area);
			}

			// Setup position protection
			StartProtection(
				takeProfit: new Unit(2, UnitTypes.Percent),
				stopLoss: new Unit(1, UnitTypes.Percent)
			);
		}

		private void ProcessCandle(ICandleMessage candle, decimal? emaValue, decimal? atrValue, decimal? rsiValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Skip if it's the first valid candle
			if (_prevPrice == 0)
			{
				_prevPrice = candle.ClosePrice;
				_prevRsi = rsiValue;
				return;
			}

			// Calculate Keltner Channel
			var upperBand = emaValue + (AtrMultiplier * atrValue);
			var lowerBand = emaValue - (AtrMultiplier * atrValue);

			// Check for RSI divergence
			var isBullishDivergence = rsiValue > _prevRsi && candle.ClosePrice < _prevPrice;
			var isBearishDivergence = rsiValue < _prevRsi && candle.ClosePrice > _prevPrice;

			// Trading logic
			if (candle.ClosePrice < lowerBand && isBullishDivergence && Position <= 0)
			{
				// Bullish divergence at lower band
				CancelActiveOrders();
				
				// Calculate position size
				var volume = Volume + Math.Abs(Position);
				
				// Enter long position
				BuyMarket(volume);
			}
			else if (candle.ClosePrice > upperBand && isBearishDivergence && Position >= 0)
			{
				// Bearish divergence at upper band
				CancelActiveOrders();
				
				// Calculate position size
				var volume = Volume + Math.Abs(Position);
				
				// Enter short position
				SellMarket(volume);
			}

			// Exit logic - when price reverts to EMA
			if ((Position > 0 && candle.ClosePrice > emaValue) ||
				(Position < 0 && candle.ClosePrice < emaValue))
			{
				// Exit position
				ClosePosition();
			}

			// Update previous values
			_prevRsi = rsiValue;
			_prevPrice = candle.ClosePrice;
		}
	}
}
