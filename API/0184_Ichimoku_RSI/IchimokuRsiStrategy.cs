using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Ichimoku + RSI strategy.
	/// Uses Ichimoku Cloud (Kumo) for trend determination and RSI for confirmation of oversold/overbought conditions.
	/// </summary>
	public class IchimokuRsiStrategy : Strategy
	{
		private readonly StrategyParam<int> _tenkanPeriod;
		private readonly StrategyParam<int> _kijunPeriod;
		private readonly StrategyParam<int> _senkouSpanPeriod;
		private readonly StrategyParam<int> _rsiPeriod;
		private readonly StrategyParam<decimal> _rsiOversold;
		private readonly StrategyParam<decimal> _rsiOverbought;
		private readonly StrategyParam<DataType> _candleType;

		// Indicators
		private Ichimoku _ichimoku;
		private RelativeStrengthIndex _rsi;

		/// <summary>
		/// Tenkan-Sen period (conversion line).
		/// </summary>
		public int TenkanPeriod
		{
			get => _tenkanPeriod.Value;
			set => _tenkanPeriod.Value = value;
		}

		/// <summary>
		/// Kijun-Sen period (base line).
		/// </summary>
		public int KijunPeriod
		{
			get => _kijunPeriod.Value;
			set => _kijunPeriod.Value = value;
		}

		/// <summary>
		/// Senkou Span B period (cloud span).
		/// </summary>
		public int SenkouSpanPeriod
		{
			get => _senkouSpanPeriod.Value;
			set => _senkouSpanPeriod.Value = value;
		}

		/// <summary>
		/// RSI period.
		/// </summary>
		public int RsiPeriod
		{
			get => _rsiPeriod.Value;
			set => _rsiPeriod.Value = value;
		}

		/// <summary>
		/// RSI oversold level.
		/// </summary>
		public decimal RsiOversold
		{
			get => _rsiOversold.Value;
			set => _rsiOversold.Value = value;
		}

		/// <summary>
		/// RSI overbought level.
		/// </summary>
		public decimal RsiOverbought
		{
			get => _rsiOverbought.Value;
			set => _rsiOverbought.Value = value;
		}

		/// <summary>
		/// Candle type.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public IchimokuRsiStrategy()
		{
			_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
				.SetGreaterThanZero()
				.SetDisplay("Tenkan Period", "Tenkan-Sen (Conversion Line) period", "Ichimoku")
				.SetCanOptimize(true)
				.SetOptimize(5, 20, 1);
				
			_kijunPeriod = Param(nameof(KijunPeriod), 26)
				.SetGreaterThanZero()
				.SetDisplay("Kijun Period", "Kijun-Sen (Base Line) period", "Ichimoku")
				.SetCanOptimize(true)
				.SetOptimize(20, 50, 2);

			_senkouSpanPeriod = Param(nameof(SenkouSpanPeriod), 52)
				.SetGreaterThanZero()
				.SetDisplay("Senkou Span Period", "Senkou Span B (Cloud Span) period", "Ichimoku")
				.SetCanOptimize(true)
				.SetOptimize(40, 80, 4);

			_rsiPeriod = Param(nameof(RsiPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("RSI Period", "RSI period", "RSI")
				.SetCanOptimize(true)
				.SetOptimize(8, 20, 2);

			_rsiOversold = Param(nameof(RsiOversold), 30m)
				.SetRange(10, 40)
				.SetDisplay("RSI Oversold", "RSI oversold level", "RSI")
				.SetCanOptimize(true)
				.SetOptimize(20, 40, 5);

			_rsiOverbought = Param(nameof(RsiOverbought), 70m)
				.SetRange(60, 90)
				.SetDisplay("RSI Overbought", "RSI overbought level", "RSI")
				.SetCanOptimize(true)
				.SetOptimize(60, 80, 5);

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

			// Create indicators
			_ichimoku = new Ichimoku
			{
				TenkanPeriod = TenkanPeriod,
				KijunPeriod = KijunPeriod,
				SenkouPeriod = SenkouSpanPeriod
			};

			_rsi = new RelativeStrengthIndex
			{
				Length = RsiPeriod
			};

			// Enable position protection with Kijun-Sen as stop loss
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute), // No take-profit, exit based on strategy logic
				stopLoss: new Unit(0, UnitTypes.Absolute)	// Stop-loss will be set dynamically
			);

			// Subscribe to candles and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.BindEx(_ichimoku, _rsi, ProcessCandle)
				.Start();

			// Setup chart
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _ichimoku);
				
				var secondArea = CreateChartArea();
				if (secondArea != null)
				{
					DrawIndicator(secondArea, _rsi);
				}
				
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(
			ICandleMessage candle, 
			IIndicatorValue ichimokuValue, 
			IIndicatorValue rsiValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Get indicator values
			var ichimokuData = (IchimokuIndicatorValue)ichimokuValue;
			decimal tenkan = ichimokuData.Tenkan;
			decimal kijun = ichimokuData.Kijun;
			decimal senkouA = ichimokuData.SenkouA;
			decimal senkouB = ichimokuData.SenkouB;
			decimal chikou = ichimokuData.Chikou;
			
			decimal rsi = rsiValue.GetValue<decimal>();

			// Determine if price is above/below Kumo (cloud)
			decimal kumoTop = Math.Max(senkouA, senkouB);
			decimal kumoBottom = Math.Min(senkouA, senkouB);
			bool priceAboveKumo = candle.ClosePrice > kumoTop;
			bool priceBelowKumo = candle.ClosePrice < kumoBottom;
			
			// Determine Tenkan/Kijun cross
			bool tenkanAboveKijun = tenkan > kijun;
			bool tenkanBelowKijun = tenkan < kijun;

			// Trading logic:
			// Buy when price above Kumo, Tenkan above Kijun, and RSI shows oversold condition
			if (priceAboveKumo && tenkanAboveKijun && rsi < RsiOversold && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				LogInfo($"Long entry: Price={candle.ClosePrice}, Above Kumo, Tenkan={tenkan}, Kijun={kijun}, RSI={rsi}");
			}
			// Sell when price below Kumo, Tenkan below Kijun, and RSI shows overbought condition
			else if (priceBelowKumo && tenkanBelowKijun && rsi > RsiOverbought && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				LogInfo($"Short entry: Price={candle.ClosePrice}, Below Kumo, Tenkan={tenkan}, Kijun={kijun}, RSI={rsi}");
			}
			// Exit long position when price falls below Kumo
			else if (Position > 0 && priceBelowKumo)
			{
				SellMarket(Math.Abs(Position));
				LogInfo($"Long exit: Price={candle.ClosePrice}, Below Kumo");
			}
			// Exit short position when price rises above Kumo
			else if (Position < 0 && priceAboveKumo)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short exit: Price={candle.ClosePrice}, Above Kumo");
			}

			// Set Kijun-Sen as dynamic stop-loss
			if (Position > 0)
			{
				var stopPrice = kijun;
				var stopDistance = candle.ClosePrice - stopPrice;
				var stopPercentage = stopDistance / candle.ClosePrice * 100;
				
				// Only set stop loss if it's reasonable (not too tight)
				if (stopPercentage > 0.5m)
				{
					StartProtection(
						takeProfit: new Unit(0, UnitTypes.Absolute),
						stopLoss: new Unit(stopPercentage, UnitTypes.Percent)
					);
				}
			}
			else if (Position < 0)
			{
				var stopPrice = kijun;
				var stopDistance = stopPrice - candle.ClosePrice;
				var stopPercentage = stopDistance / candle.ClosePrice * 100;
				
				// Only set stop loss if it's reasonable (not too tight)
				if (stopPercentage > 0.5m)
				{
					StartProtection(
						takeProfit: new Unit(0, UnitTypes.Absolute),
						stopLoss: new Unit(stopPercentage, UnitTypes.Percent)
					);
				}
			}
		}
	}
}