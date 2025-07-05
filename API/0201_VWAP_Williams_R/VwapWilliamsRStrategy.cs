using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on VWAP and Williams %R indicators (#201)
	/// </summary>
	public class VwapWilliamsRStrategy : Strategy
	{
		private readonly StrategyParam<int> _williamsRPeriod;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		// Store previous values
		private decimal _previousWilliamsR;

		/// <summary>
		/// Williams %R period
		/// </summary>
		public int WilliamsRPeriod
		{
			get => _williamsRPeriod.Value;
			set => _williamsRPeriod.Value = value;
		}

		/// <summary>
		/// Stop-loss percentage
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
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
		public VwapWilliamsRStrategy()
		{
			_williamsRPeriod = Param(nameof(WilliamsRPeriod), 14)
				.SetRange(5, 50)
				.SetDisplay("Williams %R Period", "Period for Williams %R indicator", "Indicators")
				.SetCanOptimize(true);

			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetRange(0.5m, 5m)
				.SetDisplay("Stop-Loss %", "Stop-loss percentage from entry price", "Risk Management")
				.SetCanOptimize(true);

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
			var vwap = new VolumeWeightedMovingAverage();
			var williamsR = new WilliamsR { Length = WilliamsRPeriod };

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(vwap, williamsR, ProcessCandle)
				.Start();

			// Enable stop-loss protection
			StartProtection(new Unit(StopLossPercent, UnitTypes.Percent), default);

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, vwap);
				DrawIndicator(area, williamsR);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal? vwapValue, decimal? williamsRValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Store previous value to detect changes
			var previousWilliamsR = _previousWilliamsR;
			_previousWilliamsR = williamsRValue;

			// Trading logic:
			// Long: Price < VWAP && Williams %R < -80 (oversold below VWAP)
			// Short: Price > VWAP && Williams %R > -20 (overbought above VWAP)

			var price = candle.ClosePrice;

			if (price < vwapValue && williamsRValue < -80 && Position <= 0)
			{
				// Buy signal - oversold condition below VWAP
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			else if (price > vwapValue && williamsRValue > -20 && Position >= 0)
			{
				// Sell signal - overbought condition above VWAP
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}
			// Exit conditions
			else if (Position > 0 && price > vwapValue)
			{
				// Exit long position when price breaks above VWAP
				SellMarket(Position);
			}
			else if (Position < 0 && price < vwapValue)
			{
				// Exit short position when price breaks below VWAP
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}
