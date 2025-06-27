using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on ADX and Donchian Channel indicators (#210)
	/// </summary>
	public class AdxDonchianStrategy : Strategy
	{
		private readonly StrategyParam<int> _adxPeriod;
		private readonly StrategyParam<int> _donchianPeriod;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// ADX period
		/// </summary>
		public int AdxPeriod
		{
			get => _adxPeriod.Value;
			set => _adxPeriod.Value = value;
		}

		/// <summary>
		/// Donchian Channel period
		/// </summary>
		public int DonchianPeriod
		{
			get => _donchianPeriod.Value;
			set => _donchianPeriod.Value = value;
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
		public AdxDonchianStrategy()
		{
			_adxPeriod = Param(nameof(AdxPeriod), 14)
				.SetRange(7, 28)
				.SetDisplay("ADX Period", "Period for ADX indicator", "Indicators")
				.SetCanOptimize(true);

			_donchianPeriod = Param(nameof(DonchianPeriod), 20)
				.SetRange(10, 50)
				.SetDisplay("Donchian Period", "Period for Donchian Channel", "Indicators")
				.SetCanOptimize(true);

			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetRange(0.5m, 5m)
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

			// Initialize indicators
			var adx = new AverageDirectionalIndex { Length = AdxPeriod };
			var donchian = new DonchianChannels { Length = DonchianPeriod };

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(donchian, (candle, donchianValues) =>
				{
					// Process ADX
					var adxValue = adx.Process(candle).ToDecimal();
					
					// Get Donchian Channel values
					var upperBand = donchianValues[0].ToDecimal();
					var middleBand = donchianValues[1].ToDecimal();
					var lowerBand = donchianValues[2].ToDecimal();

					ProcessIndicators(candle, adxValue, upperBand, middleBand, lowerBand);
				})
				.Start();
			
			// Enable percentage-based stop-loss protection
			StartProtection(new Unit(StopLossPercent, UnitTypes.Percent), default);

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, adx);
				DrawIndicator(area, donchian);
				DrawOwnTrades(area);
			}
		}

		private void ProcessIndicators(ICandleMessage candle, decimal adxValue, decimal upperBand, 
			decimal middleBand, decimal lowerBand)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var price = candle.ClosePrice;

			// Trading logic:
			// Long: ADX > 25 && Price > Donchian upper band (strong trend with breakout up)
			// Short: ADX > 25 && Price < Donchian lower band (strong trend with breakout down)
			
			var strongTrend = adxValue > 25;

			if (strongTrend && price > upperBand && Position <= 0)
			{
				// Buy signal - Strong trend with Donchian Channel breakout up
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			else if (strongTrend && price < lowerBand && Position >= 0)
			{
				// Sell signal - Strong trend with Donchian Channel breakout down
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}
			// Exit conditions - ADX weakness
			else if (Position != 0 && adxValue < 20)
			{
				// Exit position when ADX falls below 20 (trend weakening)
				if (Position > 0)
					SellMarket(Position);
				else
					BuyMarket(Math.Abs(Position));
			}
		}
	}
}
