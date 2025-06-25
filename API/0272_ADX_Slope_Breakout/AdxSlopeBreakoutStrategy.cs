using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// ADX Slope Breakout Strategy (Strategy #272)
	/// </summary>
	public class AdxSlopeBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _adxPeriod;
		private readonly StrategyParam<int> _slopePeriod;
		private readonly StrategyParam<decimal> _breakoutMultiplier;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;
		
		private AverageDirectionalIndex _adx;
		private LinearRegression _adxSlope;
		private decimal _prevSlopeValue;
		private decimal _slopeAvg;
		private decimal _slopeStdDev;
		private decimal _sumSlope;
		private decimal _sumSlopeSquared;
		private readonly Queue<decimal> _slopeValues = [];

		public int AdxPeriod
		{
			get => _adxPeriod.Value;
			set => _adxPeriod.Value = value;
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

		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}
		
		public AdxSlopeBreakoutStrategy()
		{
			_adxPeriod = Param(nameof(AdxPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ADX Period", "Period for ADX calculation", "Indicator")
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
				
			_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
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
			_adx = new AverageDirectionalIndex { Length = AdxPeriod };
			_adxSlope = new LinearRegression { Length = 2 }; // For calculating slope
			
			_prevSlopeValue = 0;
			_slopeAvg = 0;
			_slopeStdDev = 0;
			_sumSlope = 0;
			_sumSlopeSquared = 0;
			_slopeValues.Clear();
			
			// Create subscription and bind indicator
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(_adx, ProcessCandle)
				.Start();
			
			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _adx);
				DrawOwnTrades(area);
			}
			
			// Enable position protection
			StartProtection(new Unit(StopLossPercent, UnitTypes.Percent));
		}
		
		private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Get ADX value
			decimal adx = adxValue.GetValue<decimal>();
			
			// Calculate ADX slope
			var currentSlopeValue = _adxSlope.Process(new DecimalIndicatorValue(adx)).GetValue<decimal>();

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
					// Get DI+ and DI- from the ADX indicator for trend direction
					var diPlus = adxValue.GetValue<decimal>("DI+");
					var diMinus = adxValue.GetValue<decimal>("DI-");
					var isBullish = diPlus > diMinus;
					
					// Breakout logic
					if (slope > _slopeAvg + BreakoutMultiplier * _slopeStdDev && Position <= 0)
					{
						// ADX slope breakout indicates stronger trend 
						// Only go long if DI+ > DI- (bullish)
						if (isBullish)
						{
							BuyMarket(Volume + Math.Abs(Position));
							LogInfo($"Long entry: ADX slope breakout above {_slopeAvg + BreakoutMultiplier * _slopeStdDev:F2} with DI+ > DI-");
						}
					}
					else if (slope > _slopeAvg + BreakoutMultiplier * _slopeStdDev && Position >= 0)
					{
						// ADX slope breakout indicates stronger trend
						// Only go short if DI+ < DI- (bearish)
						if (!isBullish)
						{
							SellMarket(Volume + Math.Abs(Position));
							LogInfo($"Short entry: ADX slope breakout above {_slopeAvg + BreakoutMultiplier * _slopeStdDev:F2} with DI+ < DI-");
						}
					}
					
					// Exit logic - Return to mean or ADX weakening
					if (Position > 0 && (slope < _slopeAvg || !isBullish))
					{
						SellMarket(Math.Abs(Position));
						LogInfo("Long exit: ADX slope returned to mean or trend changed to bearish");
					}
					else if (Position < 0 && (slope < _slopeAvg || isBullish))
					{
						BuyMarket(Math.Abs(Position));
						LogInfo("Short exit: ADX slope returned to mean or trend changed to bullish");
					}
				}
			}
			
			// Update previous value for next iteration
			_prevSlopeValue = currentSlopeValue;
		}
	}
}
