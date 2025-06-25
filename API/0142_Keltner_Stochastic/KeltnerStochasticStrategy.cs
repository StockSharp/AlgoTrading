using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that combines Keltner Channels and Stochastic Oscillator.
	/// Enters positions when price reaches Keltner Channel boundaries
	/// and Stochastic confirms oversold/overbought conditions.
	/// </summary>
	public class KeltnerStochasticStrategy : Strategy
	{
		private readonly StrategyParam<int> _emaPeriod;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<decimal> _keltnerMultiplier;
		private readonly StrategyParam<int> _stochPeriod;
		private readonly StrategyParam<int> _stochK;
		private readonly StrategyParam<int> _stochD;
		private readonly StrategyParam<decimal> _stochOversold;
		private readonly StrategyParam<decimal> _stochOverbought;
		private readonly StrategyParam<decimal> _stopLossAtr;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// EMA period for Keltner Channel.
		/// </summary>
		public int EmaPeriod
		{
			get => _emaPeriod.Value;
			set => _emaPeriod.Value = value;
		}

		/// <summary>
		/// ATR period for Keltner Channel.
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		/// <summary>
		/// Keltner Channel multiplier.
		/// </summary>
		public decimal KeltnerMultiplier
		{
			get => _keltnerMultiplier.Value;
			set => _keltnerMultiplier.Value = value;
		}

		/// <summary>
		/// Stochastic period.
		/// </summary>
		public int StochPeriod
		{
			get => _stochPeriod.Value;
			set => _stochPeriod.Value = value;
		}

		/// <summary>
		/// Stochastic %K period.
		/// </summary>
		public int StochK
		{
			get => _stochK.Value;
			set => _stochK.Value = value;
		}

		/// <summary>
		/// Stochastic %D period.
		/// </summary>
		public int StochD
		{
			get => _stochD.Value;
			set => _stochD.Value = value;
		}

		/// <summary>
		/// Stochastic oversold level.
		/// </summary>
		public decimal StochOversold
		{
			get => _stochOversold.Value;
			set => _stochOversold.Value = value;
		}

		/// <summary>
		/// Stochastic overbought level.
		/// </summary>
		public decimal StochOverbought
		{
			get => _stochOverbought.Value;
			set => _stochOverbought.Value = value;
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

		// Variables to store current Keltner Channel values
		private decimal _emaValue;
		private decimal _atrValue;

		/// <summary>
		/// Strategy constructor.
		/// </summary>
		public KeltnerStochasticStrategy()
		{
			_emaPeriod = Param(nameof(EmaPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("EMA Period", "Period of the EMA for Keltner Channel", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "Period of the ATR for Keltner Channel", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(7, 21, 7);

			_keltnerMultiplier = Param(nameof(KeltnerMultiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Keltner Multiplier", "Multiplier for ATR in Keltner Channel", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 3.0m, 0.5m);

			_stochPeriod = Param(nameof(StochPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("Stochastic Period", "Period of the Stochastic Oscillator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(5, 20, 5);

			_stochK = Param(nameof(StochK), 3)
				.SetGreaterThanZero()
				.SetDisplay("Stochastic %K", "Smoothing of the %K line", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1, 5, 1);

			_stochD = Param(nameof(StochD), 3)
				.SetGreaterThanZero()
				.SetDisplay("Stochastic %D", "Smoothing of the %D line", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1, 5, 1);

			_stochOversold = Param(nameof(StochOversold), 20m)
				.SetNotNegative()
				.SetDisplay("Stochastic Oversold", "Level considered oversold", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10m, 30m, 5m);

			_stochOverbought = Param(nameof(StochOverbought), 80m)
				.SetNotNegative()
				.SetDisplay("Stochastic Overbought", "Level considered overbought", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(70m, 90m, 5m);

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
			_emaValue = 0;
			_atrValue = 0;

			// Create indicators
			var ema = new ExponentialMovingAverage
			{
				Length = EmaPeriod
			};

			var atr = new AverageTrueRange
			{
				Length = AtrPeriod
			};

			var stochastic = new StochasticOscillator
			{
				Length = StochPeriod,
				KPeriod = StochK,
				DPeriod = StochD
			};

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);

			subscription
				.BindEx(ema, emaValue => _emaValue = emaValue.GetValue<decimal>())
				.Start();

			subscription
				.BindEx(atr, atrValue => _atrValue = atrValue.GetValue<decimal>())
				.Start();

			subscription
				.Bind(stochastic, ProcessStochastic)
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
				
				// Create a full Keltner Channel indicator for visualization
				var keltner = new KeltnerChannel
				{
					EMA = new ExponentialMovingAverage { Length = EmaPeriod },
					ATR = new AverageTrueRange { Length = AtrPeriod },
					K = KeltnerMultiplier
				};
				
				DrawIndicator(area, keltner);
				DrawIndicator(area, stochastic);
				DrawOwnTrades(area);
			}
		}

		/// <summary>
		/// Process Stochastic indicator values.
		/// </summary>
		private void ProcessStochastic(ICandleMessage candle, decimal stochKValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading() || _emaValue == 0 || _atrValue == 0)
				return;

			// Calculate Keltner Channel bands
			var upperBand = _emaValue + (_atrValue * KeltnerMultiplier);
			var lowerBand = _emaValue - (_atrValue * KeltnerMultiplier);

			// Long entry: price below lower Keltner band and Stochastic oversold
			if (candle.ClosePrice < lowerBand && stochKValue < StochOversold && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			// Short entry: price above upper Keltner band and Stochastic overbought
			else if (candle.ClosePrice > upperBand && stochKValue > StochOverbought && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}
			// Long exit: price returns to EMA line (middle band)
			else if (Position > 0 && candle.ClosePrice > _emaValue)
			{
				SellMarket(Math.Abs(Position));
			}
			// Short exit: price returns to EMA line (middle band)
			else if (Position < 0 && candle.ClosePrice < _emaValue)
			{
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}