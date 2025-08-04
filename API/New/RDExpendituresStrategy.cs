// RDExpendituresStrategy.cs
// -----------------------------------------------------------------------------
// Long stocks with highest R&D−to−MV ratio, short lowest quintile.
// Monthly rebalance on first trading day via trigger candle.
// Requires external TryGetRDExpenseRatio.
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
    public class RDExpendituresStrategy : Strategy
    {
        private readonly StrategyParam<IEnumerable<Security>> _univ;
        private readonly StrategyParam<int> _quint;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly DataType _tf = DataType.TimeFrame(TimeSpan.FromDays(1));
        private readonly Dictionary<Security, decimal> _w = new();
        private DateTime _last = DateTime.MinValue;
        public IEnumerable<Security> Universe { get => _univ.Value; set => _univ.Value = value; }
        public int Quintile => _quint.Value;
        public decimal MinTradeUsd => _minUsd.Value;
        public RDExpendituresStrategy()
        {
            _univ = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>());
            _quint = Param(nameof(Quintile), 5);
            _minUsd = Param(nameof(MinTradeUsd), 200m);
        }
        public override IEnumerable<(Security, DataType)> GetWorkingSecurities() => Universe.Select(s => (s, _tf));
        protected override void OnStarted(DateTimeOffset t)
        {
            base.OnStarted(t);
            var trig = Universe.FirstOrDefault() ?? throw new InvalidOperationException("Universe empty");
            SubscribeCandles(trig, _tf).Bind(CandleStates.Finished).Do(c => OnDaily(c.OpenTime.Date)).Start();
        }
        private void OnDaily(DateTime d)
        {
            if (d == _last)
                return;
            _last = d;
            if (d.Day != 1 || !Exchange.IsTradingDay(d))
                return;
            Rebalance();
        }
        private void Rebalance()
        {
            var ratio = new Dictionary<Security, decimal>();
            foreach (var s in Universe)
                if (TryGetRDExpenseRatio(s, out var r))
                    ratio[s] = r;
            if (ratio.Count < Quintile * 2)
                return;
            int q = ratio.Count / Quintile;
            var longs = ratio.OrderByDescending(kv => kv.Value).Take(q).Select(kv => kv.Key).ToList();
            var shorts = ratio.OrderBy(kv => kv.Value).Take(q).Select(kv => kv.Key).ToList();
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
        private void Move(Security s, decimal tgt) { var diff = tgt - Pos(s); if (Math.Abs(diff) * s.Price < MinTradeUsd) return; RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "RDmom" }); }
        private decimal Pos(Security s) => GetPositionValue(s, Portfolio) ?? 0;
        private bool TryGetRDExpenseRatio(Security s, out decimal r) { r = 0; return false; }
    }
}
