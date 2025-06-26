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
	/// RSI with Option Open Interest Strategy.
	/// </summary>
	public class RsiWithOptionOpenInterestStrategy : Strategy
	{
		private readonly StrategyParam<int> _rsiPeriod;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _oiPeriod;
		private readonly StrategyParam<decimal> _oiDeviationFactor;
		private readonly StrategyParam<decimal> _stopLoss;
		
		private RelativeStrengthIndex _rsi;
		private SimpleMovingAverage _callOiSma;
		private SimpleMovingAverage _putOiSma;
		private StandardDeviation _callOiStdDev;
		private StandardDeviation _putOiStdDev;
		
		private decimal _currentCallOi;
		private decimal _currentPutOi;
		private decimal _avgCallOi;
		private decimal _avgPutOi;
		private decimal _stdDevCallOi;
		private decimal _stdDevPutOi;

		/// <summary>
		/// RSI Period.
		/// </summary>
		public int RsiPeriod
		{
			get => _rsiPeriod.Value;
			set => _rsiPeriod.Value = value;
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
		/// Open Interest averaging period.
		/// </summary>
		public int OiPeriod
		{
			get => _oiPeriod.Value;
			set => _oiPeriod.Value = value;
		}

		/// <summary>
		/// Standard deviation multiplier for OI threshold.
		/// </summary>
		public decimal OiDeviationFactor
		{
			get => _oiDeviationFactor.Value;
			set => _oiDeviationFactor.Value = value;
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
		/// Initialize <see cref="RsiWithOptionOpenInterestStrategy"/>.
		/// </summary>
		public RsiWithOptionOpenInterestStrategy()
		{
			_rsiPeriod = Param(nameof(RsiPeriod), 14)
				.SetRange(5, 30)
				.SetCanOptimize(true)
				.SetDisplay("RSI Period", "Period for RSI calculation", "Indicators");

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_oiPeriod = Param(nameof(OiPeriod), 20)
				.SetRange(10, 50)
				.SetCanOptimize(true)
				.SetDisplay("OI Period", "Period for Open Interest averaging", "Options");

			_oiDeviationFactor = Param(nameof(OiDeviationFactor), 2m)
				.SetRange(1m, 3m)
				.SetCanOptimize(true)
				.SetDisplay("OI StdDev Factor", "Standard deviation multiplier for OI threshold", "Options");

			_stopLoss = Param(nameof(StopLoss), 2m)
				.SetRange(1m, 5m)
				.SetCanOptimize(true)
				.SetDisplay("Stop Loss %", "Stop Loss percentage", "Risk Management");
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
			_rsi = new RelativeStrengthIndex
			{
				Length = RsiPeriod
			};

			// Indicators for Call options open interest
			_callOiSma = new SimpleMovingAverage
			{
				Length = OiPeriod
			};

			_callOiStdDev = new StandardDeviation
			{
				Length = OiPeriod
			};

			// Indicators for Put options open interest
			_putOiSma = new SimpleMovingAverage
			{
				Length = OiPeriod
			};

			_putOiStdDev = new StandardDeviation
			{
				Length = OiPeriod
			};

			// Reset state variables
			_currentCallOi = 0;
			_currentPutOi = 0;
			_avgCallOi = 0;
			_avgPutOi = 0;
			_stdDevCallOi = 0;
			_stdDevPutOi = 0;

			// Create candle subscription and bind RSI
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(_rsi, ProcessCandle)
				.Start();

			// Create subscription for option OI data (would be implemented in a real system)
			// Here we'll just simulate the data in the ProcessCandle method

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _rsi);
				DrawOwnTrades(area);
			}

			// Start position protection
			StartProtection(
				new Unit(2, UnitTypes.Percent),   // Take profit 2%
				new Unit(StopLoss, UnitTypes.Percent)  // Stop loss based on parameter
			);
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Get current RSI value
			var rsi = rsiValue.ToDecimal();

			// Simulate option open interest data (in real implementation, this would come from a data provider)
			SimulateOptionOI(candle);

			// Process option OI with indicators
			var callOiValueSma = _callOiSma.Process(_currentCallOi, candle.ServerTime, candle.State == CandleStates.Finished);
			var putOiValueSma = _putOiSma.Process(_currentPutOi, candle.ServerTime, candle.State == CandleStates.Finished);
			
			var callOiValueStdDev = _callOiStdDev.Process(_currentCallOi, candle.ServerTime, candle.State == CandleStates.Finished);
			var putOiValueStdDev = _putOiStdDev.Process(_currentPutOi, candle.ServerTime, candle.State == CandleStates.Finished);

			// Update state variables
			_avgCallOi = callOiValueSma.ToDecimal();
			_avgPutOi = putOiValueSma.ToDecimal();
			_stdDevCallOi = callOiValueStdDev.ToDecimal();
			_stdDevPutOi = putOiValueStdDev.ToDecimal();

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Calculate OI thresholds
			decimal callOiThreshold = _avgCallOi + OiDeviationFactor * _stdDevCallOi;
			decimal putOiThreshold = _avgPutOi + OiDeviationFactor * _stdDevPutOi;

			// Entry logic
			if (rsi < 30 && _currentCallOi > callOiThreshold && Position <= 0)
			{
				// RSI in oversold territory and Call OI spiking - Long entry
				BuyMarket(Volume);
				LogInfo($"Buy Signal: RSI={rsi}, Call OI={_currentCallOi}, Threshold={callOiThreshold}");
			}
			else if (rsi > 70 && _currentPutOi > putOiThreshold && Position >= 0)
			{
				// RSI in overbought territory and Put OI spiking - Short entry
				SellMarket(Volume);
				LogInfo($"Sell Signal: RSI={rsi}, Put OI={_currentPutOi}, Threshold={putOiThreshold}");
			}

			// Exit logic
			if (Position > 0 && rsi > 50)
			{
				// Exit long position when RSI returns to neutral zone
				SellMarket(Math.Abs(Position));
				LogInfo($"Exit Long: RSI={rsi}");
			}
			else if (Position < 0 && rsi < 50)
			{
				// Exit short position when RSI returns to neutral zone
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exit Short: RSI={rsi}");
			}
		}

		private void SimulateOptionOI(ICandleMessage candle)
		{
			// This is a placeholder for real option open interest data
			// In a real implementation, this would connect to an options data provider
			// We'll simulate some values based on price action for demonstration
			
			// Create pseudo-random but somewhat realistic values
			var random = new Random();
			
			// Base OI values on price movement
			bool priceUp = candle.OpenPrice < candle.ClosePrice;
			
			// Simulate bullish sentiment with higher call OI when price is rising
			// Simulate bearish sentiment with higher put OI when price is falling
			if (priceUp)
			{
				_currentCallOi = candle.TotalVolume * (1m + (decimal)random.NextDouble() * 0.5m);
				_currentPutOi = candle.TotalVolume * (0.7m + (decimal)random.NextDouble() * 0.3m);
			}
			else
			{
				_currentCallOi = candle.TotalVolume * (0.7m + (decimal)random.NextDouble() * 0.3m);
				_currentPutOi = candle.TotalVolume * (1m + (decimal)random.NextDouble() * 0.5m);
			}
			
			// Add some randomness for spikes
			if (random.NextDouble() > 0.9)
			{
				// Occasional spikes in OI
				_currentCallOi *= 1.5m;
			}
			
			if (random.NextDouble() > 0.9)
			{
				// Occasional spikes in OI
				_currentPutOi *= 1.5m;
			}
		}
	}
}
