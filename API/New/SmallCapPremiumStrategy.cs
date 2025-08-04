// SmallCapPremiumStrategy.cs
// -----------------------------------------------------------------------------
// SMB long-short: long lowest-capitalisation quintile, short highest quintile.
// Needs external market-cap feed in TryGetMarketCap().
// Rebalanced monthly via candle stream (first trading day).
// -----------------------------------------------------------------------------
// Date: 2 Aug 2025
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    public class SmallCapPremiumStrategy : Strategy
    {
        #region Params
        private readonly StrategyParam<IEnumerable<Security>> _universe;
        private readonly StrategyParam<int> _quint;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly DataType _tf = TimeSpan.FromDays(1).TimeFrame();

        public IEnumerable<Security> Universe { get => _universe.Value; set => _universe.Value = value; }
        public int Quintile => _quint.Value;
        public decimal MinTradeUsd => _minUsd.Value;
        #endregion

        private readonly Dictionary<Security, decimal> _weights = new();
        private DateTime _lastDay = DateTime.MinValue;

        public SmallCapPremiumStrategy()
        {
            _universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>());
            _quint = Param(nameof(Quintile), 5);
            _minUsd = Param(nameof(MinTradeUsd), 200m);
        }

        public override IEnumerable<(Security, DataType)> GetWorkingSecurities() =>
            Universe.Select(s => (s, _tf));

        protected override void OnStarted(DateTimeOffset t)
        {
            base.OnStarted(t);
            var trigger = Universe.FirstOrDefault() ??
                          throw new InvalidOperationException("Universe is empty");

            SubscribeCandles(_tf, true, trigger)
                .Bind(c =>
                {
                    var d = c.OpenTime.Date;
                    if (d == _lastDay)
                        return;
                    _lastDay = d;

                    if (d.Day == 1)
                        Rebalance();
                })
                .Start();
        }

        private void Rebalance()
        {
            var cap = new Dictionary<Security, decimal>();
            foreach (var s in Universe)
                if (TryGetMarketCap(s, out var v))
                    cap[s] = v;

            if (cap.Count < Quintile * 2)
                return;

            int bucket = cap.Count / Quintile;
            var small = cap.OrderBy(kv => kv.Value).Take(bucket).Select(kv => kv.Key).ToList();
            var large = cap.OrderByDescending(kv => kv.Value).Take(bucket).Select(kv => kv.Key).ToList();

            _weights.Clear();
            decimal wl = 1m / small.Count;
            decimal ws = -1m / large.Count;
            foreach (var s in small)
                _weights[s] = wl;
            foreach (var s in large)
                _weights[s] = ws;

            foreach (var pos in Positions.Keys.Where(sec => !_weights.ContainsKey(sec)))
                Move(pos, 0);

            foreach (var kv in _weights)
                Move(kv.Key, kv.Value * Portfolio.CurrentValue / kv.Key.Price);
        }

        private void Move(Security s, decimal tgt)
        {
            var diff = tgt - PositionBy(s);
            if (Math.Abs(diff) * s.Price < MinTradeUsd)
                return;

            RegisterOrder(new Order
            {
                Security = s,
                Portfolio = Portfolio,
                Side = diff > 0 ? Sides.Buy : Sides.Sell,
                Volume = Math.Abs(diff),
                Type = OrderTypes.Market,
                Comment = "SmallCap"
            });
        }

        private decimal PositionBy(Security s) =>
            GetPositionValue(s, Portfolio) ?? 0;

        private bool TryGetMarketCap(Security s, out decimal cap)
        {
            cap = 0;
            return false;   // plug in your data feed
        }
    }
}