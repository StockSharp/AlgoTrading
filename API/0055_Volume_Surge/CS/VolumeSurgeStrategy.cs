using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Volume Surge strategy
	/// Long entry: Volume exceeds average volume by k times and price is above MA
	/// Short entry: Volume exceeds average volume by k times and price is below MA
	/// Exit when volume falls below average
	/// </summary>
	public class VolumeSurgeStrategy : Strategy
	{
		private readonly StrategyParam<int> _maPeriod;
		private readonly StrategyParam<int> _volumeAvgPeriod;
		private readonly StrategyParam<decimal> _volumeSurgeMultiplier;
		private readonly StrategyParam<DataType> _candleType;
		private SimpleMovingAverage _volumeMA;

		/// <summary>
		/// MA Period
		/// </summary>
		public int MAPeriod
		{
			get => _maPeriod.Value;
			set => _maPeriod.Value = value;
		}

		/// <summary>
		/// Volume Average Period
		/// </summary>
		public int VolumeAvgPeriod
		{
			get => _volumeAvgPeriod.Value;
			set => _volumeAvgPeriod.Value = value;
		}

		/// <summary>
		/// Volume Surge Multiplier (k)
		/// </summary>
		public decimal VolumeSurgeMultiplier
		{
			get => _volumeSurgeMultiplier.Value;
			set => _volumeSurgeMultiplier.Value = value;
		}

		/// <summary>
		/// Candle type for strategy calculation
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initialize <see cref="VolumeSurgeStrategy"/>.
		/// </summary>
		public VolumeSurgeStrategy()
		{
			_maPeriod = Param(nameof(MAPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("MA Period", "Period for Moving Average calculation", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 10);

			_volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Volume Average Period", "Period for Average Volume calculation", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_volumeSurgeMultiplier = Param(nameof(VolumeSurgeMultiplier), 2.0m)
				.SetRange(1.0m, decimal.MaxValue)
				.SetDisplay("Volume Surge Multiplier", "Minimum volume increase multiplier to generate signal", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 3.0m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters");
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
			_volumeMA = null;
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

				// Create indicators
				var ma = new SimpleMovingAverage { Length = MAPeriod };

				_volumeMA = new SimpleMovingAverage { Length = VolumeAvgPeriod };

				// Create subscription
				var subscription = SubscribeCandles(CandleType);

				// Regular price MA binding for signals and visualization
				subscription
					.Bind(ma, ProcessCandle)
					.Start();

				// Configure protection
				StartProtection(
					takeProfit: new Unit(3, UnitTypes.Percent),
					stopLoss: new Unit(2, UnitTypes.Percent)
				);

				// Setup chart visualization
				var area = CreateChartArea();
				if (area != null)
				{
					DrawCandles(area, subscription);
					DrawIndicator(area, ma);
					DrawOwnTrades(area);
				}
			}

		private void ProcessCandle(ICandleMessage candle, decimal maValue)
		{
			var volumeMAValue = _volumeMA.Process(candle.TotalVolume, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal();

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Calculate volume surge ratio
			var volumeSurgeRatio = candle.TotalVolume / volumeMAValue;
			var isVolumeSurge = volumeSurgeRatio >= VolumeSurgeMultiplier;

			// Log current values
			LogInfo($"Candle Close: {candle.ClosePrice}, MA: {maValue}, Volume: {candle.TotalVolume}");
			LogInfo($"Volume MA: {volumeMAValue}, Volume Surge Ratio: {volumeSurgeRatio:P2}");
			LogInfo($"Is Volume Surge: {isVolumeSurge}, Threshold: {VolumeSurgeMultiplier}");

			// Trading logic:
			// Check for volume surge
			if (isVolumeSurge)
			{
				// Long: Volume surge and price above MA
				if (candle.ClosePrice > maValue && Position <= 0)
				{
					LogInfo($"Buy Signal: Volume Surge ({volumeSurgeRatio:P2}) and Price ({candle.ClosePrice}) > MA ({maValue})");
					BuyMarket(Volume + Math.Abs(Position));
				}
				// Short: Volume surge and price below MA
				else if (candle.ClosePrice < maValue && Position >= 0)
				{
					LogInfo($"Sell Signal: Volume Surge ({volumeSurgeRatio:P2}) and Price ({candle.ClosePrice}) < MA ({maValue})");
					SellMarket(Volume + Math.Abs(Position));
				}
			}

			// Exit logic: Volume falls below average
			if (candle.TotalVolume < volumeMAValue)
			{
				if (Position > 0)
				{
					LogInfo($"Exit Long: Volume ({candle.TotalVolume}) < Average Volume ({volumeMAValue})");
					SellMarket(Math.Abs(Position));
				}
				else if (Position < 0)
				{
					LogInfo($"Exit Short: Volume ({candle.TotalVolume}) < Average Volume ({volumeMAValue})");
					BuyMarket(Math.Abs(Position));
				}
			}
		}
	}
}