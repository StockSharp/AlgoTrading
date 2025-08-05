// PairsTradingStocksStrategy.cs
// -----------------------------------------------------------------------------
// Simplified top‑N pairs trading among given list of stock pairs.
// Uses rolling 60‑day price ratio; enter when z>|EntryZ|, exit when |z|<ExitZ.
// Triggered by daily candles. No Schedule().
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
	public class PairsTradingStocksStrategy : Strategy
	{
		private readonly StrategyParam<IEnumerable<(Security, Security)>> _pairs;
		private readonly StrategyParam<int> _window;
		private readonly StrategyParam<decimal> _entryZ;
		private readonly StrategyParam<decimal> _exitZ;
		private readonly StrategyParam<decimal> _minUsd;
		private readonly DataType _tf = TimeSpan.FromDays(1).TimeFrame();

		private class Win { public Queue<decimal> R = new(); }
		private readonly Dictionary<(Security, Security), Win> _hist = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();

		public IEnumerable<(Security A, Security B)> Pairs { get => _pairs.Value; set => _pairs.Value = value; }
		public int WindowDays => _window.Value;
		public decimal EntryZ => _entryZ.Value;
		public decimal ExitZ => _exitZ.Value;
		public decimal MinTradeUsd => _minUsd.Value;

		public PairsTradingStocksStrategy()
		{
			_pairs = Param<IEnumerable<(Security, Security)>>(nameof(Pairs), Array.Empty<(Security, Security)>());
			_window = Param(nameof(WindowDays), 60);
			_entryZ = Param(nameof(EntryZ), 2m);
			_exitZ = Param(nameof(ExitZ), 0.5m);
			_minUsd = Param(nameof(MinTradeUsd), 200m);
		}

		public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
		{
			foreach (var (a, b) in Pairs)
			{ yield return (a, _tf); yield return (b, _tf); }
		}

		protected override void OnStarted(DateTimeOffset t)
		{
			base.OnStarted(t);
			foreach (var (a, b) in Pairs)
			{
				_hist[(a, b)] = new Win();
				SubscribeCandles(_tf, true, a).Bind(c => ProcessCandle(c, a)).Start();
				SubscribeCandles(_tf, true, b).Bind(c => ProcessCandle(c, b)).Start();
			}
		}

		private void ProcessCandle(ICandleMessage candle, Security security)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Store the latest closing price for this security
			_latestPrices[security] = candle.ClosePrice;

			OnDaily();
		}

		private void OnDaily()
		{
			foreach (var pair in Pairs)
			{
				var (a, b) = pair;
				var priceA = GetLatestPrice(a);
				var priceB = GetLatestPrice(b);
				if (priceA == 0 || priceB == 0)
					continue;
				var r = priceA / priceB;
				var w = _hist[pair].R;
				if (w.Count == WindowDays)
					w.Dequeue();
				w.Enqueue(r);
				if (w.Count < WindowDays)
					continue;

				var mean = w.Average();
				var sigma = (decimal)Math.Sqrt(w.Select(x => (double)((x - mean) * (x - mean))).Average());
				if (sigma == 0)
					continue;
				var z = (r - mean) / sigma;

				if (Math.Abs(z) < ExitZ)
				{ Move(a, 0); Move(b, 0); continue; }

				var portfolioValue = Portfolio.CurrentValue ?? 0m;
				var notional = portfolioValue / 2;
				if (z > EntryZ) // A overpriced
				{
					Move(a, -notional / priceA);
					Move(b, notional / priceB);
				}
				else if (z < -EntryZ) // A underpriced
				{
					Move(a, notional / priceA);
					Move(b, -notional / priceB);
				}
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
			RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "Pairs" });
		}
		private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;
	}
}
