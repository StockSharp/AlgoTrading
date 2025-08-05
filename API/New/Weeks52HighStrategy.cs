// Weeks52HighStrategy.cs
// 52‑Week High Effect in Stocks — High‑Level API implementation for StockSharp (S#)
// Idea: Each month rank Morningstar industry groups by the cap‑weighted ratio (Price / 52‑week‑high).
// Long the six industries with the highest averages (winners) and short the six with the lowest (losers).
// Equal‑weight stocks within portfolios. Hold each tranche for <HoldingPeriod> months (default 3) so that
// one‑third of the book is rebalanced each month.
// Date: 2 August 2025

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	public class Weeks52HighStrategy : Strategy
	{
		#region Parameters

		private readonly StrategyParam<IEnumerable<Security>> _universe;      // required list
		private readonly StrategyParam<int> _industries;    // winners/losers industries count
		private readonly StrategyParam<int> _holdingMonths; // tranche holding period months
		private readonly StrategyParam<int> _windowDays;    // 52‑week lookback (trading days)

		public IEnumerable<Security> Universe { get => _universe.Value; set => _universe.Value = value; }
		public int IndustriesCount => _industries.Value;
		public int HoldingPeriodMonths => _holdingMonths.Value;
		public int LookbackDays => _windowDays.Value;

		#endregion

		private readonly Dictionary<Security, RollingWindow<decimal>> _priceWin = new();
		private readonly Dictionary<Security, decimal> _cap = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();

		private readonly List<Tranche> _tranches = new();

		public Weeks52HighStrategy()
		{
			_universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>())
				.SetDisplay("Universe", "List of securities (required)", "Universe");

			_industries = Param(nameof(IndustriesCount), 6)
				.SetDisplay("Industries", "Number of winner/loser industries", "Ranking");

			_holdingMonths = Param(nameof(HoldingPeriodMonths), 3)
				.SetDisplay("Holding (m)", "Months each tranche is held", "Tranching");

			_windowDays = Param(nameof(LookbackDays), 252)
				.SetDisplay("Lookback Days", "52‑week window (trading days)", "Data");
		}

		#region Universe & candles

		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			if (Universe == null || !Universe.Any())
				throw new InvalidOperationException("Universe cannot be empty — populate Universe before start.");

			var dt = TimeSpan.FromDays(1).TimeFrame();
			return Universe.Select(s => (s, dt));
		}

		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			foreach (var (sec, dt) in GetWorkingSecurities())
			{
				SubscribeCandles(dt, true, sec)
					.Bind(c => ProcessCandle(c, sec))
					.Start();

				_priceWin[sec] = new RollingWindow<decimal>(LookbackDays);
			}

			LogInfo($"52‑Week High strategy started. Universe={Universe.Count()} securities, Industries={IndustriesCount}");
		}

		private void ProcessCandle(ICandleMessage candle, Security security)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Store the latest closing price for this security
			_latestPrices[security] = candle.ClosePrice;

			OnCandleFinished(candle, security);
		}

		private void OnCandleFinished(ICandleMessage candle, Security sec)
		{
			if (_priceWin.TryGetValue(sec, out var win))
				win.Add(candle.ClosePrice);
		}

		#endregion

		#region Monthly rebalance & tranching

		private void MonthlyRebalance()
		{
			if (_priceWin.Values.Any(w => !w.IsFull()))
			{
				LogInfo("Waiting until all rolling windows are full.");
				return;
			}

			// Group by Morningstar industry
			var groups = new Dictionary<int, List<Security>>();
			foreach (var sec in Universe)
			{
				if (!sec.Attributes.TryGetValue("MorningstarIndustryGroupCode", out var codeObj) || codeObj is not int code || code == 0)
					continue;

				if (!groups.TryGetValue(code, out var list))
				{
					list = new List<Security>();
					groups[code] = list;
				}
				list.Add(sec);
			}

			// Compute cap‑weighted avg of Price / 52‑week‑high for each industry
			var indScores = new List<(int code, decimal score)>();
			foreach (var (code, list) in groups)
			{
				decimal totalCap = 0m;
				decimal weighted = 0m;
				foreach (var sec in list)
				{
					var price = _priceWin[sec].Last();
					var high = _priceWin[sec].Max();
					if (high == 0)
						continue;
					var ratio = price / high;

					var cap = sec.Price * (sec.VolumeStep ?? 1m);
					totalCap += cap;
					weighted += ratio * cap;
				}
				if (totalCap == 0)
					continue;
				indScores.Add((code, weighted / totalCap));
			}

			if (indScores.Count < IndustriesCount * 2)
			{
				LogInfo("Not enough industry groups formed.");
				return;
			}

			var ranked = indScores.OrderByDescending(p => p.score).ToList();
			var top = ranked.Take(IndustriesCount).Select(p => p.code).ToHashSet();
			var bottom = ranked.Skip(ranked.Count - IndustriesCount).Select(p => p.code).ToHashSet();

			var longs = new List<Security>();
			var shorts = new List<Security>();

			foreach (var sec in Universe)
			{
				if (!sec.Attributes.TryGetValue("MorningstarIndustryGroupCode", out var codeObj) || codeObj is not int code || code == 0)
					continue;
				if (top.Contains(code))
					longs.Add(sec);
				else if (bottom.Contains(code))
					shorts.Add(sec);
			}

			CreateTranche(longs, shorts);
			ExecuteTranches();
		}

		private void CreateTranche(List<Security> longs, List<Security> shorts)
		{
			if (!longs.Any() || !shorts.Any())
			{
				LogInfo("Empty long or short list, tranche skipped.");
				return;
			}

			decimal capitalPerLeg = Portfolio.CurrentValue / HoldingPeriodMonths;
			decimal longWeight = capitalPerLeg / longs.Count;
			decimal shortWeight = capitalPerLeg / shorts.Count;

			var orders = new List<(Security sec, decimal qty)>();
			foreach (var sec in longs)
				orders.Add((sec, Math.Floor(longWeight / sec.Price)));
			foreach (var sec in shorts)
				orders.Add((sec, -Math.Floor(shortWeight / sec.Price)));

			_tranches.Add(new Tranche { Orders = orders });
		}

		private void ExecuteTranches()
		{
			var toRemove = new List<Tranche>();
			foreach (var tr in _tranches)
			{
				if (tr.Age == 0)
				{
					foreach (var (sec, qty) in tr.Orders)
						SendOrder(sec, qty);
				}
				tr.Age++;
				if (tr.Age >= HoldingPeriodMonths)
				{
					foreach (var (sec, qty) in tr.Orders)
						SendOrder(sec, -qty);
					toRemove.Add(tr);
				}
			}
			_tranches.RemoveAll(t => toRemove.Contains(t));
		}

		private void SendOrder(Security sec, decimal qty)
		{
			if (qty == 0)
				return;
			RegisterOrder(new Order
			{
				Security = sec,
				Portfolio = Portfolio,
				Side = qty > 0 ? Sides.Buy : Sides.Sell,
				Volume = Math.Abs(qty),
				Type = OrderTypes.Market,
				Comment = "52‑WeekHigh"
			});
		}

		#endregion

		private class Tranche
		{
			public List<(Security sec, decimal qty)> Orders { get; set; }
			public int Age { get; set; } = 0;
		}

		#region RollingWindow helper

		private class RollingWindow<T>
		{
			private readonly Queue<T> _data;
			private readonly int _size;

			public RollingWindow(int size)
			{
				_size = size;
				_data = new Queue<T>(size);
			}

			public void Add(T value)
			{
				if (_data.Count == _size)
					_data.Dequeue();
				_data.Enqueue(value);
			}

			public bool IsFull() => _data.Count == _size;

			public T Last() => _data.Last();
			public T Max() => _data.Max();
		}

		#endregion
	}
}