
// FScoreReversalStrategy.cs
// Combines Piotroski F‑Score with 1‑month reversal.
// Long losers (1‑month return < 0) with FScore ≥ 7; short winners (return > 0) with FScore ≤ 3.
// Monthly rebalance. F‑Score feed must be provided via TryGetFScore.
// Date: 2 August 2025

using System;
using System.Collections.Generic;
using System.Linq;
using Ecng.Common;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    public class FScoreReversalStrategy : Strategy
    {
        #region Params
        private readonly StrategyParam<IEnumerable<Security>> _universe;
        private readonly StrategyParam<int> _lookback;
        private readonly StrategyParam<int> _hi;
        private readonly StrategyParam<int> _lo;
        private readonly StrategyParam<decimal> _minUsd;

        public IEnumerable<Security> Universe { get => _universe.Value; set => _universe.Value = value; }
        public int Lookback => _lookback.Value;
        public int FHi => _hi.Value;
        public int FLo => _lo.Value;
        public decimal MinTradeUsd => _minUsd.Value;
        #endregion

        private readonly Dictionary<Security, RollingWindow<decimal>> _prices = new();
        private readonly Dictionary<Security, decimal> _ret = new();
        private readonly Dictionary<Security, int> _fscore = new();
        private readonly Dictionary<Security, decimal> _w = new();

        public FScoreReversalStrategy()
        {
            _universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>());
            _lookback = Param(nameof(Lookback), 21);
            _hi = Param(nameof(FHi), 7);
            _lo = Param(nameof(FLo), 3);
            _minUsd = Param(nameof(MinTradeUsd), 50m);
        }

        public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
        {
            if (!Universe.Any())
                throw new InvalidOperationException("Universe empty");
            var tf = DataType.TimeFrame(TimeSpan.FromDays(1));
            return Universe.Select(s => (s, tf));
        }

        protected override void OnStarted(DateTimeOffset time)
        {
            base.OnStarted(time);
            foreach (var (sec, dt) in GetWorkingSecurities())
            {
                SubscribeCandles(sec, dt).Start();
                _prices[sec] = new RollingWindow<decimal>(Lookback + 1);
            }
            Schedule(TimeSpan.Zero, _ => CurrentTime.Day == 1, Rebalance);
        }

        protected override void OnCandleFinished(ICandleMessage candle)
        {
            var sec = (Security)candle.SecurityId;
            if (!_prices.TryGetValue(sec, out var win))
                return;
            win.Add(candle.ClosePrice);
            if (win.IsFull())
                _ret[sec] = (win.Last() - win[0]) / win[0];
        }

        private void Rebalance()
        {
            _fscore.Clear();
            foreach (var s in Universe)
                if (TryGetFScore(s, out var fs))
                    _fscore[s] = fs;

            var eligible = _ret.Keys.Intersect(_fscore.Keys).ToList();
            if (eligible.Count < 20)
                return;

            var longs = eligible.Where(s => _ret[s] < 0 && _fscore[s] >= FHi).ToList();
            var shorts = eligible.Where(s => _ret[s] > 0 && _fscore[s] <= FLo).ToList();
            if (!longs.Any() || !shorts.Any())
                return;

            _w.Clear();
            decimal wl = 1m / longs.Count;
            decimal ws = -1m / shorts.Count;
            foreach (var s in longs)
                _w[s] = wl;
            foreach (var s in shorts)
                _w[s] = ws;

            foreach (var pos in Positions.Keys.Where(s => !_w.ContainsKey(s)))
                Order(pos, -PositionBy(pos));

            foreach (var kv in _w)
            {
                var tgt = kv.Value * Portfolio.CurrentValue / kv.Key.Price;
                var diff = tgt - PositionBy(kv.Key);
                if (Math.Abs(diff) * kv.Key.Price >= MinTradeUsd)
                    Order(kv.Key, diff);
            }
        }

        private void Order(Security s, decimal qty)
        {
            if (qty == 0)
                return;
            RegisterOrder(new Order
            {
                Security = s,
                Portfolio = Portfolio,
                Direction = qty > 0 ? Sides.Buy : Sides.Sell,
                Volume = Math.Abs(qty),
                Type = OrderTypes.Market,
                Comment = "FScoreRev"
            });
        }

        private decimal PositionBy(Security s) => Positions.TryGetValue(s, out var q) ? q : 0m;

        #region Stub FScore
        private bool TryGetFScore(Security s, out int fscore)
        {
            fscore = 0; // TODO integrate fundamentals
            return false;
        }
        #endregion
    }

    #region RollingWindow
    public class RollingWindow<T>
    {
        private readonly Queue<T> _q = new(); private readonly int _size;
        public RollingWindow(int size) { _size = size; }
        public void Add(T v) { if (_q.Count == _size) _q.Dequeue(); _q.Enqueue(v); }
        public bool IsFull() => _q.Count == _size;
        public T Last() => _q.Last();
        public T this[int idx] => _q.ElementAt(idx);
        public int Count => _q.Count;
    }
    #endregion
}