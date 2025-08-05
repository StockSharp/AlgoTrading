// PairsTradingCountryETFsStrategy.cs
// -----------------------------------------------------------------------------
// Mean‑reversion pairs trade between two country ETFs.
// • Universe must contain exactly two ETFs {"A","B"}.
// • Calculate ratio = PriceA / PriceB.
// • Rolling window (WindowDays) for mean/std; compute z‑score.
// • Enter long A / short B when z < -EntryZ; opposite when z > EntryZ.
// • Exit when |z| < ExitZ.
// • All events triggered by each day's closed candle (no Schedule).
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
	/// Mean-reversion pairs strategy for country ETFs.
	/// Trades two ETFs based on the z-score of their price ratio.
	/// </summary>
	public class PairsTradingCountryETFsStrategy : Strategy
	{
		private readonly StrategyParam<IEnumerable<Security>> _univ;
		private readonly StrategyParam<int> _window;
		private readonly StrategyParam<decimal> _entryZ;
		private readonly StrategyParam<decimal> _exitZ;
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

		/// <summary>
		/// Strategy universe containing exactly two ETFs.
		/// </summary>
		public IEnumerable<Security> Universe
		{
			get => _univ.Value;
			set => _univ.Value = value;
		}

		/// <summary>
		/// Rolling window size in days.
		/// </summary>
		public int WindowDays
		{
			get => _window.Value;
			set => _window.Value = value;
		}

		/// <summary>
		/// Entry z-score threshold.
		/// </summary>
		public decimal EntryZ
		{
			get => _entryZ.Value;
			set => _entryZ.Value = value;
		}

		/// <summary>
		/// Exit z-score threshold.
		/// </summary>
		public decimal ExitZ
		{
			get => _exitZ.Value;
			set => _exitZ.Value = value;
		}

		/// <summary>
		/// Minimum trade value in USD.
		/// </summary>
		public decimal MinTradeUsd
		{
			get => _minUsd.Value;
			set => _minUsd.Value = value;
		}

		private Security _a, _b;
		private readonly Queue<decimal> _ratio = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private DateTime _last = DateTime.MinValue;

		public PairsTradingCountryETFsStrategy()
		{
			_univ = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>())
			.SetDisplay("Universe", "Pair of ETFs", "General");
			_window = Param(nameof(WindowDays), 60)
			.SetGreaterThanZero()
			.SetDisplay("Window Days", "Rolling window size in days", "General");
			_entryZ = Param(nameof(EntryZ), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Entry Z", "Entry z-score threshold", "General");
			_exitZ = Param(nameof(ExitZ), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Exit Z", "Exit z-score threshold", "General");
			_minUsd = Param(nameof(MinTradeUsd), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Min Trade USD", "Minimum trade value in USD", "General");
			_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
		}

		public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
		{
			if (Universe.Count() != 2)
				throw new InvalidOperationException("Universe must contain exactly two ETFs.");
			_a = Universe.ElementAt(0);
			_b = Universe.ElementAt(1);
			yield return (_a, CandleType);
			yield return (_b, CandleType);
		}

		
		protected override void OnReseted()
		{
			base.OnReseted();

			_a = _b = null;
			_ratio.Clear();
			_latestPrices.Clear();
			_last = default;
		}

		protected override void OnStarted(DateTimeOffset t)
		{
			if (Universe == null || Universe.Count() != 2)
				throw new InvalidOperationException("Universe must contain exactly two ETFs.");
			base.OnStarted(t);
			SubscribeCandles(CandleType, true, _a).Bind(c => ProcessCandle(c, _a)).Start();
			SubscribeCandles(CandleType, true, _b).Bind(c => ProcessCandle(c, _b)).Start();
		}

		private void ProcessCandle(ICandleMessage candle, Security security)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Store the latest closing price for this security
			_latestPrices[security] = candle.ClosePrice;

			var d = candle.OpenTime.Date;
			if (d == _last)
				return;
			_last = d;

			OnDaily();
		}

		private void OnDaily()
		{
			var pxA = GetLatestPrice(_a);
			var pxB = GetLatestPrice(_b);
			if (pxA == 0 || pxB == 0)
				return;

			var r = pxA / pxB;
			if (_ratio.Count == WindowDays)
				_ratio.Dequeue();
			_ratio.Enqueue(r);
			if (_ratio.Count < WindowDays)
				return;

			var mean = _ratio.Average();
			var sigma = (decimal)Math.Sqrt(_ratio.Select(x => Math.Pow((double)(x - mean), 2)).Average());
			if (sigma == 0)
				return;
			var z = (r - mean) / sigma;

			if (Math.Abs(z) < ExitZ)
			{
				Move(_a, 0);
				Move(_b, 0);
				return;
			}

			if (z > EntryZ)
			{
				// short A, long B
				Hedge(-1);
			}
			else if (z < -EntryZ)
			{
				// long A, short B
				Hedge(1);
			}
		}

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}

		private void Hedge(int dir) // dir = +1  => long A short B
		{
			var equity = Portfolio.CurrentValue ?? 0m;
			var priceA = GetLatestPrice(_a);
			var priceB = GetLatestPrice(_b);
			
			if (priceA <= 0 || priceB <= 0)
				return;

			var qty = equity / 2 / priceA;
			var qtyB = equity / 2 / priceB;
			Move(_a, dir * qty);
			Move(_b, -dir * qtyB);
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
				Comment = "PairsETF"
			});
		}
		private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;
	}
}
