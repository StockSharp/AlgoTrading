// PairsTradingCountryETFsStrategy.cs
// -----------------------------------------------------------------------------
// Mean‑reversion pairs trade between two country ETFs.
// • Universe must contain exactly two ETFs {"A","B"}.
// • Calculate ratio = PriceA / PriceB.
// • Rolling window (WindowDays) for mean/std; compute z‑score.
// • Enter long A / short B when z < -EntryZ; opposite when z > EntryZ.
// • Exit when |z| < ExitZ.
// • All events triggered by each day's closed candle (no Schedule).
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
    public class PairsTradingCountryETFsStrategy : Strategy
    {
        private readonly StrategyParam<IEnumerable<Security>> _univ;
        private readonly StrategyParam<int> _window;
        private readonly StrategyParam<decimal> _entryZ;
        private readonly StrategyParam<decimal> _exitZ;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly DataType _tf = DataType.TimeFrame(TimeSpan.FromDays(1));

        public IEnumerable<Security> Universe { get => _univ.Value; set => _univ.Value = value; }
        public int WindowDays => _window.Value;
        public decimal EntryZ => _entryZ.Value;
        public decimal ExitZ => _exitZ.Value;
        public decimal MinTradeUsd => _minUsd.Value;

        private Security _a, _b;
        private readonly Queue<decimal> _ratio = new();
        private DateTime _last = DateTime.MinValue;

        public PairsTradingCountryETFsStrategy()
        {
            _univ = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>());
            _window = Param(nameof(WindowDays), 60);
            _entryZ = Param(nameof(EntryZ), 2m);
            _exitZ = Param(nameof(ExitZ), 0.5m);
            _minUsd = Param(nameof(MinTradeUsd), 200m);
        }

        public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
        {
            if (Universe.Count() != 2)
                throw new InvalidOperationException("Universe must contain exactly two ETFs.");
            _a = Universe.ElementAt(0);
            _b = Universe.ElementAt(1);
            yield return (_a, _tf);
            yield return (_b, _tf);
        }

        protected override void OnStarted(DateTimeOffset t)
        {
            base.OnStarted(t);
            SubscribeCandles(_a, _tf).Bind(CandleStates.Finished).Do(c => OnDaily()).Start();
        }

        private void OnDaily()
        {
            var pxA = _a.LastTrade?.Price ?? 0m;
            var pxB = _b.LastTrade?.Price ?? 0m;
            if (pxA == 0 || pxB == 0)
                return;

            var r = pxA / pxB;
            if (_ratio.Count == WindowDays)
                _ratio.Dequeue();
            _ratio.Enqueue(r);
            if (_ratio.Count < WindowDays)
                return;

            var mean = _ratio.Average();
            var sigma = (decimal)Math.Sqrt(_ratio.Select(x => Math.Pow((double)(x - mean), 2)).Average());
            if (sigma == 0)
                return;
            var z = (r - mean) / sigma;

            if (Math.Abs(z) < ExitZ)
            {
                Move(_a, 0);
                Move(_b, 0);
                return;
            }

            if (z > EntryZ)
            {
                // short A, long B
                Hedge(-1);
            }
            else if (z < -EntryZ)
            {
                // long A, short B
                Hedge(1);
            }
        }

        private void Hedge(int dir) // dir = +1  => long A short B
        {
            var equity = Portfolio.CurrentValue;
            var qty = equity / 2 / _a.Price;
            var qtyB = equity / 2 / _b.Price;
            Move(_a, dir * qty);
            Move(_b, -dir * qtyB);
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
                Comment = "PairsETF"
            });
        }
        private decimal PositionBy(Security s) => Positions.TryGetValue(s, out var q) ? q : 0m;
    }
}
