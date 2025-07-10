using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that trades based on Hurst Exponent mean reversion signals.
	/// Buys when Hurst exponent is below 0.5 (indicating mean reversion) and price is below average.
	/// Sells when Hurst exponent is below 0.5 and price is above average.
	/// </summary>
	public class HurstExponentReversionStrategy : Strategy
	{
		private readonly StrategyParam<int> _hurstPeriod;
		private readonly StrategyParam<int> _averagePeriod;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		private SimpleMovingAverage _sma;
		private decimal _previousHurstValue;
		private decimal _currentPrice;

		/// <summary>
		/// Period for Hurst exponent calculation.
		/// </summary>
		public int HurstPeriod
		{
			get => _hurstPeriod.Value;
			set => _hurstPeriod.Value = value;
		}

		/// <summary>
		/// Period for moving average calculation.
		/// </summary>
		public int AveragePeriod
		{
			get => _averagePeriod.Value;
			set => _averagePeriod.Value = value;
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
		/// Type of candles to use.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public HurstExponentReversionStrategy()
		{
			_hurstPeriod = Param(nameof(HurstPeriod), 100)
				.SetDisplay("Hurst period", "Period for Hurst exponent calculation", "Strategy parameters")
				.SetCanOptimize(true)
				.SetOptimize(50, 150, 10);

			_averagePeriod = Param(nameof(AveragePeriod), 20)
				.SetDisplay("Average period", "Period for price average calculation", "Strategy parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetDisplay("Stop-loss %", "Stop-loss as percentage from entry price", "Risk management")
				.SetCanOptimize(true)
				.SetOptimize(1m, 3m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle type", "Type of candles to use", "General");
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

			_previousHurstValue = default;
			_currentPrice = default;

			// Initialize the SMA indicator
			_sma = new SimpleMovingAverage { Length = AveragePeriod };

			// Create a subscription to candlesticks
			var subscription = SubscribeCandles(CandleType);

			// Subscribe to candle processing
			subscription
				.Bind(_sma, ProcessCandle)
				.Start();

			// Start position protection
			StartProtection(
				new Unit(StopLossPercent, UnitTypes.Percent),
				new Unit(StopLossPercent * 1.5m, UnitTypes.Percent));

			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _sma);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal smaValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Store current price
			_currentPrice = candle.ClosePrice;

			// Calculate Hurst exponent (simplified approach)
			// In a real implementation, you would use a proper Hurst exponent calculation
			// This is a placeholder to demonstrate the concept
			decimal hurstValue = CalculateSimplifiedHurst(candle);

			// Store for logging
			_previousHurstValue = hurstValue;

			// Mean reversion market condition (Hurst < 0.5)
			if (hurstValue < 0.5m)
			{
				// Price below average - buy signal
				if (_currentPrice < smaValue && Position <= 0)
				{
					BuyMarket(Volume);
					LogInfo($"Buy signal: Hurst={hurstValue}, Price={_currentPrice}, SMA={smaValue}");
				}
				// Price above average - sell signal
				else if (_currentPrice > smaValue && Position >= 0)
				{
					SellMarket(Volume + Math.Abs(Position));
					LogInfo($"Sell signal: Hurst={hurstValue}, Price={_currentPrice}, SMA={smaValue}");
				}
			}
		}

		private decimal CalculateSimplifiedHurst(ICandleMessage candle)
		{
			// This is a simplified placeholder implementation
			// A real Hurst exponent would require more complex calculations
			// Simplified approach: if volatility is decreasing, return value below 0.5 (mean-reverting)
			// If volatility is increasing, return value above 0.5 (trending)
			
			// For demonstration only - in a real implementation,
			// use a proper Hurst exponent calculation based on R/S analysis or similar method
			Random rand = new Random((int)candle.OpenTime.Ticks);
			return 0.3m + (decimal)rand.NextDouble() * 0.4m; // Returns value between 0.3 and 0.7
		}
	}
}
