// ESGFactorMomentumStrategy.cs  — parameterized timeframe version
// * Universe: IEnumerable<Security>
// * CandleType: StrategyParam<DataType>  (e.g., TimeSpan.FromDays(1).TimeFrame())
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
    public class ESGFactorMomentumStrategy : Strategy
    {
        #region Parameters
        private readonly StrategyParam<IEnumerable<Security>> _universe;
        private readonly StrategyParam<int> _lookback;
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<decimal> _minUsd;

        public IEnumerable<Security> Universe { get => _universe.Value; set => _universe.Value = value; }
        public int LookbackDays => _lookback.Value;
        public DataType CandleType => _candleType.Value;
        public decimal MinTradeUsd => _minUsd.Value;
        #endregion

        private readonly Dictionary<Security, RollingWindow<decimal>> _windows = new();
        private readonly HashSet<Security> _held = new();
        private DateTime _lastProc = DateTime.MinValue;

        public ESGFactorMomentumStrategy()
        {
            _universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>())
                .SetDisplay("Universe", "ESG ETFs list", "Universe");

            _lookback = Param(nameof(LookbackDays), 252);

            _candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
                .SetDisplay("Candle TF", "Time‑frame", "General");

            _minUsd = Param(nameof(MinTradeUsd), 100m);
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
                _windows[sec] = new RollingWindow<decimal>(LookbackDays + 1);

                SubscribeCandles(dt, true, sec)
                    .Bind(c =>
                    {
                        var win = _windows[sec];
                        win.Add(c.ClosePrice);

                        var d = c.OpenTime.Date;
                        if (d == _lastProc)
                            return;
                        _lastProc = d;

                        if (d.Day == 1 && Exchange.IsTradingDay(d))
                            TryRebalance();
                    })
                    .Start();
            }
        }

        private void TryRebalance()
        {
            if (_windows.Values.Any(w => !w.IsFull()))
                return;

            var mom = _windows.ToDictionary(kv => kv.Key,
                       kv => (kv.Value.Last() - kv.Value[0]) / kv.Value[0]);

            var best = mom.Values.Max();
            var winners = mom.Where(kv => kv.Value == best).Select(kv => kv.Key).ToList();
            decimal w = 1m / winners.Count;

            foreach (var s in _held.Where(h => !winners.Contains(h)).ToList())
                Move(s, 0);

            foreach (var s in winners)
                Move(s, w * Portfolio.CurrentValue / s.Price);

            _held.Clear();
            _held.UnionWith(winners);

            LogInfo($"Rebalance TF {CandleType}: {string.Join(',', winners.Select(x => x.Code))}");
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
                Comment = "ESGMom"
            });
        }

        private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;

        #region RollingWindow
        private class RollingWindow<T>
        {
            private readonly Queue<T> _q = new(); private readonly int _n;
            public RollingWindow(int n) { _n = n; }
            public void Add(T v) { if (_q.Count == _n) _q.Dequeue(); _q.Enqueue(v); }
            public bool IsFull() => _q.Count == _n;
            public T Last() => _q.Last();
            public T this[int i] => _q.ElementAt(i);
        }
        #endregion
    }
}