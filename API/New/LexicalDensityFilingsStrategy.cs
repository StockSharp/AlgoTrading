// LexicalDensityFilingsStrategy.cs — candle-triggered
// Quarterly rebalance on first 3 trading days of Feb/May/Aug/Nov.
// Uses daily candle of first stock to trigger.
// Date: 2 Aug 2025

using System;
using System.Collections.Generic;
using System.Linq;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    public class LexicalDensityFilingsStrategy : Strategy
    {
        private readonly StrategyParam<IEnumerable<Security>> _univ;
        private readonly StrategyParam<int> _quintile;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly StrategyParam<DataType> _tf;

        public IEnumerable<Security> Universe { get => _univ.Value; set => _univ.Value = value; }
        public int Quintile => _quintile.Value;
        public decimal MinTradeUsd => _minUsd.Value;
        public DataType CandleType => _tf.Value;

        private readonly Dictionary<Security, decimal> _weights = new();
        private DateTime _lastDay = DateTime.MinValue;

        public LexicalDensityFilingsStrategy()
        {
            _univ = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>());
            _quintile = Param(nameof(Quintile), 5);
            _minUsd = Param(nameof(MinTradeUsd), 200m);
            _tf = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame());
        }

        public override IEnumerable<(Security, DataType)> GetWorkingSecurities() =>
            Universe.Select(s => (s, CandleType));

        protected override void OnStarted(DateTimeOffset time)
        {
            base.OnStarted(time);
            var trigger = Universe.FirstOrDefault() ?? throw new InvalidOperationException("Universe empty.");

            SubscribeCandles(CandleType, true, trigger)
                .Bind(c =>
                {
                    var d = c.OpenTime.Date;
                    if (d == _lastDay)
                        return;
                    _lastDay = d;

                    if (IsQuarterRebalanceDay(d))
                        Rebalance();
                }).Start();
        }

        private bool IsQuarterRebalanceDay(DateTime d)
        {
            return (d.Month == 2 || d.Month == 5 || d.Month == 8 || d.Month == 11) &&
                   d.Day <= 3;
        }

        private void Rebalance()
        {
            var dens = new Dictionary<Security, decimal>();
            foreach (var s in Universe)
                if (TryGetLexicalDensity(s, out var val))
                    dens[s] = val;

            if (dens.Count < Quintile * 2)
                return;
            int bucket = dens.Count / Quintile;

            var longSide = dens.OrderByDescending(kv => kv.Value).Take(bucket).Select(kv => kv.Key).ToList();
            var shortSide = dens.OrderBy(kv => kv.Value).Take(bucket).Select(kv => kv.Key).ToList();

            _weights.Clear();
            decimal wl = 1m / longSide.Count;
            decimal ws = -1m / shortSide.Count;
            foreach (var s in longSide)
                _weights[s] = wl;
            foreach (var s in shortSide)
                _weights[s] = ws;

            foreach (var p in Positions.Keys.Where(s => !_weights.ContainsKey(s)))
                TradeTo(p, 0);

            foreach (var kv in _weights)
                TradeTo(kv.Key, kv.Value * Portfolio.CurrentValue / kv.Key.Price);
        }

        private void TradeTo(Security s, decimal tgt)
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
                Comment = "LexDensity"
            });
        }

        private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;
        private bool TryGetLexicalDensity(Security s, out decimal v) { v = 0; return false; }
    }
}