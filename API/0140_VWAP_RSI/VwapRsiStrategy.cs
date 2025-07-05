using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that uses VWAP (Volume Weighted Average Price) as a reference point
	/// and RSI (Relative Strength Index) for oversold/overbought conditions.
	/// Enters positions when price is below VWAP and RSI is oversold (for longs)
	/// or when price is above VWAP and RSI is overbought (for shorts).
	/// </summary>
	public class VwapRsiStrategy : Strategy
	{
		private readonly StrategyParam<int> _rsiPeriod;
		private readonly StrategyParam<decimal> _rsiOversold;
		private readonly StrategyParam<decimal> _rsiOverbought;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// RSI period.
		/// </summary>
		public int RsiPeriod
		{
			get => _rsiPeriod.Value;
			set => _rsiPeriod.Value = value;
		}

		/// <summary>
		/// RSI oversold level.
		/// </summary>
		public decimal RsiOversold
		{
			get => _rsiOversold.Value;
			set => _rsiOversold.Value = value;
		}

		/// <summary>
		/// RSI overbought level.
		/// </summary>
		public decimal RsiOverbought
		{
			get => _rsiOverbought.Value;
			set => _rsiOverbought.Value = value;
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
		public VwapRsiStrategy()
		{
			_rsiPeriod = Param(nameof(RsiPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("RSI Period", "Period of the RSI indicator", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(7, 21, 7);

			_rsiOversold = Param(nameof(RsiOversold), 30m)
				.SetNotNegative()
				.SetDisplay("RSI Oversold", "RSI level considered oversold", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(20m, 40m, 5m);

			_rsiOverbought = Param(nameof(RsiOverbought), 70m)
				.SetNotNegative()
				.SetDisplay("RSI Overbought", "RSI level considered overbought", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(60m, 80m, 5m);

			_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management")
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

			// Create indicators
			var rsi = new RelativeStrengthIndex
			{
				Length = RsiPeriod
			};

			var vwap = new VolumeWeightedMovingAverage();

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);

			subscription
				.Bind(vwap, rsi, ProcessCandles)
				.Start();

			// Setup position protection
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent) // Stop loss as percentage
			);

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, vwap);
				DrawIndicator(area, rsi);
				DrawOwnTrades(area);
			}
		}

		/// <summary>
		/// Process candles and indicator values.
		/// </summary>
		private void ProcessCandles(ICandleMessage candle, decimal? vwapValue, decimal? rsiValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Long entry: price below VWAP and RSI oversold
			if (candle.ClosePrice < vwapValue && rsiValue < RsiOversold && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			// Short entry: price above VWAP and RSI overbought
			else if (candle.ClosePrice > vwapValue && rsiValue > RsiOverbought && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}
			// Long exit: price rises above VWAP
			else if (Position > 0 && candle.ClosePrice > vwapValue)
			{
				SellMarket(Math.Abs(Position));
			}
			// Short exit: price falls below VWAP
			else if (Position < 0 && candle.ClosePrice < vwapValue)
			{
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}