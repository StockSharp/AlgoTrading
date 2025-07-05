using Ecng.Common;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using System;
using System.Collections.Generic;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Hull MA with Implied Volatility Breakout strategy.
	/// Entry condition:
	/// Long: HMA(t) > HMA(t-1) && IV > Avg(IV, N) + k*StdDev(IV, N)
	/// Short: HMA(t) < HMA(t-1) && IV > Avg(IV, N) + k*StdDev(IV, N)
	/// Exit condition:
	/// Long: HMA(t) < HMA(t-1)
	/// Short: HMA(t) > HMA(t-1)
	/// </summary>
	public class HullMAWithImpliedVolatilityBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _hmaPeriod;
		private readonly StrategyParam<int> _ivPeriod;
		private readonly StrategyParam<decimal> _ivMultiplier;
		private readonly StrategyParam<decimal> _stopLossAtr;
		private readonly StrategyParam<DataType> _candleType;
		
		private readonly List<decimal> _impliedVolatilityHistory = [];
		private decimal _ivAverage;
		private decimal _ivStdDev;
		private decimal _currentIv;
		
		private decimal _prevHmaValue;
		private decimal _currentAtr;
		
		// Track trade direction
		private bool _isLong;
		private bool _isShort;
		
		/// <summary>
		/// Hull Moving Average period.
		/// </summary>
		public int HmaPeriod
		{
			get => _hmaPeriod.Value;
			set => _hmaPeriod.Value = value;
		}
		
		/// <summary>
		/// Implied Volatility averaging period.
		/// </summary>
		public int IVPeriod
		{
			get => _ivPeriod.Value;
			set => _ivPeriod.Value = value;
		}
		
		/// <summary>
		/// IV standard deviation multiplier for breakout threshold.
		/// </summary>
		public decimal IVMultiplier
		{
			get => _ivMultiplier.Value;
			set => _ivMultiplier.Value = value;
		}
		
		/// <summary>
		/// Stop loss in ATR multiples.
		/// </summary>
		public decimal StopLossAtr
		{
			get => _stopLossAtr.Value;
			set => _stopLossAtr.Value = value;
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
		public HullMAWithImpliedVolatilityBreakoutStrategy()
		{
			_hmaPeriod = Param(nameof(HmaPeriod), 9)
				.SetGreaterThanZero()
				.SetDisplay("HMA Period", "Hull Moving Average period", "HMA Settings")
				.SetCanOptimize(true)
				.SetOptimize(5, 15, 2);
				
			_ivPeriod = Param(nameof(IVPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("IV Period", "Implied Volatility averaging period", "Volatility Settings")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);
				
			_ivMultiplier = Param(nameof(IVMultiplier), 2m)
				.SetGreaterThanZero()
				.SetDisplay("IV StdDev Multiplier", "Multiplier for IV standard deviation", "Volatility Settings")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 3m, 0.5m);
				
			_stopLossAtr = Param(nameof(StopLossAtr), 2m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss (ATR)", "Stop Loss in multiples of ATR", "Risk Management")
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
			_prevHmaValue = 0;
			
			// Create indicators
			var hma = new HullMovingAverage { Length = HmaPeriod };
			var atr = new AverageTrueRange { Length = 14 }; // Fixed ATR period for stop-loss
			
			// Subscribe to candles and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			// We need to bind both HMA and ATR
			subscription
				.Bind(hma, atr, ProcessCandle)
				.Start();
			
			// Create chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, hma);
				DrawIndicator(area, atr);
				DrawOwnTrades(area);
			}
		}
		
		/// <summary>
		/// Process each candle with HMA and ATR values.
		/// </summary>
		private void ProcessCandle(ICandleMessage candle, decimal? hmaValue, decimal? atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;
			
			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Store ATR value for stop-loss calculation
			_currentAtr = atrValue;
			
			// Update implied volatility (in a real system, this would come from market data)
			UpdateImpliedVolatility(candle);
			
			// First run, just store the HMA value
			if (_prevHmaValue == 0)
			{
				_prevHmaValue = hmaValue;
				return;
			}
			
			var price = candle.ClosePrice;
			
			// Determine HMA direction
			var hmaRising = hmaValue > _prevHmaValue;
			var hmaFalling = hmaValue < _prevHmaValue;
			
			// Calculate IV breakout threshold
			var ivBreakoutThreshold = _ivAverage + IVMultiplier * _ivStdDev;
			var ivBreakout = _currentIv > ivBreakoutThreshold;
			
			// Trading logic
			
			// Entry conditions
			
			// Long entry: HMA rising and IV breakout
			if (hmaRising && ivBreakout && !_isLong && Position <= 0)
			{
				LogInfo($"Long signal: HMA rising ({hmaValue} > {_prevHmaValue}), IV breakout ({_currentIv} > {ivBreakoutThreshold})");
				BuyMarket(Volume);
				_isLong = true;
				_isShort = false;
			}
			// Short entry: HMA falling and IV breakout
			else if (hmaFalling && ivBreakout && !_isShort && Position >= 0)
			{
				LogInfo($"Short signal: HMA falling ({hmaValue} < {_prevHmaValue}), IV breakout ({_currentIv} > {ivBreakoutThreshold})");
				SellMarket(Volume);
				_isShort = true;
				_isLong = false;
			}
			
			// Exit conditions
			
			// Exit long: HMA starts falling
			if (_isLong && hmaFalling && Position > 0)
			{
				LogInfo($"Exit long: HMA falling ({hmaValue} < {_prevHmaValue})");
				SellMarket(Math.Abs(Position));
				_isLong = false;
			}
			// Exit short: HMA starts rising
			else if (_isShort && hmaRising && Position < 0)
			{
				LogInfo($"Exit short: HMA rising ({hmaValue} > {_prevHmaValue})");
				BuyMarket(Math.Abs(Position));
				_isShort = false;
			}
			
			// Apply ATR-based stop loss
			ApplyAtrStopLoss(price);
			
			// Store HMA value for next iteration
			_prevHmaValue = hmaValue;
		}
		
		/// <summary>
		/// Update implied volatility value.
		/// In a real implementation, this would fetch data from market.
		/// </summary>
		private void UpdateImpliedVolatility(ICandleMessage candle)
		{
			// Simple IV simulation based on candle's high-low range and volume
			// In reality, this would come from option pricing data
			var range = (candle.HighPrice - candle.LowPrice) / candle.LowPrice;
			var volume = candle.TotalVolume > 0 ? candle.TotalVolume : 1;
			
			// Simulate IV based on range and volume with some randomness
			decimal iv = (decimal)(range * (1 + 0.5m * (decimal)RandomGen.GetDouble()) * 100);
			
			// Add volume factor - higher volume often correlates with higher IV
			iv *= (decimal)Math.Min(1.5, 1 + Math.Log10((double)volume) * 0.1);
			
			_currentIv = iv;
			
			// Add to history
			_impliedVolatilityHistory.Add(_currentIv);
			if (_impliedVolatilityHistory.Count > IVPeriod)
			{
				_impliedVolatilityHistory.RemoveAt(0);
			}
			
			// Calculate average
			decimal sum = 0;
			foreach (var value in _impliedVolatilityHistory)
			{
				sum += value;
			}
			
			_ivAverage = _impliedVolatilityHistory.Count > 0 
				? sum / _impliedVolatilityHistory.Count 
				: 0;
				
			// Calculate standard deviation
			if (_impliedVolatilityHistory.Count > 1)
			{
				decimal sumSquaredDiffs = 0;
				foreach (var value in _impliedVolatilityHistory)
				{
					var diff = value - _ivAverage;
					sumSquaredDiffs += diff * diff;
				}
				
				_ivStdDev = (decimal)Math.Sqrt((double)(sumSquaredDiffs / (_impliedVolatilityHistory.Count - 1)));
			}
			else
			{
				_ivStdDev = 0.5m; // Default value until we have enough data
			}
			
			LogInfo($"IV: {_currentIv}, Avg: {_ivAverage}, StdDev: {_ivStdDev}");
		}
		
		/// <summary>
		/// Apply ATR-based stop loss.
		/// </summary>
		private void ApplyAtrStopLoss(decimal price)
		{
			// Only apply stop-loss if ATR is available and position exists
			if (_currentAtr <= 0 || Position == 0)
				return;
				
			// Calculate stop levels
			if (Position > 0) // Long position
			{
				var stopLevel = price - (StopLossAtr * _currentAtr);
				if (price <= stopLevel)
				{
					LogInfo($"ATR Stop Loss triggered for long position: Current {price} <= Stop {stopLevel}");
					SellMarket(Math.Abs(Position));
					_isLong = false;
				}
			}
			else if (Position < 0) // Short position
			{
				var stopLevel = price + (StopLossAtr * _currentAtr);
				if (price >= stopLevel)
				{
					LogInfo($"ATR Stop Loss triggered for short position: Current {price} >= Stop {stopLevel}");
					BuyMarket(Math.Abs(Position));
					_isShort = false;
				}
			}
		}
	}
}