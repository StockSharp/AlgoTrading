// MomentumRevVolStrategy.cs
// -----------------------------------------------------------------------------
// Composite score =  (12m momentum  * weights.Wm)
//		   + (-1m reversal   * weights.Wr)
//		   + (-volatility    * weights.Wv)
// Long top decile, short bottom decile monthly.
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
	/// <summary>
	/// Momentum / reversal / volatility composite strategy.
	/// Ranks securities by 12‑month momentum, 1‑month reversal and volatility.
	/// </summary>
	public class MomentumRevVolStrategy : Strategy
	{
		#region Params

		private readonly StrategyParam<IEnumerable<Security>> _univ;
		private readonly StrategyParam<int> _look12;
		private readonly StrategyParam<int> _look1;
		private readonly StrategyParam<int> _volWindow;
		private readonly StrategyParam<decimal> _wM;
		private readonly StrategyParam<decimal> _wR;
		private readonly StrategyParam<decimal> _wV;
		private readonly StrategyParam<decimal> _minUsd;
		private readonly StrategyParam<DataType> _tf;

		/// <summary>Trading universe.</summary>
		public IEnumerable<Security> Universe
		{
			get => _univ.Value;
			set => _univ.Value = value;
		}

		/// <summary>Lookback for momentum in days (12 months).</summary>
		public int Lookback12
		{
			get => _look12.Value;
			set => _look12.Value = value;
		}

		/// <summary>Lookback for reversal in days (1 month).</summary>
		public int Lookback1
		{
			get => _look1.Value;
			set => _look1.Value = value;
		}

		/// <summary>Volatility calculation window.</summary>
		public int VolWindow
		{
			get => _volWindow.Value;
			set => _volWindow.Value = value;
		}

		/// <summary>Momentum weight.</summary>
		public decimal WM
		{
			get => _wM.Value;
			set => _wM.Value = value;
		}

		/// <summary>Reversal weight.</summary>
		public decimal WR
		{
			get => _wR.Value;
			set => _wR.Value = value;
		}

		/// <summary>Volatility weight.</summary>
		public decimal WV
		{
			get => _wV.Value;
			set => _wV.Value = value;
		}

		/// <summary>Minimum trade amount in USD.</summary>
		public decimal MinTradeUsd
		{
			get => _minUsd.Value;
			set => _minUsd.Value = value;
		}

		/// <summary>Candle type for calculations.</summary>
		public DataType CandleType
		{
			get => _tf.Value;
			set => _tf.Value = value;
		}

		#endregion

		private class Win
		{
			public RollingWin Px; public RollingWin Ret;
		}
		private readonly Dictionary<Security, Win> _map = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private DateTime _lastDay = DateTime.MinValue;
		private readonly Dictionary<Security, decimal> _w = new();

		public MomentumRevVolStrategy()
		{
			_univ = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>())
				.SetDisplay("Universe", "Securities to trade", "Universe");

			_look12 = Param(nameof(Lookback12), 252)
				.SetDisplay("Momentum Lookback", "Days for 12M momentum", "Parameters");

			_look1 = Param(nameof(Lookback1), 21)
				.SetDisplay("Reversal Lookback", "Days for 1M reversal", "Parameters");

			_volWindow = Param(nameof(VolWindow), 60)
				.SetDisplay("Vol Window", "Window for volatility", "Parameters");

			_wM = Param(nameof(WM), 1m)
				.SetDisplay("Momentum Weight", "Weight for momentum", "Weights");

			_wR = Param(nameof(WR), 1m)
				.SetDisplay("Reversal Weight", "Weight for reversal", "Weights");

			_wV = Param(nameof(WV), 1m)
				.SetDisplay("Volatility Weight", "Weight for volatility", "Weights");

			_minUsd = Param(nameof(MinTradeUsd), 200m)
				.SetDisplay("Min USD", "Minimum trade value", "Risk");

			_tf = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles used", "General");
		}

		public override IEnumerable<(Security, DataType)> GetWorkingSecurities() =>
			Universe.Select(s => (s, CandleType));

		
		protected override void OnReseted()
		{
			base.OnReseted();

			_map.Clear();
			_latestPrices.Clear();
			_lastDay = default;
			_w.Clear();
		}

		protected override void OnStarted(DateTimeOffset t)
		{
			base.OnStarted(t);

			if (Universe == null || !Universe.Any())
				throw new InvalidOperationException("Universe cannot be empty.");

			foreach (var (s, tf) in GetWorkingSecurities())
			{
				_map[s] = new Win { Px = new RollingWin(Lookback12 + 1), Ret = new RollingWin(VolWindow + 1) };
				SubscribeCandles(tf, true, s)
					.Bind(c => ProcessCandle(c, s))
					.Start();
			}
		}

		private void OnDaily(Security s, ICandleMessage c)
		{
			var w = _map[s];
			w.Px.Add(c.ClosePrice);
			w.Ret.Add(c.ClosePrice);
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
			var score = new Dictionary<Security, decimal>();
			foreach (var kv in _map)
			{
				var pxArr = kv.Value.Px.Data;
				var nP = Lookback12;
				var n1 = Lookback1;
				if (kv.Value.Px.Size < Lookback12 + 1)
					continue;
				var mom = (pxArr[0] - pxArr[nP]) / pxArr[nP];
				var rev = (pxArr[0] - pxArr[n1]) / pxArr[n1];
				var retArr = kv.Value.Ret.ReturnSeries();
				if (retArr.Length < VolWindow)
					continue;
				var vol = (decimal)Math.Sqrt(retArr.Select(r => (double)r * (double)r).Average());
				score[kv.Key] = WM * mom - WR * rev - WV * vol;
			}
			if (score.Count < 20)
				return;
			int dec = score.Count / 10;
			var longs = score.OrderByDescending(kv => kv.Value).Take(dec).Select(kv => kv.Key).ToList();
			var shorts = score.OrderBy(kv => kv.Value).Take(dec).Select(kv => kv.Key).ToList();
			_w.Clear();
			decimal wl = 1m / longs.Count, ws = -1m / shorts.Count;
			foreach (var s in longs)
				_w[s] = wl;
			foreach (var s in shorts)
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
				Comment = "MomRevVol"
			});
		}
		private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}

		private void ProcessCandle(ICandleMessage candle, Security security)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Store the latest closing price for this security
			_latestPrices[security] = candle.ClosePrice;

			OnDaily(security, candle);
		}

		private class RollingWin
		{
			private readonly Queue<decimal> _q = new(); private readonly int _n;
			public RollingWin(int n) { _n = n; }
			public int Size => _q.Count;
			public void Add(decimal p) { if (_q.Count == _n) _q.Dequeue(); _q.Enqueue(p); }
			public decimal[] Data => _q.ToArray();
			public decimal[] ReturnSeries()
			{
				var arr = _q.ToArray();
				var res = new decimal[arr.Length - 1];
				for (int i = 1; i < arr.Length; i++)
					res[i - 1] = (arr[i] - arr[i - 1]) / arr[i - 1];
				return res;
			}
		}
	}
}
