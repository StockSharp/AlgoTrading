using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that combines ADX (Average Directional Index) for trend strength
	/// and Stochastic Oscillator for entry timing with oversold/overbought conditions.
	/// </summary>
	public class AdxStochasticStrategy : Strategy
	{
		private readonly StrategyParam<int> _adxPeriod;
		private readonly StrategyParam<decimal> _adxThreshold;
		private readonly StrategyParam<int> _stochPeriod;
		private readonly StrategyParam<int> _stochK;
		private readonly StrategyParam<int> _stochD;
		private readonly StrategyParam<decimal> _stochOversold;
		private readonly StrategyParam<decimal> _stochOverbought;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// ADX period.
		/// </summary>
		public int AdxPeriod
		{
			get => _adxPeriod.Value;
			set => _adxPeriod.Value = value;
		}

		/// <summary>
		/// ADX threshold for strong trend.
		/// </summary>
		public decimal AdxThreshold
		{
			get => _adxThreshold.Value;
			set => _adxThreshold.Value = value;
		}

		/// <summary>
		/// Stochastic period.
		/// </summary>
		public int StochPeriod
		{
			get => _stochPeriod.Value;
			set => _stochPeriod.Value = value;
		}

		/// <summary>
		/// Stochastic %K period.
		/// </summary>
		public int StochK
		{
			get => _stochK.Value;
			set => _stochK.Value = value;
		}

		/// <summary>
		/// Stochastic %D period.
		/// </summary>
		public int StochD
		{
			get => _stochD.Value;
			set => _stochD.Value = value;
		}

		/// <summary>
		/// Stochastic oversold level.
		/// </summary>
		public decimal StochOversold
		{
			get => _stochOversold.Value;
			set => _stochOversold.Value = value;
		}

		/// <summary>
		/// Stochastic overbought level.
		/// </summary>
		public decimal StochOverbought
		{
			get => _stochOverbought.Value;
			set => _stochOverbought.Value = value;
		}

		/// <summary>
		/// Stop loss percentage.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Candle type for strategy calculation.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Strategy constructor.
		/// </summary>
		public AdxStochasticStrategy()
		{
			_adxPeriod = Param(nameof(AdxPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ADX Period", "Period of the ADX indicator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(7, 21, 7);

			_adxThreshold = Param(nameof(AdxThreshold), 25m)
				.SetNotNegative()
				.SetDisplay("ADX Threshold", "ADX level considered strong trend", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(15m, 35m, 5m);

			_stochPeriod = Param(nameof(StochPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("Stochastic Period", "Period of the Stochastic Oscillator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(5, 20, 5);

			_stochK = Param(nameof(StochK), 3)
				.SetGreaterThanZero()
				.SetDisplay("Stochastic %K", "Smoothing of the %K line", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1, 5, 1);

			_stochD = Param(nameof(StochD), 3)
				.SetGreaterThanZero()
				.SetDisplay("Stochastic %D", "Smoothing of the %D line", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1, 5, 1);

			_stochOversold = Param(nameof(StochOversold), 20m)
				.SetNotNegative()
				.SetDisplay("Stochastic Oversold", "Level considered oversold", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10m, 30m, 5m);

			_stochOverbought = Param(nameof(StochOverbought), 80m)
				.SetNotNegative()
				.SetDisplay("Stochastic Overbought", "Level considered overbought", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(70m, 90m, 5m);

			_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management")
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

			// Create ADX indicator with all components
			var adx = new AverageDirectionalIndex
			{
				Length = AdxPeriod
			};

			// Create Stochastic indicator
			var stochastic = new StochasticOscillator
			{
				K = { Length = StochK },
				D = { Length = StochD }
			};

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);

			subscription
				.BindEx(adx, stochastic, ProcessIndicators)
				.Start();

			// Setup position protection
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent) // Percentage-based stop loss
			);

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, adx);
				DrawIndicator(area, stochastic);
				DrawOwnTrades(area);
			}
		}

		/// <summary>
		/// Process indicator values.
		/// </summary>
		private void ProcessIndicators(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue stochValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			var adx = adxValue.ToDecimal();
			var stoch = (StochasticValue)stochValue;
			var stochK = stoch.K;


			// Check if ADX indicates strong trend
			var isStrongTrend = adx > AdxThreshold;

			if (isStrongTrend)
			{
				// Determine trend direction using DI+ and DI- (using candle direction as a simple proxy)
				var isBullishTrend = candle.OpenPrice < candle.ClosePrice;
				var isBearishTrend = candle.OpenPrice > candle.ClosePrice;

				// Long entry: strong bullish trend with Stochastic oversold
				if (isBullishTrend && stochK < StochOversold && Position <= 0)
				{
					var volume = Volume + Math.Abs(Position);
					BuyMarket(volume);
				}
				// Short entry: strong bearish trend with Stochastic overbought
				else if (isBearishTrend && stochK > StochOverbought && Position >= 0)
				{
					var volume = Volume + Math.Abs(Position);
					SellMarket(volume);
				}
			}

			// Exit conditions
			if (adx < AdxThreshold)
			{
				// Exit all positions when trend weakens (ADX below threshold)
				if (Position != 0)
				{
					ClosePosition();
				}
			}
		}
	}
}