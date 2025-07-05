using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy that trades on volatility expansion as measured by ATR (Average True Range).
	/// It enters positions when ATR is increasing (volatility expansion) and price is above/below MA,
	/// and exits when volatility starts to contract (ATR decreasing).
	/// </summary>
	public class AtrExpansionStrategy : Strategy
	{
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<int> _maPeriod;
		private readonly StrategyParam<decimal> _atrMultiplier;
		private readonly StrategyParam<DataType> _candleType;

		private decimal _prevAtr;

		/// <summary>
		/// Period for ATR calculation (default: 14)
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		/// <summary>
		/// Period for Moving Average calculation (default: 20)
		/// </summary>
		public int MAPeriod
		{
			get => _maPeriod.Value;
			set => _maPeriod.Value = value;
		}

		/// <summary>
		/// ATR multiplier for stop-loss calculation (default: 2.0)
		/// </summary>
		public decimal AtrMultiplier
		{
			get => _atrMultiplier.Value;
			set => _atrMultiplier.Value = value;
		}

		/// <summary>
		/// Type of candles used for strategy calculation
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initialize the ATR Expansion strategy
		/// </summary>
		public AtrExpansionStrategy()
		{
			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetDisplay("ATR Period", "Period for ATR calculation", "Technical Parameters")
				.SetCanOptimize(true)
				.SetOptimize(7, 21, 7);

			_maPeriod = Param(nameof(MAPeriod), 20)
				.SetDisplay("MA Period", "Period for Moving Average calculation", "Technical Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_atrMultiplier = Param(nameof(AtrMultiplier), 2.0m)
				.SetDisplay("ATR Multiplier", "ATR multiplier for stop-loss calculation", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "Data");
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

			// Reset state variables
			_prevAtr = 0;

			// Create indicators
			var atr = new AverageTrueRange { Length = AtrPeriod };
			var sma = new SimpleMovingAverage { Length = MAPeriod };

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(atr, sma, ProcessCandle)
				.Start();

			// Configure chart
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, sma);
				DrawIndicator(area, atr);
				DrawOwnTrades(area);
			}
		}

		/// <summary>
		/// Process candle and check for ATR expansion signals
		/// </summary>
		private void ProcessCandle(ICandleMessage candle, decimal? atrValue, decimal? smaValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Initialize _prevAtr on first formed candle
			if (_prevAtr == 0)
			{
				_prevAtr = atrValue;
				return;
			}

			// Check if ATR is expanding (increasing)
			bool isAtrExpanding = atrValue > _prevAtr;
			
			// Determine price position relative to MA
			bool isPriceAboveMA = candle.ClosePrice > smaValue;
			
			// Calculate stop-loss amount based on ATR
			decimal stopLossAmount = atrValue * AtrMultiplier;

			if (Position == 0)
			{
				// No position - check for entry signals
				if (isAtrExpanding && isPriceAboveMA)
				{
					// ATR is expanding and price is above MA - buy (long)
					var order = BuyMarket(Volume);
					RegisterOrder(order);
				}
				else if (isAtrExpanding && !isPriceAboveMA)
				{
					// ATR is expanding and price is below MA - sell (short)
					var order = SellMarket(Volume);
					RegisterOrder(order);
				}
			}
			else if (Position > 0)
			{
				// Long position - check for exit signal
				if (!isAtrExpanding)
				{
					// ATR is decreasing (volatility contracting) - exit long
					var order = SellMarket(Position);
					RegisterOrder(order);
				}
			}
			else if (Position < 0)
			{
				// Short position - check for exit signal
				if (!isAtrExpanding)
				{
					// ATR is decreasing (volatility contracting) - exit short
					var order = BuyMarket(Math.Abs(Position));
					RegisterOrder(order);
				}
			}

			// Update previous ATR value
			_prevAtr = atrValue;
		}
	}
}
