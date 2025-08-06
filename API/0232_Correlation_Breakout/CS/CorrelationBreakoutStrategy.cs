using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that trades based on correlation breakout between two assets.
	/// </summary>
	public class CorrelationBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<Security> _asset1Param;
		private readonly StrategyParam<Security> _asset2Param;
		private readonly StrategyParam<DataType> _candleTypeParam;
		private readonly StrategyParam<int> _lookbackPeriodParam;
		private readonly StrategyParam<decimal> _thresholdParam;
		private readonly StrategyParam<decimal> _stopLossPercentParam;

		private decimal[] _asset1Prices;
		private decimal[] _asset2Prices;
		private int _currentIndex;
		private readonly StandardDeviation _corrStdDev;
		private decimal _avgCorrelation;
		private decimal _lastCorrelation;
		private bool _isInitialized;

		/// <summary>
		/// First asset for correlation.
		/// </summary>
		public Security Asset1
		{
			get => _asset1Param.Value;
			set => _asset1Param.Value = value;
		}

		/// <summary>
		/// Second asset for correlation.
		/// </summary>
		public Security Asset2
		{
			get => _asset2Param.Value;
			set => _asset2Param.Value = value;
		}

		/// <summary>
		/// Candle type for data.
		/// </summary>
		public DataType CandleType
		{
			get => _candleTypeParam.Value;
			set => _candleTypeParam.Value = value;
		}

		/// <summary>
		/// Period for calculating correlations.
		/// </summary>
		public int LookbackPeriod
		{
			get => _lookbackPeriodParam.Value;
			set
			{
				_lookbackPeriodParam.Value = value;
				if (_asset1Prices != null)
				{
					_asset1Prices = new decimal[value];
					_asset2Prices = new decimal[value];
				}
			}
		}

		/// <summary>
		/// Threshold multiplier for standard deviation.
		/// </summary>
		public decimal Threshold
		{
			get => _thresholdParam.Value;
			set => _thresholdParam.Value = value;
		}

		/// <summary>
		/// Stop loss percentage.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercentParam.Value;
			set => _stopLossPercentParam.Value = value;
		}

		/// <summary>
		/// Strategy constructor.
		/// </summary>
		public CorrelationBreakoutStrategy()
		{
			_asset1Param = Param<Security>(nameof(Asset1))
				.SetDisplay("Asset 1", "First asset for correlation", "Instruments");

			_asset2Param = Param<Security>(nameof(Asset2))
				.SetDisplay("Asset 2", "Second asset for correlation", "Instruments");

			_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_lookbackPeriodParam = Param(nameof(LookbackPeriod), 20)
				.SetDisplay("Lookback Period", "Period for calculating correlations", "Strategy")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5)
				.SetGreaterThanZero();

			_thresholdParam = Param(nameof(Threshold), 2m)
				.SetDisplay("Threshold", "Threshold multiplier for standard deviation", "Strategy")
				.SetCanOptimize(true)
				.SetOptimize(1m, 3m, 0.5m)
				.SetNotNegative();

			_stopLossPercentParam = Param(nameof(StopLossPercent), 2m)
				.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
				.SetNotNegative();

			_corrStdDev = new StandardDeviation { Length = LookbackPeriod };
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			if (Asset1 != null && CandleType != null)
				yield return (Asset1, CandleType);

			if (Asset2 != null && CandleType != null)
				yield return (Asset2, CandleType);
		}

		/// <inheritdoc />
		protected override void OnReseted()
		{
			base.OnReseted();

			_corrStdDev.Reset();
			_currentIndex = 0;
			_avgCorrelation = 0;
			_lastCorrelation = 0;
			_isInitialized = false;

			// Initialize arrays
			_asset1Prices = new decimal[LookbackPeriod];
			_asset2Prices = new decimal[LookbackPeriod];
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Subscribe to candles for both assets
			if (Asset1 != null && Asset2 != null && CandleType != null)
			{
				var asset1Subscription = SubscribeCandles(CandleType, security: Asset1);
				var asset2Subscription = SubscribeCandles(CandleType, security: Asset2);

				asset1Subscription
					.Bind(ProcessAsset1Candle)
					.Start();

				asset2Subscription
					.Bind(ProcessAsset2Candle)
					.Start();

				// Create chart areas if available
				var area = CreateChartArea();
				if (area != null)
				{
					DrawCandles(area, asset1Subscription);
					DrawCandles(area, asset2Subscription);
					DrawOwnTrades(area);
				}
			}
			else
			{
				LogWarning("Assets or candle type not specified. Strategy won't work properly.");
			}

			// Start position protection with stop-loss
			StartProtection(
				takeProfit: null,
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
			);
		}

		private void ProcessAsset1Candle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;

			_asset1Prices[_currentIndex] = candle.ClosePrice;
			CalculateCorrelation(candle);
		}

		private void ProcessAsset2Candle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;

			_asset2Prices[_currentIndex] = candle.ClosePrice;
			CalculateCorrelation(candle);
		}

		private void CalculateCorrelation(ICandleMessage candle)
		{
			// We need both prices for the same bar to calculate correlation
			if (_asset1Prices[_currentIndex] == 0 || _asset2Prices[_currentIndex] == 0)
				return;

			// Increment index for next bar
			_currentIndex = (_currentIndex + 1) % LookbackPeriod;

			// Skip calculation until we have enough data
			if (!_isInitialized && _currentIndex == 0)
				_isInitialized = true;

			if (!_isInitialized)
				return;

			// Calculate correlation
			_lastCorrelation = CalculatePearsonCorrelation(_asset1Prices, _asset2Prices);

			// Process correlation through the indicator
			var stdDevValue = _corrStdDev.Process(_lastCorrelation, candle.ServerTime, candle.State == CandleStates.Finished);

			// Move to next bar after first LookbackPeriod bars filled
			if (!_corrStdDev.IsFormed)
			{
				// Update running average
				_avgCorrelation = (_avgCorrelation * (_currentIndex == 0 ? LookbackPeriod - 1 : _currentIndex - 1) + _lastCorrelation) / 
					(_currentIndex == 0 ? LookbackPeriod : _currentIndex);
				return;
			}

			// Update running average after the indicator is formed
			_avgCorrelation = (_avgCorrelation * (LookbackPeriod - 1) + _lastCorrelation) / LookbackPeriod;

			// Check trading conditions
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var stdDev = stdDevValue.ToDecimal();

			// Trading logic for correlation breakout
			if (_lastCorrelation < _avgCorrelation - Threshold * stdDev && GetPositionValue(Asset1) <= 0 && GetPositionValue(Asset2) >= 0)
			{
				// Correlation breakdown - go long Asset1, short Asset2
				LogInfo($"Correlation breakdown: {_lastCorrelation} < {_avgCorrelation - Threshold * stdDev}");
				BuyMarket(Volume, Asset1);
				SellMarket(Volume, Asset2);
			}
			else if (_lastCorrelation > _avgCorrelation + Threshold * stdDev && GetPositionValue(Asset1) >= 0 && GetPositionValue(Asset2) <= 0)
			{
				// Correlation spike - go short Asset1, long Asset2
				LogInfo($"Correlation spike: {_lastCorrelation} > {_avgCorrelation + Threshold * stdDev}");
				SellMarket(Volume, Asset1);
				BuyMarket(Volume, Asset2);
			}
			else if (Math.Abs(_lastCorrelation - _avgCorrelation) < 0.2m * stdDev)
			{
				// Close position when correlation returns to average
				LogInfo($"Correlation returned to average: {_lastCorrelation} ≈ {_avgCorrelation}");
				
				if (GetPositionValue(Asset1) > 0)
					SellMarket(Math.Abs(GetPositionValue(Asset1)), Asset1);
					
				if (GetPositionValue(Asset1) < 0)
					BuyMarket(Math.Abs(GetPositionValue(Asset1)), Asset1);
					
				if (GetPositionValue(Asset2) > 0)
					SellMarket(Math.Abs(GetPositionValue(Asset2)), Asset2);
					
				if (GetPositionValue(Asset2) < 0)
					BuyMarket(Math.Abs(GetPositionValue(Asset2)), Asset2);
			}
		}

		private decimal CalculatePearsonCorrelation(decimal[] x, decimal[] y)
		{
			int n = x.Length;
			
			decimal sumX = 0, sumY = 0, sumXY = 0;
			decimal sumX2 = 0, sumY2 = 0;
			
			for (int i = 0; i < n; i++)
			{
				sumX += x[i];
				sumY += y[i];
				sumXY += x[i] * y[i];
				sumX2 += x[i] * x[i];
				sumY2 += y[i] * y[i];
			}
			
			decimal denominator = (decimal)Math.Sqrt((double)(n * sumX2 - sumX * sumX) * (double)(n * sumY2 - sumY * sumY));
			
			if (denominator == 0)
				return 0;
				
			return (n * sumXY - sumX * sumY) / denominator;
		}

		private decimal GetPositionValue(Security security)
		{
			return GetPositionValue(security, Portfolio) ?? 0;
		}
	}
}
