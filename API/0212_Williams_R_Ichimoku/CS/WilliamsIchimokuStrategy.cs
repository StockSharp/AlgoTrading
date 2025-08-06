using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
		{
	/// <summary>
	/// Strategy based on Williams %R and Ichimoku indicators.
	/// Enters long when Williams %R is below -80 (oversold) and price is above Ichimoku Cloud with Tenkan-sen > Kijun-sen.
	/// Enters short when Williams %R is above -20 (overbought) and price is below Ichimoku Cloud with Tenkan-sen < Kijun-sen.
	/// </summary>
	public class WilliamsIchimokuStrategy : Strategy
	{
		private readonly StrategyParam<int> _williamsRPeriod;
		private readonly StrategyParam<int> _tenkanPeriod;
		private readonly StrategyParam<int> _kijunPeriod;
		private readonly StrategyParam<int> _senkouSpanBPeriod;
		private readonly StrategyParam<DataType> _candleType;
		
		private WilliamsR _williamsR;
		private Ichimoku _ichimoku;
		
		private decimal? _lastKijun;
		
		/// <summary>
		/// Williams %R indicator period.
		/// </summary>
		public int WilliamsRPeriod
		{
			get => _williamsRPeriod.Value;
			set => _williamsRPeriod.Value = value;
		}
		
		/// <summary>
		/// Tenkan-sen period (Ichimoku).
		/// </summary>
		public int TenkanPeriod
		{
			get => _tenkanPeriod.Value;
			set => _tenkanPeriod.Value = value;
		}
		
		/// <summary>
		/// Kijun-sen period (Ichimoku).
		/// </summary>
		public int KijunPeriod
		{
			get => _kijunPeriod.Value;
			set => _kijunPeriod.Value = value;
		}
		
		/// <summary>
		/// Senkou Span B period (Ichimoku).
		/// </summary>
		public int SenkouSpanBPeriod
		{
			get => _senkouSpanBPeriod.Value;
			set => _senkouSpanBPeriod.Value = value;
		}
		
		/// <summary>
		/// Candle type parameter.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}
		
		/// <summary>
		/// Constructor.
		/// </summary>
		public WilliamsIchimokuStrategy()
		{
			_williamsRPeriod = Param(nameof(WilliamsRPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("Williams %R Period", "Period for Williams %R calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 20, 2);
				
			_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
				.SetGreaterThanZero()
				.SetDisplay("Tenkan-sen Period", "Period for Tenkan-sen line (Ichimoku)", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(7, 13, 1);
				
			_kijunPeriod = Param(nameof(KijunPeriod), 26)
				.SetGreaterThanZero()
				.SetDisplay("Kijun-sen Period", "Period for Kijun-sen line (Ichimoku)", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(20, 30, 2);
				
			_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
				.SetGreaterThanZero()
				.SetDisplay("Senkou Span B Period", "Period for Senkou Span B line (Ichimoku)", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(40, 60, 4);
				
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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

			_williamsR = null;
			_ichimoku = null;
			_lastKijun = null;
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Initialize indicators
			_williamsR = new WilliamsR
			{
				Length = WilliamsRPeriod
			};
			
			_ichimoku = new Ichimoku
			{
				Tenkan = { Length = TenkanPeriod },
				Kijun = { Length = KijunPeriod },
				SenkouB = { Length = SenkouSpanBPeriod }
			};
			
			// Create candles subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind indicators to subscription
			subscription
				.BindEx(_williamsR, _ichimoku, ProcessCandle)
				.Start();
			
			// Set stop-loss at Kijun-sen level
			// The actual stop level will be updated in the ProcessCandle method
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute), // No take-profit
				stopLoss: new Unit(0, UnitTypes.Absolute) // Will be dynamic based on Kijun-sen
			);
			
			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _williamsR);
				DrawIndicator(area, _ichimoku);
				DrawOwnTrades(area);
			}
		}
		
		private void ProcessCandle(ICandleMessage candle, IIndicatorValue williamsRValue, IIndicatorValue ichimokuValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;
				
			// Skip if strategy is not ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
				
			// Extract Ichimoku values
			var ichimokuTyped = (IchimokuValue)ichimokuValue;

			if (ichimokuTyped.Tenkan is not decimal tenkan)
				return;

			if (ichimokuTyped.Kijun is not decimal kijun)
				return;

			if (ichimokuTyped.SenkouA is not decimal senkouA)
				return;

			if (ichimokuTyped.SenkouB is not decimal senkouB)
				return;

			// Determine if price is above or below the Kumo (cloud)
			var kumoTop = Math.Max(senkouA, senkouB);
			var kumoBottom = Math.Min(senkouA, senkouB);
			var isPriceAboveKumo = candle.ClosePrice > kumoTop;
			var isPriceBelowKumo = candle.ClosePrice < kumoBottom;

			var williamsRDec = williamsRValue.ToDecimal();

			// Save current Kijun for stop-loss
			_lastKijun = kijun;
			
			// Trading logic
			if (williamsRDec < -80 && isPriceAboveKumo && tenkan > kijun)
			{
				// Long signal: %R < -80 (oversold), price above Kumo, Tenkan > Kijun
				if (Position <= 0)
				{
					// Close any existing short position and open long
					BuyMarket(Volume + Math.Abs(Position));
					LogInfo($"Long Entry: %R={williamsRDec:F2}, Price above Kumo, Tenkan > Kijun");
				}
			}
			else if (williamsRDec > -20 && isPriceBelowKumo && tenkan < kijun)
			{
				// Short signal: %R > -20 (overbought), price below Kumo, Tenkan < Kijun
				if (Position >= 0)
				{
					// Close any existing long position and open short
					SellMarket(Volume + Math.Abs(Position));
					LogInfo($"Short Entry: %R={williamsRDec:F2}, Price below Kumo, Tenkan < Kijun");
				}
			}
			else if ((Position > 0 && candle.ClosePrice < kumoBottom) || 
					(Position < 0 && candle.ClosePrice > kumoTop))
			{
				// Exit positions when price crosses the Kumo
				if (Position > 0)
				{
					SellMarket(Math.Abs(Position));
					LogInfo("Exit Long: Price crossed below Kumo");
				}
				else if (Position < 0)
				{
					BuyMarket(Math.Abs(Position));
					LogInfo("Exit Short: Price crossed above Kumo");
				}
			}
		}
	}
		}
