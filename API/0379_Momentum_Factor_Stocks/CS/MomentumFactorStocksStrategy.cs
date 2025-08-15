using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Classic momentum factor strategy: long the top-quintile 12-1 month momentum stocks
/// and short the bottom quintile. Rebalanced on the first trading day of each month.
/// </summary>
public class MomentumFactorStocksStrategy : Strategy
{
	#region Params
	private readonly StrategyParam<IEnumerable<Security>> _univ;
	private readonly StrategyParam<int> _look;
	private readonly StrategyParam<int> _skip;
	private readonly StrategyParam<int> _quint;
	private readonly StrategyParam<decimal> _minUsd;
	private readonly StrategyParam<DataType> _tf;

	/// <summary>
	/// Securities universe.
	/// </summary>
	public IEnumerable<Security> Universe
	{
		get => _univ.Value;
		set => _univ.Value = value;
	}

	/// <summary>
	/// Lookback period for momentum in trading days.
	/// </summary>
	public int LookbackDays
	{
		get => _look.Value;
		set => _look.Value = value;
	}

	/// <summary>
	/// Number of days skipped from the most recent data.
	/// </summary>
	public int SkipDays
	{
		get => _skip.Value;
		set => _skip.Value = value;
	}

	/// <summary>
	/// Quintile used for ranking momentum.
	/// </summary>
	public int Quintile
	{
		get => _quint.Value;
		set => _quint.Value = value;
	}

	/// <summary>
	/// Minimum trade value in USD.
	/// </summary>
	public decimal MinTradeUsd
	{
		get => _minUsd.Value;
		set => _minUsd.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _tf.Value;
		set => _tf.Value = value;
	}
	#endregion

	private readonly Dictionary<Security, RollingWin> _px = [];
	private readonly Dictionary<Security, decimal> _latestPrices = [];
	private DateTime _last = DateTime.MinValue;
	private readonly Dictionary<Security, decimal> _w = [];

	/// <summary>
	/// Initializes a new instance of <see cref="MomentumFactorStocksStrategy"/>.
	/// </summary>
	public MomentumFactorStocksStrategy()
	{
		_univ = Param<IEnumerable<Security>>(nameof(Universe), [])
				.SetDisplay("Universe", "Securities to trade", "General");

		_look = Param(nameof(LookbackDays), 252)
				.SetGreaterThanZero()
				.SetDisplay("Lookback", "Trading days for momentum", "Parameters");

		_skip = Param(nameof(SkipDays), 21)
				.SetGreaterThanZero()
				.SetDisplay("Skip days", "Days skipped from recent data", "Parameters");

		_quint = Param(nameof(Quintile), 5)
				.SetGreaterThanZero()
				.SetDisplay("Quintile", "Quintile for momentum ranking", "Parameters");

		_minUsd = Param(nameof(MinTradeUsd), 200m)
				.SetGreaterThanZero()
				.SetDisplay("Min Trade USD", "Minimum order value in USD", "Parameters");

		_tf = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Candle Type", "Time frame for candles", "General");
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

		_px.Clear();
		_latestPrices.Clear();
		_last = default;
		_w.Clear();
	}

	protected override void OnStarted(DateTimeOffset t)
	{
		base.OnStarted(t);

		if (Universe == null || !Universe.Any())
			throw new InvalidOperationException("Universe is empty.");

		foreach (var (s, tf) in GetWorkingSecurities())
		{
			_px[s] = new RollingWin(LookbackDays + 1);
			SubscribeCandles(tf, true, s)
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

		OnDaily(security, candle);
	}

	private void OnDaily(Security s, ICandleMessage c)
	{
		_px[s].Add(c.ClosePrice);
		var d = c.OpenTime.Date;
		if (d == _last)
			return;
		_last = d;
		if (d.Day != 1)
			return;
		Rebalance();
	}

	private void Rebalance()
	{
		var mom = new Dictionary<Security, decimal>();
		foreach (var kv in _px)
			if (kv.Value.Full)
				mom[kv.Key] = (kv.Value.Data[SkipDays] - kv.Value.Data[LookbackDays]) / kv.Value.Data[LookbackDays];
		if (mom.Count < Quintile * 2)
			return;
		int q = mom.Count / Quintile;
		var longs = mom.OrderByDescending(kv => kv.Value).Take(q).Select(kv => kv.Key).ToList();
		var shorts = mom.OrderBy(kv => kv.Value).Take(q).Select(kv => kv.Key).ToList();
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

	private decimal GetLatestPrice(Security security)
	{
		return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
	}

	private void Move(Security s, decimal tgt)
	{
		var diff = tgt - Pos(s);
		var price = GetLatestPrice(s);
		if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd)
			return;
		RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "MomStocks" });
	}

	private decimal Pos(Security s) => GetPositionValue(s, Portfolio) ?? 0;

	private class RollingWin { private readonly Queue<decimal> _q = []; private readonly int _n; public RollingWin(int n) { _n = n; } public bool Full => _q.Count == _n; public void Add(decimal p) { if (_q.Count == _n) _q.Dequeue(); _q.Enqueue(p); } public decimal[] Data => [.. _q]; }
}