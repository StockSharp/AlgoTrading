using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// MACD Breakout Strategy that enters positions when MACD Histogram breaks out of its normal range.
	/// </summary>
	public class MacdBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _fastEmaPeriod;
		private readonly StrategyParam<int> _slowEmaPeriod;
		private readonly StrategyParam<int> _signalPeriod;
		private readonly StrategyParam<int> _smaPeriod;
		private readonly StrategyParam<decimal> _deviationMultiplier;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		private MovingAverageConvergenceDivergence _macd;
		private SimpleMovingAverage _macdHistSma;
		private StandardDeviation _macdHistStdDev;
		
		private decimal _prevMacdHistValue;
		private decimal _prevMacdHistSmaValue;

		/// <summary>
		/// MACD Fast EMA period.
		/// </summary>
		public int FastEmaPeriod
		{
			get => _fastEmaPeriod.Value;
			set => _fastEmaPeriod.Value = value;
		}

		/// <summary>
		/// MACD Slow EMA period.
		/// </summary>
		public int SlowEmaPeriod
		{
			get => _slowEmaPeriod.Value;
			set => _slowEmaPeriod.Value = value;
		}

		/// <summary>
		/// MACD Signal line period.
		/// </summary>
		public int SignalPeriod
		{
			get => _signalPeriod.Value;
			set => _signalPeriod.Value = value;
		}

		/// <summary>
		/// Period for MACD Histogram moving average.
		/// </summary>
		public int SmaPeriod
		{
			get => _smaPeriod.Value;
			set => _smaPeriod.Value = value;
		}

		/// <summary>
		/// Standard deviation multiplier for breakout threshold.
		/// </summary>
		public decimal DeviationMultiplier
		{
			get => _deviationMultiplier.Value;
			set => _deviationMultiplier.Value = value;
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
		/// Type of candles to use.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public MacdBreakoutStrategy()
		{
			_fastEmaPeriod = Param(nameof(FastEmaPeriod), 12)
				.SetGreaterThanZero()
				.SetDisplay("Fast EMA Period", "Period for MACD fast EMA", "MACD Settings")
				.SetCanOptimize(true)
				.SetOptimize(8, 20, 4);

			_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 26)
				.SetGreaterThanZero()
				.SetDisplay("Slow EMA Period", "Period for MACD slow EMA", "MACD Settings")
				.SetCanOptimize(true)
				.SetOptimize(20, 40, 4);

			_signalPeriod = Param(nameof(SignalPeriod), 9)
				.SetGreaterThanZero()
				.SetDisplay("Signal Period", "Period for MACD signal line", "MACD Settings")
				.SetCanOptimize(true)
				.SetOptimize(5, 15, 2);

			_smaPeriod = Param(nameof(SmaPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("SMA Period", "Period for MACD Histogram moving average", "Indicator Settings")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_deviationMultiplier = Param(nameof(DeviationMultiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Deviation Multiplier", "Standard deviation multiplier for breakout threshold", "Breakout Settings")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 4.0m, 0.5m);

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
			// Initialize indicators
			_macd = new MovingAverageConvergenceDivergence
			{
				LongMa = new ExponentialMovingAverage { Length = SlowEmaPeriod },
				ShortMa = new ExponentialMovingAverage { Length = FastEmaPeriod },
				SignalMa = new ExponentialMovingAverage { Length = SignalPeriod }
			};
			
			_macdHistSma = new SimpleMovingAverage { Length = SmaPeriod };
			_macdHistStdDev = new StandardDeviation { Length = SmaPeriod };

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.Bind(_macd, ProcessCandle)
				.Start();

			// Enable position protection
			StartProtection(
				new Unit(StopLossPercent, UnitTypes.Percent),
				new Unit(StopLossPercent * 1.5m, UnitTypes.Percent));

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _macd);
				DrawOwnTrades(area);
			}

			base.OnStarted(time);
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// MACD returns multiple values (MACD Line, Signal Line, Histogram)
			// We're interested in the histogram (difference between MACD and Signal)
			// Which is typically the third value (index 2)
			var macdValues = macdValue.GetValue<IList<decimal>>();
			
			// For MACD, the typical order is: MACD Line, Signal Line, Histogram
			// But we'll check to make sure we have enough values
			if (macdValues.Count < 3)
				return;

			// Extract the histogram value (MACD Line - Signal Line)
			var macdHistValue = macdValues[2];
			
			// Process indicators for MACD histogram
			var macdHistInput = new DecimalIndicatorValue(macdHistValue);
			var macdHistSmaValue = _macdHistSma.Process(macdHistInput).GetValue<decimal>();
			var macdHistStdDevValue = _macdHistStdDev.Process(macdHistInput).GetValue<decimal>();
			
			// Store previous values on first call
			if (_prevMacdHistValue == 0 && _prevMacdHistSmaValue == 0)
			{
				_prevMacdHistValue = macdHistValue;
				_prevMacdHistSmaValue = macdHistSmaValue;
				return;
			}

			// Calculate breakout thresholds
			var upperThreshold = macdHistSmaValue + DeviationMultiplier * macdHistStdDevValue;
			var lowerThreshold = macdHistSmaValue - DeviationMultiplier * macdHistStdDevValue;

			// Trading logic
			if (macdHistValue > upperThreshold && Position <= 0)
			{
				// MACD Histogram broke above upper threshold - buy signal (long)
				BuyMarket(Volume);
				this.AddInfoLog($"Buy signal: MACD Hist({macdHistValue}) > Upper Threshold({upperThreshold})");
			}
			else if (macdHistValue < lowerThreshold && Position >= 0)
			{
				// MACD Histogram broke below lower threshold - sell signal (short)
				SellMarket(Volume + Math.Abs(Position));
				this.AddInfoLog($"Sell signal: MACD Hist({macdHistValue}) < Lower Threshold({lowerThreshold})");
			}
			// Exit conditions
			else if (Position > 0 && macdHistValue < macdHistSmaValue)
			{
				// Exit long position when MACD Histogram returns below its mean
				SellMarket(Math.Abs(Position));
				this.AddInfoLog($"Exit long: MACD Hist({macdHistValue}) < SMA({macdHistSmaValue})");
			}
			else if (Position < 0 && macdHistValue > macdHistSmaValue)
			{
				// Exit short position when MACD Histogram returns above its mean
				BuyMarket(Math.Abs(Position));
				this.AddInfoLog($"Exit short: MACD Hist({macdHistValue}) > SMA({macdHistSmaValue})");
			}

			// Update previous values
			_prevMacdHistValue = macdHistValue;
			_prevMacdHistSmaValue = macdHistSmaValue;
		}
	}
}
