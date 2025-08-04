// CommodityMomentumStrategy.cs
// -----------------------------------------------------------------------------
// Long commodities with highest 12‑month momentum (skip last month).
// Rebalanced monthly (first trading day).
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
    public class CommodityMomentumStrategy : Strategy
    {
        #region Params
        private readonly StrategyParam<IEnumerable<Security>> _univ;
        private readonly StrategyParam<int> _topN;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly StrategyParam<DataType> _tf;
        public IEnumerable<Security> Universe { get => _univ.Value; set => _univ.Value = value; }
        public int TopN => _topN.Value;
        public decimal MinTradeUsd => _minUsd.Value;
        public DataType CandleType => _tf.Value;
        #endregion

        private readonly Dictionary<Security, RollingWin> _px = new();
        private readonly Dictionary<Security, decimal> _latestPrices = new();
        private DateTime _lastDay = DateTime.MinValue;

        public CommodityMomentumStrategy()
        {
            _univ = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>());
            _topN = Param(nameof(TopN), 5);
            _minUsd = Param(nameof(MinTradeUsd), 200m);
            _tf = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame());
        }

        public override IEnumerable<(Security, DataType)> GetWorkingSecurities() =>
            Universe.Select(s => (s, CandleType));

        protected override void OnStarted(DateTimeOffset t)
        {
            base.OnStarted(t);
            foreach (var (s, tf) in GetWorkingSecurities())
            {
                _px[s] = new RollingWin(252 + 1);
                SubscribeCandles(tf, true, s)
                    .Bind(c => ProcessCandle(c, s))
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

            OnDaily(security, candle);
        }

        private void OnDaily(Security s, ICandleMessage c)
        {
            _px[s].Add(c.ClosePrice);
            var d = c.OpenTime.Date;
            if (d == _lastDay)
                return;
            _lastDay = d;
            if (d.Day != 1)
                return;
            Rebalance();
        }

        private void Rebalance()
        {
            var mom = new Dictionary<Security, decimal>();
            foreach (var kv in _px)
            {
                if (!kv.Value.Full)
                    continue;
                var arr = kv.Value.Data;
                var r = (arr[21] - arr[252]) / arr[252]; // 12m momentum excluding last month
                mom[kv.Key] = r;
            }
            if (mom.Count < TopN)
                return;
            var winners = mom.OrderByDescending(kv => kv.Value).Take(TopN).Select(kv => kv.Key).ToList();
            foreach (var position in Positions)
                if (!winners.Contains(position.Security))
                    Move(position.Security, 0);
            decimal w = 1m / winners.Count;
            var portfolioValue = Portfolio.CurrentValue ?? 0m;
            foreach (var s in winners)
            {
                var price = GetLatestPrice(s);
                if (price > 0)
                    Move(s, w * portfolioValue / price);
            }
        }

        private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;

        private decimal GetLatestPrice(Security security)
        {
            return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
        }

        private void Move(Security s, decimal tgt)
        {
            var diff = tgt - PositionBy(s);
            var price = GetLatestPrice(s);
            if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd)
                return;
            RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "ComMom" });
        }

        private class RollingWin
        {
            private readonly Queue<decimal> _q = new(); private readonly int _n;
            public RollingWin(int n) { _n = n; }
            public bool Full => _q.Count == _n;
            public void Add(decimal p) { if (_q.Count == _n) _q.Dequeue(); _q.Enqueue(p); }
            public decimal[] Data => _q.ToArray();
        }
    }
}