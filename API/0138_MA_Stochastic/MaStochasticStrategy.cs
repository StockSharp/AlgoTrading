using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that combines Moving Average and Stochastic Oscillator.
	/// Enters positions when price is above MA and Stochastic shows oversold conditions (for longs)
	/// or when price is below MA and Stochastic shows overbought conditions (for shorts).
	/// </summary>
	public class MaStochasticStrategy : Strategy
	{
		private readonly StrategyParam<int> _maPeriod;
		private readonly StrategyParam<int> _stochPeriod;
		private readonly StrategyParam<int> _stochK;
		private readonly StrategyParam<int> _stochD;
		private readonly StrategyParam<decimal> _stochOversold;
		private readonly StrategyParam<decimal> _stochOverbought;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// Moving Average period.
		/// </summary>
		public int MaPeriod
		{
			get => _maPeriod.Value;
			set => _maPeriod.Value = value;
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
		public MaStochasticStrategy()
		{
			_maPeriod = Param(nameof(MaPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("MA Period", "Period of the Moving Average", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

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
				.SetOptimize(1.0m, 5.0m, 1.0m);

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
			var ma = new SimpleMovingAverage
			{
				Length = MaPeriod
			};

			var stochastic = new StochasticOscillator
			{
				K = { Length = StochK },
				D = { Length = StochD },
			};

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);

			subscription
				.Bind(ma, stochastic, ProcessCandles)
				.Start();

			// Setup position protection
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent) // Stop loss as percentage
			);

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, ma);
				DrawIndicator(area, stochastic);
				DrawOwnTrades(area);
			}
		}

		/// <summary>
		/// Process candles and indicator values.
		/// </summary>
		private void ProcessCandles(ICandleMessage candle, decimal maValue, decimal stochKValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Long entry: price above MA and Stochastic is oversold
			if (candle.ClosePrice > maValue && stochKValue < StochOversold && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			// Short entry: price below MA and Stochastic is overbought
			else if (candle.ClosePrice < maValue && stochKValue > StochOverbought && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}
			// Long exit: price falls below MA
			else if (Position > 0 && candle.ClosePrice < maValue)
			{
				SellMarket(Math.Abs(Position));
			}
			// Short exit: price rises above MA
			else if (Position < 0 && candle.ClosePrice > maValue)
			{
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}