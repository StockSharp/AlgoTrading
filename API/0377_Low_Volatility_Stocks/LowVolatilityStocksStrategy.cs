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
		/// Long the lowest-volatility decile of stocks and short the highest-volatility decile.
		/// Volatility is measured as the standard deviation of daily returns over the specified window.
		/// Rebalanced on the first trading day of each month.
		/// </summary>
		public class LowVolatilityStocksStrategy : Strategy
		{
				#region Params
				private readonly StrategyParam<IEnumerable<Security>> _universe;
				private readonly StrategyParam<int> _window;
				private readonly StrategyParam<int> _deciles;
				private readonly StrategyParam<decimal> _minUsd;
				private readonly StrategyParam<DataType> _tf;

				/// <summary>
				/// Securities universe.
				/// </summary>
				public IEnumerable<Security> Universe
				{
						get => _universe.Value;
						set => _universe.Value = value;
				}

				/// <summary>
				/// Number of days in the volatility window.
				/// </summary>
				public int VolWindowDays
				{
						get => _window.Value;
						set => _window.Value = value;
				}

				/// <summary>
				/// Number of deciles to split the universe by volatility.
				/// </summary>
				public int Deciles
				{
						get => _deciles.Value;
						set => _deciles.Value = value;
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
				/// Candle type used for analysis.
				/// </summary>
				public DataType CandleType
				{
						get => _tf.Value;
						set => _tf.Value = value;
				}
				#endregion

				private readonly Dictionary<Security, RollingWin> _ret = new();
				private readonly Dictionary<Security, decimal> _w = new();
				private readonly Dictionary<Security, decimal> _latestPrices = new();
				private DateTime _lastDay = DateTime.MinValue;

				/// <summary>
				/// Initializes a new instance of <see cref="LowVolatilityStocksStrategy"/>.
				/// </summary>
				public LowVolatilityStocksStrategy()
				{
						_universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>())
								.SetDisplay("Universe", "Securities to trade", "General");

						_window = Param(nameof(VolWindowDays), 60)
								.SetGreaterThanZero()
								.SetDisplay("Vol window", "Days in volatility window", "Parameters");

						_deciles = Param(nameof(Deciles), 10)
								.SetGreaterThanZero()
								.SetDisplay("Deciles", "Number of deciles", "Parameters");

						_minUsd = Param(nameof(MinTradeUsd), 200m)
								.SetGreaterThanZero()
								.SetDisplay("Min Trade USD", "Minimum order value in USD", "Parameters");

						_tf = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
								.SetDisplay("Candle Type", "Time frame for candles", "General");
				}

				/// <inheritdoc />
				public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
				{
						return Universe.Select(s => (s, CandleType));
				}

				/// <inheritdoc />
				protected override void OnStarted(DateTimeOffset t)
				{
						base.OnStarted(t);

						if (Universe == null || !Universe.Any())
								throw new InvalidOperationException("Universe is empty.");

						foreach (var (s, tf) in GetWorkingSecurities())
						{
								_ret[s] = new RollingWin(VolWindowDays + 1);
								SubscribeCandles(tf, true, s)
										.Bind(c => ProcessCandle(c, s))
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
			_ret[s].Add(c.ClosePrice);
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
			var vol = new Dictionary<Security, decimal>();
			foreach (var kv in _ret)
			{
				if (!kv.Value.Full)
					continue;
				var r = kv.Value.ReturnSeries();
				var v = (decimal)Math.Sqrt(r.Select(x => (double)x * (double)x).Average());
				vol[kv.Key] = v;
			}

			if (vol.Count < Deciles * 2)
				return;
			int bucket = vol.Count / Deciles;
			var lowVol = vol.OrderBy(kv => kv.Value).Take(bucket).Select(kv => kv.Key).ToList();
			var highVol = vol.OrderByDescending(kv => kv.Value).Take(bucket).Select(kv => kv.Key).ToList();

			_w.Clear();
			decimal wl = 1m / lowVol.Count;
			decimal ws = -1m / highVol.Count;
			foreach (var s in lowVol)
				_w[s] = wl;
			foreach (var s in highVol)
				_w[s] = ws;

			foreach (var position in Positions)
				if (!_w.ContainsKey(position.Security))
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
				Comment = "LowVol"
			});
		}
		private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;

		private class RollingWin
		{
			private readonly Queue<decimal> _q = new(); private readonly int _n;
			public RollingWin(int n) { _n = n; }
			public bool Full => _q.Count == _n;
			public void Add(decimal px)
			{
				if (_q.Count == _n)
					_q.Dequeue();
				_q.Enqueue(px);
			}
			public decimal[] ReturnSeries()
			{
				var arr = _q.ToArray();
				var res = new decimal[arr.Length - 1];
				for (int i = 1; i < arr.Length; i++)
					res[i - 1] = (arr[i] - arr[i - 1]) / arr[i - 1];
				return res;
			}
		}
	}
}
