// SectorMomentumRotationStrategy.cs
// -----------------------------------------------------------------------------
// Monthly rotate among sector ETFs: hold sectors with positive 6‑month momentum.
// Equal-weight those sectors; flat otherwise.
// Trigger via daily candle of first sector ETF.
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
    public class SectorMomentumRotationStrategy : Strategy
    {
        private readonly StrategyParam<IEnumerable<Security>> _sects;
        private readonly StrategyParam<int> _look;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly DataType _tf = DataType.TimeFrame(TimeSpan.FromDays(1));
        private readonly Dictionary<Security, RollingWin> _px = new();
        private DateTime _last = DateTime.MinValue;
        public IEnumerable<Security> SectorETFs { get => _sects.Value; set => _sects.Value = value; }
        public int LookbackDays => _look.Value;
        public decimal MinTradeUsd => _minUsd.Value;
        public SectorMomentumRotationStrategy()
        {
            _sects = Param<IEnumerable<Security>>(nameof(SectorETFs), Array.Empty<Security>());
            _look = Param(nameof(LookbackDays), 126);
            _minUsd = Param(nameof(MinTradeUsd), 200m);
        }
        public override IEnumerable<(Security, DataType)> GetWorkingSecurities() => SectorETFs.Select(s => (s, _tf));
        protected override void OnStarted(DateTimeOffset t)
        {
            base.OnStarted(t);
            var trig = SectorETFs.FirstOrDefault() ?? throw new InvalidOperationException("Sectors empty");
            SubscribeCandles(trig, _tf).Bind(CandleStates.Finished).Do(c => OnDaily(c.OpenTime.Date)).Start();
            foreach (var s in SectorETFs)
                _px[s] = new RollingWin(LookbackDays + 1);
            foreach (var (s, tf) in GetWorkingSecurities())
                SubscribeCandles(s, tf).Bind(CandleStates.Finished).Do(c => _px[s].Add(c.ClosePrice)).Start();
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
            var winners = new List<Security>();
            foreach (var kv in _px)
                if (kv.Value.Full && kv.Value.Data[0] > kv.Value.Data[^1])
                    winners.Add(kv.Key);
            foreach (var s in SectorETFs.Where(x => !winners.Contains(x)))
                Move(s, 0);
            if (!winners.Any())
                return;
            decimal w = 1m / winners.Count;
            foreach (var s in winners)
                Move(s, w * Portfolio.CurrentValue / s.Price);
        }
        private void Move(Security s, decimal tgt) { var diff = tgt - Pos(s); if (Math.Abs(diff) * s.Price < MinTradeUsd) return; RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "SectMom" }); }
        private decimal Pos(Security s) => GetPositionValue(s, Portfolio) ?? 0;
        private class RollingWin { private readonly Queue<decimal> _q = new(); private readonly int _n; public RollingWin(int n) { _n = n; } public bool Full => _q.Count == _n; public void Add(decimal p) { if (_q.Count == _n) _q.Dequeue(); _q.Enqueue(p); } public decimal[] Data => _q.ToArray(); }
    }
}
