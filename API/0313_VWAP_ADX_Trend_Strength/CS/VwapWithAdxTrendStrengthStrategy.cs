using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on VWAP with ADX Trend Strength.
	/// </summary>
	public class VwapWithAdxTrendStrengthStrategy : Strategy
	{
		private readonly StrategyParam<int> _adxPeriod;
		private readonly StrategyParam<decimal> _adxThreshold;
		private readonly StrategyParam<DataType> _candleType;
		private AverageDirectionalIndex _adx;
		private VolumeWeightedMovingAverage _vwap;

		/// <summary>
		/// ADX period parameter.
		/// </summary>
		public int AdxPeriod
		{
			get => _adxPeriod.Value;
			set => _adxPeriod.Value = value;
		}

		/// <summary>
		/// ADX threshold parameter.
		/// </summary>
		public decimal AdxThreshold
		{
			get => _adxThreshold.Value;
			set => _adxThreshold.Value = value;
		}

		/// <summary>
		/// Candle type parameter.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public VwapWithAdxTrendStrengthStrategy()
		{
			_adxPeriod = Param(nameof(AdxPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(7, 28, 7);

			_adxThreshold = Param(nameof(AdxThreshold), 25m)
				.SetRange(10m, decimal.MaxValue)
				.SetDisplay("ADX Threshold", "Threshold for strong trend identification", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(15, 35, 5);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return [(Security, CandleType)];
		}

		protected override void OnReseted()
		{
			base.OnReseted();

			_adx?.Reset();
			_vwap?.Reset();
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create indicators
			_adx = new AverageDirectionalIndex { Length = AdxPeriod };
			_vwap = new VolumeWeightedMovingAverage();

			// Subscribe to candles and bind indicators
			var subscription = SubscribeCandles(CandleType);

			subscription
				.BindEx(_adx, _vwap, ProcessCandle)
				.Start();

			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _adx);
				DrawOwnTrades(area);
			}

			// Setup position protection
			StartProtection(
				takeProfit: new Unit(2, UnitTypes.Percent),
				stopLoss: new Unit(1, UnitTypes.Percent)
			);
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue vwapValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var adxTyped = (AverageDirectionalIndexValue)adxValue;

			// Extract values from ADX composite indicator
			var adx = adxTyped.MovingAverage;	 // ADX value
			var diPlus = adxTyped.Dx.Plus;  // +DI value
			var diMinus = adxTyped.Dx.Minus; // -DI value
			
			// Get VWAP
			var vwap = vwapValue.ToDecimal();
			
			// Check for strong trend
			var isStrongTrend = adx > AdxThreshold;
			
			// Check directional indicators
			var isBullishTrend = diPlus > diMinus;
			var isBearishTrend = diMinus > diPlus;
			
			// Check VWAP position
			var isAboveVwap = candle.ClosePrice > vwap;
			var isBelowVwap = candle.ClosePrice < vwap;
			
			// Trading logic
			if (isStrongTrend && isBullishTrend && isAboveVwap && Position <= 0)
			{
				// Strong bullish trend above VWAP - Go long
				CancelActiveOrders();
				
				// Calculate position size
				var volume = Volume + Math.Abs(Position);
				
				// Enter long position
				BuyMarket(volume);
			}
			else if (isStrongTrend && isBearishTrend && isBelowVwap && Position >= 0)
			{
				// Strong bearish trend below VWAP - Go short
				CancelActiveOrders();
				
				// Calculate position size
				var volume = Volume + Math.Abs(Position);
				
				// Enter short position
				SellMarket(volume);
			}
			
			// Exit logic - when ADX drops below threshold (trend weakens)
			if (adx < 20)
			{
				// Close position
				ClosePosition();
			}
		}
	}
}
