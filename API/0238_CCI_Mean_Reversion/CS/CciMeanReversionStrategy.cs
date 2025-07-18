namespace StockSharp.Samples.Strategies
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
	/// CCI Mean Reversion strategy.
	/// This strategy enters positions when CCI is significantly below or above its average value.
	/// </summary>
	public class CciMeanReversionStrategy : Strategy
	{
		private readonly StrategyParam<int> _cciPeriod;
		private readonly StrategyParam<int> _averagePeriod;
		private readonly StrategyParam<decimal> _deviationMultiplier;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLossPercent;

		private decimal _prevCci;
		private decimal _avgCci;
		private decimal _stdDevCci;
		private decimal _sumCci;
		private decimal _sumSquaresCci;
		private int _count;
		private readonly Queue<decimal> _cciValues = [];

		/// <summary>
		/// CCI Period.
		/// </summary>
		public int CciPeriod
		{
			get => _cciPeriod.Value;
			set => _cciPeriod.Value = value;
		}

		/// <summary>
		/// Period for calculating mean and standard deviation of CCI.
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
		public CciMeanReversionStrategy()
		{
			_cciPeriod = Param(nameof(CciPeriod), 20)
				.SetGreaterThanZero()
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5)
				.SetDisplay("CCI Period", "Period for Commodity Channel Index", "Indicators");

			_averagePeriod = Param(nameof(AveragePeriod), 20)
				.SetGreaterThanZero()
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 10)
				.SetDisplay("Average Period", "Period for calculating CCI average and standard deviation", "Settings");

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
			_prevCci = 0;
			_avgCci = 0;
			_stdDevCci = 0;
			_sumCci = 0;
			_sumSquaresCci = 0;
			_count = 0;
			_cciValues.Clear();

			// Create CCI indicator
			var cci = new CommodityChannelIndex { Length = CciPeriod };

			// Create subscription and bind indicator
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(cci, ProcessCandle)
				.Start();

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, cci);
				DrawOwnTrades(area);
			}

			// Enable position protection
			StartProtection(
				takeProfit: new Unit(0m), // We'll manage exits ourselves based on CCI
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
			);

			base.OnStarted(time);
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue cciValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Extract CCI value
			var currentCci = cciValue.ToDecimal();

			// Update CCI statistics
			UpdateCciStatistics(currentCci);

			// Save current CCI for next iteration
			_prevCci = currentCci;

			// If we don't have enough data yet for statistics
			if (_count < AveragePeriod)
				return;

			// Check for entry conditions
			if (Position == 0)
			{
				// Long entry - CCI is significantly below its average
				if (currentCci < _avgCci - DeviationMultiplier * _stdDevCci)
				{
					BuyMarket(Volume);
					LogInfo($"Long entry: CCI = {currentCci}, CCI Avg = {_avgCci}, CCI StdDev = {_stdDevCci}");
				}
				// Short entry - CCI is significantly above its average
				else if (currentCci > _avgCci + DeviationMultiplier * _stdDevCci)
				{
					SellMarket(Volume);
					LogInfo($"Short entry: CCI = {currentCci}, CCI Avg = {_avgCci}, CCI StdDev = {_stdDevCci}");
				}
			}
			// Check for exit conditions
			else if (Position > 0) // Long position
			{
				if (currentCci > _avgCci)
				{
					ClosePosition();
					LogInfo($"Long exit: CCI = {currentCci}, CCI Avg = {_avgCci}");
				}
			}
			else if (Position < 0) // Short position
			{
				if (currentCci < _avgCci)
				{
					ClosePosition();
					LogInfo($"Short exit: CCI = {currentCci}, CCI Avg = {_avgCci}");
				}
			}
		}

		private void UpdateCciStatistics(decimal currentCci)
		{
			// Add current value to the queue
			_cciValues.Enqueue(currentCci);
			_sumCci += currentCci;
			_sumSquaresCci += currentCci * currentCci;
			_count++;

			// If queue is larger than period, remove oldest value
			if (_cciValues.Count > AveragePeriod)
			{
				var oldestCci = _cciValues.Dequeue();
				_sumCci -= oldestCci;
				_sumSquaresCci -= oldestCci * oldestCci;
				_count--;
			}

			// Calculate average and standard deviation
			if (_count > 0)
			{
				_avgCci = _sumCci / _count;
				
				if (_count > 1)
				{
					var variance = (_sumSquaresCci - (_sumCci * _sumCci) / _count) / (_count - 1);
					_stdDevCci = variance <= 0 ? 0 : (decimal)Math.Sqrt((double)variance);
				}
				else
				{
					_stdDevCci = 0;
				}
			}
		}
	}
}
