using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that combines RSI and Stochastic Oscillator for double confirmation
	/// of oversold and overbought conditions.
	/// </summary>
	public class RsiStochasticStrategy : Strategy
	{
		private readonly StrategyParam<int> _rsiPeriod;
		private readonly StrategyParam<decimal> _rsiOversold;
		private readonly StrategyParam<decimal> _rsiOverbought;
		private readonly StrategyParam<int> _stochPeriod;
		private readonly StrategyParam<int> _stochK;
		private readonly StrategyParam<int> _stochD;
		private readonly StrategyParam<decimal> _stochOversold;
		private readonly StrategyParam<decimal> _stochOverbought;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// RSI period.
		/// </summary>
		public int RsiPeriod
		{
			get => _rsiPeriod.Value;
			set => _rsiPeriod.Value = value;
		}

		/// <summary>
		/// RSI oversold level.
		/// </summary>
		public decimal RsiOversold
		{
			get => _rsiOversold.Value;
			set => _rsiOversold.Value = value;
		}

		/// <summary>
		/// RSI overbought level.
		/// </summary>
		public decimal RsiOverbought
		{
			get => _rsiOverbought.Value;
			set => _rsiOverbought.Value = value;
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
		public RsiStochasticStrategy()
		{
			_rsiPeriod = Param(nameof(RsiPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("RSI Period", "Period of the RSI indicator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(7, 21, 7);

			_rsiOversold = Param(nameof(RsiOversold), 30m)
				.SetNotNegative()
				.SetDisplay("RSI Oversold", "RSI level considered oversold", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(20m, 40m, 5m);

			_rsiOverbought = Param(nameof(RsiOverbought), 70m)
				.SetNotNegative()
				.SetDisplay("RSI Overbought", "RSI level considered overbought", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(60m, 80m, 5m);

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

			// Create indicators
			var rsi = new RelativeStrengthIndex
			{
				Length = RsiPeriod
			};

			var stochastic = new StochasticOscillator
			{
				K = { Length = StochK },
				D = { Length = StochD },
			};

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);

			subscription
				.BindEx(rsi, stochastic, ProcessIndicators)
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
				DrawIndicator(area, rsi);
				DrawIndicator(area, stochastic);
				DrawOwnTrades(area);
			}
		}

		/// <summary>
		/// Process indicator values.
		/// </summary>
		private void ProcessIndicators(ICandleMessage candle, IIndicatorValue rsiValue, IIndicatorValue stochValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var rsi = rsiValue.ToDecimal();
			var stochTyped = (StochasticOscillatorValue)stochValue;
			var stochK = stochTyped.K;
			// Long entry: double confirmation of oversold condition
			if (rsi < RsiOversold && stochK < StochOversold && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			// Short entry: double confirmation of overbought condition
			else if (rsi > RsiOverbought && stochK > StochOverbought && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}
			// Long exit: RSI returns to neutral zone
			else if (Position > 0 && rsi > 50)
			{
				SellMarket(Math.Abs(Position));
			}
			// Short exit: RSI returns to neutral zone
			else if (Position < 0 && rsi < 50)
			{
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}