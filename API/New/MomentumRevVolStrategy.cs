
// MomentumRevVolStrategy.cs
// -----------------------------------------------------------------------------
// Composite score =  (12m momentum  * weights.Wm)
//                 + (-1m reversal   * weights.Wr)
//                 + (-volatility    * weights.Wv)
// Long top decile, short bottom decile monthly.
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
    public class MomentumRevVolStrategy : Strategy
    {
        #region Params
        private readonly StrategyParam<IEnumerable<Security>> _univ;
        private readonly StrategyParam<int> _look12;
        private readonly StrategyParam<int> _look1;
        private readonly StrategyParam<int> _volWindow;
        private readonly StrategyParam<decimal> _wM;
        private readonly StrategyParam<decimal> _wR;
        private readonly StrategyParam<decimal> _wV;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly StrategyParam<DataType> _tf;
        public IEnumerable<Security> Universe { get => _univ.Value; set => _univ.Value = value; }
        public int Lookback12 => _look12.Value;
        public int Lookback1 => _look1.Value;
        public int VolWindow => _volWindow.Value;
        public decimal WM => _wM.Value;
        public decimal WR => _wR.Value;
        public decimal WV => _wV.Value;
        public decimal MinTradeUsd => _minUsd.Value;
        public DataType CandleType => _tf.Value;
        #endregion

        private class Win
        {
            public RollingWin Px; public RollingWin Ret;
        }
        private readonly Dictionary<Security, Win> _map = new();
        private DateTime _lastDay = DateTime.MinValue;
        private readonly Dictionary<Security, decimal> _w = new();

        public MomentumRevVolStrategy()
        {
            _univ = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>());
            _look12 = Param(nameof(Lookback12), 252);
            _look1 = Param(nameof(Lookback1), 21);
            _volWindow = Param(nameof(VolWindow), 60);
            _wM = Param(nameof(WM), 1m);
            _wR = Param(nameof(WR), 1m);
            _wV = Param(nameof(WV), 1m);
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
                _map[s] = new Win { Px = new RollingWin(Lookback12 + 1), Ret = new RollingWin(VolWindow + 1) };
                SubscribeCandles(tf, true, s)
                    .Bind(c => OnDaily((Security)c.SecurityId, c))
                    .Start();
            }
        }

        private void OnDaily(Security s, ICandleMessage c)
        {
            var w = _map[s];
            w.Px.Add(c.ClosePrice);
            w.Ret.Add(c.ClosePrice);
            var d = c.OpenTime.Date;
            if (d == _lastDay)
                return;
            _lastDay = d;
            if (d.Day != 1 || !Exchange.IsTradingDay(d))
                return;
            Rebalance();
        }

        private void Rebalance()
        {
            var score = new Dictionary<Security, decimal>();
            foreach (var kv in _map)
            {
                var pxArr = kv.Value.Px.Data;
                var nP = Lookback12;
                var n1 = Lookback1;
                if (kv.Value.Px.Size < Lookback12 + 1)
                    continue;
                var mom = (pxArr[0] - pxArr[nP]) / pxArr[nP];
                var rev = (pxArr[0] - pxArr[n1]) / pxArr[n1];
                var retArr = kv.Value.Ret.ReturnSeries();
                if (retArr.Length < VolWindow)
                    continue;
                var vol = (decimal)Math.Sqrt(retArr.Select(r => (double)r * (double)r).Average());
                score[kv.Key] = WM * mom - WR * rev - WV * vol;
            }
            if (score.Count < 20)
                return;
            int dec = score.Count / 10;
            var longs = score.OrderByDescending(kv => kv.Value).Take(dec).Select(kv => kv.Key).ToList();
            var shorts = score.OrderBy(kv => kv.Value).Take(dec).Select(kv => kv.Key).ToList();
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
                Comment = "MomRevVol"
            });
        }
        private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;

        private class RollingWin
        {
            private readonly Queue<decimal> _q = new(); private readonly int _n;
            public RollingWin(int n) { _n = n; }
            public int Size => _q.Count;
            public void Add(decimal p) { if (_q.Count == _n) _q.Dequeue(); _q.Enqueue(p); }
            public decimal[] Data => _q.ToArray();
            public decimal[] ReturnSeries()
            {
                var arr = _q.ToArray();
                var res = new decimal[arr.Length - 1];
                for (int i = 1; i < arr.Length; i++)
                    res[i - 1] = (arr[i] - arr[i - 1]) / arr[i - 1];
                return res;
            }
        }
    }
}
