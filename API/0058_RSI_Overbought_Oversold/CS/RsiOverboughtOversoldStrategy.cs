using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// RSI Overbought/Oversold strategy that buys when RSI is oversold and sells when RSI is overbought.
	/// </summary>
	public class RsiOverboughtOversoldStrategy : Strategy
	{
		private readonly StrategyParam<int> _rsiPeriod;
		private readonly StrategyParam<int> _overboughtLevel;
		private readonly StrategyParam<int> _oversoldLevel;
		private readonly StrategyParam<int> _neutralLevel;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLossPercent;

		/// <summary>
		/// RSI calculation period.
		/// </summary>
		public int RsiPeriod
		{
			get => _rsiPeriod.Value;
			set => _rsiPeriod.Value = value;
		}

		/// <summary>
		/// RSI level considered overbought.
		/// </summary>
		public int OverboughtLevel
		{
			get => _overboughtLevel.Value;
			set => _overboughtLevel.Value = value;
		}

		/// <summary>
		/// RSI level considered oversold.
		/// </summary>
		public int OversoldLevel
		{
			get => _oversoldLevel.Value;
			set => _oversoldLevel.Value = value;
		}

		/// <summary>
		/// RSI neutral level for exiting positions.
		/// </summary>
		public int NeutralLevel
		{
			get => _neutralLevel.Value;
			set => _neutralLevel.Value = value;
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
		/// Stop-loss percentage from entry price.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RsiOverboughtOversoldStrategy"/>.
		/// </summary>
		public RsiOverboughtOversoldStrategy()
		{
			_rsiPeriod = Param(nameof(RsiPeriod), 14)
				.SetRange(5, 30)
				.SetDisplay("RSI Period", "Number of bars used in RSI calculation", "Indicator Parameters")
				.SetCanOptimize(true);

			_overboughtLevel = Param(nameof(OverboughtLevel), 70)
				.SetRange(60, 80)
				.SetDisplay("Overbought Level", "RSI level considered overbought", "Signal Parameters")
				.SetCanOptimize(true);

			_oversoldLevel = Param(nameof(OversoldLevel), 30)
				.SetRange(20, 40)
				.SetDisplay("Oversold Level", "RSI level considered oversold", "Signal Parameters")
				.SetCanOptimize(true);

			_neutralLevel = Param(nameof(NeutralLevel), 50)
				.SetRange(45, 55)
				.SetDisplay("Neutral Level", "RSI level for exiting positions", "Signal Parameters")
				.SetCanOptimize(true);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
				.SetRange(0.5m, 5.0m)
				.SetDisplay("Stop Loss %", "Percentage-based stop loss from entry", "Risk Management")
				.SetCanOptimize(true);
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
			}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

				// Create RSI indicator
				var rsi = new RelativeStrengthIndex
				{
					Length = RsiPeriod
				};

				// Create candle subscription
				var subscription = SubscribeCandles(CandleType);

				// Bind RSI indicator to candles
				subscription
					.Bind(rsi, ProcessCandle)
					.Start();

				// Enable position protection
				StartProtection(
					new Unit(0, UnitTypes.Absolute), // No take profit (will exit at neutral RSI)
					new Unit(StopLossPercent, UnitTypes.Percent), // Stop loss at defined percentage
					false // No trailing stop
				);

				// Setup chart visualization if available
				var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, rsi);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			LogInfo($"RSI value: {rsiValue}, Position: {Position}");

			// Trading logic
			if (rsiValue <= OversoldLevel && Position <= 0)
			{
			// RSI indicates oversold condition - Buy signal
			if (Position < 0)
				{
					// Close any existing short position
					BuyMarket(Math.Abs(Position));
					LogInfo($"Closed short position at RSI {rsiValue}");
				}

				// Open new long position
				BuyMarket(Volume);
				LogInfo($"Buy signal: RSI {rsiValue} is below oversold level {OversoldLevel}");
			}
			else if (rsiValue >= OverboughtLevel && Position >= 0)
			{
				// RSI indicates overbought condition - Sell signal
				if (Position > 0)
				{
					// Close any existing long position
					SellMarket(Position);
					LogInfo($"Closed long position at RSI {rsiValue}");
				}

				// Open new short position
				SellMarket(Volume);
				LogInfo($"Sell signal: RSI {rsiValue} is above overbought level {OverboughtLevel}");
			}
			else if (Position > 0 && rsiValue >= NeutralLevel)
			{
				// Exit long position when RSI returns to neutral
				SellMarket(Position);
				LogInfo($"Exit long: RSI {rsiValue} returned to neutral level {NeutralLevel}");
			}
			else if (Position < 0 && rsiValue <= NeutralLevel)
			{
				// Exit short position when RSI returns to neutral
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exit short: RSI {rsiValue} returned to neutral level {NeutralLevel}");
			}
		}
	}
}