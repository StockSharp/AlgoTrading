// SyntheticLendingRatesStrategy.cs
// -----------------------------------------------------------------------------
// Uses change in synthetic lending-rate intensity (external feed) to take an
// overnight position in SPY.
//   • 15:57 ET  capture intensity I0
//   • 15:59 ET  capture I1; long if I1>I0 else short
//   • 15:58 ET next day exit to flat
// Triggered by 1‑minute SPY candles (SubscribeCandles…Bind). No Schedule().
// -----------------------------------------------------------------------------
// Date: 2 Aug 2025
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Trades based on changes in synthetic lending-rate intensity.
	/// </summary>
	public class SyntheticLendingRatesStrategy : Strategy
	{
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
		private readonly Dictionary<Security, decimal> _latestPrices = new();

		/// <summary>
		/// Minimum trade value in USD.
		/// </summary>
		public decimal MinTradeUsd
		{
			get => _minUsd.Value;
			set => _minUsd.Value = value;
		}

		private decimal? _intensityT0;

		public SyntheticLendingRatesStrategy()
		{
			_minUsd = Param(nameof(MinTradeUsd), 200m)
				.SetDisplay("Min Trade USD", "Minimum notional value for orders", "Risk Management");
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
		{
			if (Security == null)
				throw new InvalidOperationException("Security not set");

			yield return (Security, CandleType);
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset t)
		{
			if (Security == null)
				throw new InvalidOperationException("Security not set");

			base.OnStarted(t);
			SubscribeCandles(CandleType, true, Security).Bind(c => ProcessCandle(c, Security)).Start();
		}

		private void ProcessCandle(ICandleMessage candle, Security security)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Store the latest closing price for this security
			_latestPrices[security] = candle.ClosePrice;

			OnMinute(candle);
		}

		private void OnMinute(ICandleMessage c)
		{
			var utc = c.OpenTime.UtcDateTime;
			// 19:57/59 UTC ≈ 15:57/59 ET (summer); adjust in prod
			if (utc.Hour == 19 && utc.Minute == 57)
				_intensityT0 = GetIntensity();
			else if (utc.Hour == 19 && utc.Minute == 59 && _intensityT0 != null)
			{
				var dir = GetIntensity() > _intensityT0 ? 1 : -1;
				var portfolioValue = Portfolio.CurrentValue ?? 0m;
				var price = GetLatestPrice(Security);
				if (price > 0)
					Trade(dir * portfolioValue / price);
			}
			// next day exit 19:58 UTC
			else if (utc.Hour == 19 && utc.Minute == 58)
				Trade(0);
		}

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}

		private void Trade(decimal tgt)
		{
			var diff = tgt - Position;
			var price = GetLatestPrice(Security);
			if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd) 
				return;
			RegisterOrder(new Order { Security = Security, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "SynLend" });
		}

		private decimal GetIntensity() => 0m; // stub replace with real feed
	}
}