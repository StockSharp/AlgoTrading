// ResidualMomentumFactorStrategy.cs
// -----------------------------------------------------------------------------
// Uses residual momentum score from factor model (external feed).
// Monthly rebalance on first trading day via trigger candle.
// Long top decile, short bottom decile.
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
/// Residual momentum factor strategy that goes long securities with the highest
/// residual momentum and short those with the lowest.
/// </summary>
public class ResidualMomentumFactorStrategy : Strategy
{
	private readonly StrategyParam<IEnumerable<Security>> _univ;
	private readonly StrategyParam<int> _decile;
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
	/// Universe of securities to trade.
	/// </summary>
	public IEnumerable<Security> Universe
	{
		get => _univ.Value;
		set => _univ.Value = value;
	}

	/// <summary>
	/// Number of deciles used to rank securities.
	/// </summary>
	public int Decile
	{
		get => _decile.Value;
		set => _decile.Value = value;
	}

	/// <summary>
	/// Minimum dollar value per trade.
	/// </summary>
	public decimal MinTradeUsd
	{
		get => _minUsd.Value;
		set => _minUsd.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ResidualMomentumFactorStrategy"/> class.
	/// </summary>
	public ResidualMomentumFactorStrategy()
	{
		_univ = Param<IEnumerable<Security>>(nameof(Universe), [])
			.SetDisplay("Universe", "Securities to trade", "General");

		_decile = Param(nameof(Decile), 10)
			.SetDisplay("Decile", "Number of deciles for ranking", "General");

		_minUsd = Param(nameof(MinTradeUsd), 200m)
			.SetDisplay("Min Trade USD", "Minimum dollar value per trade", "General");
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
		_last = default;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Universe == null || !Universe.Any())
			throw new InvalidOperationException("Universe cannot be empty.");

		var trig = Universe.First();
		SubscribeCandles(CandleType, true, trig)
			.Bind(c => ProcessCandle(c, trig))
			.Start();
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
		var score = new Dictionary<Security, decimal>();
		foreach (var s in Universe)
		{
			if (TryGetResidualMomentum(s, out var sc))
				score[s] = sc;
		}

		if (score.Count < Decile * 2)
			return;

		int bucket = score.Count / Decile;
		var longs = score.OrderByDescending(kv => kv.Value).Take(bucket).Select(kv => kv.Key).ToList();
		var shorts = score.OrderBy(kv => kv.Value).Take(bucket).Select(kv => kv.Key).ToList();
		_w.Clear();

		decimal wl = 1m / longs.Count;
		decimal ws = -1m / shorts.Count;

		foreach (var s in longs)
			_w[s] = wl;

		foreach (var s in shorts)
			_w[s] = ws;

		foreach (var position in Positions)
		{
			if (!_w.ContainsKey(position.Security))
				Move(position.Security, 0);
		}

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

		RegisterOrder(new Order
		{
			Security = s,
			Portfolio = Portfolio,
			Side = diff > 0 ? Sides.Buy : Sides.Sell,
			Volume = Math.Abs(diff),
			Type = OrderTypes.Market,
			Comment = "ResMom",
		});
	}

	private decimal Pos(Security s) => GetPositionValue(s, Portfolio) ?? 0;

	private bool TryGetResidualMomentum(Security s, out decimal sc)
	{
		sc = 0;
		return false;
	}
}
