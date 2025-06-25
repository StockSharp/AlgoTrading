using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on MA and ADX indicators.
	/// Enters position when price crosses MA with strong trend.
	/// </summary>
	public class MaAdxStrategy : Strategy
	{
		private readonly StrategyParam<int> _maPeriod;
		private readonly StrategyParam<int> _adxPeriod;
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<decimal> _takeProfitAtrMultiplier;

		private decimal _atrValue;
		private bool _isFirstCandle = true;

		/// <summary>
		/// MA period.
		/// </summary>
		public int MaPeriod
		{
			get => _maPeriod.Value;
			set => _maPeriod.Value = value;
		}

		/// <summary>
		/// ADX period.
		/// </summary>
		public int AdxPeriod
		{
			get => _adxPeriod.Value;
			set => _adxPeriod.Value = value;
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
		/// Stop loss percentage.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
		}

		/// <summary>
		/// Take profit ATR multiplier.
		/// </summary>
		public decimal TakeProfitAtrMultiplier
		{
			get => _takeProfitAtrMultiplier.Value;
			set => _takeProfitAtrMultiplier.Value = value;
		}

		/// <summary>
		/// Initialize strategy.
		/// </summary>
		public MaAdxStrategy()
		{
			_maPeriod = Param(nameof(MaPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_adxPeriod = Param(nameof(AdxPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")
				.SetCanOptimize(true)
				.SetOptimize(7, 28, 7);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_stopLossPercent = Param(nameof(StopLossPercent), 2m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1m, 5m, 0.5m);

			_takeProfitAtrMultiplier = Param(nameof(TakeProfitAtrMultiplier), 2m)
				.SetGreaterThanZero()
				.SetDisplay("TP ATR Multiplier", "Take profit as ATR multiplier", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1m, 5m, 0.5m);
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
			var ma = new SMA { Length = MaPeriod };
			var adx = new ADX { Length = AdxPeriod };
			var atr = new ATR { Length = AdxPeriod };

			// Create subscription
			var subscription = SubscribeCandles(CandleType);

			// Bind indicators to candles
			subscription
				.Bind(ma, adx, atr, ProcessCandle)
				.Start();

			// Enable stop-loss and take-profit
			StartProtection(
				takeProfit: new Unit(TakeProfitAtrMultiplier, UnitTypes.Absolute),
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
				isStopTrailing: false,
				useMarketOrders: true
			);

			// Setup chart if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, ma);
				DrawIndicator(area, adx);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal adxValue, decimal atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Save ATR value for stop-loss calculation
			_atrValue = atrValue;

			// Skip the first candle to have previous values to compare
			if (_isFirstCandle)
			{
				_isFirstCandle = false;
				return;
			}

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Trading logic
			if (adxValue > 25)
			{
				// Strong trend detected
				if (candle.ClosePrice > maValue && Position <= 0)
				{
					// Price above MA and no long position - Buy
					var volume = Volume + Math.Abs(Position);
					BuyMarket(volume);
				}
				else if (candle.ClosePrice < maValue && Position >= 0)
				{
					// Price below MA and no short position - Sell
					var volume = Volume + Math.Abs(Position);
					SellMarket(volume);
				}
			}
		}
	}
}
