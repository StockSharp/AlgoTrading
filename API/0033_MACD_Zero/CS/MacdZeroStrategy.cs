using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that trades MACD reversions to zero line.
	/// It enters when MACD is below/above zero and trending back towards zero line,
	/// and exits when MACD crosses its signal line.
	/// </summary>
	public class MacdZeroStrategy : Strategy
	{
		private readonly StrategyParam<int> _fastPeriod;
		private readonly StrategyParam<int> _slowPeriod;
		private readonly StrategyParam<int> _signalPeriod;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		private decimal _prevMacd;

		/// <summary>
		/// Fast EMA period for MACD calculation (default: 12)
		/// </summary>
		public int FastPeriod
		{
			get => _fastPeriod.Value;
			set => _fastPeriod.Value = value;
		}

		/// <summary>
		/// Slow EMA period for MACD calculation (default: 26)
		/// </summary>
		public int SlowPeriod
		{
			get => _slowPeriod.Value;
			set => _slowPeriod.Value = value;
		}

		/// <summary>
		/// Signal line period for MACD calculation (default: 9)
		/// </summary>
		public int SignalPeriod
		{
			get => _signalPeriod.Value;
			set => _signalPeriod.Value = value;
		}

		/// <summary>
		/// Stop-loss as percentage from entry price (default: 2%)
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Type of candles used for strategy calculation
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initialize the MACD Zero strategy
		/// </summary>
		public MacdZeroStrategy()
		{
			_fastPeriod = Param(nameof(FastPeriod), 12)
				.SetDisplay("Fast EMA Period", "Fast EMA period for MACD calculation", "MACD Parameters")
				.SetCanOptimize(true)
				.SetOptimize(8, 16, 2);

			_slowPeriod = Param(nameof(SlowPeriod), 26)
				.SetDisplay("Slow EMA Period", "Slow EMA period for MACD calculation", "MACD Parameters")
				.SetCanOptimize(true)
				.SetOptimize(20, 30, 2);

			_signalPeriod = Param(nameof(SignalPeriod), 9)
				.SetDisplay("Signal Period", "Signal line period for MACD calculation", "MACD Parameters")
				.SetCanOptimize(true)
				.SetOptimize(7, 12, 1);

			_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
				.SetDisplay("Stop Loss %", "Stop loss as percentage from entry price", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 5.0m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "Data");
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

			// Reset state variables
			_prevMacd = 0;

			// Create MACD indicator with signal line

			var macd = new MovingAverageConvergenceDivergenceSignal
			{
				Macd =
				{
					ShortMa = { Length = FastPeriod },
					LongMa = { Length = SlowPeriod },
				},
				SignalMa = { Length = SignalPeriod }
			};
			// Create subscription and bind MACD indicator
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(macd, ProcessCandle)
				.Start();

			// Configure chart
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, macd);
				DrawOwnTrades(area);
			}

			// Setup protection with stop-loss
			StartProtection(
				new Unit(0), // No take profit
				new Unit(StopLossPercent, UnitTypes.Percent) // Stop loss as percentage of entry price
			);
		}

		/// <summary>
		/// Process candle and check for MACD signals
		/// </summary>
		private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
			if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
			{
				return;
			}

			// Initialize _prevMacd on first formed candle
			if (_prevMacd == 0)
			{
				_prevMacd = macd;
				return;
			}

			// Check if MACD is trending towards zero
			bool isTrendingTowardsZero = false;
			
			if (macd < 0 && macd > _prevMacd)
			{
				// MACD is negative but increasing (moving towards zero from below)
				isTrendingTowardsZero = true;
			}
			else if (macd > 0 && macd < _prevMacd)
			{
				// MACD is positive but decreasing (moving towards zero from above)
				isTrendingTowardsZero = true;
			}

			if (Position == 0)
			{
				// No position - check for entry signals
				if (macd < 0 && isTrendingTowardsZero)
				{
					// MACD is below zero and trending back to zero - buy (long)
					BuyMarket(Volume);
				}
				else if (macd > 0 && isTrendingTowardsZero)
				{
					// MACD is above zero and trending back to zero - sell (short)
					SellMarket(Volume);
				}
			}
			else if (Position > 0)
			{
				// Long position - check for exit signal
				if (macd > signal)
				{
					// MACD crossed above signal line - exit long
					SellMarket(Position);
				}
			}
			else if (Position < 0)
			{
				// Short position - check for exit signal
				if (macd < signal)
				{
					// MACD crossed below signal line - exit short
					BuyMarket(Math.Abs(Position));
				}
			}

			// Update previous MACD value
			_prevMacd = macd;
		}
	}
}
