using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// CCI Divergence strategy that looks for divergences between price and CCI
	/// as potential reversal signals.
	/// </summary>
	public class CciDivergenceStrategy : Strategy
	{
		private readonly StrategyParam<int> _cciPeriod;
		private readonly StrategyParam<int> _divergencePeriod;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<int> _overboughtLevel;
		private readonly StrategyParam<int> _oversoldLevel;

		private decimal? _previousPrice;
		private decimal? _previousCci;
		private decimal? _currentPrice;
		private decimal? _currentCci;
		private int _barsSinceDivergence;
		private bool _bullishDivergence;
		private bool _bearishDivergence;

		/// <summary>
		/// CCI calculation period.
		/// </summary>
		public int CciPeriod
		{
			get => _cciPeriod.Value;
			set => _cciPeriod.Value = value;
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
		/// CCI overbought level.
		/// </summary>
		public int OverboughtLevel
		{
			get => _overboughtLevel.Value;
			set => _overboughtLevel.Value = value;
		}

		/// <summary>
		/// CCI oversold level.
		/// </summary>
		public int OversoldLevel
		{
			get => _oversoldLevel.Value;
			set => _oversoldLevel.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CciDivergenceStrategy"/>.
		/// </summary>
		public CciDivergenceStrategy()
		{
			_cciPeriod = Param(nameof(CciPeriod), 20)
				.SetRange(10, 30)
				.SetDisplay("CCI Period", "Period for CCI calculation", "Indicator Parameters")
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

			_overboughtLevel = Param(nameof(OverboughtLevel), 100)
				.SetRange(80, 200)
				.SetDisplay("Overbought Level", "CCI level considered overbought", "Signal Parameters")
				.SetCanOptimize(true);

			_oversoldLevel = Param(nameof(OversoldLevel), -100)
				.SetRange(-200, -80)
				.SetDisplay("Oversold Level", "CCI level considered oversold", "Signal Parameters")
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
			_previousCci = null;
			_currentPrice = null;
			_currentCci = null;
			_barsSinceDivergence = 0;
			_bullishDivergence = false;
			_bearishDivergence = false;

			// Create CCI indicator
			var cci = new CommodityChannelIndex
			{
				Length = CciPeriod
			};

			// Create candle subscription
			var subscription = SubscribeCandles(CandleType);

			// Bind CCI to candles
			subscription
				.Bind(cci, ProcessCandle)
				.Start();

			// Enable position protection
			StartProtection(
				new Unit(0, UnitTypes.Absolute), // No take profit (managed by exit signals)
				new Unit(StopLossPercent, UnitTypes.Percent), // Stop loss at defined percentage
				false // No trailing stop
			);

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, cci);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal? cciValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Store previous values before updating
			if (_currentPrice.HasValue && _currentCci.HasValue)
			{
				_previousPrice = _currentPrice;
				_previousCci = _currentCci;
			}

			// Update current values
			_currentPrice = candle.ClosePrice;
			_currentCci = cciValue;

			LogInfo($"Candle: {candle.OpenTime}, Close: {candle.ClosePrice}, CCI: {cciValue:F2}");

			// Look for divergences once we have enough data
			if (_previousPrice.HasValue && _previousCci.HasValue && _currentPrice.HasValue && _currentCci.HasValue)
			{
				CheckForDivergences();
			}

			// Process signals based on detected divergences
			ProcessDivergenceSignals(candle, cciValue);
		}

		private void CheckForDivergences()
		{
			// Check for bullish divergence (lower price lows but higher CCI lows)
			if (_currentPrice < _previousPrice && _currentCci > _previousCci)
			{
				_bullishDivergence = true;
				_bearishDivergence = false;
				_barsSinceDivergence = 0;
				LogInfo($"Bullish Divergence Detected: Price {_previousPrice}->{_currentPrice}, CCI {_previousCci}->{_currentCci}");
			}
			// Check for bearish divergence (higher price highs but lower CCI highs)
			else if (_currentPrice > _previousPrice && _currentCci < _previousCci)
			{
				_bearishDivergence = true;
				_bullishDivergence = false;
				_barsSinceDivergence = 0;
				LogInfo($"Bearish Divergence Detected: Price {_previousPrice}->{_currentPrice}, CCI {_previousCci}->{_currentCci}");
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

		private void ProcessDivergenceSignals(ICandleMessage candle, decimal? cciValue)
		{
			// Entry signals based on detected divergences
			if (_bullishDivergence && Position <= 0 && cciValue < OversoldLevel)
			{
				// Bullish divergence with CCI in oversold territory - Buy signal
				if (Position < 0)
				{
					// Close any existing short position
					BuyMarket(Math.Abs(Position));
					LogInfo($"Closed short position on bullish divergence");
				}

				// Open new long position
				BuyMarket(Volume);
				LogInfo($"Buy signal: Bullish CCI divergence with oversold CCI value: {cciValue:F2}");
				
				// Reset divergence detection
				_bullishDivergence = false;
			}
			else if (_bearishDivergence && Position >= 0 && cciValue > OverboughtLevel)
			{
				// Bearish divergence with CCI in overbought territory - Sell signal
				if (Position > 0)
				{
					// Close any existing long position
					SellMarket(Position);
					LogInfo($"Closed long position on bearish divergence");
				}

				// Open new short position
				SellMarket(Volume);
				LogInfo($"Sell signal: Bearish CCI divergence with overbought CCI value: {cciValue:F2}");
				
				// Reset divergence detection
				_bearishDivergence = false;
			}
			
			// Exit signals based on CCI crossing zero line
			else if (Position > 0 && _previousCci < 0 && cciValue > 0)
			{
				// Exit long position when CCI crosses above zero
				SellMarket(Position);
				LogInfo($"Exit long: CCI crossed above zero from negative to positive");
			}
			else if (Position < 0 && _previousCci > 0 && cciValue < 0)
			{
				// Exit short position when CCI crosses below zero
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exit short: CCI crossed below zero from positive to negative");
			}
		}
	}
}