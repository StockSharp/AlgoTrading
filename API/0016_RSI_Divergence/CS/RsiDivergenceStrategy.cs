using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on RSI divergence.
	/// </summary>
	public class RsiDivergenceStrategy : Strategy
	{
		private readonly StrategyParam<int> _rsiPeriod;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		private decimal _prevPrice;
		private decimal _prevRsi;
		private bool _isFirstCandle = true;

		/// <summary>
		/// RSI period.
		/// </summary>
		public int RsiPeriod
		{
			get => _rsiPeriod.Value;
			set => _rsiPeriod.Value = value;
		}

		/// <summary>
		/// Stop loss percentage.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Candle type.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RsiDivergenceStrategy"/>.
		/// </summary>
		public RsiDivergenceStrategy()
		{
			_rsiPeriod = Param(nameof(RsiPeriod), 14)
				.SetRange(5, 50)
				.SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")
				.SetCanOptimize(true);

			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetRange(0.5m, 5m)
				.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
				.SetCanOptimize(true);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
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
			// Reset state variables
			_prevPrice = 0;
			_prevRsi = 0;
			_isFirstCandle = true;

		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create RSI indicator
			var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

			// Subscribe to candles and bind the indicator
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(rsi, ProcessCandle)
				.Start();

			// Enable position protection
			StartProtection(
				takeProfit: null,
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
				useMarketOrders: true
			);

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, rsi);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var currentPrice = candle.ClosePrice;
			var currentRsi = rsiValue.ToDecimal();

			// For the first candle, just store values and return
			if (_isFirstCandle)
			{
				_prevPrice = currentPrice;
				_prevRsi = currentRsi;
				_isFirstCandle = false;
				return;
			}

			// Detect bullish divergence: Price makes lower low but RSI makes higher low
			if (currentPrice < _prevPrice && currentRsi > _prevRsi && Position <= 0)
			{
				// Buy signal
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);

				LogInfo($"Bullish divergence detected: Price {_prevPrice} -> {currentPrice}, RSI {_prevRsi} -> {currentRsi}");
			}

			// Detect bearish divergence: Price makes higher high but RSI makes lower high
			else if (currentPrice > _prevPrice && currentRsi < _prevRsi && Position >= 0)
			{
				// Sell signal
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);

				LogInfo($"Bearish divergence detected: Price {_prevPrice} -> {currentPrice}, RSI {_prevRsi} -> {currentRsi}");
			}

			// Exit logic for long positions: RSI crosses above 70 (overbought)
			if (Position > 0 && currentRsi > 70)
			{
				SellMarket(Math.Abs(Position));
				LogInfo($"Exiting long position: RSI overbought at {currentRsi}");
			}

			// Exit logic for short positions: RSI crosses below 30 (oversold)
			else if (Position < 0 && currentRsi < 30)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exiting short position: RSI oversold at {currentRsi}");
			}

			// Update previous values for next comparison
			_prevPrice = currentPrice;
			_prevRsi = currentRsi;
		}
	}
}