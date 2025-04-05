using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Strategies
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
		
		private decimal? _lastKijun = null;
		
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
				
			_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(15)))
				.SetDisplay("Candle Type", "Type of candles to use", "General");
		}
		
		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return new[] { (Security, CandleType) };
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
				TenkanPeriod = TenkanPeriod,
				KijunPeriod = KijunPeriod,
				SenkouSpanBPeriod = SenkouSpanBPeriod
			};
			
			// Create candles subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind indicators to subscription
			subscription
				.Bind(_williamsR, _ichimoku, ProcessCandle)
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
		
		private void ProcessCandle(ICandleMessage candle, decimal williamsRValue, IIndicatorValue ichimokuValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;
				
			// Skip if strategy is not ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
				
			// Extract Ichimoku values
			var ichimokuValues = ichimokuValue as IComplexIndicatorValue;
			if (ichimokuValues == null)
				return;
				
			var tenkan = ichimokuValues[Ichimoku.TenkanLine].GetValue<decimal>();
			var kijun = ichimokuValues[Ichimoku.KijunLine].GetValue<decimal>();
			
			var senkouA = ichimokuValues[Ichimoku.SenkouSpanA].GetValue<decimal>();
			var senkouB = ichimokuValues[Ichimoku.SenkouSpanB].GetValue<decimal>();
			
			// Determine if price is above or below the Kumo (cloud)
			var kumoTop = Math.Max(senkouA, senkouB);
			var kumoBottom = Math.Min(senkouA, senkouB);
			var isPriceAboveKumo = candle.ClosePrice > kumoTop;
			var isPriceBelowKumo = candle.ClosePrice < kumoBottom;
			
			// Save current Kijun for stop-loss
			_lastKijun = kijun;
			
			// Trading logic
			if (williamsRValue < -80 && isPriceAboveKumo && tenkan > kijun)
			{
				// Long signal: %R < -80 (oversold), price above Kumo, Tenkan > Kijun
				if (Position <= 0)
				{
					// Close any existing short position and open long
					BuyMarket(Volume + Math.Abs(Position));
					this.AddInfoLog($"Long Entry: %R={williamsRValue:F2}, Price above Kumo, Tenkan > Kijun");
				}
			}
			else if (williamsRValue > -20 && isPriceBelowKumo && tenkan < kijun)
			{
				// Short signal: %R > -20 (overbought), price below Kumo, Tenkan < Kijun
				if (Position >= 0)
				{
					// Close any existing long position and open short
					SellMarket(Volume + Math.Abs(Position));
					this.AddInfoLog($"Short Entry: %R={williamsRValue:F2}, Price below Kumo, Tenkan < Kijun");
				}
			}
			else if ((Position > 0 && candle.ClosePrice < kumoBottom) || 
					(Position < 0 && candle.ClosePrice > kumoTop))
			{
				// Exit positions when price crosses the Kumo
				if (Position > 0)
				{
					SellMarket(Math.Abs(Position));
					this.AddInfoLog("Exit Long: Price crossed below Kumo");
				}
				else if (Position < 0)
				{
					BuyMarket(Math.Abs(Position));
					this.AddInfoLog("Exit Short: Price crossed above Kumo");
				}
			}
			
			// Update dynamic stop-loss based on Kijun-sen
			if (_lastKijun != null && Position != 0)
			{
				var entryPrice = Security.GetCurrentPrice(Position > 0 ? Sides.Buy : Sides.Sell);
				if (entryPrice != 0)
				{
					var stopLevel = Position > 0 ? 
						Math.Min(entryPrice * 0.97m, kijun) :  // For long positions
						Math.Max(entryPrice * 1.03m, kijun);   // For short positions
						
					// The distance from current price to stop price in percentage
					var stopDistance = Math.Abs((stopLevel / entryPrice - 1) * 100);
					
					// Update protection with dynamic stop
					UpdateProtection(
						takeProfit: new Unit(0, UnitTypes.Absolute),
						stopLoss: new Unit(stopDistance, UnitTypes.Percent)
					);
				}
			}
		}
	}
}
