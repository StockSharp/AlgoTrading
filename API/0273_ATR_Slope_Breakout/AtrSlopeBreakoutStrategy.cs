using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// ATR Slope Breakout Strategy (Strategy #273)
	/// </summary>
	public class AtrSlopeBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<int> _slopePeriod;
		private readonly StrategyParam<decimal> _breakoutMultiplier;
		private readonly StrategyParam<decimal> _stopLossAtrMultiplier;
		private readonly StrategyParam<DataType> _candleType;
		
		private AverageTrueRange _atr;
		private ExponentialMovingAverage _priceEma;
		private LinearRegression _atrSlope;
		private decimal _prevSlopeValue;
		private decimal _slopeAvg;
		private decimal _slopeStdDev;
		private decimal _sumSlope;
		private decimal _sumSlopeSquared;
		private readonly Queue<decimal> _slopeValues = [];
		private decimal _lastAtr;

		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		public int SlopePeriod
		{
			get => _slopePeriod.Value;
			set => _slopePeriod.Value = value;
		}

		public decimal BreakoutMultiplier
		{
			get => _breakoutMultiplier.Value;
			set => _breakoutMultiplier.Value = value;
		}
		
		public decimal StopLossAtrMultiplier
		{
			get => _stopLossAtrMultiplier.Value;
			set => _stopLossAtrMultiplier.Value = value;
		}

		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}
		
		public AtrSlopeBreakoutStrategy()
		{
			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "Period for ATR calculation", "Indicator")
				.SetCanOptimize(true)
				.SetOptimize(10, 20, 2);
				
			_slopePeriod = Param(nameof(SlopePeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Slope Period", "Period for slope average and standard deviation", "Indicator")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);
				
			_breakoutMultiplier = Param(nameof(BreakoutMultiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Breakout Multiplier", "Standard deviation multiplier for breakout", "Signal")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);
				
			_stopLossAtrMultiplier = Param(nameof(StopLossAtrMultiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss ATR Multiplier", "ATR multiplier for stop loss", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);
				
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
			_atr = new AverageTrueRange { Length = AtrPeriod };
			_priceEma = new ExponentialMovingAverage { Length = 20 }; // For trend direction
			_atrSlope = new LinearRegression { Length = 2 }; // For calculating slope
			
			_prevSlopeValue = 0;
			_slopeAvg = 0;
			_slopeStdDev = 0;
			_sumSlope = 0;
			_sumSlopeSquared = 0;
			_slopeValues.Clear();
			_lastAtr = 0;
			
			// Create subscription and bind indicator
			var subscription = SubscribeCandles(CandleType);
			
			// Using BindEx to get the full indicator value
			subscription
				.BindEx(_atr, ProcessCandle)
				.Start();
			
			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _atr);
				DrawIndicator(area, _priceEma);
				DrawOwnTrades(area);
			}
		}
		
		private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Get ATR value and track it for stop loss calculations
			decimal atr = atrValue.GetValue<decimal>();
			_lastAtr = atr;
			
			// Process price for trend direction
			decimal emaValue = _priceEma.Process(candle).GetValue<decimal>();
			bool priceAboveEma = candle.ClosePrice > emaValue;
			
			// Calculate ATR slope
			var currentSlopeValue = _atrSlope.Process(atr, candle.ServerTime, candle.State == CandleStates.Finished).GetValue<decimal>();

			// Update slope stats when we have 2 values to calculate slope
			if (_prevSlopeValue != 0)
			{
				// Calculate simple slope from current and previous values
				decimal slope = currentSlopeValue - _prevSlopeValue;
				
				// Update running statistics
				_slopeValues.Enqueue(slope);
				_sumSlope += slope;
				_sumSlopeSquared += slope * slope;
				
				// Remove oldest value if we have enough
				if (_slopeValues.Count > SlopePeriod)
				{
					var oldSlope = _slopeValues.Dequeue();
					_sumSlope -= oldSlope;
					_sumSlopeSquared -= oldSlope * oldSlope;
				}
				
				// Calculate average and standard deviation
				_slopeAvg = _sumSlope / _slopeValues.Count;
				decimal variance = (_sumSlopeSquared / _slopeValues.Count) - (_slopeAvg * _slopeAvg);
				_slopeStdDev = variance <= 0 ? 0 : (decimal)Math.Sqrt((double)variance);
				
				// Generate signals if we have enough data for statistics
				if (_slopeValues.Count >= SlopePeriod)
				{
					// Breakout logic - ATR slope increase indicates volatility expansion
					if (slope > _slopeAvg + BreakoutMultiplier * _slopeStdDev)
					{
						// Volatility breakout with price direction
						if (priceAboveEma && Position <= 0)
						{
							// Go long when price is above EMA (uptrend) during volatility expansion
							BuyMarket(Volume + Math.Abs(Position));
							LogInfo($"Long entry: ATR slope breakout above {_slopeAvg + BreakoutMultiplier * _slopeStdDev:F5} with price above EMA");
							
							// Set stop loss based on ATR
							SetStopLoss();
						}
						else if (!priceAboveEma && Position >= 0)
						{
							// Go short when price is below EMA (downtrend) during volatility expansion
							SellMarket(Volume + Math.Abs(Position));
							LogInfo($"Short entry: ATR slope breakout above {_slopeAvg + BreakoutMultiplier * _slopeStdDev:F5} with price below EMA");
							
							// Set stop loss based on ATR
							SetStopLoss();
						}
					}
					
					// Exit logic - Return to mean (volatility contraction)
					if (slope < _slopeAvg)
					{
						if (Position > 0)
						{
							SellMarket(Math.Abs(Position));
							LogInfo("Long exit: ATR slope returned to mean (volatility contraction)");
						}
						else if (Position < 0)
						{
							BuyMarket(Math.Abs(Position));
							LogInfo("Short exit: ATR slope returned to mean (volatility contraction)");
						}
					}
				}
			}
			
			// Update previous value for next iteration
			_prevSlopeValue = currentSlopeValue;
		}
		
		private void SetStopLoss()
		{
			// Use ATR-based stop loss
			if (_lastAtr > 0)
			{
				var stopLossAmount = _lastAtr * StopLossAtrMultiplier;
				
				// Remove any previous stop loss
				CancelProtections();
				
				// Set dynamic stop loss based on ATR
				StartProtection(
					null, // No take profit
					new Unit(stopLossAmount), // ATR-based stop loss
					false, // No trailing stop
					null,
					null,
					true); // Use market orders for faster execution
			}
		}
	}
}
