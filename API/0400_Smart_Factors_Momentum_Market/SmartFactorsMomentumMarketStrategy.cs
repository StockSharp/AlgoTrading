// SmartFactorsMomentumMarketStrategy.cs
// Smart factors momentum blended with market; monthly rotation.
// Date: 2 August 2025

using System;
using System.Collections.Generic;
using System.Linq;
using Ecng.Common;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Smart factors momentum blended with market momentum.
	/// Rotates monthly between factor basket and market ETF.
	/// </summary>
	public class SmartFactorsMomentumMarketStrategy : Strategy
	{
		private readonly StrategyParam<Dictionary<string, Security>> _factors;
		private readonly StrategyParam<int> _fastM;
		private readonly StrategyParam<int> _slowM;
		private readonly StrategyParam<int> _maM;
		private readonly StrategyParam<decimal> _minUsd;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// Dictionary of smart factor ETFs.
		/// </summary>
		public Dictionary<string, Security> Factors
		{
			get => _factors.Value;
			set => _factors.Value = value;
		}

		/// <summary>
		/// Fast momentum lookback in months.
		/// </summary>
		public int FastMonths
		{
			get => _fastM.Value;
			set => _fastM.Value = value;
		}

		/// <summary>
		/// Slow momentum lookback in months.
		/// </summary>
		public int SlowMonths
		{
			get => _slowM.Value;
			set => _slowM.Value = value;
		}

		/// <summary>
		/// Moving average window in months.
		/// </summary>
		public int MaMonths
		{
			get => _maM.Value;
			set => _maM.Value = value;
		}

		/// <summary>
		/// Minimum trade value in USD.
		/// </summary>
		public decimal MinTradeUsd
		{
			get => _minUsd.Value;
			set => _minUsd.Value = value;
		}

		/// <summary>
		/// The type of candles to use for strategy calculation.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		private readonly Dictionary<Security, RollingWindow<decimal>> _p = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private readonly RollingWindow<decimal> _smartRet;
		private readonly RollingWindow<decimal> _mktRet;
		private DateTime _lastRebalanceDate = DateTime.MinValue;

		/// <summary>
		/// Initializes strategy parameters.
		/// </summary>
		public SmartFactorsMomentumMarketStrategy()
		{
			_factors = Param(nameof(Factors), new Dictionary<string, Security>())
				.SetDisplay("Factors", "Smart factor ETFs", "General");

			_fastM = Param(nameof(FastMonths), 1)
				.SetGreaterThanZero()
				.SetDisplay("Fast Months", "Fast momentum lookback", "Parameters");

			_slowM = Param(nameof(SlowMonths), 12)
				.SetGreaterThanZero()
				.SetDisplay("Slow Months", "Slow momentum lookback", "Parameters");

			_maM = Param(nameof(MaMonths), 12)
				.SetGreaterThanZero()
				.SetDisplay("MA Months", "Moving average window", "Parameters");

			_minUsd = Param(nameof(MinTradeUsd), 50m)
				.SetGreaterThanZero()
				.SetDisplay("Min Trade USD", "Minimum trade value in USD", "Parameters");

			_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_smartRet = new RollingWindow<decimal>(_maM.Value);
			_mktRet = new RollingWindow<decimal>(_maM.Value);
		}

		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			if (Security is null)
				throw new InvalidOperationException("MarketETF not set");
			if (!Factors.Any())
				throw new InvalidOperationException("No factors");

			return Factors.Values.Append(Security).Select(s => (s, CandleType));
		}


		protected override void OnReseted()
		{
			base.OnReseted();

			_p.Clear();
			_latestPrices.Clear();
			_smartRet.Clear();
			_mktRet.Clear();
			_lastRebalanceDate = default;
		}

		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			if (Security == null)
				throw new InvalidOperationException("MarketETF not set");

			if (Factors == null || Factors.Count == 0)
				throw new InvalidOperationException("No factors");

			foreach (var (sec, dt) in GetWorkingSecurities())
			{
				SubscribeCandles(dt, true, sec)
				.Bind(c => ProcessCandle(c, sec))
				.Start();

				_p[sec] = new RollingWindow<decimal>(Math.Max(SlowMonths * 21 + 1, 260));
			}
		}

		private void ProcessCandle(ICandleMessage candle, Security security)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Store the latest closing price for this security
			_latestPrices[security] = candle.ClosePrice;

			OnCandleFinished(candle, security);
		}

		private void OnCandleFinished(ICandleMessage candle, Security sec)
		{
			if (_p.TryGetValue(sec, out var win))
				win.Add(candle.ClosePrice);

			// Check for monthly rebalancing (first trading day of month)
			var candleDate = candle.OpenTime.Date;
			if (candleDate.Day == 1 && candleDate != _lastRebalanceDate)
			{
				_lastRebalanceDate = candleDate;
				Rebalance();
			}
		}

		private void Rebalance()
		{
			if (_p.Any(kv => !kv.Value.IsFull()))
				return;

			var fastSig = new Dictionary<Security, decimal>();
			var slowSig = new Dictionary<Security, decimal>();
			foreach (var sec in Factors.Values)
			{
				var win = _p[sec];
				int fastIndex = FastMonths * 21 + 1;
				int slowIndex = SlowMonths * 21 + 1;
				fastSig[sec] = (win.Last() - win[win.Count - fastIndex]) / win[win.Count - fastIndex];
				slowSig[sec] = (win.Last() - win[win.Count - slowIndex]) / win[win.Count - slowIndex];
			}

			int rankSum = Enumerable.Range(1, Factors.Count).Sum();
			var wFast = RankWeights(fastSig, rankSum);
			var wSlow = RankWeights(slowSig, rankSum);

			var wTotal = Factors.Values.ToDictionary(s => s, s => 0.75m * wFast[s] + 0.25m * wSlow[s]);

			var smart1M = wTotal.Sum(kv => kv.Value * fastSig[kv.Key]);
			var mkt1M = (_p[Security].Last() - _p[Security][_p[Security].Count - 22]) / _p[Security][_p[Security].Count - 22];
			_smartRet.Add(smart1M);
			_mktRet.Add(mkt1M);
			if (!_smartRet.IsFull())
				return;

			int scoreSmart = 0, scoreMkt = 0;
			for (int i = 1; i <= MaMonths; i++)
			{
				var smaS = _smartRet.Take(i).Average();
				var smaM = _mktRet.Take(i).Average();
				if (smaS > smaM)
					scoreSmart++;
				else
					scoreMkt++;
			}
			decimal wSmart = scoreSmart / (decimal)MaMonths;
			decimal wMarket = scoreMkt / (decimal)MaMonths;

			TradeToTarget(Security, wMarket);
			foreach (var kv in wTotal)
				TradeToTarget(kv.Key, wSmart * kv.Value);
			foreach (var position in Positions.Where(p => p.Security != Security && !wTotal.ContainsKey(p.Security)))
				TradeToTarget(position.Security, 0m);
		}

		private Dictionary<Security, decimal> RankWeights(Dictionary<Security, decimal> sig, int rankSum)
		{
			var sorted = sig.OrderBy(kv => Math.Abs(kv.Value)).ToList();
			var d = new Dictionary<Security, decimal>();
			for (int i = 0; i < sorted.Count; i++)
				d[sorted[i].Key] = (i + 1) / (decimal)rankSum * Math.Sign(sorted[i].Value);
			return d;
		}

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}

		private void TradeToTarget(Security sec, decimal weight)
		{
			var portfolioValue = Portfolio.CurrentValue ?? 0m;
			var price = GetLatestPrice(sec);
			if (price <= 0)
				return;

			var tgt = weight * portfolioValue / price;
			var diff = tgt - PositionBy(sec);
			if (Math.Abs(diff) * price >= MinTradeUsd)
				RegisterOrder(new Order { Security = sec, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "SmartFactors" });
		}

		private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;
	}

	#region RollingWindow helper
	public class RollingWindow<T>
	{
		private readonly Queue<T> _q = new(); private readonly int _size;

		public int Count => _q.Count;

		public RollingWindow(int size) { _size = size; }
		public void Add(T v) { if (_q.Count == _size) _q.Dequeue(); _q.Enqueue(v); }
		public bool IsFull() => _q.Count == _size;
		public T Last() => _q.Last();
		public T this[int idx] => _q.ElementAt(idx);
		public IEnumerable<T> Take(int n) => _q.Reverse().Take(n);

		public void Clear() => _q.Clear();
	}
	#endregion
}