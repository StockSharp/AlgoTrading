using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Implementation of strategy #158 - Parabolic SAR + Stochastic.
	/// Buy when price is above SAR and Stochastic %K is below 20 (oversold).
	/// Sell when price is below SAR and Stochastic %K is above 80 (overbought).
	/// </summary>
	public class ParabolicSarStochasticStrategy : Strategy
	{
		private readonly StrategyParam<decimal> _accelerationFactor;
		private readonly StrategyParam<decimal> _maxAccelerationFactor;
		private readonly StrategyParam<int> _stochK;
		private readonly StrategyParam<int> _stochD;
		private readonly StrategyParam<int> _stochPeriod;
		private readonly StrategyParam<decimal> _stochOversold;
		private readonly StrategyParam<decimal> _stochOverbought;
		private readonly StrategyParam<DataType> _candleType;

		private decimal _lastStochK;
		private bool _isAboveSar;

		/// <summary>
		/// Parabolic SAR acceleration factor.
		/// </summary>
		public decimal AccelerationFactor
		{
			get => _accelerationFactor.Value;
			set => _accelerationFactor.Value = value;
		}

		/// <summary>
		/// Parabolic SAR maximum acceleration factor.
		/// </summary>
		public decimal MaxAccelerationFactor
		{
			get => _maxAccelerationFactor.Value;
			set => _maxAccelerationFactor.Value = value;
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
		/// Stochastic main period.
		/// </summary>
		public int StochPeriod
		{
			get => _stochPeriod.Value;
			set => _stochPeriod.Value = value;
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
		/// Candle type used for strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initialize <see cref="ParabolicSarStochasticStrategy"/>.
		/// </summary>
		public ParabolicSarStochasticStrategy()
		{
			_accelerationFactor = Param(nameof(AccelerationFactor), 0.02m)
				.SetRange(0.01m, 0.2m)
				.SetDisplay("Acceleration Factor", "Initial acceleration factor for SAR", "SAR Parameters");

			_maxAccelerationFactor = Param(nameof(MaxAccelerationFactor), 0.2m)
				.SetRange(0.05m, 0.5m)
				.SetDisplay("Max Acceleration Factor", "Maximum acceleration factor for SAR", "SAR Parameters");

			_stochK = Param(nameof(StochK), 3)
				.SetGreaterThanZero()
				.SetDisplay("Stochastic %K", "Stochastic %K smoothing period", "Stochastic Parameters");

			_stochD = Param(nameof(StochD), 3)
				.SetGreaterThanZero()
				.SetDisplay("Stochastic %D", "Stochastic %D smoothing period", "Stochastic Parameters");

			_stochPeriod = Param(nameof(StochPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("Stochastic Period", "Main period for Stochastic oscillator", "Stochastic Parameters");

			_stochOversold = Param(nameof(StochOversold), 20m)
				.SetRange(1, 100)
				.SetDisplay("Oversold Level", "Level below which market is considered oversold", "Stochastic Parameters");

			_stochOverbought = Param(nameof(StochOverbought), 80m)
				.SetRange(1, 100)
				.SetDisplay("Overbought Level", "Level above which market is considered overbought", "Stochastic Parameters");

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Candle type for strategy", "General");

			_lastStochK = 50; // Initialize to a neutral value
			_isAboveSar = false;
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
			var parabolicSar = new ParabolicSar
			{
				AccelerationStep = AccelerationFactor,
				AccelerationLimit = MaxAccelerationFactor
			};

			var stochastic = new StochasticOscillator
			{
				K = { Length = StochK },
				D = { Length = StochD },
			};

			// Reset state
			_lastStochK = 50;
			_isAboveSar = false;

			// Setup candle subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind indicators to candles
			subscription
				.BindEx(parabolicSar, stochastic, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, parabolicSar);
				
				// Create separate area for Stochastic
				var stochArea = CreateChartArea();
				if (stochArea != null)
				{
					DrawIndicator(stochArea, stochastic);
				}
				
				DrawOwnTrades(area);
			}

			// SAR itself will act as a dynamic stop-loss by reversing position
			// when price crosses SAR in the opposite direction
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue sarValue, IIndicatorValue stochKValue)
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var currentPrice = candle.ClosePrice;
			var priceAboveSar = currentPrice > sarValue;
			
			LogInfo($"Candle: {candle.OpenTime}, Close: {currentPrice}, " +
				   $"Parabolic SAR: {sarValue}, Stochastic %K: {stochKValue}, " +
				   $"IsAboveSAR: {priceAboveSar}, OldIsAboveSAR: {_isAboveSar}");

			// Check for SAR reversal signal (price crossing SAR)
			var sarSignalChange = priceAboveSar != _isAboveSar;

			// Trading rules
			if (priceAboveSar && stochKValue < StochOversold && Position <= 0)
			{
				// Buy signal - price above SAR (uptrend) and Stochastic oversold
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				
				LogInfo($"Buy signal: Price above SAR and Stochastic oversold ({stochKValue} < {StochOversold}). Volume: {volume}");
			}
			else if (!priceAboveSar && stochKValue > StochOverbought && Position >= 0)
			{
				// Sell signal - price below SAR (downtrend) and Stochastic overbought
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				
				LogInfo($"Sell signal: Price below SAR and Stochastic overbought ({stochKValue} > {StochOverbought}). Volume: {volume}");
			}
			// Check for SAR reversal - exit signals
			else if (sarSignalChange)
			{
				if (!priceAboveSar && Position > 0)
				{
					// Exit long position when price crosses below SAR
					SellMarket(Position);
					LogInfo($"Exit long: Price crossed below SAR. Position: {Position}");
				}
				else if (priceAboveSar && Position < 0)
				{
					// Exit short position when price crosses above SAR
					BuyMarket(Math.Abs(Position));
					LogInfo($"Exit short: Price crossed above SAR. Position: {Position}");
				}
			}

			// Update state for next iteration
			_lastStochK = stochKValue;
			_isAboveSar = priceAboveSar;
		}
	}
}
