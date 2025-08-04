// FXCarryTradeStrategy.cs — candle-triggered
// Long TopK carry currencies, short BottomK. Rebalanced on FIRST trading day
// of month using daily candle of the first currency only (no Schedule).
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
    public class FXCarryTradeStrategy : Strategy
    {
        private readonly StrategyParam<IEnumerable<Security>> _univ;
        private readonly StrategyParam<int> _topK;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly StrategyParam<DataType> _tf;

        public IEnumerable<Security> Universe { get => _univ.Value; set => _univ.Value = value; }
        public int TopK => _topK.Value;
        public decimal MinTradeUsd => _minUsd.Value;
        public DataType CandleType => _tf.Value;

        private readonly Dictionary<Security, decimal> _weights = new();
        private DateTime _lastDay = DateTime.MinValue;

        public FXCarryTradeStrategy()
        {
            _univ = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>());
            _topK = Param(nameof(TopK), 3);
            _minUsd = Param(nameof(MinTradeUsd), 200m);
            _tf = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame());
        }

        public override IEnumerable<(Security, DataType)> GetWorkingSecurities() =>
            Universe.Select(s => (s, CandleType));

        protected override void OnStarted(DateTimeOffset time)
        {
            base.OnStarted(time);
            var first = Universe.FirstOrDefault() ?? throw new InvalidOperationException("Universe empty.");

            // Use ONLY ONE currency's daily candle to trigger monthly rebalance
            SubscribeCandles(CandleType, true, first)
                .Bind(c =>
                {
                    var day = c.OpenTime.Date;
                    if (day == _lastDay)
                        return;
                    _lastDay = day;

                    if (day.Day == 1)
                        Rebalance();
                }).Start();
        }

        private void Rebalance()
        {
            var carry = new Dictionary<Security, decimal>();
            foreach (var fx in Universe)
                if (TryGetCarry(fx, out var c))
                    carry[fx] = c;

            if (carry.Count < TopK * 2)
                return;

            var top = carry.OrderByDescending(kv => kv.Value).Take(TopK).Select(kv => kv.Key).ToList();
            var bot = carry.OrderBy(kv => kv.Value).Take(TopK).Select(kv => kv.Key).ToList();

            _weights.Clear();
            decimal wl = 1m / top.Count;
            decimal ws = -1m / bot.Count;
            foreach (var s in top)
                _weights[s] = wl;
            foreach (var s in bot)
                _weights[s] = ws;

            foreach (var pos in Positions.Keys.Where(s => !_weights.ContainsKey(s)))
                Move(pos, 0);

            foreach (var kv in _weights)
                Move(kv.Key, kv.Value * Portfolio.CurrentValue / kv.Key.Price);
        }

        private void Move(Security s, decimal tgtQty)
        {
            var diff = tgtQty - PositionBy(s);
            if (Math.Abs(diff) * s.Price < MinTradeUsd)
                return;
            RegisterOrder(new Order
            {
                Security = s,
                Portfolio = Portfolio,
                Side = diff > 0 ? Sides.Buy : Sides.Sell,
                Volume = Math.Abs(diff),
                Type = OrderTypes.Market,
                Comment = "FXCarry"
            });
        }

        private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;
        private bool TryGetCarry(Security s, out decimal carry) { carry = 0; return false; }
    }
}