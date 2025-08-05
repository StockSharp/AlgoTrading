// ShortTermReversalFuturesStrategy.cs
// -----------------------------------------------------------------------------
// Prior-week reversal: long losers, short winners among Universe.
// Weekly rebalance triggered by Monday's first closed daily candle.
// -----------------------------------------------------------------------------
// Date: 2 Aug 2025
// -----------------------------------------------------------------------------
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
	/// Short-term reversal strategy for futures.
	/// Buys prior losers and sells prior winners on a weekly basis.
	/// </summary>
	public class ShortTermReversalFuturesStrategy : Strategy
	{
		private readonly StrategyParam<IEnumerable<Security>> _univ;
		private readonly StrategyParam<int> _look;
		private readonly StrategyParam<decimal> _min;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// The type of candles to use for strategy calculation.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}
		private readonly Dictionary<Security, Queue<decimal>> _px = new();
		private readonly Dictionary<Security, decimal> _w = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private DateTime _last = DateTime.MinValue;

		/// <summary>
		/// Universe of futures to trade.
		/// </summary>
		public IEnumerable<Security> Universe
		{
			get => _univ.Value;
			set => _univ.Value = value;
		}

		/// <summary>
		/// Lookback period in days for reversal calculation.
		/// </summary>
		public int LookbackDays
		{
			get => _look.Value;
			set => _look.Value = value;
		}

		/// <summary>
		/// Minimum trade value in USD.
		/// </summary>
		public decimal MinTradeUsd
		{
			get => _min.Value;
			set => _min.Value = value;
		}

		/// <summary>
		/// Initializes strategy parameters.
		/// </summary>
		public ShortTermReversalFuturesStrategy()
		{
			_univ = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>())
				.SetDisplay("Universe", "Futures contracts to trade", "General");

			_look = Param(nameof(LookbackDays), 5)
				.SetGreaterThanZero()
				.SetDisplay("Lookback Days", "Lookback period for reversal", "Parameters");

			_min = Param(nameof(MinTradeUsd), 200m)
				.SetGreaterThanZero()
				.SetDisplay("Min Trade USD", "Minimum trade value in USD", "Parameters");
			_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
		}

		public override IEnumerable<(Security, DataType)> GetWorkingSecurities() => Universe.Select(s => (s, CandleType));

		protected override void OnStarted(DateTimeOffset t)
		{
			base.OnStarted(t);

			if (Universe == null || !Universe.Any())
				throw new InvalidOperationException("Universe is empty");

			foreach (var (s, tf) in GetWorkingSecurities())
			{
				_px[s] = new Queue<decimal>();
				SubscribeCandles(tf, true, s).Bind(c => ProcessCandle(c, s)).Start();
			}
		}

		private void ProcessCandle(ICandleMessage candle, Security security)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Store the latest closing price for this security
			_latestPrices[security] = candle.ClosePrice;

			OnDaily(security, candle);
		}

		private void OnDaily(Security s, ICandleMessage c)
		{
			var q = _px[s];
			if (q.Count == LookbackDays + 1)
				q.Dequeue();
			q.Enqueue(c.ClosePrice);
			var d = c.OpenTime.Date;
			if (d == _last)
				return;
			_last = d;
			if (d.DayOfWeek != DayOfWeek.Monday)
				return;
			Rebalance();
		}
		private void Rebalance()
		{
			var perf = new Dictionary<Security, decimal>();
			foreach (var kv in _px)
				if (kv.Value.Count == LookbackDays + 1)
					perf[kv.Key] = (kv.Value.Peek() - kv.Value.Last()) / kv.Value.Last();
			if (perf.Count < 10)
				return;
			int bucket = perf.Count / 10;
			var losers = perf.OrderBy(kv => kv.Value).Take(bucket).Select(kv => kv.Key).ToList();
			var winners = perf.OrderByDescending(kv => kv.Value).Take(bucket).Select(kv => kv.Key).ToList();
			_w.Clear();
			decimal wl = 1m / losers.Count, ws = -1m / winners.Count;
			foreach (var s in losers)
				_w[s] = wl;
			foreach (var s in winners)
				_w[s] = ws;
			foreach (var position in Positions.Where(p => !_w.ContainsKey(p.Security)))
				Move(position.Security, 0);
			var portfolioValue = Portfolio.CurrentValue ?? 0m;
			foreach (var kv in _w)
			{
				var price = GetLatestPrice(kv.Key);
				if (price > 0)
					Move(kv.Key, kv.Value * portfolioValue / price);
			}
		}
		private void Move(Security s, decimal tgt) 
		{ 
			var diff = tgt - Pos(s); 
			var price = GetLatestPrice(s);
			if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd) 
				return; 
			RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "STRFut" }); 
		}

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}

		private decimal Pos(Security s) => GetPositionValue(s, Portfolio) ?? 0;
	}
}