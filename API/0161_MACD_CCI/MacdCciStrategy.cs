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
	/// Implementation of strategy #161 - MACD + CCI.
	/// Buy when MACD is above Signal line and CCI is below -100 (oversold).
	/// Sell when MACD is below Signal line and CCI is above 100 (overbought).
	/// </summary>
	public class MacdCciStrategy : Strategy
	{
		private readonly StrategyParam<int> _fastPeriod;
		private readonly StrategyParam<int> _slowPeriod;
		private readonly StrategyParam<int> _signalPeriod;
		private readonly StrategyParam<int> _cciPeriod;
		private readonly StrategyParam<decimal> _cciOversold;
		private readonly StrategyParam<decimal> _cciOverbought;
		private readonly StrategyParam<Unit> _stopLoss;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// MACD fast period.
		/// </summary>
		public int FastPeriod
		{
			get => _fastPeriod.Value;
			set => _fastPeriod.Value = value;
		}

		/// <summary>
		/// MACD slow period.
		/// </summary>
		public int SlowPeriod
		{
			get => _slowPeriod.Value;
			set => _slowPeriod.Value = value;
		}

		/// <summary>
		/// MACD signal period.
		/// </summary>
		public int SignalPeriod
		{
			get => _signalPeriod.Value;
			set => _signalPeriod.Value = value;
		}

		/// <summary>
		/// CCI period.
		/// </summary>
		public int CciPeriod
		{
			get => _cciPeriod.Value;
			set => _cciPeriod.Value = value;
		}

		/// <summary>
		/// CCI oversold level.
		/// </summary>
		public decimal CciOversold
		{
			get => _cciOversold.Value;
			set => _cciOversold.Value = value;
		}

		/// <summary>
		/// CCI overbought level.
		/// </summary>
		public decimal CciOverbought
		{
			get => _cciOverbought.Value;
			set => _cciOverbought.Value = value;
		}

		/// <summary>
		/// Stop-loss value.
		/// </summary>
		public Unit StopLoss
		{
			get => _stopLoss.Value;
			set => _stopLoss.Value = value;
		}

		/// <summary>
		/// Candle type used for strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initialize <see cref="MacdCciStrategy"/>.
		/// </summary>
		public MacdCciStrategy()
		{
			_fastPeriod = Param(nameof(FastPeriod), 12)
				.SetGreaterThanZero()
				.SetDisplay("Fast Period", "Fast EMA period for MACD", "MACD Parameters");

			_slowPeriod = Param(nameof(SlowPeriod), 26)
				.SetGreaterThanZero()
				.SetDisplay("Slow Period", "Slow EMA period for MACD", "MACD Parameters");

			_signalPeriod = Param(nameof(SignalPeriod), 9)
				.SetGreaterThanZero()
				.SetDisplay("Signal Period", "Signal line period for MACD", "MACD Parameters");

			_cciPeriod = Param(nameof(CciPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("CCI Period", "Period for Commodity Channel Index", "CCI Parameters");

			_cciOversold = Param(nameof(CciOversold), -100m)
				.SetDisplay("CCI Oversold", "CCI level to consider market oversold", "CCI Parameters");

			_cciOverbought = Param(nameof(CciOverbought), 100m)
				.SetDisplay("CCI Overbought", "CCI level to consider market overbought", "CCI Parameters");

			_stopLoss = Param(nameof(StopLoss), new Unit(2, UnitTypes.Percent))
				.SetDisplay("Stop Loss", "Stop loss percent or value", "Risk Management");

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Candle type for strategy", "General");
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

			// Create indicators

			var macd = new MovingAverageConvergenceDivergenceSignal
			{
				Macd =
				{
					ShortMa = { Length = FastPeriod },
					LongMa = { Length = SlowPeriod },
				},
				SignalMa = { Length = SignalPeriod }
			};
			var cci = new CommodityChannelIndex { Length = CciPeriod };

			// Setup candle subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind indicators to candles
			subscription
				.BindEx(macd, cci, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, macd);
				
				// Create separate area for CCI
				var cciArea = CreateChartArea();
				if (cciArea != null)
				{
					DrawIndicator(cciArea, cci);
				}
				
				DrawOwnTrades(area);
			}

			// Start protective orders
			StartProtection(new(), StopLoss);
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue cciValue)
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Note: In this implementation, the MACD and signal values are obtained separately.
			// We need to extract both MACD and signal values to determine crossovers.
			// For demonstration, we'll access these values through a direct call to the indicator.
			// In a proper implementation, we should find a way to get these values through Bind parameter values.
			
			// Get MACD line and Signal line values
			// This approach is not ideal - in a proper implementation, these values should come from the Bind parameters
			var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
			var macdLine = macdTyped.Macd; // The main MACD line
			var signalLine = macdTyped.Signal; // Signal line
			
			// Determine if MACD is above or below signal line
			var isMacdAboveSignal = macdLine > signalLine;

			var cciDec = cciValue.ToDecimal();

			LogInfo($"Candle: {candle.OpenTime}, Close: {candle.ClosePrice}, " +
				   $"MACD: {macdLine}, Signal: {signalLine}, " +
				   $"MACD > Signal: {isMacdAboveSignal}, CCI: {cciDec}");

			// Trading rules
			if (isMacdAboveSignal && cciDec < CciOversold && Position <= 0)
			{
				// Buy signal - MACD above signal line and CCI oversold
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				
				LogInfo($"Buy signal: MACD above Signal and CCI oversold ({cciDec} < {CciOversold}). Volume: {volume}");
			}
			else if (!isMacdAboveSignal && cciDec > CciOverbought && Position >= 0)
			{
				// Sell signal - MACD below signal line and CCI overbought
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				
				LogInfo($"Sell signal: MACD below Signal and CCI overbought ({cciDec} > {CciOverbought}). Volume: {volume}");
			}
			// Exit conditions based on MACD crossovers
			else if (!isMacdAboveSignal && Position > 0)
			{
				// Exit long position when MACD crosses below signal
				SellMarket(Position);
				LogInfo($"Exit long: MACD crossed below Signal. Position: {Position}");
			}
			else if (isMacdAboveSignal && Position < 0)
			{
				// Exit short position when MACD crosses above signal
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exit short: MACD crossed above Signal. Position: {Position}");
			}
		}

		private T GetIndicator<T>() where T : IIndicator
		{
			// Helper method to find an indicator by type in the Indicators collection
			foreach (var indicator in Indicators)
			{
				if (indicator is T typedIndicator)
					return typedIndicator;
			}

			return default;
		}
	}
}
