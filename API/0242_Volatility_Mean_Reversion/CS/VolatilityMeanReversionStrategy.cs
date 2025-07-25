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
	/// Volatility Mean Reversion strategy.
	/// This strategy enters positions when ATR (volatility) is significantly below or above its average value.
	/// </summary>
	public class VolatilityMeanReversionStrategy : Strategy
	{
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<int> _averagePeriod;
		private readonly StrategyParam<decimal> _deviationMultiplier;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLossPercent;

		private decimal _prevAtr;
		private decimal _avgAtr;
		private decimal _stdDevAtr;
		private decimal _sumAtr;
		private decimal _sumSquaresAtr;
		private int _count;
		private readonly Queue<decimal> _atrValues = [];

		/// <summary>
		/// ATR Period.
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		/// <summary>
		/// Period for calculating mean and standard deviation of ATR.
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
		public VolatilityMeanReversionStrategy()
		{
			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetGreaterThanZero()
				.SetCanOptimize(true)
				.SetOptimize(10, 20, 5)
				.SetDisplay("ATR Period", "Period for Average True Range indicator", "Indicators");

			_averagePeriod = Param(nameof(AveragePeriod), 20)
				.SetGreaterThanZero()
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 10)
				.SetDisplay("Average Period", "Period for calculating ATR average and standard deviation", "Settings");

			_deviationMultiplier = Param(nameof(DeviationMultiplier), 2m)
				.SetGreaterThanZero()
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 3m, 0.5m)
				.SetDisplay("Deviation Multiplier", "Multiplier for standard deviation", "Settings");

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_stopLossPercent = Param(nameof(StopLossPercent), 1.0m)
				.SetNotNegative()
				.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(0.5m, 2.0m, 0.5m);
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
			_prevAtr = 0;
			_avgAtr = 0;
			_stdDevAtr = 0;
			_sumAtr = 0;
			_sumSquaresAtr = 0;
			_count = 0;
			_atrValues.Clear();

			// Create ATR indicator
			var atr = new AverageTrueRange { Length = AtrPeriod };

			// Create subscription and bind indicator
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(atr, ProcessCandle)
				.Start();

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, atr);
				DrawOwnTrades(area);
			}

			StartProtection(
				new(),
				new Unit(StopLossPercent, UnitTypes.Percent),
				useMarketOrders: true
			);

			base.OnStarted(time);
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Extract ATR value
			var currentAtr = atrValue.ToDecimal();

			// Update ATR statistics
			UpdateAtrStatistics(currentAtr);

			// If we don't have enough data yet for statistics
			if (_count < AveragePeriod)
			{
				_prevAtr = currentAtr;
				return;
			}

			// For volatility mean reversion, we need to use price action to determine direction
			// We'll use simple momentum for direction (current price vs previous price)
			var priceDirection = candle.ClosePrice > candle.OpenPrice ? Sides.Buy : Sides.Sell;

			// Check for entry conditions
			if (Position == 0)
			{
				// Low volatility expecting increase - possibly prepare for a breakout
				if (currentAtr < _avgAtr - DeviationMultiplier * _stdDevAtr)
				{
					// In low volatility, follow the current short-term price direction
					if (priceDirection == Sides.Buy)
					{
						BuyMarket(Volume);
						LogInfo($"Long entry: ATR = {currentAtr}, Avg = {_avgAtr}, StdDev = {_stdDevAtr}, Price up");
					}
					else
					{
						SellMarket(Volume);
						LogInfo($"Short entry: ATR = {currentAtr}, Avg = {_avgAtr}, StdDev = {_stdDevAtr}, Price down");
					}
				}
				// High volatility expecting decrease - possibly looking for market exhaustion
				else if (currentAtr > _avgAtr + DeviationMultiplier * _stdDevAtr)
				{
					// In high volatility, consider going against the short-term trend
					// as excessive volatility often leads to reversals
					if (priceDirection == Sides.Sell)
					{
						BuyMarket(Volume);
						LogInfo($"Contrarian long entry: ATR = {currentAtr}, Avg = {_avgAtr}, StdDev = {_stdDevAtr}, High volatility");
					}
					else
					{
						SellMarket(Volume);
						LogInfo($"Contrarian short entry: ATR = {currentAtr}, Avg = {_avgAtr}, StdDev = {_stdDevAtr}, High volatility");
					}
				}
			}
			// Check for exit conditions
			else if (Position > 0) // Long position
			{
				if (currentAtr < _avgAtr && priceDirection == Sides.Sell)
				{
					ClosePosition();
					LogInfo($"Long exit: ATR = {currentAtr}, Avg = {_avgAtr}, Price down");
				}
			}
			else if (Position < 0) // Short position
			{
				if (currentAtr < _avgAtr && priceDirection == Sides.Buy)
				{
					ClosePosition();
					LogInfo($"Short exit: ATR = {currentAtr}, Avg = {_avgAtr}, Price up");
				}
			}

			// Save current ATR for next iteration
			_prevAtr = currentAtr;
		}

		private void UpdateAtrStatistics(decimal currentAtr)
		{
			// Add current value to the queue
			_atrValues.Enqueue(currentAtr);
			_sumAtr += currentAtr;
			_sumSquaresAtr += currentAtr * currentAtr;
			_count++;

			// If queue is larger than period, remove oldest value
			if (_atrValues.Count > AveragePeriod)
			{
				var oldestAtr = _atrValues.Dequeue();
				_sumAtr -= oldestAtr;
				_sumSquaresAtr -= oldestAtr * oldestAtr;
				_count--;
			}

			// Calculate average and standard deviation
			if (_count > 0)
			{
				_avgAtr = _sumAtr / _count;
				
				if (_count > 1)
				{
					var variance = (_sumSquaresAtr - (_sumAtr * _sumAtr) / _count) / (_count - 1);
					_stdDevAtr = variance <= 0 ? 0 : (decimal)Math.Sqrt((double)variance);
				}
				else
				{
					_stdDevAtr = 0;
				}
			}
		}
	}
}
