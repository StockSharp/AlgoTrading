using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Hurst Exponent Trend strategy.
	/// Uses Hurst exponent to identify trending markets.
	/// </summary>
	public class HurstExponentTrendStrategy : Strategy
	{
		private readonly StrategyParam<int> _hurstPeriodParam;
		private readonly StrategyParam<int> _maPeriodParam;
		private readonly StrategyParam<decimal> _hurstThresholdParam;
		private readonly StrategyParam<DataType> _candleTypeParam;

		private HurstExponent _hurst;
		private SimpleMovingAverage _sma;

		/// <summary>
		/// Hurst exponent calculation period.
		/// </summary>
		public int HurstPeriod
		{
			get => _hurstPeriodParam.Value;
			set => _hurstPeriodParam.Value = value;
		}

		/// <summary>
		/// Moving average period.
		/// </summary>
		public int MaPeriod
		{
			get => _maPeriodParam.Value;
			set => _maPeriodParam.Value = value;
		}

		/// <summary>
		/// Hurst exponent threshold for trend identification.
		/// </summary>
		public decimal HurstThreshold
		{
			get => _hurstThresholdParam.Value;
			set => _hurstThresholdParam.Value = value;
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
		public HurstExponentTrendStrategy()
		{
			_hurstPeriodParam = Param(nameof(HurstPeriod), 100)
				.SetGreaterThanZero()
				.SetDisplay("Hurst Period", "Period for Hurst exponent calculation", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(50, 150, 25);

			_maPeriodParam = Param(nameof(MaPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("MA Period", "Period for Moving Average", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 10);

			_hurstThresholdParam = Param(nameof(HurstThreshold), 0.55m)
				.SetRange(0.1m, 0.9m)
				.SetDisplay("Hurst Threshold", "Threshold value for trend identification", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(0.5m, 0.6m, 0.05m);

			_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Candle type for strategy", "Common");
		}

		/// <summary>
		/// Returns working securities.
		/// </summary>
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return [(Security, CandleType)];
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create indicators
			_hurst = new HurstExponent { Length = HurstPeriod };
			_sma = new SimpleMovingAverage { Length = MaPeriod };

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(_hurst, _sma, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _sma);
				DrawOwnTrades(area);
			}
			
			// Enable position protection
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit
				stopLoss: new Unit(2, UnitTypes.Percent) // 2% stop loss
			);
		}

		private void ProcessCandle(ICandleMessage candle, decimal hurstValue, decimal smaValue)
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Check if market is trending (Hurst > 0.5 indicates trending market)
			bool isTrending = hurstValue > HurstThreshold;
			
			if (isTrending)
			{
				// In trending markets, use price relative to MA to determine direction
				
				// Long setup - trending market with price above MA
				if (candle.ClosePrice > smaValue && Position <= 0)
				{
					// Buy signal - trending market with price above MA
					BuyMarket(Volume + Math.Abs(Position));
				}
				// Short setup - trending market with price below MA
				else if (candle.ClosePrice < smaValue && Position >= 0)
				{
					// Sell signal - trending market with price below MA
					SellMarket(Volume + Math.Abs(Position));
				}
			}
			else
			{
				// In non-trending markets, exit positions
				if (Position > 0)
				{
					SellMarket(Position);
				}
				else if (Position < 0)
				{
					BuyMarket(Math.Abs(Position));
				}
			}
		}
		
		/// <summary>
		/// Custom Hurst Exponent indicator implementation.
		/// </summary>
		private class HurstExponent : BaseIndicator
		{
			private readonly Queue<decimal> _prices = new Queue<decimal>();
			
			/// <summary>
			/// Period for calculation.
			/// </summary>
			public int Length { get; set; } = 100;
			
			/// <summary>
			/// Create a new instance of HurstExponent.
			/// </summary>
			public HurstExponent()
			{
			}
			
			/// <summary>
			/// Process input value.
			/// </summary>
			protected override IIndicatorValue OnProcess(IIndicatorValue input)
			{
				var candle = input.GetValue<ICandleMessage>();
				
				_prices.Enqueue(candle.ClosePrice);
				
				if (_prices.Count > Length)
					_prices.Dequeue();
				
				if (_prices.Count < Length)
					return new DecimalIndicatorValue(this, 0.5m);
				
				// Calculate Hurst exponent using R/S analysis
				var priceArray = _prices.ToArray();
				var hurstValue = CalculateHurst(priceArray);
				
				return new DecimalIndicatorValue(this, hurstValue);
			}
			
			private decimal CalculateHurst(decimal[] prices)
			{
				if (prices.Length < 10)
					return 0.5m; // Default to random walk
				
				var returns = new decimal[prices.Length - 1];
				for (int i = 0; i < returns.Length; i++)
				{
					returns[i] = prices[i + 1] / prices[i] - 1;
				}
				
				// Generate different time scales
				var scales = new[] { 8, 16, 32, 64 };
				var rsValues = new List<decimal>();
				var logScales = new List<decimal>();
				
				foreach (var scale in scales)
				{
					if (scale >= returns.Length)
						continue;
						
					var chunks = returns.Length / scale;
					if (chunks < 1)
						continue;
						
					var rs = new decimal[chunks];
					
					for (int i = 0; i < chunks; i++)
					{
						var chunk = returns.Skip(i * scale).Take(scale).ToArray();
						var mean = chunk.Average();
						
						// Calculate cumulative deviation
						var cumulativeDeviation = new decimal[chunk.Length];
						var sum = 0m;
						
						for (int j = 0; j < chunk.Length; j++)
						{
							sum += chunk[j] - mean;
							cumulativeDeviation[j] = sum;
						}
						
						// Calculate range and standard deviation
						var range = cumulativeDeviation.Max() - cumulativeDeviation.Min();
						
						var stdDev = 0m;
						foreach (var val in chunk)
						{
							stdDev += (val - mean) * (val - mean);
						}
						stdDev = (decimal)Math.Sqrt((double)(stdDev / chunk.Length));
						
						if (stdDev > 0)
							rs[i] = range / stdDev;
						else
							rs[i] = 1; // Avoid division by zero
					}
					
					var meanRs = rs.Average();
					rsValues.Add(meanRs);
					logScales.Add((decimal)Math.Log10((double)scale));
				}
				
				// Linear regression to estimate Hurst exponent
				if (rsValues.Count < 2)
					return 0.5m;
					
				var sumX = 0m;
				var sumY = 0m;
				var sumXY = 0m;
				var sumX2 = 0m;
				
				for (int i = 0; i < rsValues.Count; i++)
				{
					var x = logScales[i];
					var y = (decimal)Math.Log10((double)rsValues[i]);
					
					sumX += x;
					sumY += y;
					sumXY += x * y;
					sumX2 += x * x;
				}
				
				var n = rsValues.Count;
				var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
				
				// Hurst exponent is the slope of the log-log plot
				return Math.Min(Math.Max(slope, 0m), 1m); // Clamp between 0 and 1
			}
			
			/// <summary>
			/// Indicator is formed when we have enough data points.
			/// </summary>
			public override bool IsFormed => _prices.Count >= Length;
			
			/// <summary>
			/// Reset the indicator to initial state.
			/// </summary>
			public override void Reset()
			{
				base.Reset();
				_prices.Clear();
			}
			
			/// <summary>
			/// Name of the indicator.
			/// </summary>
			public override string Name => "Hurst Exponent";
		}
	}
}