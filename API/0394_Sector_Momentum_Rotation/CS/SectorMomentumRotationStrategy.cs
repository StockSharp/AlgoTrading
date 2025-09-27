// SectorMomentumRotationStrategy.cs
// -----------------------------------------------------------------------------
// Monthly rotate among sector ETFs: hold sectors with positive 6‑month momentum.
// Equal-weight those sectors; flat otherwise.
// Trigger via daily candle of first sector ETF.
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
/// Monthly sector momentum rotation strategy.
/// </summary>
public class SectorMomentumRotationStrategy : Strategy
{
	private readonly StrategyParam<IEnumerable<Security>> _sects;
	private readonly StrategyParam<int> _look;
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
	private readonly Dictionary<Security, RollingWin> _px = [];
	private readonly Dictionary<Security, decimal> _latestPrices = [];
	private DateTime _last = DateTime.MinValue;

	/// <summary>
	/// Sector ETFs to trade.
	/// </summary>
	public IEnumerable<Security> SectorETFs
	{
		get => _sects.Value;
		set => _sects.Value = value;
	}

	/// <summary>
	/// Lookback window in days.
	/// </summary>
	public int LookbackDays
	{
		get => _look.Value;
		set => _look.Value = value;
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
	/// Initializes a new instance of the <see cref="SectorMomentumRotationStrategy"/> class.
	/// </summary>
	public SectorMomentumRotationStrategy()
	{
		_sects = Param<IEnumerable<Security>>(nameof(SectorETFs), [])
			.SetDisplay("Sectors", "Sector ETFs to trade", "General");

		_look = Param(nameof(LookbackDays), 126)
			.SetDisplay("Lookback", "Lookback window in days", "General");

		_minUsd = Param(nameof(MinTradeUsd), 200m)
			.SetDisplay("Min Trade USD", "Minimum dollar value per trade", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return SectorETFs.Select(s => (s, CandleType));
	}

	/// <inheritdoc />
	
	protected override void OnReseted()
	{
		base.OnReseted();

		_px.Clear();
		_latestPrices.Clear();
		_last = default;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (SectorETFs == null || !SectorETFs.Any())
			throw new InvalidOperationException("Sectors cannot be empty.");

		var trig = SectorETFs.First();
		SubscribeCandles(CandleType, true, trig)
			.Bind(c => ProcessCandle(c, trig))
			.Start();

		foreach (var s in SectorETFs)
			_px[s] = new RollingWin(LookbackDays + 1);

		foreach (var (s, tf) in GetWorkingSecurities())
			SubscribeCandles(tf, true, s).Bind(c => ProcessDataCandle(c, s)).Start();
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

	private void ProcessDataCandle(ICandleMessage candle, Security security)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Store the latest closing price for this security
		_latestPrices[security] = candle.ClosePrice;

		// Add to price history
		_px[security].Add(candle.ClosePrice);
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
		var winners = new List<Security>();
		foreach (var kv in _px)
		{
			if (kv.Value.Full && kv.Value.Data[0] > kv.Value.Data[^1])
				winners.Add(kv.Key);
		}

		foreach (var s in SectorETFs.Where(x => !winners.Contains(x)))
			Move(s, 0);

		if (!winners.Any())
			return;

		decimal w = 1m / winners.Count;
		var portfolioValue = Portfolio.CurrentValue ?? 0m;

		foreach (var s in winners)
		{
			var price = GetLatestPrice(s);
			if (price > 0)
				Move(s, w * portfolioValue / price);
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
			Comment = "SectMom",
		});
	}

	private decimal Pos(Security s) => GetPositionValue(s, Portfolio) ?? 0;

	private class RollingWin
	{
		private readonly Queue<decimal> _q = [];
		private readonly int _n;

		public RollingWin(int n)
		{
			_n = n;
		}

		public bool Full => _q.Count == _n;

		public void Add(decimal p)
		{
			if (_q.Count == _n)
				_q.Dequeue();
			_q.Enqueue(p);
		}

		public decimal[] Data => [.. _q];
	}
}
