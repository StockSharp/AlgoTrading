using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy combining MACD (Moving Average Convergence Divergence) with volume confirmation.
	/// Enters positions when MACD line crosses the Signal line and confirms with increased volume.
	/// </summary>
	public class MacdVolumeStrategy : Strategy
	{
		private readonly StrategyParam<int> _macdFast;
		private readonly StrategyParam<int> _macdSlow;
		private readonly StrategyParam<int> _macdSignal;
		private readonly StrategyParam<int> _volumePeriod;
		private readonly StrategyParam<decimal> _volumeMultiplier;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		private decimal? _prevMacd;
		private decimal? _prevSignal;
		private decimal _avgVolume;

		/// <summary>
		/// MACD fast EMA period.
		/// </summary>
		public int MacdFast
		{
			get => _macdFast.Value;
			set => _macdFast.Value = value;
		}

		/// <summary>
		/// MACD slow EMA period.
		/// </summary>
		public int MacdSlow
		{
			get => _macdSlow.Value;
			set => _macdSlow.Value = value;
		}

		/// <summary>
		/// MACD signal line period.
		/// </summary>
		public int MacdSignal
		{
			get => _macdSignal.Value;
			set => _macdSignal.Value = value;
		}

		/// <summary>
		/// Volume averaging period.
		/// </summary>
		public int VolumePeriod
		{
			get => _volumePeriod.Value;
			set => _volumePeriod.Value = value;
		}

		/// <summary>
		/// Volume multiplier for confirmation.
		/// </summary>
		public decimal VolumeMultiplier
		{
			get => _volumeMultiplier.Value;
			set => _volumeMultiplier.Value = value;
		}

		/// <summary>
		/// Stop loss percentage.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Candle type for strategy calculation.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Strategy constructor.
		/// </summary>
		public MacdVolumeStrategy()
		{
			_macdFast = Param(nameof(MacdFast), 12)
				.SetGreaterThanZero()
				.SetDisplay("MACD Fast", "Fast EMA period of MACD", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(8, 16, 4);

			_macdSlow = Param(nameof(MacdSlow), 26)
				.SetGreaterThanZero()
				.SetDisplay("MACD Slow", "Slow EMA period of MACD", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(20, 32, 4);

			_macdSignal = Param(nameof(MacdSignal), 9)
				.SetGreaterThanZero()
				.SetDisplay("MACD Signal", "Signal line period of MACD", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(5, 13, 4);

			_volumePeriod = Param(nameof(VolumePeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Volume Period", "Period for volume averaging", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.5m)
				.SetGreaterThanZero()
				.SetDisplay("Volume Multiplier", "Multiplier for average volume to confirm entry", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 2.0m, 0.5m);

			_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

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

			// Initialize variables
			_prevMacd = null;
			_prevSignal = null;
			_avgVolume = 0;

			// Create indicators

			var macd = new MovingAverageConvergenceDivergenceSignal
			{
				Macd =
				{
					ShortMa = { Length = MacdFast },
					LongMa = { Length = MacdSlow },
				},
				SignalMa = { Length = MacdSignal }
			};
			var volumeAvg = new SimpleMovingAverage
			{
				Length = VolumePeriod
			};

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);

			// Bind volume average indicator separately to update volume average
			subscription
				.BindEx(volumeAvg, ProcessVolumeAverage)
				.Start();

			// Bind MACD for trade decisions
			subscription
				.Bind(macd, ProcessMacd)
				.Start();

			// Setup position protection
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent) // Percentage-based stop loss
			);

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, macd);
				DrawOwnTrades(area);
			}
		}

		/// <summary>
		/// Process volume average indicator values.
		/// </summary>
		private void ProcessVolumeAverage(ICandleMessage candle, IIndicatorValue volumeAvgValue)
		{
			if (volumeAvgValue.IsFinal)
			{
				_avgVolume = volumeAvgValue.ToDecimal();
			}
		}

		/// <summary>
		/// Process MACD indicator values.
		/// </summary>
		private void ProcessMacd(ICandleMessage candle, decimal macdValue, decimal signalValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading() || _avgVolume <= 0)
				return;

			// Check if we have previous values to compare
			if (_prevMacd.HasValue && _prevSignal.HasValue)
			{
				// Detect MACD crossover signals
				bool macdCrossedAboveSignal = _prevMacd < _prevSignal && macdValue > signalValue;
				bool macdCrossedBelowSignal = _prevMacd > _prevSignal && macdValue < signalValue;

				// Check volume confirmation
				var isVolumeHighEnough = candle.TotalVolume > _avgVolume * VolumeMultiplier;

				if (isVolumeHighEnough)
				{
					// Long entry: MACD crosses above Signal with increased volume
					if (macdCrossedAboveSignal && Position <= 0)
					{
						var volume = Volume + Math.Abs(Position);
						BuyMarket(volume);
					}
					// Short entry: MACD crosses below Signal with increased volume
					else if (macdCrossedBelowSignal && Position >= 0)
					{
						var volume = Volume + Math.Abs(Position);
						SellMarket(volume);
					}
				}
			}

			// Exit logic - when MACD crosses back
			if (Position > 0 && macdValue < signalValue)
			{
				SellMarket(Math.Abs(Position));
			}
			else if (Position < 0 && macdValue > signalValue)
			{
				BuyMarket(Math.Abs(Position));
			}

			// Update previous values
			_prevMacd = macdValue;
			_prevSignal = signalValue;
		}
	}
}