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

/// <summary>Return Asymmetry Commodity with point-in-time inputs and execution-based portfolio accounting.</summary>
public class ReturnAsymmetryCommodityStrategy : Strategy
{
	private readonly StrategyParam<IEnumerable<Security>> _universe;
	private readonly StrategyParam<decimal> _capital;
	private readonly StrategyParam<decimal> _leverage;
	private readonly StrategyParam<decimal> _minTradeUsd;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<string> _priceHistoryFile;
	private readonly StrategyParam<int> _windowDays;
	private readonly StrategyParam<int> _topN;
	private Security[] _securities = [];
	private readonly Dictionary<Security, (DateTime time, decimal price)> _latest = [];
	private readonly Dictionary<Security, SortedDictionary<DateTime, decimal>> _history = [];
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

	/// <summary>Optional CSV of causal daily close prices.</summary>
	public string PriceHistoryFile { get => _priceHistoryFile.Value; set => _priceHistoryFile.Value = value; }

	/// <summary>Daily returns in the up/down balance window.</summary>
	public int WindowDays { get => _windowDays.Value; set => _windowDays.Value = value; }

	/// <summary>Distinct securities on each extreme side.</summary>
	public int TopN { get => _topN.Value; set => _topN.Value = value; }

	/// <summary>Initialize the strategy parameters.</summary>
	public ReturnAsymmetryCommodityStrategy()
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
		_priceHistoryFile = Param<string>(nameof(PriceHistoryFile))
			.SetDisplay("PriceHistoryFile", "Optional CSV of causal daily close prices.", "Parameters");
		_windowDays = Param(nameof(WindowDays), 120).SetRange(1, int.MaxValue)
			.SetDisplay("WindowDays", "Daily returns in the up/down balance window.", "Parameters");
		_topN = Param(nameof(TopN), 5).SetRange(1, int.MaxValue)
			.SetDisplay("TopN", "Distinct securities on each extreme side.", "Parameters");
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
		base.OnStarted2(time);
		foreach (var security in _securities)
			SubscribeCandles(CandleType, security: security).Bind(candle => ProcessCandle(security, candle)).Start();
	}

	private void ProcessCandle(Security security, ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || candle.ClosePrice <= 0m) return;
		_latest[security] = (candle.OpenTime, candle.ClosePrice);
		_history[security][candle.OpenTime.Date] = candle.ClosePrice;
		if (_latest.Count != _securities.Length || _latest.Values.Any(v => v.time != candle.OpenTime)
			|| _lastBar == candle.OpenTime || !IsFormedAndOnlineAndAllowTrading()) return;
		_lastBar = candle.OpenTime;
		var month = candle.OpenTime.Year * 100 + candle.OpenTime.Month;
		if (_lastRebalance != month && SelectTargets(candle.OpenTime))
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
				Side = delta > 0m ? Sides.Buy : Sides.Sell, Volume = quantity, Comment = "0392Rebalance",
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

	private bool SelectTargets(DateTime time)
	{
		var scores = new List<(Security security, decimal score)>();
		foreach (var security in _securities)
		{
			var prices = Daily(security, time, WindowDays + 1, DateTime.MaxValue);
			if (prices.Length != WindowDays + 1) continue;
			var returns = prices.Zip(prices.Skip(1), (a, b) => b / a - 1m).ToArray();
			var up = returns.Where(r => r > 0m).Sum();
			var down = -returns.Where(r => r < 0m).Sum();
			scores.Add((security, up + down == 0m ? 0m : (up - down) / (up + down)));
		}
		if (scores.Count < 2L * TopN) return false;
		var ranked = scores.OrderByDescending(x => x.score).ThenBy(x => x.security.Id, StringComparer.Ordinal).Select(x => x.security).ToArray();
		SetBasketTargets(ranked.Take(TopN), ranked.TakeLast(TopN), Budget);
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