using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Volume Slope Breakout Strategy (Strategy #274)
	/// </summary>
	public class VolumeSlopeBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _volumeSmaPeriod;
		private readonly StrategyParam<int> _slopePeriod;
		private readonly StrategyParam<decimal> _breakoutMultiplier;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;
		
		private VolumeIndicator _volumeIndicator;
		private SimpleMovingAverage _volumeSma;
		private ExponentialMovingAverage _priceEma;
		private LinearRegression _volumeSlope;
		private decimal _prevSlopeValue;
		private decimal _slopeAvg;
		private decimal _slopeStdDev;
		private decimal _sumSlope;
		private decimal _sumSlopeSquared;
		private readonly Queue<decimal> _slopeValues = new Queue<decimal>();

		public int VolumeSMAPeriod
		{
			get => _volumeSmaPeriod.Value;
			set => _volumeSmaPeriod.Value = value;
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
		
		public VolumeSlopeBreakoutStrategy()
		{
			_volumeSmaPeriod = Param(nameof(VolumeSMAPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Volume SMA Period", "Period for volume SMA calculation", "Indicator")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);
				
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
			_volumeIndicator = new VolumeIndicator();
			_volumeSma = new SimpleMovingAverage { Length = VolumeSMAPeriod };
			_priceEma = new ExponentialMovingAverage { Length = 20 }; // For trend direction
			_volumeSlope = new LinearRegression { Length = 2 }; // For calculating slope
			
			_prevSlopeValue = 0;
			_slopeAvg = 0;
			_slopeStdDev = 0;
			_sumSlope = 0;
			_sumSlopeSquared = 0;
			_slopeValues.Clear();
			
			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind indicators and processing logic using event handlers
			// since we need to track multiple indicator values
			subscription.WhenCandlesFinished(this)
				.Do(ProcessCandle)
				.Apply(this);
			
			// Start subscription
			subscription.Start();
			
			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _volumeIndicator);
				DrawIndicator(area, _volumeSma);
				DrawIndicator(area, _priceEma);
				DrawOwnTrades(area);
			}
			
			// Enable position protection
			StartProtection(new Unit(StopLossPercent, UnitTypes.Percent));
		}
		
		private void ProcessCandle(ICandleMessage candle)
		{
			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Process volume indicator
			decimal volumeValue = _volumeIndicator.Process(candle).GetValue<decimal>();
			
			// Process volume SMA
			decimal volumeSma = _volumeSma.Process(new DecimalIndicatorValue(volumeValue)).GetValue<decimal>();
			
			// Process price EMA for trend direction
			decimal priceEma = _priceEma.Process(candle).GetValue<decimal>();
			bool priceAboveEma = candle.ClosePrice > priceEma;
			
			// Calculate volume slope (current volume relative to SMA)
			decimal volumeRatio = volumeValue / volumeSma;
			
			// We use LinearRegression to calculate slope of this ratio
			var currentSlopeValue = _volumeSlope.Process(new DecimalIndicatorValue(volumeRatio)).GetValue<decimal>();

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
				if (_slopeValues.Count >= SlopePeriod && volumeValue > volumeSma)
				{
					// Breakout logic - Volume slope increase with price confirmation
					if (slope > _slopeAvg + BreakoutMultiplier * _slopeStdDev)
					{
						if (priceAboveEma && Position <= 0)
						{
							// Go long on volume spike with price above EMA
							BuyMarket(Volume + Math.Abs(Position));
							LogInfo($"Long entry: Volume slope breakout above {_slopeAvg + BreakoutMultiplier * _slopeStdDev:F3} with price above EMA");
						}
						else if (!priceAboveEma && Position >= 0)
						{
							// Go short on volume spike with price below EMA
							SellMarket(Volume + Math.Abs(Position));
							LogInfo($"Short entry: Volume slope breakout above {_slopeAvg + BreakoutMultiplier * _slopeStdDev:F3} with price below EMA");
						}
					}
					
					// Exit logic - Volume spike down (unusual activity ending)
					if (slope < _slopeAvg - BreakoutMultiplier * _slopeStdDev)
					{
						if (Position > 0)
						{
							SellMarket(Math.Abs(Position));
							LogInfo("Long exit: Volume activity declining");
						}
						else if (Position < 0)
						{
							BuyMarket(Math.Abs(Position));
							LogInfo("Short exit: Volume activity declining");
						}
					}
				}
				
				// Additional exit rule - Return to mean with lower volume
				if (volumeValue < volumeSma && slope < _slopeAvg)
				{
					if (Position > 0)
					{
						SellMarket(Math.Abs(Position));
						LogInfo("Long exit: Volume returned to normal levels");
					}
					else if (Position < 0)
					{
						BuyMarket(Math.Abs(Position));
						LogInfo("Short exit: Volume returned to normal levels");
					}
				}
			}
			
			// Update previous value for next iteration
			_prevSlopeValue = currentSlopeValue;
		}
	}
}
