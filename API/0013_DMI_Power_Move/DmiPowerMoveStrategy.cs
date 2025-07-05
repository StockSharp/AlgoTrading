using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on DMI (Directional Movement Index) power moves.
	/// It enters long position when +DI exceeds -DI by a specified threshold and ADX is strong.
	/// It enters short position when -DI exceeds +DI by a specified threshold and ADX is strong.
	/// </summary>
	public class DmiPowerMoveStrategy : Strategy
	{
		private readonly StrategyParam<int> _dmiPeriod;
		private readonly StrategyParam<decimal> _diDifferenceThreshold;
		private readonly StrategyParam<decimal> _adxThreshold;
		private readonly StrategyParam<decimal> _adxExitThreshold;
		private readonly StrategyParam<decimal> _atrMultiplier;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// Period for DMI calculation.
		/// </summary>
		public int DmiPeriod
		{
			get => _dmiPeriod.Value;
			set => _dmiPeriod.Value = value;
		}

		/// <summary>
		/// Minimum difference between +DI and -DI to generate a signal.
		/// </summary>
		public decimal DiDifferenceThreshold
		{
			get => _diDifferenceThreshold.Value;
			set => _diDifferenceThreshold.Value = value;
		}

		/// <summary>
		/// Minimum ADX value to consider trend strong enough for entry.
		/// </summary>
		public decimal AdxThreshold
		{
			get => _adxThreshold.Value;
			set => _adxThreshold.Value = value;
		}

		/// <summary>
		/// ADX value below which to exit positions.
		/// </summary>
		public decimal AdxExitThreshold
		{
			get => _adxExitThreshold.Value;
			set => _adxExitThreshold.Value = value;
		}

		/// <summary>
		/// Multiplier for ATR to determine stop-loss distance.
		/// </summary>
		public decimal AtrMultiplier
		{
			get => _atrMultiplier.Value;
			set => _atrMultiplier.Value = value;
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
		/// Initialize the DMI Power Move strategy.
		/// </summary>
		public DmiPowerMoveStrategy()
		{
			_dmiPeriod = Param(nameof(DmiPeriod), 14)
				.SetDisplay("DMI Period", "Period for Directional Movement Index calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 20, 2);

			_diDifferenceThreshold = Param(nameof(DiDifferenceThreshold), 5m)
				.SetDisplay("DI Difference Threshold", "Minimum difference between +DI and -DI for signal", "Trading parameters")
				.SetCanOptimize(true)
				.SetOptimize(3, 10, 1);

			_adxThreshold = Param(nameof(AdxThreshold), 30m)
				.SetDisplay("ADX Threshold", "Minimum ADX value to consider trend strong", "Trading parameters")
				.SetCanOptimize(true)
				.SetOptimize(20, 40, 5);

			_adxExitThreshold = Param(nameof(AdxExitThreshold), 25m)
				.SetDisplay("ADX Exit Threshold", "ADX value below which to exit positions", "Exit parameters")
				.SetCanOptimize(true)
				.SetOptimize(15, 30, 5);

			_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
				.SetDisplay("ATR Multiplier", "Multiplier for ATR to determine stop-loss distance", "Risk parameters")
				.SetCanOptimize(true)
				.SetOptimize(1, 3, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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

			// Create indicators
			var dmi = new AverageDirectionalIndex { Length = DmiPeriod };
			var atr = new AverageTrueRange { Length = DmiPeriod };

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(dmi, atr, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, dmi);
				DrawOwnTrades(area);
			}

			// Start protection with ATR-based stop loss
			StartProtection(
				takeProfit: null,
				stopLoss: new Unit(AtrMultiplier, UnitTypes.Absolute),
				isStopTrailing: true
			);
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var adxTyped = (AverageDirectionalIndexValue)adxValue;

			// Get individual components from DMI indicator value
			if (adxTyped.MovingAverage is not decimal adx ||
				adxTyped.Dx.Plus is not decimal plusDiValue ||
				adxTyped.Dx.Minus is not decimal minusDiValue)
			{
				return;
			}

			// For real implementation, use separate DirectionalIndex indicators with Plus/Minus directions
			// and bind them separately to get actual values

			// Calculate the difference between +DI and -DI
			var diDifference = plusDiValue - minusDiValue;
			
			// Check trading conditions
			var isStrongBullishTrend = diDifference > DiDifferenceThreshold && adx > AdxThreshold;
			var isStrongBearishTrend = diDifference < -DiDifferenceThreshold && adx > AdxThreshold;
			var isWeakTrend = adx < AdxExitThreshold;
			
			// Entry logic
			if (isStrongBullishTrend && Position <= 0)
			{
				// Strong bullish trend - Buy signal
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				LogInfo($"Buy signal: +DI - (-DI) = {diDifference}, ADX = {adx}");
			}
			else if (isStrongBearishTrend && Position >= 0)
			{
				// Strong bearish trend - Sell signal
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				LogInfo($"Sell signal: -DI - (+DI) = {-diDifference}, ADX = {adx}");
			}
			// Exit logic
			else if (isWeakTrend && Position != 0)
			{
				// Trend is weakening - Exit position
				ClosePosition();
				LogInfo($"Exit signal: ADX = {adx} (below threshold {AdxExitThreshold})");
			}
		}
	}
}