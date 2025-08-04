// ShortInterestEffectStrategy.cs
// -----------------------------------------------------------------------------
// Monthly: long stocks with lowest short-interest ratio, short highest.
// Requires TryGetShortInterest(Security) external feed.
// Trigger via daily candle of first stock.
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
    public class ShortInterestEffectStrategy : Strategy
    {
        private readonly StrategyParam<IEnumerable<Security>> _univ;
        private readonly StrategyParam<int> _decile;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly DataType _tf = TimeSpan.FromDays(1).TimeFrame();
        private readonly Dictionary<Security, decimal> _w = new();
        private DateTime _last = DateTime.MinValue;
        public IEnumerable<Security> Universe { get => _univ.Value; set => _univ.Value = value; }
        public int Decile => _decile.Value;
        public decimal MinTradeUsd => _minUsd.Value;
        public ShortInterestEffectStrategy()
        {
            _univ = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>());
            _decile = Param(nameof(Decile), 10);
            _minUsd = Param(nameof(MinTradeUsd), 200m);
        }
        public override IEnumerable<(Security, DataType)> GetWorkingSecurities() => Universe.Select(s => (s, _tf));
        protected override void OnStarted(DateTimeOffset t)
        {
            base.OnStarted(t);
            var trig = Universe.FirstOrDefault() ?? throw new InvalidOperationException("Universe empty");
            SubscribeCandles(_tf, true, trig).Bind(c => OnDaily(c.OpenTime.Date)).Start();
        }
        private void OnDaily(DateTime d)
        {
            if (d == _last)
                return;
            _last = d;
            if (d.Day != 1)
                return;
            Rebalance();
        }
        private void Rebalance()
        {
            var si = new Dictionary<Security, decimal>();
            foreach (var s in Universe)
                if (TryGetShortInterest(s, out var v))
                    si[s] = v;
            if (si.Count < Decile * 2)
                return;
            int bucket = si.Count / Decile;
            var longs = si.OrderBy(kv => kv.Value).Take(bucket).Select(kv => kv.Key).ToList();   // lowest SI
            var shorts = si.OrderByDescending(kv => kv.Value).Take(bucket).Select(kv => kv.Key).ToList();
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
        private void Move(Security s, decimal tgt) { var diff = tgt - Pos(s); if (Math.Abs(diff) * s.Price < MinTradeUsd) return; RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "ShortInt" }); }
        private decimal Pos(Security s) => GetPositionValue(s, Portfolio) ?? 0;
        private bool TryGetShortInterest(Security s, out decimal v) { v = 0; return false; }
    }
}
