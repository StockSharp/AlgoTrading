using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on MACD and Bollinger Bands indicators (#206)
	/// </summary>
	public class MacdBollingerStrategy : Strategy
	{
		private readonly StrategyParam<int> _macdFast;
		private readonly StrategyParam<int> _macdSlow;
		private readonly StrategyParam<int> _macdSignal;
		private readonly StrategyParam<int> _bollingerPeriod;
		private readonly StrategyParam<decimal> _bollingerDeviation;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<decimal> _atrMultiplier;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// MACD fast EMA period
		/// </summary>
		public int MacdFast
		{
			get => _macdFast.Value;
			set => _macdFast.Value = value;
		}

		/// <summary>
		/// MACD slow EMA period
		/// </summary>
		public int MacdSlow
		{
			get => _macdSlow.Value;
			set => _macdSlow.Value = value;
		}

		/// <summary>
		/// MACD signal line period
		/// </summary>
		public int MacdSignal
		{
			get => _macdSignal.Value;
			set => _macdSignal.Value = value;
		}

		/// <summary>
		/// Bollinger Bands period
		/// </summary>
		public int BollingerPeriod
		{
			get => _bollingerPeriod.Value;
			set => _bollingerPeriod.Value = value;
		}

		/// <summary>
		/// Bollinger Bands standard deviation multiplier
		/// </summary>
		public decimal BollingerDeviation
		{
			get => _bollingerDeviation.Value;
			set => _bollingerDeviation.Value = value;
		}

		/// <summary>
		/// ATR period for stop-loss
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		/// <summary>
		/// ATR multiplier for stop-loss
		/// </summary>
		public decimal AtrMultiplier
		{
			get => _atrMultiplier.Value;
			set => _atrMultiplier.Value = value;
		}

		/// <summary>
		/// Candle type for strategy
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public MacdBollingerStrategy()
		{
			_macdFast = Param(nameof(MacdFast), 12)
				.SetRange(5, 20, 1)
				.SetDisplay("MACD Fast", "MACD fast EMA period", "MACD")
				.SetCanOptimize(true);

			_macdSlow = Param(nameof(MacdSlow), 26)
				.SetRange(15, 40, 1)
				.SetDisplay("MACD Slow", "MACD slow EMA period", "MACD")
				.SetCanOptimize(true);

			_macdSignal = Param(nameof(MacdSignal), 9)
				.SetRange(5, 15, 1)
				.SetDisplay("MACD Signal", "MACD signal line period", "MACD")
				.SetCanOptimize(true);

			_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
				.SetRange(10, 50, 5)
				.SetDisplay("Bollinger Period", "Bollinger Bands period", "Bollinger")
				.SetCanOptimize(true);

			_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
				.SetRange(1.0m, 3.0m, 0.5m)
				.SetDisplay("Bollinger Deviation", "Bollinger Bands standard deviation multiplier", "Bollinger")
				.SetCanOptimize(true);

			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetRange(7, 28, 7)
				.SetDisplay("ATR Period", "ATR period for stop-loss calculation", "Risk Management")
				.SetCanOptimize(true);

			_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
				.SetRange(1m, 4m, 0.5m)
				.SetDisplay("ATR Multiplier", "Multiplier for ATR-based stop-loss", "Risk Management")
				.SetCanOptimize(true);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return new[] { (Security, CandleType) };
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Initialize indicators
			var macd = new MovingAverageConvergenceDivergence
			{
				LongMa = new ExponentialMovingAverage { Length = MacdSlow },
				ShortMa = new ExponentialMovingAverage { Length = MacdFast },
				SignalMa = new ExponentialMovingAverage { Length = MacdSignal }
			};

			var bollinger = new BollingerBands
			{
				Length = BollingerPeriod,
				Width = BollingerDeviation
			};

			var atr = new AverageTrueRange { Length = AtrPeriod };

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(bollinger, (candle, bollingerValues) =>
				{
					// Process MACD
					var macdResult = macd.Process(candle);
					var macdLine = macdResult.GetValue<decimal>();
					var signalLine = macd.SignalMa.GetCurrentValue();
					
					// Get Bollinger values
					var middleBand = bollingerValues[0].GetValue<decimal>();
					var upperBand = bollingerValues[1].GetValue<decimal>();
					var lowerBand = bollingerValues[2].GetValue<decimal>();
					
					// Process ATR
					var atrValue = atr.Process(candle).GetValue<decimal>();

					ProcessIndicators(candle, macdLine, signalLine, middleBand, upperBand, lowerBand, atrValue);
				})
				.Start();
			
			// Enable ATR-based stop protection
			StartProtection(default, new Unit(AtrMultiplier, UnitTypes.Absolute));

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, bollinger);
				DrawIndicator(area, macd);
				DrawOwnTrades(area);
			}
		}

		private void ProcessIndicators(ICandleMessage candle, decimal macdLine, decimal signalLine, 
			decimal middleBand, decimal upperBand, decimal lowerBand, decimal atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var price = candle.ClosePrice;

			// Trading logic:
			// Long: MACD > Signal && Price < BB_lower (trend up with oversold conditions)
			// Short: MACD < Signal && Price > BB_upper (trend down with overbought conditions)
			
			var macdCrossOver = macdLine > signalLine;

			if (macdCrossOver && price < lowerBand && Position <= 0)
			{
				// Buy signal - MACD crossing above signal line at lower Bollinger Band
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			else if (!macdCrossOver && price > upperBand && Position >= 0)
			{
				// Sell signal - MACD crossing below signal line at upper Bollinger Band
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}
			// Exit conditions
			else if (Position > 0 && price > middleBand)
			{
				// Exit long position when price returns to middle band
				SellMarket(Position);
			}
			else if (Position < 0 && price < middleBand)
			{
				// Exit short position when price returns to middle band
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}
