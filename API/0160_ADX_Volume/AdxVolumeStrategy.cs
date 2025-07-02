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
	/// Implementation of strategy #160 - ADX + Volume.
	/// Enter trades when ADX is above threshold with above average volume.
	/// Direction determined by DI+ and DI- comparison.
	/// </summary>
	public class AdxVolumeStrategy : Strategy
	{
		private readonly StrategyParam<int> _adxPeriod;
		private readonly StrategyParam<decimal> _adxThreshold;
		private readonly StrategyParam<int> _volumeAvgPeriod;
		private readonly StrategyParam<Unit> _stopLoss;
		private readonly StrategyParam<DataType> _candleType;

		// For volume tracking
		private decimal _averageVolume;
		private int _volumeCounter;

		/// <summary>
		/// ADX period.
		/// </summary>
		public int AdxPeriod
		{
			get => _adxPeriod.Value;
			set => _adxPeriod.Value = value;
		}

		/// <summary>
		/// ADX threshold value to determine strong trend.
		/// </summary>
		public decimal AdxThreshold
		{
			get => _adxThreshold.Value;
			set => _adxThreshold.Value = value;
		}

		/// <summary>
		/// Volume average period.
		/// </summary>
		public int VolumeAvgPeriod
		{
			get => _volumeAvgPeriod.Value;
			set => _volumeAvgPeriod.Value = value;
		}

		/// <summary>
		/// Stop-loss value.
		/// </summary>
		public Unit StopLoss
		{
			get => _stopLoss.Value;
			set => _stopLoss.Value = value;
		}

		/// <summary>
		/// Candle type used for strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initialize <see cref="AdxVolumeStrategy"/>.
		/// </summary>
		public AdxVolumeStrategy()
		{
			_adxPeriod = Param(nameof(AdxPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ADX Period", "Period for ADX indicator", "ADX Parameters");

			_adxThreshold = Param(nameof(AdxThreshold), 25m)
				.SetRange(10, 50)
				.SetDisplay("ADX Threshold", "Threshold above which trend is considered strong", "ADX Parameters");

			_volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Volume Average Period", "Period for volume moving average", "Volume Parameters");

			_stopLoss = Param(nameof(StopLoss), new Unit(2, UnitTypes.Absolute))
				.SetDisplay("Stop Loss", "Stop loss in ATR or value", "Risk Management");

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Candle type for strategy", "General");

			_averageVolume = 0;
			_volumeCounter = 0;
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

			// Create ADX indicator
			var adx = new AverageDirectionalIndex { Length = AdxPeriod };

			// Reset volume tracking
			_averageVolume = 0;
			_volumeCounter = 0;

			// Setup candle subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind ADX indicator to candles
			subscription
				.BindEx(adx, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, adx);
				DrawOwnTrades(area);
			}

			// Start protective orders
			StartProtection(new(), StopLoss);
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue diPlusValue, IIndicatorValue diMinusValue)
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Update average volume calculation
			var currentVolume = candle.TotalVolume;
			
			if (_volumeCounter < VolumeAvgPeriod)
			{
				_volumeCounter++;
				_averageVolume = ((_averageVolume * (_volumeCounter - 1)) + currentVolume) / _volumeCounter;
			}
			else
			{
				_averageVolume = (_averageVolume * (VolumeAvgPeriod - 1) + currentVolume) / VolumeAvgPeriod;
			}

			// Check if volume is above average
			var isVolumeAboveAverage = currentVolume > _averageVolume;

			LogInfo($"Candle: {candle.OpenTime}, Close: {candle.ClosePrice}, " +
				   $"ADX: {adxValue}, DI+: {diPlusValue}, DI-: {diMinusValue}, " +
				   $"Volume: {currentVolume}, Avg Volume: {_averageVolume}");

			// Trading rules
			if (adxValue > AdxThreshold && isVolumeAboveAverage)
			{
				// Strong trend detected with above average volume
				
				if (diPlusValue > diMinusValue && Position <= 0)
				{
					// Bullish trend - DI+ > DI-
					var volume = Volume + Math.Abs(Position);
					BuyMarket(volume);
					
					LogInfo($"Buy signal: Strong trend (ADX: {adxValue}) with DI+ > DI- and high volume. Volume: {volume}");
				}
				else if (diMinusValue > diPlusValue && Position >= 0)
				{
					// Bearish trend - DI- > DI+
					var volume = Volume + Math.Abs(Position);
					SellMarket(volume);
					
					LogInfo($"Sell signal: Strong trend (ADX: {adxValue}) with DI- > DI+ and high volume. Volume: {volume}");
				}
			}
			// Exit conditions
			else if (adxValue < AdxThreshold * 0.8m)
			{
				// Trend weakening - exit all positions
				if (Position > 0)
				{
					SellMarket(Position);
					LogInfo($"Exit long: ADX weakening below {AdxThreshold * 0.8m}. Position: {Position}");
				}
				else if (Position < 0)
				{
					BuyMarket(Math.Abs(Position));
					LogInfo($"Exit short: ADX weakening below {AdxThreshold * 0.8m}. Position: {Position}");
				}
			}
			// Check if DI+/DI- cross to exit positions
			else if (diPlusValue < diMinusValue && Position > 0)
			{
				// DI+ crosses below DI- while in long position
				SellMarket(Position);
				LogInfo($"Exit long: DI+ crossed below DI-. Position: {Position}");
			}
			else if (diPlusValue > diMinusValue && Position < 0)
			{
				// DI+ crosses above DI- while in short position
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exit short: DI+ crossed above DI-. Position: {Position}");
			}
		}
	}
}
