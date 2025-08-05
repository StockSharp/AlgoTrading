using System;
using System.Collections.Generic;
using Ecng.Common;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Harami Bullish pattern strategy.
	/// Strategy enters long position when a bullish harami pattern is detected.
	/// </summary>
	public class HaramiBullishStrategy : Strategy
	{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLossPercent;
		
		private ICandleMessage _previousCandle;
		private bool _patternDetected;

		/// <summary>
		/// Candle type and timeframe for the strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Stop-loss as percentage below the pattern's low.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HaramiBullishStrategy"/>.
		/// </summary>
		public HaramiBullishStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
						 .SetDisplay("Candle Type", "Type of candles to use for pattern detection", "General");
			
			_stopLossPercent = Param(nameof(StopLossPercent), 1m)
							  .SetDisplay("Stop Loss %", "Stop-loss percentage below pattern's low", "Protection")
							  .SetRange(0.1m, 5m);
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
			
			_previousCandle = null;
			_patternDetected = false;
		}

		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);
			// Create and setup subscription for candles
			var subscription = SubscribeCandles(CandleType);
			
			// Bind the candle processor
			subscription
				.Bind(ProcessCandle)
				.Start();
			
			// Enable stop-loss protection
			StartProtection(new Unit(0), new Unit(StopLossPercent, UnitTypes.Percent));
			
			// Setup chart if available
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
			
			// Skip first candle as we need at least one previous candle to detect the pattern
			if (_previousCandle == null)
			{
				_previousCandle = candle;
				return;
			}
			
			// Check for Harami Bullish pattern:
			// 1. Previous candle is bearish (close < open)
			// 2. Current candle is bullish (close > open)
			// 3. Current candle is completely inside the previous candle (high < prev high and low > prev low)
			bool isPreviousBearish = _previousCandle.OpenPrice > _previousCandle.ClosePrice;
			bool isCurrentBullish = candle.OpenPrice < candle.ClosePrice;
			bool isInsidePrevious = candle.HighPrice < _previousCandle.HighPrice && 
									candle.LowPrice > _previousCandle.LowPrice;
			
			// Detect Harami Bullish pattern
			if (isPreviousBearish && isCurrentBullish && isInsidePrevious && !_patternDetected)
			{
				_patternDetected = true;
				
				// Calculate position size (if we already have a position, this will close it and open a new one)
				var volume = Volume + Math.Abs(Position);
				
				// Enter long position at market price
				BuyMarket(volume);
				
				// Set stop-loss level
				var stopLossLevel = candle.LowPrice * (1 - StopLossPercent / 100);
				
				LogInfo($"Harami Bullish detected. Buying at {candle.ClosePrice}. Stop-loss set at {stopLossLevel}");
			}
			else if (_patternDetected)
			{
				// Check for exit condition: price breaks above the previous candle's high
				if (candle.HighPrice > _previousCandle.HighPrice)
				{
					// If we have a long position and price breaks above previous high, close the position
					if (Position > 0)
					{
						SellMarket(Math.Abs(Position));
						_patternDetected = false;
						
						LogInfo($"Exit signal: Price broke above previous high ({_previousCandle.HighPrice}). Closing position at {candle.ClosePrice}");
					}
				}
			}
			
			// Store current candle as previous for the next iteration
			_previousCandle = candle;
		}
	}
}