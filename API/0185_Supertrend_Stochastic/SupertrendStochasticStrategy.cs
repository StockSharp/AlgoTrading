using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Supertrend + Stochastic strategy.
	/// Strategy enters trades when Supertrend indicates trend direction and Stochastic confirms with oversold/overbought conditions.
	/// </summary>
	public class SupertrendStochasticStrategy : Strategy
	{
		private readonly StrategyParam<int> _supertrendPeriod;
		private readonly StrategyParam<decimal> _supertrendMultiplier;
		private readonly StrategyParam<int> _stochPeriod;
		private readonly StrategyParam<int> _stochK;
		private readonly StrategyParam<int> _stochD;
		private readonly StrategyParam<DataType> _candleType;

		// Indicators
		private SuperTrend _supertrend;
		private StochasticOscillator _stochastic;

		/// <summary>
		/// Supertrend period.
		/// </summary>
		public int SupertrendPeriod
		{
			get => _supertrendPeriod.Value;
			set => _supertrendPeriod.Value = value;
		}

		/// <summary>
		/// Supertrend multiplier.
		/// </summary>
		public decimal SupertrendMultiplier
		{
			get => _supertrendMultiplier.Value;
			set => _supertrendMultiplier.Value = value;
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
		/// Candle type.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public SupertrendStochasticStrategy()
		{
			_supertrendPeriod = Param(nameof(SupertrendPeriod), 10)
				.SetGreaterThanZero()
				.SetDisplay("Supertrend Period", "Supertrend ATR period length", "Supertrend")
				.SetCanOptimize(true)
				.SetOptimize(5, 20, 1);

			_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3.0m)
				.SetGreaterThanZero()
				.SetDisplay("Supertrend Multiplier", "Supertrend ATR multiplier", "Supertrend")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 5.0m, 0.5m);

			_stochPeriod = Param(nameof(StochPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("Stochastic Period", "Stochastic oscillator period", "Stochastic")
				.SetCanOptimize(true)
				.SetOptimize(5, 30, 5);

			_stochK = Param(nameof(StochK), 3)
				.SetGreaterThanZero()
				.SetDisplay("Stochastic %K", "Stochastic %K period", "Stochastic")
				.SetCanOptimize(true)
				.SetOptimize(1, 10, 1);

			_stochD = Param(nameof(StochD), 3)
				.SetGreaterThanZero()
				.SetDisplay("Stochastic %D", "Stochastic %D period", "Stochastic")
				.SetCanOptimize(true)
				.SetOptimize(1, 10, 1);

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
			_supertrend = new()
			{
				Length = SupertrendPeriod,
				Multiplier = SupertrendMultiplier
			};

			_stochastic = new()
			{
				K = { Length = StochK },
				D = { Length = StochD },
			};

			// Enable dynamic stop-loss using Supertrend
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute), // No take-profit, use strategy logic
				stopLoss: new Unit(0, UnitTypes.Absolute)	// Dynamic stop-loss set below
			);

			// Subscribe to candles and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.BindEx(_supertrend, _stochastic, ProcessCandle)
				.Start();

			// Setup chart
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _supertrend);
				
				var secondArea = CreateChartArea();
				if (secondArea != null)
				{
					DrawIndicator(secondArea, _stochastic);
				}
				
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(
			ICandleMessage candle, 
			IIndicatorValue supertrendValue, 
			IIndicatorValue stochasticValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Get indicator values
			var supertrend = (SuperTrendIndicatorValue)supertrendValue;
			decimal supertrendLine = supertrend.Value;
			
			// Is trend bullish or bearish
			bool isBullish = supertrend.IsUpTrend;
			bool isBearish = !isBullish;
			
			var stoch = (StochasticOscillatorValue)stochasticValue;
			decimal stochK = stoch.K;
			decimal stochD = stoch.D;

			bool isAboveSupertrend = candle.ClosePrice > supertrendLine;
			bool isBelowSupertrend = candle.ClosePrice < supertrendLine;

			// Trading logic:
			// Buy when price is above Supertrend line (bullish) and Stochastic shows oversold condition
			if (isAboveSupertrend && isBullish && stochK < 20 && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				LogInfo($"Long entry: Price={candle.ClosePrice}, Supertrend={supertrendLine}, Stochastic %K={stochK}");
			}
			// Sell when price is below Supertrend line (bearish) and Stochastic shows overbought condition
			else if (isBelowSupertrend && isBearish && stochK > 80 && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				LogInfo($"Short entry: Price={candle.ClosePrice}, Supertrend={supertrendLine}, Stochastic %K={stochK}");
			}
			// Exit long position when price falls below Supertrend line
			else if (Position > 0 && isBelowSupertrend)
			{
				SellMarket(Math.Abs(Position));
				LogInfo($"Long exit: Price={candle.ClosePrice}, Below Supertrend={supertrendLine}");
			}
			// Exit short position when price rises above Supertrend line
			else if (Position < 0 && isAboveSupertrend)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short exit: Price={candle.ClosePrice}, Above Supertrend={supertrendLine}");
			}

			// Set Supertrend value as dynamic stop-loss
			if (Position != 0)
			{
				if (Position > 0 && isBullish)
				{
					// For long positions, set stop to Supertrend line
					var stopDistance = candle.ClosePrice - supertrendLine;
					var stopPercentage = stopDistance / candle.ClosePrice * 100;
					
					// Only set stop if it's reasonable (not too tight)
					if (stopPercentage > 0.3m)
					{
						StartProtection(
							takeProfit: new Unit(0, UnitTypes.Absolute),
							stopLoss: new Unit(stopPercentage, UnitTypes.Percent)
						);
					}
				}
				else if (Position < 0 && isBearish)
				{
					// For short positions, set stop to Supertrend line
					var stopDistance = supertrendLine - candle.ClosePrice;
					var stopPercentage = stopDistance / candle.ClosePrice * 100;
					
					// Only set stop if it's reasonable (not too tight)
					if (stopPercentage > 0.3m)
					{
						StartProtection(
							takeProfit: new Unit(0, UnitTypes.Absolute),
							stopLoss: new Unit(stopPercentage, UnitTypes.Percent)
						);
					}
				}
			}
		}
	}
}