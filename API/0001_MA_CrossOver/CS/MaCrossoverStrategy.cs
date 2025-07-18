using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Moving average crossover strategy.
	/// Enters long when fast MA crosses above slow MA.
	/// Enters short when fast MA crosses below slow MA.
	/// Implements stop-loss as a percentage of entry price.
	/// </summary>
	public class MaCrossoverStrategy : Strategy
	{
		private readonly StrategyParam<int> _fastLength;
		private readonly StrategyParam<int> _slowLength;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		private decimal _entryPrice;
		private bool _isLongPosition;

		/// <summary>
		/// Fast MA period length.
		/// </summary>
		public int FastLength
		{
			get => _fastLength.Value;
			set => _fastLength.Value = value;
		}

		/// <summary>
		/// Slow MA period length.
		/// </summary>
		public int SlowLength
		{
			get => _slowLength.Value;
			set => _slowLength.Value = value;
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
		/// The type of candles to use for strategy calculation.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public MaCrossoverStrategy()
		{
			_fastLength = Param(nameof(FastLength), 10)
				.SetGreaterThanZero()
				.SetDisplay("Fast MA Length", "Period of the fast moving average", "MA Settings")
				.SetCanOptimize(true)
				.SetOptimize(5, 20, 5);

			_slowLength = Param(nameof(SlowLength), 50)
				.SetGreaterThanZero()
				.SetDisplay("Slow MA Length", "Period of the slow moving average", "MA Settings")
				.SetCanOptimize(true)
				.SetOptimize(20, 100, 10);

			_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 5.0m, 1.0m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
			base.OnStarted(time);

			// Initialize variables
			_entryPrice = 0;
			_isLongPosition = false;

			// Create indicators
			var fastMa = new SMA { Length = FastLength };
			var slowMa = new SMA { Length = SlowLength };

			// Create and setup subscription for candles
			var subscription = SubscribeCandles(CandleType);
			
			// Previous values for crossover detection
			var previousFastValue = 0m;
			var previousSlowValue = 0m;
			var wasFastLessThanSlow = false;
			var isInitialized = false;
			
			subscription
				.Bind(fastMa, slowMa, (candle, fastValue, slowValue) =>
				{
					// Skip unfinished candles
					if (candle.State != CandleStates.Finished)
						return;

					// Check if strategy is ready to trade
					if (!IsFormedAndOnlineAndAllowTrading())
						return;
						
					// Initialize on first complete values
					if (!isInitialized && fastMa.IsFormed && slowMa.IsFormed)
					{
						previousFastValue = fastValue;
						previousSlowValue = slowValue;
						wasFastLessThanSlow = fastValue < slowValue;
						isInitialized = true;
						LogInfo($"Strategy initialized. Fast MA: {fastValue}, Slow MA: {slowValue}");
						return;
					}
					
					if (!isInitialized)
						return;

					// Current crossover state
					var isFastLessThanSlow = fastValue < slowValue;
					
					LogInfo($"Candle: {candle.OpenTime}, Close: {candle.ClosePrice}, Fast MA: {fastValue}, Slow MA: {slowValue}");
					
					// Check for crossovers
					if (wasFastLessThanSlow != isFastLessThanSlow)
					{
						// Crossover happened
						if (!isFastLessThanSlow) // Fast MA crossed above Slow MA
						{
							// Buy signal
							if (Position <= 0)
							{
								_entryPrice = candle.ClosePrice;
								_isLongPosition = true;
								BuyMarket(Volume + Math.Abs(Position));
								LogInfo($"Long entry: Fast MA {fastValue} crossed above Slow MA {slowValue}");
							}
						}
						else // Fast MA crossed below Slow MA
						{
							// Sell signal
							if (Position >= 0)
							{
								_entryPrice = candle.ClosePrice;
								_isLongPosition = false;
								SellMarket(Volume + Math.Abs(Position));
								LogInfo($"Short entry: Fast MA {fastValue} crossed below Slow MA {slowValue}");
							}
						}
						
						// Update the crossover state
						wasFastLessThanSlow = isFastLessThanSlow;
					}
					
					// Check stop-loss conditions
					if (Position != 0 && _entryPrice != 0)
					{
						CheckStopLoss(candle.ClosePrice);
					}
					
					// Update previous values
					previousFastValue = fastValue;
					previousSlowValue = slowValue;
				})
				.Start();

			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, fastMa);
				DrawIndicator(area, slowMa);
				DrawOwnTrades(area);
			}
		}

		private void CheckStopLoss(decimal currentPrice)
		{
			if (_entryPrice == 0)
				return;

			var stopLossThreshold = _stopLossPercent.Value / 100.0m;
			
			if (_isLongPosition && Position > 0)
			{
				// For long positions, exit if price falls below entry price - stop percentage
				var stopPrice = _entryPrice * (1.0m - stopLossThreshold);
				if (currentPrice <= stopPrice)
				{
					SellMarket(Math.Abs(Position));
					LogInfo($"Long stop-loss triggered at {currentPrice}. Entry was {_entryPrice}, Stop level: {stopPrice}");
				}
			}
			else if (!_isLongPosition && Position < 0)
			{
				// For short positions, exit if price rises above entry price + stop percentage
				var stopPrice = _entryPrice * (1.0m + stopLossThreshold);
				if (currentPrice >= stopPrice)
				{
					BuyMarket(Math.Abs(Position));
					LogInfo($"Short stop-loss triggered at {currentPrice}. Entry was {_entryPrice}, Stop level: {stopPrice}");
				}
			}
		}
	}
}
