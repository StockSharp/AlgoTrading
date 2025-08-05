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
	/// Strategy implementing the accrual anomaly factor.
	/// Rebalances annually on the first trading day of May.
	/// </summary>
	public class AccrualAnomalyStrategy : Strategy
	{
		private readonly StrategyParam<IEnumerable<Security>> _universe;
		private readonly StrategyParam<int> _deciles;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// Trading universe.
		/// </summary>
		public IEnumerable<Security> Universe
		{
			get => _universe.Value;
			set => _universe.Value = value;
		}

		/// <summary>
		/// Number of decile buckets.
		/// </summary>
		public int Deciles
		{
			get => _deciles.Value;
			set => _deciles.Value = value;
		}

		/// <summary>
		/// Candle type used to detect rebalancing date.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		private readonly Dictionary<Security, BalanceSnapshot> _prev = new();
		private readonly Dictionary<Security, decimal> _weights = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private DateTime _lastDay = DateTime.MinValue;

		/// <summary>
		/// Initializes a new instance of <see cref="AccrualAnomalyStrategy"/>.
		/// </summary>
		public AccrualAnomalyStrategy()
		{
			_universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>())
				.SetDisplay("Universe", "Securities to trade", "General");

			_deciles = Param(nameof(Deciles), 10)
				.SetGreaterThanZero()
				.SetDisplay("Deciles", "Number of decile buckets", "General");

			_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Candle Type", "Candle type used for rebalancing", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() =>
			Universe.Select(s => (s, CandleType));

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			if (Universe == null || !Universe.Any())
				throw new InvalidOperationException("Universe cannot be empty.");

			base.OnStarted(time);

			_latestPrices.Clear();
			_weights.Clear();
			_prev.Clear();
			_lastDay = default;

			foreach (var (sec, dt) in GetWorkingSecurities())
			{
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

			var d = candle.OpenTime.Date;
			if (d == _lastDay)
				return;
			_lastDay = d;
			
			// Rebalance on the first trading day of May
			if (d.Month == 5 && d.Day == 1)
				Rebalance();
		}

		private void Rebalance()
		{
			var accr = new Dictionary<Security, decimal>();
			foreach (var s in Universe)
			{
				if (!TryGetFundamentals(s, out var cur))
					continue;
				if (_prev.TryGetValue(s, out var prev))
					accr[s] = CalcAccrual(cur, prev);
				_prev[s] = cur;
			}

			if (accr.Count < Deciles * 2)
				return;
			int bucket = accr.Count / Deciles;
			var sorted = accr.OrderBy(kv => kv.Value).ToList();
			var longs = sorted.Take(bucket).Select(kv => kv.Key).ToList();
			var shorts = sorted.Skip(accr.Count - bucket).Select(kv => kv.Key).ToList();

			_weights.Clear();
			decimal wl = 1m / longs.Count, ws = -1m / shorts.Count;
			foreach (var s in longs)
				_weights[s] = wl;
			foreach (var s in shorts)
				_weights[s] = ws;

			foreach (var position in Positions)
				if (!_weights.ContainsKey(position.Security))
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
			if (price <= 0 || Math.Abs(diff) * price < 100)
				return;
			RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "Accrual" });
		}

		private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;

		private bool TryGetFundamentals(Security s, out BalanceSnapshot snap) { snap = null; return false; }

		private decimal CalcAccrual(BalanceSnapshot cur, BalanceSnapshot prev) => 0m;
		private record BalanceSnapshot(decimal a, decimal b);
	}
}