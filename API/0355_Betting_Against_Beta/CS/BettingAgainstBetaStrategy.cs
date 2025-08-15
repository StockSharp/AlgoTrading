// BettingAgainstBetaStrategy.cs (candle-driven, param TF)
// Long lowest-beta decile, short highest-beta; monthly rebalance.
// Date: 2 August 2025

using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Betting Against Beta strategy.
/// Longs the lowest beta decile and shorts the highest beta decile.
/// </summary>
public class BettingAgainstBetaStrategy : Strategy
{
	private readonly StrategyParam<IEnumerable<Security>> _universe;
	private readonly StrategyParam<int> _window;
	private readonly StrategyParam<DataType> _tf;
	private readonly StrategyParam<int> _deciles;
	private readonly StrategyParam<decimal> _minUsd;

	/// <summary>
	/// Securities universe.
	/// </summary>
	public IEnumerable<Security> Universe
	{
		get => _universe.Value;
		set => _universe.Value = value;
	}

	/// <summary>
	/// Lookback window length in days.
	/// </summary>
	public int WindowDays
	{
		get => _window.Value;
		set => _window.Value = value;
	}

	/// <summary>
	/// Number of deciles for long/short groups.
	/// </summary>
	public int Deciles
	{
		get => _deciles.Value;
		set => _deciles.Value = value;
	}

	/// <summary>
	/// Candle time frame used by the strategy.
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

	private readonly Dictionary<Security, RollingWindow<decimal>> _wins = [];
	private readonly Dictionary<Security, decimal> _weights = [];
	private readonly Dictionary<Security, decimal> _latestPrices = [];
	private DateTime _lastDay = DateTime.MinValue;

	/// <summary>
	/// Initializes a new instance of the <see cref="BettingAgainstBetaStrategy"/> class.
	/// </summary>
	public BettingAgainstBetaStrategy()
	{
		_universe = Param<IEnumerable<Security>>(nameof(Universe), [])
			.SetDisplay("Universe", "Securities universe", "General");

		_window = Param(nameof(WindowDays), 252)
			.SetDisplay("Window Days", "Lookback window length", "General")
			.SetGreaterThanZero();

		_deciles = Param(nameof(Deciles), 10)
			.SetDisplay("Deciles", "Number of deciles", "General")
			.SetGreaterThanZero();

		_tf = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle time frame", "General");

		_minUsd = Param(nameof(MinTradeUsd), 100m)
			.SetDisplay("Min Trade USD", "Minimum trade value in USD", "General")
			.SetGreaterThanZero();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
			throw new InvalidOperationException("Benchmark not set");

		return Universe.Append(Security).Select(s => (s, CandleType));
	}

	/// <inheritdoc />
	
	protected override void OnReseted()
	{
		base.OnReseted();

		_wins.Clear();
		_weights.Clear();
		_latestPrices.Clear();
		_lastDay = default;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		if (Security == null)
			throw new InvalidOperationException("Benchmark not set");

		if (Universe == null || !Universe.Any())
			throw new InvalidOperationException("Universe is empty");

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

		var benchRet = GetReturns(_wins[Security]);
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
		RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "BAB" });
	}
	private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;

	#region RollingWindow
	private class RollingWindow<T>
	{
		private readonly Queue<T> _q = [];
		private readonly int _n;
		public RollingWindow(int n) { _n = n; }
		public void Add(T v) { if (_q.Count == _n) _q.Dequeue(); _q.Enqueue(v); }
		public bool IsFull() => _q.Count == _n;
		public T Last() => _q.Last();
		public T this[int i] => _q.ElementAt(i);
		public T[] ToArray() => [.. _q];
	}
	#endregion

}
