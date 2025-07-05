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
	/// Strategy based on Upthrust Reversal pattern, which occurs when price makes a new high above resistance 
	/// but immediately reverses and closes below the resistance level, indicating a bearish reversal.
	/// </summary>
	public class UpthrustReversalStrategy : Strategy
	{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _lookbackPeriod;
		private readonly StrategyParam<int> _maPeriod;
		private readonly StrategyParam<decimal> _stopLossPercent;

		private SimpleMovingAverage _ma;
		private Highest _highest;
		
		private decimal _lastHighestValue;

		/// <summary>
		/// Candle type and timeframe for the strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Period for high range detection.
		/// </summary>
		public int LookbackPeriod
		{
			get => _lookbackPeriod.Value;
			set => _lookbackPeriod.Value = value;
		}

		/// <summary>
		/// Period for moving average calculation.
		/// </summary>
		public int MaPeriod
		{
			get => _maPeriod.Value;
			set => _maPeriod.Value = value;
		}

		/// <summary>
		/// Stop-loss percentage from entry price.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UpthrustReversalStrategy"/>.
		/// </summary>
		public UpthrustReversalStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
						 .SetDisplay("Candle Type", "Type of candles to use for analysis", "General");
			
			_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
							 .SetDisplay("Lookback Period", "Period for resistance level detection", "Range")
							 .SetRange(5, 50);
			
			_maPeriod = Param(nameof(MaPeriod), 20)
					   .SetDisplay("MA Period", "Period for moving average calculation", "Trend")
					   .SetRange(5, 50);
			
			_stopLossPercent = Param(nameof(StopLossPercent), 1m)
							  .SetDisplay("Stop Loss %", "Stop-loss percentage from entry price", "Protection")
							  .SetRange(0.5m, 3m);
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
			_ma = new SimpleMovingAverage { Length = MaPeriod };
			_highest = new Highest { Length = LookbackPeriod };
			
			// Create and setup subscription for candles
			var subscription = SubscribeCandles(CandleType);
			
			// Bind indicators and processor
			subscription
				.Bind(_ma, _highest, ProcessCandle)
				.Start();
			
			// Enable stop-loss protection
			StartProtection(new Unit(0), new Unit(StopLossPercent, UnitTypes.Percent));
			
			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _ma);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal? ma, decimal? highest)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Store the last highest value
			_lastHighestValue = highest;
			
			// Determine candle characteristics
			bool isBearish = candle.ClosePrice < candle.OpenPrice;
			bool piercesAboveResistance = candle.HighPrice > _lastHighestValue;
			bool closeBelowResistance = candle.ClosePrice < _lastHighestValue;
			
			// Upthrust pattern:
			// 1. Price spikes above recent high (resistance level)
			// 2. But closes below the resistance level (bearish rejection)
			if (piercesAboveResistance && closeBelowResistance && isBearish)
			{
				// Enter short position only if we're not already short
				if (Position >= 0)
				{
					var volume = Volume + Math.Abs(Position);
					SellMarket(volume);
					
					LogInfo($"Upthrust Reversal detected. Resistance level: {_lastHighestValue}, High: {candle.HighPrice}. Short entry at {candle.ClosePrice}");
				}
			}
			
			// Exit conditions
			if (Position < 0)
			{
				// Exit when price falls below the moving average (take profit)
				if (candle.ClosePrice < ma)
				{
					BuyMarket(Math.Abs(Position));
					
					LogInfo($"Exit signal: Price below MA. Closed short position at {candle.ClosePrice}");
				}
			}
		}
	}
}