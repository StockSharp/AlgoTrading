
// AssetClassMomentumRotationalStrategy.cs  (revised)
// • Universe = IEnumerable<Security>
// • CandleType param
// • Momentum computed on daily candle close; rebalance on 1st trading day of month
// Date: 2 August 2025

using System;
using System.Collections.Generic;
using System.Linq;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    public class AssetClassMomentumRotationalStrategy : Strategy
    {
        #region Params
        private readonly StrategyParam<IEnumerable<Security>> _universe;
        private readonly StrategyParam<int> _rocLen;
        private readonly StrategyParam<int> _topN;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly StrategyParam<DataType> _candleType;

        public IEnumerable<Security> Universe { get => _universe.Value; set => _universe.Value = value; }
        public int RocLength => _rocLen.Value;
        public int TopN => _topN.Value;
        public decimal MinTradeUsd => _minUsd.Value;
        public DataType CandleType => _candleType.Value;
        #endregion

        private readonly Dictionary<Security, RateOfChange> _roc = new();
        private readonly HashSet<Security> _held = new();
        private DateTime _lastDay = DateTime.MinValue;

        public AssetClassMomentumRotationalStrategy()
        {
            _universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>());
            _rocLen = Param(nameof(RocLength), 252);
            _topN = Param(nameof(TopN), 3);
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
                var win = new RateOfChange { Length = RocLength };
                _roc[sec] = win;

                SubscribeCandles(sec, dt)
                    .Bind(CandleStates.Finished)
                    .Do(c =>
                    {
                        win.Process(c);
                        var d = c.OpenTime.Date;
                        if (d == _lastDay)
                            return;
                        _lastDay = d;
                        if (d.Day == 1 && Exchange.IsTradingDay(d))
                            TryRebalance();
                    })
                    .Start();
            }
        }

        private void TryRebalance()
        {
            var ready = _roc.Where(kv => kv.Value.IsFormed)
                            .ToDictionary(kv => kv.Key, kv => kv.Value.GetCurrentValue<decimal>());
            if (ready.Count < TopN)
                return;

            var selected = ready.OrderByDescending(kv => kv.Value).Take(TopN).Select(kv => kv.Key).ToHashSet();

            foreach (var sec in _held.Where(h => !selected.Contains(h)).ToList())
                Move(sec, 0);

            decimal capEach = Portfolio.CurrentValue / TopN;
            foreach (var sec in selected)
                Move(sec, capEach / sec.Price);

            _held.Clear();
            _held.UnionWith(selected);
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
                Comment = "ACMomentum"
            });
        }

        private decimal PositionBy(Security s) => Positions.TryGetValue(s, out var q) ? q : 0m;
    }
}