// SkewnessCommodityStrategy.cs
// -----------------------------------------------------------------------------
// Compute skewness of daily returns over WindowDays for each futures contract.
// Long TopN most negative-skew, short TopN most positive-skew.
// Monthly rebalance on the first trading day, triggered by candle-stream
// (SubscribeCandles → Bind(CandleStates.Finished) → OnDaily).
 // No Schedule() is used.
// -----------------------------------------------------------------------------
// Date: 2 Aug 2025
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Skewness-based commodity strategy.
	/// Longs negatively skewed futures and shorts positively skewed ones.
	/// </summary>
	public class SkewnessCommodityStrategy : Strategy
	{
		#region Parameters
		private readonly StrategyParam<IEnumerable<Security>> _futures;
		private readonly StrategyParam<int> _window;
		private readonly StrategyParam<int> _topN;
		private readonly StrategyParam<decimal> _minUsd;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// Futures contracts to evaluate.
		/// </summary>
		public IEnumerable<Security> Futures
		{
			get => _futures.Value;
			set => _futures.Value = value;
		}

		/// <summary>
		/// Window length in days for skewness calculation.
		/// </summary>
		public int WindowDays
		{
			get => _window.Value;
			set => _window.Value = value;
		}

		/// <summary>
		/// Number of contracts per side to trade.
		/// </summary>
		public int TopN
		{
			get => _topN.Value;
			set => _topN.Value = value;
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
		#endregion

		// rolling windows of prices
		private readonly Dictionary<Security, Queue<decimal>> _px = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private DateTime _lastProcessed = DateTime.MinValue;
		private readonly Dictionary<Security, decimal> _weight = new();

		/// <summary>
		/// Initializes strategy parameters.
		/// </summary>
		public SkewnessCommodityStrategy()
		{
			_futures = Param<IEnumerable<Security>>(nameof(Futures), Array.Empty<Security>())
				.SetDisplay("Futures", "Contracts to analyze", "General");

			_window = Param(nameof(WindowDays), 120)
				.SetGreaterThanZero()
				.SetDisplay("Window Days", "Window length for skewness", "Parameters");

			_topN = Param(nameof(TopN), 5)
				.SetGreaterThanZero()
				.SetDisplay("Top N", "Number of contracts per side", "Parameters");

			_minUsd = Param(nameof(MinTradeUsd), 200m)
				.SetGreaterThanZero()
				.SetDisplay("Min Trade USD", "Minimum trade value in USD", "Parameters");
			_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
		}

		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() =>
			Futures.Select(f => (f, CandleType));

		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			if (Futures == null || !Futures.Any())
				throw new InvalidOperationException("Futures set is empty");

			foreach (var (s, tf) in GetWorkingSecurities())
			{
				_px[s] = new Queue<decimal>();
				SubscribeCandles(tf, true, s)
					.Bind(c => OnDaily(s, c))
					.Start();
			}
		}

		private void OnDaily(Security s, ICandleMessage candle)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Store the latest closing price for this security
			_latestPrices[s] = candle.ClosePrice;

			var q = _px[s];
			if (q.Count == WindowDays + 1)
				q.Dequeue();
			q.Enqueue(candle.ClosePrice);

			var d = candle.OpenTime.Date;
			if (d == _lastProcessed)
				return;
			_lastProcessed = d;

			if (d.Day != 1)
				return;

			Rebalance();
		}

		private void Rebalance()
		{
			var skew = new Dictionary<Security, double>();

			foreach (var kv in _px)
			{
				var q = kv.Value;
				if (q.Count < WindowDays + 1)
					continue;

				var arr = q.ToArray();
				var ret = new double[arr.Length - 1];
				for (int i = 1; i < arr.Length; i++)
					ret[i - 1] = (double)((arr[i] - arr[i - 1]) / arr[i - 1]);

				var mean = ret.Average();
				var sd = Math.Sqrt(ret.Select(r => (r - mean) * (r - mean)).Average());
				if (sd == 0)
					continue;

				var sk = ret.Select(r => Math.Pow((r - mean) / sd, 3)).Average();
				skew[kv.Key] = sk;
			}

			if (skew.Count < TopN * 2)
				return;

			var longSide = skew.OrderBy(kv => kv.Value).Take(TopN).Select(kv => kv.Key).ToList();        // most negative
			var shortSide = skew.OrderByDescending(kv => kv.Value).Take(TopN).Select(kv => kv.Key).ToList();

			_weight.Clear();
			decimal wl = 1m / longSide.Count;
			decimal ws = -1m / shortSide.Count;
			foreach (var s in longSide)
				_weight[s] = wl;
			foreach (var s in shortSide)
				_weight[s] = ws;

			foreach (var pos in Positions.Where(position => !_weight.ContainsKey(position.Security)))
				TradeTo(pos.Security, 0);

			var portfolioValue = Portfolio.CurrentValue ?? 0m;
			foreach (var kv in _weight)
			{
				var price = GetLatestPrice(kv.Key);
				if (price > 0)
					TradeTo(kv.Key, kv.Value * portfolioValue / price);
			}
		}

		private void TradeTo(Security s, decimal tgtQty)
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
				Comment = "SkewCom"
			});
		}

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}

		private decimal PositionBy(Security s) =>
			GetPositionValue(s, Portfolio) ?? 0m;
	}
}
