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
		/// Combines momentum with the asset-growth effect.
		/// Stocks with the highest asset growth are ranked by momentum and rebalanced monthly.
		/// January is skipped with no positions.
		/// </summary>
		public class MomentumAssetGrowthStrategy : Strategy
		{
				#region Params
				private readonly StrategyParam<IEnumerable<Security>> _univ;
				private readonly StrategyParam<int> _momLook;
				private readonly StrategyParam<int> _skip;
				private readonly StrategyParam<int> _decile;
				private readonly StrategyParam<int> _quint;
				private readonly StrategyParam<decimal> _minUsd;
				private readonly StrategyParam<DataType> _tf;

				/// <summary>
				/// Securities universe.
				/// </summary>
				public IEnumerable<Security> Universe
				{
						get => _univ.Value;
						set => _univ.Value = value;
				}

				/// <summary>
				/// Lookback period for momentum in trading days.
				/// </summary>
				public int MomLook
				{
						get => _momLook.Value;
						set => _momLook.Value = value;
				}

				/// <summary>
				/// Number of months skipped from the most recent data.
				/// </summary>
				public int SkipMonths
				{
						get => _skip.Value;
						set => _skip.Value = value;
				}

				/// <summary>
				/// Decile for selecting high asset-growth stocks.
				/// </summary>
				public int AssetDecile
				{
						get => _decile.Value;
						set => _decile.Value = value;
				}

				/// <summary>
				/// Quintile used for momentum ranking.
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

				/// <summary>
				/// Candle type used for analysis.
				/// </summary>
				public DataType CandleType
				{
						get => _tf.Value;
						set => _tf.Value = value;
				}
				#endregion

				private readonly Dictionary<Security, RollingWin> _px = new();
				private readonly Dictionary<Security, decimal> _w = new();
				private readonly Dictionary<Security, decimal> _latestPrices = new();
				private DateTime _last = DateTime.MinValue;

				/// <summary>
				/// Initializes a new instance of <see cref="MomentumAssetGrowthStrategy"/>.
				/// </summary>
				public MomentumAssetGrowthStrategy()
				{
						_univ = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>())
								.SetDisplay("Universe", "Securities to trade", "General");

						_momLook = Param(nameof(MomLook), 252)
								.SetGreaterThanZero()
								.SetDisplay("Momentum lookback", "Trading days for momentum", "Parameters");

						_skip = Param(nameof(SkipMonths), 1)
								.SetGreaterThanZero()
								.SetDisplay("Skip months", "Months skipped from recent data", "Parameters");

						_decile = Param(nameof(AssetDecile), 10)
								.SetGreaterThanZero()
								.SetDisplay("Asset decile", "Decile for high asset growth", "Parameters");

						_quint = Param(nameof(Quintile), 5)
								.SetGreaterThanZero()
								.SetDisplay("Quintile", "Quintile for momentum ranking", "Parameters");

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
				
		protected override void OnReseted()
		{
			base.OnReseted();

			_px.Clear();
			_w.Clear();
			_latestPrices.Clear();
			_last = default;
		}

		protected override void OnStarted(DateTimeOffset t)
				{
						base.OnStarted(t);

						if (Universe == null || !Universe.Any())
								throw new InvalidOperationException("Universe is empty.");

						foreach (var (s, tf) in GetWorkingSecurities())
						{
								_px[s] = new RollingWin(MomLook + 1);
								SubscribeCandles(tf, true, s)
										.Bind(c => ProcessCandle(c, s))
										.Start();
						}
				}

		private void OnDaily(Security s, ICandleMessage c)
		{
			_px[s].Add(c.ClosePrice);
			_latestPrices[s] = c.ClosePrice;
			var d = c.OpenTime.Date;
			if (d == _last)
				return;
			_last = d;
			if (d.Month == 1)
				return; // skip January
			if (d.Day != 1)
				return;
			Rebalance();
		}

		private void Rebalance()
		{
			var aset = new Dictionary<Security, decimal>();
			foreach (var s in Universe)
				if (TryGetAssetGrowth(s, out var g))
					aset[s] = g;

			if (aset.Count < AssetDecile)
				return;
			int dec = aset.Count / AssetDecile;
			var highAG = aset.OrderByDescending(kv => kv.Value).Take(dec).Select(kv => kv.Key).ToList();

			var mom = new Dictionary<Security, decimal>();
			foreach (var s in highAG)
				if (_px[s].Full)
					mom[s] = (_px[s].Data[SkipMonths * 21] - _px[s].Data[MomLook]) / _px[s].Data[MomLook];

			if (mom.Count < Quintile * 2)
				return;
			int q = mom.Count / Quintile;
			var longs = mom.OrderByDescending(kv => kv.Value).Take(q).Select(kv => kv.Key).ToList();
			var shorts = mom.OrderBy(kv => kv.Value).Take(q).Select(kv => kv.Key).ToList();

			_w.Clear();
			decimal wl = 1m / longs.Count, ws = -1m / shorts.Count;
			foreach (var s in longs)
				_w[s] = wl;
			foreach (var s in shorts)
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

		private void Move(Security s, decimal tgt)
		{
			var diff = tgt - Pos(s);
			var price = GetLatestPrice(s);
			if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd)
				return;
			RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "MomAG" });
		}
		private decimal Pos(Security s) => GetPositionValue(s, Portfolio) ?? 0;
		private bool TryGetAssetGrowth(Security s, out decimal g) { g = 0; return false; }

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
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

		private class RollingWin
		{
			private readonly Queue<decimal> _q = new(); private readonly int _n;
			public RollingWin(int n) { _n = n; }
			public bool Full => _q.Count == _n;
			public decimal[] Data => _q.ToArray();
			public void Add(decimal p) { if (_q.Count == _n) _q.Dequeue(); _q.Enqueue(p); }
		}
	}
}