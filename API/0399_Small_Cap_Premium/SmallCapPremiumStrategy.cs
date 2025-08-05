// SmallCapPremiumStrategy.cs
// -----------------------------------------------------------------------------
// SMB long-short: long lowest-capitalisation quintile, short highest quintile.
// Needs external market-cap feed in TryGetMarketCap().
// Rebalanced monthly via candle stream (first trading day).
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
	/// Small-cap premium strategy.
	/// Longs small-cap stocks and shorts large-cap stocks.
	/// </summary>
	public class SmallCapPremiumStrategy : Strategy
	{
		#region Params
		private readonly StrategyParam<IEnumerable<Security>> _universe;
		private readonly StrategyParam<int> _quint;
		private readonly StrategyParam<decimal> _minUsd;
		private readonly DataType _tf = TimeSpan.FromDays(1).TimeFrame();

		/// <summary>
		/// Universe of stocks to rank by market capitalization.
		/// </summary>
		public IEnumerable<Security> Universe
		{
			get => _universe.Value;
			set => _universe.Value = value;
		}

		/// <summary>
		/// Number of buckets to split universe into.
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
		#endregion

		private readonly Dictionary<Security, decimal> _weights = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private DateTime _lastDay = DateTime.MinValue;

		/// <summary>
		/// Initializes strategy parameters.
		/// </summary>
		public SmallCapPremiumStrategy()
		{
			_universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>())
				.SetDisplay("Universe", "Stocks to rank", "General");

			_quint = Param(nameof(Quintile), 5)
				.SetGreaterThanZero()
				.SetDisplay("Quintiles", "Number of buckets", "Parameters");

			_minUsd = Param(nameof(MinTradeUsd), 200m)
				.SetGreaterThanZero()
				.SetDisplay("Min Trade USD", "Minimum trade value in USD", "Parameters");
		}

		public override IEnumerable<(Security, DataType)> GetWorkingSecurities() =>
			Universe.Select(s => (s, _tf));

		protected override void OnStarted(DateTimeOffset t)
		{
			base.OnStarted(t);

			if (Universe == null || !Universe.Any())
				throw new InvalidOperationException("Universe is empty");

			var trigger = Universe.First();

			SubscribeCandles(_tf, true, trigger)
				.Bind(c => ProcessCandle(c, trigger))
				.Start();
		}

		private void ProcessCandle(ICandleMessage candle, Security security)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Store the latest closing price for this security
			_latestPrices[security] = candle.ClosePrice;

			var d = candle.OpenTime.Date;
			if (d == _lastDay)
				return;
			_lastDay = d;

			if (d.Day == 1)
				Rebalance();
		}

		private void Rebalance()
		{
			var cap = new Dictionary<Security, decimal>();
			foreach (var s in Universe)
				if (TryGetMarketCap(s, out var v))
					cap[s] = v;

			if (cap.Count < Quintile * 2)
				return;

			int bucket = cap.Count / Quintile;
			var small = cap.OrderBy(kv => kv.Value).Take(bucket).Select(kv => kv.Key).ToList();
			var large = cap.OrderByDescending(kv => kv.Value).Take(bucket).Select(kv => kv.Key).ToList();

			_weights.Clear();
			decimal wl = 1m / small.Count;
			decimal ws = -1m / large.Count;
			foreach (var s in small)
				_weights[s] = wl;
			foreach (var s in large)
				_weights[s] = ws;

			foreach (var position in Positions.Where(pos => !_weights.ContainsKey(pos.Security)))
				Move(position.Security, 0);

			var portfolioValue = Portfolio.CurrentValue ?? 0m;
			foreach (var kv in _weights)
			{
				var price = GetLatestPrice(kv.Key);
				if (price > 0)
					Move(kv.Key, kv.Value * portfolioValue / price);
			}
		}

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}

		private void Move(Security s, decimal tgt)
		{
			var diff = tgt - PositionBy(s);
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
				Comment = "SmallCap"
			});
		}

		private decimal PositionBy(Security s) =>
			GetPositionValue(s, Portfolio) ?? 0;

		private bool TryGetMarketCap(Security s, out decimal cap)
		{
			cap = 0;
			return false;   // plug in your data feed
		}
	}
}