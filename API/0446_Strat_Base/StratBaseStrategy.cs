using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy Base Template - template for creating new strategies
	/// </summary>
	public class StratBaseStrategy : Strategy
	{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _emaLength;
		private readonly StrategyParam<bool> _showLong;
		private readonly StrategyParam<bool> _showShort;
		private readonly StrategyParam<bool> _useTP;
		private readonly StrategyParam<decimal> _tpPercent;
		private readonly StrategyParam<bool> _useSL;
		private readonly StrategyParam<decimal> _slPercent;

		private ExponentialMovingAverage _ema;

		/// <summary>
		/// Candle type for strategy calculation.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// EMA length.
		/// </summary>
		public int EmaLength
		{
			get => _emaLength.Value;
			set => _emaLength.Value = value;
		}

		/// <summary>
		/// Enable long entries.
		/// </summary>
		public bool ShowLong
		{
			get => _showLong.Value;
			set => _showLong.Value = value;
		}

		/// <summary>
		/// Enable short entries.
		/// </summary>
		public bool ShowShort
		{
			get => _showShort.Value;
			set => _showShort.Value = value;
		}

		/// <summary>
		/// Use take profit.
		/// </summary>
		public bool UseTP
		{
			get => _useTP.Value;
			set => _useTP.Value = value;
		}

		/// <summary>
		/// Take profit percentage.
		/// </summary>
		public decimal TpPercent
		{
			get => _tpPercent.Value;
			set => _tpPercent.Value = value;
		}

		/// <summary>
		/// Use stop loss.
		/// </summary>
		public bool UseSL
		{
			get => _useSL.Value;
			set => _useSL.Value = value;
		}

		/// <summary>
		/// Stop loss percentage.
		/// </summary>
		public decimal SlPercent
		{
			get => _slPercent.Value;
			set => _slPercent.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public StratBaseStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_emaLength = Param(nameof(EmaLength), 10)
				.SetGreaterThanZero()
				.SetDisplay("EMA Length", "EMA period", "Moving Averages")
				.SetCanOptimize(true)
				.SetOptimize(5, 50, 5);

			_showLong = Param(nameof(ShowLong), true)
				.SetDisplay("Long Entries", "Enable long entries", "Strategy");

			_showShort = Param(nameof(ShowShort), false)
				.SetDisplay("Short Entries", "Enable short entries", "Strategy");

			_useTP = Param(nameof(UseTP), false)
				.SetDisplay("Enable Take Profit", "Use take profit", "Take Profit");

			_tpPercent = Param(nameof(TpPercent), 1.2m)
				.SetRange(0.1m, 10.0m)
				.SetDisplay("TP Percent", "Take profit percentage", "Take Profit")
				.SetCanOptimize(true)
				.SetOptimize(0.5m, 3.0m, 0.3m);

			_useSL = Param(nameof(UseSL), false)
				.SetDisplay("Enable Stop Loss", "Use stop loss", "Stop Loss");

			_slPercent = Param(nameof(SlPercent), 1.8m)
				.SetRange(0.1m, 10.0m)
				.SetDisplay("SL Percent", "Stop loss percentage", "Stop Loss")
				.SetCanOptimize(true)
				.SetOptimize(0.5m, 5.0m, 0.5m);
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
			_ema = new ExponentialMovingAverage { Length = EmaLength };

			// Create subscription for candles
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(_ema, ProcessCandle)
				.Start();

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _ema);
				DrawOwnTrades(area);
			}

			// Setup protection
			var takeProfit = UseTP ? new Unit(TpPercent / 100m, UnitTypes.Percent) : new Unit();
			var stopLoss = UseSL ? new Unit(SlPercent / 100m, UnitTypes.Percent) : new Unit();
			StartProtection(takeProfit, stopLoss);
		}

		private void ProcessCandle(ICandleMessage candle, decimal emaValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Wait for indicator to form
			if (!_ema.IsFormed)
				return;

			// TODO: Implement entry and exit conditions
			// This is a base template - specific logic should be added here
			
			CheckEntryConditions(candle, emaValue);
			CheckExitConditions(candle, emaValue);
		}

		private void CheckEntryConditions(ICandleMessage candle, decimal emaValue)
		{
			var currentPrice = candle.ClosePrice;

			// TODO: Add specific entry logic here
			// Example conditions (to be replaced with actual strategy logic):
			
			if (ShowLong && Position == 0)
			{
				// Long entry condition placeholder
				// RegisterOrder(this.CreateOrder(Sides.Buy, currentPrice, GetOrderVolume()));
			}

			if (ShowShort && Position == 0)
			{
				// Short entry condition placeholder  
				// RegisterOrder(this.CreateOrder(Sides.Sell, currentPrice, GetOrderVolume()));
			}
		}

		private void CheckExitConditions(ICandleMessage candle, decimal emaValue)
		{
			// TODO: Add specific exit logic here
			// Example exit conditions (to be replaced with actual strategy logic):

			if (Position > 0)
			{
				// Long exit condition placeholder
				// RegisterOrder(this.CreateOrder(Sides.Sell, candle.ClosePrice, Math.Abs(Position)));
			}

			if (Position < 0)
			{
				// Short exit condition placeholder
				// RegisterOrder(this.CreateOrder(Sides.Buy, candle.ClosePrice, Math.Abs(Position)));
			}
		}

		private decimal GetOrderVolume()
		{
			return 1;
		}
	}
}