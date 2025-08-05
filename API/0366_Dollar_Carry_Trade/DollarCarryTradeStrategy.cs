// DollarCarryTradeStrategy.cs
// Simple dollar carry trade: go long USD versus the K lowest‑yielding G10 currencies,
// short USD versus the K highest‑yielding. Carry = deposit‑rate differential (USD – FX).
// Rebalanced on the first trading day of each month using candle-based timing.
// Date: 2 August 2025

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
	/// Dollar carry trade strategy (High‑Level API).
	/// <para>
	///     * <see cref="Pairs"/> – list of FX instruments where a positive direction means buying USD
	///       (e.g., USDJPY future or USD/CAD spot).<br/>
	///     * Each month we fetch the latest carry for every pair (stub <c>TryGetCarry</c>).<br/>
	///     * Rank by carry, long USD against the K lowest carry currencies and short against
	///       the K highest carry currencies (dollar‑neutral, not notional‑neutral).<br/>
	///     * Position weights are equal within each leg.
	/// </para>
	/// Integrate your own data source in <see cref="TryGetCarry"/> to make this live.
	/// </summary>
	public class DollarCarryTradeStrategy : Strategy
	{
		private readonly StrategyParam<IEnumerable<Security>> _pairs;
		private readonly StrategyParam<int> _k;
		private readonly StrategyParam<decimal> _minUsd;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// FX pairs or currency futures.
		/// </summary>
		public IEnumerable<Security> Pairs
		{
			get => _pairs.Value;
			set => _pairs.Value = value;
		}

		/// <summary>
		/// Number of currencies in each carry leg.
		/// </summary>
		public int K
		{
			get => _k.Value;
			set => _k.Value = value;
		}

		/// <summary>
		/// Ignore trades whose notional is below this threshold.
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

		private readonly Dictionary<Security, decimal> _carry = new();
		private readonly Dictionary<Security, decimal> _weights = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private DateTime _lastRebalanceDate = DateTime.MinValue;

		/// <summary>
		/// Constructor.
		/// </summary>
		public DollarCarryTradeStrategy()
		{
			// Currency pairs to trade.
			_pairs = Param<IEnumerable<Security>>(nameof(Pairs), Array.Empty<Security>())
				.SetDisplay("Pairs", "USD crosses (required)", "Universe");

			// Number of currencies per carry leg.
			_k = Param(nameof(K), 3)
				.SetDisplay("K", "# of currencies per leg", "Ranking");

			// Minimum notional for rebalancing trades.
			_minUsd = Param(nameof(MinTradeUsd), 100m)
				.SetDisplay("Min Trade $", "Ignore tiny rebalances", "Risk");

			_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			if (!Pairs.Any())
				throw new InvalidOperationException("Pairs list is empty – populate before start.");

			// Subscribe to daily candles for monthly rebalancing trigger
			return Pairs.Select(s => (s, CandleType));
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			if (!Pairs.Any())
				throw new InvalidOperationException("Pairs list is empty – populate before start.");

			// Subscribe to daily candles for timing monthly rebalancing
			foreach (var pair in Pairs)
			{
				SubscribeCandles(CandleType, true, pair)
					.Bind(c => ProcessCandle(c, pair))
					.Start();
			}

			LogInfo($"Dollar Carry strategy started. Universe = {Pairs.Count()} pairs, K = {K}");
		}

		private void ProcessCandle(ICandleMessage candle, Security security)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Store the latest closing price for this security
			_latestPrices[security] = candle.ClosePrice;

			// Check for monthly rebalancing (first trading day of month)
			var candleDate = candle.OpenTime.Date;
			if (candleDate.Day == 1 && candleDate != _lastRebalanceDate)
			{
				_lastRebalanceDate = candleDate;
				Rebalance();
			}
		}

		private void Rebalance()
		{
			// 1. Load carry values
			_carry.Clear();
			foreach (var p in Pairs)
			{
				if (TryGetCarry(p, out var c))
					_carry[p] = c;
			}

			if (_carry.Count < K * 2)
			{
				LogInfo("Not enough carry data yet.");
				return;
			}

			// 2. Rank
			var highCarry = _carry.OrderByDescending(kv => kv.Value).Take(K).Select(kv => kv.Key).ToList();
			var lowCarry = _carry.OrderBy(kv => kv.Value).Take(K).Select(kv => kv.Key).ToList();

			// 3. Target weights (equal‑weight each leg, gross 2, net 0)
			_weights.Clear();
			decimal wLong = 1m / lowCarry.Count;   // long USD vs low carry
			decimal wShort = -1m / highCarry.Count;  // short USD vs high carry

			foreach (var s in lowCarry)
				_weights[s] = wLong;
			foreach (var s in highCarry)
				_weights[s] = wShort;

			// 4. Exit obsolete positions
			foreach (var position in Positions.Where(pos => !_weights.ContainsKey(pos.Security)))
				TradeToTarget(position.Security, 0m);

			// 5. Align to target
			var portfolioValue = Portfolio.CurrentValue ?? 0m;
			foreach (var kv in _weights)
			{
				var sec = kv.Key;
				var price = GetLatestPrice(sec);
				if (price > 0)
				{
					var tgtQty = kv.Value * portfolioValue / price;
					TradeToTarget(sec, tgtQty);
				}
			}

			LogInfo($"Rebalanced: Long {lowCarry.Count} | Short {highCarry.Count}");
		}

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}

		private void TradeToTarget(Security sec, decimal tgtQty)
		{
			var diff = tgtQty - PositionBy(sec);
			var price = GetLatestPrice(sec);
			if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd)
				return;

			RegisterOrder(new Order
			{
				Security = sec,
				Portfolio = Portfolio,
				Side = diff > 0 ? Sides.Buy : Sides.Sell,
				Volume = Math.Abs(diff),
				Type = OrderTypes.Market,
				Comment = "DollarCarry"
			});
		}

		private decimal PositionBy(Security sec) =>
			GetPositionValue(sec, Portfolio) ?? 0m;

		/// <summary>
		/// Retrieve latest interest‑rate differential: positive if USD yield &gt; FX yield.
		/// Replace this stub with call to your rates database or API.
		/// </summary>
		private bool TryGetCarry(Security pair, out decimal carry)
		{
			carry = 0m;
			return false;
		}
	}
}