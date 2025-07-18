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
	/// Harami Bearish pattern strategy.
	/// Strategy enters short position when a bearish harami pattern is detected.
	/// </summary>
	public class HaramiBearishStrategy : Strategy
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
		/// Stop-loss as percentage above the pattern's high.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HaramiBearishStrategy"/>.
		/// </summary>
		public HaramiBearishStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
						 .SetDisplay("Candle Type", "Type of candles to use for pattern detection", "General");
			
			_stopLossPercent = Param(nameof(StopLossPercent), 1m)
							  .SetDisplay("Stop Loss %", "Stop-loss percentage above pattern's high", "Protection")
							  .SetRange(0.1m, 5m);
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
			
			_previousCandle = null;
			_patternDetected = false;

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
			
			// Check for Harami Bearish pattern:
			// 1. Previous candle is bullish (close > open)
			// 2. Current candle is bearish (close < open)
			// 3. Current candle is completely inside the previous candle (high < prev high and low > prev low)
			bool isPreviousBullish = _previousCandle.OpenPrice < _previousCandle.ClosePrice;
			bool isCurrentBearish = candle.OpenPrice > candle.ClosePrice;
			bool isInsidePrevious = candle.HighPrice < _previousCandle.HighPrice && 
									candle.LowPrice > _previousCandle.LowPrice;
			
			// Detect Harami Bearish pattern
			if (isPreviousBullish && isCurrentBearish && isInsidePrevious && !_patternDetected)
			{
				_patternDetected = true;
				
				// Calculate position size (if we already have a position, this will close it and open a new one)
				var volume = Volume + Math.Abs(Position);
				
				// Enter short position at market price
				SellMarket(volume);
				
				// Set stop-loss level
				var stopLossLevel = candle.HighPrice * (1 + StopLossPercent / 100);
				
				LogInfo($"Harami Bearish detected. Selling at {candle.ClosePrice}. Stop-loss set at {stopLossLevel}");
			}
			else if (_patternDetected)
			{
				// Check for exit condition: price breaks below the previous candle's low
				if (candle.LowPrice < _previousCandle.LowPrice)
				{
					// If we have a short position and price breaks below previous low, close the position
					if (Position < 0)
					{
						BuyMarket(Math.Abs(Position));
						_patternDetected = false;
						
						LogInfo($"Exit signal: Price broke below previous low ({_previousCandle.LowPrice}). Closing position at {candle.ClosePrice}");
					}
				}
			}
			
			// Store current candle as previous for the next iteration
			_previousCandle = candle;
		}
	}
}