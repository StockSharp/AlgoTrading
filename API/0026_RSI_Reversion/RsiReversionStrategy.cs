using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on RSI mean reversion.
	/// </summary>
	public class RsiReversionStrategy : Strategy
	{
		private readonly StrategyParam<int> _rsiPeriod;
		private readonly StrategyParam<decimal> _oversoldThreshold;
		private readonly StrategyParam<decimal> _overboughtThreshold;
		private readonly StrategyParam<decimal> _exitLevel;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// RSI period.
		/// </summary>
		public int RsiPeriod
		{
			get => _rsiPeriod.Value;
			set => _rsiPeriod.Value = value;
		}

		/// <summary>
		/// RSI oversold threshold.
		/// </summary>
		public decimal OversoldThreshold
		{
			get => _oversoldThreshold.Value;
			set => _oversoldThreshold.Value = value;
		}

		/// <summary>
		/// RSI overbought threshold.
		/// </summary>
		public decimal OverboughtThreshold
		{
			get => _overboughtThreshold.Value;
			set => _overboughtThreshold.Value = value;
		}

		/// <summary>
		/// RSI exit level.
		/// </summary>
		public decimal ExitLevel
		{
			get => _exitLevel.Value;
			set => _exitLevel.Value = value;
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
		/// Initializes a new instance of the <see cref="RsiReversionStrategy"/>.
		/// </summary>
		public RsiReversionStrategy()
		{
			_rsiPeriod = Param(nameof(RsiPeriod), 14)
				.SetRange(5, 30, 1)
				.SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")
				.SetCanOptimize(true);

			_oversoldThreshold = Param(nameof(OversoldThreshold), 30m)
				.SetRange(10m, 40m, 5m)
				.SetDisplay("Oversold Threshold", "RSI threshold for oversold condition", "Strategy")
				.SetCanOptimize(true);

			_overboughtThreshold = Param(nameof(OverboughtThreshold), 70m)
				.SetRange(60m, 90m, 5m)
				.SetDisplay("Overbought Threshold", "RSI threshold for overbought condition", "Strategy")
				.SetCanOptimize(true);

			_exitLevel = Param(nameof(ExitLevel), 50m)
				.SetRange(40m, 60m, 5m)
				.SetDisplay("Exit Level", "RSI level for exits (mean reversion)", "Strategy")
				.SetCanOptimize(true);

			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetRange(0.5m, 5m, 0.5m)
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

			// Create RSI indicator
			var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

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

			// Get RSI value
			decimal rsi = rsiValue.ToDecimal();

			// Entry logic for mean reversion
			if (rsi < OversoldThreshold && Position <= 0)
			{
				// Buy when RSI is oversold (below threshold)
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				LogInfo($"Buy signal: RSI oversold at {rsi:F2}");
			}
			else if (rsi > OverboughtThreshold && Position >= 0)
			{
				// Sell when RSI is overbought (above threshold)
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				LogInfo($"Sell signal: RSI overbought at {rsi:F2}");
			}

			// Exit logic based on RSI returning to mid-range (mean reversion)
			if (Position > 0 && rsi > ExitLevel)
			{
				// Exit long position when RSI returns to neutral zone
				SellMarket(Math.Abs(Position));
				LogInfo($"Exiting long position: RSI returned to {rsi:F2}");
			}
			else if (Position < 0 && rsi < ExitLevel)
			{
				// Exit short position when RSI returns to neutral zone
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exiting short position: RSI returned to {rsi:F2}");
			}
		}
	}
}