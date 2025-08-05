using System;
using System.Linq;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Equal-weight BTC and ETH basket rebalancing strategy.
	/// Rebalances weekly at the first hourly candle of Monday.
	/// </summary>
	public class CryptoRebalancingPremiumStrategy : Strategy
	{
		private readonly StrategyParam<Security> _btc;
		private readonly StrategyParam<Security> _eth;
		private readonly StrategyParam<decimal> _minUsd;
		private readonly StrategyParam<DataType> _candleType;
		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private DateTime _last = DateTime.MinValue;

		/// <summary>
		/// BTC security.
		/// </summary>
		public Security BTC
		{
			get => _btc.Value;
			set => _btc.Value = value;
		}

		/// <summary>
		/// ETH security.
		/// </summary>
		public Security ETH
		{
			get => _eth.Value;
			set => _eth.Value = value;
		}

		/// <summary>
		/// Minimum trade amount in USD.
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

		/// <summary>
		/// Initializes a new instance of <see cref="CryptoRebalancingPremiumStrategy"/>.
		/// </summary>
		public CryptoRebalancingPremiumStrategy()
		{
			// BTC security parameter.
			_btc = Param<Security>(nameof(BTC), null)
				.SetDisplay("BTC", "Bitcoin security", "General");

			// ETH security parameter.
			_eth = Param<Security>(nameof(ETH), null)
				.SetDisplay("ETH", "Ethereum security", "General");

			// Minimum trade amount parameter.
			_minUsd = Param(nameof(MinTradeUsd), 200m)
				.SetGreaterThanZero()
				.SetDisplay("Min Trade USD", "Minimum dollar amount per trade", "Trading");

			_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return [(BTC, CandleType), (ETH, CandleType)];
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset t)
		{
			base.OnStarted(t);

			var securities = GetWorkingSecurities().ToArray();
			if (securities.Length == 0 || securities.Any(p => p.sec == null))
				throw new InvalidOperationException("Working securities collection is empty or contains null.");

			foreach (var (sec, dt) in securities)
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

			OnTick(candle.OpenTime.UtcDateTime);
		}

		private void OnTick(DateTime utc)
		{
			if (utc == _last)
				return;

			_last = utc;

			if (utc.DayOfWeek != DayOfWeek.Monday || utc.Hour != 0)
				return;

			Rebalance();
		}

		private void Rebalance()
		{
			var portfolioValue = Portfolio.CurrentValue ?? 0m;
			decimal half = portfolioValue / 2;

			var btcPrice = GetLatestPrice(BTC);
			var ethPrice = GetLatestPrice(ETH);

			if (btcPrice > 0)
				Move(BTC, half / btcPrice);

			if (ethPrice > 0)
				Move(ETH, half / ethPrice);
		}

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}

		private void Move(Security s, decimal tgt)
		{
			var diff = tgt - Pos(s);
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
				Comment = "RebalPrem"
			});
		}

		private decimal Pos(Security s) => GetPositionValue(s, Portfolio) ?? 0;
	}
}
