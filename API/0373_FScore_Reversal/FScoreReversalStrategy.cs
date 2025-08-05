// FScoreReversalStrategy.cs
// Combines Piotroski F‑Score with 1‑month reversal.
// Long losers (1‑month return < 0) with FScore ≥ 7; short winners (return > 0) with FScore ≤ 3.
// Monthly rebalance. F‑Score feed must be provided via TryGetFScore.
// Date: 2 August 2025

using System;
using System.Collections.Generic;
using System.Linq;
using Ecng.Common;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Piotroski F-Score reversal strategy combining fundamental strength
	/// with 1-month price reversal.
	/// </summary>
	public class FScoreReversalStrategy : Strategy
	{
		#region Params
		private readonly StrategyParam<IEnumerable<Security>> _universe;
		private readonly StrategyParam<int> _lookback;
		private readonly StrategyParam<int> _hi;
		private readonly StrategyParam<int> _lo;
		private readonly StrategyParam<decimal> _minUsd;
		
		/// <summary>
		/// Securities universe to trade.
		/// </summary>
		public IEnumerable<Security> Universe { get => _universe.Value; set => _universe.Value = value; }
		
		/// <summary>
		/// Lookback period for 1-month return.
		/// </summary>
		public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }
		
		/// <summary>
		/// Minimum F-Score for long positions.
		/// </summary>
		public int FHi { get => _hi.Value; set => _hi.Value = value; }
		
		/// <summary>
		/// Maximum F-Score for short positions.
		/// </summary>
		public int FLo { get => _lo.Value; set => _lo.Value = value; }
		
		/// <summary>
		/// Minimum trade value in USD.
		/// </summary>
		public decimal MinTradeUsd { get => _minUsd.Value; set => _minUsd.Value = value; }
		#endregion

		private readonly Dictionary<Security, FScoreRollingWindow> _prices = new();
		private readonly Dictionary<Security, decimal> _ret = new();
		private readonly Dictionary<Security, int> _fscore = new();
		private readonly Dictionary<Security, decimal> _w = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private DateTime _lastRebalance = DateTime.MinValue;

		public FScoreReversalStrategy()
		{
			_universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>())
				.SetDisplay("Universe", "Securities universe", "General");
			
			_lookback = Param(nameof(Lookback), 21)
				.SetGreaterThanZero()
				.SetDisplay("Lookback", "Lookback period in days", "General");
			
			_hi = Param(nameof(FHi), 7)
				.SetDisplay("High F-Score", "Minimum F-Score for longs", "General");
			
			_lo = Param(nameof(FLo), 3)
				.SetDisplay("Low F-Score", "Maximum F-Score for shorts", "General");
			
			_minUsd = Param(nameof(MinTradeUsd), 50m)
				.SetGreaterThanZero()
				.SetDisplay("Min trade USD", "Minimum order value", "Risk");
		}

		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			if (!Universe.Any())
				throw new InvalidOperationException("Universe empty");
			var tf = TimeSpan.FromDays(1).TimeFrame();
			return Universe.Select(s => (s, tf));
		}

		protected override void OnStarted(DateTimeOffset time)
		{
			if (Universe == null || !Universe.Any())
				throw new InvalidOperationException("Universe empty");

			base.OnStarted(time);

			foreach (var (sec, dt) in GetWorkingSecurities())
			{
				SubscribeCandles(dt, true, sec)
					.Bind(c => ProcessCandle(c, sec))
					.Start();

				_prices[sec] = new FScoreRollingWindow(Lookback + 1);
			}
		}

		private void ProcessCandle(ICandleMessage candle, Security security)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Store the latest closing price for this security
			_latestPrices[security] = candle.ClosePrice;

			if (!_prices.TryGetValue(security, out var win))
				return;
			win.Add(candle.ClosePrice);
			if (win.IsFull())
				_ret[security] = (win.Last() - win[0]) / win[0];

			var d = candle.OpenTime.Date;
			if (d.Day == 1 && _lastRebalance != d)
			{
				_lastRebalance = d;
				Rebalance();
			}
		}

		private void Rebalance()
		{
			_fscore.Clear();
			foreach (var s in Universe)
				if (TryGetFScore(s, out var fs))
					_fscore[s] = fs;

			var eligible = _ret.Keys.Intersect(_fscore.Keys).ToList();
			if (eligible.Count < 20)
				return;

			var longs = eligible.Where(s => _ret[s] < 0 && _fscore[s] >= FHi).ToList();
			var shorts = eligible.Where(s => _ret[s] > 0 && _fscore[s] <= FLo).ToList();
			if (!longs.Any() || !shorts.Any())
				return;

			_w.Clear();
			decimal wl = 1m / longs.Count;
			decimal ws = -1m / shorts.Count;
			foreach (var s in longs)
				_w[s] = wl;
			foreach (var s in shorts)
				_w[s] = ws;

			foreach (var position in Positions)
				if (!_w.ContainsKey(position.Security))
					Order(position.Security, -PositionBy(position.Security));

			var portfolioValue = Portfolio.CurrentValue ?? 0m;
			foreach (var kv in _w)
			{
				var price = GetLatestPrice(kv.Key);
				if (price > 0)
				{
					var tgt = kv.Value * portfolioValue / price;
					var diff = tgt - PositionBy(kv.Key);
					if (Math.Abs(diff) * price >= MinTradeUsd)
						Order(kv.Key, diff);
				}
			}
		}

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}

		private void Order(Security s, decimal qty)
		{
			if (qty == 0)
				return;
			RegisterOrder(new Order
			{
				Security = s,
				Portfolio = Portfolio,
				Side = qty > 0 ? Sides.Buy : Sides.Sell,
				Volume = Math.Abs(qty),
				Type = OrderTypes.Market,
				Comment = "FScoreRev"
			});
		}

		private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;

		#region Stub FScore
		private bool TryGetFScore(Security s, out int fscore)
		{
			fscore = 0; // TODO integrate fundamentals
			return false;
		}
		#endregion
	}

	#region FScoreRollingWindow
	internal class FScoreRollingWindow
	{
		private readonly Queue<decimal> _q = new(); 
		private readonly int _size;
		
		public FScoreRollingWindow(int size) { _size = size; }
		public void Add(decimal v) { if (_q.Count == _size) _q.Dequeue(); _q.Enqueue(v); }
		public bool IsFull() => _q.Count == _size;
		public decimal Last() => _q.Last();
		public decimal this[int idx] => _q.ElementAt(idx);
		public int Count => _q.Count;
	}
	#endregion
}
