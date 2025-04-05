using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Strategies
{
	/// <summary>
	/// Strategy that trades on On Balance Volume (OBV) breakouts.
	/// When OBV rises significantly above its average, it enters a long position.
	/// When OBV falls significantly below its average, it enters a short position.
	/// </summary>
	public class OBVBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _avgPeriod;
		private readonly StrategyParam<decimal> _multiplier;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLoss;
		
		private OnBalanceVolume _obv;
		private SimpleMovingAverage _obvAverage;
		private decimal _prevObvValue;
		private decimal _prevObvAvgValue;
		
		/// <summary>
		/// Period for OBV average calculation.
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
		/// Initialize <see cref="OBVBreakoutStrategy"/>.
		/// </summary>
		public OBVBreakoutStrategy()
		{
			_avgPeriod = Param(nameof(AvgPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Average Period", "Period for OBV average calculation", "Indicators")
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
			return new[] { (Security, CandleType) };
		}
		
		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);
			
			_prevObvValue = 0;
			_prevObvAvgValue = 0;
			
			// Create indicators
			_obv = new OnBalanceVolume();
			_obvAverage = new SimpleMovingAverage { Length = AvgPeriod };
			
			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			// Bind OBV to the candle subscription
			subscription
				.BindEx(_obv, ProcessObv)
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
				DrawIndicator(area, _obv);
				DrawOwnTrades(area);
			}
		}
		
		private void ProcessObv(ICandleMessage candle, IIndicatorValue obvValue)
		{
			if (candle.State != CandleStates.Finished)
				return;
			
			if (!obvValue.IsFinal)
				return;
			
			// Get current OBV value
			var currentObv = obvValue.GetValue<decimal>();
			
			// Process OBV through average indicator
			var obvAvgValue = _obvAverage.Process(new DecimalIndicatorValue(currentObv));
			var currentObvAvg = obvAvgValue.GetValue<decimal>();
			
			// For first values, just save and skip
			if (_prevObvValue == 0)
			{
				_prevObvValue = currentObv;
				_prevObvAvgValue = currentObvAvg;
				return;
			}
			
			// Calculate standard deviation of OBV (simplified approach)
			var stdDev = Math.Abs(currentObv - currentObvAvg) * 1.5m; // Simplified approximation
			
			// Skip if indicators are not formed yet
			if (!_obvAverage.IsFormed)
			{
				_prevObvValue = currentObv;
				_prevObvAvgValue = currentObvAvg;
				return;
			}
			
			// Check if trading is allowed
			if (!IsFormedAndOnlineAndAllowTrading())
			{
				_prevObvValue = currentObv;
				_prevObvAvgValue = currentObvAvg;
				return;
			}
			
			// OBV breakout detection
			if (currentObv > currentObvAvg + Multiplier * stdDev && Position <= 0)
			{
				// OBV breaking out upward - Buy
				CancelActiveOrders();
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (currentObv < currentObvAvg - Multiplier * stdDev && Position >= 0)
			{
				// OBV breaking out downward - Sell
				CancelActiveOrders();
				SellMarket(Volume + Math.Abs(Position));
			}
			// Check for exit condition - OBV returns to average
			else if ((Position > 0 && currentObv < currentObvAvg) || 
					 (Position < 0 && currentObv > currentObvAvg))
			{
				// Exit position
				ClosePosition();
			}
			
			// Update previous values
			_prevObvValue = currentObv;
			_prevObvAvgValue = currentObvAvg;
		}
	}
}
