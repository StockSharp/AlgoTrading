using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that trades on Williams %R breakouts.
	/// When Williams %R rises significantly above its average or falls significantly below its average, 
	/// it enters position in the corresponding direction.
	/// </summary>
	public class WilliamsRBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _williamsRPeriod;
		private readonly StrategyParam<int> _avgPeriod;
		private readonly StrategyParam<decimal> _multiplier;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLoss;
		
		private WilliamsR _williamsR;
		private SimpleMovingAverage _williamsRAverage;
		private decimal _prevWilliamsRValue;
		private decimal _prevWilliamsRAvgValue;
		
		/// <summary>
		/// Williams %R period.
		/// </summary>
		public int WilliamsRPeriod
		{
			get => _williamsRPeriod.Value;
			set => _williamsRPeriod.Value = value;
		}
		
		/// <summary>
		/// Period for Williams %R average calculation.
		/// </summary>
		public int AvgPeriod
		{
			get => _avgPeriod.Value;
			set => _avgPeriod.Value = value;
		}
		
		/// <summary>
		/// Standard deviation multiplier for breakout detection.
		/// </summary>
		public decimal Multiplier
		{
			get => _multiplier.Value;
			set => _multiplier.Value = value;
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
		/// Stop-loss percentage.
		/// </summary>
		public decimal StopLoss
		{
			get => _stopLoss.Value;
			set => _stopLoss.Value = value;
		}
		
		/// <summary>
		/// Initialize <see cref="WilliamsRBreakoutStrategy"/>.
		/// </summary>
		public WilliamsRBreakoutStrategy()
		{
			_williamsRPeriod = Param(nameof(WilliamsRPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("Williams %R Period", "Period for Williams %R indicator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 2);
			
			_avgPeriod = Param(nameof(AvgPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Average Period", "Period for Williams %R average calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);
			
			_multiplier = Param(nameof(Multiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Multiplier", "Standard deviation multiplier for breakout detection", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);
			
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
			
			_stopLoss = Param(nameof(StopLoss), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss %", "Stop Loss percentage", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 5.0m, 0.5m);
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
			
			_prevWilliamsRValue = 0;
			_prevWilliamsRAvgValue = 0;
			
			// Create indicators
			_williamsR = new WilliamsR { Length = WilliamsRPeriod };
			_williamsRAverage = new SimpleMovingAverage { Length = AvgPeriod };
			
			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			// Bind Williams %R to the candle subscription
			subscription
				.BindEx(_williamsR, ProcessWilliamsR)
				.Start();
				
			// Enable stop loss protection
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute),
				stopLoss: new Unit(StopLoss, UnitTypes.Percent)
			);
			
			// Create chart area for visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _williamsR);
				DrawOwnTrades(area);
			}
		}
		
		private void ProcessWilliamsR(ICandleMessage candle, IIndicatorValue williamsRValue)
		{
			if (candle.State != CandleStates.Finished)
				return;
			
			if (!williamsRValue.IsFinal)
				return;
			
			// Get current Williams %R value
			var currentWilliamsR = williamsRValue.ToDecimal();
			
			// Process Williams %R through average indicator
			var williamsRAvgValue = _williamsRAverage.Process(currentWilliamsR, candle.ServerTime, candle.State == CandleStates.Finished);
			var currentWilliamsRAvg = williamsRAvgValue.ToDecimal();
			
			// For first values, just save and skip
			if (_prevWilliamsRValue == 0)
			{
				_prevWilliamsRValue = currentWilliamsR;
				_prevWilliamsRAvgValue = currentWilliamsRAvg;
				return;
			}
			
			// Calculate standard deviation of Williams %R (simplified approach)
			var stdDev = Math.Abs(currentWilliamsR - currentWilliamsRAvg) * 1.5m; // Simplified approximation
			
			// Skip if indicators are not formed yet
			if (!_williamsRAverage.IsFormed)
			{
				_prevWilliamsRValue = currentWilliamsR;
				_prevWilliamsRAvgValue = currentWilliamsRAvg;
				return;
			}
			
			// Check if trading is allowed
			if (!IsFormedAndOnlineAndAllowTrading())
			{
				_prevWilliamsRValue = currentWilliamsR;
				_prevWilliamsRAvgValue = currentWilliamsRAvg;
				return;
			}
			
			// Williams %R breakout detection
			if (currentWilliamsR > currentWilliamsRAvg + Multiplier * stdDev && Position <= 0)
			{
				// Williams %R breaking out upward (but remember Williams %R is negative, less negative = bullish)
				CancelActiveOrders();
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (currentWilliamsR < currentWilliamsRAvg - Multiplier * stdDev && Position >= 0)
			{
				// Williams %R breaking out downward (more negative = bearish)
				CancelActiveOrders();
				SellMarket(Volume + Math.Abs(Position));
			}
			// Check for exit condition - Williams %R returns to average
			else if ((Position > 0 && currentWilliamsR < currentWilliamsRAvg) || 
					 (Position < 0 && currentWilliamsR > currentWilliamsRAvg))
			{
				// Exit position
				ClosePosition();
			}
			
			// Update previous values
			_prevWilliamsRValue = currentWilliamsR;
			_prevWilliamsRAvgValue = currentWilliamsRAvg;
		}
	}
}
