using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Shooting Star candlestick pattern.
	/// Shooting Star is a bearish reversal pattern that forms after an advance
	/// and is characterized by a small body with a long upper shadow.
	/// </summary>
	public class ShootingStarStrategy : Strategy
	{
		private readonly StrategyParam<decimal> _shadowToBodyRatio;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<bool> _confirmationRequired;
		
		private decimal? _shootingStarHigh;
		private decimal? _shootingStarLow;
		private bool _patternDetected;

		/// <summary>
		/// Minimum ratio between upper shadow and body to qualify as a shooting star.
		/// </summary>
		public decimal ShadowToBodyRatio
		{
			get => _shadowToBodyRatio.Value;
			set => _shadowToBodyRatio.Value = value;
		}

		/// <summary>
		/// Type of candles to use.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Stop-loss percentage above the shooting star's high.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Whether to require a confirmation candle after the shooting star.
		/// </summary>
		public bool ConfirmationRequired
		{
			get => _confirmationRequired.Value;
			set => _confirmationRequired.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ShootingStarStrategy"/>.
		/// </summary>
		public ShootingStarStrategy()
		{
			_shadowToBodyRatio = Param(nameof(ShadowToBodyRatio), 2.0m)
				.SetRange(1.5m, 5.0m)
				.SetDisplay("Shadow/Body Ratio", "Minimum ratio of upper shadow to body length", "Pattern Parameters")
				.SetCanOptimize(true);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_stopLossPercent = Param(nameof(StopLossPercent), 1.0m)
				.SetRange(0.5m, 3.0m)
				.SetDisplay("Stop Loss %", "Percentage above shooting star's high for stop-loss", "Risk Management")
				.SetCanOptimize(true);
				
			_confirmationRequired = Param(nameof(ConfirmationRequired), true)
				.SetDisplay("Confirmation Required", "Whether to wait for a bearish confirmation candle", "Pattern Parameters");
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

			_shootingStarHigh = null;
			_shootingStarLow = null;
			_patternDetected = false;

			// Create highest indicator for trend identification
			var highest = new Highest { Length = 10 };

			// Subscribe to candles
			var subscription = SubscribeCandles(CandleType);

			// Bind candle processing with the highest indicator
			subscription
				.Bind(highest, ProcessCandle)
				.Start();

			// Enable position protection
			StartProtection(
				new Unit(0, UnitTypes.Absolute), // No take profit (manual exit)
				new Unit(StopLossPercent, UnitTypes.Percent), // Stop loss above shooting star's high
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

		private void ProcessCandle(ICandleMessage candle, decimal? highestValue)
		{
		// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			if (highestValue == null)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Already in position, no need to search for new patterns
			if (Position < 0)
				return;
			
			// If we have detected a shooting star and are waiting for confirmation
			if (_patternDetected)
			{
				// If confirmation required and we get a bearish candle
				if (ConfirmationRequired && candle.ClosePrice < candle.OpenPrice)
				{
					// Sell signal - Shooting Star with confirmation candle
					SellMarket(Volume);
					LogInfo($"Shooting Star pattern confirmed: Sell at {candle.ClosePrice}, Stop Loss at {_shootingStarHigh * (1 + StopLossPercent / 100)}");
					
					// Reset pattern detection
					_patternDetected = false;
					_shootingStarHigh = null;
					_shootingStarLow = null;
				}
				// If no confirmation required or we don't want to wait anymore
				else if (!ConfirmationRequired)
				{
					// Sell signal - Shooting Star without waiting for confirmation
					SellMarket(Volume);
					LogInfo($"Shooting Star pattern detected: Sell at {candle.ClosePrice}, Stop Loss at {_shootingStarHigh * (1 + StopLossPercent / 100)}");
					
					// Reset pattern detection
					_patternDetected = false;
					_shootingStarHigh = null;
					_shootingStarLow = null;
				}
				// If we've seen a shooting star but today's candle doesn't confirm, reset
				else if (candle.ClosePrice > candle.OpenPrice)
				{
					_patternDetected = false;
					_shootingStarHigh = null;
					_shootingStarLow = null;
				}
			}
			
			// Pattern detection logic
			else
			{
				// Identify shooting star pattern
				// 1. Candle should appear after an advance (price near recent highs)
				// 2. Upper shadow should be at least X times longer than the body
				// 3. Candle should have small or no lower shadow
				
				// Check if we're near recent highs
				var isNearHighs = Math.Abs(candle.HighPrice - highestValue) / highestValue < 0.03m;
				
				// Check if high is above previous high (market is advancing)
				var isAdvance = candle.HighPrice > highestValue;
				
				// Calculate candle body and shadows
				var bodyLength = Math.Abs(candle.ClosePrice - candle.OpenPrice);
				var upperShadow = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
				var lowerShadow = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;
				
				// Check for bearish shooting star pattern
				var isBearish = candle.ClosePrice < candle.OpenPrice;
				var hasLongUpperShadow = upperShadow > bodyLength * ShadowToBodyRatio;
				var hasSmallLowerShadow = lowerShadow < bodyLength * 0.3m;
				
				// Identify shooting star
				if ((isNearHighs || isAdvance) && hasLongUpperShadow && hasSmallLowerShadow)
				{
					_shootingStarHigh = candle.HighPrice;
					_shootingStarLow = candle.LowPrice;
					_patternDetected = true;
					
					LogInfo($"Potential shooting star detected at {candle.OpenTime}: high={candle.HighPrice}, body ratio={upperShadow/bodyLength:F2}");
					
					// If confirmation not required, sell immediately
					if (!ConfirmationRequired)
					{
						SellMarket(Volume);
						LogInfo($"Shooting Star pattern detected: Sell at {candle.ClosePrice}, Stop Loss at {_shootingStarHigh * (1 + StopLossPercent / 100)}");
						
						// Reset pattern detection
						_patternDetected = false;
						_shootingStarHigh = null;
						_shootingStarLow = null;
					}
				}
			}
		}
	}
}