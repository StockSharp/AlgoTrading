using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

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

		private ExponentialMovingAverage _ema;
		private AverageTrueRange _atr;
		private MovingAverageConvergenceDivergence _macd;
		
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
		/// Initializes a new instance of the <see cref="KeltnerMacdStrategy"/>.
		/// </summary>
		public KeltnerMacdStrategy()
		{
			_emaPeriod = Param(nameof(EmaPeriod), 20)
				.SetDisplayName("EMA Period")
				.SetDescription("Period for EMA calculation in Keltner Channel")
				.SetCategories("Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_multiplier = Param(nameof(Multiplier), 2m)
				.SetDisplayName("ATR Multiplier")
				.SetDescription("ATR multiplier for Keltner Channel bands")
				.SetCategories("Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.5m, 3m, 0.5m);

			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetDisplayName("ATR Period")
				.SetDescription("Period for ATR calculation in Keltner Channel")
				.SetCategories("Indicators");

			_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
				.SetDisplayName("MACD Fast Period")
				.SetDescription("Fast EMA period for MACD calculation")
				.SetCategories("Indicators")
				.SetCanOptimize(true);

			_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
				.SetDisplayName("MACD Slow Period")
				.SetDescription("Slow EMA period for MACD calculation")
				.SetCategories("Indicators")
				.SetCanOptimize(true);

			_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
				.SetDisplayName("MACD Signal Period")
				.SetDescription("Signal line period for MACD calculation")
				.SetCategories("Indicators")
				.SetCanOptimize(true);

			_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
				.SetDisplayName("Stop Loss ATR Multiplier")
				.SetDescription("ATR multiplier for stop loss calculation")
				.SetCategories("Risk Management");

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
				.SetDisplayName("Candle Type")
				.SetDescription("Timeframe of data for strategy")
				.SetCategories("General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
		{
			return [(Security, CandleType)];
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create indicators
			_ema = new ExponentialMovingAverage { Length = EmaPeriod };
			_atr = new AverageTrueRange { Length = AtrPeriod };
			_macd = new MovingAverageConvergenceDivergence
			{
				FastMa = new ExponentialMovingAverage { Length = MacdFastPeriod },
				SlowMa = new ExponentialMovingAverage { Length = MacdSlowPeriod },
				SignalMa = new ExponentialMovingAverage { Length = MacdSignalPeriod }
			};

			// Initialize variables
			_prevMacd = 0;
			_prevSignal = 0;

			// Create subscription
			var subscription = SubscribeCandles(CandleType);

			// Process candles with indicators
			subscription
				.Bind(_ema, _atr, (candle, ema, atr) => 
				{
					// Calculate Keltner Channels
					var upperBand = ema + Multiplier * atr;
					var lowerBand = ema - Multiplier * atr;
					
					// Process MACD separately to get MACD and Signal values
					var macdValue = _macd.Process(candle);
					var macd = macdValue[MovingAverageConvergenceDivergence.MacdLine];
					var signal = macdValue[MovingAverageConvergenceDivergence.SignalLine];
					
					ProcessCandle(candle, ema, upperBand, lowerBand, macd, signal, atr);
				})
				.Start();

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				
				// Create custom indicators for Keltner Channels visualization
				var upperLine = area.CreateIndicator<Highest>("Keltner Upper");
				var midLine = area.CreateIndicator<Lowest>("Keltner Middle");
				var lowerLine = area.CreateIndicator<Lowest>("Keltner Lower");
				
				// MACD in separate area
				var macdArea = CreateChartArea();
				if (macdArea != null)
				{
					DrawIndicator(macdArea, _macd);
				}
				
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal ema, decimal upperBand, decimal lowerBand, 
								 decimal macd, decimal signal, decimal atr)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

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

			// Set dynamic stop loss based on ATR
			if (Position != 0)
			{
				StartProtection(
					new Unit(0), // No take profit - use MACD cross for exit
					new Unit(AtrMultiplier * atr, UnitTypes.Absolute)
				);
			}

			// Store current values for next candle
			_prevMacd = macd;
			_prevSignal = signal;
		}
	}
}