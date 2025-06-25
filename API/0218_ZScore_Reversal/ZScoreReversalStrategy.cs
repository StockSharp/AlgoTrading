using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Strategies
{
	/// <summary>
	/// Strategy that trades based on Z-Score (normalized price deviation from the mean).
	/// Enters long when Z-Score is below a negative threshold (price significantly below mean).
	/// Enters short when Z-Score is above a positive threshold (price significantly above mean).
	/// Exits when Z-Score returns to zero (price returns to mean).
	/// </summary>
	public class ZScoreReversalStrategy : Strategy
	{
		private readonly StrategyParam<int> _lookbackPeriod;
		private readonly StrategyParam<decimal> _zScoreThreshold;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;
		
		private MovingAverage _ma;
		private StandardDeviation _stdDev;
		
		private decimal _lastZScore;
		
		/// <summary>
		/// Period for calculating mean and standard deviation.
		/// </summary>
		public int LookbackPeriod
		{
			get => _lookbackPeriod.Value;
			set => _lookbackPeriod.Value = value;
		}
		
		/// <summary>
		/// Z-Score threshold for entry signals.
		/// </summary>
		public decimal ZScoreThreshold
		{
			get => _zScoreThreshold.Value;
			set => _zScoreThreshold.Value = value;
		}
		
		/// <summary>
		/// Stop-loss percentage parameter.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}
		
		/// <summary>
		/// Candle type parameter.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}
		
		/// <summary>
		/// Constructor.
		/// </summary>
		public ZScoreReversalStrategy()
		{
			_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Lookback Period", "Period for calculating mean and standard deviation", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 40, 5);
				
			_zScoreThreshold = Param(nameof(ZScoreThreshold), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Z-Score Threshold", "Z-Score threshold for entry signals", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 3.0m, 0.5m);
				
			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetGreaterThanZero()
				.SetDisplay("Stop-loss %", "Stop-loss as percentage of entry price", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1m, 3m, 0.5m);
				
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(10).TimeFrame())
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
			
			// Initialize indicators
			_ma = new MovingAverage { Length = LookbackPeriod };
			_stdDev = new StandardDeviation { Length = LookbackPeriod };
			
			// Create candles subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind indicators to subscription
			subscription
				.Bind(_ma, _stdDev, ProcessCandle)
				.Start();
			
			// Enable position protection with stop-loss
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute), // No take-profit
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent) // Stop-loss as percentage
			);
			
			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _ma);
				DrawOwnTrades(area);
			}
		}
		
		private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal stdDevValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;
				
			// Skip if strategy is not ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Skip if standard deviation is zero (avoid division by zero)
			if (stdDevValue == 0)
				return;
			
			// Calculate Z-Score: (Price - Mean) / StdDev
			decimal zScore = (candle.ClosePrice - maValue) / stdDevValue;
			
			LogInfo($"Current Z-Score: {zScore:F4}, Mean: {maValue:F4}, StdDev: {stdDevValue:F4}");
			
			// Trading logic
			if (zScore < -ZScoreThreshold)
			{
				// Long signal: Z-Score is below negative threshold
				if (Position <= 0)
				{
					BuyMarket(Volume + Math.Abs(Position));
					LogInfo($"Long Entry: Z-Score({zScore:F4}) < -{ZScoreThreshold:F4}");
				}
			}
			else if (zScore > ZScoreThreshold)
			{
				// Short signal: Z-Score is above positive threshold
				if (Position >= 0)
				{
					SellMarket(Volume + Math.Abs(Position));
					LogInfo($"Short Entry: Z-Score({zScore:F4}) > {ZScoreThreshold:F4}");
				}
			}
			else if ((zScore > 0 && Position > 0) || (zScore < 0 && Position < 0))
			{
				// Exit signals: Z-Score crossed zero line
				if (Position > 0 && _lastZScore < 0 && zScore > 0)
				{
					SellMarket(Math.Abs(Position));
					LogInfo($"Exit Long: Z-Score crossed zero from negative to positive");
				}
				else if (Position < 0 && _lastZScore > 0 && zScore < 0)
				{
					BuyMarket(Math.Abs(Position));
					LogInfo($"Exit Short: Z-Score crossed zero from positive to negative");
				}
			}
			
			// Store current Z-Score for next calculation
			_lastZScore = zScore;
		}
	}
}
