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
	/// Currency momentum factor strategy.
	/// Long top-K momentum currencies, short bottom-K; monthly rebalance.
	/// </summary>
	public class CurrencyMomentumFactorStrategy : Strategy
	{
		private readonly StrategyParam<IEnumerable<Security>> _universe;
		private readonly StrategyParam<int> _lookback;
		private readonly StrategyParam<int> _k;
		private readonly StrategyParam<DataType> _tf;
		private readonly StrategyParam<decimal> _minUsd;

		/// <summary>
		/// Universe of currencies to trade.
		/// </summary>
		public IEnumerable<Security> Universe
		{
			get => _universe.Value;
			set => _universe.Value = value;
		}

		/// <summary>
		/// Lookback period for momentum calculation.
		/// </summary>
		public int Lookback
		{
			get => _lookback.Value;
			set => _lookback.Value = value;
		}

		/// <summary>
		/// Number of currencies to long and short.
		/// </summary>
		public int K
		{
			get => _k.Value;
			set => _k.Value = value;
		}

		/// <summary>
		/// Candle type used for calculations.
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

		private readonly Dictionary<Security, RollingWindow<decimal>> _wins = new();
		private readonly Dictionary<Security, decimal> _w = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private DateTime _lastDay = DateTime.MinValue;

		/// <summary>
		/// Constructor.
		/// </summary>
		public CurrencyMomentumFactorStrategy()
		{
			_universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>())
				.SetDisplay("Universe", "Securities to trade", "General");

			_lookback = Param(nameof(Lookback), 252)
				.SetGreaterThanZero()
				.SetDisplay("Lookback", "Momentum lookback period", "Parameters");

			_k = Param(nameof(K), 3)
				.SetGreaterThanZero()
				.SetDisplay("Top/Bottom K", "Number of currencies long/short", "Parameters");

			_tf = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Candle Type", "Timeframe for candles", "General");

			_minUsd = Param(nameof(MinTradeUsd), 100m)
				.SetGreaterThanZero()
				.SetDisplay("Min Trade USD", "Minimum trade value in USD", "Risk Management");
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
				throw new InvalidOperationException("Universe must not be empty.");

			foreach (var (security, dt) in GetWorkingSecurities())
			{
				_wins[security] = new RollingWindow<decimal>(Lookback + 1);

				SubscribeCandles(dt, true, security)
					.Bind(candle => ProcessCandle(candle, security))
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

			HandleDaily(candle, security);
		}

		private void HandleDaily(ICandleMessage candle, Security security)
		{
			_wins[security].Add(candle.ClosePrice);

			var day = candle.OpenTime.Date;
			if (day == _lastDay)
				return;

			_lastDay = day;

			if (day.Day == 1)
				Rebalance();
		}

		private void Rebalance()
		{
			if (_wins.Values.Any(w => !w.IsFull()))
				return;

			var mom = _wins.ToDictionary(kv => kv.Key, kv => (kv.Value.Last() - kv.Value[0]) / kv.Value[0]);
			var top = mom.OrderByDescending(kv => kv.Value).Take(K).Select(kv => kv.Key).ToList();
			var bot = mom.OrderBy(kv => kv.Value).Take(K).Select(kv => kv.Key).ToList();

			_w.Clear();

			var wl = 1m / top.Count;
			var ws = -1m / bot.Count;

			foreach (var s in top)
				_w[s] = wl;

			foreach (var s in bot)
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

		private void Move(Security security, decimal target)
		{
			var diff = target - PositionBy(security);
			var price = GetLatestPrice(security);

			if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd)
				return;

			RegisterOrder(new Order
			{
				Security = security,
				Portfolio = Portfolio,
				Side = diff > 0 ? Sides.Buy : Sides.Sell,
				Volume = Math.Abs(diff),
				Type = OrderTypes.Market,
				Comment = "CurrMom"
			});
		}

		private decimal PositionBy(Security security) => GetPositionValue(security, Portfolio) ?? 0;

		#region RollingWindow

		private class RollingWindow<T>
		{
			private readonly Queue<T> _q = new();
			private readonly int _n;

			public RollingWindow(int n)
			{
				_n = n;
			}

			public void Add(T value)
			{
				if (_q.Count == _n)
					_q.Dequeue();

				_q.Enqueue(value);
			}

			public bool IsFull() => _q.Count == _n;

			public T Last() => _q.Last();

			public T this[int index] => _q.ElementAt(index);
		}

		#endregion
	}
}

