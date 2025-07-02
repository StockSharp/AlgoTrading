using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Elder's Impulse System.
	/// </summary>
	public class ElderImpulseStrategy : Strategy
	{
		private readonly StrategyParam<int> _emaPeriod;
		private readonly StrategyParam<int> _macdFastPeriod;
		private readonly StrategyParam<int> _macdSlowPeriod;
		private readonly StrategyParam<int> _macdSignalPeriod;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// EMA period.
		/// </summary>
		public int EmaPeriod
		{
			get => _emaPeriod.Value;
			set => _emaPeriod.Value = value;
		}

		/// <summary>
		/// MACD fast period.
		/// </summary>
		public int MacdFastPeriod
		{
			get => _macdFastPeriod.Value;
			set => _macdFastPeriod.Value = value;
		}

		/// <summary>
		/// MACD slow period.
		/// </summary>
		public int MacdSlowPeriod
		{
			get => _macdSlowPeriod.Value;
			set => _macdSlowPeriod.Value = value;
		}

		/// <summary>
		/// MACD signal period.
		/// </summary>
		public int MacdSignalPeriod
		{
			get => _macdSignalPeriod.Value;
			set => _macdSignalPeriod.Value = value;
		}

		/// <summary>
		/// Stop loss percentage.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Candle type.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		// Cache for EMA direction
		private decimal _previousEma;
		private bool _isFirstCandle = true;

		/// <summary>
		/// Initializes a new instance of the <see cref="ElderImpulseStrategy"/>.
		/// </summary>
		public ElderImpulseStrategy()
		{
			_emaPeriod = Param(nameof(EmaPeriod), 13)
				.SetRange(8, 21)
				.SetDisplay("EMA Period", "Period for EMA calculation", "Indicators")
				.SetCanOptimize(true);

			_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
				.SetRange(8, 20)
				.SetDisplay("MACD Fast Period", "Fast period for MACD", "Indicators")
				.SetCanOptimize(true);

			_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
				.SetRange(20, 40)
				.SetDisplay("MACD Slow Period", "Slow period for MACD", "Indicators")
				.SetCanOptimize(true);

			_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
				.SetRange(5, 15)
				.SetDisplay("MACD Signal Period", "Signal period for MACD", "Indicators")
				.SetCanOptimize(true);

			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetRange(0.5m, 5m)
				.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
				.SetCanOptimize(true);

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

			// Reset state variables
			_previousEma = 0;
			_isFirstCandle = true;

			// Create indicators
			var ema = new ExponentialMovingAverage { Length = EmaPeriod };
			var macd = new MovingAverageConvergenceDivergenceSignal
			{
				Macd =
				{
					ShortMa = { Length = MacdFastPeriod },
					LongMa = { Length = MacdSlowPeriod },
				},
				SignalMa = { Length = MacdSignalPeriod }
			};

			// Subscribe to candles
			var subscription = SubscribeCandles(CandleType);
			
			// Process candles with both indicators
			subscription
				.BindEx(ema, macd, ProcessCandle)
				.Start();

			// Enable position protection
			StartProtection(
				takeProfit: null,
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
				useMarketOrders: true
			);

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, ema);
				DrawIndicator(area, macd);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue macdValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			if (_isFirstCandle)
			{
				_previousEma = emaValue;
				_isFirstCandle = false;
				return;
			}

			// Determine EMA direction
			bool isEmaRising = emaValue > _previousEma;

			// Get MACD histogram value (MACD - Signal)
			decimal macdHistogram = macdValue.Macd - macdValue.Signal;

			// Elder Impulse System:
			// 1. Green bar: EMA rising and MACD histogram rising
			// 2. Red bar: EMA falling and MACD histogram falling
			// 3. Blue bar: EMA and MACD histogram in opposite directions

			bool isBullish = isEmaRising && macdHistogram > 0;
			bool isBearish = !isEmaRising && macdHistogram < 0;

			// Entry logic
			if (isBullish && Position <= 0)
			{
				// Buy signal: EMA rising and MACD histogram positive
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				LogInfo($"Buy signal: EMA rising, MACD histogram positive. EMA = {emaValue:F2}, MACD Histogram = {macdHistogram:F4}");
			}
			else if (isBearish && Position >= 0)
			{
				// Sell signal: EMA falling and MACD histogram negative
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				LogInfo($"Sell signal: EMA falling, MACD histogram negative. EMA = {emaValue:F2}, MACD Histogram = {macdHistogram:F4}");
			}

			// Exit logic
			if (Position > 0 && macdHistogram < 0)
			{
				// Exit long position when MACD histogram turns negative
				SellMarket(Math.Abs(Position));
				LogInfo($"Exiting long position: MACD histogram turned negative. MACD Histogram = {macdHistogram:F4}");
			}
			else if (Position < 0 && macdHistogram > 0)
			{
				// Exit short position when MACD histogram turns positive
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exiting short position: MACD histogram turned positive. MACD Histogram = {macdHistogram:F4}");
			}

			// Store current EMA value for next comparison
			_previousEma = emaValue;
		}
	}
}