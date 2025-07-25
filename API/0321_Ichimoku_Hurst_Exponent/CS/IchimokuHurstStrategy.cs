using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Collections;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Ichimoku Kinko Hyo indicator with Hurst exponent filter.
	/// </summary>
	public class IchimokuHurstStrategy : Strategy
	{
		private readonly StrategyParam<int> _tenkanPeriod;
		private readonly StrategyParam<int> _kijunPeriod;
		private readonly StrategyParam<int> _senkouSpanBPeriod;
		private readonly StrategyParam<int> _hurstPeriod;
		private readonly StrategyParam<decimal> _hurstThreshold;
		private readonly StrategyParam<DataType> _candleType;
		
		private Ichimoku _ichimoku;
		
		// Data for Hurst exponent calculations
		private readonly SynchronizedList<decimal> _prices = [];
		private decimal _hurstExponent;

		/// <summary>
		/// Tenkan-sen (conversion line) period.
		/// </summary>
		public int TenkanPeriod
		{
			get => _tenkanPeriod.Value;
			set => _tenkanPeriod.Value = value;
		}

		/// <summary>
		/// Kijun-sen (base line) period.
		/// </summary>
		public int KijunPeriod
		{
			get => _kijunPeriod.Value;
			set => _kijunPeriod.Value = value;
		}

		/// <summary>
		/// Senkou Span B (leading span B) period.
		/// </summary>
		public int SenkouSpanBPeriod
		{
			get => _senkouSpanBPeriod.Value;
			set => _senkouSpanBPeriod.Value = value;
		}

		/// <summary>
		/// Hurst exponent calculation period.
		/// </summary>
		public int HurstPeriod
		{
			get => _hurstPeriod.Value;
			set => _hurstPeriod.Value = value;
		}

		/// <summary>
		/// Hurst exponent threshold for trend strength.
		/// </summary>
		public decimal HurstThreshold
		{
			get => _hurstThreshold.Value;
			set => _hurstThreshold.Value = value;
		}

		/// <summary>
		/// Candle type to use for the strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="IchimokuHurstStrategy"/>.
		/// </summary>
		public IchimokuHurstStrategy()
		{
			_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
				.SetDisplay("Tenkan Period", "Tenkan-sen (conversion line) period", "Ichimoku")
				.SetCanOptimize(true)
				.SetOptimize(5, 15, 1);

			_kijunPeriod = Param(nameof(KijunPeriod), 26)
				.SetDisplay("Kijun Period", "Kijun-sen (base line) period", "Ichimoku")
				.SetCanOptimize(true)
				.SetOptimize(20, 40, 2);

			_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
				.SetDisplay("Senkou Span B Period", "Senkou Span B (leading span B) period", "Ichimoku")
				.SetCanOptimize(true)
				.SetOptimize(40, 70, 5);
				
			_hurstPeriod = Param(nameof(HurstPeriod), 100)
				.SetDisplay("Hurst Period", "Hurst exponent calculation period", "Hurst Exponent")
				.SetCanOptimize(true)
				.SetOptimize(50, 200, 10);
				
			_hurstThreshold = Param(nameof(HurstThreshold), 0.5m)
				.SetDisplay("Hurst Threshold", "Hurst exponent threshold for trend strength", "Hurst Exponent")
				.SetCanOptimize(true)
				.SetOptimize(0.45m, 0.6m, 0.05m);

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

			_prices.Clear();
			_hurstExponent = 0.5m; // Default Hurst exponent value

			// Create Ichimoku indicator
			_ichimoku = new Ichimoku
			{
				Tenkan = { Length = TenkanPeriod },
				Kijun = { Length = KijunPeriod },
				SenkouB = { Length = SenkouSpanBPeriod }
			};

			// Create subscription and bind indicator
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.BindEx(_ichimoku, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _ichimoku);
				DrawOwnTrades(area);
			}
		}
		
		private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Store Ichimoku values
			var ichimokuTyped = (IchimokuValue)ichimokuValue;

			if (ichimokuTyped.Tenkan is not decimal tenkan)
				return;

			if (ichimokuTyped.Kijun is not decimal kijun)
				return;

			if (ichimokuTyped.SenkouA is not decimal senkouA)
				return;

			if (ichimokuTyped.SenkouB is not decimal senkouB)
				return;

			// Update price data for Hurst exponent calculation
			_prices.Add(candle.ClosePrice);
			
			// Keep only the number of prices needed for Hurst calculation
			while (_prices.Count > HurstPeriod)
				_prices.RemoveAt(0);
			
			// Calculate Hurst exponent when we have enough data
			if (_prices.Count >= HurstPeriod)
				CalculateHurstExponent();
			
			// Continue with position checks
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Check if price is above/below Kumo (cloud)
			bool isPriceAboveKumo = candle.ClosePrice > Math.Max(senkouA, senkouB);
			bool isPriceBelowKumo = candle.ClosePrice < Math.Min(senkouA, senkouB);
			
			// Trading logic
			// Buy when price is above the cloud, Tenkan > Kijun, and Hurst > threshold (trending market)
			if (isPriceAboveKumo && tenkan > kijun && _hurstExponent > HurstThreshold && Position <= 0)
			{
				BuyMarket(Volume);
				LogInfo($"Buy Signal: Price {candle.ClosePrice:F2} above Kumo, Tenkan {tenkan:F2} > Kijun {kijun:F2}, Hurst {_hurstExponent:F3}");
			}
			// Sell when price is below the cloud, Tenkan < Kijun, and Hurst > threshold (trending market)
			else if (isPriceBelowKumo && tenkan < kijun && _hurstExponent > HurstThreshold && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				LogInfo($"Sell Signal: Price {candle.ClosePrice:F2} below Kumo, Tenkan {tenkan:F2} < Kijun {kijun:F2}, Hurst {_hurstExponent:F3}");
			}
			// Exit long position when price falls below the cloud
			else if (Position > 0 && isPriceBelowKumo)
			{
				SellMarket(Position);
				LogInfo($"Exit Long: Price {candle.ClosePrice:F2} fell below Kumo");
			}
			// Exit short position when price rises above the cloud
			else if (Position < 0 && isPriceAboveKumo)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exit Short: Price {candle.ClosePrice:F2} rose above Kumo");
			}
		}
		
		private void CalculateHurstExponent()
		{
			// This is a simplified Hurst exponent calculation using R/S analysis
			// Note: A full implementation would use multiple time scales
			
			// Calculate log returns
			List<decimal> logReturns = [];
			for (int i = 1; i < _prices.Count; i++)
			{
				if (_prices[i-1] != 0)
					logReturns.Add((decimal)Math.Log((double)(_prices[i] / _prices[i-1])));
			}
			
			if (logReturns.Count < 10)
				return;
			
			// Calculate mean
			decimal mean = logReturns.Sum() / logReturns.Count;
			
			// Calculate cumulative deviation series
			List<decimal> cumulativeDeviation = [];
			decimal sum = 0;
			
			foreach (var logReturn in logReturns)
			{
				sum += (logReturn - mean);
				cumulativeDeviation.Add(sum);
			}
			
			// Calculate range (max - min of cumulative deviation)
			decimal range = cumulativeDeviation.Max() - cumulativeDeviation.Min();
			
			// Calculate standard deviation
			decimal sumSquares = logReturns.Sum(x => (x - mean) * (x - mean));
			decimal stdDev = (decimal)Math.Sqrt((double)(sumSquares / logReturns.Count));
			
			if (stdDev == 0)
				return;
			
			// Calculate R/S statistic
			decimal rs = range / stdDev;
			
			// Hurst = log(R/S) / log(N)
			decimal logN = (decimal)Math.Log((double)logReturns.Count);
			if (logN != 0)
				_hurstExponent = (decimal)Math.Log((double)rs) / logN;
				
			LogInfo($"Calculated Hurst Exponent: {_hurstExponent:F3} (R/S: {rs:F3}, N: {logReturns.Count})");
		}
	}
}