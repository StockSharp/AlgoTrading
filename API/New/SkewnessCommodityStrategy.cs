// SkewnessCommodityStrategy.cs
// -----------------------------------------------------------------------------
// Compute skewness of daily returns over WindowDays for each futures contract.
// Long TopN most negative-skew, short TopN most positive-skew.
// Monthly rebalance on the first trading day, triggered by candle-stream
// (SubscribeCandles → Bind(CandleStates.Finished) → OnDaily).
// No Schedule() is used.
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
    public class SkewnessCommodityStrategy : Strategy
    {
        #region Parameters
        private readonly StrategyParam<IEnumerable<Security>> _futures;
        private readonly StrategyParam<int> _window;
        private readonly StrategyParam<int> _topN;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly DataType _tf = DataType.TimeFrame(TimeSpan.FromDays(1));

        public IEnumerable<Security> Futures { get => _futures.Value; set => _futures.Value = value; }
        public int WindowDays => _window.Value;
        public int TopN => _topN.Value;
        public decimal MinTradeUsd => _minUsd.Value;
        #endregion

        // rolling windows of prices
        private readonly Dictionary<Security, Queue<decimal>> _px = new();
        private DateTime _lastProcessed = DateTime.MinValue;
        private readonly Dictionary<Security, decimal> _weight = new();

        public SkewnessCommodityStrategy()
        {
            _futures = Param<IEnumerable<Security>>(nameof(Futures), Array.Empty<Security>());
            _window = Param(nameof(WindowDays), 120);
            _topN = Param(nameof(TopN), 5);
            _minUsd = Param(nameof(MinTradeUsd), 200m);
        }

        public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() =>
            Futures.Select(f => (f, _tf));

        protected override void OnStarted(DateTimeOffset time)
        {
            base.OnStarted(time);

            foreach (var (s, tf) in GetWorkingSecurities())
            {
                _px[s] = new Queue<decimal>();
                SubscribeCandles(s, tf)
                    .Bind(CandleStates.Finished)
                    .Do(c => OnDaily((Security)c.SecurityId, c))
                    .Start();
            }
        }

        private void OnDaily(Security s, ICandleMessage candle)
        {
            var q = _px[s];
            if (q.Count == WindowDays + 1)
                q.Dequeue();
            q.Enqueue(candle.ClosePrice);

            var d = candle.OpenTime.Date;
            if (d == _lastProcessed)
                return;
            _lastProcessed = d;

            if (d.Day != 1 || !Exchange.IsTradingDay(d))
                return;

            Rebalance();
        }

        private void Rebalance()
        {
            var skew = new Dictionary<Security, double>();

            foreach (var kv in _px)
            {
                var q = kv.Value;
                if (q.Count < WindowDays + 1)
                    continue;

                var arr = q.ToArray();
                var ret = new double[arr.Length - 1];
                for (int i = 1; i < arr.Length; i++)
                    ret[i - 1] = (double)((arr[i] - arr[i - 1]) / arr[i - 1]);

                var mean = ret.Average();
                var sd = Math.Sqrt(ret.Select(r => (r - mean) * (r - mean)).Average());
                if (sd == 0)
                    continue;

                var sk = ret.Select(r => Math.Pow((r - mean) / sd, 3)).Average();
                skew[kv.Key] = sk;
            }

            if (skew.Count < TopN * 2)
                return;

            var longSide = skew.OrderBy(kv => kv.Value).Take(TopN).Select(kv => kv.Key).ToList();        // most negative
            var shortSide = skew.OrderByDescending(kv => kv.Value).Take(TopN).Select(kv => kv.Key).ToList();

            _weight.Clear();
            decimal wl = 1m / longSide.Count;
            decimal ws = -1m / shortSide.Count;
            foreach (var s in longSide)
                _weight[s] = wl;
            foreach (var s in shortSide)
                _weight[s] = ws;

            foreach (var pos in Positions.Keys.Where(sec => !_weight.ContainsKey(sec)))
                TradeTo(pos, 0);

            foreach (var kv in _weight)
                TradeTo(kv.Key, kv.Value * Portfolio.CurrentValue / kv.Key.Price);
        }

        private void TradeTo(Security s, decimal tgtQty)
        {
            var diff = tgtQty - PositionBy(s);
            if (Math.Abs(diff) * s.Price < MinTradeUsd)
                return;

            RegisterOrder(new Order
            {
                Security = s,
                Portfolio = Portfolio,
                Direction = diff > 0 ? Sides.Buy : Sides.Sell,
                Volume = Math.Abs(diff),
                Type = OrderTypes.Market,
                Comment = "SkewCom"
            });
        }

        private decimal PositionBy(Security s) =>
            Positions.TryGetValue(s, out var v) ? v : 0m;
    }
}
