using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on lexical density of company filings.
/// Rebalances quarterly using the first three trading days of February, May, August and November.
/// </summary>
public class LexicalDensityFilingsStrategy : Strategy
{
	private readonly StrategyParam<IEnumerable<Security>> _univ;
	private readonly StrategyParam<int> _quintile;
	private readonly StrategyParam<decimal> _minUsd;
	private readonly StrategyParam<DataType> _tf;

	/// <summary>
	/// Universe of securities to trade.
	/// </summary>
	public IEnumerable<Security> Universe
	{
		get => _univ.Value;
		set => _univ.Value = value;
	}

	/// <summary>
	/// Number of quintiles for ranking lexical density.
	/// </summary>
	public int Quintile
	{
		get => _quintile.Value;
		set => _quintile.Value = value;
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

	private readonly Dictionary<Security, decimal> _weights = [];
	private readonly Dictionary<Security, decimal> _latestPrices = [];
	private DateTime _lastDay = DateTime.MinValue;

	/// <summary>
	/// Initializes a new instance of <see cref="LexicalDensityFilingsStrategy"/>.
	/// </summary>
	public LexicalDensityFilingsStrategy()
	{
		_univ = Param<IEnumerable<Security>>(nameof(Universe), [])
			.SetDisplay("Universe", "Securities to trade", "General");

		_quintile = Param(nameof(Quintile), 5)
			.SetGreaterThanZero()
			.SetDisplay("Quintile", "Number of quintiles for ranking", "Parameters");

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

		_weights.Clear();
		_latestPrices.Clear();
		_lastDay = default;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Universe == null || !Universe.Any())
			throw new InvalidOperationException("Universe is empty.");

		var trigger = Universe.First();

		SubscribeCandles(CandleType, true, trigger)
			.Bind(c => ProcessCandle(c, trigger))
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, Security security)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Store the latest closing price for this security
		_latestPrices[security] = candle.ClosePrice;

		var d = candle.OpenTime.Date;

		if (d == _lastDay)
			return;

		_lastDay = d;

		if (IsQuarterRebalanceDay(d))
			Rebalance();
	}

	private static bool IsQuarterRebalanceDay(DateTime d)
	{
		return (d.Month == 2 || d.Month == 5 || d.Month == 8 || d.Month == 11) &&
			   d.Day <= 3;
	}

	private void Rebalance()
	{
		var dens = new Dictionary<Security, decimal>();

		foreach (var s in Universe)
		{
			if (TryGetLexicalDensity(s, out var val))
				dens[s] = val;
		}

		if (dens.Count < Quintile * 2)
			return;

		var bucket = dens.Count / Quintile;

		var longSide = dens.OrderByDescending(kv => kv.Value).Take(bucket).Select(kv => kv.Key).ToList();
		var shortSide = dens.OrderBy(kv => kv.Value).Take(bucket).Select(kv => kv.Key).ToList();

		_weights.Clear();

		var wl = 1m / longSide.Count;
		var ws = -1m / shortSide.Count;

		foreach (var s in longSide)
			_weights[s] = wl;

		foreach (var s in shortSide)
			_weights[s] = ws;

		foreach (var position in Positions)
		{
			if (!_weights.ContainsKey(position.Security))
				TradeTo(position.Security, 0);
		}

		var portfolioValue = Portfolio.CurrentValue ?? 0m;

		foreach (var kv in _weights)
		{
			var price = GetLatestPrice(kv.Key);
			if (price > 0)
				TradeTo(kv.Key, kv.Value * portfolioValue / price);
		}
	}

	private void TradeTo(Security s, decimal tgt)
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
			Comment = "LexDensity",
		});
	}

	private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;

	private bool TryGetLexicalDensity(Security s, out decimal v)
	{
		v = 0;
		return false;
	}

	private decimal GetLatestPrice(Security security)
	{
		return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
	}
}

