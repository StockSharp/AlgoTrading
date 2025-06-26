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
	/// Implementation of strategy #151 - Ichimoku + Volume.
	/// Buy when price is above Kumo cloud, Tenkan-sen is above Kijun-sen, and volume is above average.
	/// Sell when price is below Kumo cloud, Tenkan-sen is below Kijun-sen, and volume is above average.
	/// </summary>
	public class IchimokuVolumeStrategy : Strategy
	{
		private readonly StrategyParam<int> _tenkanPeriod;
		private readonly StrategyParam<int> _kijunPeriod;
		private readonly StrategyParam<int> _senkouSpanPeriod;
		private readonly StrategyParam<int> _volumeAvgPeriod;
		private readonly StrategyParam<Unit> _stopLoss;
		private readonly StrategyParam<DataType> _candleType;

		private decimal _averageVolume;
		private int _volumeCounter;

		/// <summary>
		/// Tenkan-sen period.
		/// </summary>
		public int TenkanPeriod
		{
			get => _tenkanPeriod.Value;
			set => _tenkanPeriod.Value = value;
		}

		/// <summary>
		/// Kijun-sen period.
		/// </summary>
		public int KijunPeriod
		{
			get => _kijunPeriod.Value;
			set => _kijunPeriod.Value = value;
		}

		/// <summary>
		/// Senkou Span period.
		/// </summary>
		public int SenkouSpanPeriod
		{
			get => _senkouSpanPeriod.Value;
			set => _senkouSpanPeriod.Value = value;
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
		/// Initialize <see cref="IchimokuVolumeStrategy"/>.
		/// </summary>
		public IchimokuVolumeStrategy()
		{
			_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
				.SetGreaterThanZero()
				.SetDisplay("Tenkan Period", "Tenkan-sen period (fast)", "Ichimoku Parameters");

			_kijunPeriod = Param(nameof(KijunPeriod), 26)
				.SetGreaterThanZero()
				.SetDisplay("Kijun Period", "Kijun-sen period (slow)", "Ichimoku Parameters");

			_senkouSpanPeriod = Param(nameof(SenkouSpanPeriod), 52)
				.SetGreaterThanZero()
				.SetDisplay("Senkou Span Period", "Senkou Span B period", "Ichimoku Parameters");

			_volumeAvgPeriod = Param(nameof(VolumeAvgPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Volume Average Period", "Period for volume moving average", "Volume Parameters");

			_stopLoss = Param(nameof(StopLoss), new Unit(2, UnitTypes.Percent))
				.SetDisplay("Stop Loss", "Stop loss percent or value", "Risk Management");

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

			// Create Ichimoku indicator
			var ichimoku = new Ichimoku
			{
				TenkanPeriod = TenkanPeriod,
				KijunPeriod = KijunPeriod,
				SenkouSpanPeriod = SenkouSpanPeriod
			};

			// Reset volume tracking
			_averageVolume = 0;
			_volumeCounter = 0;

			// Setup candle subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind Ichimoku indicator to candles
			subscription.BindEx(ichimoku, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, ichimoku);
				DrawOwnTrades(area);
			}

			// Start protective orders (stop-loss)
			StartProtection(StopLoss);
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Ichimoku values
			var tenkanSen = values[0].ToDecimal();
			var kijunSen = values[1].ToDecimal();
			var senkouSpanA = values[2].ToDecimal();
			var senkouSpanB = values[3].ToDecimal();
			
			// Calculate Kumo cloud boundaries
			var upperKumo = Math.Max(senkouSpanA, senkouSpanB);
			var lowerKumo = Math.Min(senkouSpanA, senkouSpanB);

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

			LogInfo($"Candle: {candle.OpenTime}, Close: {candle.ClosePrice}, TenkanSen: {tenkanSen}, " +
				   $"KijunSen: {kijunSen}, Upper Kumo: {upperKumo}, Lower Kumo: {lowerKumo}, " +
				   $"Volume: {currentVolume}, Avg Volume: {_averageVolume}");

			// Trading rules
			if (candle.ClosePrice > upperKumo && tenkanSen > kijunSen && isVolumeAboveAverage && Position <= 0)
			{
				// Buy signal - price above Kumo, Tenkan above Kijun, volume above average
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				
				LogInfo($"Buy signal: Price above Kumo, Tenkan above Kijun, Volume above average. Volume: {volume}");
			}
			else if (candle.ClosePrice < lowerKumo && tenkanSen < kijunSen && isVolumeAboveAverage && Position >= 0)
			{
				// Sell signal - price below Kumo, Tenkan below Kijun, volume above average
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				
				LogInfo($"Sell signal: Price below Kumo, Tenkan below Kijun, Volume above average. Volume: {volume}");
			}
		}
	}
}
