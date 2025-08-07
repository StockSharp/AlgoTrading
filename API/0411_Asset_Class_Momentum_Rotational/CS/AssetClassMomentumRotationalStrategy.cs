using System;
using System.Collections.Generic;
using System.Linq;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Momentum rotation strategy across asset classes.
	/// Calculates rate of change and rebalances on the first trading day of each month.
	/// </summary>
	public class AssetClassMomentumRotationalStrategy : Strategy
	{
		private readonly StrategyParam<IEnumerable<Security>> _universe;
		private readonly StrategyParam<int> _rocLen;
		private readonly StrategyParam<int> _topN;
		private readonly StrategyParam<decimal> _minUsd;
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
		/// Rate of change lookback length.
		/// </summary>
		public int RocLength
		{
			get => _rocLen.Value;
			set => _rocLen.Value = value;
		}

		/// <summary>
		/// Number of top assets to hold.
		/// </summary>
		public int TopN
		{
			get => _topN.Value;
			set => _topN.Value = value;
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
		/// Candle type used to compute momentum.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		private readonly Dictionary<Security, RateOfChange> _roc = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private readonly HashSet<Security> _held = new();
		private DateTime _lastDay = DateTime.MinValue;

		/// <summary>
		/// Initializes a new instance of <see cref="AssetClassMomentumRotationalStrategy"/>.
		/// </summary>
		public AssetClassMomentumRotationalStrategy()
		{
			_universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>())
				.SetDisplay("Universe", "Securities to trade", "General");

			_rocLen = Param(nameof(RocLength), 252)
				.SetGreaterThanZero()
				.SetDisplay("ROC Length", "Rate of change lookback", "General");

			_topN = Param(nameof(TopN), 3)
				.SetGreaterThanZero()
				.SetDisplay("Top N", "Number of assets to hold", "General");

			_minUsd = Param(nameof(MinTradeUsd), 50m)
				.SetGreaterThanZero()
				.SetDisplay("Min Trade USD", "Minimum trade value", "General");

			_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Candle Type", "Candle type used for momentum", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() =>
			Universe.Select(s => (s, CandleType));

		protected override void OnReseted()
		{
			base.OnReseted();

			_roc.Clear();
			_latestPrices.Clear();
			_held.Clear();
			_lastDay = default;
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			if (Universe == null || !Universe.Any())
				throw new InvalidOperationException("Universe cannot be empty.");

			base.OnStarted(time);
			foreach (var (sec, dt) in GetWorkingSecurities())
			{
				var win = new RateOfChange { Length = RocLength };
				_roc[sec] = win;

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

			// Process the candle through the indicator
			var win = _roc[security];
			win.Process(candle);

			var d = candle.OpenTime.Date;
			if (d == _lastDay)
				return;
			_lastDay = d;
			
			if (d.Day == 1)
				TryRebalance();
		}

		private void TryRebalance()
		{
			var ready = _roc.Where(kv => kv.Value.IsFormed)
							.ToDictionary(kv => kv.Key, kv => kv.Value.GetCurrentValue<decimal>());
			if (ready.Count < TopN)
				return;

			var selected = ready.OrderByDescending(kv => kv.Value).Take(TopN).Select(kv => kv.Key).ToHashSet();

			foreach (var sec in _held.Where(h => !selected.Contains(h)).ToList())
				Move(sec, 0);

			var portfolioValue = Portfolio.CurrentValue ?? 0m;
			decimal capEach = portfolioValue / TopN;
			foreach (var sec in selected)
			{
				var price = GetLatestPrice(sec);
				if (price > 0)
					Move(sec, capEach / price);
			}

			_held.Clear();
			_held.UnionWith(selected);
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
				Comment = "ACMomentum"
			});
		}

		private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;
	}
}