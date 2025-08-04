// TrendFollowingStocksStrategy.cs
// -----------------------------------------------------------------------------
// Breakout trend following: new all‑time high entry, ATR(10) trailing stop exit.
// Daily candles only; no Schedule().
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
    public class TrendFollowingStocksStrategy : Strategy
    {
        private readonly StrategyParam<IEnumerable<Security>> _universe;
        private readonly StrategyParam<int> _atrLen;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly DataType _tf = TimeSpan.FromDays(1).TimeFrame();

        private class StockInfo
        {
            public List<decimal> Close = new();
            public decimal Trail;
        }
        private readonly Dictionary<Security, StockInfo> _info = new();

        public IEnumerable<Security> Universe { get => _universe.Value; set => _universe.Value = value; }
        public int AtrLen => _atrLen.Value;
        public decimal MinTradeUsd => _minUsd.Value;

        public TrendFollowingStocksStrategy()
        {
            _universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>());
            _atrLen = Param(nameof(AtrLen), 10);
            _minUsd = Param(nameof(MinTradeUsd), 200m);
        }

        public override IEnumerable<(Security, DataType)> GetWorkingSecurities() =>
            Universe.Select(s => (s, _tf));

        protected override void OnStarted(DateTimeOffset t)
        {
            base.OnStarted(t);
            foreach (var (s, tf) in GetWorkingSecurities())
            {
                _info[s] = new StockInfo();
                SubscribeCandles(tf, true, s)
                    .Bind(c => OnDaily(s, c))
                    .Start();
            }
        }

        private void OnDaily(Security s, ICandleMessage c)
        {
            var data = _info[s];
            data.Close.Add(c.ClosePrice);
            if (data.Close.Count > 3000)
                data.Close.RemoveAt(0);

            if (data.Close.Count < AtrLen + 1)
                return;

            var atr = data.Close.Zip(data.Close.Skip(1), (p0, p1) => Math.Abs(p0 - p1))
                                .TakeLast(AtrLen).Average();
            var trailCandidate = c.ClosePrice - atr;

            // Entry
            if (c.ClosePrice >= data.Close.Max() && PositionBy(s) == 0)
            {
                data.Trail = trailCandidate;
                Move(s, Portfolio.CurrentValue / Universe.Count() / c.ClosePrice);
            }

            // Exit
            if (PositionBy(s) > 0 && c.ClosePrice <= data.Trail)
                Move(s, 0);

            // Update trailing stop upwards
            if (PositionBy(s) > 0 && trailCandidate > data.Trail)
                data.Trail = trailCandidate;
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
                Comment = "TrendFollow"
            });
        }

        private decimal PositionBy(Security s) =>
            GetPositionValue(s, Portfolio) ?? 0;
    }
}