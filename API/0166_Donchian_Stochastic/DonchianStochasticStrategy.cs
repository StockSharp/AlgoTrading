using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Donchian Channel + Stochastic strategy.
	/// Strategy enters the market when the price breaks out of Donchian Channel with Stochastic confirming oversold/overbought conditions.
	/// </summary>
	public class DonchianStochasticStrategy : Strategy
	{
		private readonly StrategyParam<int> _donchianPeriod;
		private readonly StrategyParam<int> _stochPeriod;
		private readonly StrategyParam<int> _stochK;
		private readonly StrategyParam<int> _stochD;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLossPercent;

		// Indicators
		private DonchianChannels _donchian;
		private StochasticOscillator _stochastic;

		/// <summary>
		/// Donchian Channel period.
		/// </summary>
		public int DonchianPeriod
		{
			get => _donchianPeriod.Value;
			set => _donchianPeriod.Value = value;
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
		public DonchianStochasticStrategy()
		{
			_donchianPeriod = Param(nameof(DonchianPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Donchian Period", "Donchian Channel lookback period", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_stochPeriod = Param(nameof(StochPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("Stochastic Period", "Stochastic oscillator period", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(5, 30, 5);

			_stochK = Param(nameof(StochK), 3)
				.SetGreaterThanZero()
				.SetDisplay("Stochastic %K", "Stochastic %K period", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1, 10, 1);

			_stochD = Param(nameof(StochD), 3)
				.SetGreaterThanZero()
				.SetDisplay("Stochastic %D", "Stochastic %D period", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1, 10, 1);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1m, 5m, 0.5m);
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
			_donchian = new DonchianChannels
			{
				Length = DonchianPeriod
			};

			_stochastic = new StochasticOscillator
			{
				K = { Length = StochK },
				D = { Length = StochD },
			};

			// Enable position protection
			var takeProfitUnit = new Unit(0, UnitTypes.Absolute); // No take profit - we'll exit based on strategy rules
			var stopLossUnit = new Unit(StopLossPercent, UnitTypes.Percent);
			StartProtection(takeProfitUnit, stopLossUnit);

			// Subscribe to candles and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.Bind(_donchian, _stochastic, ProcessCandle)
				.Start();

			// Setup chart
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _donchian);
				
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
			(decimal upper, decimal middle, decimal lower) donchianValues, 
			(decimal k, decimal d) stochValues)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			decimal upperBand = donchianValues.upper;
			decimal lowerBand = donchianValues.lower;
			decimal middleBand = donchianValues.middle;
			decimal stochK = stochValues.k;

			// Trading logic:
			// Buy when price breaks above upper Donchian band with Stochastic showing oversold condition
			if (candle.ClosePrice >= upperBand && stochK < 20 && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				LogInfo($"Long entry: Price={candle.ClosePrice}, Upper Band={upperBand}, Stochastic %K={stochK}");
			}
			// Sell when price breaks below lower Donchian band with Stochastic showing overbought condition
			else if (candle.ClosePrice <= lowerBand && stochK > 80 && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				LogInfo($"Short entry: Price={candle.ClosePrice}, Lower Band={lowerBand}, Stochastic %K={stochK}");
			}
			// Exit long position when price falls below middle band
			else if (Position > 0 && candle.ClosePrice < middleBand)
			{
				SellMarket(Math.Abs(Position));
				LogInfo($"Long exit: Price={candle.ClosePrice}, Middle Band={middleBand}");
			}
			// Exit short position when price rises above middle band
			else if (Position < 0 && candle.ClosePrice > middleBand)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short exit: Price={candle.ClosePrice}, Middle Band={middleBand}");
			}
		}
	}
}