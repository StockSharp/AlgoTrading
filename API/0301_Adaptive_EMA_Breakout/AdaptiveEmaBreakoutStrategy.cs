using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Adaptive EMA breakout with trend confirmation.
	/// </summary>
	public class AdaptiveEmaBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _fast;
		private readonly StrategyParam<int> _slow;
		private readonly StrategyParam<int> _lookback;
		private readonly StrategyParam<decimal> _stopMultiplier;
		private readonly StrategyParam<DataType> _candleType;

		private decimal _prevAdaptiveEmaValue;
		private bool _isFirstCandle = true;

		/// <summary>
		/// Fast EMA Period parameter for KAMA calculation.
		/// </summary>
		public int Fast
		{
			get => _fast.Value;
			set => _fast.Value = value;
		}

		/// <summary>
		/// Slow EMA Period parameter for KAMA calculation.
		/// </summary>
		public int Slow
		{
			get => _slow.Value;
			set => _slow.Value = value;
		}

		/// <summary>
		/// Lookback period for KAMA calculation.
		/// </summary>
		public int Lookback
		{
			get => _lookback.Value;
			set => _lookback.Value = value;
		}

		/// <summary>
		/// Stop-loss multiplier relative to ATR.
		/// </summary>
		public decimal StopMultiplier
		{
			get => _stopMultiplier.Value;
			set => _stopMultiplier.Value = value;
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
		/// Initialize a new instance of <see cref="AdaptiveEmaBreakoutStrategy"/>.
		/// </summary>
		public AdaptiveEmaBreakoutStrategy()
		{
			_fast = this.Param(nameof(Fast), 2)
				.SetGreaterThanZero()
				.SetDisplay("Fast period", "Fast (EMA) period for calculating KAMA", "KAMA Settings")
				.SetCanOptimize(true)
				.SetOptimize(2, 10, 1);

			_slow = this.Param(nameof(Slow), 30)
				.SetGreaterThanZero()
				.SetDisplay("Slow period", "Slow (EMA) period for calculating KAMA", "KAMA Settings")
				.SetCanOptimize(true)
				.SetOptimize(20, 40, 5);

			_lookback = this.Param(nameof(Lookback), 10)
				.SetGreaterThanZero()
				.SetDisplay("Lookback", "Main period for calculating KAMA", "KAMA Settings")
				.SetCanOptimize(true)
				.SetOptimize(5, 20, 5);

			_stopMultiplier = this.Param(nameof(StopMultiplier), 2m)
				.SetGreaterThanZero()
				.SetDisplay("Stop ATR multiplier", "ATR multiplier for stop-loss", "Strategy Settings")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_candleType = this.Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle type", "Type of candles for strategy", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security security, DataType dataType)> GetWorkingSecurities()
		{
			return [(Security, CandleType)];
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create indicators
			var adaptiveEma = new KaufmanAdaptiveMovingAverage { Length = Lookback, Fast = Fast, Slow = Slow };
			var atr = new AverageTrueRange { Length = 14 };

			// Reset state variables
			_isFirstCandle = true;
			_prevAdaptiveEmaValue = 0;

			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind indicators to subscription
			subscription
				.Bind(adaptiveEma, atr, ProcessCandle)
				.Start();

			// Enable position protection
			StartProtection(
				takeProfit: new Unit(0), // We'll handle exits in the strategy logic
				stopLoss: new Unit(0),   // We'll handle stops in the strategy logic
				useMarketOrders: true
			);

			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, adaptiveEma);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal adaptiveEmaValue, decimal atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Initialize values on first candle
			if (_isFirstCandle)
			{
				_prevAdaptiveEmaValue = adaptiveEmaValue;
				_isFirstCandle = false;
				return;
			}

			// Calculate trend direction
			var adaptiveEmaTrendUp = adaptiveEmaValue > _prevAdaptiveEmaValue;
			
			// Define entry conditions
			var longEntryCondition = candle.ClosePrice > adaptiveEmaValue && adaptiveEmaTrendUp && Position <= 0;
			var shortEntryCondition = candle.ClosePrice < adaptiveEmaValue && !adaptiveEmaTrendUp && Position >= 0;
			
			// Define exit conditions
			var longExitCondition = candle.ClosePrice < adaptiveEmaValue && Position > 0;
			var shortExitCondition = candle.ClosePrice > adaptiveEmaValue && Position < 0;

			// Execute trading logic
			if (longEntryCondition)
			{
				// Calculate position size
				var positionSize = Volume + Math.Abs(Position);
				
				// Calculate stop loss level
				var stopPrice = candle.ClosePrice - atrValue * StopMultiplier;
				
				// Enter long position
				BuyMarket(positionSize);
				
				LogInfo($"Long entry: Price={candle.ClosePrice}, KAMA={adaptiveEmaValue}, ATR={atrValue}, Stop={stopPrice}");
			}
			else if (shortEntryCondition)
			{
				// Calculate position size
				var positionSize = Volume + Math.Abs(Position);
				
				// Calculate stop loss level
				var stopPrice = candle.ClosePrice + atrValue * StopMultiplier;
				
				// Enter short position
				SellMarket(positionSize);
				
				LogInfo($"Short entry: Price={candle.ClosePrice}, KAMA={adaptiveEmaValue}, ATR={atrValue}, Stop={stopPrice}");
			}
			else if (longExitCondition)
			{
				// Exit long position
				SellMarket(Math.Abs(Position));
				LogInfo($"Long exit: Price={candle.ClosePrice}, KAMA={adaptiveEmaValue}");
			}
			else if (shortExitCondition)
			{
				// Exit short position
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short exit: Price={candle.ClosePrice}, KAMA={adaptiveEmaValue}");
			}

			// Store current value for next candle
			_prevAdaptiveEmaValue = adaptiveEmaValue;
		}
	}
}