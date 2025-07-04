using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Ichimoku Cloud and Stochastic Oscillator indicators.
	/// Enters long when price is above Kumo (cloud), Tenkan > Kijun, and Stochastic is oversold (< 20)
	/// Enters short when price is below Kumo, Tenkan < Kijun, and Stochastic is overbought (> 80)
	/// </summary>
	public class IchimokuStochasticStrategy : Strategy
	{
		private readonly StrategyParam<int> _tenkanPeriod;
		private readonly StrategyParam<int> _kijunPeriod;
		private readonly StrategyParam<int> _senkouPeriod;
		private readonly StrategyParam<int> _stochPeriod;
		private readonly StrategyParam<int> _stochK;
		private readonly StrategyParam<int> _stochD;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// Tenkan-sen period
		/// </summary>
		public int TenkanPeriod
		{
			get => _tenkanPeriod.Value;
			set => _tenkanPeriod.Value = value;
		}

		/// <summary>
		/// Kijun-sen period
		/// </summary>
		public int KijunPeriod
		{
			get => _kijunPeriod.Value;
			set => _kijunPeriod.Value = value;
		}

		/// <summary>
		/// Senkou Span period
		/// </summary>
		public int SenkouPeriod
		{
			get => _senkouPeriod.Value;
			set => _senkouPeriod.Value = value;
		}

		/// <summary>
		/// Stochastic %K period
		/// </summary>
		public int StochPeriod
		{
			get => _stochPeriod.Value;
			set => _stochPeriod.Value = value;
		}
		
		/// <summary>
		/// Stochastic %K smoothing period
		/// </summary>
		public int StochK
		{
			get => _stochK.Value;
			set => _stochK.Value = value;
		}
		
		/// <summary>
		/// Stochastic %D period
		/// </summary>
		public int StochD
		{
			get => _stochD.Value;
			set => _stochD.Value = value;
		}

		/// <summary>
		/// Candle type for strategy calculation
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public IchimokuStochasticStrategy()
		{
			_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
				.SetGreaterThanZero()
				.SetDisplay("Tenkan-sen Period", "Period for Tenkan-sen line", "Ichimoku")
				.SetCanOptimize(true)
				.SetOptimize(7, 12, 1);

			_kijunPeriod = Param(nameof(KijunPeriod), 26)
				.SetGreaterThanZero()
				.SetDisplay("Kijun-sen Period", "Period for Kijun-sen line", "Ichimoku")
				.SetCanOptimize(true)
				.SetOptimize(20, 30, 2);

			_senkouPeriod = Param(nameof(SenkouPeriod), 52)
				.SetGreaterThanZero()
				.SetDisplay("Senkou Span Period", "Period for Senkou Span B line", "Ichimoku")
				.SetCanOptimize(true)
				.SetOptimize(40, 60, 5);

			_stochPeriod = Param(nameof(StochPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("Stochastic Period", "Period for Stochastic Oscillator", "Stochastic")
				.SetCanOptimize(true)
				.SetOptimize(10, 20, 2);
				
			_stochK = Param(nameof(StochK), 3)
				.SetGreaterThanZero()
				.SetDisplay("Stochastic %K", "Smoothing for Stochastic %K line", "Stochastic")
				.SetCanOptimize(true)
				.SetOptimize(1, 5, 1);
				
			_stochD = Param(nameof(StochD), 3)
				.SetGreaterThanZero()
				.SetDisplay("Stochastic %D", "Period for Stochastic %D line", "Stochastic")
				.SetCanOptimize(true)
				.SetOptimize(1, 5, 1);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
				.SetDisplay("Candle Type", "Timeframe for strategy", "General");
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
				SenkouB = { Length = SenkouPeriod }
			};

			var stochastic = new StochasticOscillator
			{
				K = { Length = StochK },
				D = { Length = StochD },
			};

			// Subscribe to candles and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.BindEx(ichimoku, stochastic, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, ichimoku);
				
				// Create a separate area for Stochastic
				var stochArea = CreateChartArea();
				if (stochArea != null)
				{
					DrawIndicator(stochArea, stochastic);
				}
				
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue, IIndicatorValue stochasticValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;
			
			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Get additional values from Ichimoku
			var ichimokuIndicator = (Ichimoku)Indicators.FindById(nameof(Ichimoku));
			if (ichimokuIndicator == null)
				return;

			// Current price (close of the candle)
			var price = candle.ClosePrice;
			
			// Get individual Ichimoku components
			var tenkan = ichimokuIndicator.Tenkan.GetCurrentValue();
			var kijun = ichimokuIndicator.Kijun.GetCurrentValue();
			var senkouA = ichimokuIndicator.SenkouA.GetCurrentValue();
			var senkouB = ichimokuIndicator.SenkouB.GetCurrentValue();
			
			// Check if price is above/below Kumo cloud
			var isAboveKumo = price > Math.Max(senkouA, senkouB);
			var isBelowKumo = price < Math.Min(senkouA, senkouB);
			
			// Check Tenkan/Kijun cross (trend direction)
			var isBullishCross = tenkan > kijun;
			var isBearishCross = tenkan < kijun;
			
			// Get Stochastic %K value
			var stochasticK = stochasticValue;

			// Trading logic
			if (isAboveKumo && isBullishCross && stochasticK < 20 && Position <= 0)
			{
				// Buy signal: price above cloud, bullish cross, and oversold stochastic
				BuyMarket(Volume + Math.Abs(Position));
				
				// Use Kijun-sen as stop-loss
				RegisterOrder(CreateOrder(Sides.Sell, kijun, Math.Abs(Position + Volume)));
			}
			else if (isBelowKumo && isBearishCross && stochasticK > 80 && Position >= 0)
			{
				// Sell signal: price below cloud, bearish cross, and overbought stochastic
				SellMarket(Volume + Math.Abs(Position));
				
				// Use Kijun-sen as stop-loss
				RegisterOrder(CreateOrder(Sides.Buy, kijun, Math.Abs(Position + Volume)));
			}
			// Exit conditions
			else if (price < kijun && Position > 0)
			{
				// Exit long position when price falls below Kijun-sen
				SellMarket(Position);
			}
			else if (price > kijun && Position < 0)
			{
				// Exit short position when price rises above Kijun-sen
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}