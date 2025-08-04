// EarningsQualityFactorStrategy.cs — candle‑stream version
// Rebalance triggered by first finished candle of July 1.
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
    public class EarningsQualityFactorStrategy : Strategy
    {
        private readonly StrategyParam<IEnumerable<Security>> _universe;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly StrategyParam<DataType> _candleType;

        public IEnumerable<Security> Universe { get => _universe.Value; set => _universe.Value = value; }
        public decimal MinTradeUsd => _minUsd.Value;
        public DataType CandleType => _candleType.Value;

        private readonly Dictionary<Security, decimal> _weights = new();
        private readonly Dictionary<Security, decimal> _latestPrices = new();
        private DateTime _lastProcessed = DateTime.MinValue;

        public EarningsQualityFactorStrategy()
        {
            _universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>());
            _minUsd = Param(nameof(MinTradeUsd), 100m);
            _candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame());
        }

        public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() =>
            Universe.Select(s => (s, CandleType));

        protected override void OnStarted(DateTimeOffset time)
        {
            base.OnStarted(time);
            if (!Universe.Any())
                throw new InvalidOperationException("Universe empty");

            foreach (var (sec, dt) in GetWorkingSecurities())
            {
                SubscribeCandles(dt, true, sec)
                    .Bind(c => ProcessCandle(c, sec))
                    .Start();
            }
        }

        private void Rebalance()
        {
            var scores = new Dictionary<Security, decimal>();
            foreach (var s in Universe)
                if (TryGetQualityScore(s, out var q))
                    scores[s] = q;

            if (scores.Count < 20)
                return;

            int dec = scores.Count / 10;
            var longs = scores.OrderByDescending(kv => kv.Value).Take(dec).Select(kv => kv.Key).ToList();
            var shorts = scores.OrderBy(kv => kv.Value).Take(dec).Select(kv => kv.Key).ToList();

            _weights.Clear();
            decimal wl = 1m / longs.Count;
            decimal ws = -1m / shorts.Count;
            foreach (var s in longs)
                _weights[s] = wl;
            foreach (var s in shorts)
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

        private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;

        private decimal GetLatestPrice(Security security)
        {
            return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
        }

        private void ProcessCandle(ICandleMessage candle, Security security)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Store the latest closing price for this security
            _latestPrices[security] = candle.ClosePrice;

            var d = candle.OpenTime.Date;
            if (d == _lastProcessed)
                return;
            _lastProcessed = d;
            if (d.Month == 7 && d.Day == 1)
                Rebalance();
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
                Comment = "EQuality"
            });
        }

        private bool TryGetQualityScore(Security s, out decimal score) { score = 0m; return false; }
    }
}