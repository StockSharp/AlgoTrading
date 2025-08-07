// EarningsAnnouncementPremiumStrategy.cs
// ------------------------------------------------------------
// Long DaysBefore days BEFORE earnings announcement,
// exit DaysAfter days AFTER announcement.
// Candle-stream style: SubscribeCandles → Bind(CandleStates.Finished) → DailyScan once per day.
// Date: 2 August 2025
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Earnings announcement premium strategy.
	/// Buys <see cref="DaysBefore"/> days before an earnings announcement
	/// and exits <see cref="DaysAfter"/> days after the announcement.
	/// </summary>
	public class EarningsAnnouncementPremiumStrategy : Strategy
	{
		#region Parameters
		private readonly StrategyParam<IEnumerable<Security>> _universe;
		private readonly StrategyParam<int> _daysBefore;
		private readonly StrategyParam<int> _daysAfter;
		private readonly StrategyParam<decimal> _capitalUsd;
		private readonly StrategyParam<decimal> _minUsd;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// The securities universe.
		/// </summary>
		public IEnumerable<Security> Universe
		{
			get => _universe.Value;
			set => _universe.Value = value;
		}

		/// <summary>
		/// Number of days before earnings to enter.
		/// </summary>
		public int DaysBefore
		{
			get => _daysBefore.Value;
			set => _daysBefore.Value = value;
		}

		/// <summary>
		/// Number of days after earnings to exit.
		/// </summary>
		public int DaysAfter
		{
			get => _daysAfter.Value;
			set => _daysAfter.Value = value;
		}

		/// <summary>
		/// Capital per trade in USD.
		/// </summary>
		public decimal CapitalPerTradeUsd
		{
			get => _capitalUsd.Value;
			set => _capitalUsd.Value = value;
		}

		/// <summary>
		/// Minimum trade value in USD.
		/// </summary>
		public decimal MinTradeUsd
		{
			get => _minUsd.Value;
			set => _minUsd.Value = value;
		}

		/// <summary>
		/// The candle type to use for calculations.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}
		#endregion

		private readonly Dictionary<Security, DateTimeOffset> _exitSchedule = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private DateTime _lastProcessed = DateTime.MinValue;

		/// <summary>
		/// Initializes a new instance of <see cref="EarningsAnnouncementPremiumStrategy"/>.
		/// </summary>
		public EarningsAnnouncementPremiumStrategy()
		{
			_universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>())
				.SetDisplay("Universe", "Securities to trade", "General");

			_daysBefore = Param(nameof(DaysBefore), 5)
				.SetDisplay("Days Before", "Days before earnings to enter", "General");

			_daysAfter = Param(nameof(DaysAfter), 1)
				.SetDisplay("Days After", "Days after earnings to exit", "General");

			_capitalUsd = Param(nameof(CapitalPerTradeUsd), 5000m)
				.SetDisplay("Capital per Trade (USD)", "Capital allocated per trade", "Risk");

			_minUsd = Param(nameof(MinTradeUsd), 100m)
				.SetDisplay("Minimum Trade (USD)", "Minimal trade value", "Risk");

			_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to process", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() =>
			Universe.Select(s => (s, CandleType));

		/// <inheritdoc />
		
		protected override void OnReseted()
		{
			base.OnReseted();

			_exitSchedule.Clear();
			_latestPrices.Clear();
			_lastProcessed = default;
		}

		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			if (Universe == null || !Universe.Any())
				throw new InvalidOperationException("Universe is empty.");

			foreach (var (sec, tf) in GetWorkingSecurities())
			{
				SubscribeCandles(tf, true, sec)
					.Bind(c => ProcessCandle(c, sec))
					.Start();
			}
		}

		private void ProcessCandle(ICandleMessage candle, Security security)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Store the latest closing price for this security
			_latestPrices[security] = candle.ClosePrice;

			var day = candle.OpenTime.Date;
			if (day == _lastProcessed)
				return;
			_lastProcessed = day;
			DailyScan(day);
		}

		private void DailyScan(DateTime today)
		{
			/* -------- Entries -------- */
			foreach (var stock in Universe)
			{
				if (!TryGetNextEarningsDate(stock, out var earnDate))
					continue;

				var diff = (earnDate.Date - today).TotalDays;
				if (diff == DaysBefore && !_exitSchedule.ContainsKey(stock))
				{
					var price = GetLatestPrice(stock);
					if (price <= 0)
						continue;
						
					var qty = CapitalPerTradeUsd / price;
					if (qty * price >= MinTradeUsd)
					{
						Place(stock, qty, Sides.Buy, "Enter");
						_exitSchedule[stock] = earnDate.Date.AddDays(DaysAfter);
					}
				}
			}

			/* -------- Exits -------- */
			foreach (var kv in _exitSchedule.ToList())
			{
				if (today < kv.Value)
					continue;
				var pos = PositionBy(kv.Key);
				if (pos > 0)
					Place(kv.Key, pos, Sides.Sell, "Exit");
				_exitSchedule.Remove(kv.Key);
			}
		}

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}

		#region Helpers
		private void Place(Security s, decimal qty, Sides side, string tag)
		{
			RegisterOrder(new Order
			{
				Security = s,
				Portfolio = Portfolio,
				Side = side,
				Volume = qty,
				Type = OrderTypes.Market,
				Comment = $"EarnPrem-{tag}"
			});
		}

		private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;
		#endregion

		#region External data stub
		private bool TryGetNextEarningsDate(Security s, out DateTimeOffset dt)
		{
			dt = DateTimeOffset.MinValue;
			return false; // TODO
		}
		#endregion
	}
}
