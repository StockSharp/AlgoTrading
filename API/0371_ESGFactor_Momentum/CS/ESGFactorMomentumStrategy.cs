// ESGFactorMomentumStrategy.cs - parameterized timeframe version
// * Universe: IEnumerable<Security>
// * CandleType: StrategyParam<DataType> (e.g., TimeSpan.FromDays(1).TimeFrame())
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
/// Momentum strategy based on ESG factors. Rebalances to the best performing
/// security from the universe using a momentum lookback period.
/// </summary>
public class ESGFactorMomentumStrategy : Strategy
{
	#region Parameters
	private readonly StrategyParam<IEnumerable<Security>> _universe;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _minUsd;

	/// <summary>
	/// The universe of securities to trade.
	/// </summary>
	public IEnumerable<Security> Universe
	{
		get => _universe.Value;
		set => _universe.Value = value;
	}

	/// <summary>
	/// Momentum lookback period in days.
	/// </summary>
	public int LookbackDays
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Candle type used for price data.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Minimum trade amount in USD.
	/// </summary>
	public decimal MinTradeUsd
	{
		get => _minUsd.Value;
		set => _minUsd.Value = value;
	}
	#endregion

	private readonly Dictionary<Security, RollingWindow<decimal>> _windows = [];
	private readonly Dictionary<Security, decimal> _latestPrices = [];
	private readonly HashSet<Security> _held = [];
	private DateTime _lastProc = DateTime.MinValue;

	/// <summary>
	/// Initializes a new instance of <see cref="ESGFactorMomentumStrategy"/>.
	/// </summary>
	public ESGFactorMomentumStrategy()
	{
		// Universe parameter
		_universe = Param<IEnumerable<Security>>(nameof(Universe), [])
			.SetDisplay("Universe", "ESG ETFs list", "Universe");

		// Lookback period parameter
		_lookback = Param(nameof(LookbackDays), 252);

		// Candle type parameter
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle TF", "Time-frame", "General");

		// Minimum trade amount parameter
		_minUsd = Param(nameof(MinTradeUsd), 100m);
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

		_windows.Clear();
		_latestPrices.Clear();
		_held.Clear();
		_lastProc = default;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Universe == null || !Universe.Any())
			throw new InvalidOperationException("Universe is empty.");

		foreach (var (sec, dt) in GetWorkingSecurities())
		{
			_windows[sec] = new RollingWindow<decimal>(LookbackDays + 1);

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

		var win = _windows[security];
		win.Add(candle.ClosePrice);

		var d = candle.OpenTime.Date;
		if (d == _lastProc)
			return;
		_lastProc = d;

		if (d.Day == 1)
			TryRebalance();
	}

	private void TryRebalance()
	{
		if (_windows.Values.Any(w => !w.IsFull()))
			return;

		var mom = _windows.ToDictionary(kv => kv.Key,
			kv => (kv.Value.Last() - kv.Value[0]) / kv.Value[0]);

		var best = mom.Values.Max();
		var winners = mom.Where(kv => kv.Value == best).Select(kv => kv.Key).ToList();
		var w = 1m / winners.Count;

		foreach (var s in _held.Where(h => !winners.Contains(h)).ToList())
			Move(s, 0);

		var portfolioValue = Portfolio.CurrentValue ?? 0m;
		foreach (var s in winners)
		{
			var price = GetLatestPrice(s);
			if (price > 0)
				Move(s, w * portfolioValue / price);
		}

		_held.Clear();
		_held.UnionWith(winners);

		LogInfo($"Rebalance TF {CandleType}: {string.Join(',', winners.Select(x => x.Code))}");
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
			Comment = "ESGMom",
		});
	}

	private decimal PositionBy(Security s)
	{
		return GetPositionValue(s, Portfolio) ?? 0m;
	}

	private decimal GetLatestPrice(Security security)
	{
		return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
	}

	#region RollingWindow
	private class RollingWindow<T>
	{
		private readonly Queue<T> _q = [];
		private readonly int _n;

		public RollingWindow(int n)
		{
			_n = n;
		}

		public void Add(T v)
		{
			if (_q.Count == _n)
				_q.Dequeue();

			_q.Enqueue(v);
		}

		public bool IsFull()
		{
			return _q.Count == _n;
		}

		public T Last()
		{
			return _q.Last();
		}

		public T this[int i]
		{
			get => _q.ElementAt(i);
		}
	}
	#endregion
}
