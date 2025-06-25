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
	/// ADX Mean Reversion strategy.
	/// This strategy enters positions when ADX is significantly below or above its average value.
	/// </summary>
	public class AdxMeanReversionStrategy : Strategy
	{
		private readonly StrategyParam<int> _adxPeriod;
		private readonly StrategyParam<int> _averagePeriod;
		private readonly StrategyParam<decimal> _deviationMultiplier;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLossPercent;

		private decimal _prevAdx;
		private decimal _avgAdx;
		private decimal _stdDevAdx;
		private decimal _sumAdx;
		private decimal _sumSquaresAdx;
		private int _count;
		private readonly Queue<decimal> _adxValues = new();

		/// <summary>
		/// ADX Period.
		/// </summary>
		public int AdxPeriod
		{
			get => _adxPeriod.Value;
			set => _adxPeriod.Value = value;
		}

		/// <summary>
		/// Period for calculating mean and standard deviation of ADX.
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
		public AdxMeanReversionStrategy()
		{
			_adxPeriod = Param(nameof(AdxPeriod), 14)
				.SetGreaterThanZero()
				.SetCanOptimize(true)
				.SetOptimize(10, 20, 5)
				.SetDisplay("ADX Period", "Period for ADX indicator", "Indicators");

			_averagePeriod = Param(nameof(AveragePeriod), 20)
				.SetGreaterThanZero()
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 10)
				.SetDisplay("Average Period", "Period for calculating ADX average and standard deviation", "Settings");

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
			return new[] { (Security, CandleType) };
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			// Reset variables
			_prevAdx = 0;
			_avgAdx = 0;
			_stdDevAdx = 0;
			_sumAdx = 0;
			_sumSquaresAdx = 0;
			_count = 0;
			_adxValues.Clear();

			// Create ADX indicator
			var adx = new AverageDirectionalMovementIndex { Length = AdxPeriod };

			// Create subscription and bind indicator
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(adx, ProcessCandle)
				.Start();

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, adx);
				DrawOwnTrades(area);
			}

			// Enable position protection
			StartProtection(
				takeProfit: new Unit(0m), // We'll manage exits ourselves based on ADX
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
			);

			base.OnStarted(time);
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Extract ADX value
			var currentAdx = adxValue.GetValue<decimal>();

			// Update ADX statistics
			UpdateAdxStatistics(currentAdx);

			// Save current ADX for next iteration
			_prevAdx = currentAdx;

			// If we don't have enough data yet for statistics
			if (_count < AveragePeriod)
				return;

			// Check for entry conditions
			if (Position == 0)
			{
				// Positive trend strength should correspond to price direction for entry
				var plusDi = adxValue.GetValue<decimal>("Plus");
				var minusDi = adxValue.GetValue<decimal>("Minus");
				var direction = plusDi > minusDi ? Sides.Buy : Sides.Sell;

				// ADX is significantly below its average - mean reversion expects it to rise
				// This could indicate a period of low trend strength that might change
				if (currentAdx < _avgAdx - _deviationMultiplier * _stdDevAdx)
				{
					if (direction == Sides.Buy)
					{
						BuyMarket(Volume);
						LogInfo($"Long entry: ADX = {currentAdx}, Avg = {_avgAdx}, StdDev = {_stdDevAdx}, +DI > -DI");
					}
					else
					{
						SellMarket(Volume);
						LogInfo($"Short entry: ADX = {currentAdx}, Avg = {_avgAdx}, StdDev = {_stdDevAdx}, +DI < -DI");
					}
				}
				// ADX is significantly above its average - mean reversion expects it to fall
				// This could indicate a period of high trend strength that might weaken
				else if (currentAdx > _avgAdx + _deviationMultiplier * _stdDevAdx)
				{
					// For high ADX values, we're more cautious and might want to go against the direction
					// as extremely high ADX may indicate trend exhaustion
					if (direction == Sides.Sell)
					{
						BuyMarket(Volume);
						LogInfo($"Long entry (trend strength exhaustion): ADX = {currentAdx}, Avg = {_avgAdx}, StdDev = {_stdDevAdx}");
					}
					else
					{
						SellMarket(Volume);
						LogInfo($"Short entry (trend strength exhaustion): ADX = {currentAdx}, Avg = {_avgAdx}, StdDev = {_stdDevAdx}");
					}
				}
			}
			// Check for exit conditions
			else if (Position > 0) // Long position
			{
				if (currentAdx > _avgAdx)
				{
					ClosePosition();
					LogInfo($"Long exit: ADX = {currentAdx}, Avg = {_avgAdx}");
				}
			}
			else if (Position < 0) // Short position
			{
				if (currentAdx < _avgAdx)
				{
					ClosePosition();
					LogInfo($"Short exit: ADX = {currentAdx}, Avg = {_avgAdx}");
				}
			}
		}

		private void UpdateAdxStatistics(decimal currentAdx)
		{
			// Add current value to the queue
			_adxValues.Enqueue(currentAdx);
			_sumAdx += currentAdx;
			_sumSquaresAdx += currentAdx * currentAdx;
			_count++;

			// If queue is larger than period, remove oldest value
			if (_adxValues.Count > AveragePeriod)
			{
				var oldestAdx = _adxValues.Dequeue();
				_sumAdx -= oldestAdx;
				_sumSquaresAdx -= oldestAdx * oldestAdx;
				_count--;
			}

			// Calculate average and standard deviation
			if (_count > 0)
			{
				_avgAdx = _sumAdx / _count;
				
				if (_count > 1)
				{
					var variance = (_sumSquaresAdx - (_sumAdx * _sumAdx) / _count) / (_count - 1);
					_stdDevAdx = variance <= 0 ? 0 : (decimal)Math.Sqrt((double)variance);
				}
				else
				{
					_stdDevAdx = 0;
				}
			}
		}
	}
}
