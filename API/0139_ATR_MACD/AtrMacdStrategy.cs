using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that uses ATR (Average True Range) for volatility detection
	/// and MACD for trend direction confirmation.
	/// Enters positions when volatility increases and MACD confirms trend direction.
	/// </summary>
	public class AtrMacdStrategy : Strategy
	{
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<int> _atrAvgPeriod;
		private readonly StrategyParam<decimal> _atrMultiplier;
		private readonly StrategyParam<int> _macdFast;
		private readonly StrategyParam<int> _macdSlow;
		private readonly StrategyParam<int> _macdSignal;
		private readonly StrategyParam<decimal> _stopLossAtr;
		private readonly StrategyParam<DataType> _candleType;

		private decimal _prevAtrAvg;

		/// <summary>
		/// ATR indicator period.
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		/// <summary>
		/// Period for averaging ATR values.
		/// </summary>
		public int AtrAvgPeriod
		{
			get => _atrAvgPeriod.Value;
			set => _atrAvgPeriod.Value = value;
		}

		/// <summary>
		/// Multiplier for ATR comparison.
		/// </summary>
		public decimal AtrMultiplier
		{
			get => _atrMultiplier.Value;
			set => _atrMultiplier.Value = value;
		}

		/// <summary>
		/// MACD fast period.
		/// </summary>
		public int MacdFast
		{
			get => _macdFast.Value;
			set => _macdFast.Value = value;
		}

		/// <summary>
		/// MACD slow period.
		/// </summary>
		public int MacdSlow
		{
			get => _macdSlow.Value;
			set => _macdSlow.Value = value;
		}

		/// <summary>
		/// MACD signal period.
		/// </summary>
		public int MacdSignal
		{
			get => _macdSignal.Value;
			set => _macdSignal.Value = value;
		}

		/// <summary>
		/// Stop loss in ATR multiples.
		/// </summary>
		public decimal StopLossAtr
		{
			get => _stopLossAtr.Value;
			set => _stopLossAtr.Value = value;
		}

		/// <summary>
		/// Candle type for strategy calculation.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Strategy constructor.
		/// </summary>
		public AtrMacdStrategy()
		{
			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "Period of the ATR indicator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(7, 21, 7);

			_atrAvgPeriod = Param(nameof(AtrAvgPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("ATR Avg Period", "Period for averaging ATR values", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_atrMultiplier = Param(nameof(AtrMultiplier), 1.0m)
				.SetGreaterThanZero()
				.SetDisplay("ATR Multiplier", "Multiplier for ATR comparison", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(0.5m, 2.0m, 0.5m);

			_macdFast = Param(nameof(MacdFast), 12)
				.SetGreaterThanZero()
				.SetDisplay("MACD Fast", "Fast period of the MACD indicator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(8, 16, 4);

			_macdSlow = Param(nameof(MacdSlow), 26)
				.SetGreaterThanZero()
				.SetDisplay("MACD Slow", "Slow period of the MACD indicator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(20, 32, 4);

			_macdSignal = Param(nameof(MacdSignal), 9)
				.SetGreaterThanZero()
				.SetDisplay("MACD Signal", "Signal period of the MACD indicator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(5, 13, 4);

			_stopLossAtr = Param(nameof(StopLossAtr), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss ATR", "Stop loss as ATR multiplier", "Risk Management")
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

			// Initialize variables
			_prevAtrAvg = 0;

			// Create indicators
			var atr = new AverageTrueRange
			{
				Length = AtrPeriod
			};

			var atrAvg = new SimpleMovingAverage
			{
				Length = AtrAvgPeriod
			};

			var macd = new MovingAverageConvergenceDivergenceSignal
			{
				Macd =
				{
					ShortMa = { Length = MacdFast },
					LongMa = { Length = MacdSlow },
				},
				SignalMa = { Length = MacdSignal }
			};
			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);

			subscription
				.BindEx(atr, atrValue => ProcessAtr(atrValue, atrAvg))
				.Start();

			subscription
				.Bind(macd, ProcessMacd)
				.Start();

			// Setup position protection
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit
				stopLoss: new Unit(StopLossAtr, UnitTypes.Absolute) // Stop loss as ATR multiplier
			);

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, atr);
				DrawIndicator(area, macd);
				DrawOwnTrades(area);
			}
		}

		/// <summary>
		/// Process ATR indicator values.
		/// </summary>
		private void ProcessAtr(IIndicatorValue atrValue, SimpleMovingAverage atrAvg)
		{
			if (!atrValue.IsFinal)
				return;

			// Process ATR through averaging indicator
			var avgValue = atrAvg.Process(atrValue);
			if (!avgValue.IsFinal)
				return;

			// Store current ATR average value
			var currentAtrAvg = avgValue.ToDecimal();
			_prevAtrAvg = currentAtrAvg;
		}

		/// <summary>
		/// Process MACD indicator values.
		/// </summary>
		private void ProcessMacd(ICandleMessage candle, decimal macdValue, decimal signalValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading() || _prevAtrAvg == 0)
				return;

			// Get current ATR value
			var currentAtr = candle.GetTrueRange();
			
			// Check if volatility is increasing
			var isVolatilityIncreasing = currentAtr > _prevAtrAvg * AtrMultiplier;
			
			if (isVolatilityIncreasing)
			{
				// Long entry: MACD above Signal in rising volatility
				if (macdValue > signalValue && Position <= 0)
				{
					var volume = Volume + Math.Abs(Position);
					BuyMarket(volume);
				}
				// Short entry: MACD below Signal in rising volatility
				else if (macdValue < signalValue && Position >= 0)
				{
					var volume = Volume + Math.Abs(Position);
					SellMarket(volume);
				}
			}

			// Exit conditions based on MACD crossovers
			if (Position > 0 && macdValue < signalValue)
			{
				SellMarket(Math.Abs(Position));
			}
			else if (Position < 0 && macdValue > signalValue)
			{
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}