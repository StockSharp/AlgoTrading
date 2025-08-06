using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Momentum adjusted by volatility (ATR)
	/// Enters positions when the volatility-adjusted momentum exceeds average plus a multiple of standard deviation
	/// </summary>
	public class VolatilityAdjustedMomentumStrategy : Strategy
	{
		private readonly StrategyParam<int> _momentumPeriod;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<int> _lookbackPeriod;
		private readonly StrategyParam<decimal> _deviationMultiplier;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<Unit> _stopLoss;

		private Momentum _momentum;
		private AverageTrueRange _atr;
		
		private decimal _momentumAtrRatio;
		private decimal _avgRatio;
		private decimal _stdDevRatio;
		private decimal[] _ratios;
		private int _currentIndex;

		/// <summary>
		/// Momentum period
		/// </summary>
		public int MomentumPeriod
		{
			get => _momentumPeriod.Value;
			set => _momentumPeriod.Value = value;
		}

		/// <summary>
		/// ATR period
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		/// <summary>
		/// Lookback period for statistics calculation
		/// </summary>
		public int LookbackPeriod
		{
			get => _lookbackPeriod.Value;
			set => _lookbackPeriod.Value = value;
		}

		/// <summary>
		/// Standard deviation multiplier for breakout detection
		/// </summary>
		public decimal DeviationMultiplier
		{
			get => _deviationMultiplier.Value;
			set => _deviationMultiplier.Value = value;
		}

		/// <summary>
		/// Candle type
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Stop loss value
		/// </summary>
		public Unit StopLoss
		{
			get => _stopLoss.Value;
			set => _stopLoss.Value = value;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public VolatilityAdjustedMomentumStrategy()
		{
			_momentumPeriod = Param(nameof(MomentumPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("Momentum Period", "Period for Momentum indicator", "Indicator Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 2);

			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "Period for Average True Range indicator", "Indicator Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 2);

			_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Lookback Period", "Period for statistics calculation", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_deviationMultiplier = Param(nameof(DeviationMultiplier), 2m)
				.SetGreaterThanZero()
				.SetDisplay("Deviation Multiplier", "Standard deviation multiplier for breakout detection", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(1m, 3m, 0.5m);
				
			_stopLoss = Param(nameof(StopLoss), new Unit(2, UnitTypes.Absolute))
				.SetDisplay("Stop Loss", "Stop loss value in ATRs", "Risk Management");

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return [(Security, CandleType)];
		}

		/// <inheritdoc />
		protected override void OnReseted()
		{
			base.OnReseted();
			_momentumAtrRatio = 0;
			_avgRatio = 0;
			_stdDevRatio = 0;
			_currentIndex = 0;
			_ratios = new decimal[LookbackPeriod];
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			_momentum = new Momentum { Length = MomentumPeriod };
			_atr = new AverageTrueRange { Length = AtrPeriod };
			
			_ratios = new decimal[LookbackPeriod];

			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(_momentum, _atr, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _momentum);
				DrawIndicator(area, _atr);
				DrawOwnTrades(area);
			}

			// Set up position protection
			StartProtection(
				takeProfit: null, // We'll handle exits via strategy logic
				stopLoss: StopLoss,
				isStopTrailing: true
			);

			base.OnStarted(time);
		}

		private void ProcessCandle(ICandleMessage candle, decimal momentumValue, decimal atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if indicators are formed
			if (!_momentum.IsFormed || !_atr.IsFormed)
				return;

			// Avoid division by zero
			if (atrValue == 0)
				return;
			
			// Calculate the momentum/ATR ratio
			_momentumAtrRatio = momentumValue / atrValue;
			
			// Store ratio in array and update index
			_ratios[_currentIndex] = _momentumAtrRatio;
			_currentIndex = (_currentIndex + 1) % LookbackPeriod;
			
			// Calculate statistics once we have enough data
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
				
			CalculateStatistics();
			
			// Trading logic
			if (Math.Abs(_avgRatio) > 0)  // Avoid division by zero
			{
				// Long signal: momentum/ATR ratio exceeds average + k*stddev (we don't have a long position)
				if (_momentumAtrRatio > _avgRatio + DeviationMultiplier * _stdDevRatio && Position <= 0)
				{
					// Cancel existing orders
					CancelActiveOrders();
					
					// Enter long position
					var volume = Volume + Math.Abs(Position);
					BuyMarket(volume);
					
					LogInfo($"Long signal: Momentum/ATR {_momentumAtrRatio} > Avg {_avgRatio} + {DeviationMultiplier}*StdDev {_stdDevRatio}");
				}
				// Short signal: momentum/ATR ratio falls below average - k*stddev (we don't have a short position)
				else if (_momentumAtrRatio < _avgRatio - DeviationMultiplier * _stdDevRatio && Position >= 0)
				{
					// Cancel existing orders
					CancelActiveOrders();
					
					// Enter short position
					var volume = Volume + Math.Abs(Position);
					SellMarket(volume);
					
					LogInfo($"Short signal: Momentum/ATR {_momentumAtrRatio} < Avg {_avgRatio} - {DeviationMultiplier}*StdDev {_stdDevRatio}");
				}
				
				// Exit conditions - when momentum/ATR ratio returns to average
				if (Position > 0 && _momentumAtrRatio < _avgRatio)
				{
					// Exit long position
					SellMarket(Math.Abs(Position));
					LogInfo($"Exit long: Momentum/ATR {_momentumAtrRatio} < Avg {_avgRatio}");
				}
				else if (Position < 0 && _momentumAtrRatio > _avgRatio)
				{
					// Exit short position
					BuyMarket(Math.Abs(Position));
					LogInfo($"Exit short: Momentum/ATR {_momentumAtrRatio} > Avg {_avgRatio}");
				}
			}
		}
		
		private void CalculateStatistics()
		{
			// Reset statistics
			_avgRatio = 0;
			decimal sumSquaredDiffs = 0;
			
			// Calculate average
			for (int i = 0; i < LookbackPeriod; i++)
			{
				_avgRatio += _ratios[i];
			}
			_avgRatio /= LookbackPeriod;
			
			// Calculate standard deviation
			for (int i = 0; i < LookbackPeriod; i++)
			{
				decimal diff = _ratios[i] - _avgRatio;
				sumSquaredDiffs += diff * diff;
			}
			
			_stdDevRatio = (decimal)Math.Sqrt((double)(sumSquaredDiffs / LookbackPeriod));
		}
	}
}