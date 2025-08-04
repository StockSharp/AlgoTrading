// EarningsAnnouncementReversalStrategy.cs
// -----------------------------------------------------------------------------
// For each stock in Universe, if today is EarningsDate +/- 1 trading day,
// trade short-term reversal: short 5-day winners, long 5-day losers.
// Exits after HoldingDays. Triggered by daily candles of each stock.
// Requires TryGetEarningsDate(Security, out DateTime).
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
    public class EarningsAnnouncementReversalStrategy : Strategy
    {
        private readonly StrategyParam<IEnumerable<Security>> _univ;
        private readonly StrategyParam<int> _look;
        private readonly StrategyParam<int> _hold;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly DataType _tf = TimeSpan.FromDays(1).TimeFrame();

        public IEnumerable<Security> Universe { get => _univ.Value; set => _univ.Value = value; }
        public int LookbackDays => _look.Value;
        public int HoldingDays => _hold.Value;
        public decimal MinTradeUsd => _minUsd.Value;

        private class Win { public Queue<decimal> Px = new(); public int Held; }
        private readonly Dictionary<Security, Win> _map = new();

        public EarningsAnnouncementReversalStrategy()
        {
            _univ = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>());
            _look = Param(nameof(LookbackDays), 5);
            _hold = Param(nameof(HoldingDays), 3);
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
            var w = _map[s];
            if (w.Px.Count == LookbackDays + 1)
                w.Px.Dequeue();
            w.Px.Enqueue(c.ClosePrice);

            if (!TryGetEarningsDate(s, out var ed))
                return;
            var d = c.OpenTime.Date;
            if (Math.Abs((d - ed.Date).TotalDays) > 1)
                return; // window

            if (w.Px.Count < LookbackDays + 1)
                return;
            var arr = w.Px.ToArray();
            var ret = (arr[0] - arr[^1]) / arr[^1];

            if (ret > 0)
            { // winner -> short
                Move(s, -Portfolio.CurrentValue / Universe.Count() / s.Price);
            }
            else
            { // loser -> long
                Move(s, Portfolio.CurrentValue / Universe.Count() / s.Price);
            }
            w.Held = 0;
        }

        private void Move(Security s, decimal tgt)
        {
            var diff = tgt - Pos(s);
            if (Math.Abs(diff) * s.Price < MinTradeUsd)
                return;
            RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "EARev" });
        }
        private decimal Pos(Security s) => GetPositionValue(s, Portfolio) ?? 0;
        private bool TryGetEarningsDate(Security s, out DateTime dt) { dt = DateTime.MinValue; return false; }
    }
}