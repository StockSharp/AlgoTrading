using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Volatility Adjusted Mean Reversion strategy.
	/// Uses ATR and Standard Deviation to create adaptive entry thresholds.
	/// </summary>
	public class VolatilityAdjustedMeanReversionStrategy : Strategy
	{
		private readonly StrategyParam<int> _periodParam;
		private readonly StrategyParam<decimal> _multiplierParam;
		private readonly StrategyParam<DataType> _candleTypeParam;

		private SimpleMovingAverage _sma;
		private AverageTrueRange _atr;
		private StandardDeviation _stdDev;
		
		/// <summary>
		/// Period for indicators.
		/// </summary>
		public int Period
		{
			get => _periodParam.Value;
			set => _periodParam.Value = value;
		}

		/// <summary>
		/// Multiplier for entry threshold.
		/// </summary>
		public decimal Multiplier
		{
			get => _multiplierParam.Value;
			set => _multiplierParam.Value = value;
		}

		/// <summary>
		/// Candle type for strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleTypeParam.Value;
			set => _candleTypeParam.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public VolatilityAdjustedMeanReversionStrategy()
		{
			_periodParam = Param(nameof(Period), 20)
				.SetGreaterThanZero()
				.SetDisplay("Period", "Period for indicators", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 10);

			_multiplierParam = Param(nameof(Multiplier), 2.0m)
				.SetRange(0.1m, decimal.MaxValue)
				.SetDisplay("Multiplier", "Multiplier for entry threshold", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Candle type for strategy", "Common");
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
			_sma = new SimpleMovingAverage { Length = Period };
			_atr = new AverageTrueRange { Length = Period };
			_stdDev = new StandardDeviation { Length = Period };

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			// First, bind SMA and ATR
			subscription
				.Bind(_sma, _atr, _stdDev, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _sma);
				DrawIndicator(area, _atr);
				DrawOwnTrades(area);
			}
			
			// Enable position protection
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit
				stopLoss: new Unit(2, UnitTypes.Absolute) // Stop loss at 2*ATR
			);
		}

		private void ProcessCandle(ICandleMessage candle, decimal? smaValue, decimal? atrValue, decimal? stdDevValue)
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Skip if standard deviation is too small to avoid division by zero
			if (stdDevValue < 0.0001m)
				return;
				
			// Calculate volatility ratio
			var volatilityRatio = atrValue / stdDevValue;
			
			// Calculate volatility-adjusted thresholds
			var threshold = Multiplier * atrValue / volatilityRatio;
			var upperThreshold = smaValue + threshold;
			var lowerThreshold = smaValue - threshold;
			
			// Long setup - price below lower threshold
			if (candle.ClosePrice < lowerThreshold && Position <= 0)
			{
				// Buy signal - price has deviated too much below average
				BuyMarket(Volume + Math.Abs(Position));
			}
			// Short setup - price above upper threshold
			else if (candle.ClosePrice > upperThreshold && Position >= 0)
			{
				// Sell signal - price has deviated too much above average
				SellMarket(Volume + Math.Abs(Position));
			}
			// Exit long position when price returns to average
			else if (Position > 0 && candle.ClosePrice >= smaValue)
			{
				// Close long position
				SellMarket(Position);
			}
			// Exit short position when price returns to average
			else if (Position < 0 && candle.ClosePrice <= smaValue)
			{
				// Close short position
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}