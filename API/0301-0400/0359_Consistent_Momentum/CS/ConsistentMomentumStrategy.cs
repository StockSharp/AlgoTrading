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

/// <summary>Consistent Momentum with explicit inputs and execution-based portfolio accounting.</summary>
public class ConsistentMomentumStrategy : Strategy
{
	private readonly StrategyParam<IEnumerable<Security>> _universe;
	private readonly StrategyParam<decimal> _capital;
	private readonly StrategyParam<decimal> _leverage;
	private readonly StrategyParam<decimal> _minTradeUsd;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _deciles;
	private readonly StrategyParam<string> _priceHistoryFile;
	private readonly StrategyParam<int> _lookbackDays;
	private readonly StrategyParam<int> _shortLookbackDays;
	private readonly StrategyParam<int> _holdingMonths;
	private Security[] _securities = [];
	private readonly Dictionary<Security, (DateTime time, decimal price)> _latest = [];
	private readonly Dictionary<Security, SortedDictionary<DateTime, decimal>> _history = [];
	private readonly Dictionary<Security, decimal> _targets = [];
	private readonly List<Order> _pending = [];
	private int _lastRebalance;
	private DateTime? _lastBar;

	private sealed record Tranche(DateTime Expires, Dictionary<Security, decimal> Quantities);
	private readonly List<Tranche> _tranches = [];

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

	/// <summary>Long daily momentum lookback.</summary>
	public int LookbackDays { get => _lookbackDays.Value; set => _lookbackDays.Value = value; }

	/// <summary>Short daily momentum lookback.</summary>
	public int ShortLookbackDays { get => _shortLookbackDays.Value; set => _shortLookbackDays.Value = value; }

	/// <summary>Lifetime and gross capital denominator of each monthly tranche.</summary>
	public int HoldingMonths { get => _holdingMonths.Value; set => _holdingMonths.Value = value; }

	/// <summary>Initialize the published strategy parameters.</summary>
	public ConsistentMomentumStrategy()
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
		_deciles = Param(nameof(Deciles), 10).SetRange(2, 10000)
			.SetDisplay("Deciles", "Number of cross-sectional buckets, requiring at least this many eligible securities", "Parameters");
		_priceHistoryFile = Param<string>(nameof(PriceHistoryFile))
			.SetDisplay("PriceHistoryFile", "Optional daily close CSV for warmup; current and future dates cannot prefill a decision", "Parameters");
		_lookbackDays = Param(nameof(LookbackDays), 147).SetGreaterThanZero()
			.SetDisplay("LookbackDays", "Long daily momentum lookback", "Parameters");
		_shortLookbackDays = Param(nameof(ShortLookbackDays), 21).SetGreaterThanZero()
			.SetDisplay("ShortLookbackDays", "Short daily momentum lookback", "Parameters");
		_holdingMonths = Param(nameof(HoldingMonths), 6).SetGreaterThanZero()
			.SetDisplay("HoldingMonths", "Lifetime and gross capital denominator of each monthly tranche", "Parameters");
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
		_tranches.Clear();
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

		if (ShortLookbackDays >= LookbackDays) throw new InvalidOperationException("ShortLookbackDays must be less than LookbackDays.");
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
		UpdateTrancheTargets(new DateTime(candle.OpenTime.Year, candle.OpenTime.Month, 1));
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
				Side = delta > 0m ? Sides.Buy : Sides.Sell, Volume = quantity, Comment = "0359Rebalance",
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
		if (_securities.Length < Deciles || _securities.Any(s => Daily(s, time).Length < LookbackDays + 1)) return false;
		decimal Momentum(Security s, int length)
		{
			var values = Daily(s, time);
			return values[^1] / values[values.Length - length - 1] - 1m;
		}
		var count = _securities.Length / Deciles;
		var longRank = _securities.OrderBy(s => Momentum(s, LookbackDays)).ThenBy(s => s.Id, StringComparer.Ordinal).ToArray();
		var shortRank = _securities.OrderBy(s => Momentum(s, ShortLookbackDays)).ThenBy(s => s.Id, StringComparer.Ordinal).ToArray();
		var longs = longRank.TakeLast(count).Intersect(shortRank.TakeLast(count)).ToArray();
		var shorts = longRank.Take(count).Intersect(shortRank.Take(count)).ToArray();
		var quantities = _securities.ToDictionary(s => s, _ => 0m);
		foreach (var s in longs) quantities[s] = Units(s, Capital * Leverage / (2m * HoldingMonths * longs.Length));
		foreach (var s in shorts) quantities[s] = -Units(s, Capital * Leverage / (2m * HoldingMonths * shorts.Length));
		var month = new DateTime(time.Year, time.Month, 1);
		_tranches.Add(new(month.AddMonths(HoldingMonths), quantities));
		UpdateTrancheTargets(month);
		return true;
	}

	private void UpdateTrancheTargets(DateTime month)
	{
		_tranches.RemoveAll(t => t.Expires <= month);
		foreach (var s in _securities) _targets[s] = _tranches.Sum(t => t.Quantities[s]);
	}

}
