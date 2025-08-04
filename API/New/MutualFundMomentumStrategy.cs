// MutualFundMomentumStrategy.cs — candle-triggered (daily)
// Quarterly rebalance triggered by first fund's daily candle.
// Date: 2 Aug 2025

using System;
using System.Collections.Generic;
using System.Linq;
using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Algo.Candles;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	public class MutualFundMomentumStrategy : Strategy
	{
		private readonly StrategyParam<IEnumerable<Security>> _funds;
		private readonly StrategyParam<decimal> _minUsd;
		private readonly DataType _tf = TimeSpan.FromDays(1).TimeFrame();

		public IEnumerable<Security> Funds { get => _funds.Value; set => _funds.Value = value; }
		public decimal MinTradeUsd => _minUsd.Value;

		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private DateTime _lastDay = DateTime.MinValue;

		public MutualFundMomentumStrategy()
		{
			_funds = Param<IEnumerable<Security>>(nameof(Funds), Array.Empty<Security>());
			_minUsd = Param(nameof(MinTradeUsd), 200m);
		}

		public override IEnumerable<(Security, DataType)> GetWorkingSecurities() =>
			Funds.Select(f => (f, _tf));

		protected override void OnStarted(DateTimeOffset t)
		{
			base.OnStarted(t);
			var trigger = Funds.FirstOrDefault() ?? throw new InvalidOperationException("Funds empty.");

			SubscribeCandles(_tf, true, trigger)
				.Bind(c => ProcessCandle(c, trigger))
				.Start();
		}

		private void ProcessCandle(ICandleMessage candle, Security security)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Store the latest closing price for this security
			_latestPrices[security] = candle.ClosePrice;

			var d = candle.OpenTime.Date;
			if (d == _lastDay)
				return;
			_lastDay = d;

			if (IsQuarterRebalanceDay(d))
				Rebalance();
		}

		private bool IsQuarterRebalanceDay(DateTime d) =>
			d.Month % 3 == 0 && d.Day <= 3;

		private void Rebalance()
		{
			var perf = new Dictionary<Security, decimal>();
			foreach (var f in Funds)
				if (TryGetNAV6m(f, out var nav6, out var nav0))
					perf[f] = (nav0 - nav6) / nav6;

			if (perf.Count < 10)
				return;
			int dec = perf.Count / 10;
			var longs = perf.OrderByDescending(kv => kv.Value).Take(dec).Select(kv => kv.Key).ToList();

			foreach (var position in Positions)
				if (!longs.Contains(position.Security))
					Move(position.Security, 0);

			decimal w = 1m / longs.Count;
			var portfolioValue = Portfolio.CurrentValue ?? 0m;
			foreach (var s in longs)
			{
				var price = GetLatestPrice(s);
				if (price > 0)
					Move(s, w * portfolioValue / price);
			}
		}

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}

		private void Move(Security s, decimal tgt)
		{
			var diff = tgt - PositionBy(s);
			var price = GetLatestPrice(s);
			if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd)
				return;
			RegisterOrder(new Order
			{
				Security = s,
				Portfolio = Portfolio,
				Side = diff > 0 ? Sides.Buy : Sides.Sell,
				Volume = Math.Abs(diff),
				Type = OrderTypes.Market,
				Comment = "MutualMom"
			});
		}

		private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;
		private bool TryGetNAV6m(Security f, out decimal nav6, out decimal nav0) { nav6 = nav0 = 0; return false; }
	}
}
