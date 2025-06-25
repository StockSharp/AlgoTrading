using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Parabolic SAR indicator.
	/// It enters long position when price is above SAR and short position when price is below SAR.
	/// </summary>
	public class ParabolicSarTrendStrategy : Strategy
	{
		private readonly StrategyParam<decimal> _accelerationFactor;
		private readonly StrategyParam<decimal> _maxAccelerationFactor;
		private readonly StrategyParam<DataType> _candleType;

		// Current state
		private decimal _prevSarValue;
		private bool _prevIsPriceAboveSar;

		/// <summary>
		/// Initial acceleration factor for SAR.
		/// </summary>
		public decimal AccelerationFactor
		{
			get => _accelerationFactor.Value;
			set => _accelerationFactor.Value = value;
		}

		/// <summary>
		/// Maximum acceleration factor for SAR.
		/// </summary>
		public decimal MaxAccelerationFactor
		{
			get => _maxAccelerationFactor.Value;
			set => _maxAccelerationFactor.Value = value;
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
		/// Initialize the Parabolic SAR Trend strategy.
		/// </summary>
		public ParabolicSarTrendStrategy()
		{
			_accelerationFactor = Param(nameof(AccelerationFactor), 0.02m)
				.SetDisplay("Acceleration Factor", "Initial acceleration factor for SAR calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(0.01m, 0.05m, 0.01m);

			_maxAccelerationFactor = Param(nameof(MaxAccelerationFactor), 0.2m)
				.SetDisplay("Max Acceleration Factor", "Maximum acceleration factor for SAR calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(0.1m, 0.5m, 0.1m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
				
			_prevSarValue = 0;
			_prevIsPriceAboveSar = false;
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

			// Create Parabolic SAR indicator
			var parabolicSar = new ParabolicSar
			{
				AccelerationFactor = AccelerationFactor,
				AccelerationFactorMax = MaxAccelerationFactor
			};

			// Create subscription and bind indicator
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(parabolicSar, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, parabolicSar);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal sarValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Check the price position relative to SAR
			var isPriceAboveSar = candle.ClosePrice > sarValue;

			// Detect signal - crossing of price and SAR
			var isEntrySignal = _prevSarValue > 0 && isPriceAboveSar != _prevIsPriceAboveSar;
			
			if (isEntrySignal)
			{
				var volume = Volume + Math.Abs(Position);

				// Long entry - price crosses above SAR
				if (isPriceAboveSar && Position <= 0)
				{
					BuyMarket(volume);
					LogInfo($"Buy signal: Price {candle.ClosePrice} crossed above SAR {sarValue}");
				}
				// Short entry - price crosses below SAR
				else if (!isPriceAboveSar && Position >= 0)
				{
					SellMarket(volume);
					LogInfo($"Sell signal: Price {candle.ClosePrice} crossed below SAR {sarValue}");
				}
			}
			// Exit logic - when SAR catches up with price
			else if ((Position > 0 && !isPriceAboveSar) || (Position < 0 && isPriceAboveSar))
			{
				ClosePosition();
				LogInfo($"Exit signal: SAR {sarValue} catching up with price {candle.ClosePrice}");
			}

			// Update previous values
			_prevSarValue = sarValue;
			_prevIsPriceAboveSar = isPriceAboveSar;
		}
	}
}