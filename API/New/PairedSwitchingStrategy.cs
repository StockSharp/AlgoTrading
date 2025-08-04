// PairedSwitchingStrategy.cs
// -----------------------------------------------------------------------------
// Each quarter hold the ETF (of two) with higher previous‑quarter return.
// Quarter = calendar quarter.  Rebalance on first trading day of Jan/Apr/Jul/Oct.
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
    public class PairedSwitchingStrategy : Strategy
    {
        private readonly StrategyParam<Security> _first;
        private readonly StrategyParam<Security> _second;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly StrategyParam<DataType> _tf;
        public Security FirstETF { get => _first.Value; set => _first.Value = value; }
        public Security SecondETF { get => _second.Value; set => _second.Value = value; }
        public decimal MinTradeUsd => _minUsd.Value;
        public DataType CandleType => _tf.Value;

        private readonly RollingWin _p1 = new(63 + 1); 
        private readonly RollingWin _p2 = new(63 + 1);
        private readonly Dictionary<Security, decimal> _latestPrices = new();
        private DateTime _last = DateTime.MinValue;

        public PairedSwitchingStrategy()
        {
            _first = Param<Security>(nameof(FirstETF), null);
            _second = Param<Security>(nameof(SecondETF), null);
            _minUsd = Param(nameof(MinTradeUsd), 200m);
            _tf = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame());
        }

        public override IEnumerable<(Security, DataType)> GetWorkingSecurities() =>
            new[] { (FirstETF, CandleType), (SecondETF, CandleType) };

        protected override void OnStarted(DateTimeOffset t)
        {
            base.OnStarted(t);
            SubscribeCandles(CandleType, true, FirstETF).Bind(c => ProcessCandle(c, FirstETF, true)).Start();
            SubscribeCandles(CandleType, true, SecondETF).Bind(c => ProcessCandle(c, SecondETF, false)).Start();
        }

        private void ProcessCandle(ICandleMessage candle, Security security, bool isFirst)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Store the latest closing price for this security
            _latestPrices[security] = candle.ClosePrice;

            OnDaily(isFirst, candle);
        }

        private void OnDaily(bool first, ICandleMessage c)
        {
            (first ? _p1 : _p2).Add(c.ClosePrice);
            var d = c.OpenTime.Date;
            if (d == _last)
                return;
            _last = d;
            if (!(d.Month % 3 == 1 && d.Day == 1))
                return; // first trading day quarter
            Rebalance();
        }

        private void Rebalance()
        {
            if (!_p1.Full || !_p2.Full)
                return;
            var r1 = (_p1.Data[0] - _p1.Data[^1]) / _p1.Data[^1];
            var r2 = (_p2.Data[0] - _p2.Data[^1]) / _p2.Data[^1];
            var longEtf = r1 > r2 ? FirstETF : SecondETF;
            var other = r1 > r2 ? SecondETF : FirstETF;
            
            var portfolioValue = Portfolio.CurrentValue ?? 0m;
            var longPrice = GetLatestPrice(longEtf);
            if (longPrice > 0)
                Move(longEtf, portfolioValue / longPrice);
            Move(other, 0);
        }

        private decimal Pos(Security s) => GetPositionValue(s, Portfolio) ?? 0;

        private decimal GetLatestPrice(Security security)
        {
            return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
        }

        private void Move(Security s, decimal tgt)
        {
            var diff = tgt - Pos(s);
            var price = GetLatestPrice(s);
            if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd)
                return;
            RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "PairSwitch" });
        }

        private class RollingWin { private readonly Queue<decimal> _q = new(); private readonly int _n; public RollingWin(int n) { _n = n; } public bool Full => _q.Count == _n; public void Add(decimal p) { if (_q.Count == _n) _q.Dequeue(); _q.Enqueue(p); } public decimal[] Data => _q.ToArray(); }
    }
}
