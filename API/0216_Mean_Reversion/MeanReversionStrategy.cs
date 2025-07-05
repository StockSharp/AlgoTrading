using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Strategies
{
	/// <summary>
	/// Statistical Mean Reversion strategy.
	/// Enters long when price falls below the mean by a specified number of standard deviations.
	/// Enters short when price rises above the mean by a specified number of standard deviations.
	/// Exits positions when price returns to the mean.
	/// </summary>
	public class MeanReversionStrategy : Strategy
	{
		private readonly StrategyParam<int> _movingAveragePeriod;
		private readonly StrategyParam<decimal> _deviationMultiplier;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;
		
		private SimpleMovingAverage _ma;
		private StandardDeviation _stdDev;
		
		/// <summary>
		/// Moving average period parameter.
		/// </summary>
		public int MovingAveragePeriod
		{
			get => _movingAveragePeriod.Value;
			set => _movingAveragePeriod.Value = value;
		}
		
		/// <summary>
		/// Standard deviation multiplier parameter.
		/// </summary>
		public decimal DeviationMultiplier
		{
			get => _deviationMultiplier.Value;
			set => _deviationMultiplier.Value = value;
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
		public MeanReversionStrategy()
		{
			_movingAveragePeriod = Param(nameof(MovingAveragePeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("MA Period", "Period for moving average calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);
				
			_deviationMultiplier = Param(nameof(DeviationMultiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Deviation Multiplier", "Standard deviation multiplier for entry signals", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 3.0m, 0.5m);
				
			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetGreaterThanZero()
				.SetDisplay("Stop-loss %", "Stop-loss as percentage of entry price", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1m, 3m, 0.5m);
				
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
			
			// Initialize indicators
			_ma = new() { Length = MovingAveragePeriod };
			_stdDev = new() { Length = MovingAveragePeriod };
			
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
		
		private void ProcessCandle(ICandleMessage candle, decimal? maValue, decimal? stdDevValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			if (maValue == null || stdDevValue == null)
				return;

			// Skip if strategy is not ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Calculate upper and lower bands based on mean and standard deviation
			decimal upperBand = maValue.Value + (stdDevValue.Value * DeviationMultiplier);
			decimal lowerBand = maValue.Value - (stdDevValue.Value * DeviationMultiplier);
			
			// Trading logic
			if (candle.ClosePrice < lowerBand)
			{
				// Long signal: Price below lower band (mean - k*stdDev)
				if (Position <= 0)
				{
					BuyMarket(Volume + Math.Abs(Position));
					LogInfo($"Long Entry: Price({candle.ClosePrice}) < Lower Band({lowerBand:F2})");
				}
			}
			else if (candle.ClosePrice > upperBand)
			{
				// Short signal: Price above upper band (mean + k*stdDev)
				if (Position >= 0)
				{
					SellMarket(Volume + Math.Abs(Position));
					LogInfo($"Short Entry: Price({candle.ClosePrice}) > Upper Band({upperBand:F2})");
				}
			}
			else if ((Position > 0 && candle.ClosePrice > maValue.Value) ||
			(Position < 0 && candle.ClosePrice < maValue.Value))
				{
					// Exit signals: Price returned to the mean
					if (Position > 0)
						{
							SellMarket(Math.Abs(Position));
								LogInfo($"Exit Long: Price({candle.ClosePrice}) > MA({maValue.Value:F2})");
								}
				else if (Position < 0)
					{
						BuyMarket(Math.Abs(Position));
							LogInfo($"Exit Short: Price({candle.ClosePrice}) < MA({maValue.Value:F2})");
								}
				}
		}
	}
}
