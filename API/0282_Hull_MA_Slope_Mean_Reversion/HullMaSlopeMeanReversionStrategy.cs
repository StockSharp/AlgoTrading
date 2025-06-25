using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Hull Moving Average Slope Mean Reversion Strategy.
	/// This strategy trades based on the mean reversion of the Hull Moving Average slope.
	/// </summary>
	public class HullMaSlopeMeanReversionStrategy : Strategy
	{
		private readonly StrategyParam<int> _hullPeriod;
		private readonly StrategyParam<int> _lookbackPeriod;
		private readonly StrategyParam<decimal> _deviationMultiplier;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<decimal> _atrMultiplier;
		private readonly StrategyParam<DataType> _candleType;
		
		private HullMovingAverage _hullMa;
		private AverageTrueRange _atr;
		private SimpleMovingAverage _slopeAverage;
		private StandardDeviation _slopeStdDev;
		
		private decimal _currentHullMa;
		private decimal _prevHullMa;
		private decimal _currentSlope;
		private decimal _prevSlope;
		private decimal _prevSlopeAverage;
		private decimal _prevSlopeStdDev;
		private decimal _currentAtr;

		/// <summary>
		/// Hull Moving Average period.
		/// </summary>
		public int HullPeriod
		{
			get => _hullPeriod.Value;
			set => _hullPeriod.Value = value;
		}

		/// <summary>
		/// Lookback period for calculating the average and standard deviation of slope.
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
		/// ATR period for stop loss calculation.
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		/// <summary>
		/// ATR multiplier for stop loss calculation.
		/// </summary>
		public decimal AtrMultiplier
		{
			get => _atrMultiplier.Value;
			set => _atrMultiplier.Value = value;
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
		public HullMaSlopeMeanReversionStrategy()
		{
			_hullPeriod = Param(nameof(HullPeriod), 9)
				.SetDisplayName("Hull MA Period")
				.SetCategory("Hull MA")
				.SetCanOptimize(true)
				.SetOptimize(5, 20, 1);

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

			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetDisplayName("ATR Period")
				.SetCategory("Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(7, 21, 7);

			_atrMultiplier = Param(nameof(AtrMultiplier), 2.0m)
				.SetDisplayName("ATR Multiplier")
				.SetCategory("Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplayName("Candle Type")
				.SetCategory("General");
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
			_hullMa = new HullMovingAverage { Length = HullPeriod };
			_atr = new AverageTrueRange { Length = AtrPeriod };
			_slopeAverage = new SimpleMovingAverage { Length = LookbackPeriod };
			_slopeStdDev = new StandardDeviation { Length = LookbackPeriod };
			
			// Reset stored values
			_currentHullMa = 0;
			_prevHullMa = 0;
			_currentSlope = 0;
			_prevSlope = 0;
			_prevSlopeAverage = 0;
			_prevSlopeStdDev = 0;
			_currentAtr = 0;

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			// First binding for Hull MA
			subscription
				.BindEx(_hullMa, ProcessHullMa)
				.Start();
				
			// Additional binding for ATR (separate to keep the code cleaner)
			subscription
				.BindEx(_atr, ProcessAtr)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _hullMa);
				DrawOwnTrades(area);
			}
		}
		
		private void ProcessAtr(ICandleMessage candle, IIndicatorValue value)
		{
			if (candle.State != CandleStates.Finished)
				return;
				
			_currentAtr = value.GetValue<decimal>();
		}

		private void ProcessHullMa(ICandleMessage candle, IIndicatorValue value)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Get the Hull MA value
			_currentHullMa = value.GetValue<decimal>();
			
			// First value handling
			if (_prevHullMa == 0)
			{
				_prevHullMa = _currentHullMa;
				return;
			}
			
			// Calculate the slope of Hull MA
			_currentSlope = (_currentHullMa - _prevHullMa) / _prevHullMa * 100; // As percentage
			
			// Calculate average and standard deviation of slope
			var slopeAverage = _slopeAverage.Process(new DecimalIndicatorValue(_currentSlope)).GetValue<decimal>();
			var slopeStdDev = _slopeStdDev.Process(new DecimalIndicatorValue(_currentSlope)).GetValue<decimal>();
			
			// Skip until we have enough slope data
			if (_prevSlope == 0)
			{
				_prevSlope = _currentSlope;
				_prevSlopeAverage = slopeAverage;
				_prevSlopeStdDev = slopeStdDev;
				return;
			}
			
			// Calculate thresholds for slope
			var highThreshold = _prevSlopeAverage + _prevSlopeStdDev * DeviationMultiplier;
			var lowThreshold = _prevSlopeAverage - _prevSlopeStdDev * DeviationMultiplier;
			
			// Trading logic:
			// When slope is falling below the lower threshold (mean reversion - slope will rise)
			if (_currentSlope < lowThreshold && _prevSlope >= lowThreshold && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				LogInfo($"Hull MA slope fallen below threshold: {_currentSlope} < {lowThreshold}. Buying at {candle.ClosePrice}");
			}
			// When slope is rising above the upper threshold (mean reversion - slope will fall)
			else if (_currentSlope > highThreshold && _prevSlope <= highThreshold && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				LogInfo($"Hull MA slope risen above threshold: {_currentSlope} > {highThreshold}. Selling at {candle.ClosePrice}");
			}
			
			// Exit positions when slope returns to average
			else if (Position > 0 && _currentSlope > _prevSlopeAverage && _prevSlope <= _prevSlopeAverage)
			{
				SellMarket(Math.Abs(Position));
				LogInfo($"Hull MA slope returned to average from below: {_currentSlope} > {_prevSlopeAverage}. Closing long position at {candle.ClosePrice}");
			}
			else if (Position < 0 && _currentSlope < _prevSlopeAverage && _prevSlope >= _prevSlopeAverage)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Hull MA slope returned to average from above: {_currentSlope} < {_prevSlopeAverage}. Closing short position at {candle.ClosePrice}");
			}
			
			// Dynamic ATR-based stop loss
			else if (_currentAtr > 0)
			{
				var stopLevel = Position > 0 
					? candle.ClosePrice - _currentAtr * AtrMultiplier
					: Position < 0 
						? candle.ClosePrice + _currentAtr * AtrMultiplier
						: 0;
						
				if ((Position > 0 && candle.LowPrice <= stopLevel) || 
					(Position < 0 && candle.HighPrice >= stopLevel))
				{
					if (Position > 0)
					{
						SellMarket(Math.Abs(Position));
						LogInfo($"ATR Stop Loss triggered for long position: {candle.LowPrice} <= {stopLevel}. Closing at {candle.ClosePrice}");
					}
					else if (Position < 0)
					{
						BuyMarket(Math.Abs(Position));
						LogInfo($"ATR Stop Loss triggered for short position: {candle.HighPrice} >= {stopLevel}. Closing at {candle.ClosePrice}");
					}
				}
			}
			
			// Store current values for next comparison
			_prevHullMa = _currentHullMa;
			_prevSlope = _currentSlope;
			_prevSlopeAverage = slopeAverage;
			_prevSlopeStdDev = slopeStdDev;
		}
	}
}