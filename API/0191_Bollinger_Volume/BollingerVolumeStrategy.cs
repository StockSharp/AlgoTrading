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
	/// Strategy based on Bollinger Bands and Volume.
	/// Enters long when price breaks above upper Bollinger Band with high volume.
	/// Enters short when price breaks below lower Bollinger Band with high volume.
	/// Exits when price reverts to middle band.
	/// </summary>
	public class BollingerVolumeStrategy : Strategy
	{
		private readonly StrategyParam<int> _bollingerPeriod;
		private readonly StrategyParam<decimal> _bollingerDeviation;
		private readonly StrategyParam<int> _volumePeriod;
		private readonly StrategyParam<decimal> _atrMultiplier;
		private readonly StrategyParam<DataType> _candleType;

		private BollingerBands _bollinger;
		private Volume _volumeIndicator;
		private SimpleMovingAverage _volumeAverage;
		private AverageTrueRange _atr;

		/// <summary>
		/// Bollinger Bands period.
		/// </summary>
		public int BollingerPeriod
		{
			get => _bollingerPeriod.Value;
			set => _bollingerPeriod.Value = value;
		}

		/// <summary>
		/// Bollinger Bands deviation multiplier.
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
		/// ATR multiplier for stop loss calculation.
		/// </summary>
		public decimal AtrMultiplier
		{
			get => _atrMultiplier.Value;
			set => _atrMultiplier.Value = value;
		}

		/// <summary>
		/// Candle type for strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BollingerVolumeStrategy"/>.
		/// </summary>
		public BollingerVolumeStrategy()
		{
			_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
				.SetDisplayName("Bollinger Period")
				.SetDescription("Period for Bollinger Bands calculation")
				.SetCategories("Indicators")
				.SetCanOptimize(true);

			_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
				.SetDisplayName("Bollinger Deviation")
				.SetDescription("Standard deviation multiplier for Bollinger Bands")
				.SetCategories("Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 3m, 0.5m);

			_volumePeriod = Param(nameof(VolumePeriod), 20)
				.SetDisplayName("Volume Period")
				.SetDescription("Period for volume average calculation")
				.SetCategories("Indicators");

			_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
				.SetDisplayName("ATR Multiplier")
				.SetDescription("ATR multiplier for stop loss calculation")
				.SetCategories("Risk Management");

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplayName("Candle Type")
				.SetDescription("Timeframe of data for strategy")
				.SetCategories("General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
		{
			return [(Security, CandleType)];
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create indicators
			_bollinger = new BollingerBands
			{
				Length = BollingerPeriod,
				Width = BollingerDeviation
			};

			_volumeIndicator = new Volume();
			_volumeAverage = new SimpleMovingAverage { Length = VolumePeriod };
			_atr = new AverageTrueRange { Length = 14 };

			// Create subscription
			var subscription = SubscribeCandles(CandleType);

			// Process candles with indicators
			subscription
				.Bind(_bollinger, _atr, (candle, bollinger, atr) => ProcessCandle(candle, bollinger, atr))
				.Start();

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _bollinger);
				DrawOwnTrades(area);

				// Draw volume in separate area
				var volumeArea = CreateChartArea();
				if (volumeArea != null)
				{
					DrawIndicator(volumeArea, _volumeIndicator);
					DrawIndicator(volumeArea, _volumeAverage);
				}
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal atr)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Process volume indicators
			var volumeValue = _volumeIndicator.Process(candle).GetValue<decimal>();
			var avgVolume = _volumeAverage.Process(volumeValue).GetValue<decimal>();

			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Check if volume is above average (high volume)
			bool isHighVolume = volumeValue > avgVolume;
			
			// Trading logic
			if (candle.ClosePrice > upper && isHighVolume && Position <= 0)
			{
				// Breakout above upper band with high volume - go long
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (candle.ClosePrice < lower && isHighVolume && Position >= 0)
			{
				// Breakdown below lower band with high volume - go short
				SellMarket(Volume + Math.Abs(Position));
			}
			else if (Position > 0 && candle.ClosePrice < middle)
			{
				// Exit long position when price drops below middle band
				ClosePosition();
			}
			else if (Position < 0 && candle.ClosePrice > middle)
			{
				// Exit short position when price rises above middle band
				ClosePosition();
			}

			// Set dynamic stop loss based on ATR
			var stopLoss = AtrMultiplier * atr;
			var takeProfit = AtrMultiplier * atr * 1.5m;
			
			// Update protection
			StartProtection(new Unit(takeProfit, UnitTypes.Absolute), new Unit(stopLoss, UnitTypes.Absolute));
		}
	}
}