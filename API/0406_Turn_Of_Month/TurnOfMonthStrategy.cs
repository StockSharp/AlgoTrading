// TurnOfMonthStrategy.cs
// -----------------------------------------------------------------------------
// Holds index ETFs only around turn-of-the-month window.
// Default: long SPY from (N=1) trading day before month‑end close
//          until D=3 trading day of new month close.
// Trigger: daily candle of ETF.  No Schedule().
// -----------------------------------------------------------------------------
// Date: 2 Aug 2025
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Turn-of-the-month effect strategy for index ETFs.
	/// </summary>
	public class TurnOfMonthStrategy : Strategy
	{
		private readonly StrategyParam<int> _prior;
		private readonly StrategyParam<int> _after;
		private readonly StrategyParam<decimal> _minUsd;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// The type of candles to use for strategy calculation.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private int _tdMonthEnd = int.MaxValue;
		private int _tdMonthStart = 0;

		/// <summary>
		/// Number of trading days before month-end to enter.
		/// </summary>
		public int DaysPrior
		{
			get => _prior.Value;
			set => _prior.Value = value;
		}

		/// <summary>
		/// Number of trading days into new month to exit.
		/// </summary>
		public int DaysAfter
		{
			get => _after.Value;
			set => _after.Value = value;
		}

		/// <summary>
		/// Minimum trade value in USD.
		/// </summary>
		public decimal MinTradeUsd
		{
			get => _minUsd.Value;
			set => _minUsd.Value = value;
		}

		public TurnOfMonthStrategy()
		{
			_prior = Param(nameof(DaysPrior), 1)
				.SetDisplay("Days Prior", "Trading days before month end", "Parameters");

			_after = Param(nameof(DaysAfter), 3)
				.SetDisplay("Days After", "Trading days into new month", "Parameters");

			_minUsd = Param(nameof(MinTradeUsd), 200m)
				.SetDisplay("Min Trade USD", "Minimum notional value for orders", "Risk Management");
			_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
		{
			if (Security == null)
				throw new InvalidOperationException("Set ETF");

			yield return (Security, CandleType);
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			if (Security == null)
				throw new InvalidOperationException("ETF must be set.");

			base.OnStarted(time);

			SubscribeCandles(CandleType, true, Security)
				.Bind(c => ProcessCandle(c, Security))
				.Start();
		}

		private void ProcessCandle(ICandleMessage candle, Security security)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Store the latest closing price for this security
			_latestPrices[security] = candle.ClosePrice;

			OnDaily(candle.OpenTime.Date);
		}

		private void OnDaily(DateTime d)
		{
			_tdMonthEnd = TradingDaysLeftInMonth(d);
			_tdMonthStart = TradingDayNumber(d);

			bool inWindow = (_tdMonthEnd <= DaysPrior) || (_tdMonthStart <= DaysAfter);
			var portfolioValue = Portfolio.CurrentValue ?? 0m;
			var price = GetLatestPrice(Security);
			var tgt = inWindow && price > 0 ? portfolioValue / price : 0;
			TradeTo(tgt);
		}

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}

		private void TradeTo(decimal tgtQty)
		{
			var diff = tgtQty - PositionBy(Security);
			var price = GetLatestPrice(Security);
			if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd)
				return;

			RegisterOrder(new Order
			{
				Security = Security,
				Portfolio = Portfolio,
				Side = diff > 0 ? Sides.Buy : Sides.Sell,
				Volume = Math.Abs(diff),
				Type = OrderTypes.Market,
				Comment = "TurnMonth"
			});
		}

		private int TradingDaysLeftInMonth(DateTime d)
		{
			int cnt = 0;
			var cur = d;
			while (cur.Month == d.Month)
			{ 
				// Simple approximation: assume weekdays are trading days (Monday-Friday)
				if (cur.DayOfWeek != DayOfWeek.Saturday && cur.DayOfWeek != DayOfWeek.Sunday) 
					cnt++; 
				cur = cur.AddDays(1); 
			}
			return cnt - 1;
		}

		private int TradingDayNumber(DateTime d)
		{
			int n = 0;
			var cur = new DateTime(d.Year, d.Month, 1);
			while (cur <= d)
			{ 
				// Simple approximation: assume weekdays are trading days (Monday-Friday)
				if (cur.DayOfWeek != DayOfWeek.Saturday && cur.DayOfWeek != DayOfWeek.Sunday) 
					n++; 
				cur = cur.AddDays(1); 
			}
			return n;
		}
		private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;
	}
}