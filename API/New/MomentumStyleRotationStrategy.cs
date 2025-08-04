// MomentumStyleRotationStrategy.cs
// -----------------------------------------------------------------------------
// Rotates among Factor ETFs (e.g., Momentum, Value, Quality) and Market ETF
// based on trailing 3‑month performance ranking.  Monthly rebalance.
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
    public class MomentumStyleRotationStrategy : Strategy
    {
        #region Params
        private readonly StrategyParam<IEnumerable<Security>> _factors;
        private readonly StrategyParam<Security> _market;
        private readonly StrategyParam<int> _look;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly StrategyParam<DataType> _tf;

        public IEnumerable<Security> FactorETFs { get => _factors.Value; set => _factors.Value = value; }
        public Security MarketETF { get => _market.Value; set => _market.Value = value; }
        public int LookbackDays => _look.Value;
        public decimal MinTradeUsd => _minUsd.Value;
        public DataType CandleType => _tf.Value;
        #endregion

        private readonly Dictionary<Security, RollingWin> _px = new();
        private readonly Dictionary<Security, decimal> _latestPrices = new();
        private DateTime _lastDay = DateTime.MinValue;

        public MomentumStyleRotationStrategy()
        {
            _factors = Param<IEnumerable<Security>>(nameof(FactorETFs), Array.Empty<Security>());
            _market = Param<Security>(nameof(MarketETF), null);
            _look = Param(nameof(LookbackDays), 63);
            _minUsd = Param(nameof(MinTradeUsd), 200m);
            _tf = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame());
        }

        public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
        {
            foreach (var s in FactorETFs)
                yield return (s, CandleType);
            if (MarketETF != null)
                yield return (MarketETF, CandleType);
        }

        protected override void OnStarted(DateTimeOffset t)
        {
            base.OnStarted(t);
            foreach (var (s, tf) in GetWorkingSecurities())
            {
                _px[s] = new RollingWin(LookbackDays + 1);
                SubscribeCandles(tf, true, s)
                    .Bind(c => ProcessCandle(c, s))
                    .Start();
            }
        }

        private void ProcessCandle(ICandleMessage candle, Security security)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Store the latest closing price for this security
            _latestPrices[security] = candle.ClosePrice;

            OnDaily(security, candle);
        }

        private void OnDaily(Security s, ICandleMessage c)
        {
            _px[s].Add(c.ClosePrice);
            var d = c.OpenTime.Date;
            if (d == _lastDay)
                return;
            _lastDay = d;
            if (d.Day != 1)
                return;
            Rebalance();
        }

        private void Rebalance()
        {
            var perf = new Dictionary<Security, decimal>();
            foreach (var kv in _px)
                if (kv.Value.Full)
                    perf[kv.Key] = (kv.Value.Data[0] - kv.Value.Data[^1]) / kv.Value.Data[^1];

            if (perf.Count == 0)
                return;
            var best = perf.OrderByDescending(kv => kv.Value).First().Key;
            
            foreach (var position in Positions)
                if (position.Security != best)
                    Move(position.Security, 0);
                    
            var portfolioValue = Portfolio.CurrentValue ?? 0m;
            var price = GetLatestPrice(best);
            if (price > 0)
                Move(best, portfolioValue / price);
        }

        private decimal GetLatestPrice(Security security)
        {
            return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
        }

        private void Move(Security s, decimal tgt)
        {
            var diff = tgt - PositionBy(s);
            var price = GetLatestPrice(s);
            if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd)
                return;
            RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "StyleRot" });
        }
        private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;

        private class RollingWin
        {
            private readonly Queue<decimal> _q = new(); private readonly int _n;
            public RollingWin(int n) { _n = n; }
            public bool Full => _q.Count == _n;
            public void Add(decimal p) { if (_q.Count == _n) _q.Dequeue(); _q.Enqueue(p); }
            public decimal[] Data => _q.ToArray();
        }
    }
}
