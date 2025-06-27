using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// ATR Mean Reversion strategy.
	/// Trades when price deviates from its average by a multiple of ATR.
	/// </summary>
	public class AtrMeanReversionStrategy : Strategy
	{
		private readonly StrategyParam<int> _maPeriodParam;
		private readonly StrategyParam<int> _atrPeriodParam;
		private readonly StrategyParam<decimal> _multiplierParam;
		private readonly StrategyParam<DataType> _candleTypeParam;

		private SimpleMovingAverage _sma;
		private AverageTrueRange _atr;

		/// <summary>
		/// Moving average period.
		/// </summary>
		public int MaPeriod
		{
			get => _maPeriodParam.Value;
			set => _maPeriodParam.Value = value;
		}

		/// <summary>
		/// ATR indicator period.
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriodParam.Value;
			set => _atrPeriodParam.Value = value;
		}

		/// <summary>
		/// ATR multiplier for entry threshold.
		/// </summary>
		public decimal Multiplier
		{
			get => _multiplierParam.Value;
			set => _multiplierParam.Value = value;
		}

		/// <summary>
		/// Candle type for strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleTypeParam.Value;
			set => _candleTypeParam.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public AtrMeanReversionStrategy()
		{
			_maPeriodParam = Param(nameof(MaPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("MA Period", "Period for Moving Average", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 10);

			_atrPeriodParam = Param(nameof(AtrPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "Period for ATR indicator", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(7, 21, 7);

			_multiplierParam = Param(nameof(Multiplier), 2.0m)
				.SetRange(0.1m, decimal.MaxValue)
				.SetDisplay("ATR Multiplier", "ATR multiplier for entry threshold", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Candle type for strategy", "Common");
		}

		/// <summary>
		/// Returns working securities.
		/// </summary>
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return [(Security, CandleType)];
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create indicators
			_sma = new SimpleMovingAverage { Length = MaPeriod };
			_atr = new AverageTrueRange { Length = AtrPeriod };

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(_sma, _atr, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _sma);
				DrawIndicator(area, _atr);
				DrawOwnTrades(area);
			}
			
			// Enable position protection
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit
				stopLoss: new Unit(2, UnitTypes.Absolute) // Stop loss at 2*ATR
			);
		}

		private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal atrValue)
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Calculate entry thresholds
			var upperThreshold = smaValue + Multiplier * atrValue;
			var lowerThreshold = smaValue - Multiplier * atrValue;
			
			// Long setup - price below lower threshold
			if (candle.ClosePrice < lowerThreshold && Position <= 0)
			{
				// Buy signal - price has deviated too much below average
				BuyMarket(Volume + Math.Abs(Position));
			}
			// Short setup - price above upper threshold
			else if (candle.ClosePrice > upperThreshold && Position >= 0)
			{
				// Sell signal - price has deviated too much above average
				SellMarket(Volume + Math.Abs(Position));
			}
			// Exit long position when price returns to average
			else if (Position > 0 && candle.ClosePrice >= smaValue)
			{
				// Close long position
				SellMarket(Position);
			}
			// Exit short position when price returns to average
			else if (Position < 0 && candle.ClosePrice <= smaValue)
			{
				// Close short position
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}