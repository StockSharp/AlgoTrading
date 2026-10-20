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

/// <summary>Momentum Rev Vol with point-in-time inputs and execution-based portfolio accounting.</summary>
public class MomentumRevVolStrategy : Strategy
{
	private readonly StrategyParam<IEnumerable<Security>> _universe;
	private readonly StrategyParam<decimal> _capital;
	private readonly StrategyParam<decimal> _leverage;
	private readonly StrategyParam<decimal> _minTradeUsd;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<string> _priceHistoryFile;
	private readonly StrategyParam<int> _lookback12;
	private readonly StrategyParam<int> _lookback1;
	private readonly StrategyParam<int> _volWindow;
	private readonly StrategyParam<decimal> _wM;
	private readonly StrategyParam<decimal> _wR;
	private readonly StrategyParam<decimal> _wV;
	private readonly StrategyParam<int> _decile;
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

	/// <summary>Daily return intervals for long-term momentum.</summary>
	public int Lookback12 { get => _lookback12.Value; set => _lookback12.Value = value; }

	/// <summary>Daily return intervals for reversal.</summary>
	public int Lookback1 { get => _lookback1.Value; set => _lookback1.Value = value; }

	/// <summary>Daily returns in the sample volatility window.</summary>
	public int VolWindow { get => _volWindow.Value; set => _volWindow.Value = value; }

	/// <summary>Weight of long-term momentum.</summary>
	public decimal WM { get => _wM.Value; set => _wM.Value = value; }

	/// <summary>Weight of negative short-term returns.</summary>
	public decimal WR { get => _wR.Value; set => _wR.Value = value; }

	/// <summary>Weight of negative sample return volatility.</summary>
	public decimal WV { get => _wV.Value; set => _wV.Value = value; }

	/// <summary>Number of equal rank buckets.</summary>
	public int Decile { get => _decile.Value; set => _decile.Value = value; }

	/// <summary>Initialize the strategy parameters.</summary>
	public MomentumRevVolStrategy()
	{
		_universe = Param<IEnumerable<Security>>(nameof(Universe)).SetRequired()
			.SetDisplay("Universe", "Explicit execution universe with native volume metadata.", "Parameters");
		_capital = Param(nameof(Capital), 100000m).SetRange(0.000001m, decimal.MaxValue)
			.SetDisplay("Capital", "Fixed USD capital budget.", "Parameters");
		_leverage = Param(nameof(Leverage), 1m).SetRange(0.000001m, decimal.MaxValue)
			.SetDisplay("Leverage", "Gross exposure divided by capital.", "Parameters");
		_minTradeUsd = Param(nameof(MinTradeUsd), 200m).SetRange(0m, decimal.MaxValue)
			.SetDisplay("MinTradeUsd", "Minimum USD adjustment; complete liquidation is exempt.", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1440).TimeFrame())
			.SetDisplay("CandleType", "Finished execution candle type.", "Parameters");
		_priceHistoryFile = Param<string>(nameof(PriceHistoryFile))
			.SetDisplay("PriceHistoryFile", "Optional CSV of causal daily close prices.", "Parameters");
		_lookback12 = Param(nameof(Lookback12), 252).SetRange(1, int.MaxValue)
			.SetDisplay("Lookback12", "Daily return intervals for long-term momentum.", "Parameters");
		_lookback1 = Param(nameof(Lookback1), 21).SetRange(1, int.MaxValue)
			.SetDisplay("Lookback1", "Daily return intervals for reversal.", "Parameters");
		_volWindow = Param(nameof(VolWindow), 60).SetRange(2, int.MaxValue)
			.SetDisplay("VolWindow", "Daily returns in the sample volatility window.", "Parameters");
		_wM = Param(nameof(WM), 1m).SetRange(0m, decimal.MaxValue)
			.SetDisplay("WM", "Weight of long-term momentum.", "Parameters");
		_wR = Param(nameof(WR), 1m).SetRange(0m, decimal.MaxValue)
			.SetDisplay("WR", "Weight of negative short-term returns.", "Parameters");
		_wV = Param(nameof(WV), 1m).SetRange(0m, decimal.MaxValue)
			.SetDisplay("WV", "Weight of negative sample return volatility.", "Parameters");
		_decile = Param(nameof(Decile), 10).SetRange(2, int.MaxValue)
			.SetDisplay("Decile", "Number of equal rank buckets.", "Parameters");
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
				Side = delta > 0m ? Sides.Buy : Sides.Sell, Volume = quantity, Comment = "0380Rebalance",
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
		var lookback = Math.Max(Lookback12, Math.Max(Lookback1, VolWindow));
		foreach (var security in _securities)
		{
			var prices = Daily(security, time, lookback + 1, DateTime.MaxValue);
			if (prices.Length != lookback + 1) continue;
			var returns = prices.TakeLast(VolWindow + 1).Zip(prices.TakeLast(VolWindow), (a, b) => b / a - 1m).ToArray();
			var mean = returns.Average();
			var variance = returns.Sum(r => (r - mean) * (r - mean)) / (returns.Length - 1);
			var volatility = (decimal)Math.Sqrt((double)variance);
			var score = WM * (prices[^1] / prices[^(Lookback12 + 1)] - 1m)
				- WR * (prices[^1] / prices[^(Lookback1 + 1)] - 1m) - WV * volatility;
			scores.Add((security, score));
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