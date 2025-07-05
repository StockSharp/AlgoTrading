using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Ichimoku Cloud and ADX indicators.
	/// 
	/// Entry criteria:
	/// Long: Price > Kumo (cloud) && Tenkan > Kijun && ADX > 25 (uptrend with strong movement)
	/// Short: Price < Kumo (cloud) && Tenkan < Kijun && ADX > 25 (downtrend with strong movement)
	/// 
	/// Exit criteria:
	/// Long: Price < Kumo (price falls below cloud)
	/// Short: Price > Kumo (price rises above cloud)
	/// </summary>
	public class IchimokuAdxStrategy : Strategy
	{
		private readonly StrategyParam<int> _tenkanPeriod;
		private readonly StrategyParam<int> _kijunPeriod;
		private readonly StrategyParam<int> _senkouSpanBPeriod;
		private readonly StrategyParam<int> _adxPeriod;
		private readonly StrategyParam<decimal> _adxThreshold;
		private readonly StrategyParam<DataType> _candleType;

		// Previous state tracking
		private bool _isPriceAboveCloud;
		private bool _isTenkanAboveKijun;
		private decimal _lastAdxValue;

		/// <summary>
		/// Period for Tenkan-sen calculation (conversion line).
		/// </summary>
		public int TenkanPeriod
		{
			get => _tenkanPeriod.Value;
			set => _tenkanPeriod.Value = value;
		}

		/// <summary>
		/// Period for Kijun-sen calculation (base line).
		/// </summary>
		public int KijunPeriod
		{
			get => _kijunPeriod.Value;
			set => _kijunPeriod.Value = value;
		}

		/// <summary>
		/// Period for Senkou Span B calculation (second cloud component).
		/// </summary>
		public int SenkouSpanBPeriod
		{
			get => _senkouSpanBPeriod.Value;
			set => _senkouSpanBPeriod.Value = value;
		}

		/// <summary>
		/// Period for ADX calculation.
		/// </summary>
		public int AdxPeriod
		{
			get => _adxPeriod.Value;
			set => _adxPeriod.Value = value;
		}

		/// <summary>
		/// Threshold for ADX to confirm trend strength.
		/// </summary>
		public decimal AdxThreshold
		{
			get => _adxThreshold.Value;
			set => _adxThreshold.Value = value;
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
		/// Constructor.
		/// </summary>
		public IchimokuAdxStrategy()
		{
			_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
				.SetGreaterThanZero()
				.SetDisplay("Tenkan Period", "Period for Tenkan-sen (conversion line)", "Ichimoku")
				.SetCanOptimize(true)
				.SetOptimize(7, 13, 2);

			_kijunPeriod = Param(nameof(KijunPeriod), 26)
				.SetGreaterThanZero()
				.SetDisplay("Kijun Period", "Period for Kijun-sen (base line)", "Ichimoku")
				.SetCanOptimize(true)
				.SetOptimize(20, 32, 3);

			_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
				.SetGreaterThanZero()
				.SetDisplay("Senkou Span B Period", "Period for Senkou Span B (second cloud component)", "Ichimoku")
				.SetCanOptimize(true)
				.SetOptimize(40, 60, 5);

			_adxPeriod = Param(nameof(AdxPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 20, 5);

			_adxThreshold = Param(nameof(AdxThreshold), 25m)
				.SetGreaterThanZero()
				.SetDisplay("ADX Threshold", "Minimum ADX value to confirm trend strength", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(20m, 30m, 5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
				
			// Initialize state tracking variables
			_isPriceAboveCloud = false;
			_isTenkanAboveKijun = false;
			_lastAdxValue = 0;
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
			var ichimoku = new Ichimoku
			{
				Tenkan = { Length = TenkanPeriod },
				Kijun = { Length = KijunPeriod },
				SenkouB = { Length = SenkouSpanBPeriod }
			};
			
			var adx = new AverageDirectionalIndex { Length = AdxPeriod };

			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			// We'll need to manually bind Ichimoku and ADX separately as they have different output values
			subscription
				.BindEx(ichimoku, (candle, ichimokuValue) => {
					// Process candle with Ichimoku
					ProcessIchimokuData(candle, ichimokuValue);
				})
				.Start();
				
			subscription
				.BindEx(adx, (candle, adxValue) => {
					// Process candle with ADX
					ProcessAdxData(candle, adxValue);
				})
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, ichimoku);
				
				// Create separate area for ADX
				var adxArea = CreateChartArea();
				if (adxArea != null)
				{
					DrawIndicator(adxArea, adx);
				}
				
				DrawOwnTrades(area);
			}
		}
		
		// Process Ichimoku indicator data
		private void ProcessIchimokuData(ICandleMessage candle, IIndicatorValue ichimokuValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Get Ichimoku values
			// The component values must be extracted based on the Ichimoku implementation
			var ichimokuTyped = (IchimokuValue)ichimokuValue;

			if (ichimokuTyped.Tenkan is not decimal tenkan)
				return;

			if (ichimokuTyped.Kijun is not decimal kijun)
				return;

			if (ichimokuTyped.SenkouA is not decimal senkouA)
				return;

			if (ichimokuTyped.SenkouB is not decimal senkouB)
				return;

			// Determine cloud boundaries
			var cloudTop = Math.Max(senkouA, senkouB);
			var cloudBottom = Math.Min(senkouA, senkouB);
			
			// Update state
			var isPriceAboveCloud = candle.ClosePrice > cloudTop;
			var isPriceBelowCloud = candle.ClosePrice < cloudBottom;
			var isTenkanAboveKijun = tenkan > kijun;
			
			// Log current state
			LogInfo($"Close: {candle.ClosePrice}, Tenkan: {tenkan:N2}, Kijun: {kijun:N2}, " + 
						  $"Cloud Top: {cloudTop:N2}, Cloud Bottom: {cloudBottom:N2}, ADX: {_lastAdxValue:N2}");
			
			var isPriceRelativeToCloudChanged = _isPriceAboveCloud != isPriceAboveCloud;
			
			// Only make trading decisions if both Ichimoku and ADX have been calculated
			if (_lastAdxValue <= 0)
				return;
				
			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			var isStrongTrend = _lastAdxValue > AdxThreshold;
			
			// Trading logic
			if (Position == 0) // No position
			{
				if (isPriceAboveCloud && isTenkanAboveKijun && isStrongTrend)
				{
					// Buy signal: price above cloud, Tenkan above Kijun, strong trend
					BuyMarket(Volume);
					LogInfo($"Buy signal: Price above cloud, Tenkan above Kijun, ADX = {_lastAdxValue}");
				}
				else if (isPriceBelowCloud && !isTenkanAboveKijun && isStrongTrend)
				{
					// Sell signal: price below cloud, Tenkan below Kijun, strong trend
					SellMarket(Volume);
					LogInfo($"Sell signal: Price below cloud, Tenkan below Kijun, ADX = {_lastAdxValue}");
				}
			}
			else if (isPriceRelativeToCloudChanged) // Exit on cloud crossing
			{
				if (Position > 0 && !isPriceAboveCloud)
				{
					// Exit long position: price fell below cloud
					SellMarket(Math.Abs(Position));
					LogInfo($"Exit long position: Price fell into/below cloud");
				}
				else if (Position < 0 && !isPriceBelowCloud)
				{
					// Exit short position: price rose above cloud
					BuyMarket(Math.Abs(Position));
					LogInfo($"Exit short position: Price rose into/above cloud");
				}
			}
			
			// Update tracking variables
			_isPriceAboveCloud = isPriceAboveCloud;
			_isTenkanAboveKijun = isTenkanAboveKijun;
		}
		
		// Process ADX indicator data
		private void ProcessAdxData(ICandleMessage candle, IIndicatorValue adxValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;
				
			// Get ADX value (this is the main value)
			_lastAdxValue = adxValue.ToDecimal();
		}
	}
}
