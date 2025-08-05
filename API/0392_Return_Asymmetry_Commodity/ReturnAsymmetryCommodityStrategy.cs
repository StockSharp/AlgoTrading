// ReturnAsymmetryCommodityStrategy.cs
// -----------------------------------------------------------------------------
// Uses return asymmetry over past WindowDays for each commodity future:
// asym = Σ positive returns / |Σ negative returns|.
// Monthly (first trading day) long TopN asymmetry, short BottomN.
// Trigger via daily candle of first future (no Schedule).
// External data: none beyond prices.
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
	/// Strategy that ranks commodity futures by return asymmetry and trades the top
	/// and bottom groups each month.
	/// </summary>
	public class ReturnAsymmetryCommodityStrategy : Strategy
	{
		#region Params

		private readonly StrategyParam<IEnumerable<Security>> _futs;
		private readonly StrategyParam<int> _window;
		private readonly StrategyParam<int> _top;
		private readonly StrategyParam<decimal> _minUsd;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// Commodity futures to trade.
		/// </summary>
		public IEnumerable<Security> Futures
		{
			get => _futs.Value;
			set => _futs.Value = value;
		}

		/// <summary>
		/// Lookback window in days.
		/// </summary>
		public int WindowDays
		{
			get => _window.Value;
			set => _window.Value = value;
		}

		/// <summary>
		/// Number of instruments to long/short.
		/// </summary>
		public int TopN
		{
			get => _top.Value;
			set => _top.Value = value;
		}

		/// <summary>
		/// Minimum dollar value per trade.
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

		private class Win
		{
			public Queue<decimal> Px = new();
		}

		private readonly Dictionary<Security, Win> _map = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private DateTime _lastDay = DateTime.MinValue;
		private readonly Dictionary<Security, decimal> _w = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="ReturnAsymmetryCommodityStrategy"/> class.
		/// </summary>
		public ReturnAsymmetryCommodityStrategy()
		{
			_futs = Param<IEnumerable<Security>>(nameof(Futures), Array.Empty<Security>())
				.SetDisplay("Futures", "Commodity futures to trade", "General");

			_window = Param(nameof(WindowDays), 120)
				.SetDisplay("Window", "Lookback window in days", "General");

			_top = Param(nameof(TopN), 5)
				.SetDisplay("Top N", "Number of instruments to long/short", "General");

			_minUsd = Param(nameof(MinTradeUsd), 200m)
				.SetDisplay("Min Trade USD", "Minimum dollar value per trade", "General");
			_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return Futures.Select(s => (s, CandleType));
		}

		/// <inheritdoc />
		
		protected override void OnReseted()
		{
			base.OnReseted();

			_map.Clear();
			_latestPrices.Clear();
			_lastDay = default;
			_w.Clear();
		}

		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			if (Futures == null || !Futures.Any())
				throw new InvalidOperationException("Futures cannot be empty.");

			foreach (var (sec, dt) in GetWorkingSecurities())
			{
				_map[sec] = new Win();
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

			OnDaily(security, candle);
		}

		private void OnDaily(Security s, ICandleMessage c)
		{
			var q = _map[s].Px;
			if (q.Count == WindowDays)
				q.Dequeue();
			q.Enqueue(c.ClosePrice);

			var d = c.OpenTime.Date;
			if (d == _lastDay)
				return;
			_lastDay = d;

			if (d.Day != 1)
				return;

			Rebalance();
		}

		private void Rebalance()
		{
			var asym = new Dictionary<Security, decimal>();
			foreach (var kv in _map)
			{
				var q = kv.Value.Px;
				if (q.Count < WindowDays)
					continue;
				var arr = q.ToArray();
				decimal pos = 0, neg = 0;
				for (int i = 1; i < arr.Length; i++)
				{
					var r = (arr[i] - arr[i - 1]) / arr[i - 1];
					if (r > 0)
						pos += r;
					else
						neg += r;
				}
				if (neg == 0)
					continue;
				asym[kv.Key] = pos / Math.Abs(neg);
			}

			if (asym.Count < TopN * 2)
				return;

			var longs = asym.OrderByDescending(kv => kv.Value).Take(TopN).Select(kv => kv.Key).ToList();
			var shorts = asym.OrderBy(kv => kv.Value).Take(TopN).Select(kv => kv.Key).ToList();

			_w.Clear();
			decimal wl = 1m / longs.Count;
			decimal ws = -1m / shorts.Count;

			foreach (var s in longs)
				_w[s] = wl;

			foreach (var s in shorts)
				_w[s] = ws;

			foreach (var position in Positions.Where(pos => !_w.ContainsKey(pos.Security)))
				Move(position.Security, 0);

			var portfolioValue = Portfolio.CurrentValue ?? 0m;

			foreach (var kv in _w)
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

		private void Move(Security s, decimal tgtQty)
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
				Comment = "AsymCom",
			});
		}

		private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;
	}
}
