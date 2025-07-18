using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Bearish Engulfing candlestick pattern.
	/// This pattern occurs when a bearish (black) candlestick completely engulfs
	/// the previous bullish (white) candlestick, signaling a potential bearish reversal.
	/// </summary>
	public class EngulfingBearishStrategy : Strategy
	{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<bool> _requireUptrend;
		private readonly StrategyParam<int> _uptrendBars;

		private ICandleMessage _previousCandle;
		private int _consecutiveUpBars;

		/// <summary>
		/// Type of candles to use.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Stop-loss percentage above the pattern's high.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Whether to require a prior uptrend before the pattern.
		/// </summary>
		public bool RequireUptrend
		{
			get => _requireUptrend.Value;
			set => _requireUptrend.Value = value;
		}

		/// <summary>
		/// Number of consecutive bullish bars to define uptrend.
		/// </summary>
		public int UptrendBars
		{
			get => _uptrendBars.Value;
			set => _uptrendBars.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="EngulfingBearishStrategy"/>.
		/// </summary>
		public EngulfingBearishStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_stopLossPercent = Param(nameof(StopLossPercent), 1.0m)
				.SetRange(0.5m, 3.0m)
				.SetDisplay("Stop Loss %", "Percentage above pattern's high for stop-loss", "Risk Management")
				.SetCanOptimize(true);

			_requireUptrend = Param(nameof(RequireUptrend), true)
				.SetDisplay("Require Uptrend", "Whether to require a prior uptrend", "Pattern Parameters");

			_uptrendBars = Param(nameof(UptrendBars), 3)
				.SetRange(2, 5)
				.SetDisplay("Uptrend Bars", "Number of consecutive bullish bars for uptrend", "Pattern Parameters")
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

			_previousCandle = null;
			_consecutiveUpBars = 0;

			// Subscribe to candles
			var subscription = SubscribeCandles(CandleType);

			// Bind candle processing
			subscription
				.Bind(ProcessCandle)
				.Start();

			// Enable position protection
			StartProtection(
				new Unit(0, UnitTypes.Absolute), // No take profit (manual exit)
				new Unit(StopLossPercent, UnitTypes.Percent), // Stop loss above pattern's high
				false // No trailing
			);

			// Setup chart visualization if available
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

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Already in position, no need to search for new patterns
			if (Position < 0)
			{
				// Store current candle for next iteration
				_previousCandle = candle;
				return;
			}

			// Track consecutive up bars for uptrend identification
			if (candle.ClosePrice > candle.OpenPrice)
			{
				_consecutiveUpBars++;
			}
			else
			{
				_consecutiveUpBars = 0;
			}

			// If we have a previous candle, check for engulfing pattern
			if (_previousCandle != null)
			{
				// Check for bearish engulfing pattern:
				// 1. Previous candle is bullish (close > open)
				// 2. Current candle is bearish (close < open)
				// 3. Current candle's body completely engulfs previous candle's body
				
				var isPreviousBullish = _previousCandle.ClosePrice > _previousCandle.OpenPrice;
				var isCurrentBearish = candle.ClosePrice < candle.OpenPrice;
				
				var isPreviousEngulfed = candle.OpenPrice > _previousCandle.ClosePrice && 
										 candle.ClosePrice < _previousCandle.OpenPrice;
				
				var isUptrendPresent = !RequireUptrend || _consecutiveUpBars >= UptrendBars;
				
				if (isPreviousBullish && isCurrentBearish && isPreviousEngulfed && isUptrendPresent)
				{
					// Bearish engulfing pattern detected
					var patternHigh = Math.Max(candle.HighPrice, _previousCandle.HighPrice);
					
					// Sell signal
					SellMarket(Volume);
					LogInfo($"Bearish Engulfing pattern detected at {candle.OpenTime}: Open={candle.OpenPrice}, Close={candle.ClosePrice}");
					LogInfo($"Previous candle: Open={_previousCandle.OpenPrice}, Close={_previousCandle.ClosePrice}");
					LogInfo($"Stop Loss set at {patternHigh * (1 + StopLossPercent / 100)}");
					
					// Reset consecutive up bars
					_consecutiveUpBars = 0;
				}
			}

			// Store current candle for next iteration
			_previousCandle = candle;
		}
	}
}