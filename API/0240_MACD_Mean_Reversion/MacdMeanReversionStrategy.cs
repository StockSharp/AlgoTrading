namespace StockSharp.Strategies
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Strategies;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// MACD Histogram Mean Reversion strategy.
	/// This strategy enters positions when MACD Histogram is significantly below or above its average value.
	/// </summary>
	public class MacdMeanReversionStrategy : Strategy
	{
		private readonly StrategyParam<int> _fastMacdPeriod;
		private readonly StrategyParam<int> _slowMacdPeriod;
		private readonly StrategyParam<int> _signalPeriod;
		private readonly StrategyParam<int> _averagePeriod;
		private readonly StrategyParam<decimal> _deviationMultiplier;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLossPercent;

		private decimal _prevMacdHist;
		private decimal _avgMacdHist;
		private decimal _stdDevMacdHist;
		private decimal _sumMacdHist;
		private decimal _sumSquaresMacdHist;
		private int _count;
		private readonly Queue<decimal> _macdHistValues = new();

		/// <summary>
		/// Fast EMA period for MACD.
		/// </summary>
		public int FastMacdPeriod
		{
			get => _fastMacdPeriod.Value;
			set => _fastMacdPeriod.Value = value;
		}

		/// <summary>
		/// Slow EMA period for MACD.
		/// </summary>
		public int SlowMacdPeriod
		{
			get => _slowMacdPeriod.Value;
			set => _slowMacdPeriod.Value = value;
		}

		/// <summary>
		/// Signal line period for MACD.
		/// </summary>
		public int SignalPeriod
		{
			get => _signalPeriod.Value;
			set => _signalPeriod.Value = value;
		}

		/// <summary>
		/// Period for calculating mean and standard deviation of MACD Histogram.
		/// </summary>
		public int AveragePeriod
		{
			get => _averagePeriod.Value;
			set => _averagePeriod.Value = value;
		}

		/// <summary>
		/// Deviation multiplier for entry signals.
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
		/// Stop-loss percentage.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public MacdMeanReversionStrategy()
		{
			_fastMacdPeriod = Param(nameof(FastMacdPeriod), 12)
				.SetGreaterThanZero()
				.SetCanOptimize(true)
				.SetOptimize(8, 16, 4)
				.SetDisplay("Fast EMA Period", "Fast EMA period for MACD", "Indicators");

			_slowMacdPeriod = Param(nameof(SlowMacdPeriod), 26)
				.SetGreaterThanZero()
				.SetCanOptimize(true)
				.SetOptimize(20, 30, 5)
				.SetDisplay("Slow EMA Period", "Slow EMA period for MACD", "Indicators");

			_signalPeriod = Param(nameof(SignalPeriod), 9)
				.SetGreaterThanZero()
				.SetCanOptimize(true)
				.SetOptimize(5, 13, 4)
				.SetDisplay("Signal Period", "Signal line period for MACD", "Indicators");

			_averagePeriod = Param(nameof(AveragePeriod), 20)
				.SetGreaterThanZero()
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 10)
				.SetDisplay("Average Period", "Period for calculating MACD Histogram average", "Settings");

			_deviationMultiplier = Param(nameof(DeviationMultiplier), 2m)
				.SetGreaterThanZero()
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 3m, 0.5m)
				.SetDisplay("Deviation Multiplier", "Multiplier for standard deviation", "Settings");

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetGreaterThanZero()
				.SetCanOptimize(true)
				.SetOptimize(1m, 3m, 0.5m)
				.SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security security, DataType dataType)> GetWorkingSecurities()
		{
			return [(Security, CandleType)];
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			// Reset variables
			_prevMacdHist = 0;
			_avgMacdHist = 0;
			_stdDevMacdHist = 0;
			_sumMacdHist = 0;
			_sumSquaresMacdHist = 0;
			_count = 0;
			_macdHistValues.Clear();

			// Create MACD indicator
			var macd = new MovingAverageConvergenceDivergence
			{
				FastEma = new ExponentialMovingAverage { Length = FastMacdPeriod },
				SlowEma = new ExponentialMovingAverage { Length = SlowMacdPeriod },
				SignalEma = new ExponentialMovingAverage { Length = SignalPeriod }
			};

			// Create histogram indicator based on MACD
			var macdHistogram = new MacdHistogram { Macd = macd };

			// Create subscription and bind indicator
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(macdHistogram, ProcessCandle)
				.Start();

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, macd);
				DrawIndicator(area, macdHistogram);
				DrawOwnTrades(area);
			}

			// Enable position protection
			StartProtection(
				takeProfit: new Unit(0m), // We'll manage exits ourselves based on MACD Histogram
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
			);

			base.OnStarted(time);
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdHistValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Extract MACD Histogram value
			var currentMacdHist = macdHistValue.GetValue<decimal>();

			// Update MACD Histogram statistics
			UpdateMacdHistStatistics(currentMacdHist);

			// Save current MACD Histogram for next iteration
			_prevMacdHist = currentMacdHist;

			// If we don't have enough data yet for statistics
			if (_count < AveragePeriod)
				return;

			// Check for entry conditions
			if (Position == 0)
			{
				// Long entry - MACD Histogram is significantly below its average
				if (currentMacdHist < _avgMacdHist - _deviationMultiplier * _stdDevMacdHist)
				{
					BuyMarket(Volume);
					LogInfo($"Long entry: MACD Hist = {currentMacdHist}, Avg = {_avgMacdHist}, StdDev = {_stdDevMacdHist}");
				}
				// Short entry - MACD Histogram is significantly above its average
				else if (currentMacdHist > _avgMacdHist + _deviationMultiplier * _stdDevMacdHist)
				{
					SellMarket(Volume);
					LogInfo($"Short entry: MACD Hist = {currentMacdHist}, Avg = {_avgMacdHist}, StdDev = {_stdDevMacdHist}");
				}
			}
			// Check for exit conditions
			else if (Position > 0) // Long position
			{
				if (currentMacdHist > _avgMacdHist)
				{
					ClosePosition();
					LogInfo($"Long exit: MACD Hist = {currentMacdHist}, Avg = {_avgMacdHist}");
				}
			}
			else if (Position < 0) // Short position
			{
				if (currentMacdHist < _avgMacdHist)
				{
					ClosePosition();
					LogInfo($"Short exit: MACD Hist = {currentMacdHist}, Avg = {_avgMacdHist}");
				}
			}
		}

		private void UpdateMacdHistStatistics(decimal currentMacdHist)
		{
			// Add current value to the queue
			_macdHistValues.Enqueue(currentMacdHist);
			_sumMacdHist += currentMacdHist;
			_sumSquaresMacdHist += currentMacdHist * currentMacdHist;
			_count++;

			// If queue is larger than period, remove oldest value
			if (_macdHistValues.Count > AveragePeriod)
			{
				var oldestMacdHist = _macdHistValues.Dequeue();
				_sumMacdHist -= oldestMacdHist;
				_sumSquaresMacdHist -= oldestMacdHist * oldestMacdHist;
				_count--;
			}

			// Calculate average and standard deviation
			if (_count > 0)
			{
				_avgMacdHist = _sumMacdHist / _count;
				
				if (_count > 1)
				{
					var variance = (_sumSquaresMacdHist - (_sumMacdHist * _sumMacdHist) / _count) / (_count - 1);
					_stdDevMacdHist = variance <= 0 ? 0 : (decimal)Math.Sqrt((double)variance);
				}
				else
				{
					_stdDevMacdHist = 0;
				}
			}
		}
	}
}
