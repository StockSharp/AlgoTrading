using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on MACD indicator.
	/// It enters long position when MACD crosses above signal line and short position when MACD crosses below signal line.
	/// </summary>
	public class MacdTrendStrategy : Strategy
	{
		private readonly StrategyParam<int> _fastEmaPeriod;
		private readonly StrategyParam<int> _slowEmaPeriod;
		private readonly StrategyParam<int> _signalPeriod;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		// Current state
		private bool _prevIsMacdAboveSignal;

		/// <summary>
		/// Period for fast EMA in MACD.
		/// </summary>
		public int FastEmaPeriod
		{
			get => _fastEmaPeriod.Value;
			set => _fastEmaPeriod.Value = value;
		}

		/// <summary>
		/// Period for slow EMA in MACD.
		/// </summary>
		public int SlowEmaPeriod
		{
			get => _slowEmaPeriod.Value;
			set => _slowEmaPeriod.Value = value;
		}

		/// <summary>
		/// Period for signal line in MACD.
		/// </summary>
		public int SignalPeriod
		{
			get => _signalPeriod.Value;
			set => _signalPeriod.Value = value;
		}

		/// <summary>
		/// Stop-loss as percentage of entry price.
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
		/// Initialize the MACD Trend strategy.
		/// </summary>
		public MacdTrendStrategy()
		{
			_fastEmaPeriod = Param(nameof(FastEmaPeriod), 12)
				.SetDisplay("Fast EMA Period", "Period for fast EMA in MACD", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(8, 16, 2);

			_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 26)
				.SetDisplay("Slow EMA Period", "Period for slow EMA in MACD", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(20, 32, 2);

			_signalPeriod = Param(nameof(SignalPeriod), 9)
				.SetDisplay("Signal Period", "Period for signal line in MACD", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(5, 13, 2);

			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetDisplay("Stop Loss (%)", "Stop loss as a percentage of entry price", "Risk parameters")
				.SetCanOptimize(true)
				.SetOptimize(1, 3, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
				
			_prevIsMacdAboveSignal = false;
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

			// Create MACD indicator with signal line

			var macd = new MovingAverageConvergenceDivergenceSignal
			{
				Macd =
				{
					ShortMa = { Length = FastEmaPeriod },
					LongMa = { Length = SlowEmaPeriod },
				},
				SignalMa = { Length = SignalPeriod }
			};
			// Create subscription and bind indicator
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(macd, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, macd);
				DrawOwnTrades(area);
			}

			// Start protection with stop loss
			StartProtection(
				takeProfit: null,
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
			);
		}

		private void ProcessCandle(ICandleMessage candle, decimal macdValue, decimal signalValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Check MACD position relative to signal line
			var isMacdAboveSignal = macdValue > signalValue;
			
			// Check for crossovers
			var isMacdCrossedAboveSignal = isMacdAboveSignal && !_prevIsMacdAboveSignal;
			var isMacdCrossedBelowSignal = !isMacdAboveSignal && _prevIsMacdAboveSignal;

			// Entry/exit logic based on MACD crossovers
			if (isMacdCrossedAboveSignal && Position <= 0)
			{
				// MACD crossed above signal line - Buy signal
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				LogInfo($"Buy signal: MACD ({macdValue:F5}) crossed above Signal ({signalValue:F5})");
			}
			else if (isMacdCrossedBelowSignal && Position >= 0)
			{
				// MACD crossed below signal line - Sell signal
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				LogInfo($"Sell signal: MACD ({macdValue:F5}) crossed below Signal ({signalValue:F5})");
			}
			// Exit logic based on opposite crossover
			else if (isMacdCrossedBelowSignal && Position > 0)
			{
				SellMarket(Position);
				LogInfo($"Exit long: MACD ({macdValue:F5}) crossed below Signal ({signalValue:F5})");
			}
			else if (isMacdCrossedAboveSignal && Position < 0)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exit short: MACD ({macdValue:F5}) crossed above Signal ({signalValue:F5})");
			}

			// Update previous state
			_prevIsMacdAboveSignal = isMacdAboveSignal;
		}
	}
}