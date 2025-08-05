// FXCarryTradeStrategy.cs — candle-triggered
// Long TopK carry currencies, short BottomK. Rebalanced on FIRST trading day
// of month using daily candle of the first currency only (no Schedule).
// Date: 2 Aug 2025

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
	/// FX carry trade strategy going long top carry currencies and short bottom ones.
	/// </summary>
	public class FXCarryTradeStrategy : Strategy
	{
		private readonly StrategyParam<IEnumerable<Security>> _univ;
		private readonly StrategyParam<int> _topK;
		private readonly StrategyParam<decimal> _minUsd;
		private readonly StrategyParam<DataType> _tf;
		
		/// <summary>
		/// Universe of FX securities.
		/// </summary>
		public IEnumerable<Security> Universe { get => _univ.Value; set => _univ.Value = value; }
		
		/// <summary>
		/// Number of currencies to long and short.
		/// </summary>
		public int TopK { get => _topK.Value; set => _topK.Value = value; }
		
		/// <summary>
		/// Minimum trade value in USD.
		/// </summary>
		public decimal MinTradeUsd { get => _minUsd.Value; set => _minUsd.Value = value; }
		
		/// <summary>
		/// Candle type for calculations.
		/// </summary>
		public DataType CandleType { get => _tf.Value; set => _tf.Value = value; }

		private readonly Dictionary<Security, decimal> _weights = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private DateTime _lastDay = DateTime.MinValue;

		public FXCarryTradeStrategy()
		{
			_univ = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>())
				.SetDisplay("Universe", "Currencies to trade", "General");
			
			_topK = Param(nameof(TopK), 3)
				.SetGreaterThanZero()
				.SetDisplay("Top K", "Number of currencies to long and short", "General");
			
			_minUsd = Param(nameof(MinTradeUsd), 200m)
				.SetGreaterThanZero()
				.SetDisplay("Min trade USD", "Minimum order value", "Risk");
			
			_tf = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Candle Type", "Time frame for candles", "General");
		}

		public override IEnumerable<(Security, DataType)> GetWorkingSecurities() =>
			Universe.Select(s => (s, CandleType));

		protected override void OnStarted(DateTimeOffset time)
		{
			if (Universe == null || !Universe.Any())
				throw new InvalidOperationException("Universe empty.");

			base.OnStarted(time);
			var first = Universe.First();

			// Use ONLY ONE currency's daily candle to trigger monthly rebalance
				SubscribeCandles(CandleType, true, first)
					.Bind(c => ProcessCandle(c, first))
					.Start();
		}

		private void ProcessCandle(ICandleMessage candle, Security security)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Store the latest closing price for this security
			_latestPrices[security] = candle.ClosePrice;

			var day = candle.OpenTime.Date;
			if (day == _lastDay)
				return;
			_lastDay = day;

			if (day.Day == 1)
				Rebalance();
		}

		private void Rebalance()
		{
			var carry = new Dictionary<Security, decimal>();
			foreach (var fx in Universe)
				if (TryGetCarry(fx, out var c))
					carry[fx] = c;

			if (carry.Count < TopK * 2)
				return;

			var top = carry.OrderByDescending(kv => kv.Value).Take(TopK).Select(kv => kv.Key).ToList();
			var bot = carry.OrderBy(kv => kv.Value).Take(TopK).Select(kv => kv.Key).ToList();

			_weights.Clear();
			decimal wl = 1m / top.Count;
			decimal ws = -1m / bot.Count;
			foreach (var s in top)
				_weights[s] = wl;
			foreach (var s in bot)
				_weights[s] = ws;

			foreach (var position in Positions)
				if (!_weights.ContainsKey(position.Security))
					Move(position.Security, 0);

			var portfolioValue = Portfolio.CurrentValue ?? 0m;
			foreach (var kv in _weights)
			{
				var price = GetLatestPrice(kv.Key);
				if (price > 0)
					Move(kv.Key, kv.Value * portfolioValue / price);
			}
		}

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}

		private void Move(Security s, decimal tgtQty)
		{
			var diff = tgtQty - PositionBy(s);
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
				Comment = "FXCarry"
			});
		}

		private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;
		private bool TryGetCarry(Security s, out decimal carry) { carry = 0; return false; }
	}
}
