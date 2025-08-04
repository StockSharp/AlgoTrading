// DispersionTradingStrategy.cs  (revised, candle-stream driven)
// • Trades equity index vs. constituents based on average correlation (dispersion signal).
// • Parameters: IndexSec, Constituents (IEnumerable<Security>), CandleType (StrategyParam<DataType>),
//   LookbackDays, CorrThreshold, MinTradeUsd.
// • Subscribes to daily candles; on each finished candle checks if new day → recompute correlation;
//   if avgCorr < threshold => open dispersion (long constituents, short index), else flat.
// Date: 2 August 2025

using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	public class DispersionTradingStrategy : Strategy
	{
		#region Params
		private readonly StrategyParam<Security> _index;
		private readonly StrategyParam<IEnumerable<Security>> _const;
		private readonly StrategyParam<int> _lookback;
		private readonly StrategyParam<decimal> _corrThresh;
		private readonly StrategyParam<decimal> _minUsd;
		private readonly StrategyParam<DataType> _tf;

		public Security IndexSec { get => _index.Value; set => _index.Value = value; }
		public IEnumerable<Security> Constituents { get => _const.Value; set => _const.Value = value; }
		public int LookbackDays => _lookback.Value;
		public decimal CorrThreshold => _corrThresh.Value;
		public decimal MinTradeUsd => _minUsd.Value;
		public DataType CandleType => _tf.Value;
		#endregion

		private readonly Dictionary<Security, RollingWindow<decimal>> _wins = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private DateTime _lastDay = DateTime.MinValue;
		private bool _open;                                // current dispersion state

		public DispersionTradingStrategy()
		{
			_index = Param<Security>(nameof(IndexSec), null);
			_const = Param<IEnumerable<Security>>(nameof(Constituents), Array.Empty<Security>());
			_lookback = Param(nameof(LookbackDays), 60);
			_corrThresh = Param(nameof(CorrThreshold), 0.4m);
			_minUsd = Param(nameof(MinTradeUsd), 100m);
			_tf = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame());
		}

		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			if (IndexSec == null || !Constituents.Any())
				throw new InvalidOperationException("Set IndexSec and Constituents");
			return Constituents.Append(IndexSec).Select(s => (s, CandleType));
		}

		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			foreach (var (sec, dt) in GetWorkingSecurities())
			{
				_wins[sec] = new RollingWindow<decimal>(LookbackDays + 1);

				SubscribeCandles(dt, true, sec)
					.Bind(c => ProcessCandle(c, sec))
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

			if (_wins.Values.Any(w => !w.IsFull()))
				return;

			EvaluateSignal();        // daily check after windows full
		}

		private void EvaluateSignal()
		{
			var indexRet = Returns(_wins[IndexSec]);

			var corrs = new List<decimal>();
			foreach (var s in Constituents)
				corrs.Add(Corr(Returns(_wins[s]), indexRet));

			var avg = corrs.Average();

			if (avg < CorrThreshold && !_open)
				OpenDispersion();
			else if (avg >= CorrThreshold && _open)
				CloseAll();
		}

		private void OpenDispersion()
		{
			int n = Constituents.Count();
			var portfolioValue = Portfolio.CurrentValue ?? 0m;
			decimal capLeg = portfolioValue * 0.5m; // 50% per leg
			decimal eachLong = capLeg / n;

			foreach (var s in Constituents)
			{
				var price = GetLatestPrice(s);
				if (price > 0)
					TradeToTarget(s, eachLong / price);
			}

			var indexPrice = GetLatestPrice(IndexSec);
			if (indexPrice > 0)
				TradeToTarget(IndexSec, -capLeg / indexPrice);   // short index
				
			_open = true;
			LogInfo("Opened dispersion spread");
		}

		private void CloseAll()
		{
			foreach (var position in Positions)
				TradeToTarget(position.Security, 0m);
			_open = false;
			LogInfo("Closed dispersion spread");
		}

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}

		#region Helper math / trading
		private decimal[] Returns(RollingWindow<decimal> win)
		{
			var arr = win.ToArray();
			var r = new decimal[arr.Length - 1];
			for (int i = 1; i < arr.Length; i++)
				r[i - 1] = (arr[i] - arr[i - 1]) / arr[i - 1];
			return r;
		}

		private decimal Corr(decimal[] x, decimal[] y)
		{
			int n = Math.Min(x.Length, y.Length);
			var meanX = x.Take(n).Average();
			var meanY = y.Take(n).Average();
			decimal num = 0, dx = 0, dy = 0;
			for (int i = 0; i < n; i++)
			{
				var a = x[i] - meanX;
				var b = y[i] - meanY;
				num += a * b;
				dx += a * a;
				dy += b * b;
			}
			return dx > 0 && dy > 0 ? num / (decimal)Math.Sqrt((double)(dx * dy)) : 0m;
		}

		private void TradeToTarget(Security s, decimal tgtQty)
		{
			var diff = tgtQty - PositionBy(s);
			var price = GetLatestPrice(s);
			if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd)
				return;

			RegisterOrder(new Order
			{
				Security = s,
				Portfolio = Portfolio,
				Side = diff > 0 ? Sides.Buy : Sides.Sell,
				Volume = Math.Abs(diff),
				Type = OrderTypes.Market,
				Comment = "Dispersion"
			});
		}

		private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;

		#endregion

		#region RollingWindow
		private class RollingWindow<T>
		{
			private readonly Queue<T> _q = new();
			private readonly int _n;
			public RollingWindow(int n) { _n = n; }
			public void Add(T v) { if (_q.Count == _n) _q.Dequeue(); _q.Enqueue(v); }
			public bool IsFull() => _q.Count == _n;
			public T Last() => _q.Last();
			public T[] ToArray() => _q.ToArray();
		}
		#endregion
	}
}