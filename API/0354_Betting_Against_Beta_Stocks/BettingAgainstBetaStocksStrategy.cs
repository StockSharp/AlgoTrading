// BettingAgainstBetaStocksStrategy.cs (candle-driven, param TF)
// Long lowest-beta decile, short highest-beta; monthly rebalance.
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
	/// <summary>
	/// Implements the Betting Against Beta (BAB) strategy for equities.
	/// Longs the lowest beta decile and shorts the highest beta decile
	/// with monthly rebalancing.
	/// </summary>
	public class BettingAgainstBetaStocksStrategy : Strategy
	{
		private readonly StrategyParam<IEnumerable<Security>> _universe;
		private readonly StrategyParam<Security> _benchmark;
		private readonly StrategyParam<int> _window;
		private readonly StrategyParam<DataType> _tf;
		private readonly StrategyParam<int> _deciles;
		private readonly StrategyParam<decimal> _minUsd;

		/// <summary>
		/// Universe of stocks to trade.
		/// </summary>
		public IEnumerable<Security> Universe
		{
			get => _universe.Value;
			set => _universe.Value = value;
		}

		/// <summary>
		/// Benchmark security used for beta calculation.
		/// </summary>
		public Security Benchmark
		{
			get => _benchmark.Value;
			set => _benchmark.Value = value;
		}

		/// <summary>
		/// Rolling window length in days for beta estimation.
		/// </summary>
		public int WindowDays
		{
			get => _window.Value;
			set => _window.Value = value;
		}

		/// <summary>
		/// Number of deciles to split the universe into.
		/// </summary>
		public int Deciles
		{
			get => _deciles.Value;
			set => _deciles.Value = value;
		}

		/// <summary>
		/// Candle type used for calculations.
		/// </summary>
		public DataType CandleType
		{
			get => _tf.Value;
			set => _tf.Value = value;
		}

		/// <summary>
		/// Minimum USD value for a trade.
		/// </summary>
		public decimal MinTradeUsd
		{
			get => _minUsd.Value;
			set => _minUsd.Value = value;
		}

		private readonly Dictionary<Security, RollingWindow<decimal>> _wins = new();
		private readonly Dictionary<Security, decimal> _weights = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private DateTime _lastDay = DateTime.MinValue;

		/// <summary>
		/// Initializes strategy parameters.
		/// </summary>
		public BettingAgainstBetaStocksStrategy()
		{
			_universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>())
				.SetDisplay("Universe", "Securities universe for strategy", "General");

			_benchmark = Param<Security>(nameof(Benchmark), null)
				.SetDisplay("Benchmark", "Benchmark security for beta", "General");

			_window = Param(nameof(WindowDays), 252)
				.SetGreaterThanZero()
				.SetDisplay("Window (days)", "Rolling window length for beta", "Parameters");

			_deciles = Param(nameof(Deciles), 10)
				.SetGreaterThanZero()
				.SetDisplay("Deciles", "Number of buckets for sorting", "Parameters");

			_tf = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to process", "General");

			_minUsd = Param(nameof(MinTradeUsd), 100m)
				.SetGreaterThanZero()
				.SetDisplay("Min Trade $", "Minimum trade value", "Risk Management");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			if (Benchmark == null)
				throw new InvalidOperationException("Benchmark not set");

			return Universe.Append(Benchmark).Select(s => (s, CandleType));
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			if (Universe == null || !Universe.Any())
				throw new InvalidOperationException("Universe is empty");
			if (Benchmark == null)
				throw new InvalidOperationException("Benchmark not set");

			base.OnStarted(time);

			foreach (var (sec, dt) in GetWorkingSecurities())
			{
				_wins[sec] = new RollingWindow<decimal>(WindowDays + 1);

				SubscribeCandles(dt, true, sec)
					.Bind(candle => ProcessCandle(candle, sec))
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

			// Add closing price to rolling window
			_wins[security].Add(candle.ClosePrice);

			var d = candle.OpenTime.Date;
			if (d == _lastDay)
				return;
			_lastDay = d;
			
			if (d.Day == 1)
				TryRebalance();
		}

		private void TryRebalance()
		{
			if (_wins.Values.Any(w => !w.IsFull()))
				return;

			var benchRet = GetReturns(_wins[Benchmark]);
			var betas = new Dictionary<Security, decimal>();

			foreach (var s in Universe)
			{
				var r = GetReturns(_wins[s]);
				betas[s] = Beta(r, benchRet);
			}

			int bucket = betas.Count / Deciles;
			if (bucket == 0)
				return;

			var sorted = betas.OrderBy(kv => kv.Value).ToList();
			var longs = sorted.Take(bucket).Select(kv => kv.Key).ToList();
			var shorts = sorted.Skip(betas.Count - bucket).Select(kv => kv.Key).ToList();

			_weights.Clear();
			decimal wl = 1m / longs.Count, ws = -1m / shorts.Count;
			foreach (var s in longs)
				_weights[s] = wl;
			foreach (var s in shorts)
				_weights[s] = ws;

			foreach (var position in Positions)
				if (!_weights.ContainsKey(position.Security))
					Move(position.Security, 0);

			var portfolioValue = Portfolio.CurrentValue ?? 0m;
			foreach (var kv in _weights)
			{
				var price = GetLatestPrice(kv.Key);
				if (price > 0)
					Move(kv.Key, kv.Value * portfolioValue / price);
			}
		}

		private decimal[] GetReturns(RollingWindow<decimal> win)
		{
			var arr = win.ToArray();
			var r = new decimal[arr.Length - 1];
			for (int i = 1; i < arr.Length; i++)
				r[i - 1] = (arr[i] - arr[i - 1]) / arr[i - 1];
			return r;
		}
		private decimal Beta(decimal[] x, decimal[] y)
		{
			int n = Math.Min(x.Length, y.Length);
			var meanX = x.Take(n).Average();
			var meanY = y.Take(n).Average();
			decimal cov = 0, varM = 0;
			for (int i = 0; i < n; i++)
			{
				cov += (x[i] - meanX) * (y[i] - meanY);
				varM += (y[i] - meanY) * (y[i] - meanY);
			}
			return varM != 0 ? cov / varM : 0m;
		}

		private void Move(Security s, decimal tgt)
		{
			var diff = tgt - PositionBy(s);
			var price = GetLatestPrice(s);
			if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd)
				return;
			RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "BABStocks" });
		}
		private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}

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
			public T[] ToArray() => _q.ToArray();
		}
		#endregion

	}
}