// ShortTermReversalStocksStrategy.cs
// -----------------------------------------------------------------------------
// Prior-week reversal: long losers, short winners among Universe.
// Weekly rebalance triggered by Monday's first closed daily candle.
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
    public class ShortTermReversalStocksStrategy : Strategy
    {
        private readonly StrategyParam<IEnumerable<Security>> _univ;
        private readonly StrategyParam<int> _look;
        private readonly StrategyParam<decimal> _min;
        private readonly DataType _tf = TimeSpan.FromDays(1).TimeFrame();
        private readonly Dictionary<Security, Queue<decimal>> _px = new();
        private readonly Dictionary<Security, decimal> _w = new();
        private readonly Dictionary<Security, decimal> _latestPrices = new();
        private DateTime _last = DateTime.MinValue;
        public IEnumerable<Security> Universe { get => _univ.Value; set => _univ.Value = value; }
        public int LookbackDays => _look.Value;
        public decimal MinTradeUsd => _min.Value;
        public ShortTermReversalStocksStrategy()
        {
            _univ = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>());
            _look = Param(nameof(LookbackDays), 5);
            _min = Param(nameof(MinTradeUsd), 200m);
        }
        public override IEnumerable<(Security, DataType)> GetWorkingSecurities() => Universe.Select(s => (s, _tf));
        protected override void OnStarted(DateTimeOffset t)
        {
            base.OnStarted(t);
            foreach (var (s, tf) in GetWorkingSecurities())
            {
                _px[s] = new Queue<decimal>();
                SubscribeCandles(tf, true, s).Bind(c => ProcessCandle(c, s)).Start();
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
            var q = _px[s];
            if (q.Count == LookbackDays + 1)
                q.Dequeue();
            q.Enqueue(c.ClosePrice);
            var d = c.OpenTime.Date;
            if (d == _last)
                return;
            _last = d;
            if (d.DayOfWeek != DayOfWeek.Monday)
                return;
            Rebalance();
        }
        private void Rebalance()
        {
            var perf = new Dictionary<Security, decimal>();
            foreach (var kv in _px)
                if (kv.Value.Count == LookbackDays + 1)
                    perf[kv.Key] = (kv.Value.Peek() - kv.Value.Last()) / kv.Value.Last();
            if (perf.Count < 10)
                return;
            int bucket = perf.Count / 10;
            var losers = perf.OrderBy(kv => kv.Value).Take(bucket).Select(kv => kv.Key).ToList();
            var winners = perf.OrderByDescending(kv => kv.Value).Take(bucket).Select(kv => kv.Key).ToList();
            _w.Clear();
            decimal wl = 1m / losers.Count, ws = -1m / winners.Count;
            foreach (var s in losers)
                _w[s] = wl;
            foreach (var s in winners)
                _w[s] = ws;
            foreach (var position in Positions.Where(p => !_w.ContainsKey(p.Security)))
                Move(position.Security, 0);
            var portfolioValue = Portfolio.CurrentValue ?? 0m;
            foreach (var kv in _w)
            {
                var price = GetLatestPrice(kv.Key);
                if (price > 0)
                    Move(kv.Key, kv.Value * portfolioValue / price);
            }
        }
        private void Move(Security s, decimal tgt) 
        { 
            var diff = tgt - Pos(s); 
            var price = GetLatestPrice(s);
            if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd) 
                return; 
            RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "STRStock" }); 
        }

        private decimal GetLatestPrice(Security security)
        {
            return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
        }

        private decimal Pos(Security s) => GetPositionValue(s, Portfolio) ?? 0;
    }
}