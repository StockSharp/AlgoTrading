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

/// <summary>ROAEffect Stocks with point-in-time inputs and execution-based portfolio accounting.</summary>
public class ROAEffectStocksStrategy : Strategy
{
	private readonly StrategyParam<IEnumerable<Security>> _universe;
	private readonly StrategyParam<decimal> _capital;
	private readonly StrategyParam<decimal> _leverage;
	private readonly StrategyParam<decimal> _minTradeUsd;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<string> _roaFile;
	private readonly StrategyParam<int> _decile;
	private readonly StrategyParam<int> _maxScoreAgeDays;
	private sealed record Score(Security Security, DateTime Period, DateTime Published, decimal Value);
	private readonly List<Score> _scores = [];
	private Security[] _securities = [];
	private readonly Dictionary<Security, (DateTime time, decimal price, DateTime closed)> _latest = [];
	private readonly Dictionary<Security, decimal> _targets = [];
	private readonly List<Order> _pending = [];
	private int _lastRebalance;
	private DateTime? _lastBar;

	/// <summary>Explicit execution universe with native volume metadata.</summary>
	public IEnumerable<Security> Universe { get => _universe.Value; set => _universe.Value = value; }

	/// <summary>Fixed USD capital budget.</summary>
	public decimal Capital { get => _capital.Value; set => _capital.Value = value; }

	/// <summary>Gross exposure divided by capital.</summary>
	public decimal Leverage { get => _leverage.Value; set => _leverage.Value = value; }

	/// <summary>Minimum USD adjustment; complete liquidation is exempt.</summary>
	public decimal MinTradeUsd { get => _minTradeUsd.Value; set => _minTradeUsd.Value = value; }

	/// <summary>Finished execution candle type.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>Required CSV of externally published scores.</summary>
	public string RoaFile { get => _roaFile.Value; set => _roaFile.Value = value; }

	/// <summary>Number of equal monthly rank buckets.</summary>
	public int Decile { get => _decile.Value; set => _decile.Value = value; }

	/// <summary>Maximum age of a published score's period end.</summary>
	public int MaxScoreAgeDays { get => _maxScoreAgeDays.Value; set => _maxScoreAgeDays.Value = value; }

	/// <summary>Initialize the strategy parameters.</summary>
	public ROAEffectStocksStrategy()
	{
		_universe = Param<IEnumerable<Security>>(nameof(Universe)).SetRequired()
			.SetDisplay("Universe", "Explicit execution universe with native volume metadata.", "Parameters");
		_capital = Param(nameof(Capital), 100000m).SetRange(0.000001m, decimal.MaxValue)
			.SetDisplay("Capital", "Fixed USD capital budget.", "Parameters");
		_leverage = Param(nameof(Leverage), 1m).SetRange(0.000001m, decimal.MaxValue)
			.SetDisplay("Leverage", "Gross exposure divided by capital.", "Parameters");
		_minTradeUsd = Param(nameof(MinTradeUsd), 200m).SetRange(0m, decimal.MaxValue)
			.SetDisplay("MinTradeUsd", "Minimum USD adjustment; complete liquidation is exempt.", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("CandleType", "Finished execution candle type.", "Parameters");
		_roaFile = Param<string>(nameof(RoaFile)).SetRequired()
			.SetDisplay("RoaFile", "Required CSV of externally published scores.", "Parameters");
		_decile = Param(nameof(Decile), 10).SetRange(2, int.MaxValue)
			.SetDisplay("Decile", "Number of equal monthly rank buckets.", "Parameters");
		_maxScoreAgeDays = Param(nameof(MaxScoreAgeDays), 400).SetRange(1, int.MaxValue)
			.SetDisplay("MaxScoreAgeDays", "Maximum age of a published score's period end.", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> (Universe ?? []).Where(s => s is not null).DistinctBy(s => s.Id, StringComparer.OrdinalIgnoreCase).Select(s => (s, CandleType));

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_securities = [];
		_latest.Clear();
		_scores.Clear();
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
		foreach (var security in _securities)
		{
			if (security.VolumeStep is not > 0m || security.MinVolume is not > 0m || security.MaxVolume is not > 0m)
				throw new InvalidOperationException("Every security needs positive VolumeStep, MinVolume and MaxVolume.");
		}
		LoadScores();
		base.OnStarted2(time);
		foreach (var security in _securities)
			SubscribeCandles(CandleType, security: security).Bind(candle => ProcessCandle(security, candle)).Start();
	}

	private void ProcessCandle(Security security, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || candle.ClosePrice <= 0m) return;
		_latest[security] = (candle.OpenTime, candle.ClosePrice, candle.CloseTime);
		if (_latest.Count != _securities.Length || _latest.Values.Any(v => v.time != candle.OpenTime)
			|| _lastBar == candle.OpenTime || !IsFormedAndOnlineAndAllowTrading()) return;
		_lastBar = candle.OpenTime;
		var month = candle.OpenTime.Year * 100 + candle.OpenTime.Month;
		if (_lastRebalance != month && SelectTargets(candle.OpenTime, _latest.Values.Max(v => v.closed)))
			_lastRebalance = month;
		ReconcileTargets();
	}

	private decimal Budget => Capital * Leverage;

	private decimal UnitPrice(Security security) => _latest[security].price;

	private decimal Units(Security security, decimal notional)
		=> Math.Floor(notional / UnitPrice(security) / security.VolumeStep.Value) * security.VolumeStep.Value;

	private void ReconcileTargets()
	{
		if (_pending.Any(o => o.State is not (OrderStates.Done or OrderStates.Failed))) return;
		_pending.Clear();
		foreach (var security in _securities)
		{
			if (!_targets.TryGetValue(security, out var target)) continue;
			var delta = target - GetPositionValue(security, Portfolio).GetValueOrDefault();
			if (delta == 0m || (target != 0m && Math.Abs(delta) * UnitPrice(security) < MinTradeUsd)) continue;
			var step = security.VolumeStep.Value;
			var quantity = Math.Floor(Math.Min(Math.Abs(delta), security.MaxVolume.Value) / step) * step;
			if (quantity < security.MinVolume.Value) continue;
			var order = new Order
			{
				Security = security, Portfolio = Portfolio, Type = OrderTypes.Market,
				Side = delta > 0m ? Sides.Buy : Sides.Sell, Volume = quantity, Comment = "0393Rebalance",
			};
			_pending.Add(order);
			RegisterOrder(order);
		}
	}

	private static DateTime ParseUtc(string value)
		=> DateTime.ParseExact(value.Trim(), "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

	private static IEnumerable<string[]> ReadRows(string path, string header, int fields)
	{
		using var lines = File.ReadLines(path).GetEnumerator();
		if (!lines.MoveNext() || lines.Current.Trim() != header)
			throw new InvalidOperationException("Invalid CSV header: expected " + header + ".");
		while (lines.MoveNext())
		{
			if (string.IsNullOrWhiteSpace(lines.Current)) continue;
			var row = lines.Current.Split(',');
			if (row.Length != fields) throw new InvalidOperationException("Invalid CSV field count.");
			yield return row;
		}
	}

	private void LoadScores()
	{
		var keys = new HashSet<(Security, DateTime, DateTime)>();
		foreach (var row in ReadRows(RoaFile, "SecurityId,PeriodEnd,PublishedAt,Score", 4))
		{
			var security = _securities.SingleOrDefault(s => string.Equals(s.Id, row[0].Trim(), StringComparison.OrdinalIgnoreCase));
			if (security is null) continue;
			var period = DateTime.ParseExact(row[1].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
			var published = ParseUtc(row[2]);
			var value = decimal.Parse(row[3], CultureInfo.InvariantCulture);
			if (published.Date < period || !keys.Add((security, period, published)))
				throw new InvalidOperationException("Scores need causal, unique publication timestamps.");
			_scores.Add(new(security, period, published, value));
		}
	}

	private bool SelectTargets(DateTime time, DateTime asOf)
	{
		var scores = new List<(Security security, decimal score)>();
		foreach (var security in _securities)
		{
			var score = _scores.Where(r => r.Security == security && r.Published <= asOf && r.Period <= time.Date)
				.OrderByDescending(r => r.Period).ThenByDescending(r => r.Published).FirstOrDefault();
			if (score is null || (time.Date - score.Period).TotalDays > MaxScoreAgeDays) continue;
			scores.Add((security, score.Value));
		}
		if (scores.Count < Decile) return false;
		var ranked = scores.OrderByDescending(x => x.score).ThenBy(x => x.security.Id, StringComparer.Ordinal).Select(x => x.security).ToArray();
		var count = ranked.Length / Decile;
		SetBasketTargets(ranked.Take(count), ranked.TakeLast(count), Budget);
		return true;
	}

	private void SetBasketTargets(IEnumerable<Security> longBasket, IEnumerable<Security> shortBasket, decimal grossCapital)
	{
		var longs = longBasket.ToArray();
		var shorts = shortBasket.ToArray();
		foreach (var security in _securities) _targets[security] = 0m;
		foreach (var security in longs) _targets[security] = Units(security, grossCapital / (2m * longs.Length));
		foreach (var security in shorts) _targets[security] = -Units(security, grossCapital / (2m * shorts.Length));
	}
}