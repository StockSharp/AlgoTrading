// MutualFundMomentumStrategy.cs — candle-triggered (daily)
// Quarterly rebalance triggered by first fund's daily candle.
// Date: 2 Aug 2025

using System;
using System.Collections.Generic;
using System.Linq;
using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Algo.Candles;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    public class MutualFundMomentumStrategy : Strategy
    {
        private readonly StrategyParam<IEnumerable<Security>> _funds;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly DataType _tf = DataType.TimeFrame(TimeSpan.FromDays(1));

        public IEnumerable<Security> Funds { get => _funds.Value; set => _funds.Value = value; }
        public decimal MinTradeUsd => _minUsd.Value;

        private DateTime _lastDay = DateTime.MinValue;

        public MutualFundMomentumStrategy()
        {
            _funds = Param<IEnumerable<Security>>(nameof(Funds), Array.Empty<Security>());
            _minUsd = Param(nameof(MinTradeUsd), 200m);
        }

        public override IEnumerable<(Security, DataType)> GetWorkingSecurities() =>
            Funds.Select(f => (f, _tf));

        protected override void OnStarted(DateTimeOffset t)
        {
            base.OnStarted(t);
            var trigger = Funds.FirstOrDefault() ?? throw new InvalidOperationException("Funds empty.");

            SubscribeCandles(trigger, _tf)
                .Bind(CandleStates.Finished)
                .Do(c =>
                {
                    var d = c.OpenTime.Date;
                    if (d == _lastDay)
                        return;
                    _lastDay = d;

                    if (IsQuarterRebalanceDay(d))
                        Rebalance();
                }).Start();
        }

        private bool IsQuarterRebalanceDay(DateTime d) =>
            d.Month % 3 == 0 && d.Day <= 3 && Exchange.IsTradingDay(d);

        private void Rebalance()
        {
            var perf = new Dictionary<Security, decimal>();
            foreach (var f in Funds)
                if (TryGetNAV6m(f, out var nav6, out var nav0))
                    perf[f] = (nav0 - nav6) / nav6;

            if (perf.Count < 10)
                return;
            int dec = perf.Count / 10;
            var longs = perf.OrderByDescending(kv => kv.Value).Take(dec).Select(kv => kv.Key).ToList();

            foreach (var pos in Positions.Keys.Where(s => !longs.Contains(s)))
                Move(pos, 0);

            decimal w = 1m / longs.Count;
            foreach (var s in longs)
                Move(s, w * Portfolio.CurrentValue / s.Price);
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
                Direction = diff > 0 ? Sides.Buy : Sides.Sell,
                Volume = Math.Abs(diff),
                Type = OrderTypes.Market,
                Comment = "MutualMom"
            });
        }

        private decimal PositionBy(Security s) => Positions.TryGetValue(s, out var q) ? q : 0m;
        private bool TryGetNAV6m(Security f, out decimal nav6, out decimal nav0) { nav6 = nav0 = 0; return false; }
    }
}
