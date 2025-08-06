using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that uses Hull Moving Average for trend direction
	/// and volume confirmation for trade entries.
	/// </summary>
	public class HullMaVolumeStrategy : Strategy
	{
		private readonly StrategyParam<int> _hullPeriod;
		private readonly StrategyParam<int> _volumePeriod;
		private readonly StrategyParam<decimal> _volumeMultiplier;
		private readonly StrategyParam<decimal> _stopLossAtr;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<DataType> _candleType;

		private decimal? _prevHullValue;
		private decimal? _avgVolume;

		/// <summary>
		/// Hull Moving Average period.
		/// </summary>
		public int HullPeriod
		{
			get => _hullPeriod.Value;
			set => _hullPeriod.Value = value;
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
		/// Stop loss in ATR multiples.
		/// </summary>
		public decimal StopLossAtr
		{
			get => _stopLossAtr.Value;
			set => _stopLossAtr.Value = value;
		}

		/// <summary>
		/// ATR period.
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
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
		public HullMaVolumeStrategy()
		{
			_hullPeriod = Param(nameof(HullPeriod), 9)
				.SetGreaterThanZero()
				.SetDisplay("Hull MA Period", "Period of the Hull Moving Average", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(5, 15, 2);

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

			_stopLossAtr = Param(nameof(StopLossAtr), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss ATR", "Stop loss as ATR multiplier", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "Period of the ATR for stop loss calculation", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(7, 21, 7);

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

			// Initialize variables
			_prevHullValue = null;
			_avgVolume = 0;
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create indicators
			var hullMa = new HullMovingAverage
			{
				Length = HullPeriod
			};

			var volumeAvg = new SimpleMovingAverage
			{
				Length = VolumePeriod
			};

			var atr = new AverageTrueRange
			{
				Length = AtrPeriod
			};

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);

			// Bind Hull MA and ATR for trade decisions
			subscription
				.Bind(volumeAvg, hullMa, atr, ProcessIndicators)
				.Start();

			// Setup position protection with ATR-based stop loss
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit
				stopLoss: new Unit(StopLossAtr, UnitTypes.Absolute) // ATR-based stop loss
			);

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, hullMa);
				DrawIndicator(area, atr);
				DrawOwnTrades(area);
			}
		}

		/// <summary>
		/// Process Hull MA and ATR indicator values.
		/// </summary>
		private void ProcessIndicators(ICandleMessage candle, decimal volumeAvgValue, decimal hullValue, decimal atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading() || _avgVolume <= 0)
				return;

			_avgVolume = volumeAvgValue;

			// Store current Hull MA value
			var currentHullValue = hullValue;

			// If we have previous Hull MA value, we can check for trend direction
			if (_prevHullValue.HasValue)
			{
				// Check volume confirmation
				var isVolumeHighEnough = candle.TotalVolume > _avgVolume * VolumeMultiplier;

				if (isVolumeHighEnough)
				{
					// Hull MA trend rising
					if (currentHullValue > _prevHullValue.Value && Position <= 0)
					{
						var volume = Volume + Math.Abs(Position);
						BuyMarket(volume);
					}
					// Hull MA trend falling
					else if (currentHullValue < _prevHullValue.Value && Position >= 0)
					{
						var volume = Volume + Math.Abs(Position);
						SellMarket(volume);
					}
				}

				// Exit logic - reverse in Hull MA direction
				if (Position > 0 && currentHullValue < _prevHullValue.Value)
				{
					SellMarket(Math.Abs(Position));
				}
				else if (Position < 0 && currentHullValue > _prevHullValue.Value)
				{
					BuyMarket(Math.Abs(Position));
				}
			}

			// Update previous Hull MA value
			_prevHullValue = currentHullValue;
		}
	}
}