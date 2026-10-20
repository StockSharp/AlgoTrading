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

/// <summary>Fed Model with point-in-time inputs and execution-based portfolio accounting.</summary>
public class FedModelStrategy : Strategy
{
	private readonly StrategyParam<IEnumerable<Security>> _universe;
	private readonly StrategyParam<decimal> _capital;
	private readonly StrategyParam<decimal> _minTradeUsd;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _regressionMonths;
	private readonly StrategyParam<string> _yieldHistoryFile;
	private Security[] _securities = [];
	private readonly Dictionary<Security, (DateTime time, decimal price, DateTime closed)> _latest = [];
	private readonly Dictionary<Security, decimal> _targets = [];
	private readonly List<Order> _pending = [];
	private int _lastRebalance;
	private DateTime? _lastBar;
	private readonly Dictionary<Security, decimal> _nextTargets = [];
	private bool _isRotating;
	private bool _isReconciling;

	private sealed record Yield(DateTime Month, DateTime Published, decimal Spread);
	private readonly List<Yield> _yields = [];
	private bool? _equityRegime;

	/// <summary>Explicit execution securities with valid native volume metadata.</summary>
	public IEnumerable<Security> Universe { get => _universe.Value; set => _universe.Value = value; }

	/// <summary>Fixed capital budget in USD.</summary>
	public decimal Capital { get => _capital.Value; set => _capital.Value = value; }

	/// <summary>Minimum USD adjustment; complete liquidation bypasses this filter.</summary>
	public decimal MinTradeUsd { get => _minTradeUsd.Value; set => _minTradeUsd.Value = value; }

	/// <summary>Finished execution candle type.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>Consecutive monthly yield spreads in the OLS forecast.</summary>
	public int RegressionMonths { get => _regressionMonths.Value; set => _regressionMonths.Value = value; }

	/// <summary>Required point-in-time monthly equity earnings and ten-year bond yields.</summary>
	public string YieldHistoryFile { get => _yieldHistoryFile.Value; set => _yieldHistoryFile.Value = value; }

	/// <summary>Initialize the strategy parameters.</summary>
	public FedModelStrategy()
	{
		_universe = Param<IEnumerable<Security>>(nameof(Universe)).SetRequired()
			.SetDisplay("Universe", "Explicit execution securities with valid native volume metadata", "Parameters");
		_capital = Param(nameof(Capital), 100000m).SetGreaterThanZero()
			.SetDisplay("Capital", "Fixed capital budget in USD", "Parameters");
		_minTradeUsd = Param(nameof(MinTradeUsd), 100m).SetRange(0m, decimal.MaxValue)
			.SetDisplay("MinTradeUsd", "Minimum USD adjustment; complete liquidation bypasses this filter", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1440).TimeFrame())
			.SetDisplay("CandleType", "Finished execution candle type", "Parameters");
		_regressionMonths = Param(nameof(RegressionMonths), 12).SetRange(2, 120)
			.SetDisplay("RegressionMonths", "Consecutive monthly yield spreads in the OLS forecast", "Parameters");
		_yieldHistoryFile = Param<string>(nameof(YieldHistoryFile)).SetRequired()
			.SetDisplay("YieldHistoryFile", "Required point-in-time monthly equity earnings and ten-year bond yields", "Parameters");
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
		_targets.Clear();
		_pending.Clear();
		_lastRebalance = 0;
		_lastBar = null;
		_nextTargets.Clear();
		_isRotating = false;
		_isReconciling = false;
		_yields.Clear();
		_equityRegime = null;
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
		if (_securities.Length > 2 || CandleType.Arg is not TimeSpan frame || frame != TimeSpan.FromDays(1) || _securities.Any(s => s.Board is null))
			throw new InvalidOperationException("The Fed model requires daily candles, an equity security and at most one cash security, with exchange calendars.");
		LoadYields();
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
		if (!_isRotating && _lastRebalance != month && SelectTargets(candle.OpenTime, _latest.Values.Max(v => v.closed)))
			_lastRebalance = month;
		ReconcileTargets();
	}

	private decimal Budget => Math.Max(0m, Math.Min(Capital, Portfolio.CurrentValue.GetValueOrDefault()));

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
					Side = delta > 0m ? Sides.Buy : Sides.Sell, Volume = quantity, Comment = "0372Rebalance",
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

	private void LoadYields()
	{
		var keys = new HashSet<(DateTime, DateTime)>();
		foreach (var row in ReadRows(YieldHistoryFile, "Month,PublishedAt,EarningsYield,BondYield", 4))
		{
			var month = DateTime.ParseExact(row[0].Trim(), "yyyy-MM", CultureInfo.InvariantCulture);
			var published = ParseUtc(row[1]);
			var earnings = decimal.Parse(row[2], CultureInfo.InvariantCulture);
			var bond = decimal.Parse(row[3], CultureInfo.InvariantCulture);
			if (published.Date < month || !keys.Add((month, published)))
				throw new InvalidOperationException("Monthly yield observations need causal, unique publication dates.");
			_yields.Add(new(month, published, earnings - bond));
		}
	}

	private bool SelectTargets(DateTime time, DateTime asOf)
	{
		var calendar = _securities[0].Board.WorkingTime;
		if (!calendar.IsWorkingDate(time.Date, true)) return false;
		for (var next = time.Date.AddDays(1); next.Month == time.Month; next = next.AddDays(1))
			if (calendar.IsWorkingDate(next, true)) return false;
		var month = new DateTime(time.Year, time.Month, 1);
		var y = new List<decimal>();
		for (var i = RegressionMonths - 1; i >= 0; i--)
		{
			var observation = _yields.Where(v => v.Month == month.AddMonths(-i) && v.Published <= asOf)
				.OrderByDescending(v => v.Published).FirstOrDefault();
			if (observation is null) return false;
			y.Add(observation.Spread);
		}
		var meanX = (RegressionMonths - 1m) / 2m;
		var meanY = y.Average();
		var variance = Enumerable.Range(0, RegressionMonths).Sum(i => (i - meanX) * (i - meanX));
		var slope = y.Select((v, i) => (i - meanX) * (v - meanY)).Sum() / variance;
		var prediction = meanY + slope * (RegressionMonths - meanX);
		if (prediction == 0m) return false;
		var equity = prediction > 0m;
		if (_equityRegime == equity) return true;
		_equityRegime = equity;
		var security = equity ? _securities[0] : _securities.ElementAtOrDefault(1);
		var targets = new Dictionary<Security, decimal>();
		if (security is not null) targets[security] = Units(security, Budget);
		BeginRotation(targets);
		return true;
	}
}
