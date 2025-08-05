using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Average Directional Index (ADX) trend.
	/// It enters long position when ADX > 25 and price > MA, and short position when ADX > 25 and price < MA.
	/// </summary>
	public class AdxTrendStrategy : Strategy
	{
		private readonly StrategyParam<int> _adxPeriod;
		private readonly StrategyParam<int> _maPeriod;
		private readonly StrategyParam<decimal> _atrMultiplier;
		private readonly StrategyParam<int> _adxExitThreshold;
		private readonly StrategyParam<DataType> _candleType;
		
		// Current trend state
		private bool _adxAboveThreshold;
		private decimal _prevAdxValue;
		private decimal _prevMaValue;

		/// <summary>
		/// ADX period.
		/// </summary>
		public int AdxPeriod
		{
			get => _adxPeriod.Value;
			set => _adxPeriod.Value = value;
		}

		/// <summary>
		/// Moving Average period.
		/// </summary>
		public int MaPeriod
		{
			get => _maPeriod.Value;
			set => _maPeriod.Value = value;
		}

		/// <summary>
		/// ATR multiplier for stop loss.
		/// </summary>
		public decimal AtrMultiplier
		{
			get => _atrMultiplier.Value;
			set => _atrMultiplier.Value = value;
		}

		/// <summary>
		/// ADX threshold to exit position.
		/// </summary>
		public int AdxExitThreshold
		{
			get => _adxExitThreshold.Value;
			set => _adxExitThreshold.Value = value;
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
		/// Initialize the ADX Trend strategy.
		/// </summary>
		public AdxTrendStrategy()
		{
			_adxPeriod = Param(nameof(AdxPeriod), 14)
				.SetDisplay("ADX Period", "Period for calculating ADX indicator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 2);

			_maPeriod = Param(nameof(MaPeriod), 50)
				.SetDisplay("MA Period", "Period for calculating Moving Average", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(20, 100, 10);

			_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
				.SetDisplay("ATR Multiplier", "Multiplier for stop-loss based on ATR", "Risk parameters")
				.SetCanOptimize(true)
				.SetOptimize(1, 3, 0.5m);

			_adxExitThreshold = Param(nameof(AdxExitThreshold), 20)
				.SetDisplay("ADX Exit Threshold", "ADX level below which to exit position", "Exit parameters")
				.SetCanOptimize(true)
				.SetOptimize(15, 25, 1);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
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
			_adxAboveThreshold = default;
			_prevAdxValue = default;
			_prevMaValue = default;

		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create indicators
			var adx = new AverageDirectionalIndex { Length = AdxPeriod };
			var ma = new SimpleMovingAverage { Length = MaPeriod };
			var atr = new AverageTrueRange { Length = AdxPeriod };

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(adx, ma, atr, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, adx);
				DrawIndicator(area, ma);
				DrawOwnTrades(area);
			}

			// Start protection for positions
			StartProtection(
				takeProfit: null,
				stopLoss: new Unit(AtrMultiplier, UnitTypes.Absolute),
				isStopTrailing: true
			);
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue maValue, IIndicatorValue atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var adxTyped = (AverageDirectionalIndexValue)adxValue;

			if (adxTyped.MovingAverage is not decimal adxMa)
				return;

			// Check ADX threshold for entry conditions
			var isAdxEnoughForEntry = adxMa > 25;
			
			// Check ADX threshold for exit conditions
			var isAdxBelowExit = adxMa < AdxExitThreshold;
			
			// Current price relative to MA
			var isPriceAboveMa = candle.ClosePrice > maValue.ToDecimal();

			// Store ADX state
			_adxAboveThreshold = isAdxEnoughForEntry;

			// Trading logic
			if (isAdxBelowExit && Position != 0)
			{
				// Exit position when ADX weakens
				ClosePosition();
				LogInfo($"Exiting position at {candle.ClosePrice}. ADX = {adxMa} (below threshold {AdxExitThreshold})");
			}
			else if (isAdxEnoughForEntry)
			{
				var volume = Volume + Math.Abs(Position);

				// Long entry
				if (isPriceAboveMa && Position <= 0)
				{
					BuyMarket(volume);
					LogInfo($"Buy signal: ADX = {adxMa}, Price = {candle.ClosePrice}, MA = {maValue}");
				}
				// Short entry
				else if (!isPriceAboveMa && Position >= 0)
				{
					SellMarket(volume);
					LogInfo($"Sell signal: ADX = {adxMa}, Price = {candle.ClosePrice}, MA = {maValue}");
				}
			}

			// Update previous values
			_prevAdxValue = adxMa;
			_prevMaValue = maValue.ToDecimal();
		}
	}
}