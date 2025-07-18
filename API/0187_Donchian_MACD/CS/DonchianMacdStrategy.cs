using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy combining Donchian Channel breakout with MACD trend confirmation.
	/// </summary>
	public class DonchianMacdStrategy : Strategy
	{
		private readonly StrategyParam<int> _donchianPeriod;
		private readonly StrategyParam<int> _macdFast;
		private readonly StrategyParam<int> _macdSlow;
		private readonly StrategyParam<int> _macdSignal;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		private DonchianChannels _donchian;
		private MovingAverageConvergenceDivergenceSignal _macd;
		
		private decimal? _previousHighest;
		private decimal? _previousLowest;
		private decimal? _previousMacd;
		private decimal? _previousSignal;
		private decimal? _entryPrice;

		/// <summary>
		/// Donchian channel period.
		/// </summary>
		public int DonchianPeriod
		{
			get => _donchianPeriod.Value;
			set => _donchianPeriod.Value = value;
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
		/// Stop loss percentage.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
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
		/// Initializes a new instance of the <see cref="DonchianMacdStrategy"/>.
		/// </summary>
		public DonchianMacdStrategy()
		{
			_donchianPeriod = Param(nameof(DonchianPeriod), 20)
				.SetRange(5, 50)
				.SetCanOptimize(true)
				.SetDisplay("Donchian Period", "Channel lookback period", "Indicators");

			_macdFast = Param(nameof(MacdFast), 12)
				.SetRange(8, 20)
				.SetCanOptimize(true)
				.SetDisplay("MACD Fast Period", "Fast EMA period for MACD", "Indicators");

			_macdSlow = Param(nameof(MacdSlow), 26)
				.SetRange(20, 40)
				.SetCanOptimize(true)
				.SetDisplay("MACD Slow Period", "Slow EMA period for MACD", "Indicators");

			_macdSignal = Param(nameof(MacdSignal), 9)
				.SetRange(5, 15)
				.SetCanOptimize(true)
				.SetDisplay("MACD Signal Period", "Signal line period for MACD", "Indicators");

			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetRange(1m, 5m)
				.SetCanOptimize(true)
				.SetDisplay("Stop-Loss %", "Stop-loss percentage from entry price", "Risk Management");

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

			// Initialize indicators
			_donchian = new DonchianChannels
			{
				Length = DonchianPeriod
			};

			_macd = new MovingAverageConvergenceDivergenceSignal
			{
				Macd =
				{
					ShortMa = { Length = MacdFast },
					LongMa = { Length = MacdSlow },
				},
				SignalMa = { Length = MacdSignal }
			};

			// Reset state variables
			_previousHighest = 0;
			_previousLowest = decimal.MaxValue;
			_previousMacd = 0;
			_previousSignal = 0;
			_entryPrice = null;

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.BindEx(_donchian, _macd, ProcessCandle)
				.Start();

			// Setup position protection
			StartProtection(new Unit(0, UnitTypes.Absolute), new Unit(StopLossPercent, UnitTypes.Percent));

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _donchian);
				DrawIndicator(area, _macd);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue, IIndicatorValue macdValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Wait until strategy and indicators are ready
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
			var signalValue = macdTyped.Signal;
			var macdDec = macdTyped.Macd;

			// Check for breakouts with MACD trend confirmation
			// Long entry: Price breaks above Donchian high and MACD > Signal
			if (candle.ClosePrice > _previousHighest && Position <= 0 && macdDec > signalValue)
			{
				// Cancel existing orders before entering new position
				CancelActiveOrders();
				
				// Enter long position
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				_entryPrice = candle.ClosePrice;
				
				LogInfo($"Long entry signal: Price {candle.ClosePrice} broke above Donchian high {_previousHighest} with MACD confirmation");
			}
			// Short entry: Price breaks below Donchian low and MACD < Signal
			else if (candle.ClosePrice < _previousLowest && Position >= 0 && macdDec < signalValue)
			{
				// Cancel existing orders before entering new position
				CancelActiveOrders();
				
				// Enter short position
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				_entryPrice = candle.ClosePrice;
				
				LogInfo($"Short entry signal: Price {candle.ClosePrice} broke below Donchian low {_previousLowest} with MACD confirmation");
			}
			// MACD trend reversal exit
			else if ((Position > 0 && macdDec < signalValue && _previousMacd > _previousSignal) ||
					 (Position < 0 && macdDec > signalValue && _previousMacd < _previousSignal))
			{
				// Close position on MACD signal reversal
				ClosePosition();
				_entryPrice = null;
				
				LogInfo($"Exit signal: MACD trend reversal. MACD: {macdDec}, Signal: {signalValue}");
			}

			var donchianTyped = (DonchianChannelsValue)donchianValue;

			// Update previous values for next candle
			_previousHighest = donchianTyped.UpperBand;
			_previousLowest = donchianTyped.LowerBand;
			_previousMacd = macdDec;
			_previousSignal = signalValue;
		}
	}
}