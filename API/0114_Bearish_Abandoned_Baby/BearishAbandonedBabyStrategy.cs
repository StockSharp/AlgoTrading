namespace StockSharp.Strategies
{
	using System;
	using System.Collections.Generic;

	using StockSharp.Algo.Strategies;
	using StockSharp.Messages;
	using StockSharp.BusinessEntities;

	/// <summary>
	/// Strategy based on Bearish Abandoned Baby candlestick pattern.
	/// </summary>
	public class BearishAbandonedBabyStrategy : Strategy
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
		/// Initializes a new instance of the <see cref="BearishAbandonedBabyStrategy"/>.
		/// </summary>
		public BearishAbandonedBabyStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use for analysis", "Candles");

			_stopLossPercent = Param(nameof(StopLossPercent), 1m)
				.SetRange(0.1m, 5m)
				.SetDisplay("Stop Loss %", "Stop Loss percentage above the high of the doji candle", "Risk")
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

			// If we have enough candles, check for the Bearish Abandoned Baby pattern
			if (_prevCandle2 != null && _prevCandle1 != null)
			{
				// Check for bearish abandoned baby pattern:
				// 1. First candle is bullish (close > open)
				// 2. Middle candle is a doji and gaps up (low > high of first candle)
				// 3. Current candle is bearish (close < open) and gaps down (high < low of middle candle)
				
				bool firstCandleBullish = _prevCandle2.ClosePrice > _prevCandle2.OpenPrice;
				bool middleCandleGapsUp = _prevCandle1.LowPrice > _prevCanle2.HighPrice;
				bool currentCandleBearish = candle.ClosePrice < candle.OpenPrice;
				bool currentCandleGapsDown = candle.HighPrice < _prevCandle1.LowPrice;

				if (firstCandleBullish && middleCandleGapsUp && currentCandleBearish && currentCandleGapsDown)
				{
					LogInfo("Bearish Abandoned Baby pattern detected!");

					// Enter short position if we don't have one already
					if (Position >= 0)
					{
						this.SellMarket(Volume);
						LogInfo($"Short position opened: {Volume} at market");
					}
				}
			}

			// Store current candle for next pattern check
			_prevCandle2 = _prevCandle1;
			_prevCandle1 = candle;

			// Exit logic - if we're in a short position and price breaks below low of the current candle
			if (Position < 0 && candle.LowPrice < _prevCandle2?.LowPrice)
			{
				LogInfo("Exit signal: Price broke below previous candle low");
				ClosePosition();
			}
		}
	}
}