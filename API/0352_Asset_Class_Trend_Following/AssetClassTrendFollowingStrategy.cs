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
	/// Asset class trend following strategy.
	/// Uses the simple moving average filter and rebalances monthly.
	/// </summary>
	public class AssetClassTrendFollowingStrategy : Strategy
	{
		private readonly StrategyParam<IEnumerable<Security>> _universe;
		private readonly StrategyParam<int> _smaLen;
		private readonly StrategyParam<decimal> _minUsd;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// Universe of securities to trade.
		/// </summary>
		public IEnumerable<Security> Universe
		{
			get => _universe.Value;
			set => _universe.Value = value;
		}

		/// <summary>
		/// Length of the simple moving average filter.
		/// </summary>
		public int SmaLength
		{
			get => _smaLen.Value;
			set => _smaLen.Value = value;
		}

		/// <summary>
		/// Minimum dollar amount for a trade.
		/// </summary>
		public decimal MinTradeUsd
		{
			get => _minUsd.Value;
			set => _minUsd.Value = value;
		}

		/// <summary>
		/// The type of candles to use.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		private readonly Dictionary<Security, SimpleMovingAverage> _sma = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private readonly HashSet<Security> _held = new();
		private DateTime _lastDay = DateTime.MinValue;

		/// <summary>
		/// Initializes a new instance of <see cref="AssetClassTrendFollowingStrategy"/>.
		/// </summary>
		public AssetClassTrendFollowingStrategy()
		{
			_universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>())
				.SetDisplay("Universe", "Securities to trade", "General");

			_smaLen = Param(nameof(SmaLength), 210)
				.SetGreaterThanZero()
				.SetDisplay("SMA Length", "Period of the SMA filter", "General");

			_minUsd = Param(nameof(MinTradeUsd), 50m)
				.SetGreaterThanZero()
				.SetDisplay("Min Trade USD", "Minimal dollar amount per trade", "General");

			_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
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

			_sma.Clear();
			_latestPrices.Clear();
			_held.Clear();
			_lastDay = default;
		}

		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			if (Universe == null || !Universe.Any())
				throw new InvalidOperationException("Universe is empty.");

			foreach (var (sec, dt) in GetWorkingSecurities())
			{
				var sma = new SimpleMovingAverage { Length = SmaLength };
				_sma[sec] = sma;

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

			// Process the candle through the SMA indicator
			var sma = _sma[security];
			sma.Process(candle);

			var d = candle.OpenTime.Date;
			if (d == _lastDay)
				return;
			_lastDay = d;
			
			if (d.Day == 1)
				TryRebalance();
		}

		private void TryRebalance()
		{
			var longs = _sma.Where(kv => kv.Value.IsFormed)
							.Where(kv => 
							{
								var price = GetLatestPrice(kv.Key);
								return price > 0 && price > kv.Value.GetCurrentValue<decimal>();
							})
							.Select(kv => kv.Key).ToList();

			foreach (var sec in _held.Where(h => !longs.Contains(h)).ToList())
				Move(sec, 0);

			if (longs.Any())
			{
				var portfolioValue = Portfolio.CurrentValue ?? 0m;
				decimal cap = portfolioValue / longs.Count;
				foreach (var sec in longs)
				{
					var price = GetLatestPrice(sec);
					if (price > 0)
						Move(sec, cap / price);
				}
			}

			_held.Clear();
			_held.UnionWith(longs);
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
				Comment = "ACTrend"
			});
		}

		private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;
	}
}
