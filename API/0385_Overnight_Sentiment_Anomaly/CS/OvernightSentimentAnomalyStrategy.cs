// OvernightSentimentAnomalyStrategy.cs
// -----------------------------------------------------------------------------
// Goes long equity index ETF only for overnight session when market sentiment
// indicator >= Threshold. Sentiment value must be provided by external feed
// (TryGetSentiment). Uses minute candles to trigger entry 5 min before close
// and exit 5 min after open. No Schedule() is used.
// -----------------------------------------------------------------------------
// Date: 2 Aug 2025
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Trades an equity ETF overnight based on external sentiment indicator.
	/// </summary>
	public class OvernightSentimentAnomalyStrategy : Strategy
	{
		#region Params

		private readonly StrategyParam<Security> _sentimentSym;
		private readonly StrategyParam<decimal> _threshold;
		private readonly StrategyParam<decimal> _minUsd;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>Equity ETF to trade.</summary>
		public Security EquityETF
		{
			get => Security;
			set => Security = value;
		}

		/// <summary>Symbol providing sentiment values.</summary>
		public Security SentimentSymbol
		{
			get => _sentimentSym.Value;
			set => _sentimentSym.Value = value;
		}

		/// <summary>Sentiment threshold for entry.</summary>
		public decimal Threshold
		{
			get => _threshold.Value;
			set => _threshold.Value = value;
		}

		/// <summary>Minimum trade amount in USD.</summary>
		public decimal MinTradeUsd
		{
			get => _minUsd.Value;
			set => _minUsd.Value = value;
		}

		/// <summary>Candle type for timing entry/exit.</summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		#endregion

		private readonly Dictionary<Security, decimal> _latestPrices = new();

		public OvernightSentimentAnomalyStrategy()
		{
			_sentimentSym = Param<Security>(nameof(SentimentSymbol), null)
				.SetDisplay("Sentiment Symbol", "Symbol providing sentiment", "Universe");

			_threshold = Param(nameof(Threshold), 0m)
				.SetDisplay("Threshold", "Sentiment threshold", "Parameters");

			_minUsd = Param(nameof(MinTradeUsd), 200m)
				.SetDisplay("Min USD", "Minimum trade value", "Risk");

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles for timing entry/exit", "General");
		}

		public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
		{
			if (EquityETF == null)
				throw new InvalidOperationException("EquityETF not set");

			yield return (EquityETF, CandleType);
		}

		
		protected override void OnReseted()
		{
			base.OnReseted();

			_latestPrices.Clear();
		}

		protected override void OnStarted(DateTimeOffset t)
		{
			base.OnStarted(t);

			if (EquityETF == null)
				throw new InvalidOperationException("EquityETF cannot be null.");

			if (SentimentSymbol == null)
				throw new InvalidOperationException("SentimentSymbol cannot be null.");

			// Subscribe to candles for timing entry/exit
			SubscribeCandles(CandleType, true, EquityETF)
				.Bind(c => ProcessCandle(c, EquityETF))
				.Start();
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

		private void OnMinute(ICandleMessage candle)
		{
			var utc = candle.OpenTime.UtcDateTime;
			
			// 20:55 UTC ? 15:55 ET (entry 5 min before close)
			if (utc.Hour == 20 && utc.Minute == 55)
			{
				CloseEntry();
			}
			// 14:35 UTC ? 09:35 ET (exit 5 min after open next day)
			else if (utc.Hour == 14 && utc.Minute == 35)
			{
				OpenExit();
			}
		}

		private void CloseEntry()
		{
			if (!TryGetSentiment(out var sVal) || sVal < Threshold)
				return;
			var portfolioValue = Portfolio.CurrentValue ?? 0m;
			var price = GetLatestPrice(EquityETF);
			if (price <= 0)
				return;
			var qty = portfolioValue / price;
			if (qty * price < MinTradeUsd)
				return;
			Move(qty);
		}

		private void OpenExit() => Move(0);

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}

		private void Move(decimal tgt)
		{
			var diff = tgt - Pos();
			var price = GetLatestPrice(EquityETF);
			if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd)
				return;
			RegisterOrder(new Order
			{
				Security = EquityETF,
				Portfolio = Portfolio,
				Side = diff > 0 ? Sides.Buy : Sides.Sell,
				Volume = Math.Abs(diff),
				Type = OrderTypes.Market,
				Comment = "OvernightSent"
			});
		}

		private decimal Pos() => GetPositionValue(EquityETF, Portfolio) ?? 0m;

		private bool TryGetSentiment(out decimal val) { val = 0; return false; }
	}
}
