using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Donchian Channel breakout after volatility contraction.
	/// </summary>
	public class DonchianWithVolatilityContractionStrategy : Strategy
	{
		private readonly StrategyParam<int> _donchianPeriod;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<decimal> _volatilityFactor;
		private readonly StrategyParam<DataType> _candleType;

		// Indicators for maintaining the channel width
		private decimal _avgDcWidth;
		private decimal _stdDevDcWidth;
		private decimal _currentDcWidth;

		/// <summary>
		/// Donchian channel period parameter.
		/// </summary>
		public int DonchianPeriod
		{
			get => _donchianPeriod.Value;
			set => _donchianPeriod.Value = value;
		}

		/// <summary>
		/// ATR period parameter.
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		/// <summary>
		/// Volatility contraction factor parameter.
		/// </summary>
		public decimal VolatilityFactor
		{
			get => _volatilityFactor.Value;
			set => _volatilityFactor.Value = value;
		}

		/// <summary>
		/// Candle type parameter.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public DonchianWithVolatilityContractionStrategy()
		{
			_donchianPeriod = Param(nameof(DonchianPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("Donchian Period", "Period for Donchian Channel", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "Period for ATR indicator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(7, 28, 7);

			_volatilityFactor = Param(nameof(VolatilityFactor), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Volatility Factor", "Standard deviation multiplier for contraction detection", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

			// Initialize values
			_avgDcWidth = 0;
			_stdDevDcWidth = 0;
			_currentDcWidth = 0;

			// Create indicators
			var donchianHigh = new Highest { Length = DonchianPeriod };
			var donchianLow = new Lowest { Length = DonchianPeriod };
			var atr = new AverageTrueRange { Length = AtrPeriod };
			var sma = new SimpleMovingAverage { Length = DonchianPeriod };
			var standardDeviation = new StandardDeviation { Length = DonchianPeriod };

			// Subscribe to candles and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.BindEx(donchianHigh, (candle, highValue) => 
				{
					var highPrice = highValue.ToDecimal();
					
					// Process Donchian Low separately
					var lowValue = donchianLow.Process(candle);
					var lowPrice = lowValue.ToDecimal();
					
					// Process ATR
					var atrValue = atr.Process(candle);
					
					// Calculate Donchian Channel width
					_currentDcWidth = highPrice - lowPrice;
					
					// Process SMA and StdDev for the channel width
					var smaInput = new DecimalIndicatorValue(_currentDcWidth);
					var smaValue = sma.Process(smaInput);
					var stdDevInput = new DecimalIndicatorValue(_currentDcWidth);
					var stdDevValue = standardDeviation.Process(stdDevInput);
					
					_avgDcWidth = smaValue.ToDecimal();
					_stdDevDcWidth = stdDevValue.ToDecimal();
					
					// Process the strategy logic
					ProcessStrategy(candle, highPrice, lowPrice, atrValue.ToDecimal());
				})
				.Start();

			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawOwnTrades(area);
			}

			// Setup position protection
			StartProtection(
				takeProfit: new Unit(2, UnitTypes.Percent),
				stopLoss: new Unit(1, UnitTypes.Percent)
			);
		}

		private void ProcessStrategy(ICandleMessage candle, decimal donchianHigh, decimal donchianLow, decimal atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Calculate volatility threshold
			var volatilityThreshold = _avgDcWidth - VolatilityFactor * _stdDevDcWidth;
			
			// Check for volatility contraction
			var isVolatilityContracted = _currentDcWidth < volatilityThreshold;
			
			if (isVolatilityContracted)
			{
				// Breakout after volatility contraction
				if (candle.ClosePrice > donchianHigh && Position <= 0)
				{
					// Cancel any active orders before entering a new position
					CancelActiveOrders();

					// Calculate position size
					var volume = Volume + Math.Abs(Position);
					
					// Enter long position
					BuyMarket(volume);
				}
				else if (candle.ClosePrice < donchianLow && Position >= 0)
				{
					// Cancel any active orders before entering a new position
					CancelActiveOrders();

					// Calculate position size
					var volume = Volume + Math.Abs(Position);
					
					// Enter short position
					SellMarket(volume);
				}
			}
			
			// Exit logic - when price reverts to the middle of the channel
			var channelMiddle = (donchianHigh + donchianLow) / 2;
			
			if ((Position > 0 && candle.ClosePrice < channelMiddle) ||
				(Position < 0 && candle.ClosePrice > channelMiddle))
			{
				// Close position
				ClosePosition();
			}
		}
	}
}
