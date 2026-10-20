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

/// <summary>Commodity Momentum with explicit inputs and execution-based portfolio accounting.</summary>
public class CommodityMomentumStrategy : Strategy
{
	private readonly StrategyParam<IEnumerable<Security>> _universe;
	private readonly StrategyParam<decimal> _capital;
	private readonly StrategyParam<decimal> _leverage;
	private readonly StrategyParam<decimal> _minTradeUsd;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<string> _priceHistoryFile;
	private readonly StrategyParam<int> _topN;
	private readonly StrategyParam<int> _lookbackMonths;
	private readonly StrategyParam<int> _skipMonths;
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

	/// <summary>Optional daily close CSV for warmup; current and future dates cannot prefill a decision.</summary>
	public string PriceHistoryFile { get => _priceHistoryFile.Value; set => _priceHistoryFile.Value = value; }

	/// <summary>Maximum number of long commodity positions.</summary>
	public int TopN { get => _topN.Value; set => _topN.Value = value; }

	/// <summary>Calendar month lookback before the decision month.</summary>
	public int LookbackMonths { get => _lookbackMonths.Value; set => _lookbackMonths.Value = value; }

	/// <summary>Most recent completed calendar months excluded from momentum.</summary>
	public int SkipMonths { get => _skipMonths.Value; set => _skipMonths.Value = value; }

	/// <summary>Initialize the published strategy parameters.</summary>
	public CommodityMomentumStrategy()
	{
		_universe = Param<IEnumerable<Security>>(nameof(Universe)).SetRequired()
			.SetDisplay("Universe", "Execution securities with USD prices and valid volume metadata", "Parameters");
		_capital = Param(nameof(Capital), 100000m).SetGreaterThanZero()
			.SetDisplay("Capital", "Fixed gross capital budget in USD", "Parameters");
		_leverage = Param(nameof(Leverage), 1m).SetGreaterThanZero()
			.SetDisplay("Leverage", "Gross target exposure divided by the capital budget", "Parameters");
		_minTradeUsd = Param(nameof(MinTradeUsd), 200m).SetRange(0m, decimal.MaxValue)
			.SetDisplay("MinTradeUsd", "Minimum notional adjustment; complete liquidation bypasses this filter", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("CandleType", "Closed candle type; daily statistics count distinct UTC dates", "Parameters");
		_priceHistoryFile = Param<string>(nameof(PriceHistoryFile))
			.SetDisplay("PriceHistoryFile", "Optional daily close CSV for warmup; current and future dates cannot prefill a decision", "Parameters");
		_topN = Param(nameof(TopN), 5).SetGreaterThanZero()
			.SetDisplay("TopN", "Maximum number of long commodity positions", "Parameters");
		_lookbackMonths = Param(nameof(LookbackMonths), 12).SetGreaterThanZero()
			.SetDisplay("LookbackMonths", "Calendar month lookback before the decision month", "Parameters");
		_skipMonths = Param(nameof(SkipMonths), 1).SetRange(0, 120)
			.SetDisplay("SkipMonths", "Most recent completed calendar months excluded from momentum", "Parameters");
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

		if (SkipMonths >= LookbackMonths) throw new InvalidOperationException("SkipMonths must be less than LookbackMonths.");
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
		=> _history[security].Where(x => x.Key <= time.Date).Select(x => x.Value).ToArray();

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
				Side = delta > 0m ? Sides.Buy : Sides.Sell, Volume = quantity, Comment = "0358Rebalance",
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
		var month = new DateTime(time.Year, time.Month, 1);
		var end = month.AddMonths(-SkipMonths - 1);
		var start = month.AddMonths(-LookbackMonths - 1);
		var scores = new List<(Security security, decimal score)>();
		foreach (var s in _securities)
		{
			var monthly = _history[s].Where(p => p.Key < month)
				.GroupBy(p => new DateTime(p.Key.Year, p.Key.Month, 1))
				.ToDictionary(g => g.Key, g => g.Last().Value);
			if (Enumerable.Range(0, LookbackMonths - SkipMonths + 1).Any(i => !monthly.ContainsKey(start.AddMonths(i)))) continue;
			scores.Add((s, monthly[end] / monthly[start] - 1m));
		}
		if (scores.Count == 0) return false;
		var selected = scores.OrderByDescending(x => x.score).ThenBy(x => x.security.Id, StringComparer.Ordinal)
			.Take(TopN).Select(x => x.security).ToArray();
		foreach (var s in _securities) _targets[s] = selected.Contains(s) ? Units(s, Capital * Leverage / selected.Length) : 0m;
		return true;
	}

}
