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
	/// Williams %R Mean Reversion strategy.
	/// This strategy enters positions when Williams %R is significantly below or above its average value.
	/// </summary>
	public class WilliamsRMeanReversionStrategy : Strategy
	{
		private readonly StrategyParam<int> _williamsRPeriod;
		private readonly StrategyParam<int> _averagePeriod;
		private readonly StrategyParam<decimal> _deviationMultiplier;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLossPercent;

		private decimal _prevWilliamsR;
		private decimal _avgWilliamsR;
		private decimal _stdDevWilliamsR;
		private decimal _sumWilliamsR;
		private decimal _sumSquaresWilliamsR;
		private int _count;
		private readonly Queue<decimal> _williamsRValues = [];

		/// <summary>
		/// Williams %R Period.
		/// </summary>
		public int WilliamsRPeriod
		{
			get => _williamsRPeriod.Value;
			set => _williamsRPeriod.Value = value;
		}

		/// <summary>
		/// Period for calculating mean and standard deviation of Williams %R.
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
		public WilliamsRMeanReversionStrategy()
		{
			_williamsRPeriod = Param(nameof(WilliamsRPeriod), 14)
				.SetGreaterThanZero()
				.SetCanOptimize(true)
				.SetOptimize(7, 21, 7)
				.SetDisplay("Williams %R Period", "Period for Williams %R indicator", "Indicators");

			_averagePeriod = Param(nameof(AveragePeriod), 20)
				.SetGreaterThanZero()
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 10)
				.SetDisplay("Average Period", "Period for calculating Williams %R average and standard deviation", "Settings");

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
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return [(Security, CandleType)];
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			// Reset variables
			_prevWilliamsR = 0;
			_avgWilliamsR = 0;
			_stdDevWilliamsR = 0;
			_sumWilliamsR = 0;
			_sumSquaresWilliamsR = 0;
			_count = 0;
			_williamsRValues.Clear();

			// Create Williams %R indicator
			var williamsR = new WilliamsR { Length = WilliamsRPeriod };

			// Create subscription and bind indicator
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(williamsR, ProcessCandle)
				.Start();

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, williamsR);
				DrawOwnTrades(area);
			}

			// Enable position protection
			StartProtection(
				takeProfit: new Unit(0m), // We'll manage exits ourselves based on Williams %R
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
			);

			base.OnStarted(time);
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue williamsRValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Extract Williams %R value
			var currentWilliamsR = williamsRValue.ToDecimal();

			// Update Williams %R statistics
			UpdateWilliamsRStatistics(currentWilliamsR);

			// Save current Williams %R for next iteration
			_prevWilliamsR = currentWilliamsR;

			// If we don't have enough data yet for statistics
			if (_count < AveragePeriod)
				return;

			// Check for entry conditions
			if (Position == 0)
			{
				// Long entry - Williams %R is significantly below its average
				if (currentWilliamsR < _avgWilliamsR - DeviationMultiplier * _stdDevWilliamsR)
				{
					BuyMarket(Volume);
					LogInfo($"Long entry: Williams %R = {currentWilliamsR}, Avg = {_avgWilliamsR}, StdDev = {_stdDevWilliamsR}");
				}
				// Short entry - Williams %R is significantly above its average
				else if (currentWilliamsR > _avgWilliamsR + DeviationMultiplier * _stdDevWilliamsR)
				{
					SellMarket(Volume);
					LogInfo($"Short entry: Williams %R = {currentWilliamsR}, Avg = {_avgWilliamsR}, StdDev = {_stdDevWilliamsR}");
				}
			}
			// Check for exit conditions
			else if (Position > 0) // Long position
			{
				if (currentWilliamsR > _avgWilliamsR)
				{
					ClosePosition();
					LogInfo($"Long exit: Williams %R = {currentWilliamsR}, Avg = {_avgWilliamsR}");
				}
			}
			else if (Position < 0) // Short position
			{
				if (currentWilliamsR < _avgWilliamsR)
				{
					ClosePosition();
					LogInfo($"Short exit: Williams %R = {currentWilliamsR}, Avg = {_avgWilliamsR}");
				}
			}
		}

		private void UpdateWilliamsRStatistics(decimal currentWilliamsR)
		{
			// Add current value to the queue
			_williamsRValues.Enqueue(currentWilliamsR);
			_sumWilliamsR += currentWilliamsR;
			_sumSquaresWilliamsR += currentWilliamsR * currentWilliamsR;
			_count++;

			// If queue is larger than period, remove oldest value
			if (_williamsRValues.Count > AveragePeriod)
			{
				var oldestWilliamsR = _williamsRValues.Dequeue();
				_sumWilliamsR -= oldestWilliamsR;
				_sumSquaresWilliamsR -= oldestWilliamsR * oldestWilliamsR;
				_count--;
			}

			// Calculate average and standard deviation
			if (_count > 0)
			{
				_avgWilliamsR = _sumWilliamsR / _count;
				
				if (_count > 1)
				{
					var variance = (_sumSquaresWilliamsR - (_sumWilliamsR * _sumWilliamsR) / _count) / (_count - 1);
					_stdDevWilliamsR = variance <= 0 ? 0 : (decimal)Math.Sqrt((double)variance);
				}
				else
				{
					_stdDevWilliamsR = 0;
				}
			}
		}
	}
}
