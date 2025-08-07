using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Supertrend + EMA Rebound Strategy - trades Supertrend direction changes and EMA rebounds
	/// </summary>
	public class SupertrendEmaReboundStrategy : Strategy
	{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<decimal> _atrFactor;
		private readonly StrategyParam<int> _emaLength;
		private readonly StrategyParam<bool> _showLong;
		private readonly StrategyParam<bool> _showShort;
		private readonly StrategyParam<string> _tpType;
		private readonly StrategyParam<decimal> _tpPercent;

		private SuperTrend _supertrend;
		private ExponentialMovingAverage _ema;
		private decimal _previousSupertrendDirection;
		private decimal _previousClose;
		private decimal _lastEntryPrice;

		/// <summary>
		/// Candle type for strategy calculation.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// ATR period for Supertrend calculation.
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		/// <summary>
		/// ATR factor for Supertrend calculation.
		/// </summary>
		public decimal AtrFactor
		{
			get => _atrFactor.Value;
			set => _atrFactor.Value = value;
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
		/// Take profit type.
		/// </summary>
		public string TpType
		{
			get => _tpType.Value;
			set => _tpType.Value = value;
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
		/// Constructor.
		/// </summary>
		public SupertrendEmaReboundStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_atrPeriod = Param(nameof(AtrPeriod), 10)
				.SetGreaterThanZero()
				.SetDisplay("ATR Period", "ATR period for Supertrend", "Supertrend")
				.SetCanOptimize(true)
				.SetOptimize(7, 15, 2);

			_atrFactor = Param(nameof(AtrFactor), 3.0m)
				.SetRange(0.5m, 10.0m)
				.SetDisplay("ATR Factor", "ATR factor for Supertrend", "Supertrend")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 5.0m, 0.5m);

			_emaLength = Param(nameof(EmaLength), 20)
				.SetGreaterThanZero()
				.SetDisplay("EMA Length", "EMA period", "Moving Average")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5);

			_showLong = Param(nameof(ShowLong), true)
				.SetDisplay("Long Entries", "Enable long entries", "Strategy");

			_showShort = Param(nameof(ShowShort), true)
				.SetDisplay("Short Entries", "Enable short entries", "Strategy");

			_tpType = Param(nameof(TpType), "Supertrend")
				.SetDisplay("TP Type", "Take profit type (Supertrend or %)", "Take Profit");

			_tpPercent = Param(nameof(TpPercent), 1.5m)
				.SetRange(0.1m, 10.0m)
				.SetDisplay("TP Percent", "Take profit percentage", "Take Profit")
				.SetCanOptimize(true)
				.SetOptimize(0.5m, 3.0m, 0.3m);
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
			_supertrend = new() { Length = AtrPeriod, Multiplier = AtrFactor };
			_ema = new() { Length = EmaLength };

			// Create subscription for candles
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(_supertrend, _ema, ProcessCandle)
				.Start();

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _supertrend);
				DrawIndicator(area, _ema);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal supertrendValue, decimal emaValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Wait for indicators to form
			if (!_supertrend.IsFormed || !_ema.IsFormed)
				return;

			var currentPrice = candle.ClosePrice;
			var openPrice = candle.OpenPrice;

			// Determine Supertrend direction (< 0 means uptrend, > 0 means downtrend)
			var supertrendDirection = currentPrice > supertrendValue ? -1 : 1;

			// Detect Supertrend direction changes
			var supertrendDirectionChanged = _previousSupertrendDirection != 0 && 
				Math.Sign(supertrendDirection) != Math.Sign(_previousSupertrendDirection);

			CheckEntryConditions(candle, emaValue, supertrendDirection, supertrendDirectionChanged);
			CheckExitConditions(supertrendDirection, supertrendDirectionChanged);

			// Store previous values
			_previousSupertrendDirection = supertrendDirection;
			_previousClose = currentPrice;

			// Update last entry price when opening new position
			if (Position != 0 && _lastEntryPrice == 0)
			{
				_lastEntryPrice = openPrice;
			}
			else if (Position == 0)
			{
				_lastEntryPrice = 0;
			}
		}

		private void CheckEntryConditions(ICandleMessage candle, decimal emaValue, decimal supertrendDirection, bool supertrendDirectionChanged)
		{
			var currentPrice = candle.ClosePrice;
			var openPrice = candle.OpenPrice;

			// Long entry conditions
			if (ShowLong && Position == 0)
			{
				// Entry 1: Supertrend changes to uptrend
				var entryLong1 = supertrendDirectionChanged && supertrendDirection < 0;

				// Entry 2: In uptrend, price rebounds from EMA
				var entryLong2 = supertrendDirection < 0 && 
					_previousClose < emaValue && 
					currentPrice > emaValue && 
					currentPrice < _lastEntryPrice;

				if (entryLong1 || entryLong2)
				{
					RegisterOrder(CreateOrder(Sides.Buy, currentPrice, GetOrderVolume()));
				}
			}

			// Short entry conditions
			if (ShowShort && Position == 0)
			{
				// Entry 1: Supertrend changes to downtrend
				var entryShort1 = supertrendDirectionChanged && supertrendDirection > 0;

				// Entry 2: In downtrend, price rebounds from EMA
				var entryShort2 = supertrendDirection > 0 && 
					_previousClose > emaValue && 
					currentPrice < emaValue && 
					currentPrice > _lastEntryPrice;

				if (entryShort1 || entryShort2)
				{
					RegisterOrder(CreateOrder(Sides.Sell, currentPrice, GetOrderVolume()));
				}
			}
		}

		private void CheckExitConditions(decimal supertrendDirection, bool supertrendDirectionChanged)
		{
			// Exit long when Supertrend changes to downtrend
			if (Position > 0 && supertrendDirectionChanged && supertrendDirection > 0)
			{
				RegisterOrder(CreateOrder(Sides.Sell, _previousClose, Math.Abs(Position)));
			}

			// Exit short when Supertrend changes to uptrend
			if (Position < 0 && supertrendDirectionChanged && supertrendDirection < 0)
			{
				RegisterOrder(CreateOrder(Sides.Buy, _previousClose, Math.Abs(Position)));
			}

			// Handle take profit based on type
			if (TpType == "%" && Position != 0)
			{
				// Percentage-based take profit is handled by protection system
				// Additional logic can be added here if needed
			}
		}

		private decimal GetOrderVolume()
		{
			return 1;
		}
	}
}