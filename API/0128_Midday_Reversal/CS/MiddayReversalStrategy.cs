using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Implementation of Midday Reversal trading strategy.
	/// The strategy trades on price reversals that occur around noon (12:00).
	/// </summary>
	public class MiddayReversalStrategy : Strategy
	{
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;
		
		private decimal _prevCandleClose;
		private decimal _prevPrevCandleClose;

		/// <summary>
		/// Stop loss percentage from entry price.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Candle type for strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MiddayReversalStrategy"/>.
		/// </summary>
		public MiddayReversalStrategy()
		{
			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetNotNegative()
				.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Protection");
			
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles for strategy", "Strategy");
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

			_prevCandleClose = 0;
			_prevPrevCandleClose = 0;
		}

		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);
			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(ProcessCandle)
				.Start();
			
			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawOwnTrades(area);
			}
			
			// Start position protection
			StartProtection(
				takeProfit: new Unit(0), // No take profit
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
			);
		}

		private void ProcessCandle(ICandleMessage candle)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;
			
			// Skip if strategy is not ready
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			var hour = candle.OpenTime.Hour;
			var isNoon = hour == 12; // Check if the candle is around noon (12:00)
			
			// Initialize price history first
			if (_prevCandleClose == 0)
			{
				_prevCandleClose = candle.ClosePrice;
				return;
			}
			
			if (_prevPrevCandleClose == 0)
			{
				_prevPrevCandleClose = _prevCandleClose;
				_prevCandleClose = candle.ClosePrice;
				return;
			}
			
			// Check for midday reversal conditions
			if (isNoon)
			{
				bool isBullishCandle = candle.ClosePrice > candle.OpenPrice;
				bool isBearishCandle = candle.ClosePrice < candle.OpenPrice;
				bool wasPriceDecreasing = _prevCandleClose < _prevPrevCandleClose;
				bool wasPriceIncreasing = _prevCandleClose > _prevPrevCandleClose;
				
				// Buy signal: Previous decrease followed by a bullish candle at noon
				if (wasPriceDecreasing && isBullishCandle && Position <= 0)
				{
					var volume = Volume + Math.Abs(Position);
					BuyMarket(volume);
					
					LogInfo($"Buy signal at midday reversal: Time={candle.OpenTime}, PrevDown={wasPriceDecreasing}, BullishCandle={isBullishCandle}, ClosePrice={candle.ClosePrice}, Volume={volume}");
				}
				// Sell signal: Previous increase followed by a bearish candle at noon
				else if (wasPriceIncreasing && isBearishCandle && Position >= 0)
				{
					var volume = Volume + Math.Abs(Position);
					SellMarket(volume);
					
					LogInfo($"Sell signal at midday reversal: Time={candle.OpenTime}, PrevUp={wasPriceIncreasing}, BearishCandle={isBearishCandle}, ClosePrice={candle.ClosePrice}, Volume={volume}");
				}
			}
			
			// Exit condition - close at 15:00
			if (hour == 15 && Position != 0)
			{
				ClosePosition();
				LogInfo($"Closing position at 15:00: Time={candle.OpenTime}, Position={Position}");
			}
			
			// Update price history
			_prevPrevCandleClose = _prevCandleClose;
			_prevCandleClose = candle.ClosePrice;
		}
	}
}
