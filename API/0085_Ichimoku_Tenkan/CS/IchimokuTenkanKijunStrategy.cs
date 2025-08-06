using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Ichimoku Tenkan/Kijun Cross Strategy.
	/// Enters long when Tenkan crosses above Kijun and price is above Kumo.
	/// Enters short when Tenkan crosses below Kijun and price is below Kumo.
	/// </summary>
	public class IchimokuTenkanKijunStrategy : Strategy
	{
		private readonly StrategyParam<int> _tenkanPeriod;
		private readonly StrategyParam<int> _kijunPeriod;
		private readonly StrategyParam<int> _senkouSpanBPeriod;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLossPercent;

		private decimal _prevTenkan;
		private decimal _prevKijun;
		private Ichimoku _ichimoku;

		/// <summary>
		/// Tenkan-sen period.
		/// </summary>
		public int TenkanPeriod
		{
			get => _tenkanPeriod.Value;
			set => _tenkanPeriod.Value = value;
		}

		/// <summary>
		/// Kijun-sen period.
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
		/// Type of candles to use.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Stop-loss percentage.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="IchimokuTenkanKijunStrategy"/>.
		/// </summary>
		public IchimokuTenkanKijunStrategy()
		{
			_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
				.SetDisplay("Tenkan Period", "Period for Tenkan-sen calculation", "Ichimoku Settings")
				.SetRange(7, 13)
				.SetCanOptimize(true);
				
			_kijunPeriod = Param(nameof(KijunPeriod), 26)
				.SetDisplay("Kijun Period", "Period for Kijun-sen calculation", "Ichimoku Settings")
				.SetRange(20, 30)
				.SetCanOptimize(true);
				
			_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
				.SetDisplay("Senkou Span B Period", "Period for Senkou Span B calculation", "Ichimoku Settings")
				.SetRange(40, 60)
				.SetCanOptimize(true);
				
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_stopLossPercent = Param(nameof(StopLossPercent), 1.0m)
				.SetNotNegative()
				.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(0.5m, 2.0m, 0.5m);
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
			_prevTenkan = 0;
			_prevKijun = 0;
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			_ichimoku = new Ichimoku
			{
				Tenkan = { Length = TenkanPeriod },
				Kijun = { Length = KijunPeriod },
				SenkouB = { Length = SenkouSpanBPeriod }
			};


			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind indicator and candle handler
			subscription
				.BindEx(_ichimoku, ProcessCandle)
				.Start();
				
			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _ichimoku);
				DrawOwnTrades(area);
			}

			StartProtection(
				new(),
				new Unit(StopLossPercent, UnitTypes.Percent),
				useMarketOrders: true
			);
		}

		/// <summary>
		/// Process candle with Ichimoku indicator values.
		/// </summary>
		/// <param name="candle">Candle.</param>
		/// <param name="ichimokuValue">Ichimoku indicator value.</param>
		private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Get current Ichimoku values
			var ichimokuTyped = (IchimokuValue)ichimokuValue;

			if (ichimokuTyped.Tenkan is not decimal tenkan)
				return;

			if (ichimokuTyped.Kijun is not decimal kijun)
				return;

			if (ichimokuTyped.SenkouA is not decimal senkouA)
				return;

			if (ichimokuTyped.SenkouB is not decimal senkouB)
				return;

			// If first calculation, just store values
			if (_prevTenkan == 0 || _prevKijun == 0)
			{
				_prevTenkan = tenkan;
				_prevKijun = kijun;
				return;
			}

			// Check for Tenkan/Kijun cross
			bool bullishCross = _prevTenkan <= _prevKijun && tenkan > kijun;
			bool bearishCross = _prevTenkan >= _prevKijun && tenkan < kijun;
			
			// Determine if price is above or below Kumo (cloud)
			decimal lowerKumo = Math.Min(senkouA, senkouB);
			decimal upperKumo = Math.Max(senkouA, senkouB);
			bool priceAboveKumo = candle.ClosePrice > upperKumo;
			bool priceBelowKumo = candle.ClosePrice < lowerKumo;

			// Long entry: Bullish cross and price above Kumo
			if (bullishCross && priceAboveKumo && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				LogInfo($"Long entry: Tenkan ({tenkan}) crossed above Kijun ({kijun}) and price ({candle.ClosePrice}) above Kumo ({upperKumo})");
			}
			// Short entry: Bearish cross and price below Kumo
			else if (bearishCross && priceBelowKumo && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				LogInfo($"Short entry: Tenkan ({tenkan}) crossed below Kijun ({kijun}) and price ({candle.ClosePrice}) below Kumo ({lowerKumo})");
			}
			
			// Update previous values
			_prevTenkan = tenkan;
			_prevKijun = kijun;
		}
	}
}