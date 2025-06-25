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
	/// Strategy that detects unusually high volume (dark pool prints) and enters positions based on that.
	/// </summary>
	public class DarkPoolPrintsStrategy : Strategy
	{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _volumePeriod;
		private readonly StrategyParam<decimal> _volumeMultiplier;
		private readonly StrategyParam<int> _maPeriod;
		private readonly StrategyParam<decimal> _atrMultiplier;

		private SimpleMovingAverage _ma;
		private SimpleMovingAverage _volumeAverage;
		private AverageDirectionalIndex _adx; // To ensure we're in a trending market
		private AverageTrueRange _atr;

		/// <summary>
		/// Candle type and timeframe for the strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Period for volume average calculation.
		/// </summary>
		public int VolumePeriod
		{
			get => _volumePeriod.Value;
			set => _volumePeriod.Value = value;
		}

		/// <summary>
		/// Multiplier to determine significant volume.
		/// Volume > Average(Volume) * VolumeMultiplier is considered significant.
		/// </summary>
		public decimal VolumeMultiplier
		{
			get => _volumeMultiplier.Value;
			set => _volumeMultiplier.Value = value;
		}

		/// <summary>
		/// Period for moving average calculation.
		/// </summary>
		public int MaPeriod
		{
			get => _maPeriod.Value;
			set => _maPeriod.Value = value;
		}

		/// <summary>
		/// ATR multiplier for stop-loss calculation.
		/// </summary>
		public decimal AtrMultiplier
		{
			get => _atrMultiplier.Value;
			set => _atrMultiplier.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DarkPoolPrintsStrategy"/>.
		/// </summary>
		public DarkPoolPrintsStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use for analysis", "General");

			_volumePeriod = Param(nameof(VolumePeriod), 20)
				.SetDisplay("Volume Period", "Period for volume average calculation", "Volume")
				.SetRange(5, 50);

			_volumeMultiplier = Param(nameof(VolumeMultiplier), 2m)
				.SetDisplay("Volume Multiplier", "Trigger multiplier for volume significance", "Volume")
				.SetRange(1.5m, 5m);

			_maPeriod = Param(nameof(MaPeriod), 20)
				.SetDisplay("MA Period", "Period for moving average calculation", "Trend")
				.SetRange(5, 50);

			_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
				.SetDisplay("ATR Multiplier", "Multiplier for ATR-based stop-loss", "Protection")
				.SetRange(1m, 5m);
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
			_ma = new SimpleMovingAverage { Length = MaPeriod };
			_volumeAverage = new SimpleMovingAverage { Length = VolumePeriod };
			_adx = new AverageDirectionalIndex { Length = 14 }; // Standard ADX period
			_atr = new AverageTrueRange { Length = 14 }; // Standard ATR period

			// Create subscription for candles
			var subscription = SubscribeCandles(CandleType);

			// Bind indicators and processor
			subscription
				.Bind(_ma, _volumeAverage, _adx, _atr, ProcessCandle)
				.Start();

			// Enable stop-loss protection
			StartProtection(new Unit(0), new Unit(AtrMultiplier, UnitTypes.Absolute));

			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _ma);
				DrawIndicator(area, _adx);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal ma, decimal volumeAvg, decimal adx, decimal atr)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Check if we have a strong trend (ADX > 25)
			bool isStrongTrend = adx > 25;
			
			// Check if current volume is significantly higher than average
			bool isHighVolume = candle.TotalVolume > volumeAvg * VolumeMultiplier;
			
			if (!isHighVolume || !isStrongTrend)
				return;

			// Determine if the candle is bullish or bearish
			bool isBullish = candle.ClosePrice > candle.OpenPrice;
			
			// Determine if price is above or below the moving average
			bool isAboveMA = candle.ClosePrice > ma;
			bool isBelowMA = candle.ClosePrice < ma;

			// Entry rules for long or short positions
			if (isBullish && isAboveMA && Position <= 0)
			{
				// Bullish candle + high volume + price above MA = Long signal
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				
				LogInfo($"Dark Pool Print detected. Bullish candle with volume {candle.TotalVolume} (avg: {volumeAvg}). Buying at {candle.ClosePrice}");
			}
			else if (!isBullish && isBelowMA && Position >= 0)
			{
				// Bearish candle + high volume + price below MA = Short signal
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				
				LogInfo($"Dark Pool Print detected. Bearish candle with volume {candle.TotalVolume} (avg: {volumeAvg}). Selling at {candle.ClosePrice}");
			}
		}
	}
}