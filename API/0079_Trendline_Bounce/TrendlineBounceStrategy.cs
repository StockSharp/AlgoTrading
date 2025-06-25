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
	/// Strategy #79: Trendline Bounce strategy.
	/// The strategy automatically identifies trendlines by connecting highs or lows
	/// and enters positions when price bounces off a trendline with confirmation.
	/// </summary>
	public class TrendlineBounceStrategy : Strategy
	{
		private readonly StrategyParam<int> _trendlinePeriod;
		private readonly StrategyParam<int> _maPeriod;
		private readonly StrategyParam<decimal> _bounceThresholdPercent;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLossPercent;
		
		private SimpleMovingAverage _ma;
		
		// Store recent candles for trendline calculation
		private readonly Queue<ICandleMessage> _recentCandles = new Queue<ICandleMessage>();
		
		// Trendline parameters
		private decimal _supportSlope;
		private decimal _supportIntercept;
		private decimal _resistanceSlope;
		private decimal _resistanceIntercept;
		private DateTimeOffset _lastTrendlineUpdate;

		/// <summary>
		/// Period for trendline calculation.
		/// </summary>
		public int TrendlinePeriod
		{
			get => _trendlinePeriod.Value;
			set => _trendlinePeriod.Value = value;
		}

		/// <summary>
		/// Moving average period for trend confirmation.
		/// </summary>
		public int MAPeriod
		{
			get => _maPeriod.Value;
			set => _maPeriod.Value = value;
		}

		/// <summary>
		/// Bounce threshold percentage for entry signals.
		/// </summary>
		public decimal BounceThresholdPercent
		{
			get => _bounceThresholdPercent.Value;
			set => _bounceThresholdPercent.Value = value;
		}

		/// <summary>
		/// Candle type.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Stop-loss percentage.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public TrendlineBounceStrategy()
		{
			_trendlinePeriod = Param(nameof(TrendlinePeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Trendline Period", "Number of candles to use for trendline calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_maPeriod = Param(nameof(MAPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("MA Period", "Period for moving average calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 10);

			_bounceThresholdPercent = Param(nameof(BounceThresholdPercent), 0.5m)
				.SetNotNegative()
				.SetDisplay("Bounce Threshold %", "Maximum distance from trendline for bounce detection", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(0.1m, 1.0m, 0.1m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
				.SetNotNegative()
				.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);
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

			// Reset variables
			_recentCandles.Clear();
			_supportSlope = 0;
			_supportIntercept = 0;
			_resistanceSlope = 0;
			_resistanceIntercept = 0;
			_lastTrendlineUpdate = DateTimeOffset.MinValue;

			// Create MA indicator
			_ma = new SimpleMovingAverage
			{
				Length = MAPeriod
			};

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.Bind(_ma, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _ma);
				DrawOwnTrades(area);
			}

			// Start position protection
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit, rely on exit logic
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
			);
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue maValue)
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Add current candle to the queue and maintain queue size
			_recentCandles.Enqueue(candle);
			while (_recentCandles.Count > TrendlinePeriod && _recentCandles.Count > 0)
				_recentCandles.Dequeue();

			// Update trendlines periodically
			if (_lastTrendlineUpdate == DateTimeOffset.MinValue || 
				(candle.OpenTime - _lastTrendlineUpdate).TotalMinutes >= TrendlinePeriod)
			{
				UpdateTrendlines();
				_lastTrendlineUpdate = candle.OpenTime;
			}

			// Get current MA value for trend confirmation
			decimal maPrice = maValue.GetValue<decimal>();

			// Check for trendline bounces
			CheckForTrendlineBounces(candle, maPrice);
		}

		private void UpdateTrendlines()
		{
			if (_recentCandles.Count < 3)
				return;

			var candles = _recentCandles.ToArray();
			int n = candles.Length;
			
			// Calculate support trendline (connecting lows)
			List<(decimal x, decimal y)> supportPoints = new List<(decimal, decimal)>();
			
			// Find significant lows for support line
			for (int i = 1; i < n - 1; i++)
			{
				// A point is a low if it's lower than both neighbors
				if (candles[i].LowPrice < candles[i-1].LowPrice && 
					candles[i].LowPrice < candles[i+1].LowPrice)
				{
					supportPoints.Add((i, candles[i].LowPrice));
				}
			}
			
			// Calculate resistance trendline (connecting highs)
			List<(decimal x, decimal y)> resistancePoints = new List<(decimal, decimal)>();
			
			// Find significant highs for resistance line
			for (int i = 1; i < n - 1; i++)
			{
				// A point is a high if it's higher than both neighbors
				if (candles[i].HighPrice > candles[i-1].HighPrice && 
					candles[i].HighPrice > candles[i+1].HighPrice)
				{
					resistancePoints.Add((i, candles[i].HighPrice));
				}
			}

			// We need at least 2 points to define a line
			if (supportPoints.Count >= 2)
			{
				CalculateLinearRegression(supportPoints, out _supportSlope, out _supportIntercept);
				LogInfo($"Updated support trendline: y = {_supportSlope}x + {_supportIntercept}");
			}
			
			if (resistancePoints.Count >= 2)
			{
				CalculateLinearRegression(resistancePoints, out _resistanceSlope, out _resistanceIntercept);
				LogInfo($"Updated resistance trendline: y = {_resistanceSlope}x + {_resistanceIntercept}");
			}
		}

		private void CalculateLinearRegression(List<(decimal x, decimal y)> points, out decimal slope, out decimal intercept)
		{
			int n = points.Count;
			decimal sumX = 0;
			decimal sumY = 0;
			decimal sumXY = 0;
			decimal sumX2 = 0;

			foreach (var point in points)
			{
				sumX += point.x;
				sumY += point.y;
				sumXY += point.x * point.y;
				sumX2 += point.x * point.x;
			}

			decimal denominator = n * sumX2 - sumX * sumX;
			
			// Avoid division by zero
			if (denominator == 0)
			{
				slope = 0;
				intercept = (n > 0) ? sumY / n : 0;
				return;
			}

			slope = (n * sumXY - sumX * sumY) / denominator;
			intercept = (sumY - slope * sumX) / n;
		}

		private void CheckForTrendlineBounces(ICandleMessage candle, decimal maPrice)
		{
			if (_recentCandles.Count < 3)
				return;

			// Calculate the x-coordinate for the current candle
			decimal x = _recentCandles.Count - 1;
			
			// Calculate trendline values at current x
			decimal supportValue = _supportSlope * x + _supportIntercept;
			decimal resistanceValue = _resistanceSlope * x + _resistanceIntercept;
			
			LogInfo($"Current candle: Close={candle.ClosePrice}, Support={supportValue}, Resistance={resistanceValue}");
			
			// Determine if price is near a trendline
			decimal bounceThreshold = candle.ClosePrice * (BounceThresholdPercent / 100);
			
			bool nearSupport = Math.Abs(candle.LowPrice - supportValue) <= bounceThreshold;
			bool nearResistance = Math.Abs(candle.HighPrice - resistanceValue) <= bounceThreshold;
			
			// Check for bullish bounce off support
			if (nearSupport && candle.ClosePrice > candle.OpenPrice && candle.ClosePrice > supportValue && Position <= 0)
			{
				// Bullish candle bouncing off support - go long
				if (maPrice < candle.ClosePrice)  // Only go long if price is above MA (uptrend)
				{
					CancelActiveOrders();
					BuyMarket(Volume + Math.Abs(Position));
					LogInfo($"Long entry at {candle.ClosePrice} on support bounce at {supportValue}");
				}
			}
			// Check for bearish bounce off resistance
			else if (nearResistance && candle.ClosePrice < candle.OpenPrice && candle.ClosePrice < resistanceValue && Position >= 0)
			{
				// Bearish candle bouncing off resistance - go short
				if (maPrice > candle.ClosePrice)  // Only go short if price is below MA (downtrend)
				{
					CancelActiveOrders();
					SellMarket(Volume + Math.Abs(Position));
					LogInfo($"Short entry at {candle.ClosePrice} on resistance bounce at {resistanceValue}");
				}
			}

			// Exit logic based on MA crossover
			if (Position > 0 && candle.ClosePrice < maPrice)
			{
				SellMarket(Math.Abs(Position));
				LogInfo($"Long exit at {candle.ClosePrice} (price below MA {maPrice})");
			}
			else if (Position < 0 && candle.ClosePrice > maPrice)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short exit at {candle.ClosePrice} (price above MA {maPrice})");
			}
		}
	}
}