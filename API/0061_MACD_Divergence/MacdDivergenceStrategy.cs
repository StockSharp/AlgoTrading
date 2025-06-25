using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// MACD Divergence strategy that looks for divergences between price and MACD
	/// as potential reversal signals.
	/// </summary>
	public class MacdDivergenceStrategy : Strategy
	{
		private readonly StrategyParam<int> _fastMacdPeriod;
		private readonly StrategyParam<int> _slowMacdPeriod;
		private readonly StrategyParam<int> _signalPeriod;
		private readonly StrategyParam<int> _divergencePeriod;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLossPercent;

		private decimal? _previousPrice;
		private decimal? _previousMacd;
		private decimal? _currentPrice;
		private decimal? _currentMacd;
		private int _barsSinceDivergence;
		private bool _bullishDivergence;
		private bool _bearishDivergence;

		/// <summary>
		/// Fast EMA period for MACD calculation.
		/// </summary>
		public int FastMacdPeriod
		{
			get => _fastMacdPeriod.Value;
			set => _fastMacdPeriod.Value = value;
		}

		/// <summary>
		/// Slow EMA period for MACD calculation.
		/// </summary>
		public int SlowMacdPeriod
		{
			get => _slowMacdPeriod.Value;
			set => _slowMacdPeriod.Value = value;
		}

		/// <summary>
		/// Signal line period for MACD.
		/// </summary>
		public int SignalPeriod
		{
			get => _signalPeriod.Value;
			set => _signalPeriod.Value = value;
		}

		/// <summary>
		/// Number of bars to look back for divergence.
		/// </summary>
		public int DivergencePeriod
		{
			get => _divergencePeriod.Value;
			set => _divergencePeriod.Value = value;
		}

		/// <summary>
		/// Type of candles to use.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Stop-loss percentage from entry price.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MacdDivergenceStrategy"/>.
		/// </summary>
		public MacdDivergenceStrategy()
		{
			_fastMacdPeriod = Param(nameof(FastMacdPeriod), 12)
				.SetRange(5, 20)
				.SetDisplay("Fast MACD Period", "Fast EMA period for MACD", "Indicator Parameters")
				.SetCanOptimize(true);

			_slowMacdPeriod = Param(nameof(SlowMacdPeriod), 26)
				.SetRange(15, 40)
				.SetDisplay("Slow MACD Period", "Slow EMA period for MACD", "Indicator Parameters")
				.SetCanOptimize(true);

			_signalPeriod = Param(nameof(SignalPeriod), 9)
				.SetRange(5, 15)
				.SetDisplay("Signal Period", "Signal line period for MACD", "Indicator Parameters")
				.SetCanOptimize(true);

			_divergencePeriod = Param(nameof(DivergencePeriod), 5)
				.SetRange(3, 10)
				.SetDisplay("Divergence Period", "Number of bars to look back for divergence", "Signal Parameters")
				.SetCanOptimize(true);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
				.SetRange(0.5m, 5.0m)
				.SetDisplay("Stop Loss %", "Percentage-based stop loss from entry", "Risk Management")
				.SetCanOptimize(true);
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

			// Reset variables
			_previousPrice = null;
			_previousMacd = null;
			_currentPrice = null;
			_currentMacd = null;
			_barsSinceDivergence = 0;
			_bullishDivergence = false;
			_bearishDivergence = false;

			// Create MACD indicator
			var macd = new MovingAverageConvergenceDivergence
			{
				FastMa = new ExponentialMovingAverage { Length = FastMacdPeriod },
				SlowMa = new ExponentialMovingAverage { Length = SlowMacdPeriod },
				SignalMa = new ExponentialMovingAverage { Length = SignalPeriod }
			};

			// Create candle subscription
			var subscription = SubscribeCandles(CandleType);

			// Bind MACD to candles
			subscription
				.BindEx(macd, ProcessCandle)
				.Start();

			// Enable position protection
			StartProtection(
				new Unit(0, UnitTypes.Absolute), // No take profit (managed by signal cross)
				new Unit(StopLossPercent, UnitTypes.Percent), // Stop loss at defined percentage
				false // No trailing stop
			);

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, macd);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			try
			{
				// Extract MACD values - be careful with the order of indexes
				var macdLine = macdValue.GetValue<decimal>(0); // Main MACD line
				var signalLine = macdValue.GetValue<decimal>(1); // Signal line
				var histogram = macdValue.GetValue<decimal>(2); // Histogram (MACD - Signal)

				// Store previous values before updating
				if (_currentPrice.HasValue && _currentMacd.HasValue)
				{
					_previousPrice = _currentPrice;
					_previousMacd = _currentMacd;
				}

				// Update current values
				_currentPrice = candle.ClosePrice;
				_currentMacd = macdLine;

				LogInfo($"Candle: {candle.OpenTime}, Close: {candle.ClosePrice}, MACD: {macdLine:F4}, Signal: {signalLine:F4}");

				// Look for divergences once we have enough data
				if (_previousPrice.HasValue && _previousMacd.HasValue && _currentPrice.HasValue && _currentMacd.HasValue)
				{
					CheckForDivergences();
				}

				// Process signals based on detected divergences
				ProcessDivergenceSignals(candle, macdLine, signalLine);
			}
			catch (Exception ex)
			{
				LogError($"Error processing MACD values: {ex.Message}");
			}
		}

		private void CheckForDivergences()
		{
			// Check for bullish divergence (lower price lows but higher MACD lows)
			if (_currentPrice < _previousPrice && _currentMacd > _previousMacd)
			{
				_bullishDivergence = true;
				_bearishDivergence = false;
				_barsSinceDivergence = 0;
				LogInfo($"Bullish Divergence Detected: Price {_previousPrice}->{_currentPrice}, MACD {_previousMacd}->{_currentMacd}");
			}
			// Check for bearish divergence (higher price highs but lower MACD highs)
			else if (_currentPrice > _previousPrice && _currentMacd < _previousMacd)
			{
				_bearishDivergence = true;
				_bullishDivergence = false;
				_barsSinceDivergence = 0;
				LogInfo($"Bearish Divergence Detected: Price {_previousPrice}->{_currentPrice}, MACD {_previousMacd}->{_currentMacd}");
			}
			else
			{
				_barsSinceDivergence++;
				
				// Reset divergence signals after a certain number of bars
				if (_barsSinceDivergence > DivergencePeriod)
				{
					_bullishDivergence = false;
					_bearishDivergence = false;
				}
			}
		}

		private void ProcessDivergenceSignals(ICandleMessage candle, decimal macdLine, decimal signalLine)
		{
			// Entry signals based on detected divergences
			if (_bullishDivergence && Position <= 0 && macdLine > signalLine)
			{
				// Bullish divergence with MACD crossing above signal - Buy signal
				if (Position < 0)
				{
					// Close any existing short position
					BuyMarket(Math.Abs(Position));
					LogInfo($"Closed short position on bullish divergence");
				}

				// Open new long position
				BuyMarket(Volume);
				LogInfo($"Buy signal: Bullish MACD divergence with signal line cross");
				
				// Reset divergence detection
				_bullishDivergence = false;
			}
			else if (_bearishDivergence && Position >= 0 && macdLine < signalLine)
			{
				// Bearish divergence with MACD crossing below signal - Sell signal
				if (Position > 0)
				{
					// Close any existing long position
					SellMarket(Position);
					LogInfo($"Closed long position on bearish divergence");
				}

				// Open new short position
				SellMarket(Volume);
				LogInfo($"Sell signal: Bearish MACD divergence with signal line cross");
				
				// Reset divergence detection
				_bearishDivergence = false;
			}
			
			// Exit signals based on MACD crossing the signal line
			else if (Position > 0 && macdLine < signalLine)
			{
				// Exit long position when MACD crosses below signal
				SellMarket(Position);
				LogInfo($"Exit long: MACD crossed below signal line");
			}
			else if (Position < 0 && macdLine > signalLine)
			{
				// Exit short position when MACD crosses above signal
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exit short: MACD crossed above signal line");
			}
		}
	}
}