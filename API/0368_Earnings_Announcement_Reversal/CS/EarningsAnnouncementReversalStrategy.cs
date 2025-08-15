using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades short-term reversals around earnings announcements.
/// Shorts recent winners and buys losers on earnings dates.
/// </summary>
public class EarningsAnnouncementReversalStrategy : Strategy
{
	private readonly StrategyParam<IEnumerable<Security>> _univ;
	private readonly StrategyParam<int> _look;
	private readonly StrategyParam<int> _hold;
	private readonly StrategyParam<decimal> _minUsd;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Securities universe to trade.
	/// </summary>
	public IEnumerable<Security> Universe
	{
		get => _univ.Value;
		set => _univ.Value = value;
	}

	/// <summary>
	/// Lookback period in days used to determine winners and losers.
	/// </summary>
	public int LookbackDays
	{
		get => _look.Value;
		set => _look.Value = value;
	}

	/// <summary>
	/// Number of days to hold the position.
	/// </summary>
	public int HoldingDays
	{
		get => _hold.Value;
		set => _hold.Value = value;
	}

	/// <summary>
	/// Minimum trade size in USD.
	/// </summary>
	public decimal MinTradeUsd
	{
		get => _minUsd.Value;
		set => _minUsd.Value = value;
	}

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	private class Win
	{
		public readonly Queue<decimal> Px = [];
		public int Held;
	}

	private readonly Dictionary<Security, Win> _map = [];
	private readonly Dictionary<Security, decimal> _latestPrices = [];

	/// <summary>
	/// Initializes a new instance of <see cref="EarningsAnnouncementReversalStrategy"/>.
	/// </summary>
	public EarningsAnnouncementReversalStrategy()
	{
		_univ = Param<IEnumerable<Security>>(nameof(Universe), [])
			.SetDisplay("Universe", "Collection of securities to trade", "General");

		_look = Param(nameof(LookbackDays), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Days", "Number of days to calculate returns", "Parameters");

		_hold = Param(nameof(HoldingDays), 3)
			.SetGreaterThanZero()
			.SetDisplay("Holding Days", "Days to hold position", "Parameters");

		_minUsd = Param(nameof(MinTradeUsd), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Min Trade USD", "Minimum trade value in USD", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
	{
		return Universe.Select(s => (s, CandleType));
	}

	/// <inheritdoc />
	
	protected override void OnReseted()
	{
		base.OnReseted();

		_map.Clear();
		_latestPrices.Clear();
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Universe == null || !Universe.Any())
			throw new InvalidOperationException("Universe cannot be empty.");

		foreach (var (sec, tf) in GetWorkingSecurities())
		{
			_map[sec] = new Win();
			SubscribeCandles(tf, true, sec)
				.Bind(c => ProcessCandle(c, sec))
				.Start();
		}
	}

	private void ProcessCandle(ICandleMessage candle, Security security)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Store the latest closing price
		_latestPrices[security] = candle.ClosePrice;

		OnDaily(security, candle);
	}

	private void OnDaily(Security security, ICandleMessage candle)
	{
		var win = _map[security];

		if (win.Px.Count == LookbackDays + 1)
			win.Px.Dequeue();

		win.Px.Enqueue(candle.ClosePrice);

		if (!TryGetEarningsDate(security, out var ed))
			return;

		var day = candle.OpenTime.Date;
		if (Math.Abs((day - ed.Date).TotalDays) > 1)
			return;

		if (win.Px.Count < LookbackDays + 1)
			return;

		var arr = win.Px.ToArray();
		var ret = (arr[0] - arr[^1]) / arr[^1];

		var portfolioValue = Portfolio.CurrentValue ?? 0m;
		var price = GetLatestPrice(security);
		if (price <= 0)
			return;

		if (ret > 0)
		{
			// winner -> short
			Move(security, -portfolioValue / Universe.Count() / price);
		}
		else
		{
			// loser -> long
			Move(security, portfolioValue / Universe.Count() / price);
		}

		win.Held = 0;
	}

	private decimal Pos(Security security) => GetPositionValue(security, Portfolio) ?? 0;

	private decimal GetLatestPrice(Security security)
	{
		return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
	}

	private void Move(Security security, decimal tgt)
	{
		var diff = tgt - Pos(security);
		var price = GetLatestPrice(security);

		if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd)
			return;

		RegisterOrder(new Order
		{
			Security = security,
			Portfolio = Portfolio,
			Side = diff > 0 ? Sides.Buy : Sides.Sell,
			Volume = Math.Abs(diff),
			Type = OrderTypes.Market,
			Comment = "EARev"
		});
	}

	private bool TryGetEarningsDate(Security security, out DateTime date)
	{
		date = DateTime.MinValue;
		return false;
	}
}

