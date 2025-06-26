using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Stochastic strategy with Implied Volatility Skew.
	/// </summary>
	public class StochasticImpliedVolatilitySkewStrategy : Strategy
	{
		private readonly StrategyParam<int> _stochLength;
		private readonly StrategyParam<int> _stochK;
		private readonly StrategyParam<int> _stochD;
		private readonly StrategyParam<int> _ivPeriod;
		private readonly StrategyParam<decimal> _stopLoss;
		private readonly StrategyParam<DataType> _candleType;
		
		private StochasticOscillator _stochastic;
		private SimpleMovingAverage _ivSkewSma;
		private decimal _currentIvSkew;
		private decimal _avgIvSkew;

		/// <summary>
		/// Stochastic length parameter.
		/// </summary>
		public int StochLength
		{
			get => _stochLength.Value;
			set => _stochLength.Value = value;
		}

		/// <summary>
		/// Stochastic %K smoothing parameter.
		/// </summary>
		public int StochK
		{
			get => _stochK.Value;
			set => _stochK.Value = value;
		}

		/// <summary>
		/// Stochastic %D smoothing parameter.
		/// </summary>
		public int StochD
		{
			get => _stochD.Value;
			set => _stochD.Value = value;
		}

		/// <summary>
		/// IV Skew averaging period.
		/// </summary>
		public int IvPeriod
		{
			get => _ivPeriod.Value;
			set => _ivPeriod.Value = value;
		}

		/// <summary>
		/// Stop loss percentage.
		/// </summary>
		public decimal StopLoss
		{
			get => _stopLoss.Value;
			set => _stopLoss.Value = value;
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
		/// Initialize <see cref="StochasticImpliedVolatilitySkewStrategy"/>.
		/// </summary>
		public StochasticImpliedVolatilitySkewStrategy()
		{
			_stochLength = Param(nameof(StochLength), 14)
				.SetRange(5, 30)
				.SetCanOptimize(true)
				.SetDisplay("Stoch Length", "Period for Stochastic Oscillator", "Indicators");

			_stochK = Param(nameof(StochK), 3)
				.SetRange(1, 10)
				.SetCanOptimize(true)
				.SetDisplay("Stoch %K", "Smoothing for Stochastic %K line", "Indicators");

			_stochD = Param(nameof(StochD), 3)
				.SetRange(1, 10)
				.SetCanOptimize(true)
				.SetDisplay("Stoch %D", "Smoothing for Stochastic %D line", "Indicators");

			_ivPeriod = Param(nameof(IvPeriod), 20)
				.SetRange(10, 50)
				.SetCanOptimize(true)
				.SetDisplay("IV Period", "Period for IV Skew averaging", "Options");

			_stopLoss = Param(nameof(StopLoss), 2m)
				.SetRange(1m, 5m)
				.SetCanOptimize(true)
				.SetDisplay("Stop Loss %", "Stop Loss percentage", "Risk Management");

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

			// Create Stochastic Oscillator
			_stochastic = new StochasticOscillator
			{
				Length = StochLength,
				K = StochK,
				D = StochD
			};

			// Create IV Skew SMA
			_ivSkewSma = new SimpleMovingAverage
			{
				Length = IvPeriod
			};

			// Reset state variables
			_currentIvSkew = 0;
			_avgIvSkew = 0;

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(_stochastic, ProcessCandle)
				.Start();

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _stochastic);
				DrawOwnTrades(area);
			}

			// Start position protection
			StartProtection(
				new Unit(2, UnitTypes.Percent),   // Take profit 2%
				new Unit(StopLoss, UnitTypes.Percent)  // Stop loss based on parameter
			);
		}

		private void ProcessCandle(ICandleMessage candle, decimal stochK, decimal stochD)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Simulate IV Skew data (in real implementation, this would come from options data provider)
			SimulateIvSkew(candle);

			// Process IV Skew with SMA
			var ivSkewSmaValue = _ivSkewSma.Process(_currentIvSkew, candle.ServerTime, candle.State == CandleStates.Finished);
			_avgIvSkew = ivSkewSmaValue.ToDecimal();

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Entry logic
			if (stochK < 20 && _currentIvSkew > _avgIvSkew && Position <= 0)
			{
				// Stochastic in oversold territory and IV Skew above average - Long entry
				BuyMarket(Volume);
				LogInfo($"Buy Signal: Stoch %K={stochK}, IV Skew={_currentIvSkew}, Avg IV Skew={_avgIvSkew}");
			}
			else if (stochK > 80 && _currentIvSkew < _avgIvSkew && Position >= 0)
			{
				// Stochastic in overbought territory and IV Skew below average - Short entry
				SellMarket(Volume);
				LogInfo($"Sell Signal: Stoch %K={stochK}, IV Skew={_currentIvSkew}, Avg IV Skew={_avgIvSkew}");
			}

			// Exit logic
			if (Position > 0 && stochK > 50)
			{
				// Exit long position when Stochastic returns to neutral zone
				SellMarket(Math.Abs(Position));
				LogInfo($"Exit Long: Stoch %K={stochK}");
			}
			else if (Position < 0 && stochK < 50)
			{
				// Exit short position when Stochastic returns to neutral zone
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exit Short: Stoch %K={stochK}");
			}
		}

		private void SimulateIvSkew(ICandleMessage candle)
		{
			// This is a placeholder for real IV Skew data
			// In a real implementation, this would connect to an options data provider
			// IV Skew measures the difference in IV between calls and puts at equidistant strikes
			
			// Create pseudo-random but somewhat realistic values
			var random = new Random();
			
			// Base IV Skew values on price movement and volatility
			bool priceUp = candle.OpenPrice < candle.ClosePrice;
			decimal candleRange = (candle.HighPrice - candle.LowPrice) / candle.LowPrice;
			
			// When prices are rising, puts are often bid up for protection (negative skew)
			// When prices are falling, calls become relatively cheaper (positive skew)
			if (priceUp)
			{
				// During uptrends, skew tends to be more negative
				_currentIvSkew = -0.1m - candleRange - (decimal)random.NextDouble() * 0.2m;
			}
			else
			{
				// During downtrends, skew can become less negative or even positive
				_currentIvSkew = 0.05m - candleRange + (decimal)random.NextDouble() * 0.2m;
			}
			
			// Add some randomness for market events
			if (random.NextDouble() > 0.95)
			{
				// Occasional extreme skew events (e.g., market fear or greed)
				_currentIvSkew *= 1.5m;
			}
		}
	}
}
