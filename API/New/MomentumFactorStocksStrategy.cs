// MomentumFactorStocksStrategy.cs
// -----------------------------------------------------------------------------
// Classic UMD: long top-quintile 12‑1 month momentum, short bottom quintile.
// Monthly rebalance (first trading day).
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
    public class MomentumFactorStocksStrategy : Strategy
    {
        #region Params
        private readonly StrategyParam<IEnumerable<Security>> _univ;
        private readonly StrategyParam<int> _look;
        private readonly StrategyParam<int> _skip;
        private readonly StrategyParam<int> _quint;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly StrategyParam<DataType> _tf;
        public IEnumerable<Security> Universe { get => _univ.Value; set => _univ.Value = value; }
        public int LookbackDays => _look.Value;
        public int SkipDays => _skip.Value;
        public int Quintile => _quint.Value;
        public decimal MinTradeUsd => _minUsd.Value;
        public DataType CandleType => _tf.Value;
        #endregion
        private readonly Dictionary<Security, RollingWin> _px = new();
        private DateTime _last = DateTime.MinValue;
        private readonly Dictionary<Security, decimal> _w = new();
        public MomentumFactorStocksStrategy()
        {
            _univ = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>());
            _look = Param(nameof(LookbackDays), 252);
            _skip = Param(nameof(SkipDays), 21);
            _quint = Param(nameof(Quintile), 5);
            _minUsd = Param(nameof(MinTradeUsd), 200m);
            _tf = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame());
        }
        public override IEnumerable<(Security, DataType)> GetWorkingSecurities() => Universe.Select(s => (s, CandleType));
        protected override void OnStarted(DateTimeOffset t)
        {
            base.OnStarted(t);
            foreach (var (s, tf) in GetWorkingSecurities())
            {
                _px[s] = new RollingWin(LookbackDays + 1);
                SubscribeCandles(s, tf)
                    .Bind(CandleStates.Finished)
                    .Do(c => OnDaily((Security)c.SecurityId, c)).Start();
            }
        }
        private void OnDaily(Security s, ICandleMessage c)
        {
            _px[s].Add(c.ClosePrice);
            var d = c.OpenTime.Date;
            if (d == _last)
                return;
            _last = d;
            if (d.Day != 1 || !Exchange.IsTradingDay(d))
                return;
            Rebalance();
        }
        private void Rebalance()
        {
            var mom = new Dictionary<Security, decimal>();
            foreach (var kv in _px)
                if (kv.Value.Full)
                    mom[kv.Key] = (kv.Value.Data[SkipDays] - kv.Value.Data[LookbackDays]) / kv.Value.Data[LookbackDays];
            if (mom.Count < Quintile * 2)
                return;
            int q = mom.Count / Quintile;
            var longs = mom.OrderByDescending(kv => kv.Value).Take(q).Select(kv => kv.Key).ToList();
            var shorts = mom.OrderBy(kv => kv.Value).Take(q).Select(kv => kv.Key).ToList();
            _w.Clear();
            decimal wl = 1m / longs.Count, ws = -1m / shorts.Count;
            foreach (var s in longs)
                _w[s] = wl;
            foreach (var s in shorts)
                _w[s] = ws;
            foreach (var p in Positions.Keys.Where(s => !_w.ContainsKey(s)))
                Move(p, 0);
            foreach (var kv in _w)
                Move(kv.Key, kv.Value * Portfolio.CurrentValue / kv.Key.Price);
        }
        private void Move(Security s, decimal tgt)
        {
            var diff = tgt - Pos(s);
            if (Math.Abs(diff) * s.Price < MinTradeUsd)
                return;
            RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Direction = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "MomStocks" });
        }
        private decimal Pos(Security s) => Positions.TryGetValue(s, out var q) ? q : 0m;
        private class RollingWin { private readonly Queue<decimal> _q = new(); private readonly int _n; public RollingWin(int n) { _n = n; } public bool Full => _q.Count == _n; public void Add(decimal p) { if (_q.Count == _n) _q.Dequeue(); _q.Enqueue(p); } public decimal[] Data => _q.ToArray(); }
    }
}