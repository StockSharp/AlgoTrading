using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Bollinger Bands with Kalman Filter Strategy.
	/// Enters positions when price is at Bollinger extremes and confirmed by Kalman Filter trend direction.
	/// </summary>
	public class BollingerKalmanFilterStrategy : Strategy
	{
		private readonly StrategyParam<int> _bollingerLength;
		private readonly StrategyParam<decimal> _bollingerDeviation;
		private readonly StrategyParam<decimal> _kalmanQ; // Process noise
		private readonly StrategyParam<decimal> _kalmanR; // Measurement noise
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// Bollinger Bands length.
		/// </summary>
		public int BollingerLength
		{
			get => _bollingerLength.Value;
			set => _bollingerLength.Value = value;
		}

		/// <summary>
		/// Bollinger Bands deviation.
		/// </summary>
		public decimal BollingerDeviation
		{
			get => _bollingerDeviation.Value;
			set => _bollingerDeviation.Value = value;
		}

		/// <summary>
		/// Kalman Filter process noise.
		/// </summary>
		public decimal KalmanQ
		{
			get => _kalmanQ.Value;
			set => _kalmanQ.Value = value;
		}

		/// <summary>
		/// Kalman Filter measurement noise.
		/// </summary>
		public decimal KalmanR
		{
			get => _kalmanR.Value;
			set => _kalmanR.Value = value;
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
		/// Initialize strategy.
		/// </summary>
		public BollingerKalmanFilterStrategy()
		{
			_bollingerLength = Param(nameof(BollingerLength), 20)
				.SetGreaterThanZero()
				.SetDisplay("Bollinger Length", "Length of the Bollinger Bands", "Bollinger Settings")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Bollinger Settings")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 2.5m, 0.5m);

			_kalmanQ = Param(nameof(KalmanQ), 0.01m)
				.SetGreaterThanZero()
				.SetDisplay("Kalman Q", "Process noise for Kalman Filter", "Kalman Filter Settings")
				.SetCanOptimize(true)
				.SetOptimize(0.001m, 0.1m, 0.01m);

			_kalmanR = Param(nameof(KalmanR), 0.1m)
				.SetGreaterThanZero()
				.SetDisplay("Kalman R", "Measurement noise for Kalman Filter", "Kalman Filter Settings")
				.SetCanOptimize(true)
				.SetOptimize(0.01m, 1.0m, 0.1m);

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

			// Create indicators
			var bollinger = new BollingerBands
			{
				Length = BollingerLength,
				Width = BollingerDeviation
			};

			var kalmanFilter = new KalmanFilter
			{
				Q = KalmanQ,
				R = KalmanR
			};

			// Create subscription for candles
			var subscription = SubscribeCandles(CandleType);
			
			// Bind indicators to the subscription
			subscription
				.BindEx(bollinger, kalmanFilter, ProcessCandle)
				.Start();

			// Start position protection
			StartProtection(
				takeProfit: new Unit(2, UnitTypes.Percent),
				stopLoss: new Unit(1, UnitTypes.Percent)
			);

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, bollinger);
				DrawIndicator(area, kalmanFilter);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue kalmanValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Extract values from indicators
			decimal midBand = bollingerValue[0].To<decimal>();  // Middle band (SMA)
			decimal upperBand = bollingerValue[1].To<decimal>(); // Upper band
			decimal lowerBand = bollingerValue[2].To<decimal>(); // Lower band
			
			decimal kalmanFilterValue = kalmanValue.ToDecimal();
			
			// Log the values
			LogInfo($"Price: {candle.ClosePrice}, Kalman: {kalmanFilterValue}, BB middle: {midBand}, BB upper: {upperBand}, BB lower: {lowerBand}");

			// Trading logic: Buy when price is below lower band but Kalman filter shows upward trend
			if (candle.ClosePrice < lowerBand && kalmanFilterValue > candle.ClosePrice && Position <= 0)
			{
				// If we have a short position, close it first
				if (Position < 0)
					BuyMarket(Math.Abs(Position));
				
				// Open a long position
				BuyMarket(Volume);
				LogInfo($"Buy signal: Price below lower band ({candle.ClosePrice} < {lowerBand}) with Kalman uptrend ({kalmanFilterValue} > {candle.ClosePrice})");
			}
			// Trading logic: Sell when price is above upper band but Kalman filter shows downward trend
			else if (candle.ClosePrice > upperBand && kalmanFilterValue < candle.ClosePrice && Position >= 0)
			{
				// If we have a long position, close it first
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				
				// Open a short position
				SellMarket(Volume);
				LogInfo($"Sell signal: Price above upper band ({candle.ClosePrice} > {upperBand}) with Kalman downtrend ({kalmanFilterValue} < {candle.ClosePrice})");
			}
			// Exit signals
			else if ((Position > 0 && candle.ClosePrice > midBand) || 
					 (Position < 0 && candle.ClosePrice < midBand))
			{
				// Close position when price returns to middle band
				ClosePosition();
				LogInfo($"Exit signal: Price returned to middle band. Position closed at {candle.ClosePrice}");
			}
		}
	}
}