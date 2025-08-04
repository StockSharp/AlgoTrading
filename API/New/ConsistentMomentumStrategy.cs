// ConsistentMomentumStrategy.cs (revised)
// Uses rolling windows on daily candles; tranches monthly; CandleType param.
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
    public class ConsistentMomentumStrategy : Strategy
    {
        private readonly StrategyParam<IEnumerable<Security>> _universe;
        private readonly StrategyParam<int> _lookback;
        private readonly StrategyParam<DataType> _tf;
        private readonly StrategyParam<int> _holding;
        private readonly StrategyParam<decimal> _minUsd;

        public IEnumerable<Security> Universe { get => _universe.Value; set => _universe.Value = value; }
        public int LookbackDays => _lookback.Value;   // 7*21 default
        public DataType CandleType => _tf.Value;
        public int HoldingMonths => _holding.Value;
        public decimal MinTradeUsd => _minUsd.Value;

        private readonly Dictionary<Security, RollingWindow<decimal>> _prices = new();
        private readonly Dictionary<Security, decimal> _latestPrices = new();
        private readonly List<Tranche> _tranches = new();
        private DateTime _lastDay = DateTime.MinValue;

        public ConsistentMomentumStrategy()
        {
            _universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>());
            _lookback = Param(nameof(LookbackDays), 7 * 21);
            _tf = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame());
            _holding = Param(nameof(HoldingMonths), 6);
            _minUsd = Param(nameof(MinTradeUsd), 50m);
        }

        public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() =>
            Universe.Select(s => (s, CandleType));

        protected override void OnStarted(DateTimeOffset time)
        {
            base.OnStarted(time);
            foreach (var (sec, dt) in GetWorkingSecurities())
            {
                _prices[sec] = new RollingWindow<decimal>(LookbackDays + 1);
                SubscribeCandles(dt, true, sec)
                    .Bind(c => ProcessCandle(c, sec))
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

            ProcessDaily(candle, security);
        }

        private void ProcessDaily(ICandleMessage c, Security sec)
        {
            _prices[sec].Add(c.ClosePrice);

            var d = c.OpenTime.Date;
            if (d == _lastDay)
                return;
            _lastDay = d;

            // Age tranches
            foreach (var tr in _tranches.ToList())
            {
                tr.Age++;
                if (tr.Age >= HoldingMonths)
                {
                    foreach (var (s, qty) in tr.Pos)
                        Move(s, 0);
                    _tranches.Remove(tr);
                }
            }

            if (d.Day != 1)
                return;

            if (_prices.Values.Any(w => !w.IsFull()))
                return;

            // momentum windows
            int m7 = 7 * 21;
            int m6 = 6 * 21;
            var m71 = _prices.ToDictionary(kv => kv.Key,
                       kv => (kv.Value[m7 - 21] - kv.Value[0]) / kv.Value[0]);
            var m60 = _prices.ToDictionary(kv => kv.Key,
                       kv => (kv.Value.Last() - kv.Value[21]) / kv.Value[21]);

            int dec = _prices.Count / 10;
            var top71 = m71.OrderByDescending(kv => kv.Value).Take(dec).Select(kv => kv.Key).ToHashSet();
            var top60 = m60.OrderByDescending(kv => kv.Value).Take(dec).Select(kv => kv.Key).ToHashSet();
            var bot71 = m71.OrderBy(kv => kv.Value).Take(dec).Select(kv => kv.Key).ToHashSet();
            var bot60 = m60.OrderBy(kv => kv.Value).Take(dec).Select(kv => kv.Key).ToHashSet();

            var longs = top71.Intersect(top60).ToList();
            var shorts = bot71.Intersect(bot60).ToList();
            if (!longs.Any() || !shorts.Any())
                return;

            decimal cap = Portfolio.CurrentValue ?? 0m;
            decimal wl = cap * 0.5m / longs.Count;
            decimal ws = cap * 0.5m / shorts.Count;

            var tranche = new Tranche();
            foreach (var s in longs)
            {
                var price = GetLatestPrice(s);
                if (price > 0)
                {
                    var qty = wl / price;
                    Move(s, qty);
                    tranche.Pos.Add((s, qty));
                }
            }
            foreach (var s in shorts)
            {
                var price = GetLatestPrice(s);
                if (price > 0)
                {
                    var qty = -ws / price;
                    Move(s, qty);
                    tranche.Pos.Add((s, qty));
                }
            }
            _tranches.Add(tranche);
        }

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
            RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "ConsMom" });
        }
        private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;

        private class Tranche { public List<(Security PosSec, decimal PosQty)> Pos = new(); public int Age; }

        #region RollingWindow
        private class RollingWindow<T>
        {
            private readonly Queue<T> _q = new();
            private readonly int _n;
            public RollingWindow(int n) { _n = n; }
            public void Add(T v) { if (_q.Count == _n) _q.Dequeue(); _q.Enqueue(v); }
            public bool IsFull() => _q.Count == _n;
            public T Last() => _q.Last();
            public T this[int i] => _q.ElementAt(i);
        }
        #endregion

    }}