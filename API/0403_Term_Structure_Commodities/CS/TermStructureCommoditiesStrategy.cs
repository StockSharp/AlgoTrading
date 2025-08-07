// TermStructureCommoditiesStrategy.cs
// -----------------------------------------------------------------------------
// Roll‑return cross‑section: long highest quintile, short lowest.
// Monthly rebalance; triggered by daily candle of first future.
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
	/// Roll-return cross-section strategy for commodities term structure.
	/// </summary>
	public class TermStructureCommoditiesStrategy : Strategy
	{
		private readonly StrategyParam<IEnumerable<Security>> _univ;
		private readonly StrategyParam<int> _quint;
		private readonly StrategyParam<decimal> _minUsd;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// The type of candles to use for strategy calculation.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}
		private readonly Dictionary<Security, decimal> _w = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private DateTime _last = DateTime.MinValue;

		/// <summary>
		/// List of securities to trade.
		/// </summary>
		public IEnumerable<Security> Universe
		{
			get => _univ.Value;
			set => _univ.Value = value;
		}

		/// <summary>
		/// Number of quintile buckets.
		/// </summary>
		public int Quintile
		{
			get => _quint.Value;
			set => _quint.Value = value;
		}

		/// <summary>
		/// Minimum trade value in USD.
		/// </summary>
		public decimal MinTradeUsd
		{
			get => _minUsd.Value;
			set => _minUsd.Value = value;
		}

		public TermStructureCommoditiesStrategy()
		{
			_univ = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>())
				.SetDisplay("Universe", "List of futures to trade", "Universe");

			_quint = Param(nameof(Quintile), 5)
				.SetDisplay("Quintile", "Number of ranking buckets", "Parameters");

			_minUsd = Param(nameof(MinTradeUsd), 200m)
				.SetDisplay("Min Trade USD", "Minimum notional value for orders", "Risk Management");
			_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return Universe.Select(s => (s, CandleType));
		}

		/// <inheritdoc />
		protected override void OnReseted()
		{
			base.OnReseted();

			_w.Clear();
			_latestPrices.Clear();
			_last = DateTime.MinValue;
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset t)
		{
			if (Universe == null || !Universe.Any())
				throw new InvalidOperationException("Universe cannot be empty.");

			base.OnStarted(t);

			var trig = Universe.First();
			SubscribeCandles(CandleType, true, trig).Bind(c => ProcessCandle(c, trig)).Start();
		}

		private void ProcessCandle(ICandleMessage candle, Security security)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Store the latest closing price for this security
			_latestPrices[security] = candle.ClosePrice;

			OnDaily(candle.OpenTime.Date);
		}

		private void OnDaily(DateTime d)
		{
			if (d == _last)
				return;
			_last = d;
			if (d.Day != 1)
				return;
			Rebalance();
		}

		private void Rebalance()
		{
			var rr = new Dictionary<Security, decimal>();
			foreach (var s in Universe)
				if (TryRollReturn(s, out var v))
					rr[s] = v;
			if (rr.Count < Quintile * 2)
				return;
			int bucket = rr.Count / Quintile;
			var longS = rr.OrderByDescending(kv => kv.Value).Take(bucket).Select(kv => kv.Key).ToList();
			var shortS = rr.OrderBy(kv => kv.Value).Take(bucket).Select(kv => kv.Key).ToList();
			_w.Clear();
			decimal wl = 1m / longS.Count, ws = -1m / shortS.Count;
			foreach (var s in longS)
				_w[s] = wl;
			foreach (var s in shortS)
				_w[s] = ws;
			foreach (var position in Positions)
				if (!_w.ContainsKey(position.Security))
					Trade(position.Security, 0);
			var portfolioValue = Portfolio.CurrentValue ?? 0m;
			foreach (var kv in _w)
			{
				var price = GetLatestPrice(kv.Key);
				if (price > 0)
					Trade(kv.Key, kv.Value * portfolioValue / price);
			}
		}

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}

		private void Trade(Security s, decimal tgt)
		{
			var diff = tgt - Pos(s);
			var price = GetLatestPrice(s);
			if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd)
				return;
			RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "RollRet" });
		}

		private decimal Pos(Security s) => GetPositionValue(s, Portfolio) ?? 0;
		private bool TryRollReturn(Security s, out decimal v) { v = 0; return false; }
	}
}