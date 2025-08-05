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
	/// Strategy that goes long on Bitcoin during predefined strong intraday hours.
	/// </summary>
	public class BitcoinIntradaySeasonalityStrategy : Strategy
	{
		private readonly StrategyParam<Security> _btc;
		private readonly StrategyParam<int[]> _hoursLong;
		private readonly StrategyParam<decimal> _minUsd;
		private readonly DataType _tf = TimeSpan.FromHours(1).TimeFrame();

		/// <summary>
		/// Bitcoin security to trade.
		/// </summary>
		public Security BTC
		{
			get => _btc.Value;
			set => _btc.Value = value;
		}

		/// <summary>
		/// UTC hours when the strategy holds a long position.
		/// </summary>
		public int[] HoursLong
		{
			get => _hoursLong.Value;
			set => _hoursLong.Value = value;
		}

		/// <summary>
		/// Minimum trade value in USD.
		/// </summary>
		public decimal MinTradeUsd
		{
			get => _minUsd.Value;
			set => _minUsd.Value = value;
		}

		private readonly Dictionary<Security, decimal> _latestPrices = new();

		/// <summary>
		/// Initializes a new instance of <see cref="BitcoinIntradaySeasonalityStrategy"/>.
		/// </summary>
		public BitcoinIntradaySeasonalityStrategy()
		{
			// Bitcoin security.
			_btc = Param<Security>(nameof(BTC), null)
				.SetDisplay("BTC Security", "Security representing Bitcoin", "General");

			// Hours to stay long (UTC).
			_hoursLong = Param(nameof(HoursLong), new[] { 0, 1, 2, 3 })
				.SetDisplay("Long Hours", "UTC hours when the strategy stays long", "General");

			// Minimum trade size.
			_minUsd = Param(nameof(MinTradeUsd), 200m)
				.SetGreaterThanZero()
				.SetDisplay("Min Trade USD", "Minimum order value in USD", "Trading");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			if (BTC == null)
				throw new InvalidOperationException("BTC security not set.");

			return new[] { (BTC, _tf) };
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset t)
		{
			base.OnStarted(t);

			if (HoursLong == null || HoursLong.Length == 0)
				throw new InvalidOperationException("HoursLong cannot be empty.");

			if (BTC == null)
				throw new InvalidOperationException("BTC security not set.");

			SubscribeCandles(_tf, true, BTC)
				.Bind(c => ProcessCandle(c, BTC))
				.Start();
		}

		private void ProcessCandle(ICandleMessage candle, Security security)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Store the latest closing price for this security
			_latestPrices[security] = candle.ClosePrice;

			OnHourClose(candle);
		}

		private void OnHourClose(ICandleMessage c)
		{
			var hour = c.OpenTime.UtcDateTime.Hour; // assume server UTC
			var inSeason = HoursLong.Contains(hour);

			var portfolioValue = Portfolio.CurrentValue ?? 0m;
			var price = GetLatestPrice(BTC);

			var tgt = inSeason && price > 0 ? portfolioValue / price : 0m;
			var diff = tgt - PositionBy(BTC);

			if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd)
				return;

			RegisterOrder(new Order
			{
				Security = BTC,
				Portfolio = Portfolio,
				Side = diff > 0 ? Sides.Buy : Sides.Sell,
				Volume = Math.Abs(diff),
				Type = OrderTypes.Market,
				Comment = "BTCSeason",
			});
		}

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}

		private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;
	}
}

