using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Ichimoku Cloud Width Mean Reversion Strategy.
	/// This strategy trades based on the mean reversion of the Ichimoku Cloud width.
	/// </summary>
	public class IchimokuCloudWidthMeanReversionStrategy : Strategy
	{
		private readonly StrategyParam<int> _tenkanPeriod;
		private readonly StrategyParam<int> _kijunPeriod;
		private readonly StrategyParam<int> _senkouSpanBPeriod;
		private readonly StrategyParam<int> _lookbackPeriod;
		private readonly StrategyParam<decimal> _deviationMultiplier;
		private readonly StrategyParam<DataType> _candleType;
		
		private Ichimoku _ichimoku;
		private SimpleMovingAverage _cloudWidthAverage;
		private StandardDeviation _cloudWidthStdDev;
		
		private decimal _currentCloudWidth;
		private decimal _prevCloudWidth;
		private decimal _prevCloudWidthAverage;
		private decimal _prevCloudWidthStdDev;

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
		/// Lookback period for calculating the average and standard deviation of cloud width.
		/// </summary>
		public int LookbackPeriod
		{
			get => _lookbackPeriod.Value;
			set => _lookbackPeriod.Value = value;
		}

		/// <summary>
		/// Deviation multiplier for mean reversion detection.
		/// </summary>
		public decimal DeviationMultiplier
		{
			get => _deviationMultiplier.Value;
			set => _deviationMultiplier.Value = value;
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
		public IchimokuCloudWidthMeanReversionStrategy()
		{
			_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
				.SetDisplayName("Tenkan Period")
				.SetCategory("Ichimoku")
				.SetCanOptimize(true)
				.SetOptimize(5, 20, 1);

			_kijunPeriod = Param(nameof(KijunPeriod), 26)
				.SetDisplayName("Kijun Period")
				.SetCategory("Ichimoku")
				.SetCanOptimize(true)
				.SetOptimize(20, 40, 2);

			_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
				.SetDisplayName("Senkou Span B Period")
				.SetCategory("Ichimoku")
				.SetCanOptimize(true)
				.SetOptimize(40, 80, 4);

			_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
				.SetDisplayName("Lookback Period")
				.SetCategory("Mean Reversion")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_deviationMultiplier = Param(nameof(DeviationMultiplier), 2.0m)
				.SetDisplayName("Deviation Multiplier")
				.SetCategory("Mean Reversion")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplayName("Candle Type")
				.SetCategory("General");
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
			_ichimoku = new Ichimoku
			{
				TenkanPeriod = TenkanPeriod,
				KijunPeriod = KijunPeriod,
				SenkouSpanBPeriod = SenkouSpanBPeriod
			};
			
			_cloudWidthAverage = new SimpleMovingAverage { Length = LookbackPeriod };
			_cloudWidthStdDev = new StandardDeviation { Length = LookbackPeriod };
			
			// Reset stored values
			_currentCloudWidth = 0;
			_prevCloudWidth = 0;
			_prevCloudWidthAverage = 0;
			_prevCloudWidthStdDev = 0;

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(_ichimoku, ProcessIchimoku)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _ichimoku);
				DrawOwnTrades(area);
			}
			
			// Start position protection using Kijun-sen
			StartProtection(
				takeProfit: null,
				stopLoss: null
			);
		}

		private void ProcessIchimoku(ICandleMessage candle, IIndicatorValue value)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Extract values from the Ichimoku indicator
			var ichimokuValue = value.GetValue<IchimokuIndicatorValue>();
			var senkouSpanA = ichimokuValue.SenkouSpanA;
			var senkouSpanB = ichimokuValue.SenkouSpanB;
			var kijunSen = ichimokuValue.KijunSen;
			
			// Calculate cloud width (absolute difference between Senkou Span A and B)
			_currentCloudWidth = Math.Abs(senkouSpanA - senkouSpanB);
			
			// Calculate average and standard deviation of cloud width
			var cloudWidthAverage = _cloudWidthAverage.Process(new DecimalIndicatorValue(_currentCloudWidth)).GetValue<decimal>();
			var cloudWidthStdDev = _cloudWidthStdDev.Process(new DecimalIndicatorValue(_currentCloudWidth)).GetValue<decimal>();
			
			// Skip the first value
			if (_prevCloudWidth == 0)
			{
				_prevCloudWidth = _currentCloudWidth;
				_prevCloudWidthAverage = cloudWidthAverage;
				_prevCloudWidthStdDev = cloudWidthStdDev;
		}
	}
				return;
			}
			
			// Calculate thresholds
			var narrowThreshold = _prevCloudWidthAverage - _prevCloudWidthStdDev * DeviationMultiplier;
			var wideThreshold = _prevCloudWidthAverage + _prevCloudWidthStdDev * DeviationMultiplier;
			
			// Trading logic:
			// When cloud is narrowing (compression)
			if (_currentCloudWidth < narrowThreshold && _prevCloudWidth >= narrowThreshold && Position == 0)
			{
				// Determine direction based on price position relative to cloud
				if (candle.ClosePrice > Math.Max(senkouSpanA, senkouSpanB))
				{
					BuyMarket(Volume);
					LogInfo($"Ichimoku cloud compression (bullish): {_currentCloudWidth} < {narrowThreshold}. Buying at {candle.ClosePrice}");
				}
				else if (candle.ClosePrice < Math.Min(senkouSpanA, senkouSpanB))
				{
					SellMarket(Volume);
					LogInfo($"Ichimoku cloud compression (bearish): {_currentCloudWidth} < {narrowThreshold}. Selling at {candle.ClosePrice}");
				}
			}
			// When cloud is widening (expansion)
			else if (_currentCloudWidth > wideThreshold && _prevCloudWidth <= wideThreshold && Position == 0)
{
	// Determine direction based on price position relative to cloud
	if (candle.ClosePrice < Math.Min(senkouSpanA, senkouSpanB))
	{
		SellMarket(Volume);
		LogInfo($"Ichimoku cloud expansion (bearish): {_currentCloudWidth} > {wideThreshold}. Selling at {candle.ClosePrice}");
	}
	else if (candle.ClosePrice > Math.Max(senkouSpanA, senkouSpanB))
	{
		BuyMarket(Volume);
		LogInfo($"Ichimoku cloud expansion (bullish): {_currentCloudWidth} > {wideThreshold}. Buying at {candle.ClosePrice}");
	}
}

// Exit positions when width returns to average
else if (_currentCloudWidth >= 0.9m * _prevCloudWidthAverage &&
	 _currentCloudWidth <= 1.1m * _prevCloudWidthAverage &&
	 (_prevCloudWidth < 0.9m * _prevCloudWidthAverage || _prevCloudWidth > 1.1m * _prevCloudWidthAverage) &&
	 (Position != 0))
{
	if (Position > 0)
	{
		SellMarket(Math.Abs(Position));
		LogInfo($"Ichimoku cloud width returned to average: {_currentCloudWidth} ≈ {_prevCloudWidthAverage}. Closing long position at {candle.ClosePrice}");
	}
	else if (Position < 0)
	{
		BuyMarket(Math.Abs(Position));
		LogInfo($"Ichimoku cloud width returned to average: {_currentCloudWidth} ≈ {_prevCloudWidthAverage}. Closing short position at {candle.ClosePrice}");
	}
}
			}
			}
			}