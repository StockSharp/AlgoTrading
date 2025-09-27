// RDExpendituresStrategy.cs
// -----------------------------------------------------------------------------
// Long stocks with highest R&D−to−MV ratio, short lowest quintile.
// Monthly rebalance on first trading day via trigger candle.
// Requires external TryGetRDExpenseRatio.
// -----------------------------------------------------------------------------
// Date: 2 Aug 2025
// -----------------------------------------------------------------------------
using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// R&D expenditures strategy.
/// Long high R&D-to-market-value stocks and short low ones.
/// </summary>
public class RDExpendituresStrategy : Strategy
{
	private readonly StrategyParam<IEnumerable<Security>> _univ;
	private readonly StrategyParam<int> _quint;
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
	private readonly Dictionary<Security, decimal> _w = [];
	private readonly Dictionary<Security, decimal> _latestPrices = [];
	private DateTime _last = DateTime.MinValue;

	/// <summary>
	/// Strategy universe.
	/// </summary>
	public IEnumerable<Security> Universe
	{
		get => _univ.Value;
		set => _univ.Value = value;
	}

	/// <summary>
	/// Number of quintiles to split the universe.
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

	public RDExpendituresStrategy()
	{
		_univ = Param<IEnumerable<Security>>(nameof(Universe), [])
		.SetDisplay("Universe", "Securities to trade", "General");
		_quint = Param(nameof(Quintile), 5)
		.SetGreaterThanZero()
		.SetDisplay("Quintile", "Number of quintiles", "General");
		_minUsd = Param(nameof(MinTradeUsd), 200m)
		.SetGreaterThanZero()
		.SetDisplay("Min Trade USD", "Minimum trade value in USD", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security, DataType)> GetWorkingSecurities() => Universe.Select(s => (s, CandleType));

	
	protected override void OnReseted()
	{
		base.OnReseted();

		_w.Clear();
		_latestPrices.Clear();
		_last = default;
	}

	protected override void OnStarted(DateTimeOffset t)
	{
		if (Universe == null || !Universe.Any())
			throw new InvalidOperationException("Universe must not be empty.");
		base.OnStarted(t);
		var trig = Universe.FirstOrDefault() ?? throw new InvalidOperationException("Universe empty");
		SubscribeCandles(CandleType, true, trig).Bind(c => ProcessCandle(c, trig)).Start();
	}

	private void ProcessCandle(ICandleMessage candle, Security security)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Store the latest closing price for this security
		_latestPrices[security] = candle.ClosePrice;

		OnDaily(candle.OpenTime.Date);
	}

	private void OnDaily(DateTime d)
	{
		if (d == _last)
			return;
		_last = d;
		if (d.Day != 1)
			return;
		Rebalance();
	}

	private void Rebalance()
	{
		var ratio = new Dictionary<Security, decimal>();
		foreach (var s in Universe)
			if (TryGetRDExpenseRatio(s, out var r))
				ratio[s] = r;
		if (ratio.Count < Quintile * 2)
			return;
		int q = ratio.Count / Quintile;
		var longs = ratio.OrderByDescending(kv => kv.Value).Take(q).Select(kv => kv.Key).ToList();
		var shorts = ratio.OrderBy(kv => kv.Value).Take(q).Select(kv => kv.Key).ToList();
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
		RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "RDmom" }); 
	}

	private decimal Pos(Security s) => GetPositionValue(s, Portfolio) ?? 0;
	private bool TryGetRDExpenseRatio(Security s, out decimal r) { r = 0; return false; }
}
