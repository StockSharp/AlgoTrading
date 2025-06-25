using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that trades based on Parabolic SAR signals when RSI shows divergence from price.
	/// </summary>
	public class ParabolicSarRsiDivergenceStrategy : Strategy
	{
		private readonly StrategyParam<decimal> _sarAccelerationFactor;
		private readonly StrategyParam<decimal> _sarMaxAccelerationFactor;
		private readonly StrategyParam<int> _rsiPeriod;
		private readonly StrategyParam<DataType> _candleType;

		private decimal _prevRsi;
		private decimal _prevPrice;
		private bool _divergenceDetected;

		/// <summary>
		/// Strategy parameter: Parabolic SAR acceleration factor.
		/// </summary>
		public decimal SarAccelerationFactor
		{
			get => _sarAccelerationFactor.Value;
			set => _sarAccelerationFactor.Value = value;
		}

		/// <summary>
		/// Strategy parameter: Parabolic SAR maximum acceleration factor.
		/// </summary>
		public decimal SarMaxAccelerationFactor
		{
			get => _sarMaxAccelerationFactor.Value;
			set => _sarMaxAccelerationFactor.Value = value;
		}

		/// <summary>
		/// Strategy parameter: RSI period.
		/// </summary>
		public int RsiPeriod
		{
			get => _rsiPeriod.Value;
			set => _rsiPeriod.Value = value;
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
		public ParabolicSarRsiDivergenceStrategy()
		{
			_sarAccelerationFactor = Param(nameof(SarAccelerationFactor), 0.02m)
				.SetRange(0.01m, 0.25m)
				.SetDisplay("SAR Acceleration Factor", "Initial acceleration factor for Parabolic SAR", "Indicator Settings");

			_sarMaxAccelerationFactor = Param(nameof(SarMaxAccelerationFactor), 0.2m)
				.SetRange(0.1m, 0.5m)
				.SetDisplay("SAR Max Acceleration Factor", "Maximum acceleration factor for Parabolic SAR", "Indicator Settings");

			_rsiPeriod = Param(nameof(RsiPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("RSI Period", "Period for RSI calculation", "Indicator Settings");

			_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
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
			_prevRsi = 0;
			_prevPrice = 0;
			_divergenceDetected = false;

			// Create Parabolic SAR indicator
			var parabolicSar = new ParabolicSar
			{
				Af = SarAccelerationFactor,
				MaxAf = SarMaxAccelerationFactor
			};

			// Create RSI indicator
			var rsi = new RelativeStrengthIndex
			{
				Length = RsiPeriod
			};

			// Create subscription for candles
			var subscription = SubscribeCandles(CandleType);

			// Bind indicators to subscription and start
			subscription
				.Bind(parabolicSar, rsi, ProcessSignals)
				.Start();

			// Add chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, parabolicSar);
				DrawIndicator(area, rsi);
				DrawOwnTrades(area);
			}
		}

		private void ProcessSignals(ICandleMessage candle, decimal sarValue, decimal rsiValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Check for RSI divergence
			CheckRsiDivergence(candle.ClosePrice, rsiValue);

			// Trading logic based on Parabolic SAR and RSI divergence
			if (_divergenceDetected)
			{
				bool isBelowSar = candle.ClosePrice < sarValue;

				// Bullish divergence (price falling but RSI rising) and price above SAR
				if (!isBelowSar && _prevPrice > candle.ClosePrice && _prevRsi < rsiValue && Position <= 0)
				{
					LogInfo($"Buy signal: Bullish divergence with price ({candle.ClosePrice}) above SAR ({sarValue})");
					BuyMarket(Volume + Math.Abs(Position));
					_divergenceDetected = false;
				}
				// Bearish divergence (price rising but RSI falling) and price below SAR
				else if (isBelowSar && _prevPrice < candle.ClosePrice && _prevRsi > rsiValue && Position >= 0)
				{
					LogInfo($"Sell signal: Bearish divergence with price ({candle.ClosePrice}) below SAR ({sarValue})");
					SellMarket(Volume + Math.Abs(Position));
					_divergenceDetected = false;
				}
			}

			// Exit logic based on SAR flips
			if ((Position > 0 && candle.ClosePrice < sarValue) ||
				(Position < 0 && candle.ClosePrice > sarValue))
			{
				LogInfo($"Exit signal: Price crossed SAR in opposite direction. Price: {candle.ClosePrice}, SAR: {sarValue}");
				ClosePosition();
			}

			// Store previous values for next comparison
			_prevRsi = rsiValue;
			_prevPrice = candle.ClosePrice;
		}

		private void CheckRsiDivergence(decimal currentPrice, decimal currentRsi)
		{
			// If we have previous values to compare
			if (_prevPrice != 0 && _prevRsi != 0)
			{
				// Bullish divergence: price making lower lows but RSI making higher lows
				bool bullishDivergence = currentPrice < _prevPrice && currentRsi > _prevRsi;

				// Bearish divergence: price making higher highs but RSI making lower highs
				bool bearishDivergence = currentPrice > _prevPrice && currentRsi < _prevRsi;

				if (bullishDivergence || bearishDivergence)
				{
					_divergenceDetected = true;
					LogInfo($"Divergence detected: {(bullishDivergence ? "Bullish" : "Bearish")}. " +
						$"Price: {_prevPrice}->{currentPrice}, RSI: {_prevRsi}->{currentRsi}");
				}
			}
		}
	}
}
