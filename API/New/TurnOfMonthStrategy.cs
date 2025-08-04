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
	public class TurnOfMonthStrategy : Strategy
	{
		private readonly StrategyParam<Security> _etf;
		private readonly StrategyParam<int> _prior;
		private readonly StrategyParam<int> _after;
		private readonly StrategyParam<decimal> _minUsd;
		private readonly DataType _tf = TimeSpan.FromDays(1).TimeFrame();

		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private int _tdMonthEnd = int.MaxValue;
		private int _tdMonthStart = 0;

		public Security ETF { get => _etf.Value; set => _etf.Value = value; }
		public int DaysPrior => _prior.Value;  // enter N days before month-end
		public int DaysAfter => _after.Value;  // exit D days into new month
		public decimal MinTradeUsd => _minUsd.Value;

		public TurnOfMonthStrategy()
		{
			_etf = Param<Security>(nameof(ETF), null);
			_prior = Param(nameof(DaysPrior), 1);
			_after = Param(nameof(DaysAfter), 3);
			_minUsd = Param(nameof(MinTradeUsd), 200m);
		}

		public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
		{
			if (ETF == null)
				throw new InvalidOperationException("Set ETF");
			yield return (ETF, _tf);
		}

		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			SubscribeCandles(_tf, true, ETF)
				.Bind(c => ProcessCandle(c, ETF))
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
			var price = GetLatestPrice(ETF);
			var tgt = inWindow && price > 0 ? portfolioValue / price : 0;
			TradeTo(tgt);
		}

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}

		private void TradeTo(decimal tgtQty)
		{
			var diff = tgtQty - PositionBy(ETF);
			var price = GetLatestPrice(ETF);
			if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd)
				return;

			RegisterOrder(new Order
			{
				Security = ETF,
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
			{ if (Exchange.IsTradingDay(cur)) cnt++; cur = cur.AddDays(1); }
			return cnt - 1;
		}
		private int TradingDayNumber(DateTime d)
		{
			int n = 0;
			var cur = new DateTime(d.Year, d.Month, 1);
			while (cur <= d)
			{ if (Exchange.IsTradingDay(cur)) n++; cur = cur.AddDays(1); }
			return n;
		}
		private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;
	}
}