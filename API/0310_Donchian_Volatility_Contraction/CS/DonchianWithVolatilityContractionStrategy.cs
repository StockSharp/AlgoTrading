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

\t\t/// <inheritdoc />
\t\tprotected override void OnReseted()
\t\t{
\t\t\tbase.OnReseted();

\t\t\t_avgDcWidth = default;
\t\t\t_stdDevDcWidth = default;
\t\t\t_currentDcWidth = default;
\t\t}

\t\tprotected override void OnStarted(DateTimeOffset time)
\t\t{
\t\t\tbase.OnStarted(time);

\t\t\t// Create indicators
\t\t\tvar donchianHigh = new Highest { Length = DonchianPeriod };
\t\t\tvar donchianLow = new Lowest { Length = DonchianPeriod };
\t\t\tvar atr = new AverageTrueRange { Length = AtrPeriod };
\t\t\tvar sma = new SimpleMovingAverage { Length = DonchianPeriod };
\t\t\tvar standardDeviation = new StandardDeviation { Length = DonchianPeriod };

\t\t\t// Subscribe to candles and bind indicators
\t\t\tvar subscription = SubscribeCandles(CandleType);

\t\t\tsubscription
\t\t\t\t.BindEx(donchianHigh, (candle, highValue) =>
\t\t\t\t{
\t\t\t\t\tvar highPrice = highValue.ToDecimal();

\t\t\t\t\t// Process Donchian Low separately
\t\t\t\t\tvar lowValue = donchianLow.Process(candle);
\t\t\t\t\tvar lowPrice = lowValue.ToDecimal();

\t\t\t\t\t// Process ATR
\t\t\t\t\tvar atrValue = atr.Process(candle);

\t\t\t\t\t// Calculate Donchian Channel width
\t\t\t\t\t_currentDcWidth = highPrice - lowPrice;

\t\t\t\t\t// Process SMA and StdDev for the channel width
\t\t\t\t\tvar smaValue = sma.Process(_currentDcWidth, candle.ServerTime, candle.State == CandleStates.Finished);
\t\t\t\t\tvar stdDevValue = standardDeviation.Process(_currentDcWidth, candle.ServerTime, candle.State == CandleStates.Finished);

\t\t\t\t\t_avgDcWidth = smaValue.ToDecimal();
\t\t\t\t\t_stdDevDcWidth = stdDevValue.ToDecimal();

\t\t\t\t\t// Process the strategy logic
\t\t\t\t\tProcessStrategy(candle, highPrice, lowPrice, atrValue.ToDecimal());
\t\t\t\t})
\t\t\t\t.Start();

\t\t\t// Setup chart if available
\t\t\tvar area = CreateChartArea();
\t\t\tif (area != null)
\t\t\t{
\t\t\t\tDrawCandles(area, subscription);
\t\t\t\tDrawOwnTrades(area);
\t\t\t}

\t\t\t// Setup position protection
\t\t\tStartProtection(
\t\t\t\ttakeProfit: new Unit(2, UnitTypes.Percent),
\t\t\t\tstopLoss: new Unit(1, UnitTypes.Percent)
\t\t\t);
\t\t}

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
