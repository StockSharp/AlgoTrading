using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Vela Superada Strategy - trades on candle pattern reversals with EMA, RSI and MACD filters
	/// </summary>
	public class VelaSuperadaStrategy : Strategy
	{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _emaLength;
		private readonly StrategyParam<int> _rsiLength;
		private readonly StrategyParam<bool> _showLong;
		private readonly StrategyParam<bool> _showShort;
		private readonly StrategyParam<decimal> _tpPercent;
		private readonly StrategyParam<decimal> _slPercent;

		private ExponentialMovingAverage _ema;
		private RelativeStrengthIndex _rsi;
		private MovingAverageConvergenceDivergence _macd;

		private decimal _previousClose;
		private decimal _previousOpen;
		private decimal _previousMacd;
		private decimal _trailingStopLong;
		private decimal _trailingStopShort;
		private decimal _entryPrice;

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
		/// RSI calculation length.
		/// </summary>
		public int RsiLength
		{
			get => _rsiLength.Value;
			set => _rsiLength.Value = value;
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
		/// Take profit percentage.
		/// </summary>
		public decimal TpPercent
		{
			get => _tpPercent.Value;
			set => _tpPercent.Value = value;
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
		public VelaSuperadaStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_emaLength = Param(nameof(EmaLength), 10)
				.SetGreaterThanZero()
				.SetDisplay("EMA Length", "EMA period", "Moving Averages")
				.SetCanOptimize(true)
				.SetOptimize(5, 25, 5);

			_rsiLength = Param(nameof(RsiLength), 14)
				.SetGreaterThanZero()
				.SetDisplay("RSI Length", "RSI calculation length", "RSI")
				.SetCanOptimize(true)
				.SetOptimize(7, 21, 2);

			_showLong = Param(nameof(ShowLong), true)
				.SetDisplay("Long Entries", "Enable long entries", "Strategy");

			_showShort = Param(nameof(ShowShort), false)
				.SetDisplay("Short Entries", "Enable short entries", "Strategy");

			_tpPercent = Param(nameof(TpPercent), 1.2m)
				.SetDisplay("TP Percent", "Take profit percentage", "Take Profit")
				.SetCanOptimize(true)
				.SetOptimize(0.5m, 3.0m, 0.3m);

			_slPercent = Param(nameof(SlPercent), 1.8m)
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
		protected override void OnReseted()
		{
			base.OnReseted();

			_entryPrice = default;
			_previousClose = default;
			_previousOpen = default;
			_previousMacd = default;
			_trailingStopLong = default;
			_trailingStopShort = default;
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Initialize indicators
			_ema = new ExponentialMovingAverage { Length = EmaLength };
			_rsi = new RelativeStrengthIndex { Length = RsiLength };
			_macd = new MovingAverageConvergenceDivergence 
			{ 
				ShortMa = { Length = 12 }, 
				LongMa = { Length = 26 }
			};

			// Create subscription for candles
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(_ema, _rsi, _macd, ProcessCandle)
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
			StartProtection(new Unit(TpPercent / 100m, UnitTypes.Percent), new Unit(SlPercent / 100m, UnitTypes.Percent));
		}

		private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaValue, IIndicatorValue rsiValue, IIndicatorValue macdValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Wait for indicators to form
			if (!_ema.IsFormed || !_rsi.IsFormed || !_macd.IsFormed)
				return;

			var currentPrice = candle.ClosePrice;
			var openPrice = candle.OpenPrice;

			// Detect candle patterns
			var bullishPattern = _previousClose < _previousOpen && currentPrice > openPrice; // Previous red, current green
			var bearishPattern = _previousClose > _previousOpen && currentPrice < openPrice; // Previous green, current red

			CheckEntryConditions(candle, emaValue.ToDecimal(), rsiValue.ToDecimal(), macdValue.ToDecimal(), bullishPattern, bearishPattern);
			UpdateTrailingStops(candle);

			// Store previous values
			_previousClose = currentPrice;
			_previousOpen = openPrice;
			_previousMacd = macdValue.ToDecimal();

			// Update entry price when position opened
			if (Position != 0 && _entryPrice == 0)
			{
				_entryPrice = openPrice;
			}
			else if (Position == 0)
			{
				_entryPrice = 0;
				_trailingStopLong = 0;
				_trailingStopShort = 0;
			}
		}

		private void CheckEntryConditions(ICandleMessage candle, decimal emaValue, decimal rsiValue, decimal macdValue, bool bullishPattern, bool bearishPattern)
		{
			var currentPrice = candle.ClosePrice;

			// Long entry: bullish pattern, close > EMA, previous close > EMA, RSI < 65, MACD rising
			if (ShowLong && 
				bullishPattern && 
				currentPrice > emaValue && 
				_previousClose > emaValue && 
				rsiValue < 65 && 
				macdValue > _previousMacd && 
				Position == 0)
			{
				RegisterOrder(CreateOrder(Sides.Buy, currentPrice, Volume));
			}

			// Short entry: bearish pattern, close < EMA, previous close < EMA, RSI > 35, MACD falling
			if (ShowShort && 
				bearishPattern && 
				currentPrice < emaValue && 
				_previousClose < emaValue && 
				rsiValue > 35 && 
				macdValue < _previousMacd && 
				Position == 0)
			{
				RegisterOrder(CreateOrder(Sides.Sell, currentPrice, Volume));
			}
		}

		private void UpdateTrailingStops(ICandleMessage candle)
		{
			if (Position == 0 || _entryPrice == 0)
				return;

			var currentPrice = candle.ClosePrice;
			var avgTpPrice = (_entryPrice * (1 + TpPercent / 100m) + _entryPrice) / 2;

			// Update trailing stop for long positions
			if (Position > 0)
			{
				var basicStop = _entryPrice * (1 - SlPercent / 100m);
				
				if (currentPrice > avgTpPrice)
				{
					// Move to breakeven plus small profit when above average TP
					_trailingStopLong = _entryPrice * 1.002m;
				}
				else
				{
					// Use higher of current trailing stop or basic stop
					_trailingStopLong = Math.Max(_trailingStopLong, basicStop);
				}
			}

			// Update trailing stop for short positions
			if (Position < 0)
			{
				var basicStop = _entryPrice * (1 + SlPercent / 100m);
				var avgTpPriceShort = (_entryPrice * (1 - TpPercent / 100m) + _entryPrice) / 2;
				
				if (currentPrice < avgTpPriceShort)
				{
					// Move to breakeven minus small profit when below average TP
					_trailingStopShort = _entryPrice * 0.998m;
				}
				else
				{
					// Use lower of current trailing stop or basic stop
					_trailingStopShort = _trailingStopShort == 0 ? basicStop : Math.Min(_trailingStopShort, basicStop);
				}
			}
		}
	}
}