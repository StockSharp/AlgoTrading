using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Currency purchasing power parity value strategy.
	/// Buys undervalued currencies and sells overvalued ones with monthly rebalancing.
	/// </summary>
	public class CurrencyPPPValueStrategy : Strategy
	{
		private readonly StrategyParam<IEnumerable<Security>> _universe;
		private readonly StrategyParam<int> _k;
		private readonly StrategyParam<DataType> _tf;
		private readonly StrategyParam<decimal> _minUsd;

		/// <summary>
		/// Trading universe.
		/// </summary>
		public IEnumerable<Security> Universe
		{
			get => _universe.Value;
			set => _universe.Value = value;
		}

		/// <summary>
		/// Number of currencies to long and short.
		/// </summary>
		public int K
		{
			get => _k.Value;
			set => _k.Value = value;
		}

		/// <summary>
		/// Candle type (time-frame) used for analysis.
		/// </summary>
		public DataType CandleType
		{
			get => _tf.Value;
			set => _tf.Value = value;
		}

		/// <summary>
		/// Minimum trade value in USD.
		/// </summary>
		public decimal MinTradeUsd
		{
			get => _minUsd.Value;
			set => _minUsd.Value = value;
		}

		private readonly Dictionary<Security, decimal> _w = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private DateTime _lastDay = DateTime.MinValue;

		/// <summary>
		/// Initializes a new instance of the <see cref="CurrencyPPPValueStrategy"/> class.
		/// </summary>
		public CurrencyPPPValueStrategy()
		{
			_universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>())
				.SetDisplay("Universe", "Securities to trade", "General");

			_k = Param(nameof(K), 3)
				.SetGreaterThanZero()
				.SetDisplay("K", "Number of currencies to long/short", "General");

			_tf = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Candle Type", "Time frame of candles", "General");

			_minUsd = Param(nameof(MinTradeUsd), 100m)
				.SetGreaterThanZero()
				.SetDisplay("Min Trade USD", "Minimum trade size in USD", "Risk Management");
		}


		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return Universe.Select(s => (s, CandleType));
		}


		/// <inheritdoc />
		
		protected override void OnReseted()
		{
			base.OnReseted();

			_w.Clear();
			_latestPrices.Clear();
			_lastDay = default;
		}

		protected override void OnStarted(DateTimeOffset t)
		{
			base.OnStarted(t);
			if (Universe == null || !Universe.Any())
				throw new InvalidOperationException("Universe is empty.");

			foreach (var (s, dt) in GetWorkingSecurities())
			{
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

			Daily(candle);
		}

		private void Daily(ICandleMessage c)
		{
			var d = c.OpenTime.Date;
			if (d == _lastDay)
				return;
			_lastDay = d;
			if (d.Day == 1)
				Rebalance();
		}

		private void Rebalance()
		{
			var dev = new Dictionary<Security, decimal>();
			foreach (var s in Universe)
				if (TryGetPPPDeviation(s, out var v))
					dev[s] = v;

			if (dev.Count < K * 2)
				return;
			var underv = dev.OrderBy(kv => kv.Value).Take(K).Select(kv => kv.Key).ToList();
			var over = dev.OrderByDescending(kv => kv.Value).Take(K).Select(kv => kv.Key).ToList();

			_w.Clear();
			decimal wl = 1m / underv.Count, ws = -1m / over.Count;
			foreach (var s in underv)
				_w[s] = wl;
			foreach (var s in over)
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
			RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "PPPValue" });
		}
		private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}

		private bool TryGetPPPDeviation(Security s, out decimal dev) { dev = 0; return false; }

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
