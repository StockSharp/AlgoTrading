using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Donchian Channels and CCI indicators (#202)
	/// </summary>
	public class DonchianCciStrategy : Strategy
	{
		private readonly StrategyParam<int> _donchianPeriod;
		private readonly StrategyParam<int> _cciPeriod;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// Period for Donchian Channel
		/// </summary>
		public int DonchianPeriod
		{
			get => _donchianPeriod.Value;
			set => _donchianPeriod.Value = value;
		}

		/// <summary>
		/// Period for CCI indicator
		/// </summary>
		public int CciPeriod
		{
			get => _cciPeriod.Value;
			set => _cciPeriod.Value = value;
		}

		/// <summary>
		/// Stop-loss percentage
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Candle type for strategy
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public DonchianCciStrategy()
		{
			_donchianPeriod = Param(nameof(DonchianPeriod), 20)
				.SetRange(10, 50, 5)
				.SetDisplay("Donchian Period", "Period for Donchian Channel", "Indicators")
				.SetCanOptimize(true);

			_cciPeriod = Param(nameof(CciPeriod), 20)
				.SetRange(10, 50, 5)
				.SetDisplay("CCI Period", "Period for CCI indicator", "Indicators")
				.SetCanOptimize(true);

			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetRange(0.5m, 5m, 0.5m)
				.SetDisplay("Stop-Loss %", "Stop-loss percentage from entry price", "Risk Management")
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

			// Initialize Indicators
			var donchian = new DonchianChannels { Length = DonchianPeriod };
			var cci = new CommodityChannelIndex { Length = CciPeriod };

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(donchian, (candle, donchianValues) =>
				{
					// Get CCI value
					var cciValue = cci.Process(candle).GetValue<decimal>();

					// Get Donchian values
					var upperBand = donchianValues[0].GetValue<decimal>();
					var middleBand = donchianValues[1].GetValue<decimal>();
					var lowerBand = donchianValues[2].GetValue<decimal>();

					ProcessIndicators(candle, upperBand, middleBand, lowerBand, cciValue);
				})
				.Start();

			// Enable stop-loss protection
			StartProtection(new Unit(StopLossPercent, UnitTypes.Percent), default);

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, donchian);
				DrawIndicator(area, cci);
				DrawOwnTrades(area);
			}
		}

		private void ProcessIndicators(ICandleMessage candle, decimal upperBand, decimal middleBand, 
			decimal lowerBand, decimal cciValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var price = candle.ClosePrice;

			// Trading logic:
			// Long: Price > Donchian Upper && CCI < -100 (breakout up with oversold conditions)
			// Short: Price < Donchian Lower && CCI > 100 (breakout down with overbought conditions)
			
			if (price > upperBand && cciValue < -100 && Position <= 0)
			{
				// Buy signal - breakout up with oversold conditions
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			else if (price < lowerBand && cciValue > 100 && Position >= 0)
			{
				// Sell signal - breakout down with overbought conditions
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}
			// Exit conditions
			else if (Position > 0 && price < middleBand)
			{
				// Exit long position when price falls below middle band
				SellMarket(Position);
			}
			else if (Position < 0 && price > middleBand)
			{
				// Exit short position when price rises above middle band
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}
