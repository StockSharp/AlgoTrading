using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on "Tweezer Top" candlestick pattern.
	/// This pattern forms when two candlesticks have nearly identical highs, with the first
	/// being bullish and the second being bearish, indicating a potential reversal.
	/// </summary>
	public class TweezerTopStrategy : Strategy
	{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<decimal> _highTolerancePercent;

		private ICandleMessage _previousCandle;
		private ICandleMessage _currentCandle;
		private decimal _entryPrice;

		/// <summary>
		/// Candle type and timeframe for strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Stop-loss percent from entry price.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Tolerance percentage for comparing high prices.
		/// </summary>
		public decimal HighTolerancePercent
		{
			get => _highTolerancePercent.Value;
			set => _highTolerancePercent.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public TweezerTopStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
							.SetDisplay("Candle Type", "Type of candles for strategy calculation", "General");

			_stopLossPercent = Param(nameof(StopLossPercent), 1m)
								.SetRange(0.1m, 5m)
								.SetDisplay("Stop Loss %", "Stop loss as percentage above high", "Risk Management");

			_highTolerancePercent = Param(nameof(HighTolerancePercent), 0.1m)
									.SetRange(0.05m, 1m)
									.SetDisplay("High Tolerance %", "Maximum percentage difference between highs", "Pattern Parameters");
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
					_currentCandle = null;
					_entryPrice = 0;
			}

			/// <inheritdoc />
			protected override void OnStarted(DateTimeOffset time)
			{
					base.OnStarted(time);

					// Create subscription and bind to process candles
					var subscription = SubscribeCandles(CandleType);
					subscription
							.Bind(ProcessCandle)
							.Start();

		// Setup protection with stop loss
		StartProtection(
			takeProfit: null,
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
			isStopTrailing: false
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
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Shift candles
			_previousCandle = _currentCandle;
			_currentCandle = candle;

			if (_previousCandle == null)
				return;

			// Check for Tweezer Top pattern
			var isTweezerTop = IsTweezerTop(_previousCandle, _currentCandle);

			// Check for entry condition
			if (isTweezerTop && Position == 0)
			{
				LogInfo("Tweezer Top pattern detected. Going short.");
				SellMarket(Volume);
				_entryPrice = candle.ClosePrice;
			}
			// Check for exit condition
			else if (Position < 0 && candle.LowPrice < _entryPrice)
			{
				LogInfo("Price below entry low. Taking profit.");
				BuyMarket(Math.Abs(Position));
			}
		}

		private bool IsTweezerTop(ICandleMessage candle1, ICandleMessage candle2)
		{
			// First candle must be bullish (close > open)
			if (candle1.ClosePrice <= candle1.OpenPrice)
				return false;

			// Second candle must be bearish (close < open)
			if (candle2.ClosePrice >= candle2.OpenPrice)
				return false;

			// Calculate the tolerance range for high comparisons
			var highTolerance = candle1.HighPrice * (HighTolerancePercent / 100m);
			
			// High prices must be approximately equal
			var highsAreEqual = Math.Abs(candle1.HighPrice - candle2.HighPrice) <= highTolerance;
			if (!highsAreEqual)
				return false;

			return true;
		}
	}
}