using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Bollinger Bands squeeze.
	/// </summary>
	public class BollingerSqueezeStrategy : Strategy
	{
		private readonly StrategyParam<int> _bollingerPeriod;
		private readonly StrategyParam<decimal> _bollingerDeviation;
		private readonly StrategyParam<decimal> _squeezeThreshold;
		private readonly StrategyParam<DataType> _candleType;

		private decimal _previousBandWidth;
		private bool _isFirstValue = true;
		private bool _isInSqueeze = false;

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
		/// Squeeze threshold.
		/// </summary>
		public decimal SqueezeThreshold
		{
			get => _squeezeThreshold.Value;
			set => _squeezeThreshold.Value = value;
		}

		/// <summary>
		/// Candle type.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BollingerSqueezeStrategy"/>.
		/// </summary>
		public BollingerSqueezeStrategy()
		{
			_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
				.SetRange(10, 50)
				.SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Indicators")
				.SetCanOptimize(true);

			_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
				.SetRange(1m, 3m)
				.SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators")
				.SetCanOptimize(true);

			_squeezeThreshold = Param(nameof(SqueezeThreshold), 0.1m)
				.SetRange(0.05m, 0.5m)
				.SetDisplay("Squeeze Threshold", "Threshold for Bollinger Bands width to identify squeeze", "Strategy")
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

			// Reset state variables
			_previousBandWidth = 0;
			_isFirstValue = true;
			_isInSqueeze = false;

			// Create Bollinger Bands indicator
			var bollingerBands = new BollingerBands
			{
				Length = BollingerPeriod,
				Width = BollingerDeviation
			};

			// Subscribe to candles and bind the indicator
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(bollingerBands, ProcessCandle)
				.Start();

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, bollingerBands);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var bb = (BollingerBandsValue)bollingerValue;
			var middleBand = bb.MovingAverage;
			var upperBand = bb.UpBand;
			var lowerBand = bb.LowBand;

			// Calculate Bollinger Bands width relative to the middle band
			decimal bandWidth = (upperBand - lowerBand) / middleBand;

			if (_isFirstValue)
			{
				_previousBandWidth = bandWidth;
				_isFirstValue = false;
				return;
			}

			// Detect squeeze (narrow Bollinger Bands)
			bool isSqueeze = bandWidth < SqueezeThreshold;

			// Check for breakout from squeeze
			if (_isInSqueeze && !isSqueeze && bandWidth > _previousBandWidth)
			{
				// Squeeze is ending with expanding bands - potential breakout
				
				// Determine breakout direction by price relative to middle band
				if (candle.ClosePrice > upperBand && Position <= 0)
				{
					// Bullish breakout (price breaks above upper band)
					var volume = Volume + Math.Abs(Position);
					BuyMarket(volume);
					LogInfo($"Buy signal: Bollinger squeeze breakout upward. Width: {bandWidth:F4}, Price: {candle.ClosePrice}, Upper Band: {upperBand}");
				}
				else if (candle.ClosePrice < lowerBand && Position >= 0)
				{
					// Bearish breakout (price breaks below lower band)
					var volume = Volume + Math.Abs(Position);
					SellMarket(volume);
					LogInfo($"Sell signal: Bollinger squeeze breakout downward. Width: {bandWidth:F4}, Price: {candle.ClosePrice}, Lower Band: {lowerBand}");
				}
			}

			// Update squeeze state
			_isInSqueeze = isSqueeze;
			
			// Exit logic
			if (Position > 0 && candle.ClosePrice < middleBand)
			{
				// Exit long position when price falls below middle band
				SellMarket(Math.Abs(Position));
				LogInfo($"Exiting long position: Price below middle band. Price: {candle.ClosePrice}, Middle Band: {middleBand}");
			}
			else if (Position < 0 && candle.ClosePrice > middleBand)
			{
				// Exit short position when price rises above middle band
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exiting short position: Price above middle band. Price: {candle.ClosePrice}, Middle Band: {middleBand}");
			}

			// Store current band width for next comparison
			_previousBandWidth = bandWidth;
		}
	}
}