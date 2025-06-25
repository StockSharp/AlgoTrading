using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Ichimoku with Volatility Contraction strategy.
	/// Enters positions when Ichimoku signals a trend and volatility is contracting.
	/// </summary>
	public class IchimokuVolatilityContractionStrategy : Strategy
	{
		private readonly StrategyParam<int> _tenkanPeriod;
		private readonly StrategyParam<int> _kijunPeriod;
		private readonly StrategyParam<int> _senkouSpanBPeriod;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<decimal> _deviationFactor;
		private readonly StrategyParam<DataType> _candleType;
		
		private decimal _avgAtr;
		private decimal _atrStdDev;
		private int _processedCandles;

		/// <summary>
		/// Tenkan-sen (Conversion Line) period.
		/// </summary>
		public int TenkanPeriod
		{
			get => _tenkanPeriod.Value;
			set => _tenkanPeriod.Value = value;
		}

		/// <summary>
		/// Kijun-sen (Base Line) period.
		/// </summary>
		public int KijunPeriod
		{
			get => _kijunPeriod.Value;
			set => _kijunPeriod.Value = value;
		}

		/// <summary>
		/// Senkou Span B (Leading Span B) period.
		/// </summary>
		public int SenkouSpanBPeriod
		{
			get => _senkouSpanBPeriod.Value;
			set => _senkouSpanBPeriod.Value = value;
		}

		/// <summary>
		/// ATR period for volatility calculation.
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		/// <summary>
		/// Deviation factor for volatility contraction detection.
		/// </summary>
		public decimal DeviationFactor
		{
			get => _deviationFactor.Value;
			set => _deviationFactor.Value = value;
		}

		/// <summary>
		/// Candle type for strategy calculation.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initialize strategy.
		/// </summary>
		public IchimokuVolatilityContractionStrategy()
		{
			_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
				.SetGreaterThanZero()
				.SetDisplay("Tenkan Period", "Period for Tenkan-sen (Conversion Line)", "Ichimoku Settings")
				.SetCanOptimize(true)
				.SetOptimize(7, 11, 1);

			_kijunPeriod = Param(nameof(KijunPeriod), 26)
				.SetGreaterThanZero()
				.SetDisplay("Kijun Period", "Period for Kijun-sen (Base Line)", "Ichimoku Settings")
				.SetCanOptimize(true)
				.SetOptimize(20, 30, 2);

			_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
				.SetGreaterThanZero()
				.SetDisplay("Senkou Span B Period", "Period for Senkou Span B (Leading Span B)", "Ichimoku Settings")
				.SetCanOptimize(true)
				.SetOptimize(40, 60, 4);

			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "Period for Average True Range calculation", "Volatility Settings")
				.SetCanOptimize(true)
				.SetOptimize(10, 20, 2);

			_deviationFactor = Param(nameof(DeviationFactor), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Deviation Factor", "Factor multiplied by standard deviation to detect volatility contraction", "Volatility Settings")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 3.0m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

			// Initialize values
			_avgAtr = 0;
			_atrStdDev = 0;
			_processedCandles = 0;

			// Create Ichimoku indicator
			var ichimoku = new Ichimoku
			{
				TenkanPeriod = TenkanPeriod,
				KijunPeriod = KijunPeriod,
				SenkouSpanBPeriod = SenkouSpanBPeriod
			};

			// Create ATR indicator for volatility measurement
			var atr = new AverageTrueRange
			{
				Length = AtrPeriod
			};

			// Create subscription for candles
			var subscription = SubscribeCandles(CandleType);
			
			// Bind indicators to the subscription
			subscription
				.BindEx(ichimoku, atr, ProcessCandle)
				.Start();

			// Start position protection
			StartProtection(
				takeProfit: new Unit(2, UnitTypes.Percent),
				stopLoss: new Unit(1, UnitTypes.Percent)
			);

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, ichimoku);
				DrawIndicator(area, atr);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue, IIndicatorValue atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Get ATR value and calculate statistics
			var currentAtr = atrValue.GetValue<decimal>();
			_processedCandles++;
			
			// Using exponential moving average approach for ATR statistics
			if (_processedCandles == 1)
			{
				_avgAtr = currentAtr;
				_atrStdDev = 0;
			}
			else
			{
				// Update average ATR with smoothing factor
				decimal alpha = 2.0m / (AtrPeriod + 1);
				decimal oldAvg = _avgAtr;
				_avgAtr = alpha * currentAtr + (1 - alpha) * _avgAtr;
				
				// Update standard deviation (simplified approach)
				decimal atrDev = Math.Abs(currentAtr - oldAvg);
				_atrStdDev = alpha * atrDev + (1 - alpha) * _atrStdDev;
			}

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Extract Ichimoku values
			decimal tenkan = ichimokuValue[0].To<decimal>();     // Tenkan-sen (Conversion Line)
			decimal kijun = ichimokuValue[1].To<decimal>();      // Kijun-sen (Base Line)
			decimal senkouA = ichimokuValue[2].To<decimal>();    // Senkou Span A (Leading Span A)
			decimal senkouB = ichimokuValue[3].To<decimal>();    // Senkou Span B (Leading Span B)
			
			// Determine Kumo (cloud) boundaries
			decimal upperKumo = Math.Max(senkouA, senkouB);
			decimal lowerKumo = Math.Min(senkouA, senkouB);
			
			// Check for volatility contraction
			bool isVolatilityContraction = currentAtr < (_avgAtr - DeviationFactor * _atrStdDev);
			
			// Log the values
			LogInfo($"Tenkan: {tenkan}, Kijun: {kijun}, Cloud: {lowerKumo}-{upperKumo}, " +
							$"ATR: {currentAtr}, Avg ATR: {_avgAtr}, Contraction: {isVolatilityContraction}");

			// Trading logic with volatility contraction filter
			if (isVolatilityContraction)
			{
				// Bullish signal: Price above cloud and Tenkan above Kijun
				if (candle.ClosePrice > upperKumo && tenkan > kijun && Position <= 0)
				{
					// Close any existing short position
					if (Position < 0)
						BuyMarket(Math.Abs(Position));
					
					// Open long position
					BuyMarket(Volume);
					LogInfo($"Long signal: Price ({candle.ClosePrice}) above cloud, " +
									$"Tenkan ({tenkan}) > Kijun ({kijun}) with volatility contraction");
				}
				// Bearish signal: Price below cloud and Tenkan below Kijun
				else if (candle.ClosePrice < lowerKumo && tenkan < kijun && Position >= 0)
				{
					// Close any existing long position
					if (Position > 0)
						SellMarket(Math.Abs(Position));
					
					// Open short position
					SellMarket(Volume);
					LogInfo($"Short signal: Price ({candle.ClosePrice}) below cloud, " +
									$"Tenkan ({tenkan}) < Kijun ({kijun}) with volatility contraction");
				}
			}
			
			// Exit logic
			if ((Position > 0 && candle.ClosePrice < lowerKumo) ||
				(Position < 0 && candle.ClosePrice > upperKumo))
			{
				// Close position when price crosses the cloud in the opposite direction
				ClosePosition();
				LogInfo($"Exit signal: Price exited cloud in opposite direction. Position closed at {candle.ClosePrice}");
			}
		}
	}
}