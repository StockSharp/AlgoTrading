namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Ecng.Common;
using Ecng.ComponentModel;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>Lexical Density Filings with point-in-time inputs and execution-based portfolio accounting.</summary>
public class LexicalDensityFilingsStrategy : Strategy
{
	private readonly StrategyParam<IEnumerable<Security>> _universe;
	private readonly StrategyParam<decimal> _capital;
	private readonly StrategyParam<decimal> _leverage;
	private readonly StrategyParam<decimal> _minTradeUsd;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _quintile;
	private readonly StrategyParam<int> _maxReportAgeDays;
	private readonly StrategyParam<string> _filingsFile;
	private Security[] _securities = [];
	private readonly Dictionary<Security, (DateTime time, decimal price, DateTime closed)> _latest = [];
	private readonly Dictionary<Security, decimal> _targets = [];
	private readonly List<Order> _pending = [];
	private int _lastRebalance;
	private DateTime? _lastBar;

	private sealed record Filing(Security Security, DateTime Period, DateTime Published, decimal Density);
	private readonly List<Filing> _filings = [];

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

	/// <summary>Number of lexical-density buckets.</summary>
	public int Quintile { get => _quintile.Value; set => _quintile.Value = value; }

	/// <summary>Maximum age of the latest filing period end in calendar days.</summary>
	public int MaxReportAgeDays { get => _maxReportAgeDays.Value; set => _maxReportAgeDays.Value = value; }

	/// <summary>Required filing manifest containing publication dates and document paths.</summary>
	public string FilingsFile { get => _filingsFile.Value; set => _filingsFile.Value = value; }

	/// <summary>Initialize the strategy parameters.</summary>
	public LexicalDensityFilingsStrategy()
	{
		_universe = Param<IEnumerable<Security>>(nameof(Universe)).SetRequired()
			.SetDisplay("Universe", "Explicit execution securities with valid native volume metadata", "Parameters");
		_capital = Param(nameof(Capital), 100000m).SetGreaterThanZero()
			.SetDisplay("Capital", "Fixed capital budget in USD", "Parameters");
		_leverage = Param(nameof(Leverage), 1m).SetGreaterThanZero()
			.SetDisplay("Leverage", "Gross target exposure divided by the capital budget", "Parameters");
		_minTradeUsd = Param(nameof(MinTradeUsd), 200m).SetRange(0m, decimal.MaxValue)
			.SetDisplay("MinTradeUsd", "Minimum USD adjustment; complete liquidation bypasses this filter", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1440).TimeFrame())
			.SetDisplay("CandleType", "Finished execution candle type", "Parameters");
		_quintile = Param(nameof(Quintile), 5).SetRange(2, int.MaxValue)
			.SetDisplay("Quintile", "Number of lexical-density buckets", "Parameters");
		_maxReportAgeDays = Param(nameof(MaxReportAgeDays), 550).SetRange(1, int.MaxValue)
			.SetDisplay("MaxReportAgeDays", "Maximum age of the latest filing period end in calendar days", "Parameters");
		_filingsFile = Param<string>(nameof(FilingsFile)).SetRequired()
			.SetDisplay("FilingsFile", "Required filing manifest containing publication dates and document paths", "Parameters");
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
		_targets.Clear();
		_pending.Clear();
		_lastRebalance = 0;
		_lastBar = null;
		_filings.Clear();
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
		if (_securities.Any(s => s.Board is null)) throw new InvalidOperationException("A review calendar requires exchange boards.");
		LoadFilings();
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
				Side = delta > 0m ? Sides.Buy : Sides.Sell, Volume = quantity, Comment = "0376Rebalance",
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

	private void LoadFilings()
	{
		var keys = new HashSet<(Security, DateTime, DateTime)>();
		foreach (var row in ReadRows(FilingsFile, "SecurityId,PeriodEnd,PublishedAt,DocumentPath", 4))
		{
			var security = _securities.SingleOrDefault(s => string.Equals(s.Id, row[0].Trim(), StringComparison.OrdinalIgnoreCase));
			if (security is null) continue;
			var period = DateTime.ParseExact(row[1].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
			var published = ParseUtc(row[2]);
			if (published.Date < period || !keys.Add((security, period, published)))
				throw new InvalidOperationException("Filing revisions need causal, unique publication dates.");
			var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(FilingsFile)), row[3].Trim()));
			var words = Words(File.ReadAllText(path)).ToArray();
			if (words.Length == 0) throw new InvalidOperationException("A filing must contain word tokens.");
			_filings.Add(new(security, period, published, (decimal)words.Distinct(StringComparer.Ordinal).Count() / words.Length));
		}
	}

	private static IEnumerable<string> Words(string text)
	{
		var buffer = new List<string>();
		var elements = StringInfo.GetTextElementEnumerator(text);
		while (elements.MoveNext())
		{
			var element = elements.GetTextElement();
			var category = CharUnicodeInfo.GetUnicodeCategory(element, 0);
			if (category is UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or UnicodeCategory.TitlecaseLetter
				or UnicodeCategory.ModifierLetter or UnicodeCategory.OtherLetter or UnicodeCategory.NonSpacingMark
				or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark)
				buffer.Add(element);
			else if (buffer.Count > 0)
			{
				yield return CultureInfo.InvariantCulture.TextInfo.ToLower(string.Concat(buffer));
				buffer.Clear();
			}
		}
		if (buffer.Count > 0) yield return CultureInfo.InvariantCulture.TextInfo.ToLower(string.Concat(buffer));
	}

	private bool SelectTargets(DateTime time, DateTime asOf)
	{
		if (time.Month is not (2 or 5 or 8 or 11)) return false;
		var calendar = _securities[0].Board.WorkingTime;
		if (!calendar.IsWorkingDate(time.Date, true)) return false;
		var first = new DateTime(time.Year, time.Month, 1);
		if (Enumerable.Range(0, time.Day).Count(i => calendar.IsWorkingDate(first.AddDays(i), true)) > 3) return false;
		var ranked = _securities.Select(s => _filings.Where(f => f.Security == s && f.Published <= asOf)
			.OrderByDescending(f => f.Period).ThenByDescending(f => f.Published).FirstOrDefault())
			.Where(f => f is not null && (time.Date - f.Period).TotalDays <= MaxReportAgeDays)
			.OrderByDescending(f => f.Density).ThenBy(f => f.Security.Id, StringComparer.Ordinal).Select(f => f.Security).ToArray();
		if (ranked.Length < Quintile) return false;
		var count = ranked.Length / Quintile;
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
