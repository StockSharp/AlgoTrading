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
	/// Asset Growth Effect strategy.
	/// Rebalances annually in July based on total asset growth.
	/// </summary>
	public class AssetGrowthEffectStrategy : Strategy
	{
		private readonly StrategyParam<IEnumerable<Security>> _universe;
		private readonly StrategyParam<int> _quant;
		private readonly StrategyParam<decimal> _lev;
		private readonly StrategyParam<decimal> _minUsd;
		private readonly StrategyParam<DataType> _candleType;

		private readonly Dictionary<Security, decimal> _prev = new();
		private readonly Dictionary<Security, decimal> _w = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();

		private DateTime _lastDay = DateTime.MinValue;

		/// <summary>
		/// Securities universe to trade.
		/// </summary>
		public IEnumerable<Security> Universe
		{
			get => _universe.Value;
			set => _universe.Value = value;
		}

		/// <summary>
		/// Number of quantiles used to rank securities.
		/// </summary>
		public int Quantiles
		{
			get => _quant.Value;
			set => _quant.Value = value;
		}

		/// <summary>
		/// Target portfolio leverage.
		/// </summary>
		public decimal Leverage
		{
			get => _lev.Value;
			set => _lev.Value = value;
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
		/// Candle type used for calculations.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AssetGrowthEffectStrategy"/> class.
		/// </summary>
		public AssetGrowthEffectStrategy()
		{
			// Securities universe parameter.
			_universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>())
				.SetDisplay("Universe", "Securities universe to trade", "General");

			// Number of quantiles parameter.
			_quant = Param(nameof(Quantiles), 10)
				.SetDisplay("Quantiles", "Number of growth quantiles", "General");

			// Portfolio leverage parameter.
			_lev = Param(nameof(Leverage), 1m)
				.SetDisplay("Leverage", "Target portfolio leverage", "General");

			// Minimum trade USD parameter.
			_minUsd = Param(nameof(MinTradeUsd), 50m)
				.SetDisplay("Min Trade USD", "Minimum trade value in USD", "General");

			// Candle type parameter.
			_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Candle Type", "Candle type for calculations", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return Universe.Select(s => (s, CandleType));
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			if (Universe == null || !Universe.Any())
				throw new InvalidOperationException("Universe cannot be empty.");

			_prev.Clear();
			_w.Clear();

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

			if (d.Month == 7 && d.Day == 1)
				Rebalance();
		}

		private void Rebalance()
		{
			var growth = new Dictionary<Security, decimal>();

			foreach (var s in Universe)
			{
				if (!TryGetTotalAssets(s, out var tot))
					continue;

				if (_prev.TryGetValue(s, out var prev) && prev > 0)
					growth[s] = (tot - prev) / prev;

				_prev[s] = tot;
			}

			if (growth.Count < Quantiles * 2)
				return;

			var qlen = growth.Count / Quantiles;
			var sorted = growth.OrderBy(kv => kv.Value).ToList();
			var longs = sorted.Take(qlen).Select(kv => kv.Key).ToList();
			var shorts = sorted.Skip(growth.Count - qlen).Select(kv => kv.Key).ToList();

			_w.Clear();

			var wl = Leverage / longs.Count;
			var ws = -Leverage / shorts.Count;

			foreach (var s in longs)
				_w[s] = wl;

			foreach (var s in shorts)
				_w[s] = ws;

			foreach (var position in Positions)
			{
				if (!_w.ContainsKey(position.Security))
					Move(position.Security, 0);
			}

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
				Comment = "AssetGrowth"
			});
		}

		private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;

		private bool TryGetTotalAssets(Security s, out decimal tot)
		{
			tot = 0;
			return false;
		}
	}
}
