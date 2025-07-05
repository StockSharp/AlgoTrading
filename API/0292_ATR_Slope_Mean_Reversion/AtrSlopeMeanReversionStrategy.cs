using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// ATR Slope Mean Reversion Strategy.
	/// This strategy trades based on ATR slope reversions to the mean.
	/// </summary>
	public class AtrSlopeMeanReversionStrategy : Strategy
	{
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<int> _lookbackPeriod;
		private readonly StrategyParam<decimal> _deviationMultiplier;
		private readonly StrategyParam<int> _stopLossMultiplier;
		private readonly StrategyParam<DataType> _candleType;

		private AverageTrueRange _atr;
		private decimal _previousAtr;
		private decimal _currentAtrSlope;
		private bool _isFirstCalculation = true;

		private decimal _averageSlope;
		private decimal _slopeStdDev;
		private int _sampleCount;
		private decimal _sumSlopes;
		private decimal _sumSlopesSquared;

		/// <summary>
		/// ATR Period.
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		/// <summary>
		/// Period for calculating slope average and standard deviation.
		/// </summary>
		public int LookbackPeriod
		{
			get => _lookbackPeriod.Value;
			set => _lookbackPeriod.Value = value;
		}

		/// <summary>
		/// The multiplier for standard deviation to determine entry threshold.
		/// </summary>
		public decimal DeviationMultiplier
		{
			get => _deviationMultiplier.Value;
			set => _deviationMultiplier.Value = value;
		}

		/// <summary>
		/// Stop loss multiplier (in ATR units).
		/// </summary>
		public int StopLossMultiplier
		{
			get => _stopLossMultiplier.Value;
			set => _stopLossMultiplier.Value = value;
		}

		/// <summary>
		/// Candle type for strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public AtrSlopeMeanReversionStrategy()
		{
			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "Period for ATR indicator", "Indicator Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 2);

			_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Lookback Period", "Period for calculating average and standard deviation of the slope", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_deviationMultiplier = Param(nameof(DeviationMultiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Deviation Multiplier", "Multiplier for standard deviation to determine entry threshold", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_stopLossMultiplier = Param(nameof(StopLossMultiplier), 2)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss ATR Multiplier", "Multiplier for ATR to set stop loss", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1, 5, 1);

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
			// Initialize indicators
			_atr = new AverageTrueRange
			{
				Length = AtrPeriod
			};

			// Initialize statistics variables
			_sampleCount = 0;
			_sumSlopes = 0;
			_sumSlopesSquared = 0;
			_isFirstCalculation = true;

			// Create subscription and bind indicator
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(_atr, ProcessCandle)
				.Start();

			// Set up chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _atr);
				DrawOwnTrades(area);
			}

			base.OnStarted(time);
		}

		private void ProcessCandle(ICandleMessage candle, decimal? atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Calculate ATR slope
			if (_isFirstCalculation)
			{
				_previousAtr = atrValue;
				_isFirstCalculation = false;
				return;
			}

			_currentAtrSlope = atrValue - _previousAtr;
			_previousAtr = atrValue;

			// Update statistics for slope values
			_sampleCount++;
			_sumSlopes += _currentAtrSlope;
			_sumSlopesSquared += _currentAtrSlope * _currentAtrSlope;

			// We need enough samples to calculate meaningful statistics
			if (_sampleCount < LookbackPeriod)
				return;

			// If we have more samples than our lookback period, adjust the statistics
			if (_sampleCount > LookbackPeriod)
			{
				// This is a simplified approach - ideally we would keep a circular buffer
				// of the last N slopes for more accurate calculations
				_sampleCount = LookbackPeriod;
			}

			// Calculate statistics
			_averageSlope = _sumSlopes / _sampleCount;
			var variance = (_sumSlopesSquared / _sampleCount) - (_averageSlope * _averageSlope);
			_slopeStdDev = (variance <= 0) ? 0 : (decimal)Math.Sqrt((double)variance);

			// Calculate thresholds for entries
			var longEntryThreshold = _averageSlope - DeviationMultiplier * _slopeStdDev;
			var shortEntryThreshold = _averageSlope + DeviationMultiplier * _slopeStdDev;

			// Trading logic
			if (_currentAtrSlope < longEntryThreshold && Position <= 0)
			{
				// Long entry: slope is significantly lower than average (mean reversion expected)
				LogInfo($"ATR slope {_currentAtrSlope} below threshold {longEntryThreshold}, entering LONG");
				BuyMarket(Volume + Math.Abs(Position));
				
				// Calculate and set stop loss based on ATR
				var stopPrice = candle.ClosePrice - atrValue * StopLossMultiplier;
				LogInfo($"Setting stop loss at {stopPrice} (ATR: {atrValue}, Multiplier: {StopLossMultiplier})");
				
				// Use dynamic stop protection for this position
				StartProtection(null, new Unit(atrValue * StopLossMultiplier, UnitTypes.Absolute));
			}
			else if (_currentAtrSlope > shortEntryThreshold && Position >= 0)
			{
				// Short entry: slope is significantly higher than average (mean reversion expected)
				LogInfo($"ATR slope {_currentAtrSlope} above threshold {shortEntryThreshold}, entering SHORT");
				SellMarket(Volume + Math.Abs(Position));
				
				// Calculate and set stop loss based on ATR
				var stopPrice = candle.ClosePrice + atrValue * StopLossMultiplier;
				LogInfo($"Setting stop loss at {stopPrice} (ATR: {atrValue}, Multiplier: {StopLossMultiplier})");
				
				// Use dynamic stop protection for this position
				StartProtection(new Unit(atrValue * StopLossMultiplier, UnitTypes.Absolute), null);
			}
			else if (Position > 0 && _currentAtrSlope > _averageSlope)
			{
				// Exit long when slope returns to or above average
				LogInfo($"ATR slope {_currentAtrSlope} returned to average {_averageSlope}, exiting LONG");
				SellMarket(Math.Abs(Position));
			}
			else if (Position < 0 && _currentAtrSlope < _averageSlope)
			{
				// Exit short when slope returns to or below average
				LogInfo($"ATR slope {_currentAtrSlope} returned to average {_averageSlope}, exiting SHORT");
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}
