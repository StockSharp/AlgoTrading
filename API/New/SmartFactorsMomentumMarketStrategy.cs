
// SmartFactorsMomentumMarketStrategy.cs
// Smart factors momentum blended with market; monthly rotation.
// Date: 2 August 2025

using System;
using System.Collections.Generic;
using System.Linq;
using Ecng.Common;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    public class SmartFactorsMomentumMarketStrategy : Strategy
    {
        private readonly StrategyParam<Dictionary<string, Security>> _factors;
        private readonly StrategyParam<Security> _market;
        private readonly StrategyParam<int> _fastM;
        private readonly StrategyParam<int> _slowM;
        private readonly StrategyParam<int> _maM;
        private readonly StrategyParam<decimal> _minUsd;

        public Dictionary<string, Security> Factors { get => _factors.Value; set => _factors.Value = value; }
        public Security MarketETF { get => _market.Value; set => _market.Value = value; }
        public int FastMonths => _fastM.Value; public int SlowMonths => _slowM.Value; public int MaMonths => _maM.Value;
        public decimal MinTradeUsd => _minUsd.Value;

        private readonly Dictionary<Security, RollingWindow<decimal>> _p = new();
        private readonly RollingWindow<decimal> _smartRet;
        private readonly RollingWindow<decimal> _mktRet;

        public SmartFactorsMomentumMarketStrategy()
        {
            _factors = Param(nameof(Factors), new Dictionary<string, Security>());
            _market = Param<Security>(nameof(MarketETF), null);
            _fastM = Param(nameof(FastMonths), 1);
            _slowM = Param(nameof(SlowMonths), 12);
            _maM = Param(nameof(MaMonths), 12);
            _minUsd = Param(nameof(MinTradeUsd), 50m);

            _smartRet = new RollingWindow<decimal>(_maM.Value);
            _mktRet = new RollingWindow<decimal>(_maM.Value);
        }

        public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
        {
            if (MarketETF is null)
                throw new InvalidOperationException("MarketETF not set");
            if (!Factors.Any())
                throw new InvalidOperationException("No factors");

            var tf = TimeSpan.FromDays(1).TimeFrame();
            return Factors.Values.Append(MarketETF).Select(s => (s, tf));
        }

        protected override void OnStarted(DateTimeOffset time)
        {
            base.OnStarted(time);
            foreach (var (sec, dt) in GetWorkingSecurities())
            {
                SubscribeCandles(dt, true, sec)
                    .Bind(OnCandleFinished)
                    .Start();

                _p[sec] = new RollingWindow<decimal>(Math.Max(SlowMonths * 21 + 1, 260));
            }
        }

        private void OnCandleFinished(ICandleMessage candle)
        {
            var sec = (Security)candle.SecurityId;
            if (_p.TryGetValue(sec, out var win))
                win.Add(candle.ClosePrice);
        }

        private void Rebalance()
        {
            if (_p.Any(kv => !kv.Value.IsFull()))
                return;

            var fastSig = new Dictionary<Security, decimal>();
            var slowSig = new Dictionary<Security, decimal>();
            foreach (var sec in Factors.Values)
            {
                var win = _p[sec];
                fastSig[sec] = (win.Last() - win[^(FastMonths * 21 + 1)]) / win[^(FastMonths * 21 + 1)];
                slowSig[sec] = (win.Last() - win[^(SlowMonths * 21 + 1)]) / win[^(SlowMonths * 21 + 1)];
            }

            int rankSum = Enumerable.Range(1, Factors.Count).Sum();
            var wFast = RankWeights(fastSig, rankSum);
            var wSlow = RankWeights(slowSig, rankSum);

            var wTotal = Factors.Values.ToDictionary(s => s, s => 0.75m * wFast[s] + 0.25m * wSlow[s]);

            var smart1M = wTotal.Sum(kv => kv.Value * fastSig[kv.Key]);
            var mkt1M = (_p[MarketETF].Last() - _p[MarketETF][^22]) / _p[MarketETF][^22];
            _smartRet.Add(smart1M);
            _mktRet.Add(mkt1M);
            if (!_smartRet.IsFull())
                return;

            int scoreSmart = 0, scoreMkt = 0;
            for (int i = 1; i <= MaMonths; i++)
            {
                var smaS = _smartRet.Take(i).Average();
                var smaM = _mktRet.Take(i).Average();
                if (smaS > smaM)
                    scoreSmart++;
                else
                    scoreMkt++;
            }
            decimal wSmart = scoreSmart / (decimal)MaMonths;
            decimal wMarket = scoreMkt / (decimal)MaMonths;

            TradeToTarget(MarketETF, wMarket);
            foreach (var kv in wTotal)
                TradeToTarget(kv.Key, wSmart * kv.Value);
            foreach (var pos in Positions.Keys.Where(s => s != MarketETF && !wTotal.ContainsKey(s)))
                TradeToTarget(pos, 0m);
        }

        private Dictionary<Security, decimal> RankWeights(Dictionary<Security, decimal> sig, int rankSum)
        {
            var sorted = sig.OrderBy(kv => Math.Abs(kv.Value)).ToList();
            var d = new Dictionary<Security, decimal>();
            for (int i = 0; i < sorted.Count; i++)
                d[sorted[i].Key] = (i + 1) / (decimal)rankSum * Math.Sign(sorted[i].Value);
            return d;
        }

        private void TradeToTarget(Security sec, decimal weight)
        {
            var tgt = weight * Portfolio.CurrentValue / sec.Price;
            var diff = tgt - PositionBy(sec);
            if (Math.Abs(diff) * sec.Price >= MinTradeUsd)
                RegisterOrder(new Order { Security = sec, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "SmartFactors" });
        }

        private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;
    }

    #region RollingWindow helper
    public class RollingWindow<T>
    {
        private readonly Queue<T> _q = new(); private readonly int _size;
        public RollingWindow(int size) { _size = size; }
        public void Add(T v) { if (_q.Count == _size) _q.Dequeue(); _q.Enqueue(v); }
        public bool IsFull() => _q.Count == _size;
        public T Last() => _q.Last();
        public T this[int idx] => _q.ElementAt(idx);
        public IEnumerable<T> Take(int n) => _q.Reverse().Take(n);
    }
    #endregion
}