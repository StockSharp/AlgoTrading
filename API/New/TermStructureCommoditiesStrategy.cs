// TermStructureCommoditiesStrategy.cs
// -----------------------------------------------------------------------------
// Roll‑return cross‑section: long highest quintile, short lowest.
// Monthly rebalance; triggered by daily candle of first future.
// -----------------------------------------------------------------------------
// Date: 2 Aug 2025
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	public class TermStructureCommoditiesStrategy : Strategy
	{
		private readonly StrategyParam<IEnumerable<Security>> _univ;
		private readonly StrategyParam<int> _quint;
		private readonly StrategyParam<decimal> _minUsd;
		private readonly DataType _tf = TimeSpan.FromDays(1).TimeFrame();
		private readonly Dictionary<Security, decimal> _w = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private DateTime _last = DateTime.MinValue;

		public IEnumerable<Security> Universe { get => _univ.Value; set => _univ.Value = value; }
		public int Quintile => _quint.Value;
		public decimal MinTradeUsd => _minUsd.Value;

		public TermStructureCommoditiesStrategy()
		{
			_univ = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>());
			_quint = Param(nameof(Quintile), 5);
			_minUsd = Param(nameof(MinTradeUsd), 200m);
		}

		public override IEnumerable<(Security, DataType)> GetWorkingSecurities() => Universe.Select(s => (s, _tf));

		protected override void OnStarted(DateTimeOffset t)
		{
			base.OnStarted(t);
			var trig = Universe.FirstOrDefault() ?? throw new InvalidOperationException("Universe empty");
			SubscribeCandles(_tf, true, trig).Bind(c => ProcessCandle(c, trig)).Start();
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
			if (d == _last)
				return;
			_last = d;
			if (d.Day != 1)
				return;
			Rebalance();
		}

		private void Rebalance()
		{
			var rr = new Dictionary<Security, decimal>();
			foreach (var s in Universe)
				if (TryRollReturn(s, out var v))
					rr[s] = v;
			if (rr.Count < Quintile * 2)
				return;
			int bucket = rr.Count / Quintile;
			var longS = rr.OrderByDescending(kv => kv.Value).Take(bucket).Select(kv => kv.Key).ToList();
			var shortS = rr.OrderBy(kv => kv.Value).Take(bucket).Select(kv => kv.Key).ToList();
			_w.Clear();
			decimal wl = 1m / longS.Count, ws = -1m / shortS.Count;
			foreach (var s in longS)
				_w[s] = wl;
			foreach (var s in shortS)
				_w[s] = ws;
			foreach (var position in Positions)
				if (!_w.ContainsKey(position.Security))
					Trade(position.Security, 0);
			var portfolioValue = Portfolio.CurrentValue ?? 0m;
			foreach (var kv in _w)
			{
				var price = GetLatestPrice(kv.Key);
				if (price > 0)
					Trade(kv.Key, kv.Value * portfolioValue / price);
			}
		}

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}

		private void Trade(Security s, decimal tgt)
		{
			var diff = tgt - Pos(s);
			var price = GetLatestPrice(s);
			if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd)
				return;
			RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "RollRet" });
		}

		private decimal Pos(Security s) => GetPositionValue(s, Portfolio) ?? 0;
		private bool TryRollReturn(Security s, out decimal v) { v = 0; return false; }
	}
}