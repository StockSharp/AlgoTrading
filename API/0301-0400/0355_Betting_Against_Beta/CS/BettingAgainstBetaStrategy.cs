namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>Betting Against Beta with explicit inputs and execution-based portfolio accounting.</summary>
public class BettingAgainstBetaStrategy : Strategy
{
	private readonly StrategyParam<IEnumerable<Security>> _universe;
	private readonly StrategyParam<decimal> _capital;
	private readonly StrategyParam<decimal> _leverage;
	private readonly StrategyParam<decimal> _minTradeUsd;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _deciles;
	private readonly StrategyParam<string> _priceHistoryFile;
	private readonly StrategyParam<Security> _benchmarkSecurity;
	private readonly StrategyParam<int> _windowDays;
	private Security[] _securities = [];
	private readonly Dictionary<Security, (DateTime time, decimal price)> _latest = [];
	private readonly Dictionary<Security, SortedDictionary<DateTime, decimal>> _history = [];
	private readonly Dictionary<Security, decimal> _targets = [];
	private readonly List<Order> _pending = [];
	private int _lastRebalance;
	private DateTime? _lastBar;

	/// <summary>Execution securities with USD prices and valid volume metadata.</summary>
	public IEnumerable<Security> Universe { get => _universe.Value; set => _universe.Value = value; }

	/// <summary>Fixed gross capital budget in USD.</summary>
	public decimal Capital { get => _capital.Value; set => _capital.Value = value; }

	/// <summary>Gross target exposure divided by the capital budget.</summary>
	public decimal Leverage { get => _leverage.Value; set => _leverage.Value = value; }

	/// <summary>Minimum notional adjustment; complete liquidation bypasses this filter.</summary>
	public decimal MinTradeUsd { get => _minTradeUsd.Value; set => _minTradeUsd.Value = value; }

	/// <summary>Closed candle type; daily statistics count distinct UTC dates.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>Number of cross-sectional buckets, requiring at least this many eligible securities.</summary>
	public int Deciles { get => _deciles.Value; set => _deciles.Value = value; }

	/// <summary>Optional daily close CSV for warmup; current and future dates cannot prefill a decision.</summary>
	public string PriceHistoryFile { get => _priceHistoryFile.Value; set => _priceHistoryFile.Value = value; }

	/// <summary>Market benchmark for synchronized daily simple returns.</summary>
	public Security BenchmarkSecurity { get => _benchmarkSecurity.Value; set => _benchmarkSecurity.Value = value; }

	/// <summary>Daily return pairs required by each covariance estimate.</summary>
	public int WindowDays { get => _windowDays.Value; set => _windowDays.Value = value; }

	/// <summary>Initialize the published strategy parameters.</summary>
	public BettingAgainstBetaStrategy()
	{
		_universe = Param<IEnumerable<Security>>(nameof(Universe)).SetRequired()
			.SetDisplay("Universe", "Execution securities with USD prices and valid volume metadata", "Parameters");
		_capital = Param(nameof(Capital), 100000m).SetGreaterThanZero()
			.SetDisplay("Capital", "Fixed gross capital budget in USD", "Parameters");
		_leverage = Param(nameof(Leverage), 1m).SetGreaterThanZero()
			.SetDisplay("Leverage", "Gross target exposure divided by the capital budget", "Parameters");
		_minTradeUsd = Param(nameof(MinTradeUsd), 100m).SetRange(0m, decimal.MaxValue)
			.SetDisplay("MinTradeUsd", "Minimum notional adjustment; complete liquidation bypasses this filter", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("CandleType", "Closed candle type; daily statistics count distinct UTC dates", "Parameters");
		_deciles = Param(nameof(Deciles), 10).SetRange(2, 10000)
			.SetDisplay("Deciles", "Number of cross-sectional buckets, requiring at least this many eligible securities", "Parameters");
		_priceHistoryFile = Param<string>(nameof(PriceHistoryFile))
			.SetDisplay("PriceHistoryFile", "Optional daily close CSV for warmup; current and future dates cannot prefill a decision", "Parameters");
		_benchmarkSecurity = Param<Security>(nameof(BenchmarkSecurity)).SetRequired()
			.SetDisplay("BenchmarkSecurity", "Market benchmark for synchronized daily simple returns", "Parameters");
		_windowDays = Param(nameof(WindowDays), 252).SetGreaterThanZero()
			.SetDisplay("WindowDays", "Daily return pairs required by each covariance estimate", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		foreach (var security in (Universe ?? []).Concat(BenchmarkSecurity is null ? [] : new[] { BenchmarkSecurity }).DistinctBy(s => s.Id, StringComparer.OrdinalIgnoreCase))
			yield return (security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_securities = [];
		_latest.Clear();
		_history.Clear();
		_targets.Clear();
		_pending.Clear();
		_lastRebalance = 0;
		_lastBar = null;

	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		_securities = (Universe ?? []).ToArray();
		if (_securities.Length == 0 || _securities.Any(s => s is null || string.IsNullOrWhiteSpace(s.Id))
			|| _securities.Select(s => s.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != _securities.Length)
			throw new InvalidOperationException("Universe must contain distinct execution securities.");
		foreach (var security in GetWorkingSecurities().Select(x => x.sec))
		{
			if (security.VolumeStep is not > 0m || security.MinVolume is not > 0m || security.MaxVolume is not > 0m)
				throw new InvalidOperationException("Every security needs positive VolumeStep, MinVolume and MaxVolume.");
			_history[security] = [];
		}

		if (BenchmarkSecurity is null) throw new InvalidOperationException("BenchmarkSecurity is required.");
		LoadPriceHistory();
		base.OnStarted2(time);
		foreach (var security in GetWorkingSecurities().Select(x => x.sec))
			SubscribeCandles(CandleType, security: security).Bind(candle => ProcessCandle(security, candle)).Start();
	}

	private void ProcessCandle(Security security, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || candle.ClosePrice <= 0m) return;
		_latest[security] = (candle.OpenTime, candle.ClosePrice);
		_history[security][candle.OpenTime.Date] = candle.ClosePrice;
		if (_latest.Count != _history.Count || _latest.Values.Any(v => v.time != candle.OpenTime)
			|| _lastBar == candle.OpenTime || !IsFormedAndOnlineAndAllowTrading()) return;
		_lastBar = candle.OpenTime;
		var month = candle.OpenTime.Year * 100 + candle.OpenTime.Month;

		if (_lastRebalance != month && SelectTargets(candle.OpenTime))
			_lastRebalance = month;

		ReconcileTargets();
	}

	private decimal[] Daily(Security security, DateTime time)
		=> _history[security].Where(x => x.Key <= LastClosedDate(time)).Select(x => x.Value).ToArray();

	private DateTime LastClosedDate(DateTime time)
		=> CandleType.Arg is TimeSpan frame && frame == TimeSpan.FromDays(1) ? time.Date : time.Date.AddDays(-1);

	private decimal Units(Security security, decimal notional)
		=> Math.Floor(notional / _latest[security].price / security.VolumeStep.Value) * security.VolumeStep.Value;

	private void ReconcileTargets()
	{
		if (_pending.Any(o => o.State is not (OrderStates.Done or OrderStates.Failed))) return;
		_pending.Clear();
		foreach (var security in _securities)
		{
			if (!_targets.TryGetValue(security, out var target)) continue;
			var delta = target - GetPositionValue(security, Portfolio).GetValueOrDefault();
			if (delta == 0m || (target != 0m && Math.Abs(delta) * _latest[security].price < MinTradeUsd)) continue;
			var step = security.VolumeStep.Value;
			var quantity = Math.Floor(Math.Min(Math.Abs(delta), security.MaxVolume.Value) / step) * step;
			if (quantity < security.MinVolume.Value) continue;
			var order = new Order
			{
				Security = security, Portfolio = Portfolio, Type = OrderTypes.Market,
				Side = delta > 0m ? Sides.Buy : Sides.Sell, Volume = quantity, Comment = "0355Rebalance",
			};
			_pending.Add(order);
			RegisterOrder(order);
		}
	}

	private void LoadPriceHistory()
	{
		if (string.IsNullOrWhiteSpace(PriceHistoryFile)) return;
		var lines = File.ReadLines(PriceHistoryFile).GetEnumerator();
		using (lines)
		{
			if (!lines.MoveNext() || lines.Current.Trim() != "SecurityId,Date,Close")
				throw new InvalidOperationException("Price history must begin with SecurityId,Date,Close.");
			while (lines.MoveNext())
			{
				if (string.IsNullOrWhiteSpace(lines.Current)) continue;
				var row = lines.Current.Split(',');
				if (row.Length != 3) throw new InvalidOperationException("A daily price row must have three fields.");
				var security = _history.Keys.SingleOrDefault(s => string.Equals(s.Id, row[0].Trim(), StringComparison.OrdinalIgnoreCase));
				if (security is null) continue;
				var date = DateTime.ParseExact(row[1].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
				var price = decimal.Parse(row[2], CultureInfo.InvariantCulture);
				if (price <= 0m || !_history[security].TryAdd(date, price))
					throw new InvalidOperationException("Daily prices must be positive and unique per security and UTC date.");
			}
		}
	}

	private bool SelectTargets(DateTime time)
	{
		var dates = _securities.Append(BenchmarkSecurity).Distinct()
			.Select(s => _history[s].Keys.Where(d => d <= LastClosedDate(time)).ToHashSet())
			.Aggregate((a, b) => { a.IntersectWith(b); return a; }).OrderBy(d => d).TakeLast(WindowDays + 1).ToArray();
		if (dates.Length < WindowDays + 1) return false;
		decimal[] Returns(Security security) => dates.Skip(1).Select((d, i) => _history[security][d] / _history[security][dates[i]] - 1m).ToArray();
		var benchmark = Returns(BenchmarkSecurity);
		var mean = benchmark.Average();
		var variance = benchmark.Sum(r => (r - mean) * (r - mean));
		if (variance <= 0m) return false;
		var ranked = _securities.Select(s =>
		{
			var returns = Returns(s);
			var average = returns.Average();
			var beta = returns.Select((r, i) => (r - average) * (benchmark[i] - mean)).Sum() / variance;
			return (security: s, beta);
		}).OrderBy(x => x.beta).ThenBy(x => x.security.Id, StringComparer.Ordinal).Select(x => x.security).ToArray();
		if (ranked.Length < Deciles) return false;
		var count = ranked.Length / Deciles;
		SetBasketTargets(ranked.Take(count), ranked.TakeLast(count), Capital * Leverage);
		return true;
	}

	private void SetBasketTargets(IEnumerable<Security> longBasket, IEnumerable<Security> shortBasket, decimal grossCapital)
	{
		var longs = longBasket.ToArray();
		var shorts = shortBasket.ToArray();
		foreach (var s in _securities) _targets[s] = 0m;
		foreach (var s in longs) _targets[s] = Units(s, grossCapital / (2m * longs.Length));
		foreach (var s in shorts) _targets[s] = -Units(s, grossCapital / (2m * shorts.Length));
	}

}
