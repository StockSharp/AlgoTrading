using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Doji Reversal strategy.
	/// The strategy looks for doji candlestick patterns after a trend and takes a reversal position.
	/// </summary>
	public class DojiReversalStrategy : Strategy
	{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _dojiThreshold;
		private readonly StrategyParam<decimal> _stopLossPercent;
		
		private ICandleMessage _previousCandle;
		private ICandleMessage _previousPreviousCandle;

		/// <summary>
		/// Candle type.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Doji threshold as percentage of candle range.
		/// </summary>
		public decimal DojiThreshold
		{
			get => _dojiThreshold.Value;
			set => _dojiThreshold.Value = value;
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
		public DojiReversalStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_dojiThreshold = Param(nameof(DojiThreshold), 0.1m)
				.SetNotNegative()
				.SetDisplay("Doji Threshold", "Maximum body size as percentage of candle range to consider it a doji", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(0.05m, 0.2m, 0.05m);

			_stopLossPercent = Param(nameof(StopLossPercent), 1.0m)
				.SetNotNegative()
				.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(0.5m, 2.0m, 0.5m);
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
			_previousPreviousCandle = null;
		}

		/// <inheritdoc />
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
			
			// Start protection with dynamic stop-loss
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit, relying on exit signal
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
			);
		}

		private void ProcessCandle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// We need 3 candles to make a decision
			if (_previousCandle == null)
			{
				_previousCandle = candle;
				return;
			}

			if (_previousPreviousCandle == null)
			{
				_previousPreviousCandle = _previousCandle;
				_previousCandle = candle;
				return;
			}

			// Check if current candle is a doji
			var isDoji = IsDojiCandle(candle);
			
			if (isDoji)
			{
				// Check for downtrend before the doji (previous candle lower than the one before it)
				var isDowntrend = _previousCandle.ClosePrice < _previousPreviousCandle.ClosePrice;
				
				// Check for uptrend before the doji (previous candle higher than the one before it)
				var isUptrend = _previousCandle.ClosePrice > _previousPreviousCandle.ClosePrice;
				
				LogInfo($"Doji detected. Downtrend before: {isDowntrend}, Uptrend before: {isUptrend}");
				
				// If we have a doji after a downtrend and no long position yet
				if (isDowntrend && Position <= 0)
				{
					// Cancel any existing orders
					CancelActiveOrders();
					
					// Enter long position
					BuyMarket(Volume + Math.Abs(Position));
					
					LogInfo($"Buying at {candle.ClosePrice} after doji in downtrend");
				}
				// If we have a doji after an uptrend and no short position yet
				else if (isUptrend && Position >= 0)
				{
					// Cancel any existing orders
					CancelActiveOrders();
					
					// Enter short position
					SellMarket(Volume + Math.Abs(Position));
					
					LogInfo($"Selling at {candle.ClosePrice} after doji in uptrend");
				}
			}

			// Exit logic - exiting when the price moves beyond the doji's high/low
			if (Position > 0 && candle.HighPrice > _previousCandle.HighPrice)
			{
				SellMarket(Math.Abs(Position));
				LogInfo($"Exiting long position at {candle.ClosePrice} (price above doji high)");
			}
			else if (Position < 0 && candle.LowPrice < _previousCandle.LowPrice)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exiting short position at {candle.ClosePrice} (price below doji low)");
			}

			// Update candles for next iteration
			_previousPreviousCandle = _previousCandle;
			_previousCandle = candle;
		}

		private bool IsDojiCandle(ICandleMessage candle)
		{
			// Calculate the body size (absolute difference between open and close)
			var bodySize = Math.Abs(candle.OpenPrice - candle.ClosePrice);
			
			// Calculate the total range of the candle
			var totalRange = candle.HighPrice - candle.LowPrice;
			
			// Avoid division by zero
			if (totalRange == 0)
				return false;
			
			// Calculate the body as a percentage of the total range
			var bodySizePercentage = bodySize / totalRange;
			
			// It's a doji if the body size is smaller than the threshold
			var isDoji = bodySizePercentage < DojiThreshold;
			
			LogInfo($"Candle analysis: Body size: {bodySize}, Total range: {totalRange}, " +
						   $"Body %: {bodySizePercentage:P2}, Is Doji: {isDoji}");
			
			return isDoji;
		}
	}
}