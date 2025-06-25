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
	/// Supertrend with Put/Call Ratio strategy.
	/// Entry condition:
	/// Long: Price > Supertrend && PCR < Avg(PCR, N) - k*StdDev(PCR, N)
	/// Short: Price < Supertrend && PCR > Avg(PCR, N) + k*StdDev(PCR, N)
	/// Exit condition:
	/// Long: Price < Supertrend
	/// Short: Price > Supertrend
	/// </summary>
	public class SupertrendWithPutCallRatioStrategy : Strategy
	{
		private readonly StrategyParam<int> _period;
		private readonly StrategyParam<decimal> _multiplier;
		private readonly StrategyParam<int> _pcrPeriod;
		private readonly StrategyParam<decimal> _pcrMultiplier;
		private readonly StrategyParam<DataType> _candleType;
		
		private readonly List<decimal> _pcrHistory = [];
		private decimal _pcrAverage;
		private decimal _pcrStdDev;
		
		private bool _isLong;
		private bool _isShort;
		
		// Simulated PCR value (in real implementation this would come from market data)
		private decimal _currentPcr;
		
		/// <summary>
		/// Supertrend period.
		/// </summary>
		public int Period
		{
			get => _period.Value;
			set => _period.Value = value;
		}
		
		/// <summary>
		/// Supertrend multiplier.
		/// </summary>
		public decimal Multiplier
		{
			get => _multiplier.Value;
			set => _multiplier.Value = value;
		}
		
		/// <summary>
		/// PCR averaging period.
		/// </summary>
		public int PCRPeriod
		{
			get => _pcrPeriod.Value;
			set => _pcrPeriod.Value = value;
		}
		
		/// <summary>
		/// PCR standard deviation multiplier for thresholds.
		/// </summary>
		public decimal PCRMultiplier
		{
			get => _pcrMultiplier.Value;
			set => _pcrMultiplier.Value = value;
		}
		
		/// <summary>
		/// Type of candles to use.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}
		
		/// <summary>
		/// Constructor with default parameters.
		/// </summary>
		public SupertrendWithPutCallRatioStrategy()
		{
			_period = Param(nameof(Period), 10)
				.SetGreaterThanZero()
				.SetDisplay("Supertrend Period", "Supertrend ATR period", "Supertrend Settings")
				.SetCanOptimize(true)
				.SetOptimize(5, 20, 3);
				
			_multiplier = Param(nameof(Multiplier), 3m)
				.SetGreaterThanZero()
				.SetDisplay("Supertrend Multiplier", "Supertrend ATR multiplier", "Supertrend Settings")
				.SetCanOptimize(true)
				.SetOptimize(2m, 4m, 0.5m);
				
			_pcrPeriod = Param(nameof(PCRPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("PCR Period", "Put/Call Ratio averaging period", "PCR Settings")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);
				
			_pcrMultiplier = Param(nameof(PCRMultiplier), 2m)
				.SetGreaterThanZero()
				.SetDisplay("PCR Std Dev Multiplier", "Multiplier for PCR standard deviation", "PCR Settings")
				.SetCanOptimize(true)
				.SetOptimize(1m, 3m, 0.5m);
				
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
			
			// Initialize flags
			_isLong = false;
			_isShort = false;
			
			// Create Supertrend indicator
			var atr = new AverageTrueRange { Length = Period };
			var supertrend = new SuperTrend { Atr = atr, Multiplier = Multiplier };
			
			// Subscribe to candles and bind indicator
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.Bind(supertrend, ProcessCandle)
				.Start();
			
			// Create chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, supertrend);
				DrawOwnTrades(area);
			}
		}
		
		/// <summary>
		/// Process each candle and Supertrend value.
		/// </summary>
		private void ProcessCandle(ICandleMessage candle, decimal supertrendValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;
			
			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Update PCR value (in a real system, this would come from market data)
			UpdatePCR(candle);
			
			// Calculate PCR thresholds based on historical data
			var bullishPcrThreshold = _pcrAverage - PCRMultiplier * _pcrStdDev;
			var bearishPcrThreshold = _pcrAverage + PCRMultiplier * _pcrStdDev;
			
			var price = candle.ClosePrice;
			var priceAboveSupertrend = price > supertrendValue;
			var priceBelowSupertrend = price < supertrendValue;
			
			// Trading logic
			
			// Entry conditions
			
			// Long entry: Price > Supertrend && PCR < bullish threshold (bullish PCR)
			if (priceAboveSupertrend && _currentPcr < bullishPcrThreshold && !_isLong && Position <= 0)
			{
				LogInfo($"Long signal: Price {price} > Supertrend {supertrendValue}, PCR {_currentPcr} < Threshold {bullishPcrThreshold}");
				BuyMarket(Volume);
				_isLong = true;
				_isShort = false;
			}
			// Short entry: Price < Supertrend && PCR > bearish threshold (bearish PCR)
			else if (priceBelowSupertrend && _currentPcr > bearishPcrThreshold && !_isShort && Position >= 0)
			{
				LogInfo($"Short signal: Price {price} < Supertrend {supertrendValue}, PCR {_currentPcr} > Threshold {bearishPcrThreshold}");
				SellMarket(Volume);
				_isShort = true;
				_isLong = false;
			}
			
			// Exit conditions (based only on Supertrend, not PCR)
			
			// Exit long: Price < Supertrend
			if (_isLong && priceBelowSupertrend && Position > 0)
			{
				LogInfo($"Exit long: Price {price} < Supertrend {supertrendValue}");
				SellMarket(Math.Abs(Position));
				_isLong = false;
			}
			// Exit short: Price > Supertrend
			else if (_isShort && priceAboveSupertrend && Position < 0)
			{
				LogInfo($"Exit short: Price {price} > Supertrend {supertrendValue}");
				BuyMarket(Math.Abs(Position));
				_isShort = false;
			}
		}
		
		/// <summary>
		/// Update Put/Call Ratio value.
		/// In a real implementation, this would fetch data from market.
		/// </summary>
		private void UpdatePCR(ICandleMessage candle)
		{
			// Simple PCR simulation
			// In reality, this would come from options market data
			var random = new Random();
			
			// Base PCR on candle pattern with some randomness
			decimal pcr;
			
			// Bullish candle tends to have lower PCR
			if (candle.ClosePrice > candle.OpenPrice)
			{
				pcr = 0.7m + (decimal)(random.NextDouble() * 0.3);
			}
			// Bearish candle tends to have higher PCR
			else
			{
				pcr = 1.0m + (decimal)(random.NextDouble() * 0.5);
			}
			
			_currentPcr = pcr;
			
			// Add to history
			_pcrHistory.Add(_currentPcr);
			if (_pcrHistory.Count > PCRPeriod)
			{
				_pcrHistory.RemoveAt(0);
			}
			
			// Calculate average
			decimal sum = 0;
			foreach (var value in _pcrHistory)
			{
				sum += value;
			}
			
			_pcrAverage = _pcrHistory.Count > 0 
				? sum / _pcrHistory.Count 
				: 1.0m; // Default to neutral (1.0)
				
			// Calculate standard deviation
			if (_pcrHistory.Count > 1)
			{
				decimal sumSquaredDiffs = 0;
				foreach (var value in _pcrHistory)
				{
					var diff = value - _pcrAverage;
					sumSquaredDiffs += diff * diff;
				}
				
				_pcrStdDev = (decimal)Math.Sqrt((double)(sumSquaredDiffs / (_pcrHistory.Count - 1)));
			}
			else
			{
				_pcrStdDev = 0.1m; // Default value until we have enough data
			}
			
			LogInfo($"PCR: {_currentPcr}, Avg: {_pcrAverage}, StdDev: {_pcrStdDev}");
		}
	}
}