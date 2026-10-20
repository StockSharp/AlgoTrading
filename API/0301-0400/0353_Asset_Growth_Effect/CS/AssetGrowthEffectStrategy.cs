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

/// <summary>Asset Growth Effect with explicit inputs and execution-based portfolio accounting.</summary>
public class AssetGrowthEffectStrategy : Strategy
{
	private readonly StrategyParam<IEnumerable<Security>> _universe;
	private readonly StrategyParam<decimal> _capital;
	private readonly StrategyParam<decimal> _leverage;
	private readonly StrategyParam<decimal> _minTradeUsd;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _quantiles;
	private readonly StrategyParam<string> _fundamentalsFile;
	private readonly StrategyParam<int> _rebalanceMonth;
	private readonly StrategyParam<int> _maxReportAgeDays;
	private Security[] _securities = [];
	private readonly Dictionary<Security, (DateTime time, decimal price)> _latest = [];
	private readonly Dictionary<Security, decimal> _targets = [];
	private readonly List<Order> _pending = [];
	private int _lastRebalance;
	private DateTime? _lastBar;

	private sealed record Report(string Id, DateTime Period, DateTime Published, decimal Assets, decimal Income, decimal CashFlow, decimal Book, decimal Shares);
	private readonly List<Report> _reports = [];

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
	public int Quantiles { get => _quantiles.Value; set => _quantiles.Value = value; }

	/// <summary>Point-in-time annual financial reports in CSV format.</summary>
	public string FundamentalsFile { get => _fundamentalsFile.Value; set => _fundamentalsFile.Value = value; }

	/// <summary>UTC calendar month of the annual portfolio rebalance.</summary>
	public int RebalanceMonth { get => _rebalanceMonth.Value; set => _rebalanceMonth.Value = value; }

	/// <summary>Maximum age of the fiscal period at a rebalance.</summary>
	public int MaxReportAgeDays { get => _maxReportAgeDays.Value; set => _maxReportAgeDays.Value = value; }

	/// <summary>Initialize the published strategy parameters.</summary>
	public AssetGrowthEffectStrategy()
	{
		_universe = Param<IEnumerable<Security>>(nameof(Universe)).SetRequired()
			.SetDisplay("Universe", "Execution securities with USD prices and valid volume metadata", "Parameters");
		_capital = Param(nameof(Capital), 100000m).SetGreaterThanZero()
			.SetDisplay("Capital", "Fixed gross capital budget in USD", "Parameters");
		_leverage = Param(nameof(Leverage), 1m).SetGreaterThanZero()
			.SetDisplay("Leverage", "Gross target exposure divided by the capital budget", "Parameters");
		_minTradeUsd = Param(nameof(MinTradeUsd), 50m).SetRange(0m, decimal.MaxValue)
			.SetDisplay("MinTradeUsd", "Minimum notional adjustment; complete liquidation bypasses this filter", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("CandleType", "Closed candle type; daily statistics count distinct UTC dates", "Parameters");
		_quantiles = Param(nameof(Quantiles), 10).SetRange(2, 10000)
			.SetDisplay("Quantiles", "Number of cross-sectional buckets, requiring at least this many eligible securities", "Parameters");
		_fundamentalsFile = Param<string>(nameof(FundamentalsFile)).SetRequired()
			.SetDisplay("FundamentalsFile", "Point-in-time annual financial reports in CSV format", "Parameters");
		_rebalanceMonth = Param(nameof(RebalanceMonth), 7).SetRange(1, 12)
			.SetDisplay("RebalanceMonth", "UTC calendar month of the annual portfolio rebalance", "Parameters");
		_maxReportAgeDays = Param(nameof(MaxReportAgeDays), 550).SetGreaterThanZero()
			.SetDisplay("MaxReportAgeDays", "Maximum age of the fiscal period at a rebalance", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		foreach (var security in (Universe ?? []).DistinctBy(s => s.Id, StringComparer.OrdinalIgnoreCase))
			yield return (security, CandleType);
	}

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
		_reports.Clear();
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
		}

		if (string.IsNullOrWhiteSpace(FundamentalsFile)) throw new InvalidOperationException("FundamentalsFile is required.");
		LoadReports();
		base.OnStarted2(time);
		foreach (var security in GetWorkingSecurities().Select(x => x.sec))
			SubscribeCandles(CandleType, security: security).Bind(candle => ProcessCandle(security, candle)).Start();
	}

	private void ProcessCandle(Security security, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || candle.ClosePrice <= 0m) return;
		_latest[security] = (candle.OpenTime, candle.ClosePrice);
		if (_latest.Count != _securities.Length || _latest.Values.Any(v => v.time != candle.OpenTime)
			|| _lastBar == candle.OpenTime || !IsFormedAndOnlineAndAllowTrading()) return;
		_lastBar = candle.OpenTime;
		var month = candle.OpenTime.Year * 100 + candle.OpenTime.Month;

		if (_lastRebalance / 100 != candle.OpenTime.Year && SelectTargets(candle.OpenTime))
			_lastRebalance = month;

		ReconcileTargets();
	}

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
				Side = delta > 0m ? Sides.Buy : Sides.Sell, Volume = quantity, Comment = "0353Rebalance",
			};
			_pending.Add(order);
			RegisterOrder(order);
		}
	}

	private void LoadReports()
	{
		var lines = File.ReadLines(FundamentalsFile).GetEnumerator();
		using (lines)
		{
			if (!lines.MoveNext() || lines.Current.Trim() != "SecurityId,PeriodEnd,PublishedAt,TotalAssets,NetIncome,OperatingCashFlow,BookEquity,SharesOutstanding")
				throw new InvalidOperationException("Invalid annual financial report CSV header.");
			var keys = new HashSet<(string, DateTime, DateTime)>();
			while (lines.MoveNext())
			{
				if (string.IsNullOrWhiteSpace(lines.Current)) continue;
				var row = lines.Current.Split(',');
				if (row.Length != 8) throw new InvalidOperationException("A financial report must have eight fields.");
				var id = row[0].Trim();
				if (!_securities.Any(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase))) continue;
				var period = DateTime.ParseExact(row[1].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
				var published = DateTime.ParseExact(row[2].Trim(), "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
				var numbers = row.Skip(3).Select(x => decimal.Parse(x, CultureInfo.InvariantCulture)).ToArray();
				if (numbers[0] <= 0m || numbers[4] <= 0m || published.Date < period || !keys.Add((id.ToUpperInvariant(), period, published)))
					throw new InvalidOperationException("Reports need positive assets/shares, causal publication dates and unique revisions.");
				_reports.Add(new(id, period, published, numbers[0], numbers[1], numbers[2], numbers[3], numbers[4]));
			}
		}
	}

	private Report FindReport(Security security, DateTime time, DateTime? period = null)
		=> _reports.Where(r => string.Equals(r.Id, security.Id, StringComparison.OrdinalIgnoreCase)
			&& r.Published <= time && r.Period <= time.Date && (period is null || r.Period == period))
			.OrderByDescending(r => r.Period).ThenByDescending(r => r.Published).FirstOrDefault();

	private bool SelectTargets(DateTime time)
	{
		if (time.Month != RebalanceMonth) return false;
		var scores = new List<(Security security, decimal score)>();
		foreach (var security in _securities)
		{
			var report = FindReport(security, time);
			if (report is null || (time.Date - report.Period).TotalDays > MaxReportAgeDays) continue;
			var prior = FindReport(security, time, report.Period.AddYears(-1));
			if (prior is null) continue;
			var score = report.Assets / prior.Assets - 1m;
			scores.Add((security, score));
		}
		if (scores.Count < Quantiles) return false;
		var ranked = scores.OrderBy(x => x.score).ThenBy(x => x.security.Id, StringComparer.Ordinal).Select(x => x.security).ToArray();
		var count = ranked.Length / Quantiles;
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
