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
		private readonly StrategyParam<int[]> _hoursLong;
		private readonly StrategyParam<decimal> _minUsd;
		private readonly StrategyParam<DataType> _candleType;

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

		/// <summary>
		/// The type of candles to use for strategy calculation.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		private readonly Dictionary<Security, decimal> _latestPrices = new();

		/// <summary>
		/// Initializes a new instance of <see cref="BitcoinIntradaySeasonalityStrategy"/>.
		/// </summary>
		public BitcoinIntradaySeasonalityStrategy()
		{
			// Hours to stay long (UTC).
			_hoursLong = Param(nameof(HoursLong), new[] { 0, 1, 2, 3 })
				.SetDisplay("Long Hours", "UTC hours when the strategy stays long", "General");

			// Minimum trade size.
			_minUsd = Param(nameof(MinTradeUsd), 200m)
				.SetGreaterThanZero()
				.SetDisplay("Min Trade USD", "Minimum order value in USD", "Trading");

			_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			if (Security == null)
				throw new InvalidOperationException("BTC security not set.");

			return new[] { (Security, CandleType) };
		}

		/// <inheritdoc />
		
		protected override void OnReseted()
		{
			base.OnReseted();

			_latestPrices.Clear();
		}

		protected override void OnStarted(DateTimeOffset t)
		{
			base.OnStarted(t);

			if (HoursLong == null || HoursLong.Length == 0)
				throw new InvalidOperationException("HoursLong cannot be empty.");

			if (Security == null)
				throw new InvalidOperationException("BTC security not set.");

			SubscribeCandles(CandleType, true, Security)
				.Bind(c => ProcessCandle(c, Security))
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
			var price = GetLatestPrice(Security);

			var tgt = inSeason && price > 0 ? portfolioValue / price : 0m;
			var diff = tgt - PositionBy(Security);

			if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd)
				return;

			RegisterOrder(new Order
			{
				Security = Security,
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

