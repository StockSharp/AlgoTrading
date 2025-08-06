using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Supertrend and Momentum indicators.
	/// </summary>
	public class SupertrendWithMomentumStrategy : Strategy
	{
		private readonly StrategyParam<int> _supertrendPeriod;
		private readonly StrategyParam<decimal> _supertrendMultiplier;
		private readonly StrategyParam<int> _momentumPeriod;
		private readonly StrategyParam<DataType> _candleType;

		// Store previous values to detect changes
		private decimal _prevMomentum;

		/// <summary>
		/// Supertrend period parameter.
		/// </summary>
		public int SupertrendPeriod
		{
			get => _supertrendPeriod.Value;
			set => _supertrendPeriod.Value = value;
		}

		/// <summary>
		/// Supertrend multiplier parameter.
		/// </summary>
		public decimal SupertrendMultiplier
		{
			get => _supertrendMultiplier.Value;
			set => _supertrendMultiplier.Value = value;
		}

		/// <summary>
		/// Momentum period parameter.
		/// </summary>
		public int MomentumPeriod
		{
			get => _momentumPeriod.Value;
			set => _momentumPeriod.Value = value;
		}

		/// <summary>
		/// Candle type parameter.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public SupertrendWithMomentumStrategy()
		{
			_supertrendPeriod = Param(nameof(SupertrendPeriod), 10)
				.SetGreaterThanZero()
				.SetDisplay("Supertrend Period", "Period of the Supertrend indicator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(5, 20, 1);

			_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3.0m)
				.SetGreaterThanZero()
				.SetDisplay("Supertrend Multiplier", "Multiplier for the Supertrend indicator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 5.0m, 0.5m);

			_momentumPeriod = Param(nameof(MomentumPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("Momentum Period", "Period of the Momentum indicator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(5, 30, 5);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return [(Security, CandleType)];
		}

\t\t/// <inheritdoc />
\t\tprotected override void OnReseted()
\t\t{
\t\t\tbase.OnReseted();

\t\t\t_prevMomentum = default;
\t\t}

\t\t/// <inheritdoc />
\t\tprotected override void OnStarted(DateTimeOffset time)
\t\t{
\t\t\tbase.OnStarted(time);

\t\t\t// Create indicators
\t\t\tvar supertrend = new SuperTrend
\t\t\t{
\t\t\t\tLength = SupertrendPeriod,
\t\t\t\tMultiplier = SupertrendMultiplier
\t\t\t};

\t\t\tvar momentum = new Momentum
\t\t\t{
\t\t\t\tLength = MomentumPeriod
\t\t\t};

\t\t\t// Subscribe to candles and bind indicators
\t\t\tvar subscription = SubscribeCandles(CandleType);

\t\t\tsubscription
\t\t\t\t.Bind(supertrend, momentum, ProcessCandle)
\t\t\t\t.Start();

\t\t\t// Setup chart if available
\t\t\tvar area = CreateChartArea();
\t\t\tif (area != null)
\t\t\t{
\t\t\t\tDrawCandles(area, subscription);
\t\t\t\tDrawIndicator(area, supertrend);
\t\t\t\tDrawIndicator(area, momentum);
\t\t\t\tDrawOwnTrades(area);
\t\t\t}

\t\t\t// Setup position protection
\t\t\tStartProtection(
\t\t\t\ttakeProfit: new Unit(2, UnitTypes.Percent),
\t\t\t\tstopLoss: new Unit(1, UnitTypes.Percent)
\t\t\t);
\t\t}

		private void ProcessCandle(ICandleMessage candle, decimal supertrendValue, decimal momentumValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var isAboveSupertrend = candle.ClosePrice > supertrendValue;
			var isMomentumRising = momentumValue > _prevMomentum;

			// Strategy logic:
			// Buy when price is above Supertrend and Momentum is rising
			// Sell when price is below Supertrend and Momentum is falling
			if (isAboveSupertrend && isMomentumRising && Position <= 0)
			{
				// Cancel any active orders before entering a new position
				CancelActiveOrders();

				// Calculate position size
				var volume = Volume + Math.Abs(Position);
				
				// Enter long position
				BuyMarket(volume);
			}
			else if (!isAboveSupertrend && !isMomentumRising && Position >= 0)
			{
				// Cancel any active orders before entering a new position
				CancelActiveOrders();

				// Calculate position size
				var volume = Volume + Math.Abs(Position);
				
				// Enter short position
				SellMarket(volume);
			}

			// Store current momentum value for next comparison
			_prevMomentum = momentumValue;
		}
	}
}
