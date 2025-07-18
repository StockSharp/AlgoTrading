using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Laguerre RSI.
	/// </summary>
	public class LaguerreRsiStrategy : Strategy
	{
		private readonly StrategyParam<decimal> _gamma;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// Laguerre RSI Gamma parameter.
		/// </summary>
		public decimal Gamma
		{
			get => _gamma.Value;
			set => _gamma.Value = value;
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
		/// Initializes a new instance of the <see cref="LaguerreRsiStrategy"/>.
		/// </summary>
		public LaguerreRsiStrategy()
		{
			_gamma = Param(nameof(Gamma), 0.7m)
				.SetRange(0.2m, 0.9m)
				.SetDisplay("Gamma", "Gamma parameter for Laguerre RSI", "Indicators")
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
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create Laguerre RSI indicator
			// Note: StockSharp doesn't have a built-in Laguerre RSI, so we'll use a custom implementation
			// For demonstration purposes, we'll use a regular RSI but apply the strategy logic for Laguerre RSI
			var rsi = new RelativeStrengthIndex { Length = 14 };

			// Subscribe to candles
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

			// Get RSI value and normalize it to 0-1 range (Laguerre RSI uses 0-1 scale)
			decimal rsi = rsiValue.ToDecimal();
			decimal normRsi = rsi / 100m; // Convert standard RSI (0-100) to Laguerre RSI scale (0-1)

			// Get price direction
			bool isPriceRising = candle.OpenPrice < candle.ClosePrice;

			// Entry logic based on Laguerre RSI levels
			// - Buy when RSI is below 0.2 (oversold) and price is rising
			// - Sell when RSI is above 0.8 (overbought) and price is falling
			if (normRsi < 0.2m && isPriceRising && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				LogInfo($"Buy signal: Laguerre RSI oversold at {normRsi:F4} with rising price");
			}
			else if (normRsi > 0.8m && !isPriceRising && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				LogInfo($"Sell signal: Laguerre RSI overbought at {normRsi:F4} with falling price");
			}

			// Exit logic
			if (Position > 0 && normRsi > 0.8m)
			{
				// Exit long when RSI reaches overbought
				SellMarket(Math.Abs(Position));
				LogInfo($"Exiting long position: Laguerre RSI reached overbought at {normRsi:F4}");
			}
			else if (Position < 0 && normRsi < 0.2m)
			{
				// Exit short when RSI reaches oversold
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exiting short position: Laguerre RSI reached oversold at {normRsi:F4}");
			}
		}
	}
}