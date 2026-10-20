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

/// <summary>F-Score Reversal with point-in-time inputs and execution-based portfolio accounting.</summary>
public class FScoreReversalStrategy : Strategy
{
	private readonly StrategyParam<IEnumerable<Security>> _universe;
	private readonly StrategyParam<decimal> _capital;
	private readonly StrategyParam<decimal> _leverage;
	private readonly StrategyParam<decimal> _minTradeUsd;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<string> _priceHistoryFile;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<int> _fHi;
	private readonly StrategyParam<int> _fLo;
	private readonly StrategyParam<bool> _isShortEnabled;
	private readonly StrategyParam<int> _maxReportAgeDays;
	private readonly StrategyParam<string> _fScoreFile;
	private Security[] _securities = [];
	private readonly Dictionary<Security, (DateTime time, decimal price, DateTime closed)> _latest = [];
	private readonly Dictionary<Security, SortedDictionary<DateTime, decimal>> _history = [];
	private readonly Dictionary<Security, decimal> _targets = [];
	private readonly List<Order> _pending = [];
	private int _lastRebalance;
	private DateTime? _lastBar;
	private readonly Dictionary<Security, decimal> _nextTargets = [];
	private bool _isRotating;
	private bool _isReconciling;

	private sealed record Score(Security Security, DateTime Period, DateTime Published, int Value);
	private readonly List<Score> _scores = [];

	/// <summary>Explicit execution securities with valid native volume metadata.</summary>
	public IEnumerable<Security> Universe { get => _universe.Value; set => _universe.Value = value; }

	/// <summary>Fixed capital budget in USD.</summary>
	public decimal Capital { get => _capital.Value; set => _capital.Value = value; }

	/// <summary>Gross target exposure divided by the capital budget.</summary>
	public decimal Leverage { get => _leverage.Value; set => _leverage.Value = value; }

	/// <summary>Minimum USD adjustment; complete liquidation bypasses this filter.</summary>
	public decimal MinTradeUsd { get => _minTradeUsd.Value; set => _minTradeUsd.Value = value; }

	/// <summary>Finished execution candle type.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>Optional daily close CSV; future dates cannot prefill a decision.</summary>
	public string PriceHistoryFile { get => _priceHistoryFile.Value; set => _priceHistoryFile.Value = value; }

	/// <summary>Completed daily returns in the reversal window.</summary>
	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }

	/// <summary>Inclusive minimum published Piotroski score for the long leg.</summary>
	public int FHi { get => _fHi.Value; set => _fHi.Value = value; }

	/// <summary>Inclusive maximum published Piotroski score for the short leg.</summary>
	public int FLo { get => _fLo.Value; set => _fLo.Value = value; }

	/// <summary>Whether to allocate the optional weak-score short leg.</summary>
	public bool IsShortEnabled { get => _isShortEnabled.Value; set => _isShortEnabled.Value = value; }

	/// <summary>Maximum age of the latest fiscal report in calendar days.</summary>
	public int MaxReportAgeDays { get => _maxReportAgeDays.Value; set => _maxReportAgeDays.Value = value; }

	/// <summary>Required published integer Piotroski scores from zero through nine.</summary>
	public string FScoreFile { get => _fScoreFile.Value; set => _fScoreFile.Value = value; }

	/// <summary>Initialize the strategy parameters.</summary>
	public FScoreReversalStrategy()
	{
		_universe = Param<IEnumerable<Security>>(nameof(Universe)).SetRequired()
			.SetDisplay("Universe", "Explicit execution securities with valid native volume metadata", "Parameters");
		_capital = Param(nameof(Capital), 100000m).SetGreaterThanZero()
			.SetDisplay("Capital", "Fixed capital budget in USD", "Parameters");
		_leverage = Param(nameof(Leverage), 1m).SetGreaterThanZero()
			.SetDisplay("Leverage", "Gross target exposure divided by the capital budget", "Parameters");
		_minTradeUsd = Param(nameof(MinTradeUsd), 100m).SetRange(0m, decimal.MaxValue)
			.SetDisplay("MinTradeUsd", "Minimum USD adjustment; complete liquidation bypasses this filter", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1440).TimeFrame())
			.SetDisplay("CandleType", "Finished execution candle type", "Parameters");
		_priceHistoryFile = Param<string>(nameof(PriceHistoryFile))
			.SetDisplay("PriceHistoryFile", "Optional daily close CSV; future dates cannot prefill a decision", "Parameters");
		_lookback = Param(nameof(Lookback), 21).SetRange(1, int.MaxValue)
			.SetDisplay("Lookback", "Completed daily returns in the reversal window", "Parameters");
		_fHi = Param(nameof(FHi), 7).SetRange(0, 9)
			.SetDisplay("FHi", "Inclusive minimum published Piotroski score for the long leg", "Parameters");
		_fLo = Param(nameof(FLo), 3).SetRange(0, 9)
			.SetDisplay("FLo", "Inclusive maximum published Piotroski score for the short leg", "Parameters");
		_isShortEnabled = Param(nameof(IsShortEnabled), true)
			.SetDisplay("IsShortEnabled", "Whether to allocate the optional weak-score short leg", "Parameters");
		_maxReportAgeDays = Param(nameof(MaxReportAgeDays), 550).SetRange(1, int.MaxValue)
			.SetDisplay("MaxReportAgeDays", "Maximum age of the latest fiscal report in calendar days", "Parameters");
		_fScoreFile = Param<string>(nameof(FScoreFile)).SetRequired()
			.SetDisplay("FScoreFile", "Required published integer Piotroski scores from zero through nine", "Parameters");
		Trades.TradeAdded += _ => ContinueRotation();
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
		_history.Clear();
		_targets.Clear();
		_pending.Clear();
		_lastRebalance = 0;
		_lastBar = null;
		_nextTargets.Clear();
		_isRotating = false;
		_isReconciling = false;
		_scores.Clear();
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
			_history[security] = [];
		}
		LoadPriceHistory();
		if (FHi <= FLo) throw new InvalidOperationException("FHi must be greater than FLo.");
		LoadScores();
		base.OnStarted2(time);
		foreach (var security in _securities)
			SubscribeCandles(CandleType, security: security).Bind(candle => ProcessCandle(security, candle)).Start();
	}

	private void ProcessCandle(Security security, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || candle.ClosePrice <= 0m) return;
		_latest[security] = (candle.OpenTime, candle.ClosePrice, candle.CloseTime);
		_history[security][candle.OpenTime.Date] = candle.ClosePrice;
		if (_latest.Count != _securities.Length || _latest.Values.Any(v => v.time != candle.OpenTime)
			|| _lastBar == candle.OpenTime || !IsFormedAndOnlineAndAllowTrading()) return;
		_lastBar = candle.OpenTime;
		var month = candle.OpenTime.Year * 100 + candle.OpenTime.Month;
		if (!_isRotating && _lastRebalance != month && SelectTargets(candle.OpenTime, _latest.Values.Max(v => v.closed)))
			_lastRebalance = month;
		ReconcileTargets();
	}

	private decimal Budget => Capital * Leverage;

	private decimal UnitPrice(Security security) => _latest[security].price;

	private decimal Units(Security security, decimal notional)
		=> Math.Floor(notional / UnitPrice(security) / security.VolumeStep.Value) * security.VolumeStep.Value;

	private void ReconcileTargets()
	{
		if (_isReconciling) return;
		if (_pending.Any(o => o.State is not (OrderStates.Done or OrderStates.Failed))) return;
		_pending.Clear();
		_isReconciling = true;
		try
		{
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
					Side = delta > 0m ? Sides.Buy : Sides.Sell, Volume = quantity, Comment = "0373Rebalance",
				};
				_pending.Add(order);
				RegisterOrder(order);
			}
		}
		finally
		{
			_isReconciling = false;
		}
		ContinueRotation();
	}

	private void BeginRotation(Dictionary<Security, decimal> targets)
	{
		_nextTargets.Clear();
		foreach (var target in targets) _nextTargets[target.Key] = target.Value;
		foreach (var security in _securities) _targets[security] = 0m;
		_isRotating = true;
	}

	private void ContinueRotation()
	{
		if (!_isRotating || _isReconciling || !IsFormedAndOnlineAndAllowTrading()
			|| _pending.Any(o => o.State is not (OrderStates.Done or OrderStates.Failed))
			|| _securities.Any(s => GetPositionValue(s, Portfolio).GetValueOrDefault() != 0m)) return;
		_isRotating = false;
		foreach (var security in _securities) _targets[security] = _nextTargets.GetValueOrDefault(security);
		_nextTargets.Clear();
		ReconcileTargets();
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

	private DateTime LastClosedDate(DateTime time)
		=> CandleType.Arg is TimeSpan frame && frame == TimeSpan.FromDays(1) ? time.Date : time.Date.AddDays(-1);

	private decimal[] Daily(Security security, DateTime time, int count, DateTime cutoff)
		=> _history[security].Where(x => x.Key <= LastClosedDate(time) && x.Key < cutoff).Select(x => x.Value).TakeLast(count).ToArray();

	private void LoadPriceHistory()
	{
		if (string.IsNullOrWhiteSpace(PriceHistoryFile)) return;
		foreach (var row in ReadRows(PriceHistoryFile, "SecurityId,Date,Close", 3))
		{
			var security = _securities.SingleOrDefault(s => string.Equals(s.Id, row[0].Trim(), StringComparison.OrdinalIgnoreCase));
			if (security is null) continue;
			var date = DateTime.ParseExact(row[1].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
			var price = decimal.Parse(row[2], CultureInfo.InvariantCulture);
			if (price <= 0m || !_history[security].TryAdd(date, price))
				throw new InvalidOperationException("Daily prices must be positive and unique per security and UTC date.");
		}
	}

	private void LoadScores()
	{
		var keys = new HashSet<(Security, DateTime, DateTime)>();
		foreach (var row in ReadRows(FScoreFile, "SecurityId,PeriodEnd,PublishedAt,FScore", 4))
		{
			var security = _securities.SingleOrDefault(s => string.Equals(s.Id, row[0].Trim(), StringComparison.OrdinalIgnoreCase));
			if (security is null) continue;
			var period = DateTime.ParseExact(row[1].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
			var published = ParseUtc(row[2]);
			var score = int.Parse(row[3], CultureInfo.InvariantCulture);
			if (score is < 0 or > 9 || published.Date < period || !keys.Add((security, period, published)))
				throw new InvalidOperationException("Piotroski scores must be integers 0..9 with causal, unique publication dates.");
			_scores.Add(new(security, period, published, score));
		}
	}

	private bool SelectTargets(DateTime time, DateTime asOf)
	{
		var candidates = new List<(Security security, int score, decimal momentum)>();
		foreach (var security in _securities)
		{
			var report = _scores.Where(r => r.Security == security && r.Published <= asOf)
				.OrderByDescending(r => r.Period).ThenByDescending(r => r.Published).FirstOrDefault();
			var prices = Daily(security, time, Lookback + 1, DateTime.MaxValue);
			if (report is null || (time.Date - report.Period).TotalDays > MaxReportAgeDays || prices.Length != Lookback + 1) continue;
			candidates.Add((security, report.Value, prices[^1] / prices[0] - 1m));
		}
		if (candidates.Count == 0) return false;
		var longLeg = candidates.Where(x => x.score >= FHi).OrderBy(x => x.momentum).ThenBy(x => x.security.Id, StringComparer.Ordinal).FirstOrDefault().security;
		var shortLeg = IsShortEnabled ? candidates.Where(x => x.score <= FLo).OrderByDescending(x => x.momentum).ThenBy(x => x.security.Id, StringComparer.Ordinal).FirstOrDefault().security : null;
		var targets = new Dictionary<Security, decimal>();
		if (longLeg is not null) targets[longLeg] = Units(longLeg, Budget / 2m);
		if (shortLeg is not null) targets[shortLeg] = -Units(shortLeg, Budget / 2m);
		BeginRotation(targets);
		return true;
	}
}
