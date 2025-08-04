// FedModelStrategy.cs
// ----------------------------------------------------------------------------
// Fed‑Model yield‑gap timing (Quantpedia #21):
//     • Compare S&P‑500 earnings yield with 10‑year Treasury yield.
//     • Monthly: if one‑month forecast of excess return > 0 → long equity index,
//       else move to cash ETF.
//     • Uses daily candles only to detect first trading day of month; all
//       macro data must come from external feed stubs.
// ----------------------------------------------------------------------------
// Parameters
// ----------
// Universe[0]  : equity index ETF (long leg)
// Universe[1]  : cash proxy ETF  (optional)
// BondYieldSym : 10‑year yield series (Security)
// EarnYieldSym : earnings‑yield series (Security)
// RegressionMonths (default 12)
// ----------------------------------------------------------------------------
// Date: 2 Aug 2025
// ----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    public class FedModelStrategy : Strategy
    {
        #region Params
        private readonly StrategyParam<IEnumerable<Security>> _univ;
        private readonly StrategyParam<Security> _bond;
        private readonly StrategyParam<Security> _earn;
        private readonly StrategyParam<int> _months;
        private readonly StrategyParam<DataType> _tf;
        private readonly StrategyParam<decimal> _minUsd;

        public IEnumerable<Security> Universe { get => _univ.Value; set => _univ.Value = value; }
        public Security BondYieldSym { get => _bond.Value; set => _bond.Value = value; }
        public Security EarningsYieldSym { get => _earn.Value; set => _earn.Value = value; }
        public int RegressionMonths => _months.Value;
        public DataType CandleType => _tf.Value;
        public decimal MinTradeUsd => _minUsd.Value;
        #endregion

        private readonly RollingWin _eq = new();
        private readonly RollingWin _gap = new();
        private readonly RollingWin _rf = new();
        private DateTime _lastMonth = DateTime.MinValue;

        public FedModelStrategy()
        {
            _univ = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>());
            _bond = Param<Security>(nameof(BondYieldSym), null);
            _earn = Param<Security>(nameof(EarningsYieldSym), null);
            _months = Param(nameof(RegressionMonths), 12);
            _tf = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame());
            _minUsd = Param(nameof(MinTradeUsd), 200m);

            int n = RegressionMonths + 1;
            _eq.SetSize(n);
            _gap.SetSize(n);
            _rf.SetSize(n);
        }

        public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
        {
            foreach (var s in Universe)
                yield return (s, CandleType);
            if (BondYieldSym != null)
                yield return (BondYieldSym, CandleType);
            if (EarningsYieldSym != null)
                yield return (EarningsYieldSym, CandleType);
        }

        protected override void OnStarted(DateTimeOffset t)
        {
            base.OnStarted(t);
            foreach (var (s, tf) in GetWorkingSecurities())
                SubscribeCandles(tf, true, s)
                    .Bind(c => OnDaily(c))
                    .Start();
        }

        private void OnDaily(ICandleMessage c)
        {
            var d = c.OpenTime.Date;
            if (d.Day != 1 || _lastMonth == d)
                return;
            _lastMonth = d;

            if (c.SecurityId != Universe.First())
                return;

            _eq.Add(c.ClosePrice);
            _rf.Add(GetRF(d));

            var gap = GetYieldGap(d);
            if (gap == null)
                return;
            _gap.Add(gap.Value);

            if (!_eq.Full || !_gap.Full)
                return;

            var x = _gap.Data;
            var yret = new decimal[_eq.Size - 1];
            for (int i = 1; i < _eq.Size; i++)
                yret[i - 1] = (_eq.Data[i] - _eq.Data[i - 1]) / _eq.Data[i - 1] - _rf.Data[i - 1];

            int n = yret.Length;
            decimal meanX = x.Take(n).Average();
            decimal meanY = yret.Average();
            decimal cov = 0, varX = 0;
            for (int i = 0; i < n; i++)
            {
                var dx = x[i] - meanX;
                cov += dx * (yret[i] - meanY);
                varX += dx * dx;
            }
            if (varX == 0)
                return;
            var beta = cov / varX;
            var alpha = meanY - beta * meanX;
            var forecast = alpha + beta * x[^1];

            var equity = Universe.First();
            var cash = Universe.ElementAtOrDefault(1);

            if (forecast > 0)
            {
                Move(equity, 1m);
                if (cash != null)
                    Move(cash, 0);
            }
            else
            {
                Move(equity, 0);
                if (cash != null)
                    Move(cash, 1m);
            }
        }

        private void Move(Security s, decimal weight)
        {
            if (s == null)
                return;
            var tgt = weight * Portfolio.CurrentValue / s.Price;
            var diff = tgt - Pos(s);
            if (Math.Abs(diff) * s.Price < MinTradeUsd)
                return;

            RegisterOrder(new Order
            {
                Security = s,
                Portfolio = Portfolio,
                Side = diff > 0 ? Sides.Buy : Sides.Sell,
                Volume = Math.Abs(diff),
                Type = OrderTypes.Market,
                Comment = "FedModel"
            });
        }

        private decimal Pos(Security s) => GetPositionValue(s, Portfolio) ?? 0;

        private decimal GetRF(DateTime d) => 0.0002m;

        private decimal? GetYieldGap(DateTime d)
        {
            if (!SeriesVal(EarningsYieldSym, d, out var ey))
                return null;
            if (!SeriesVal(BondYieldSym, d, out var y10))
                return null;
            return ey - y10;
        }

        private bool SeriesVal(Security s, DateTime d, out decimal v) { v = 0; return false; }

        private class RollingWin
        {
            public decimal[] Data;
            public int Size => Data.Length;
            private int _n;
            public bool Full => _n == Data.Length;
            public void SetSize(int n) { Data = new decimal[n]; _n = 0; }
            public void Add(decimal v) { if (_n < Data.Length) _n++; for (int i = Data.Length - 1; i > 0; i--) Data[i] = Data[i - 1]; Data[0] = v; }
        }
    }
}