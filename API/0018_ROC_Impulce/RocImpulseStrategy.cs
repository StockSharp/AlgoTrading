using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Rate of Change (ROC) impulse.
	/// </summary>
	public class RocImpulseStrategy : Strategy
	{
		private readonly StrategyParam<int> _rocPeriod;
		private readonly StrategyParam<decimal> _atrMultiplier;
		private readonly StrategyParam<DataType> _candleType;

		private decimal _previousRoc;
		private bool _isFirstCandle = true;

		/// <summary>
		/// ROC period.
		/// </summary>
		public int RocPeriod
		{
			get => _rocPeriod.Value;
			set => _rocPeriod.Value = value;
		}

		/// <summary>
		/// ATR multiplier for stop-loss.
		/// </summary>
		public decimal AtrMultiplier
		{
			get => _atrMultiplier.Value;
			set => _atrMultiplier.Value = value;
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
		/// Initializes a new instance of the <see cref="RocImpulseStrategy"/>.
		/// </summary>
		public RocImpulseStrategy()
		{
			_rocPeriod = Param(nameof(RocPeriod), 12)
				.SetRange(5, 30)
				.SetDisplay("ROC Period", "Period for Rate of Change calculation", "Indicators")
				.SetCanOptimize(true);

			_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
				.SetRange(1m, 5m)
				.SetDisplay("ATR Multiplier", "Multiplier for ATR stop loss", "Risk Management")
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

			// Reset state variables
			_previousRoc = 0;
			_isFirstCandle = true;

			// Create indicators
			var roc = new RateOfChange { Length = RocPeriod };
			var atr = new AverageTrueRange { Length = 14 };

			// Subscribe to candles and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(roc, atr, ProcessCandle)
				.Start();

			// Enable position protection with ATR-based stop loss
			StartProtection(
				takeProfit: null,
				stopLoss: new Unit(AtrMultiplier, UnitTypes.Absolute),
				isStopTrailing: false,
				useMarketOrders: true
			);

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, roc);
				DrawIndicator(area, atr);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal rocValue, decimal atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			if (_isFirstCandle)
			{
				_previousRoc = rocValue;
				_isFirstCandle = false;
				return;
			}

			// Entry logic for long positions:
			// ROC is positive and increasing (positive momentum)
			if (rocValue > 0 && rocValue > _previousRoc && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				LogInfo($"Buy signal: ROC positive and increasing. Current: {rocValue:F4}, Previous: {_previousRoc:F4}");
			}
			// Entry logic for short positions:
			// ROC is negative and decreasing (negative momentum)
			else if (rocValue < 0 && rocValue < _previousRoc && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				LogInfo($"Sell signal: ROC negative and decreasing. Current: {rocValue:F4}, Previous: {_previousRoc:F4}");
			}

			// Exit logic for long positions: ROC turns negative
			if (Position > 0 && rocValue < 0)
			{
				SellMarket(Math.Abs(Position));
				LogInfo($"Exiting long position: ROC turned negative at {rocValue:F4}");
			}
			// Exit logic for short positions: ROC turns positive
			else if (Position < 0 && rocValue > 0)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exiting short position: ROC turned positive at {rocValue:F4}");
			}

			// Store current ROC value for next comparison
			_previousRoc = rocValue;
		}
	}
}