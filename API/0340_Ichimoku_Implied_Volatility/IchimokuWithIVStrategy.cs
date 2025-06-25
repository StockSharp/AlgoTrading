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
	/// Ichimoku with Implied Volatility strategy.
	/// Entry condition:
	/// Long: Price > Kumo && Tenkan > Kijun && IV > Avg(IV, N)
	/// Short: Price < Kumo && Tenkan < Kijun && IV > Avg(IV, N)
	/// Exit condition:
	/// Long: Price < Kumo
	/// Short: Price > Kumo
	/// </summary>
	public class IchimokuWithImpliedVolatilityStrategy : Strategy
	{
		private readonly StrategyParam<int> _tenkanPeriod;
		private readonly StrategyParam<int> _kijunPeriod;
		private readonly StrategyParam<int> _senkouSpanBPeriod;
		private readonly StrategyParam<int> _ivPeriod;
		private readonly StrategyParam<DataType> _candleType;
		
		private readonly List<decimal> _impliedVolatilityHistory = [];
		private decimal _avgImpliedVolatility;
		
		// Store previous indicator values for easier tracking
		private decimal _prevPrice;
		private bool _prevAboveKumo;
		private bool _prevTenkanAboveKijun;
		
		/// <summary>
		/// Tenkan-Sen period.
		/// </summary>
		public int TenkanPeriod
		{
			get => _tenkanPeriod.Value;
			set => _tenkanPeriod.Value = value;
		}
		
		/// <summary>
		/// Kijun-Sen period.
		/// </summary>
		public int KijunPeriod
		{
			get => _kijunPeriod.Value;
			set => _kijunPeriod.Value = value;
		}
		
		/// <summary>
		/// Senkou Span B period.
		/// </summary>
		public int SenkouSpanBPeriod
		{
			get => _senkouSpanBPeriod.Value;
			set => _senkouSpanBPeriod.Value = value;
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
		public IchimokuWithImpliedVolatilityStrategy()
		{
			_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
				.SetGreaterThanZero()
				.SetDisplay("Tenkan-Sen Period", "Tenkan-Sen (Conversion Line) period", "Ichimoku Settings")
				.SetCanOptimize(true)
				.SetOptimize(5, 13, 2);
				
			_kijunPeriod = Param(nameof(KijunPeriod), 26)
				.SetGreaterThanZero()
				.SetDisplay("Kijun-Sen Period", "Kijun-Sen (Base Line) period", "Ichimoku Settings")
				.SetCanOptimize(true)
				.SetOptimize(20, 30, 2);
				
			_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
				.SetGreaterThanZero()
				.SetDisplay("Senkou Span B Period", "Senkou Span B (2nd Leading Span) period", "Ichimoku Settings")
				.SetCanOptimize(true)
				.SetOptimize(40, 60, 4);
				
			_ivPeriod = Param(nameof(IVPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("IV Period", "Implied Volatility averaging period", "Volatility Settings")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);
				
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
			
			// Create Ichimoku indicator
			var ichimoku = new Ichimoku
			{
				TenkanPeriod = TenkanPeriod,
				KijunPeriod = KijunPeriod,
				SenkouSpanBPeriod = SenkouSpanBPeriod
			};
			
			// Subscribe to candles and bind indicator
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.Bind(ichimoku, ProcessCandle)
				.Start();
			
			// Create chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, ichimoku);
				DrawOwnTrades(area);
			}
			
			// Enable position protection using Kijun-Sen as stop-loss
			StartProtection(
				new Unit(0), // No take profit
				new Unit(0)  // Dynamic stop-loss will be handled manually
			);
		}
		
		/// <summary>
		/// Process each candle and Ichimoku values.
		/// </summary>
		private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;
			
			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Get Ichimoku values
			var tenkan = ichimokuValue.GetValue<Tuple<decimal, decimal, decimal, decimal, decimal>>().Item1;
			var kijun = ichimokuValue.GetValue<Tuple<decimal, decimal, decimal, decimal, decimal>>().Item2;
			var senkouA = ichimokuValue.GetValue<Tuple<decimal, decimal, decimal, decimal, decimal>>().Item3;
			var senkouB = ichimokuValue.GetValue<Tuple<decimal, decimal, decimal, decimal, decimal>>().Item4;
			
			// Determine if price is above Kumo (cloud)
			var kumoTop = Math.Max(senkouA, senkouB);
			var kumoBottom = Math.Min(senkouA, senkouB);
			var priceAboveKumo = candle.ClosePrice > kumoTop;
			var priceBelowKumo = candle.ClosePrice < kumoBottom;
			
			// Check Tenkan/Kijun cross
			var tenkanAboveKijun = tenkan > kijun;
			
			// Update Implied Volatility (in a real system, this would come from market data)
			UpdateImpliedVolatility(candle);
			
			// Check IV condition
			var ivHigherThanAverage = GetImpliedVolatility() > _avgImpliedVolatility;
			
			// First run, just store values
			if (_prevPrice == 0)
			{
				_prevPrice = candle.ClosePrice;
				_prevAboveKumo = priceAboveKumo;
				_prevTenkanAboveKijun = tenkanAboveKijun;
				return;
			}
			
			// Trading logic based on Ichimoku and IV
			
			// Long entry condition
			if (priceAboveKumo && tenkanAboveKijun && ivHigherThanAverage && Position <= 0)
			{
				LogInfo("Long signal: Price above Kumo, Tenkan above Kijun, IV elevated");
				BuyMarket(Volume);
			}
			// Short entry condition
			else if (priceBelowKumo && !tenkanAboveKijun && ivHigherThanAverage && Position >= 0)
			{
				LogInfo("Short signal: Price below Kumo, Tenkan below Kijun, IV elevated");
				SellMarket(Volume);
			}
			
			// Exit conditions
			
			// Exit long if price falls below Kumo
			if (Position > 0 && !priceAboveKumo)
			{
				LogInfo("Exit long: Price fell below Kumo");
				SellMarket(Math.Abs(Position));
			}
			// Exit short if price rises above Kumo
			else if (Position < 0 && !priceBelowKumo)
			{
				LogInfo("Exit short: Price rose above Kumo");
				BuyMarket(Math.Abs(Position));
			}
			
			// Use Kijun-Sen as trailing stop
			ApplyKijunAsStop(candle.ClosePrice, kijun);
			
			// Update previous values
			_prevPrice = candle.ClosePrice;
			_prevAboveKumo = priceAboveKumo;
			_prevTenkanAboveKijun = tenkanAboveKijun;
		}
		
		/// <summary>
		/// Update implied volatility value.
		/// In a real implementation, this would fetch data from market.
		/// </summary>
		private void UpdateImpliedVolatility(ICandleMessage candle)
		{
			// Simple IV simulation based on candle's high-low range
			// In reality, this would come from option pricing data
			decimal iv = (candle.HighPrice - candle.LowPrice) / candle.OpenPrice * 100;
			
			// Add some random fluctuation to simulate IV behavior
			var random = new Random();
			iv *= (decimal)(0.8 + 0.4 * random.NextDouble());
			
			// Add to history and maintain history length
			_impliedVolatilityHistory.Add(iv);
			if (_impliedVolatilityHistory.Count > IVPeriod)
			{
				_impliedVolatilityHistory.RemoveAt(0);
			}
			
			// Calculate average IV
			decimal sum = 0;
			foreach (var value in _impliedVolatilityHistory)
			{
				sum += value;
			}
			
			_avgImpliedVolatility = _impliedVolatilityHistory.Count > 0 
				? sum / _impliedVolatilityHistory.Count 
				: 0;
				
			LogInfo($"IV: {iv}, Avg IV: {_avgImpliedVolatility}");
		}
		
		/// <summary>
		/// Get current implied volatility.
		/// </summary>
		private decimal GetImpliedVolatility()
		{
			return _impliedVolatilityHistory.Count > 0 
				? _impliedVolatilityHistory[_impliedVolatilityHistory.Count - 1] 
				: 0;
		}
		
		/// <summary>
		/// Use Kijun-Sen as a trailing stop level.
		/// </summary>
		private void ApplyKijunAsStop(decimal price, decimal kijun)
		{
			// Long position: exit if price drops below Kijun
			if (Position > 0 && price < kijun)
			{
				LogInfo("Kijun-Sen stop triggered for long position");
				SellMarket(Math.Abs(Position));
			}
			// Short position: exit if price rises above Kijun
			else if (Position < 0 && price > kijun)
			{
				LogInfo("Kijun-Sen stop triggered for short position");
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}
