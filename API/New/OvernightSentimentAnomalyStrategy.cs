
// OvernightSentimentAnomalyStrategy.cs
// -----------------------------------------------------------------------------
// Goes long equity index ETF only for overnight session when market sentiment
// indicator >= Threshold. Sentiment value must be provided by external feed
// (TryGetSentiment).  No candles needed; trade executed once per trading day
// near session close / open simulated via Schedule.
// -----------------------------------------------------------------------------
// Date: 2 Aug 2025
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	public class OvernightSentimentAnomalyStrategy : Strategy
	{
		#region Params
		private readonly StrategyParam<Security> _etf;
		private readonly StrategyParam<Security> _sentimentSym;
		private readonly StrategyParam<decimal> _threshold;
		private readonly StrategyParam<decimal> _minUsd;

		public Security EquityETF { get => _etf.Value; set => _etf.Value = value; }
		public Security SentimentSymbol { get => _sentimentSym.Value; set => _sentimentSym.Value = value; }
		public decimal Threshold => _threshold.Value;
		public decimal MinTradeUsd => _minUsd.Value;
		#endregion

		public OvernightSentimentAnomalyStrategy()
		{
			_etf = Param<Security>(nameof(EquityETF), null);
			_sentimentSym = Param<Security>(nameof(SentimentSymbol), null);
			_threshold = Param(nameof(Threshold), 0m);
			_minUsd = Param(nameof(MinTradeUsd), 200m);
		}

		public override IEnumerable<(Security, DataType)> GetWorkingSecurities() =>
			Array.Empty<(Security, DataType)>();

		protected override void OnStarted(DateTimeOffset t)
		{
			base.OnStarted(t);

			// enter 5 min before close, exit next day open+5min
			Schedule(TimeSpan.FromMinutes(-5), _ => true, CloseEntry);
			Schedule(TimeSpan.FromMinutes(5), _ => true, OpenExit);
		}

		private void CloseEntry()
		{
			if (!TryGetSentiment(out var sVal) || sVal < Threshold)
				return;
			var qty = Portfolio.CurrentValue / EquityETF.Price;
			if (qty * EquityETF.Price < MinTradeUsd)
				return;
			Move(qty);
		}

		private void OpenExit() => Move(0);

		private void Move(decimal tgt)
		{
			var diff = tgt - Pos();
			if (Math.Abs(diff) * EquityETF.Price < MinTradeUsd)
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
