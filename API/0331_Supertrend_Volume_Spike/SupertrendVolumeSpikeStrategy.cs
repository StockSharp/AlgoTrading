using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that trades based on Supertrend indicator with volume confirmation.
	/// Enters position when price crosses Supertrend line with high volume.
	/// </summary>
	public class SupertrendVolumeStrategy : Strategy
	{
		private readonly StrategyParam<int> _supertrendPeriod;
		private readonly StrategyParam<decimal> _supertrendMultiplier;
		private readonly StrategyParam<int> _volumeLookback;
		private readonly StrategyParam<decimal> _volumeDeviationMultiplier;
		private readonly StrategyParam<DataType> _candleType;

		private decimal _prevClosePrice;
		private bool? _wasBelowSupertrend;
		private decimal _avgVolume;
		private decimal _volumeStdDev;

		/// <summary>
		/// Strategy parameter: Supertrend period.
		/// </summary>
		public int SupertrendPeriod
		{
			get => _supertrendPeriod.Value;
			set => _supertrendPeriod.Value = value;
		}

		/// <summary>
		/// Strategy parameter: Supertrend multiplier.
		/// </summary>
		public decimal SupertrendMultiplier
		{
			get => _supertrendMultiplier.Value;
			set => _supertrendMultiplier.Value = value;
		}

		/// <summary>
		/// Strategy parameter: Volume lookback period.
		/// </summary>
		public int VolumeLookback
		{
			get => _volumeLookback.Value;
			set => _volumeLookback.Value = value;
		}

		/// <summary>
		/// Strategy parameter: Volume deviation multiplier.
		/// </summary>
		public decimal VolumeDeviationMultiplier
		{
			get => _volumeDeviationMultiplier.Value;
			set => _volumeDeviationMultiplier.Value = value;
		}

		/// <summary>
		/// Strategy parameter: Candle type.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public SupertrendVolumeStrategy()
		{
			_supertrendPeriod = Param(nameof(SupertrendPeriod), 10)
				.SetGreaterThanZero()
				.SetDisplay("Supertrend Period", "ATR period for Supertrend indicator", "Indicator Settings");

			_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3m)
				.SetGreaterThan(0)
				.SetDisplay("Supertrend Multiplier", "ATR multiplier for Supertrend indicator", "Indicator Settings");

			_volumeLookback = Param(nameof(VolumeLookback), 20)
				.SetGreaterThanZero()
				.SetDisplay("Volume Lookback", "Number of periods to calculate volume average and deviation", "Volume Settings");

			_volumeDeviationMultiplier = Param(nameof(VolumeDeviationMultiplier), 2m)
				.SetGreaterThan(0)
				.SetDisplay("Volume Deviation Multiplier", "Standard deviation multiplier for volume spike detection", "Volume Settings");

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security security, DataType dataType)> GetWorkingSecurities()
		{
			return new[] { (Security, CandleType) };
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Reset state variables
			_prevClosePrice = 0;
			_wasBelowSupertrend = null;
			_avgVolume = 0;
			_volumeStdDev = 0;

			// Create Supertrend indicator
			var supertrend = new SuperTrend
			{
				Length = SupertrendPeriod,
				Multiplier = SupertrendMultiplier
			};

			// Create StandardDeviation for volume analysis
			var volumeStdDev = new StandardDeviation
			{
				Length = VolumeLookback
			};

			// Create SMA for volume analysis
			var volumeSma = new SimpleMovingAverage
			{
				Length = VolumeLookback
			};

			// Create subscription for candles
			var subscription = SubscribeCandles(CandleType);

			// Bind indicator to subscription and start
			subscription
				.BindEx(supertrend, ProcessSupertrend)
				.Start();

			// Add chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, supertrend);
				DrawOwnTrades(area);
			}

			// Start position protection with dynamic stop at Supertrend level
			StartProtection();
		}

		private void ProcessSupertrend(ICandleMessage candle, IIndicatorValue supertrendValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Update volume statistics
			UpdateVolumeStatistics(candle);

			// Get values from indicator
			decimal supertrendResult = supertrendValue.GetValue<decimal>();
			bool isBelowSupertrend = candle.ClosePrice < supertrendResult;

			// Check for Supertrend crossover
			if (_wasBelowSupertrend.HasValue && _wasBelowSupertrend.Value != isBelowSupertrend)
			{
				// Check if volume is high enough (spike)
				bool isVolumeSpike = candle.TotalVolume > _avgVolume + _volumeStdDev * VolumeDeviationMultiplier;

				if (isVolumeSpike)
				{
					// Supertrend crossed from below to above with volume spike - Sell signal
					if (isBelowSupertrend && Position >= 0)
					{
						LogInfo("Sell signal: Price below Supertrend with volume spike");
						SellMarket(Volume + Math.Abs(Position));
					}
					// Supertrend crossed from above to below with volume spike - Buy signal
					else if (!isBelowSupertrend && Position <= 0)
					{
						LogInfo("Buy signal: Price above Supertrend with volume spike");
						BuyMarket(Volume + Math.Abs(Position));
					}
				}
			}

			// Store state for next candle
			_wasBelowSupertrend = isBelowSupertrend;
			_prevClosePrice = candle.ClosePrice;
		}

		private void UpdateVolumeStatistics(ICandleMessage candle)
		{
			// Simple exponential smoothing for volume statistics
			// This is a simplified approach compared to using standalone indicators
			if (_avgVolume == 0)
			{
				// First calculation
				_avgVolume = candle.TotalVolume;
				_volumeStdDev = 0;
			}
			else
			{
				// Update average volume
				decimal smoothingFactor = 2m / (VolumeLookback + 1);
				_avgVolume = _avgVolume * (1 - smoothingFactor) + candle.TotalVolume * smoothingFactor;

				// Update volume standard deviation
				decimal volumeDiff = candle.TotalVolume - _avgVolume;
				_volumeStdDev = _volumeStdDev * (1 - smoothingFactor) + Math.Abs(volumeDiff) * smoothingFactor;
			}
		}
	}
}
