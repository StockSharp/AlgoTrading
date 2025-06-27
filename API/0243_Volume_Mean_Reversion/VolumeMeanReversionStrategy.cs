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
	/// Volume Mean Reversion strategy.
	/// This strategy enters positions when trading volume is significantly below or above its average value.
	/// </summary>
	public class VolumeMeanReversionStrategy : Strategy
	{
		private readonly StrategyParam<int> _averagePeriod;
		private readonly StrategyParam<decimal> _deviationMultiplier;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLossPercent;

		private decimal _avgVolume;
		private decimal _stdDevVolume;
		private decimal _sumVolume;
		private decimal _sumSquaresVolume;
		private int _count;
		private readonly Queue<decimal> _volumeValues = [];

		/// <summary>
		/// Period for calculating mean and standard deviation of Volume.
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
		public VolumeMeanReversionStrategy()
		{
			_averagePeriod = Param(nameof(AveragePeriod), 20)
				.SetGreaterThanZero()
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 10)
				.SetDisplay("Average Period", "Period for calculating Volume average and standard deviation", "Settings");

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
			_avgVolume = 0;
			_stdDevVolume = 0;
			_sumVolume = 0;
			_sumSquaresVolume = 0;
			_count = 0;
			_volumeValues.Clear();

			// Create Volume indicator (for visualization)
			var volume = new VolumeIndicator();

			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(ProcessCandle)
				.Start();

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, volume);
				DrawOwnTrades(area);

				// Create additional area for volume
				var volumeArea = CreateChartArea();
				if (volumeArea != null)
					DrawIndicator(volumeArea, volume);
			}

			// Enable position protection
			StartProtection(
				takeProfit: new Unit(0m), // We'll manage exits ourselves based on Volume
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
			);

			base.OnStarted(time);
		}

		private void ProcessCandle(ICandleMessage candle)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Extract Volume value (for candles, this is TotalVolume)
			var currentVolume = candle.TotalVolume;

			// Update Volume statistics
			UpdateVolumeStatistics(currentVolume);

			// If we don't have enough data yet for statistics
			if (_count < AveragePeriod)
				return;

			// For volume-based strategies, price direction is important
			var priceDirection = candle.ClosePrice > candle.OpenPrice ? Sides.Buy : Sides.Sell;

			// Check for entry conditions
			if (Position == 0)
			{
				// Volume is significantly below average - expecting a return to average trading activity
				if (currentVolume < _avgVolume - DeviationMultiplier * _stdDevVolume)
				{
					// In low volume environments, we might look for potential market accumulation
					// and follow the small price movement which could be institutional accumulation
					if (priceDirection == Sides.Buy)
					{
						BuyMarket(Volume);
						LogInfo($"Long entry: Volume = {currentVolume}, Avg = {_avgVolume}, StdDev = {_stdDevVolume}, Low volume with price up");
					}
					else
					{
						SellMarket(Volume);
						LogInfo($"Short entry: Volume = {currentVolume}, Avg = {_avgVolume}, StdDev = {_stdDevVolume}, Low volume with price down");
					}
				}
				// Volume is significantly above average - potential high volume climax
				else if (currentVolume > _avgVolume + DeviationMultiplier * _stdDevVolume)
				{
					// High volume often indicates climactic moves that might reverse
					// So we consider going against the price direction on high volume bars
					if (priceDirection == Sides.Sell)
					{
						BuyMarket(Volume);
						LogInfo($"Contrarian long entry: Volume = {currentVolume}, Avg = {_avgVolume}, StdDev = {_stdDevVolume}, High volume with price down");
					}
					else
					{
						SellMarket(Volume);
						LogInfo($"Contrarian short entry: Volume = {currentVolume}, Avg = {_avgVolume}, StdDev = {_stdDevVolume}, High volume with price up");
					}
				}
			}
			// Check for exit conditions
			else if (Position > 0) // Long position
			{
				// Exit long position when volume returns to average
				if (currentVolume > _avgVolume || (currentVolume > _avgVolume * 0.8m && priceDirection == Sides.Sell))
				{
					ClosePosition();
					LogInfo($"Long exit: Volume = {currentVolume}, Avg = {_avgVolume}");
				}
			}
			else if (Position < 0) // Short position
			{
				// Exit short position when volume returns to average
				if (currentVolume > _avgVolume || (currentVolume > _avgVolume * 0.8m && priceDirection == Sides.Buy))
				{
					ClosePosition();
					LogInfo($"Short exit: Volume = {currentVolume}, Avg = {_avgVolume}");
				}
			}
		}

		private void UpdateVolumeStatistics(decimal currentVolume)
		{
			// Add current value to the queue
			_volumeValues.Enqueue(currentVolume);
			_sumVolume += currentVolume;
			_sumSquaresVolume += currentVolume * currentVolume;
			_count++;

			// If queue is larger than period, remove oldest value
			if (_volumeValues.Count > AveragePeriod)
			{
				var oldestVolume = _volumeValues.Dequeue();
				_sumVolume -= oldestVolume;
				_sumSquaresVolume -= oldestVolume * oldestVolume;
				_count--;
			}

			// Calculate average and standard deviation
			if (_count > 0)
			{
				_avgVolume = _sumVolume / _count;
				
				if (_count > 1)
				{
					var variance = (_sumSquaresVolume - (_sumVolume * _sumVolume) / _count) / (_count - 1);
					_stdDevVolume = variance <= 0 ? 0 : (decimal)Math.Sqrt((double)variance);
				}
				else
				{
					_stdDevVolume = 0;
				}
			}
		}
	}
}
