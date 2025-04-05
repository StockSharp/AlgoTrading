using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Hammer Candle strategy that enters long positions when a hammer candlestick pattern appears.
	/// </summary>
	public class HammerCandleStrategy : Strategy
	{
		private readonly StrategyParam<DataType> _candleType;
		private bool _isPositionOpen;

		/// <summary>
		/// Candle type and timeframe used by the strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public HammerCandleStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
						 .SetDisplay("Candle Type", "Type of candles to use for pattern detection", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return new[] { (Security, CandleType) };
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			_isPositionOpen = false;

			// Subscribe to candles
			var subscription = SubscribeCandles(CandleType);

			subscription
				.Bind(ProcessCandle)
				.Start();

			// Setup stop loss/take profit protection
			StartProtection(
				takeProfit: new Unit(2, UnitTypes.Percent),
				stopLoss: new Unit(1, UnitTypes.Percent)
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

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Already have a position, wait for exit
			if (_isPositionOpen)
				return;

			// Check for hammer pattern:
			// 1. Lower shadow is at least twice the size of the body
			// 2. Small or no upper shadow
			// 3. Current low is lower than previous low (downtrend)

			var bodySize = Math.Abs(candle.OpenPrice - candle.ClosePrice);
			var lowerShadow = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;
			var upperShadow = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);

			var isHammer =
				lowerShadow > bodySize * 2m &&
				upperShadow < bodySize * 0.5m &&
				candle.ClosePrice > candle.OpenPrice;  // Bullish candle

			if (isHammer)
			{
				// Enter long position on hammer pattern
				BuyMarket(Volume);
				_isPositionOpen = true;

				LogInfo($"Hammer pattern detected. Low: {candle.LowPrice}, Body size: {bodySize}, Lower shadow: {lowerShadow}");
			}
		}

		/// <inheritdoc />
		protected override void OnPositionChanged(PositionChangeType changeType, decimal value)
		{
			base.OnPositionChanged(changeType, value);

			// Reset position flag when position is closed
			if (Position == 0)
				_isPositionOpen = false;
		}
	}
}