using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Donchian Channel and RSI.
	/// Enters long when price breaks above upper Donchian Channel and RSI < 30 (oversold).
	/// Enters short when price breaks below lower Donchian Channel and RSI > 70 (overbought).
	/// Exits when price reverts to the middle line of the Donchian Channel.
	/// </summary>
	public class DonchianRsiStrategy : Strategy
	{
		private readonly StrategyParam<int> _donchianPeriod;
		private readonly StrategyParam<int> _rsiPeriod;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		private Highest _highest;
		private Lowest _lowest;
		private RelativeStrengthIndex _rsi;

		/// <summary>
		/// Period for Donchian Channel calculation.
		/// </summary>
		public int DonchianPeriod
		{
			get => _donchianPeriod.Value;
			set => _donchianPeriod.Value = value;
		}

		/// <summary>
		/// Period for RSI calculation.
		/// </summary>
		public int RsiPeriod
		{
			get => _rsiPeriod.Value;
			set => _rsiPeriod.Value = value;
		}

		/// <summary>
		/// Stop loss percentage value.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Candle type for strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DonchianRsiStrategy"/>.
		/// </summary>
		public DonchianRsiStrategy()
		{
			_donchianPeriod = Param(nameof(DonchianPeriod), 20)
				.SetDisplayName("Donchian Period")
				.SetDescription("Period for Donchian Channel calculation")
				.SetCategories("Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_rsiPeriod = Param(nameof(RsiPeriod), 14)
				.SetDisplayName("RSI Period")
				.SetDescription("Period for Relative Strength Index calculation")
				.SetCategories("Indicators")
				.SetCanOptimize(true)
				.SetOptimize(7, 21, 7);

			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetDisplayName("Stop Loss (%)")
				.SetDescription("Stop loss percentage from entry price")
				.SetCategories("Risk Management");

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
				.SetDisplayName("Candle Type")
				.SetDescription("Timeframe of data for strategy")
				.SetCategories("General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
		{
			return [(Security, CandleType)];
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create indicators
			_highest = new Highest { Length = DonchianPeriod };
			_lowest = new Lowest { Length = DonchianPeriod };
			_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

			// Enable position protection
			StartProtection(new Unit(StopLossPercent, UnitTypes.Percent), new Unit(StopLossPercent, UnitTypes.Percent));

			// Create subscription
			var subscription = SubscribeCandles(CandleType);

			// Process candles with indicators
			subscription
				.Bind(_highest, _lowest, _rsi, ProcessCandle)
				.Start();

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _highest);
				DrawIndicator(area, _lowest);
				DrawOwnTrades(area);

				// RSI in separate area
				var rsiArea = CreateChartArea();
				if (rsiArea != null)
				{
					DrawIndicator(rsiArea, _rsi);
				}
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue, decimal rsiValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Calculate middle line of Donchian Channel
			var middleLine = (highestValue + lowestValue) / 2;

			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Trading logic
			if (candle.ClosePrice > highestValue && rsiValue < 30 && Position <= 0)
			{
				// Price breaks above upper Donchian Channel with RSI oversold - go long
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (candle.ClosePrice < lowestValue && rsiValue > 70 && Position >= 0)
			{
				// Price breaks below lower Donchian Channel with RSI overbought - go short
				SellMarket(Volume + Math.Abs(Position));
			}
			else if (Position > 0 && candle.ClosePrice < middleLine)
			{
				// Exit long position when price falls below middle line
				ClosePosition();
			}
			else if (Position < 0 && candle.ClosePrice > middleLine)
			{
				// Exit short position when price rises above middle line
				ClosePosition();
			}
		}
	}
}