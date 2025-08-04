// CurrencyMomentumFactorStrategy.cs (full, candle-driven)
// Long top-K momentum currencies, short bottom-K; monthly rebalance.
// Date: 2 August 2025

using System;
using System.Collections.Generic;
using System.Linq;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	public class CurrencyMomentumFactorStrategy : Strategy
	{
		private readonly StrategyParam<IEnumerable<Security>> _universe;
		private readonly StrategyParam<int> _lookback;
		private readonly StrategyParam<int> _k;
		private readonly StrategyParam<DataType> _tf;
		private readonly StrategyParam<decimal> _minUsd;

		public IEnumerable<Security> Universe { get => _universe.Value; set => _universe.Value = value; }
		public int Lookback => _lookback.Value;
		public int K => _k.Value;
		public DataType CandleType => _tf.Value;
		public decimal MinTradeUsd => _minUsd.Value;

		private readonly Dictionary<Security, RollingWindow<decimal>> _wins = new();
		private readonly Dictionary<Security, decimal> _w = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private DateTime _lastDay = DateTime.MinValue;

		public CurrencyMomentumFactorStrategy()
		{
			_universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>());
			_lookback = Param(nameof(Lookback), 252);
			_k = Param(nameof(K), 3);
			_tf = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame());
			_minUsd = Param(nameof(MinTradeUsd), 100m);
		}

		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() =>
			Universe.Select(s => (s, CandleType));

		protected override void OnStarted(DateTimeOffset t)
		{
			base.OnStarted(t);
			foreach (var (s, dt) in GetWorkingSecurities())
			{
				_wins[s] = new RollingWindow<decimal>(Lookback + 1);
				SubscribeCandles(dt, true, s)
					.Bind(c => ProcessCandle(c, s))
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

			HandleDaily(candle, security);
		}

		private void HandleDaily(ICandleMessage c, Security s)
		{
			_wins[s].Add(c.ClosePrice);
			var d = c.OpenTime.Date;
			if (d == _lastDay)
				return;
			_lastDay = d;
			if (d.Day == 1)
				Rebalance();
		}

		private void Rebalance()
		{
			if (_wins.Values.Any(w => !w.IsFull()))
				return;
			var mom = _wins.ToDictionary(kv => kv.Key, kv => (kv.Value.Last() - kv.Value[0]) / kv.Value[0]);
			var top = mom.OrderByDescending(kv => kv.Value).Take(K).Select(kv => kv.Key).ToList();
			var bot = mom.OrderBy(kv => kv.Value).Take(K).Select(kv => kv.Key).ToList();

			_w.Clear();
			decimal wl = 1m / top.Count, ws = -1m / bot.Count;
			foreach (var s in top)
				_w[s] = wl;
			foreach (var s in bot)
				_w[s] = ws;

			foreach (var position in Positions)
				if (!_w.ContainsKey(position.Security))
					Move(position.Security, 0);

			var portfolioValue = Portfolio.CurrentValue ?? 0m;
			foreach (var kv in _w)
			{
				var price = GetLatestPrice(kv.Key);
				if (price > 0)
					Move(kv.Key, kv.Value * portfolioValue / price);
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
			RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "CurrMom" });
		}
		private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;

		#region RollingWindow
		private class RollingWindow<T>
		{
			private readonly Queue<T> _q = new();
			private readonly int _n;
			public RollingWindow(int n) { _n = n; }
			public void Add(T v) { if (_q.Count == _n) _q.Dequeue(); _q.Enqueue(v); }
			public bool IsFull() => _q.Count == _n;
			public T Last() => _q.Last();
			public T this[int i] => _q.ElementAt(i);
		}
		#endregion

	}
}