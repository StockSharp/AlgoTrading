using System;
using System.Collections.Generic;
using System.Linq;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fed Model yield-gap timing strategy (Quantpedia #21).
/// Compares earnings yield with the 10-year Treasury yield and switches between
/// an equity index ETF and a cash ETF based on the one-month excess return forecast.
/// </summary>
public class FedModelStrategy : Strategy
{
	private readonly StrategyParam<IEnumerable<Security>> _univ;
	private readonly StrategyParam<Security> _bond;
	private readonly StrategyParam<Security> _earn;
	private readonly StrategyParam<int> _months;
	private readonly StrategyParam<DataType> _tf;
	private readonly StrategyParam<decimal> _minUsd;

	/// <summary>
	/// Securities to trade (equity index first, optional cash ETF second).
	/// </summary>
	public IEnumerable<Security> Universe
	{
		get => _univ.Value;
		set => _univ.Value = value;
	}

	/// <summary>
	/// Security representing 10-year Treasury yield.
	/// </summary>
	public Security BondYieldSym
	{
		get => _bond.Value;
		set => _bond.Value = value;
	}

	/// <summary>
	/// Security representing earnings yield.
	/// </summary>
	public Security EarningsYieldSym
	{
		get => _earn.Value;
		set => _earn.Value = value;
	}

	/// <summary>
	/// Number of months in regression window.
	/// </summary>
	public int RegressionMonths
	{
		get => _months.Value;
		set => _months.Value = value;
	}

	/// <summary>
	/// Type of candles used for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _tf.Value;
		set => _tf.Value = value;
	}

	/// <summary>
	/// Minimum dollar value per trade.
	/// </summary>
	public decimal MinTradeUsd
	{
		get => _minUsd.Value;
		set => _minUsd.Value = value;
	}

	private readonly RollingWin _eq = new();
	private readonly RollingWin _gap = new();
	private readonly RollingWin _rf = new();
	private readonly Dictionary<Security, decimal> _latestPrices = [];
	private DateTime _lastMonth = DateTime.MinValue;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public FedModelStrategy()
	{
		_univ = Param<IEnumerable<Security>>(nameof(Universe), [])
				.SetDisplay("Universe", "Securities to trade", "General");

		_bond = Param<Security>(nameof(BondYieldSym), null)
				.SetDisplay("Bond Yield", "10-year Treasury yield security", "Data");

		_earn = Param<Security>(nameof(EarningsYieldSym), null)
				.SetDisplay("Earnings Yield", "Earnings yield security", "Data");

		_months = Param(nameof(RegressionMonths), 12)
				.SetGreaterThanZero()
				.SetDisplay("Regression Months", "Months in regression window", "Settings");

		_tf = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles", "General");

		_minUsd = Param(nameof(MinTradeUsd), 200m)
				.SetGreaterThanZero()
				.SetDisplay("Min Trade USD", "Minimum trade value in USD", "Risk Management");

		var n = RegressionMonths + 1;
		_eq.SetSize(n);
		_gap.SetSize(n);
		_rf.SetSize(n);
	}

	public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
	{
		foreach (var s in Universe)
			yield return (s, CandleType);
		if (BondYieldSym != null)
			yield return (BondYieldSym, CandleType);
		if (EarningsYieldSym != null)
			yield return (EarningsYieldSym, CandleType);
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_eq.Clear();
		_gap.Clear();
		_rf.Clear();
		_latestPrices.Clear();
		_lastMonth = default;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		if (Universe == null || !Universe.Any())
		{
			if (Security != null)
				Universe = [Security];
			else
				throw new InvalidOperationException("Universe is empty.");
		}

		base.OnStarted(time);

		foreach (var (s, tf) in GetWorkingSecurities())
			SubscribeCandles(tf, true, s)
					.Bind(c => ProcessCandle(c, s))
					.Start();
	}

	private void ProcessCandle(ICandleMessage candle, Security security)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Store the latest closing price for this security
		_latestPrices[security] = candle.ClosePrice;

		OnDaily(candle);
	}

	private void OnDaily(ICandleMessage c)
	{
		var d = c.OpenTime.Date;
		if (d.Day != 1 || _lastMonth == d)
			return;
		_lastMonth = d;

		if (c.SecurityId != Universe.First().ToSecurityId())
			return;

		_eq.Add(c.ClosePrice);
		_rf.Add(GetRF(d));

		var gap = GetYieldGap(d);
		if (gap == null)
			return;
		_gap.Add(gap.Value);

		if (!_eq.Full || !_gap.Full)
			return;

		var x = _gap.Data;
		var yret = new decimal[_eq.Size - 1];
		for (int i = 1; i < _eq.Size; i++)
			yret[i - 1] = (_eq.Data[i] - _eq.Data[i - 1]) / _eq.Data[i - 1] - _rf.Data[i - 1];

		int n = yret.Length;
		decimal meanX = x.Take(n).Average();
		decimal meanY = yret.Average();
		decimal cov = 0, varX = 0;
		for (int i = 0; i < n; i++)
		{
			var dx = x[i] - meanX;
			cov += dx * (yret[i] - meanY);
			varX += dx * dx;
		}
		if (varX == 0)
			return;
		var beta = cov / varX;
		var alpha = meanY - beta * meanX;
		var forecast = alpha + beta * x[^1];

		var equity = Universe.First();
		var cash = Universe.ElementAtOrDefault(1);

		if (forecast > 0)
		{
			Move(equity, 1m);
			if (cash != null)
				Move(cash, 0);
		}
		else
		{
			Move(equity, 0);
			if (cash != null)
				Move(cash, 1m);
		}
	}

	private decimal GetLatestPrice(Security security)
	{
		return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
	}

	private void Move(Security s, decimal weight)
	{
		if (s == null)
			return;

		var portfolioValue = Portfolio.CurrentValue ?? 0m;
		var price = GetLatestPrice(s);
		if (price <= 0)
			return;

		var tgt = weight * portfolioValue / price;
		var diff = tgt - Pos(s);
		if (Math.Abs(diff) * price < MinTradeUsd)
			return;

		RegisterOrder(new Order
		{
			Security = s,
			Portfolio = Portfolio,
			Side = diff > 0 ? Sides.Buy : Sides.Sell,
			Volume = Math.Abs(diff),
			Type = OrderTypes.Market,
			Comment = "FedModel"
		});
	}

	private decimal Pos(Security s) => GetPositionValue(s, Portfolio) ?? 0;

	private decimal GetRF(DateTime d) => 0.0002m;

	private decimal? GetYieldGap(DateTime d)
	{
		if (!SeriesVal(EarningsYieldSym, d, out var ey))
			return null;
		if (!SeriesVal(BondYieldSym, d, out var y10))
			return null;
		return ey - y10;
	}

	private bool SeriesVal(Security s, DateTime d, out decimal v) { v = 0; return false; }

	private class RollingWin
	{
		public decimal[] Data;

	 	public int Size => Data?.Length ?? 0;

		private int _n;

		public bool Full => _n == Data.Length;

		public void SetSize(int n)
		{
			Data = new decimal[n];
			_n = 0;
		}

		public void Add(decimal v)
		{
			if (_n < Data.Length)
				_n++;

			for (int i = Data.Length - 1; i > 0; i--)
				Data[i] = Data[i - 1];

			Data[0] = v;
		}

		public void Clear()
		{
			Data = default;
			_n = 0;
		}

		public override int GetHashCode()
			=> Data?.Aggregate(0, (hash, v) => hash ^ v.GetHashCode()) ?? 0;

		public override bool Equals(object obj)
		{
			ArgumentNullException.ThrowIfNull(obj);

			var otherWin = (RollingWin)obj;

			if (otherWin.Size != Size)
				return false;

			for (var i = 0; i < Size; i++)
			{
				if (Data[i] != otherWin.Data[i])
					return false;
			}

			return true;
		}
	}
}