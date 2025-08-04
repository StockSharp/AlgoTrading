// ReturnAsymmetryCommodityStrategy.cs
// -----------------------------------------------------------------------------
// Uses return asymmetry over past WindowDays for each commodity future:
// asym = Σ positive returns / |Σ negative returns|.
// Monthly (first trading day) long TopN asymmetry, short BottomN.
// Trigger via daily candle of first future (no Schedule).
// External data: none beyond prices.
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
	public class ReturnAsymmetryCommodityStrategy : Strategy
	{
		#region Params
		private readonly StrategyParam<IEnumerable<Security>> _futs;
		private readonly StrategyParam<int> _window;
		private readonly StrategyParam<int> _top;
		private readonly StrategyParam<decimal> _minUsd;
		private readonly DataType _tf = TimeSpan.FromDays(1).TimeFrame();

		public IEnumerable<Security> Futures { get => _futs.Value; set => _futs.Value = value; }
		public int WindowDays => _window.Value;
		public int TopN => _top.Value;
		public decimal MinTradeUsd => _minUsd.Value;
		#endregion

		private class Win { public Queue<decimal> Px = new(); }
		private readonly Dictionary<Security, Win> _map = new();
		private DateTime _lastDay = DateTime.MinValue;
		private readonly Dictionary<Security, decimal> _w = new();

		public ReturnAsymmetryCommodityStrategy()
		{
			_futs = Param<IEnumerable<Security>>(nameof(Futures), Array.Empty<Security>());
			_window = Param(nameof(WindowDays), 120);
			_top = Param(nameof(TopN), 5);
			_minUsd = Param(nameof(MinTradeUsd), 200m);
		}

		public override IEnumerable<(Security, DataType)> GetWorkingSecurities() =>
			Futures.Select(s => (s, _tf));

		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			foreach (var (sec, dt) in GetWorkingSecurities())
			{
				_map[sec] = new Win();
				SubscribeCandles(dt, true, sec)
					.Bind(c => OnDaily((Security)c.SecurityId, c))
					.Start();
			}
		}

		private void OnDaily(Security s, ICandleMessage c)
		{
			var q = _map[s].Px;
			if (q.Count == WindowDays)
				q.Dequeue();
			q.Enqueue(c.ClosePrice);

			var d = c.OpenTime.Date;
			if (d == _lastDay)
				return;
			_lastDay = d;

			if (d.Day != 1)
				return;
			Rebalance();
		}

		private void Rebalance()
		{
			var asym = new Dictionary<Security, decimal>();
			foreach (var kv in _map)
			{
				var q = kv.Value.Px;
				if (q.Count < WindowDays)
					continue;
				var arr = q.ToArray();
				decimal pos = 0, neg = 0;
				for (int i = 1; i < arr.Length; i++)
				{
					var r = (arr[i] - arr[i - 1]) / arr[i - 1];
					if (r > 0)
						pos += r;
					else
						neg += r;
				}
				if (neg == 0)
					continue;
				asym[kv.Key] = pos / Math.Abs(neg);
			}

			if (asym.Count < TopN * 2)
				return;
			var longs = asym.OrderByDescending(kv => kv.Value).Take(TopN).Select(kv => kv.Key).ToList();
			var shorts = asym.OrderBy(kv => kv.Value).Take(TopN).Select(kv => kv.Key).ToList();

			_w.Clear();
			decimal wl = 1m / longs.Count;
			decimal ws = -1m / shorts.Count;
			foreach (var s in longs)
				_w[s] = wl;
			foreach (var s in shorts)
				_w[s] = ws;

			foreach (var pos in Positions.Keys.Where(sec => !_w.ContainsKey(sec)))
				Move(pos, 0);

			foreach (var kv in _w)
				Move(kv.Key, kv.Value * Portfolio.CurrentValue / kv.Key.Price);
		}

		private void Move(Security s, decimal tgtQty)
		{
			var diff = tgtQty - PositionBy(s);
			if (Math.Abs(diff) * s.Price < MinTradeUsd)
				return;
			RegisterOrder(new Order
			{
				Security = s,
				Portfolio = Portfolio,
				Side = diff > 0 ? Sides.Buy : Sides.Sell,
				Volume = Math.Abs(diff),
				Type = OrderTypes.Market,
				Comment = "AsymCom"
			});
		}

		private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;
	}
}
