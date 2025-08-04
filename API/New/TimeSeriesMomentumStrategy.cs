// TimeSeriesMomentumStrategy.cs
// -----------------------------------------------------------------------------
// 12‑month momentum sign, weight = 1/vol (60‑day). Monthly rebalance.
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
    public class TimeSeriesMomentumStrategy : Strategy
    {
        private readonly StrategyParam<IEnumerable<Security>> _univ;
        private readonly StrategyParam<int> _look;
        private readonly StrategyParam<int> _vol;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly DataType _tf = TimeSpan.FromDays(1).TimeFrame();
        private class Win { public Queue<decimal> Px = new(); }
        private readonly Dictionary<Security, Win> _map = new();
        private readonly Dictionary<Security, decimal> _w = new();
        private DateTime _last = DateTime.MinValue;
        public IEnumerable<Security> Universe { get => _univ.Value; set => _univ.Value = value; }
        public int Lookback => _look.Value;
        public int VolWindow => _vol.Value;
        public decimal MinTradeUsd => _minUsd.Value;
        public TimeSeriesMomentumStrategy()
        {
            _univ = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>());
            _look = Param(nameof(Lookback), 252);
            _vol = Param(nameof(VolWindow), 60);
            _minUsd = Param(nameof(MinTradeUsd), 200m);
        }
        public override IEnumerable<(Security, DataType)> GetWorkingSecurities() => Universe.Select(s => (s, _tf));
        protected override void OnStarted(DateTimeOffset t)
        {
            base.OnStarted(t);
            foreach (var (s, tf) in GetWorkingSecurities())
            {
                _map[s] = new Win();
                SubscribeCandles(tf, true, s).Bind(c => OnDaily((Security)c.SecurityId, c)).Start();
            }
        }
        private void OnDaily(Security s, ICandleMessage c)
        {
            var q = _map[s].Px;
            if (q.Count == Math.Max(Lookback, VolWindow) + 1)
                q.Dequeue();
            q.Enqueue(c.ClosePrice);
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
            var sig = new Dictionary<Security, (decimal perf, double vol)>();
            foreach (var kv in _map)
            {
                if (kv.Value.Px.Count < Lookback + 1 || kv.Value.Px.Count < VolWindow + 1)
                    continue;
                var arr = kv.Value.Px.ToArray();
                var perf = (arr[0] - arr[Lookback]) / arr[Lookback];
                double[] ret = new double[VolWindow];
                for (int i = 1; i <= VolWindow; i++)
                    ret[i - 1] = (double)((arr[i - 1] - arr[i]) / arr[i]);
                var vol = Math.Sqrt(ret.Select(x => x * x).Average());
                sig[kv.Key] = (perf, vol);
            }
            if (!sig.Any())
                return;
            _w.Clear();
            foreach (var kv in sig)
            {
                var dir = kv.Value.perf > 0 ? 1 : -1;
                _w[kv.Key] = dir / (decimal)kv.Value.vol;
            }
            var norm = _w.Values.Sum(x => Math.Abs(x));
            foreach (var k in _w.Keys.ToList())
                _w[k] /= norm;
            foreach (var p in Positions.Keys.Where(x => !_w.ContainsKey(x)))
                Trade(p, 0);
            foreach (var kv in _w)
                Trade(kv.Key, kv.Value * Portfolio.CurrentValue / kv.Key.Price);
        }
        private void Trade(Security s, decimal tgt)
        {
            var diff = tgt - Pos(s);
            if (Math.Abs(diff) * s.Price < MinTradeUsd)
                return;
            RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "TSMom" });
        }
        private decimal Pos(Security s) => GetPositionValue(s, Portfolio) ?? 0;
    }
}