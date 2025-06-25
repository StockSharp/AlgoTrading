using System;
using System.Collections.Generic;
using Ecng.Common;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on rejection candles that indicate potential reversals.
	/// </summary>
	public class RejectionCandleStrategy : Strategy
	{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLossPercent;
		
		private ICandleMessage _previousCandle;
		private bool _inPosition;
		private Sides _currentPositionSide;

		/// <summary>
		/// Candle type and timeframe for the strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Stop-loss percentage from entry price.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RejectionCandleStrategy"/>.
		/// </summary>
		public RejectionCandleStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
						 .SetDisplay("Candle Type", "Type of candles to use for pattern detection", "General");
			
			_stopLossPercent = Param(nameof(StopLossPercent), 1m)
							  .SetDisplay("Stop Loss %", "Stop-loss percentage from entry price", "Protection")
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
			_inPosition = false;
			_currentPositionSide = Sides.None;

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
			
			// Skip first candle as we need at least one previous candle
			if (_previousCandle == null)
			{
				_previousCandle = candle;
				return;
			}
			
			// Determine candle characteristics
			bool isBullish = candle.ClosePrice > candle.OpenPrice;
			bool isBearish = candle.ClosePrice < candle.OpenPrice;
			bool hasUpperWick = candle.HighPrice > Math.Max(candle.OpenPrice, candle.ClosePrice);
			bool hasLowerWick = candle.LowPrice < Math.Min(candle.OpenPrice, candle.ClosePrice);
			bool madeLowerLow = candle.LowPrice < _previousCandle.LowPrice;
			bool madeHigherHigh = candle.HighPrice > _previousCandle.HighPrice;
			
			// 1. Bearish Rejection (Pin Bar): Made a higher high but closed lower with a long upper wick
			if (madeHigherHigh && isBearish && hasUpperWick)
			{
				// Calculate upper wick size as a percentage of candle body
				decimal bodySize = Math.Abs(candle.ClosePrice - candle.OpenPrice);
				decimal upperWickSize = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
				
				// Upper wick should be significant compared to body
				if (upperWickSize > bodySize * 1.5m)
				{
					// If we already have a long position, close it
					if (Position > 0)
					{
						SellMarket(Math.Abs(Position));
						_inPosition = false;
						_currentPositionSide = Sides.None;
						LogInfo($"Closed long position at {candle.ClosePrice} on bearish rejection");
					}
					// Enter short if we're not already short
					else if (Position <= 0)
					{
						var volume = Volume + Math.Abs(Position);
						SellMarket(volume);
						_inPosition = true;
						_currentPositionSide = Sides.Sell;
						LogInfo($"Bearish rejection detected. Short entry at {candle.ClosePrice}");
					}
				}
			}
			
			// 2. Bullish Rejection (Pin Bar): Made a lower low but closed higher with a long lower wick
			else if (madeLowerLow && isBullish && hasLowerWick)
			{
				// Calculate lower wick size as a percentage of candle body
				decimal bodySize = Math.Abs(candle.ClosePrice - candle.OpenPrice);
				decimal lowerWickSize = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;
				
				// Lower wick should be significant compared to body
				if (lowerWickSize > bodySize * 1.5m)
				{
					// If we already have a short position, close it
					if (Position < 0)
					{
						BuyMarket(Math.Abs(Position));
						_inPosition = false;
						_currentPositionSide = Sides.None;
						LogInfo($"Closed short position at {candle.ClosePrice} on bullish rejection");
					}
					// Enter long if we're not already long
					else if (Position >= 0)
					{
						var volume = Volume + Math.Abs(Position);
						BuyMarket(volume);
						_inPosition = true;
						_currentPositionSide = Sides.Buy;
						LogInfo($"Bullish rejection detected. Long entry at {candle.ClosePrice}");
					}
				}
			}
			
			// Check for exit conditions if in position
			if (_inPosition)
			{
				if (_currentPositionSide == Sides.Buy && candle.HighPrice > _previousCandle.HighPrice)
				{
					// For long positions: exit when price breaks above the high of previous candle
					SellMarket(Math.Abs(Position));
					_inPosition = false;
					_currentPositionSide = Sides.None;
					LogInfo($"Exit signal: Price broke above previous high ({_previousCandle.HighPrice}). Closed long at {candle.ClosePrice}");
				}
				else if (_currentPositionSide == Sides.Sell && candle.LowPrice < _previousCandle.LowPrice)
				{
					// For short positions: exit when price breaks below the low of previous candle
					BuyMarket(Math.Abs(Position));
					_inPosition = false;
					_currentPositionSide = Sides.None;
					LogInfo($"Exit signal: Price broke below previous low ({_previousCandle.LowPrice}). Closed short at {candle.ClosePrice}");
				}
			}
			
			// Store current candle as previous for the next iteration
			_previousCandle = candle;
		}
	}
}