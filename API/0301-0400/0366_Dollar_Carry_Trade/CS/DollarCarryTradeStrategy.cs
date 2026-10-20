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

/// <summary>Dollar Carry Trade with point-in-time inputs and execution-based portfolio accounting.</summary>
public class DollarCarryTradeStrategy : Strategy
{
	private readonly StrategyParam<IEnumerable<Security>> _universe;
	private readonly StrategyParam<decimal> _capital;
	private readonly StrategyParam<decimal> _leverage;
	private readonly StrategyParam<decimal> _minTradeUsd;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _k;
	private readonly StrategyParam<string> _ratesFile;
	private readonly StrategyParam<int> _maxRateAgeDays;
	private Security[] _securities = [];
	private readonly Dictionary<Security, (DateTime time, decimal price, DateTime closed)> _latest = [];
	private readonly Dictionary<Security, decimal> _targets = [];
	private readonly List<Order> _pending = [];
	private int _lastRebalance;
	private DateTime? _lastBar;

	private sealed record Rate(Security Security, DateTime Published, decimal BaseRate, decimal QuoteRate, decimal UsdPerUnit, bool IsUsdBase);
	private readonly List<Rate> _rates = [];
	private readonly Dictionary<Security, decimal> _usdPrices = [];

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

	/// <summary>Currencies on each side of the USD carry portfolio.</summary>
	public int K { get => _k.Value; set => _k.Value = value; }

	/// <summary>Required point-in-time interest rates, USD unit values and pair orientation.</summary>
	public string RatesFile { get => _ratesFile.Value; set => _ratesFile.Value = value; }

	/// <summary>Maximum age of the published rate snapshot in calendar days.</summary>
	public int MaxRateAgeDays { get => _maxRateAgeDays.Value; set => _maxRateAgeDays.Value = value; }

	/// <summary>Initialize the strategy parameters.</summary>
	public DollarCarryTradeStrategy()
	{
		_universe = Param<IEnumerable<Security>>(nameof(Universe)).SetRequired()
			.SetDisplay("Universe", "Explicit execution securities with valid native volume metadata", "Parameters");
		_capital = Param(nameof(Capital), 100000m).SetGreaterThanZero()
			.SetDisplay("Capital", "Fixed capital budget in USD", "Parameters");
		_leverage = Param(nameof(Leverage), 1m).SetGreaterThanZero()
			.SetDisplay("Leverage", "Gross target exposure divided by the capital budget", "Parameters");
		_minTradeUsd = Param(nameof(MinTradeUsd), 100m).SetRange(0m, decimal.MaxValue)
			.SetDisplay("MinTradeUsd", "Minimum USD adjustment; complete liquidation bypasses this filter", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("CandleType", "Finished execution candle type", "Parameters");
		_k = Param(nameof(K), 3).SetRange(1, int.MaxValue)
			.SetDisplay("K", "Currencies on each side of the USD carry portfolio", "Parameters");
		_ratesFile = Param<string>(nameof(RatesFile)).SetRequired()
			.SetDisplay("RatesFile", "Required point-in-time interest rates, USD unit values and pair orientation", "Parameters");
		_maxRateAgeDays = Param(nameof(MaxRateAgeDays), 45).SetRange(1, int.MaxValue)
			.SetDisplay("MaxRateAgeDays", "Maximum age of the published rate snapshot in calendar days", "Parameters");
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
		_rates.Clear();
		_usdPrices.Clear();
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
		LoadRates();
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

	private decimal UnitPrice(Security security) => _usdPrices[security];

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
				Side = delta > 0m ? Sides.Buy : Sides.Sell, Volume = quantity, Comment = "0366Rebalance",
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

	private void LoadRates()
	{
		var keys = new HashSet<(Security, DateTime)>();
		foreach (var row in ReadRows(RatesFile, "SecurityId,PublishedAt,BaseRate,QuoteRate,UsdPerUnit,IsUsdBase", 6))
		{
			var security = _securities.SingleOrDefault(s => string.Equals(s.Id, row[0].Trim(), StringComparison.OrdinalIgnoreCase));
			if (security is null) continue;
			var published = ParseUtc(row[1]);
			var baseRate = decimal.Parse(row[2], CultureInfo.InvariantCulture);
			var quoteRate = decimal.Parse(row[3], CultureInfo.InvariantCulture);
			var usd = decimal.Parse(row[4], CultureInfo.InvariantCulture);
			if (usd <= 0m || !keys.Add((security, published)))
				throw new InvalidOperationException("Rate snapshots need positive USD unit values and unique publication timestamps.");
			_rates.Add(new(security, published, baseRate, quoteRate, usd, bool.Parse(row[5].Trim())));
		}
		if (_rates.GroupBy(r => r.Security).Any(g => g.Select(r => r.IsUsdBase).Distinct().Count() != 1))
			throw new InvalidOperationException("A currency pair cannot change its USD base/quote orientation.");
	}

	private bool SelectTargets(DateTime time, DateTime asOf)
	{
		var ranked = _securities.Select(s => _rates.Where(r => r.Security == s && r.Published <= asOf)
			.OrderByDescending(r => r.Published).FirstOrDefault())
			.Where(r => r is not null && (asOf - r.Published).TotalDays <= MaxRateAgeDays)
			.OrderBy(r => r.IsUsdBase ? r.QuoteRate - r.BaseRate : r.BaseRate - r.QuoteRate)
			.ThenBy(r => r.Security.Id, StringComparer.Ordinal).ToArray();
		if (ranked.Length < 2 * K) return false;
		foreach (var rate in ranked) _usdPrices[rate.Security] = rate.UsdPerUnit;
		foreach (var security in _securities) _targets[security] = 0m;
		foreach (var rate in ranked.Take(K)) _targets[rate.Security] = (rate.IsUsdBase ? 1m : -1m) * Units(rate.Security, Budget / (2m * K));
		foreach (var rate in ranked.TakeLast(K)) _targets[rate.Security] = (rate.IsUsdBase ? -1m : 1m) * Units(rate.Security, Budget / (2m * K));
		return true;
	}
}
