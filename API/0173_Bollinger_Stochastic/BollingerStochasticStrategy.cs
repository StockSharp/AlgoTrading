using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that combines Bollinger Bands and Stochastic Oscillator to identify overbought and oversold conditions.
	/// Buy when price reaches lower Bollinger Band and Stochastic is below 20.
	/// Sell when price reaches upper Bollinger Band and Stochastic is above 80.
	/// </summary>
	public class BollingerStochasticStrategy : Strategy
	{
		private readonly StrategyParam<int> _bollingerLength;
		private readonly StrategyParam<decimal> _bollingerDeviation;
		private readonly StrategyParam<int> _stochasticPeriod;
		private readonly StrategyParam<int> _stochasticK;
		private readonly StrategyParam<int> _stochasticD;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// Bollinger Bands period.
		/// </summary>
		public int BollingerLength
		{
			get => _bollingerLength.Value;
			set => _bollingerLength.Value = value;
		}

		/// <summary>
		/// Bollinger Bands deviation multiplier.
		/// </summary>
		public decimal BollingerDeviation
		{
			get => _bollingerDeviation.Value;
			set => _bollingerDeviation.Value = value;
		}

		/// <summary>
		/// Stochastic Oscillator period.
		/// </summary>
		public int StochasticPeriod
		{
			get => _stochasticPeriod.Value;
			set => _stochasticPeriod.Value = value;
		}

		/// <summary>
		/// Stochastic %K period.
		/// </summary>
		public int StochasticK
		{
			get => _stochasticK.Value;
			set => _stochasticK.Value = value;
		}

		/// <summary>
		/// Stochastic %D period.
		/// </summary>
		public int StochasticD
		{
			get => _stochasticD.Value;
			set => _stochasticD.Value = value;
		}

		/// <summary>
		/// Candle type to use for strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BollingerStochasticStrategy"/>.
		/// </summary>
		public BollingerStochasticStrategy()
		{
			_bollingerLength = Param(nameof(BollingerLength), 20)
				.SetGreaterThanZero()
				.SetDisplay("Bollinger Length", "Length of the Bollinger Bands indicator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
				.SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_stochasticPeriod = Param(nameof(StochasticPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("Stochastic Period", "Stochastic Oscillator period length", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(5, 30, 5);

			_stochasticK = Param(nameof(StochasticK), 3)
				.SetGreaterThanZero()
				.SetDisplay("Stochastic %K", "%K smoothing period", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1, 10, 1);

			_stochasticD = Param(nameof(StochasticD), 3)
				.SetGreaterThanZero()
				.SetDisplay("Stochastic %D", "%D smoothing period", "Indicators")
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

			// Create Bollinger Bands indicator
			var bollingerBands = new BollingerBands
			{
				Length = BollingerLength,
				Width = BollingerDeviation
			};

			// Create Stochastic Oscillator
			var stochastic = new StochasticOscillator
			{
				KPeriod = StochasticPeriod,
				KSmoothingPeriod = StochasticK,
				DSmoothingPeriod = StochasticD
			};

			// Create subscription for candles
			var subscription = SubscribeCandles(CandleType);

			// Bind indicators to candle subscription
			subscription
				.Bind(bollingerBands, stochastic, (candle, bollinger, stoch) => ProcessCandle(candle, bollinger.MiddleBand, bollinger.UpperBand, bollinger.LowerBand, stoch.K, stoch.D))
				.Apply(this);

			// Enable position protection with stop-loss
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit
				stopLoss: new Unit(2, UnitTypes.Percent)	 // 2% stop-loss
			);

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, bollingerBands);

				var stochArea = CreateChartArea();
				if (stochArea != null)
				{
					// Draw stochastic in a separate panel
					DrawIndicator(stochArea, stochastic);
				}

				DrawOwnTrades(area);
			}
		}

		/// <summary>
		/// Process candle with indicator values.
		/// </summary>
		private void ProcessCandle(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand, decimal stochK, decimal stochD)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Trading logic:
			// Long: Price below lower Bollinger Band and Stochastic %K below 20 (oversold)
			if (candle.ClosePrice <= lowerBand && stochK < 20 && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				LogInfo($"Buy signal: Price below lower BB ({lowerBand:F2}) and Stochastic K ({stochK:F2}) below 20");
			}
			// Short: Price above upper Bollinger Band and Stochastic %K above 80 (overbought)
			else if (candle.ClosePrice >= upperBand && stochK > 80 && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				LogInfo($"Sell signal: Price above upper BB ({upperBand:F2}) and Stochastic K ({stochK:F2}) above 80");
			}
			// Exit Long: Price moves back to middle band
			else if (candle.ClosePrice >= middleBand && Position > 0)
			{
				SellMarket(Math.Abs(Position));
				LogInfo($"Exit long position: Price ({candle.ClosePrice:F2}) reached middle band ({middleBand:F2})");
			}
			// Exit Short: Price moves back to middle band
			else if (candle.ClosePrice <= middleBand && Position < 0)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exit short position: Price ({candle.ClosePrice:F2}) reached middle band ({middleBand:F2})");
			}
		}
	}
}