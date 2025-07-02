using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that uses Bollinger Bands breakouts with volume confirmation.
	/// Enters positions when price breaks above/below Bollinger Bands with increased volume.
	/// </summary>
	public class BollingerVolumeStrategy : Strategy
	{
		private readonly StrategyParam<int> _bollingerPeriod;
		private readonly StrategyParam<decimal> _bollingerDeviation;
		private readonly StrategyParam<int> _volumePeriod;
		private readonly StrategyParam<decimal> _volumeMultiplier;
		private readonly StrategyParam<decimal> _stopLossAtr;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<DataType> _candleType;

		private decimal _avgVolume;

		/// <summary>
		/// Bollinger Bands period.
		/// </summary>
		public int BollingerPeriod
		{
			get => _bollingerPeriod.Value;
			set => _bollingerPeriod.Value = value;
		}

		/// <summary>
		/// Bollinger Bands standard deviation multiplier.
		/// </summary>
		public decimal BollingerDeviation
		{
			get => _bollingerDeviation.Value;
			set => _bollingerDeviation.Value = value;
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
		public BollingerVolumeStrategy()
		{
			_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Bollinger Period", "Period of the Bollinger Bands", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 3.0m, 0.5m);

			_volumePeriod = Param(nameof(VolumePeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Volume Period", "Period for volume averaging", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.5m)
				.SetGreaterThanZero()
				.SetDisplay("Volume Multiplier", "Multiplier for average volume to confirm breakouts", "Indicators")
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
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Initialize variables
			_avgVolume = 0;

			// Create indicators
			var bollinger = new BollingerBands
			{
				Length = BollingerPeriod,
				Width = BollingerDeviation
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

			// Bind volume average indicator separately to update volume average
			subscription
				.BindEx(volumeAvg, ProcessVolumeAverage)
				.Start();

			// Bind Bollinger and ATR for trade decisions
			subscription
				.BindEx(bollinger, atr, ProcessBollingerAndAtr)
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
				DrawIndicator(area, bollinger);
				DrawIndicator(area, atr);
				DrawOwnTrades(area);
			}
		}

		/// <summary>
		/// Process volume average indicator values.
		/// </summary>
		private void ProcessVolumeAverage(IIndicatorValue volumeAvgValue)
		{
			if (volumeAvgValue.IsFinal)
			{
				_avgVolume = volumeAvgValue.ToDecimal();
			}
		}

		/// <summary>
		/// Process Bollinger Bands and ATR indicator values.
		/// </summary>
		private void ProcessBollingerAndAtr(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading() || _avgVolume <= 0)
			return;

			var bb = (BollingerBandsValue)bollingerValue;
			var middleBand = bb.MiddleBand;
			var upperBand = bb.UpperBand;
			var lowerBand = bb.LowerBand;

			var atr = atrValue.ToDecimal();

			// Check volume confirmation
			var isVolumeHighEnough = candle.TotalVolume > _avgVolume * VolumeMultiplier;

			if (isVolumeHighEnough)
			{
				// Long entry: price breaks above upper Bollinger Band with increased volume
				if (candle.ClosePrice > upperBand && Position <= 0)
				{
					var volume = Volume + Math.Abs(Position);
					BuyMarket(volume);
				}
				// Short entry: price breaks below lower Bollinger Band with increased volume
				else if (candle.ClosePrice < lowerBand && Position >= 0)
				{
					var volume = Volume + Math.Abs(Position);
					SellMarket(volume);
				}
			}

			// Exit logic - price returns to middle band
			if (Position > 0 && candle.ClosePrice < middleBand)
			{
				SellMarket(Math.Abs(Position));
			}
			else if (Position < 0 && candle.ClosePrice > middleBand)
			{
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}