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

/// <summary>Momentum Asset Growth with point-in-time inputs and execution-based portfolio accounting.</summary>
public class MomentumAssetGrowthStrategy : Strategy
{
	private readonly StrategyParam<IEnumerable<Security>> _universe;
	private readonly StrategyParam<decimal> _capital;
	private readonly StrategyParam<decimal> _leverage;
	private readonly StrategyParam<decimal> _minTradeUsd;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<string> _priceHistoryFile;
	private readonly StrategyParam<int> _momLook;
	private readonly StrategyParam<int> _skipMonths;
	private readonly StrategyParam<int> _assetDecile;
	private readonly StrategyParam<int> _quintile;
	private readonly StrategyParam<int> _maxReportAgeDays;
	private readonly StrategyParam<string> _fundamentalsFile;
	private Security[] _securities = [];
	private readonly Dictionary<Security, (DateTime time, decimal price, DateTime closed)> _latest = [];
	private readonly Dictionary<Security, SortedDictionary<DateTime, decimal>> _history = [];
	private readonly Dictionary<Security, decimal> _targets = [];
	private readonly List<Order> _pending = [];
	private int _lastRebalance;
	private DateTime? _lastBar;

	private sealed record Report(string Id, DateTime Period, DateTime Published, decimal Assets, decimal Income, decimal CashFlow);
	private readonly List<Report> _reports = [];

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

	/// <summary>Completed daily returns ending before the skipped calendar months.</summary>
	public int MomLook { get => _momLook.Value; set => _momLook.Value = value; }

	/// <summary>Recent whole calendar months excluded from momentum.</summary>
	public int SkipMonths { get => _skipMonths.Value; set => _skipMonths.Value = value; }

	/// <summary>Number of asset-growth buckets; only the highest is eligible.</summary>
	public int AssetDecile { get => _assetDecile.Value; set => _assetDecile.Value = value; }

	/// <summary>Number of momentum buckets within the growth subset.</summary>
	public int Quintile { get => _quintile.Value; set => _quintile.Value = value; }

	/// <summary>Maximum age of the latest fiscal year end in calendar days.</summary>
	public int MaxReportAgeDays { get => _maxReportAgeDays.Value; set => _maxReportAgeDays.Value = value; }

	/// <summary>Required point-in-time annual financial reports.</summary>
	public string FundamentalsFile { get => _fundamentalsFile.Value; set => _fundamentalsFile.Value = value; }

	/// <summary>Initialize the strategy parameters.</summary>
	public MomentumAssetGrowthStrategy()
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
		_priceHistoryFile = Param<string>(nameof(PriceHistoryFile))
			.SetDisplay("PriceHistoryFile", "Optional daily close CSV; future dates cannot prefill a decision", "Parameters");
		_momLook = Param(nameof(MomLook), 252).SetRange(1, int.MaxValue)
			.SetDisplay("MomLook", "Completed daily returns ending before the skipped calendar months", "Parameters");
		_skipMonths = Param(nameof(SkipMonths), 1).SetRange(0, 24)
			.SetDisplay("SkipMonths", "Recent whole calendar months excluded from momentum", "Parameters");
		_assetDecile = Param(nameof(AssetDecile), 10).SetRange(1, int.MaxValue)
			.SetDisplay("AssetDecile", "Number of asset-growth buckets; only the highest is eligible", "Parameters");
		_quintile = Param(nameof(Quintile), 5).SetRange(2, int.MaxValue)
			.SetDisplay("Quintile", "Number of momentum buckets within the growth subset", "Parameters");
		_maxReportAgeDays = Param(nameof(MaxReportAgeDays), 550).SetRange(1, int.MaxValue)
			.SetDisplay("MaxReportAgeDays", "Maximum age of the latest fiscal year end in calendar days", "Parameters");
		_fundamentalsFile = Param<string>(nameof(FundamentalsFile)).SetRequired()
			.SetDisplay("FundamentalsFile", "Required point-in-time annual financial reports", "Parameters");
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
		_reports.Clear();
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
		LoadReports();

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
				Side = delta > 0m ? Sides.Buy : Sides.Sell, Volume = quantity, Comment = "0378Rebalance",
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

	private void LoadReports()
	{
		var keys = new HashSet<(string, DateTime, DateTime)>();
		foreach (var row in ReadRows(FundamentalsFile, "SecurityId,PeriodEnd,PublishedAt,TotalAssets,NetIncome,OperatingCashFlow,BookEquity,SharesOutstanding", 8))
		{
			var id = row[0].Trim();
			if (!_securities.Any(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase))) continue;
			var period = DateTime.ParseExact(row[1].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
			var published = ParseUtc(row[2]);
			var values = row.Skip(3).Select(x => decimal.Parse(x, CultureInfo.InvariantCulture)).ToArray();
			if (values[0] <= 0m || values[4] <= 0m || published.Date < period || !keys.Add((id.ToUpperInvariant(), period, published)))
				throw new InvalidOperationException("Reports need positive assets/shares, causal publication dates and unique revisions.");
			_reports.Add(new(id, period, published, values[0], values[1], values[2]));
		}
	}

	private Report FindReport(Security security, DateTime asOf, DateTime? period = null)
		=> _reports.Where(r => string.Equals(r.Id, security.Id, StringComparison.OrdinalIgnoreCase)
			&& r.Published <= asOf && r.Period <= asOf.Date && (period is null || r.Period == period))
			.OrderByDescending(r => r.Period).ThenByDescending(r => r.Published).FirstOrDefault();

	private bool SelectTargets(DateTime time, DateTime asOf)
	{
		if (time.Month == 1) return false;
		var growth = new List<(Security security, decimal score)>();
		foreach (var security in _securities)
		{
			var latest = FindReport(security, asOf);
			if (latest is null || (time.Date - latest.Period).TotalDays > MaxReportAgeDays) continue;
			var previous = FindReport(security, asOf, latest.Period.AddYears(-1));
			if (previous is null) continue;
			growth.Add((security, latest.Assets / previous.Assets - 1m));
		}
		if (growth.Count < AssetDecile) return false;
		var eligible = growth.OrderByDescending(x => x.score).ThenBy(x => x.security.Id, StringComparer.Ordinal)
			.Take(growth.Count / AssetDecile).Select(x => x.security);
		var cutoff = new DateTime(time.Year, time.Month, 1).AddMonths(-SkipMonths);
		var momentum = new List<(Security security, decimal score)>();
		foreach (var security in eligible)
		{
			var prices = Daily(security, time, MomLook + 1, cutoff);
			if (prices.Length == MomLook + 1) momentum.Add((security, prices[^1] / prices[0] - 1m));
		}
		if (momentum.Count < Quintile) return false;
		var ranked = momentum.OrderByDescending(x => x.score).ThenBy(x => x.security.Id, StringComparer.Ordinal).Select(x => x.security).ToArray();
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
