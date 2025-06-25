using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy #80: Pivot Point Reversal strategy.
	/// The strategy calculates daily pivot points and their support/resistance levels,
	/// and enters positions when price bounces off these levels with confirmation.
	/// </summary>
	public class PivotPointReversalStrategy : Strategy
	{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLossPercent;
		
		// Store pivot points
		private decimal _pivot;
		private decimal _r1;  // Resistance level 1
		private decimal _r2;  // Resistance level 2
		private decimal _s1;  // Support level 1
		private decimal _s2;  // Support level 2
		
		// Previous day's OHLC
		private decimal _prevHigh;
		private decimal _prevLow;
		private decimal _prevClose;
		private DateTimeOffset _currentDay;
		private bool _newDayStarted;

		/// <summary>
		/// Candle type.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Stop-loss percentage.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public PivotPointReversalStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
				.SetNotNegative()
				.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);
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

			// Initialize variables
			_prevHigh = 0;
			_prevLow = 0;
			_prevClose = 0;
			_pivot = 0;
			_r1 = 0;
			_r2 = 0;
			_s1 = 0;
			_s2 = 0;
			_currentDay = DateTimeOffset.MinValue;
			_newDayStarted = true;

			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Subscribe to the previous day's candles to get OHLC data
			var dailySubscription = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
			
			// Process daily candles to get previous day's data
			dailySubscription
				.Bind(ProcessDailyCandle)
				.Start();
			
			// Process regular candles for trading signals
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
				takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit, rely on exit logic
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
			);
		}

		private void ProcessDailyCandle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;
			
			// Store previous day's OHLC for pivot point calculation
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_prevClose = candle.ClosePrice;
			
			// Calculate pivot points for the new day
			CalculatePivotPoints();
			
			LogInfo($"New daily candle: High={_prevHigh}, Low={_prevLow}, Close={_prevClose}");
			LogInfo($"Pivot Points: P={_pivot}, R1={_r1}, R2={_r2}, S1={_s1}, S2={_s2}");
		}

		private void CalculatePivotPoints()
		{
			// Only calculate if we have valid data
			if (_prevHigh == 0 || _prevLow == 0 || _prevClose == 0)
				return;
			
			// Calculate pivot point (standard formula)
			_pivot = (_prevHigh + _prevLow + _prevClose) / 3;
			
			// Calculate resistance levels
			_r1 = (2 * _pivot) - _prevLow;
			_r2 = _pivot + (_prevHigh - _prevLow);
			
			// Calculate support levels
			_s1 = (2 * _pivot) - _prevHigh;
			_s2 = _pivot - (_prevHigh - _prevLow);
		}

		private void CheckForNewDay(DateTimeOffset time)
		{
			if (_currentDay == DateTimeOffset.MinValue)
			{
				_currentDay = time.Date;
				_newDayStarted = true;
				return;
			}
			
			if (time.Date > _currentDay)
			{
				_currentDay = time.Date;
				_newDayStarted = true;
				
				// If new day started but we don't have daily candle data yet,
				// we can still use the previous day's high, low, and close
				if (_prevHigh > 0 && _prevLow > 0 && _prevClose > 0)
				{
					CalculatePivotPoints();
				}
			}
		}

		private void ProcessCandle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Check if a new day started
			CheckForNewDay(candle.OpenTime);
			
			// Clear positions at the start of a new day
			if (_newDayStarted)
			{
				if (Position != 0)
				{
					if (Position > 0)
						SellMarket(Math.Abs(Position));
					else
						BuyMarket(Math.Abs(Position));
					
					LogInfo($"New day started, closing position at {candle.ClosePrice}");
				}
				_newDayStarted = false;
			}
			
			// Skip trading if pivot points are not calculated yet
			if (_pivot == 0)
				return;
			
			// Determine if candle is bullish or bearish
			bool isBullish = candle.ClosePrice > candle.OpenPrice;
			bool isBearish = candle.ClosePrice < candle.OpenPrice;
			
			// Calculate proximity to pivot points
			decimal priceThreshold = candle.ClosePrice * 0.001m; // 0.1% threshold
			
			// Check if price is near S1 and bullish
			bool nearS1 = Math.Abs(candle.LowPrice - _s1) <= priceThreshold;
			if (nearS1 && isBullish && Position <= 0)
			{
				// Bullish candle bouncing off S1 - go long
				CancelActiveOrders();
				BuyMarket(Volume + Math.Abs(Position));
				LogInfo($"Long entry at {candle.ClosePrice} on S1 bounce at {_s1}");
			}
			
			// Check if price is near R1 and bearish
			bool nearR1 = Math.Abs(candle.HighPrice - _r1) <= priceThreshold;
			if (nearR1 && isBearish && Position >= 0)
			{
				// Bearish candle bouncing off R1 - go short
				CancelActiveOrders();
				SellMarket(Volume + Math.Abs(Position));
				LogInfo($"Short entry at {candle.ClosePrice} on R1 bounce at {_r1}");
			}
			
			// Exit logic - target pivot point
			if (Position > 0 && candle.ClosePrice > _pivot)
			{
				SellMarket(Math.Abs(Position));
				LogInfo($"Long exit at {candle.ClosePrice} (price above pivot {_pivot})");
			}
			else if (Position < 0 && candle.ClosePrice < _pivot)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short exit at {candle.ClosePrice} (price below pivot {_pivot})");
			}
		}
	}
}