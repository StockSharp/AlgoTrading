using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Donchian Width Mean Reversion Strategy.
	/// This strategy trades based on the mean reversion of the Donchian Channel width.
	/// </summary>
	public class DonchianWidthMeanReversionStrategy : Strategy
	{
		private readonly StrategyParam<int> _donchianPeriod;
		private readonly StrategyParam<int> _lookbackPeriod;
		private readonly StrategyParam<decimal> _deviationMultiplier;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;
		
		private DonchianChannels _donchian;
		private SimpleMovingAverage _widthAverage;
		private StandardDeviation _widthStdDev;
		
		private decimal _currentWidth;
		private decimal _prevWidth;
		private decimal _prevWidthAverage;
		private decimal _prevWidthStdDev;

		/// <summary>
		/// Donchian Channel period.
		/// </summary>
		public int DonchianPeriod
		{
			get => _donchianPeriod.Value;
			set => _donchianPeriod.Value = value;
		}

		/// <summary>
		/// Lookback period for calculating the average and standard deviation of width.
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
		/// Stop loss percentage.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
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
		public DonchianWidthMeanReversionStrategy()
		{
			_donchianPeriod = Param(nameof(DonchianPeriod), 20)
				.SetDisplay("Donchian Period", "Donchian Channel period", "Donchian")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
				.SetDisplay("Lookback Period", "Lookback period for calculating the average and standard deviation of width", "Mean Reversion")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_deviationMultiplier = Param(nameof(DeviationMultiplier), 2.0m)
				.SetDisplay("Deviation Multiplier", "Deviation multiplier for mean reversion detection", "Mean Reversion")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
				.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 5.0m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Candle type for strategy", "General");
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

			// Initialize indicators
			_donchian = new DonchianChannels { Length = DonchianPeriod };
			_widthAverage = new SimpleMovingAverage { Length = LookbackPeriod };
			_widthStdDev = new StandardDeviation { Length = LookbackPeriod };
			
			// Reset stored values
			_currentWidth = 0;
			_prevWidth = 0;
			_prevWidthAverage = 0;
			_prevWidthStdDev = 0;

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(_donchian, ProcessDonchian)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _donchian);
				DrawOwnTrades(area);
			}
			
			// Start position protection
			StartProtection(
				takeProfit: null,
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
			);
		}

		private void ProcessDonchian(ICandleMessage candle, IIndicatorValue value)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Extract upper and lower bands from the indicator value
			var donchianValue = value.GetValue<(decimal upper, decimal middle, decimal lower)>();
			var upperBand = donchianValue.upper;
			var lowerBand = donchianValue.lower;
			
			// Calculate the Donchian channel width
			_currentWidth = upperBand - lowerBand;
			
			// Calculate the average and standard deviation of the width
			var widthAverage = _widthAverage.Process(_currentWidth, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal();
			var widthStdDev = _widthStdDev.Process(_currentWidth, candle.ServerTime, candle.State == CandleStates.Finished).ToDecimal();
			
			// Skip the first value
			if (_prevWidth == 0)
			{
				_prevWidth = _currentWidth;
				_prevWidthAverage = widthAverage;
				_prevWidthStdDev = widthStdDev;
				return;
			}
			
			// Calculate thresholds
			var narrowThreshold = _prevWidthAverage - _prevWidthStdDev * DeviationMultiplier;
			var wideThreshold = _prevWidthAverage + _prevWidthStdDev * DeviationMultiplier;
			
			// Trading logic:
			// When channel is narrowing (compression), enter long position
			if (_currentWidth < narrowThreshold && _prevWidth >= narrowThreshold && Position == 0)
			{
				BuyMarket(Volume);
				LogInfo($"Donchian channel width compression: {_currentWidth} < {narrowThreshold}. Buying at {candle.ClosePrice}");
			}
			// When channel is widening (expansion), enter short position
			else if (_currentWidth > wideThreshold && _prevWidth <= wideThreshold && Position == 0)
			{
				SellMarket(Volume);
				LogInfo($"Donchian channel width expansion: {_currentWidth} > {wideThreshold}. Selling at {candle.ClosePrice}");
			}
			
			// Exit positions when width returns to average
			else if ((Position > 0 || Position < 0) && 
					 Math.Abs(_currentWidth - _prevWidthAverage) < 0.1m * _prevWidthStdDev &&
					 Math.Abs(_prevWidth - _prevWidthAverage) >= 0.1m * _prevWidthStdDev)
			{
				if (Position > 0)
				{
					SellMarket(Math.Abs(Position));
					LogInfo($"Donchian width returned to average: {_currentWidth} ≈ {_prevWidthAverage}. Closing long position at {candle.ClosePrice}");
				}
				else if (Position < 0)
				{
					BuyMarket(Math.Abs(Position));
					LogInfo($"Donchian width returned to average: {_currentWidth} ≈ {_prevWidthAverage}. Closing short position at {candle.ClosePrice}");
				}
			}
			
			// Store current values for next comparison
			_prevWidth = _currentWidth;
			_prevWidthAverage = widthAverage;
			_prevWidthStdDev = widthStdDev;
		}
	}
}