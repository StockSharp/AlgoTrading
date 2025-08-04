
// CrudeOilPredictsEquityStrategy.cs (daily candles version)
// If last-month oil return > 0, invest in equity ETF, else stay in cash ETF.
// Date: 2 August 2025

using System;
using System.Collections.Generic;
using System.Linq;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    public class CrudeOilPredictsEquityStrategy : Strategy
    {
        private readonly StrategyParam<Security> _equity;
        private readonly StrategyParam<Security> _oil;
        private readonly StrategyParam<Security> _cash;
        private readonly StrategyParam<DataType> _tf;
        private readonly StrategyParam<int> _lookback;

        public Security Equity { get => _equity.Value; set => _equity.Value = value; }
        public Security Oil { get => _oil.Value; set => _oil.Value = value; }
        public Security CashEtf { get => _cash.Value; set => _cash.Value = value; }
        public DataType CandleType => _tf.Value;
        public int Lookback => _lookback.Value;

        private readonly Dictionary<Security, RollingWindow<decimal>> _wins = new();
        private DateTime _lastDay = DateTime.MinValue;

        public CrudeOilPredictsEquityStrategy()
        {
            _equity = Param<Security>(nameof(Equity), null);
            _oil = Param<Security>(nameof(Oil), null);
            _cash = Param<Security>(nameof(CashEtf), null);
            _tf = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame());
            _lookback = Param(nameof(Lookback), 22); // 1 month
        }

        public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
        {
            if (Equity == null || Oil == null || CashEtf == null)
                throw new InvalidOperationException("Set securities");
            return new[] { (Equity, CandleType), (Oil, CandleType), (CashEtf, CandleType) };
        }

        protected override void OnStarted(DateTimeOffset time)
        {
            base.OnStarted(time);
            foreach (var (s, dt) in GetWorkingSecurities())
            {
                _wins[s] = new RollingWindow<decimal>(Lookback + 1);
                SubscribeCandles(s, dt)
                    .Bind(CandleStates.Finished)
                    .Do(c =>
                    {
                        _wins[s].Add(c.ClosePrice);
                        var d = c.OpenTime.Date;
                        if (d == _lastDay)
                            return;
                        _lastDay = d;
                        if (d.Day == 1 && Exchange.IsTradingDay(d))
                            Rebalance();
                    })
                    .Start();
            }
        }

        private void Rebalance()
        {
            if (!_wins[Oil].IsFull())
                return;
            var oilRet = (_wins[Oil].Last() - _wins[Oil][0]) / _wins[Oil][0];
            if (oilRet > 0)
                MoveTo(Equity);
            else
                MoveTo(CashEtf);
        }

        private void MoveTo(Security target)
        {
            foreach (var pos in Positions.Keys.Where(s => s != target))
                Move(s, 0);
            Move(target, Portfolio.CurrentValue / target.Price);
        }

        private void Move(Security s, decimal tgt)
        {
            var diff = tgt - PositionBy(s);
            if (Math.Abs(diff) * s.Price < 100)
                return;
            RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "OilEq" });
        }
        private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;

        #region RollingWindow
        private class RollingWindow<T>
        {
            private readonly Queue<T> _q = new();
            private readonly int _n;
            public RollingWindow(int n) { _n = n; }
            public void Add(T v) { if (_q.Count == _n) _q.Dequeue(); _q.Enqueue(v); }
            public bool IsFull() => _q.Count == _n;
            public T Last() => _q.Last();
            public T this[int i] => _q.ElementAt(i);
        }
        #endregion

    }
}