// AccrualAnomalyStrategy.cs  (revised to candle-driven)
// Yearly rebalance when first finished candle of May detected.
// Parameters: Universe (IEnumerable<Security>), CandleType param
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
    public class AccrualAnomalyStrategy : Strategy
    {
        private readonly StrategyParam<IEnumerable<Security>> _universe;
        private readonly StrategyParam<int> _deciles;
        private readonly StrategyParam<DataType> _candleType;

        public IEnumerable<Security> Universe { get => _universe.Value; set => _universe.Value = value; }
        public int Deciles => _deciles.Value;
        public DataType CandleType => _candleType.Value;

        private readonly Dictionary<Security, BalanceSnapshot> _prev = new();
        private readonly Dictionary<Security, decimal> _weights = new();
        private DateTime _lastDay = DateTime.MinValue;

        public AccrualAnomalyStrategy()
        {
            _universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>());
            _deciles = Param(nameof(Deciles), 10);
            _candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame());
        }

        public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() =>
            Universe.Select(s => (s, CandleType));

        protected override void OnStarted(DateTimeOffset time)
        {
            base.OnStarted(time);
            foreach (var (sec, dt) in GetWorkingSecurities())
            {
                SubscribeCandles(dt, true, sec)
                    .Bind(c =>
                    {
                        var d = c.OpenTime.Date;
                        if (d == _lastDay)
                            return;
                        _lastDay = d;
                        if (d.Month == 5 && Exchange.IsTradingDay(d) && d.Day == 1)
                            Rebalance();
                    })
                    .Start();
            }
        }

        private void Rebalance()
        {
            var accr = new Dictionary<Security, decimal>();
            foreach (var s in Universe)
            {
                if (!TryGetFundamentals(s, out var cur))
                    continue;
                if (_prev.TryGetValue(s, out var prev))
                    accr[s] = CalcAccrual(cur, prev);
                _prev[s] = cur;
            }

            if (accr.Count < Deciles * 2)
                return;
            int bucket = accr.Count / Deciles;
            var sorted = accr.OrderBy(kv => kv.Value).ToList();
            var longs = sorted.Take(bucket).Select(kv => kv.Key).ToList();
            var shorts = sorted.Skip(accr.Count - bucket).Select(kv => kv.Key).ToList();

            _weights.Clear();
            decimal wl = 1m / longs.Count, ws = -1m / shorts.Count;
            foreach (var s in longs)
                _weights[s] = wl;
            foreach (var s in shorts)
                _weights[s] = ws;

            foreach (var p in Positions.Keys.Where(s => !_weights.ContainsKey(s)))
                Move(p, 0);

            foreach (var kv in _weights)
                Move(kv.Key, kv.Value * Portfolio.CurrentValue / kv.Key.Price);
        }

        private void Move(Security s, decimal tgt)
        {
            var diff = tgt - PositionBy(s);
            if (Math.Abs(diff) * s.Price < 100)
                return;
            RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "Accrual" });
        }

        private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;

        private bool TryGetFundamentals(Security s, out BalanceSnapshot snap) { snap = null; return false; }

        private decimal CalcAccrual(BalanceSnapshot cur, BalanceSnapshot prev) => 0m;
        private record BalanceSnapshot(decimal a, decimal b);
    }
}