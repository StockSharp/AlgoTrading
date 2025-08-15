// TrendFollowingStocksStrategy.cs
// -----------------------------------------------------------------------------
// Breakout trend following: new all‑time high entry, ATR(10) trailing stop exit.
// Daily candles only; no Schedule().
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
/// Breakout trend-following strategy with ATR trailing stop.
/// </summary>
public class TrendFollowingStocksStrategy : Strategy
{
	private readonly StrategyParam<IEnumerable<Security>> _universe;
	private readonly StrategyParam<int> _atrLen;
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

	private class StockInfo
	{
		public List<decimal> Close = [];
		public decimal Trail;
	}
	private readonly Dictionary<Security, StockInfo> _info = [];
	private readonly Dictionary<Security, decimal> _latestPrices = [];

	/// <summary>
	/// List of securities to trade.
	/// </summary>
	public IEnumerable<Security> Universe
	{
		get => _universe.Value;
		set => _universe.Value = value;
	}

	/// <summary>
	/// ATR period length.
	/// </summary>
	public int AtrLen
	{
		get => _atrLen.Value;
		set => _atrLen.Value = value;
	}

	/// <summary>
	/// Minimum trade value in USD.
	/// </summary>
	public decimal MinTradeUsd
	{
		get => _minUsd.Value;
		set => _minUsd.Value = value;
	}

	public TrendFollowingStocksStrategy()
	{
		_universe = Param<IEnumerable<Security>>(nameof(Universe), [])
			.SetDisplay("Universe", "List of securities to trade", "Universe");

		_atrLen = Param(nameof(AtrLen), 10)
			.SetDisplay("ATR Length", "ATR period length", "Parameters");

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

		_latestPrices.Clear();
		_info.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset t)
	{
		if (Universe == null || !Universe.Any())
			throw new InvalidOperationException("Universe cannot be empty.");

		base.OnStarted(t);
		foreach (var (s, tf) in GetWorkingSecurities())
		{
			_info[s] = new StockInfo();
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
		var data = _info[s];
		data.Close.Add(c.ClosePrice);
		if (data.Close.Count > 3000)
			data.Close.RemoveAt(0);

		if (data.Close.Count < AtrLen + 1)
			return;

		var atr = data.Close.Zip(data.Close.Skip(1), (p0, p1) => Math.Abs(p0 - p1))
							.TakeLast(AtrLen).Average();
		var trailCandidate = c.ClosePrice - atr;

		var portfolioValue = Portfolio.CurrentValue ?? 0m;
		var universeCount = Universe.Count();

		// Entry
		if (c.ClosePrice >= data.Close.Max() && PositionBy(s) == 0 && universeCount > 0)
		{
			data.Trail = trailCandidate;
			Move(s, portfolioValue / universeCount / c.ClosePrice);
		}

		// Exit
		if (PositionBy(s) > 0 && c.ClosePrice <= data.Trail)
			Move(s, 0);

		// Update trailing stop upwards
		if (PositionBy(s) > 0 && trailCandidate > data.Trail)
			data.Trail = trailCandidate;
	}

	private decimal GetLatestPrice(Security security)
	{
		return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
	}

	private void Move(Security s, decimal tgtQty)
	{
		var diff = tgtQty - PositionBy(s);
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
			Comment = "TrendFollow"
		});
	}

	private decimal PositionBy(Security s) =>
		GetPositionValue(s, Portfolio) ?? 0;
}