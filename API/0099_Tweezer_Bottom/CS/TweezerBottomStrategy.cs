using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on "Tweezer Bottom" candlestick pattern.
	/// This pattern forms when two candlesticks have nearly identical lows, with the first
	/// being bearish and the second being bullish, indicating a potential reversal.
	/// </summary>
	public class TweezerBottomStrategy : Strategy
	{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<decimal> _lowTolerancePercent;

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
		/// Tolerance percentage for comparing low prices.
		/// </summary>
		public decimal LowTolerancePercent
		{
			get => _lowTolerancePercent.Value;
			set => _lowTolerancePercent.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public TweezerBottomStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
							.SetDisplay("Candle Type", "Type of candles for strategy calculation", "General");

			_stopLossPercent = Param(nameof(StopLossPercent), 1m)
								.SetRange(0.1m, 5m)
								.SetDisplay("Stop Loss %", "Stop loss as percentage below low", "Risk Management");

			_lowTolerancePercent = Param(nameof(LowTolerancePercent), 0.1m)
									.SetRange(0.05m, 1m)
									.SetDisplay("Low Tolerance %", "Maximum percentage difference between lows", "Pattern Parameters");
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

			// Check for Tweezer Bottom pattern
			var isTweezerBottom = IsTweezerBottom(_previousCandle, _currentCandle);

			// Check for entry condition
			if (isTweezerBottom && Position == 0)
			{
				LogInfo("Tweezer Bottom pattern detected. Going long.");
				BuyMarket(Volume);
				_entryPrice = candle.ClosePrice;
			}
			// Check for exit condition
			else if (Position > 0 && candle.HighPrice > _entryPrice)
			{
				LogInfo("Price exceeded entry high. Taking profit.");
				SellMarket(Math.Abs(Position));
			}
		}

		private bool IsTweezerBottom(ICandleMessage candle1, ICandleMessage candle2)
		{
			// First candle must be bearish (close < open)
			if (candle1.ClosePrice >= candle1.OpenPrice)
				return false;

			// Second candle must be bullish (close > open)
			if (candle2.ClosePrice <= candle2.OpenPrice)
				return false;

			// Calculate the tolerance range for low comparisons
			var lowTolerance = candle1.LowPrice * (LowTolerancePercent / 100m);
			
			// Low prices must be approximately equal
			var lowsAreEqual = Math.Abs(candle1.LowPrice - candle2.LowPrice) <= lowTolerance;
			if (!lowsAreEqual)
				return false;

			return true;
		}
	}
}