using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy #73: Keltner Channel Reversal strategy.
	/// The strategy enters long when price is below lower Keltner Channel and a bullish candle appears,
	/// enters short when price is above upper Keltner Channel and a bearish candle appears.
	/// </summary>
	public class KeltnerChannelReversalStrategy : Strategy
	{
		private readonly StrategyParam<int> _emaPeriod;
		private readonly StrategyParam<decimal> _atrMultiplier;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLossAtrMultiplier;

		private KeltnerChannels _keltnerChannel;
		private AverageTrueRange _atr;

		/// <summary>
		/// EMA period for Keltner Channel calculation.
		/// </summary>
		public int EmaPeriod
		{
			get => _emaPeriod.Value;
			set => _emaPeriod.Value = value;
		}

		/// <summary>
		/// ATR multiplier for Keltner Channel calculation.
		/// </summary>
		public decimal AtrMultiplier
		{
			get => _atrMultiplier.Value;
			set => _atrMultiplier.Value = value;
		}

		/// <summary>
		/// ATR period for Keltner Channel calculation.
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		/// <summary>
		/// Candle type.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// ATR multiplier for stop-loss.
		/// </summary>
		public decimal StopLossAtrMultiplier
		{
			get => _stopLossAtrMultiplier.Value;
			set => _stopLossAtrMultiplier.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public KeltnerChannelReversalStrategy()
		{
			_emaPeriod = Param(nameof(EmaPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("EMA Period", "Period for the EMA in Keltner Channel", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 5);

			_atrMultiplier = Param(nameof(AtrMultiplier), 2.0m)
				.SetNotNegative()
				.SetDisplay("ATR Multiplier", "Multiplier for the ATR in Keltner Channel", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "Period for ATR calculation", "Indicators");

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_stopLossAtrMultiplier = Param(nameof(StopLossAtrMultiplier), 2.0m)
				.SetNotNegative()
				.SetDisplay("Stop Loss ATR Multiplier", "ATR multiplier for stop-loss calculation", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);
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
			_keltnerChannel = new KeltnerChannels
			{
				Length = EmaPeriod,
				K = AtrMultiplier,
				ATRLength = AtrPeriod
			};

			_atr = new AverageTrueRange
			{
				Length = AtrPeriod
			};

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(_keltnerChannel, _atr, ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _keltnerChannel);
				DrawOwnTrades(area);
			}

			// Start position protection
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit, rely on the strategy's exit logic
				stopLoss: new Unit(StopLossAtrMultiplier, UnitTypes.ATR)
			);
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue keltnerValue, IIndicatorValue atrValue)
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Get Keltner Channel values
			var upper = keltnerValue.GetValue<KeltnerChannelValue>().Upper;
			var lower = keltnerValue.GetValue<KeltnerChannelValue>().Lower;
			var middle = keltnerValue.GetValue<KeltnerChannelValue>().Middle;

			// Determine if the candle is bullish or bearish
			var isBullish = candle.ClosePrice > candle.OpenPrice;
			var isBearish = candle.ClosePrice < candle.OpenPrice;

			// Long entry: Price below lower band and bullish candle
			if (candle.ClosePrice < lower && isBullish && Position <= 0)
			{
				// Cancel active orders first
				CancelActiveOrders();
				
				// Enter long position
				BuyMarket(Volume + Math.Abs(Position));
				
				LogInfo($"Long entry: Price {candle.ClosePrice} below lower band {lower} with bullish candle");
			}
			// Short entry: Price above upper band and bearish candle
			else if (candle.ClosePrice > upper && isBearish && Position >= 0)
			{
				// Cancel active orders first
				CancelActiveOrders();
				
				// Enter short position
				SellMarket(Volume + Math.Abs(Position));
				
				LogInfo($"Short entry: Price {candle.ClosePrice} above upper band {upper} with bearish candle");
			}
			// Long exit: Price returns to middle band
			else if (candle.ClosePrice > middle && Position > 0)
			{
				SellMarket(Math.Abs(Position));
				LogInfo($"Long exit: Price {candle.ClosePrice} above middle band {middle}");
			}
			// Short exit: Price returns to middle band
			else if (candle.ClosePrice < middle && Position < 0)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Short exit: Price {candle.ClosePrice} below middle band {middle}");
			}
		}
	}
}