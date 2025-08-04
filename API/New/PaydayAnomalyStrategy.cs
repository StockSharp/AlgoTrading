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
        private readonly StrategyParam<decimal> _minUsd;
        private readonly DataType _tf = TimeSpan.FromDays(1).TimeFrame();
        private readonly Dictionary<Security, decimal> _latestPrices = new();

        public decimal MinTradeUsd => _minUsd.Value;
        private DateTime _last = DateTime.MinValue;

        public PaydayAnomalyStrategy()
        {
            _minUsd = Param(nameof(MinTradeUsd), 200m);
        }

        public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
        {
            if (Security == null)
                throw new InvalidOperationException("Security not set");
            yield return (Security, _tf);
        }

        protected override void OnStarted(DateTimeOffset t)
        {
            base.OnStarted(t);
            SubscribeCandles(_tf, true, Security).Bind(c => ProcessCandle(c, Security)).Start();
        }

        private void ProcessCandle(ICandleMessage candle, Security security)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Store the latest closing price for this security
            _latestPrices[security] = candle.ClosePrice;

            OnDaily(candle.OpenTime.Date);
        }

        private void OnDaily(DateTime d)
        {
            if (d == _last)
                return;
            _last = d;
            int tdMonthEnd = TradingDaysLeftInMonth(d);
            int tdMonthStart = TradingDayNumber(d);
            bool inWindow = tdMonthEnd <= 2 || tdMonthStart <= 3;
            var portfolioValue = Portfolio.CurrentValue ?? 0m;
            var price = GetLatestPrice(Security);
            var tgt = inWindow && price > 0 ? portfolioValue / price : 0;
            var diff = tgt - Pos();
            if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd)
                return;
            RegisterOrder(new Order { Security = Security, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "Payday" });
        }

        private decimal GetLatestPrice(Security security)
        {
            return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
        }

        private int TradingDaysLeftInMonth(DateTime d)
        {
            int cnt = 0;
            var cur = d;
            while (cur.Month == d.Month)
            { 
                // Simple approximation: assume weekdays are trading days
                if (cur.DayOfWeek != DayOfWeek.Saturday && cur.DayOfWeek != DayOfWeek.Sunday) 
                    cnt++; 
                cur = cur.AddDays(1); 
            }
            return cnt - 1;
        }

        private int TradingDayNumber(DateTime d)
        {
            int num = 0;
            var cur = new DateTime(d.Year, d.Month, 1);
            while (cur <= d)
            { 
                // Simple approximation: assume weekdays are trading days
                if (cur.DayOfWeek != DayOfWeek.Saturday && cur.DayOfWeek != DayOfWeek.Sunday) 
                    num++; 
                cur = cur.AddDays(1); 
            }
            return num;
        }

        private decimal Pos() => GetPositionValue(Security, Portfolio) ?? 0;
    }
}