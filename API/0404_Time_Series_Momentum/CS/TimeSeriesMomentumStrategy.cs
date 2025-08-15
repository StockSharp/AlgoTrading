// TimeSeriesMomentumStrategy.cs
// -----------------------------------------------------------------------------
// 12‑month momentum sign, weight = 1/vol (60‑day). Monthly rebalance.
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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Time series momentum strategy with volatility scaling.
/// </summary>
public class TimeSeriesMomentumStrategy : Strategy
{
	private readonly StrategyParam<IEnumerable<Security>> _univ;
	private readonly StrategyParam<int> _look;
	private readonly StrategyParam<int> _vol;
	private readonly StrategyParam<decimal> _minUsd;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	private class Win { public Queue<decimal> Px = []; }
	private readonly Dictionary<Security, Win> _map = [];
	private readonly Dictionary<Security, decimal> _w = [];
	private readonly Dictionary<Security, decimal> _latestPrices = [];
	private DateTime _last = DateTime.MinValue;

	/// <summary>
	/// List of securities to trade.
	/// </summary>
	public IEnumerable<Security> Universe
	{
		get => _univ.Value;
		set => _univ.Value = value;
	}

	/// <summary>
	/// Lookback period in trading days.
	/// </summary>
	public int Lookback
	{
		get => _look.Value;
		set => _look.Value = value;
	}

	/// <summary>
	/// Window for volatility calculation.
	/// </summary>
	public int VolWindow
	{
		get => _vol.Value;
		set => _vol.Value = value;
	}

	/// <summary>
	/// Minimum trade value in USD.
	/// </summary>
	public decimal MinTradeUsd
	{
		get => _minUsd.Value;
		set => _minUsd.Value = value;
	}

	public TimeSeriesMomentumStrategy()
	{
		_univ = Param<IEnumerable<Security>>(nameof(Universe), [])
			.SetDisplay("Universe", "List of securities to trade", "Universe");

		_look = Param(nameof(Lookback), 252)
			.SetDisplay("Lookback", "Performance lookback in days", "Parameters");

		_vol = Param(nameof(VolWindow), 60)
			.SetDisplay("Vol Window", "Volatility window in days", "Parameters");

		_minUsd = Param(nameof(MinTradeUsd), 200m)
			.SetDisplay("Min Trade USD", "Minimum notional value for orders", "Risk Management");
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_last = DateTime.MinValue;
		_map.Clear();

	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset t)
	{
		if (Universe == null || !Universe.Any())
			throw new InvalidOperationException("Universe cannot be empty.");

		base.OnStarted(t);

		foreach (var (s, tf) in GetWorkingSecurities())
		{
			_map[s] = new Win();
			SubscribeCandles(tf, true, s).Bind(c => ProcessCandle(c, s)).Start();
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
		var q = _map[s].Px;
		if (q.Count == Math.Max(Lookback, VolWindow) + 1)
			q.Dequeue();
		q.Enqueue(c.ClosePrice);
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
		var sig = new Dictionary<Security, (decimal perf, double vol)>();
		foreach (var kv in _map)
		{
			if (kv.Value.Px.Count < Lookback + 1 || kv.Value.Px.Count < VolWindow + 1)
				continue;
			var arr = kv.Value.Px.ToArray();
			var perf = (arr[0] - arr[Lookback]) / arr[Lookback];
			double[] ret = new double[VolWindow];
			for (int i = 1; i <= VolWindow; i++)
				ret[i - 1] = (double)((arr[i - 1] - arr[i]) / arr[i]);
			var vol = Math.Sqrt(ret.Select(x => x * x).Average());
			sig[kv.Key] = (perf, vol);
		}
		if (!sig.Any())
			return;
		_w.Clear();
		foreach (var kv in sig)
		{
			var dir = kv.Value.perf > 0 ? 1 : -1;
			_w[kv.Key] = dir / (decimal)kv.Value.vol;
		}
		var norm = _w.Values.Sum(x => Math.Abs(x));
		foreach (var k in _w.Keys.ToList())
			_w[k] /= norm;
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
		RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "TSMom" });
	}

	private decimal Pos(Security s) => GetPositionValue(s, Portfolio) ?? 0;
}