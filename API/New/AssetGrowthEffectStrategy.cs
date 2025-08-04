// AssetGrowthEffectStrategy.cs (revised, candle-driven)
// Annual rebalance when first finished candle in July detected (after June close).
// Params: Universe, CandleType, etc.
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
    public class AssetGrowthEffectStrategy : Strategy
    {
        private readonly StrategyParam<IEnumerable<Security>> _universe;
        private readonly StrategyParam<int> _quant;
        private readonly StrategyParam<decimal> _lev;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly StrategyParam<DataType> _candleType;

        public IEnumerable<Security> Universe { get => _universe.Value; set => _universe.Value = value; }
        public int Quantiles => _quant.Value;
        public decimal Leverage => _lev.Value;
        public decimal MinTradeUsd => _minUsd.Value;
        public DataType CandleType => _candleType.Value;

        private readonly Dictionary<Security, decimal> _prev = new();
        private readonly Dictionary<Security, decimal> _w = new();
        private DateTime _lastDay = DateTime.MinValue;

        public AssetGrowthEffectStrategy()
        {
            _universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>());
            _quant = Param(nameof(Quantiles), 10);
            _lev = Param(nameof(Leverage), 1m);
            _minUsd = Param(nameof(MinTradeUsd), 50m);
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
                        if (d.Month == 7 && d.Day == 1)
                            Rebalance();
                    })
                    .Start();
            }
        }

        private void Rebalance()
        {
            var growth = new Dictionary<Security, decimal>();
            foreach (var s in Universe)
            {
                if (!TryGetTotalAssets(s, out var tot))
                    continue;
                if (_prev.TryGetValue(s, out var prev) && prev > 0)
                    growth[s] = (tot - prev) / prev;
                _prev[s] = tot;
            }

            if (growth.Count < Quantiles * 2)
                return;
            int qlen = growth.Count / Quantiles;
            var sorted = growth.OrderBy(kv => kv.Value).ToList();
            var longs = sorted.Take(qlen).Select(kv => kv.Key).ToList();
            var shorts = sorted.Skip(growth.Count - qlen).Select(kv => kv.Key).ToList();

            _w.Clear();
            decimal wl = Leverage / longs.Count;
            decimal ws = -Leverage / shorts.Count;
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
            RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "AssetGrowth" });
        }

        private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;

        private bool TryGetTotalAssets(Security s, out decimal tot) { tot = 0; return false; }
    }
}