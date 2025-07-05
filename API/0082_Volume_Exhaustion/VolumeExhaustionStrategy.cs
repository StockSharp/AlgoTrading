using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Volume Exhaustion Strategy.
	/// Looks for volume spikes with corresponding bullish/bearish candles.
	/// </summary>
	public class VolumeExhaustionStrategy : Strategy
	{
		private readonly StrategyParam<int> _volumePeriod;
		private readonly StrategyParam<decimal> _volumeMultiplier;
		private readonly StrategyParam<int> _maPeriod;
		private readonly StrategyParam<Unit> _atrMultiplier;
		private readonly StrategyParam<DataType> _candleType;
		
		private SimpleMovingAverage _ma;
		private AverageTrueRange _atr;
		private SimpleMovingAverage _volumeAvg;

		/// <summary>
		/// Period for volume average calculation.
		/// </summary>
		public int VolumePeriod
		{
			get => _volumePeriod.Value;
			set => _volumePeriod.Value = value;
		}

		/// <summary>
		/// Multiplier to determine volume spike.
		/// </summary>
		public decimal VolumeMultiplier
		{
			get => _volumeMultiplier.Value;
			set => _volumeMultiplier.Value = value;
		}

		/// <summary>
		/// Period for moving average.
		/// </summary>
		public int MaPeriod
		{
			get => _maPeriod.Value;
			set => _maPeriod.Value = value;
		}

		/// <summary>
		/// ATR multiplier for stop-loss calculation.
		/// </summary>
		public Unit AtrMultiplier
		{
			get => _atrMultiplier.Value;
			set => _atrMultiplier.Value = value;
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
		/// Initializes a new instance of the <see cref="VolumeExhaustionStrategy"/>.
		/// </summary>
		public VolumeExhaustionStrategy()
		{
			_volumePeriod = Param(nameof(VolumePeriod), 20)
				.SetDisplay("Volume Average Period", "Period for volume average calculation", "Volume Settings")
				.SetRange(5, 50)
				.SetCanOptimize(true);
				
			_volumeMultiplier = Param(nameof(VolumeMultiplier), 2.0m)
				.SetDisplay("Volume Multiplier", "Multiplier to determine volume spike", "Volume Settings")
				.SetRange(1.5m, 3.0m)
				.SetCanOptimize(true);
				
			_maPeriod = Param(nameof(MaPeriod), 20)
				.SetDisplay("MA Period", "Period for moving average", "Trend Settings")
				.SetRange(10, 50)
				.SetCanOptimize(true);
				
			_atrMultiplier = Param(nameof(AtrMultiplier), new Unit(2, UnitTypes.Absolute))
				.SetDisplay("ATR Multiplier", "Multiplier for ATR stop-loss", "Risk Management")
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

			// Create indicators
			_ma = new SimpleMovingAverage { Length = MaPeriod };
			_atr = new AverageTrueRange { Length = 14 };
			_volumeAvg = new SimpleMovingAverage { Length = VolumePeriod };

			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind indicators and process candles
			subscription
				.Bind(_ma, _atr, _volumeAvg, ProcessCandle)
				.Start();
				
			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _ma);
				DrawOwnTrades(area);
			}
		}

		/// <summary>
		/// Process candle with indicator values.
		/// </summary>
		/// <param name="candle">Candle.</param>
		/// <param name="maValue">Moving average value.</param>
		/// <param name="atrValue">ATR value.</param>
		/// <param name="volumeAvgValue">Volume average value.</param>
		private void ProcessCandle(ICandleMessage candle, decimal? maValue, decimal? atrValue, decimal? volumeAvgValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Determine candle direction
			bool isBullishCandle = candle.ClosePrice > candle.OpenPrice;
			bool isBearishCandle = candle.ClosePrice < candle.OpenPrice;
			
			// Check for volume spike
			bool isVolumeSpike = candle.TotalVolume > volumeAvgValue * VolumeMultiplier;
			
			if (!isVolumeSpike)
				return;
				
			// Long entry: Volume spike with bullish candle
			if (isVolumeSpike && isBullishCandle && candle.ClosePrice > maValue && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				LogInfo($"Long entry: Volume spike ({candle.TotalVolume} > {volumeAvgValue * VolumeMultiplier}) with bullish candle");
				
				// Set stop-loss based on ATR
				decimal stopPrice = candle.ClosePrice - (atrValue * AtrMultiplier.Value);
				StartProtection(null, new Unit(candle.ClosePrice - stopPrice, UnitTypes.Absolute), false, useMarketOrders: true);
			}
			// Short entry: Volume spike with bearish candle
			else if (isVolumeSpike && isBearishCandle && candle.ClosePrice < maValue && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				LogInfo($"Short entry: Volume spike ({candle.TotalVolume} > {volumeAvgValue * VolumeMultiplier}) with bearish candle");
				
				// Set stop-loss based on ATR
				decimal stopPrice = candle.ClosePrice + (atrValue * AtrMultiplier.Value);
				StartProtection(null, new Unit(stopPrice - candle.ClosePrice, UnitTypes.Absolute), false, useMarketOrders: true);
			}
		}
	}
}