// PaydayAnomalyStrategy.cs
// -----------------------------------------------------------------------------
// Holds market ETF only during days -2..+3 around typical U.S. payday
// (assume salary hits 1st business day of month). Long ETF from two trading
// days before month‑end through third trading day of new month.
// Trigger: daily candle close.
// -----------------------------------------------------------------------------
// Date: 2 Aug 2025
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
namespace StockSharp.Samples.Strategies
{
    public class PaydayAnomalyStrategy : Strategy
    {
        private readonly StrategyParam<Security> _etf;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly DataType _tf = DataType.TimeFrame(TimeSpan.FromDays(1));
        public Security ETF { get => _etf.Value; set => _etf.Value = value; }
        public decimal MinTradeUsd => _minUsd.Value;
        private DateTime _last = DateTime.MinValue;
        public PaydayAnomalyStrategy()
        {
            _etf = Param<Security>(nameof(ETF), null);
            _minUsd = Param(nameof(MinTradeUsd), 200m);
        }
        public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
        {
            if (ETF == null)
                throw new InvalidOperationException("ETF not set");
            return new[] { (ETF, _tf) };
        }
        protected override void OnStarted(DateTimeOffset t)
        {
            base.OnStarted(t);
            SubscribeCandles(ETF, _tf).Bind(CandleStates.Finished).Do(c => OnDaily(c.OpenTime.Date)).Start();
        }
        private void OnDaily(DateTime d)
        {
            if (d == _last)
                return;
            _last = d;
            int tdMonthEnd = TradingDaysLeftInMonth(d);
            int tdMonthStart = TradingDayNumber(d);
            bool inWindow = tdMonthEnd <= 2 || tdMonthStart <= 3;
            var tgt = inWindow ? Portfolio.CurrentValue / ETF.Price : 0;
            var diff = tgt - Pos();
            if (Math.Abs(diff) * ETF.Price < MinTradeUsd)
                return;
            RegisterOrder(new Order { Security = ETF, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "Payday" });
        }
        private int TradingDaysLeftInMonth(DateTime d)
        {
            int cnt = 0;
            var cur = d;
            while (cur.Month == d.Month)
            { if (Exchange.IsTradingDay(cur)) cnt++; cur = cur.AddDays(1); }
            return cnt - 1;
        }
        private int TradingDayNumber(DateTime d)
        {
            int num = 0;
            var cur = new DateTime(d.Year, d.Month, 1);
            while (cur <= d)
            { if (Exchange.IsTradingDay(cur)) num++; cur = cur.AddDays(1); }
            return num;
        }
        private decimal Pos() => Positions.TryGetValue(ETF, out var q) ? q : 0m;
    }
}
