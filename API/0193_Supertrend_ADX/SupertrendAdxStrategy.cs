using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Supertrend indicator and ADX for trend strength confirmation.
	/// 
	/// Entry criteria:
	/// Long: Price > Supertrend && ADX > 25 (uptrend with strong movement)
	/// Short: Price < Supertrend && ADX > 25 (downtrend with strong movement)
	/// 
	/// Exit criteria:
	/// Long: Price < Supertrend (price falls below Supertrend)
	/// Short: Price > Supertrend (price rises above Supertrend)
	/// </summary>
	public class SupertrendAdxStrategy : Strategy
	{
		private readonly StrategyParam<int> _supertrendPeriod;
		private readonly StrategyParam<decimal> _supertrendMultiplier;
		private readonly StrategyParam<int> _adxPeriod;
		private readonly StrategyParam<decimal> _adxThreshold;
		private readonly StrategyParam<DataType> _candleType;

		private decimal _lastSupertrend;
		private bool _isAboveSupertrend;

		/// <summary>
		/// Period for Supertrend calculation.
		/// </summary>
		public int SupertrendPeriod
		{
			get => _supertrendPeriod.Value;
			set => _supertrendPeriod.Value = value;
		}

		/// <summary>
		/// Multiplier for Supertrend calculation.
		/// </summary>
		public decimal SupertrendMultiplier
		{
			get => _supertrendMultiplier.Value;
			set => _supertrendMultiplier.Value = value;
		}

		/// <summary>
		/// Period for ADX calculation.
		/// </summary>
		public int AdxPeriod
		{
			get => _adxPeriod.Value;
			set => _adxPeriod.Value = value;
		}

		/// <summary>
		/// Threshold for ADX to confirm trend strength.
		/// </summary>
		public decimal AdxThreshold
		{
			get => _adxThreshold.Value;
			set => _adxThreshold.Value = value;
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
		public SupertrendAdxStrategy()
		{
			_supertrendPeriod = Param(nameof(SupertrendPeriod), 10)
				.SetGreaterThanZero()
				.SetDisplay("Supertrend Period", "Period for ATR calculation in Supertrend", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(5, 20, 5);

			_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3.0m)
				.SetGreaterThanZero()
				.SetDisplay("Supertrend Multiplier", "Multiplier for ATR in Supertrend", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 5.0m, 1.0m);

			_adxPeriod = Param(nameof(AdxPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(7, 21, 7);

			_adxThreshold = Param(nameof(AdxThreshold), 25m)
				.SetGreaterThanZero()
				.SetDisplay("ADX Threshold", "Minimum ADX value to confirm trend strength", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(20m, 30m, 5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_lastSupertrend = 0;
			_isAboveSupertrend = false;
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

			// Create indicators
			var atr = new AverageTrueRange { Length = SupertrendPeriod };
			var supertrend = new SuperTrend { Length = SupertrendPeriod, Multiplier = SupertrendMultiplier };
			var adx = new AverageDirectionalIndex { Length = AdxPeriod };

			// Create subscription and bind ATR to Supertrend
			var subscription = SubscribeCandles(CandleType);

			// Process candles with Supertrend and ADX indicators
			subscription
				.Bind(supertrend, adx, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, supertrend);
				DrawIndicator(area, adx);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal supertrendValue, decimal adxValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Determine current position relative to Supertrend
			var isAboveSupertrend = candle.ClosePrice > supertrendValue;
			var isStrongTrend = adxValue > AdxThreshold;

			// Log current state
			LogInfo($"Close: {candle.ClosePrice}, Supertrend: {supertrendValue}, ADX: {adxValue}, Above: {isAboveSupertrend}, Strong Trend: {isStrongTrend}");

			// Check for trend change (crossing Supertrend line)
			var trendChanged = isAboveSupertrend != _isAboveSupertrend && _lastSupertrend > 0;

			// Trading logic
			if (Position == 0) // No position
			{
				if (isAboveSupertrend && isStrongTrend)
				{
					// Buy signal
					BuyMarket(Volume);
					LogInfo($"Buy signal: Price above Supertrend with strong trend (ADX: {adxValue})");
				}
				else if (!isAboveSupertrend && isStrongTrend)
				{
					// Sell signal
					SellMarket(Volume);
					LogInfo($"Sell signal: Price below Supertrend with strong trend (ADX: {adxValue})");
				}
			}
			else if (trendChanged) // Exit on trend change
			{
				if (Position > 0 && !isAboveSupertrend)
				{
					// Exit long position
					SellMarket(Math.Abs(Position));
					LogInfo($"Exit long position: Price crossed below Supertrend ({supertrendValue})");
				}
				else if (Position < 0 && isAboveSupertrend)
				{
					// Exit short position
					BuyMarket(Math.Abs(Position));
					LogInfo($"Exit short position: Price crossed above Supertrend ({supertrendValue})");
				}
			}

			// Save current state
			_lastSupertrend = supertrendValue;
			_isAboveSupertrend = isAboveSupertrend;
		}
	}
}