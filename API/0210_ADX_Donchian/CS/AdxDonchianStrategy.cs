using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on ADX and Donchian Channel indicators
	/// </summary>
	public class AdxDonchianStrategy : Strategy
	{
		private readonly StrategyParam<int> _adxPeriod;
		private readonly StrategyParam<int> _donchianPeriod;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _adxThreshold;
		private readonly StrategyParam<decimal> _multiplier;

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
		/// ADX threshold for strong trend detection
		/// </summary>
		public int AdxThreshold
		{
			get => _adxThreshold.Value;
			set => _adxThreshold.Value = value;
		}

		/// <summary>
		 /// Multiplier for Donchian Channel border sensitivity (in percent, e.g. 0.1 for 0.1%)
		 /// </summary>
		public decimal Multiplier
		{
			get => _multiplier.Value;
			set => _multiplier.Value = value;
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

			_donchianPeriod = Param(nameof(DonchianPeriod), 5)
				.SetRange(5, 50)
				.SetDisplay("Donchian Period", "Period for Donchian Channel", "Indicators")
				.SetCanOptimize(true);

			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetRange(0.5m, 5m)
				.SetDisplay("Stop-Loss %", "Stop-loss percentage from entry price", "Risk Management")
				.SetCanOptimize(true);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_adxThreshold = Param(nameof(AdxThreshold), 10)
				.SetRange(5, 40)
				.SetDisplay("ADX Threshold", "ADX value for strong trend detection", "Indicators")
				.SetCanOptimize(true);

			_multiplier = Param(nameof(Multiplier), 0.1m)
				.SetRange(0m, 1m)
				.SetDisplay("Multiplier %", "Sensitivity to Donchian Channel border (percent)", "Indicators")
				.SetCanOptimize(true);
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
				.BindEx(donchian, adx, ProcessCandle)
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

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue, IIndicatorValue adxValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Process ADX
			var typedAdx = (AverageDirectionalIndexValue)adxValue;

			// Get Donchian Channel values
			var typedDonchian = (DonchianChannelsValue)donchianValue;
			var upperBand = typedDonchian.UpperBand;
			var middleBand = typedDonchian.Middle;
			var lowerBand = typedDonchian.LowerBand;

			var price = candle.ClosePrice;

			// Trading logic:
			// Long: ADX > AdxThreshold && Price >= upperBorder (strong trend with breakout up)
			// Short: ADX > AdxThreshold && Price <= lowerBorder (strong trend with breakout down)
			
			var strongTrend = typedAdx.MovingAverage > AdxThreshold;

			var upperBorder = upperBand * (1 - Multiplier / 100);
			var lowerBorder = lowerBand * (1 + Multiplier / 100);

			if (strongTrend && price >= upperBorder && Position <= 0)
			{
				// Buy signal - Strong trend with Donchian Channel breakout up (with multiplier)
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			else if (strongTrend && price <= lowerBorder && Position >= 0)
			{
				// Sell signal - Strong trend with Donchian Channel breakout down (with multiplier)
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}
			// Exit conditions - ADX weakness
			else if (Position != 0 && typedAdx.MovingAverage < AdxThreshold - 5)
			{
				// Exit position when ADX falls below (threshold - 5)
				if (Position > 0)
					SellMarket(Position);
				else
					BuyMarket(Math.Abs(Position));
			}
		}
	}
}
