using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using System;
using System.Collections.Generic;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Keltner Channels and MACD.
	/// Enters long when price breaks above upper Keltner Channel with MACD > Signal.
	/// Enters short when price breaks below lower Keltner Channel with MACD < Signal.
	/// Exits when MACD crosses its signal line in the opposite direction.
	/// </summary>
	public class KeltnerMacdStrategy : Strategy
	{
		private readonly StrategyParam<int> _emaPeriod;
		private readonly StrategyParam<decimal> _multiplier;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<int> _macdFastPeriod;
		private readonly StrategyParam<int> _macdSlowPeriod;
		private readonly StrategyParam<int> _macdSignalPeriod;
		private readonly StrategyParam<decimal> _atrMultiplier;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLossPercent;

		private ExponentialMovingAverage _ema;
		private AverageTrueRange _atr;
		private MovingAverageConvergenceDivergenceSignal _macd;
		
		private decimal _prevMacd;
		private decimal _prevSignal;

		/// <summary>
		/// EMA period for Keltner Channel middle line.
		/// </summary>
		public int EmaPeriod
		{
			get => _emaPeriod.Value;
			set => _emaPeriod.Value = value;
		}

		/// <summary>
		/// ATR multiplier for Keltner Channel bands.
		/// </summary>
		public decimal Multiplier
		{
			get => _multiplier.Value;
			set => _multiplier.Value = value;
		}

		/// <summary>
		/// ATR period for Keltner Channel bands.
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		/// <summary>
		/// MACD fast EMA period.
		/// </summary>
		public int MacdFastPeriod
		{
			get => _macdFastPeriod.Value;
			set => _macdFastPeriod.Value = value;
		}

		/// <summary>
		/// MACD slow EMA period.
		/// </summary>
		public int MacdSlowPeriod
		{
			get => _macdSlowPeriod.Value;
			set => _macdSlowPeriod.Value = value;
		}

		/// <summary>
		/// MACD signal line period.
		/// </summary>
		public int MacdSignalPeriod
		{
			get => _macdSignalPeriod.Value;
			set => _macdSignalPeriod.Value = value;
		}

		/// <summary>
		/// ATR multiplier for stop loss calculation.
		/// </summary>
		public decimal AtrMultiplier
		{
			get => _atrMultiplier.Value;
			set => _atrMultiplier.Value = value;
		}

		/// <summary>
		/// Candle type for strategy.
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
		/// Initializes a new instance of the <see cref="KeltnerMacdStrategy"/>.
		/// </summary>
		public KeltnerMacdStrategy()
		{
			_emaPeriod = Param(nameof(EmaPeriod), 20)
				.SetDisplay("EMA Period", "Period for EMA calculation in Keltner Channel", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_multiplier = Param(nameof(Multiplier), 2m)
				.SetDisplay("ATR Multiplier", "ATR multiplier for Keltner Channel bands", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 3m, 0.5m);

			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetDisplay("ATR Period", "Period for ATR calculation in Keltner Channel", "Indicators");

			_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
				.SetDisplay("MACD Fast Period", "Fast EMA period for MACD calculation", "Indicators")
				.SetCanOptimize(true);

			_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
				.SetDisplay("MACD Slow Period", "Slow EMA period for MACD calculation", "Indicators")
				.SetCanOptimize(true);

			_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
				.SetDisplay("MACD Signal Period", "Signal line period for MACD calculation", "Indicators")
				.SetCanOptimize(true);

			_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
				.SetDisplay("Stop Loss ATR Multiplier", "ATR multiplier for stop loss calculation", "Risk Management");

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
				.SetDisplay("Candle Type", "Timeframe of data for strategy", "General");

			_stopLossPercent = Param(nameof(StopLossPercent), 1.0m)
				.SetNotNegative()
				.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(0.5m, 2.0m, 0.5m);
		}

		/// <inheritdoc />
		public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
		{
			return [(Security, CandleType)];
		}

		/// <inheritdoc />
		protected override void OnReseted()
		{
			base.OnReseted();

			_ema = default;
			_atr = default;
			_macd = default;
			_prevMacd = default;
			_prevSignal = default;
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create indicators
			_ema = new ExponentialMovingAverage { Length = EmaPeriod };
			_atr = new AverageTrueRange { Length = AtrPeriod };

			_macd = new MovingAverageConvergenceDivergenceSignal
			{
				Macd =
				{
					ShortMa = { Length = MacdFastPeriod },
					LongMa = { Length = MacdSlowPeriod },
				},
				SignalMa = { Length = MacdSignalPeriod }
			};

			// Create subscription
			var subscription = SubscribeCandles(CandleType);

			// Process candles with indicators
			subscription
				.BindEx(_ema, _atr, _macd, ProcessCandle)
				.Start();

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				
				// MACD in separate area
				var macdArea = CreateChartArea();
				if (macdArea != null)
				{
					DrawIndicator(macdArea, _macd);
				}
				
				DrawOwnTrades(area);
			}

			StartProtection(
				new Unit(0), // No take profit - use MACD cross for exit
				new Unit(StopLossPercent, UnitTypes.Absolute)
			);
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue atrValue, IIndicatorValue macdValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			var ema = emaValue.ToDecimal();
			var atr = atrValue.ToDecimal();

			// Calculate Keltner Channels
			var upperBand = ema + Multiplier * atr;
			var lowerBand = ema - Multiplier * atr;

			var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

			// Process MACD separately to get MACD and Signal values
			if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
			{
				return;
			}

			// Detect MACD crosses
			bool macdCrossedAboveSignal = _prevMacd <= _prevSignal && macd > signal;
			bool macdCrossedBelowSignal = _prevMacd >= _prevSignal && macd < signal;

			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading())
			{
				// Store current values for next candle
				_prevMacd = macd;
				_prevSignal = signal;
				return;
			}

			// Trading logic
			if (candle.ClosePrice > upperBand && macd > signal && Position <= 0)
			{
				// Price breaks above upper Keltner Channel with bullish MACD - go long
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (candle.ClosePrice < lowerBand && macd < signal && Position >= 0)
			{
				// Price breaks below lower Keltner Channel with bearish MACD - go short
				SellMarket(Volume + Math.Abs(Position));
			}
			
			// Exit logic based on MACD crosses
			if (Position > 0 && macdCrossedBelowSignal)
			{
				// Exit long position when MACD crosses below Signal
				ClosePosition();
			}
			else if (Position < 0 && macdCrossedAboveSignal)
			{
				// Exit short position when MACD crosses above Signal
				ClosePosition();
			}

			// Store current values for next candle
			_prevMacd = macd;
			_prevSignal = signal;
		}
	}
}