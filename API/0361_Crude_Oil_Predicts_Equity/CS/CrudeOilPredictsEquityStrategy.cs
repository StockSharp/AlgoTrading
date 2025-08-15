using System;
using System.Collections.Generic;
using System.Linq;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that invests in an equity ETF when the last month's crude oil return is positive;
/// otherwise, it holds a cash ETF.
/// </summary>
public class CrudeOilPredictsEquityStrategy : Strategy
{
	private readonly StrategyParam<Security> _oil;
	private readonly StrategyParam<Security> _cash;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _lookback;

	/// <summary>
	/// Equity ETF to invest in.
	/// </summary>
	public Security Equity
	{
		get => Security;
		set => Security = value;
	}

	/// <summary>
	/// Crude oil security used for signal calculation.
	/// </summary>
	public Security Oil
	{
		get => _oil.Value;
		set => _oil.Value = value;
	}

	/// <summary>
	/// Cash ETF to hold when oil return is negative.
	/// </summary>
	public Security CashEtf
	{
		get => _cash.Value;
		set => _cash.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of candles to look back for return calculation.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	private readonly Dictionary<Security, RollingWindow<decimal>> _wins = [];
	private readonly Dictionary<Security, decimal> _latestPrices = [];
	private DateTime _lastDay = DateTime.MinValue;

	/// <summary>
	/// Initializes a new instance of <see cref="CrudeOilPredictsEquityStrategy"/>.
	/// </summary>
	public CrudeOilPredictsEquityStrategy()
	{
		_oil = Param<Security>(nameof(Oil), null)
			.SetDisplay("Oil", "Crude oil security for signal", "General");

		_cash = Param<Security>(nameof(CashEtf), null)
			.SetDisplay("Cash ETF", "Cash ETF when not invested", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for analysis", "General");

		_lookback = Param(nameof(Lookback), 22)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Number of candles for return calculation", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Equity == null || Oil == null || CashEtf == null)
			throw new InvalidOperationException("Set securities");

		return [(Equity, CandleType), (Oil, CandleType), (CashEtf, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_wins.Clear();
		_latestPrices.Clear();
		_lastDay = default;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var securities = GetWorkingSecurities().ToArray();
		if (securities.Length == 0)
			throw new InvalidOperationException("No securities configured.");

		foreach (var (s, dt) in securities)
		{
			_wins[s] = new RollingWindow<decimal>(Lookback + 1);

			SubscribeCandles(dt, true, s)
				.Bind(c => ProcessCandle(c, s))
				.Start();
		}
	}

	private void ProcessCandle(ICandleMessage candle, Security security)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Store the latest closing price for this security
		_latestPrices[security] = candle.ClosePrice;

		_wins[security].Add(candle.ClosePrice);
		var d = candle.OpenTime.Date;
		if (d == _lastDay)
			return;
		_lastDay = d;
		if (d.Day == 1)
			Rebalance();
	}

	private void Rebalance()
	{
		if (!_wins[Oil].IsFull())
			return;
		var oilRet = (_wins[Oil].Last() - _wins[Oil][0]) / _wins[Oil][0];
		if (oilRet > 0)
			MoveTo(Equity);
		else
			MoveTo(CashEtf);
	}

	private void MoveTo(Security target)
	{
		foreach (var position in Positions)
			if (position.Security != target)
				Move(position.Security, 0);

		var portfolioValue = Portfolio.CurrentValue ?? 0m;
		var price = GetLatestPrice(target);
		if (price > 0)
			Move(target, portfolioValue / price);
	}

	private decimal GetLatestPrice(Security security)
	{
		return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
	}

	private void Move(Security s, decimal tgt)
	{
		var diff = tgt - PositionBy(s);
		var price = GetLatestPrice(s);
		if (price <= 0 || Math.Abs(diff) * price < 100)
			return;
		RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "OilEq" });
	}
	private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;

	#region RollingWindow
	private class RollingWindow<T>
	{
		private readonly Queue<T> _q = [];
		private readonly int _n;
		public RollingWindow(int n) { _n = n; }
		public void Add(T v) { if (_q.Count == _n) _q.Dequeue(); _q.Enqueue(v); }
		public bool IsFull() => _q.Count == _n;
		public T Last() => _q.Last();
		public T this[int i] => _q.ElementAt(i);
	}
	#endregion

}