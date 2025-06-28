namespace StockSharp.Strategies
{
	using System;
	using System.Collections.Generic;

	using StockSharp.Algo.Strategies;
	using StockSharp.Messages;
	using StockSharp.BusinessEntities;

	/// <summary>
	/// Strategy based on Bullish Abandoned Baby candlestick pattern.
	/// </summary>
	public class BullishAbandonedBabyStrategy : Strategy
	{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLossPercent;

		private ICandleMessage _prevCandle1;
		private ICandleMessage _prevCandle2;

		/// <summary>
		/// Candle type and timeframe.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Stop-loss percent from entry price.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BullishAbandonedBabyStrategy"/>.
		/// </summary>
		public BullishAbandonedBabyStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use for analysis", "Candles");

			_stopLossPercent = Param(nameof(StopLossPercent), 1m)
				.SetRange(0.1m, 5m)
				.SetDisplay("Stop Loss %", "Stop Loss percentage below the low of the doji candle", "Risk")
				.SetCanOptimize(true);
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

			// Reset pattern candles
			_prevCandle1 = null;
			_prevCandle2 = null;

			// Create and subscribe to candles
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.Bind(ProcessCandle)
				.Start();

			// Configure protection for open positions
			StartProtection(
				takeProfit: new Unit(0), // No take profit, using exit logic in the strategy
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
				isStopTrailing: false);

			// Set up chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Add log entry for the candle
			LogInfo($"Candle: Open={candle.OpenPrice}, High={candle.HighPrice}, Low={candle.LowPrice}, Close={candle.ClosePrice}");

			// If we have enough candles, check for the Bullish Abandoned Baby pattern
			if (_prevCandle2 != null && _prevCandle1 != null)
			{
				// Check for bullish abandoned baby pattern:
				// 1. First candle is bearish (close < open)
				// 2. Middle candle is a doji and gaps down (high < low of first candle)
				// 3. Current candle is bullish (close > open) and gaps up (low > high of middle candle)
				
				bool firstCandleBearish = _prevCandle2.ClosePrice < _prevCandle2.OpenPrice;
				bool middleCandleGapsDown = _prevCandle1.HighPrice < _prevCandle2.LowPrice;
				bool currentCandleBullish = candle.ClosePrice > candle.OpenPrice;
				bool currentCandleGapsUp = candle.LowPrice > _prevCandle1.HighPrice;

				if (firstCandleBearish && middleCandleGapsDown && currentCandleBullish && currentCandleGapsUp)
				{
					LogInfo("Bullish Abandoned Baby pattern detected!");

					// Enter long position if we don't have one already
					if (Position <= 0)
					{
						BuyMarket(Volume);
						LogInfo($"Long position opened: {Volume} at market");
					}
				}
			}

			// Store current candle for next pattern check
			_prevCandle2 = _prevCandle1;
			_prevCandle1 = candle;

			// Exit logic - if we're in a long position and price breaks above high of the current candle
			if (Position > 0 && candle.HighPrice > _prevCandle2?.HighPrice)
			{
				LogInfo("Exit signal: Price broke above previous candle high");
				ClosePosition();
			}
		}
	}
}