// OptionExpirationWeekStrategy.cs — candle-triggered
// Long ETF only during option‑expiration week (ending 3rd Friday).
// Trigger: daily candle close.
// Date: 2 Aug 2025

using System;
using System.Collections.Generic;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    public class OptionExpirationWeekStrategy : Strategy
    {
        private readonly StrategyParam<Security> _etf;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly DataType _tf = DataType.TimeFrame(TimeSpan.FromDays(1));

        public Security ETF { get => _etf.Value; set => _etf.Value = value; }
        public decimal MinTradeUsd => _minUsd.Value;

        public OptionExpirationWeekStrategy()
        {
            _etf = Param<Security>(nameof(ETF), null);
            _minUsd = Param(nameof(MinTradeUsd), 200m);
        }

        public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
        {
            if (ETF == null)
                throw new InvalidOperationException("ETF not set.");
            return new[] { (ETF, _tf) };
        }

        protected override void OnStarted(DateTimeOffset t)
        {
            base.OnStarted(t);
            SubscribeCandles(ETF, _tf)
                .Bind(CandleStates.Finished)
                .Do(c => OnDaily(c.OpenTime.Date))
                .Start();
        }

        private void OnDaily(DateTime d)
        {
            bool inExp = IsOptionExpWeek(d);
            var tgt = inExp ? Portfolio.CurrentValue / ETF.Price : 0;
            var diff = tgt - PositionBy(ETF);
            if (Math.Abs(diff) * ETF.Price < MinTradeUsd)
                return;

            RegisterOrder(new Order
            {
                Security = ETF,
                Portfolio = Portfolio,
                Direction = diff > 0 ? Sides.Buy : Sides.Sell,
                Volume = Math.Abs(diff),
                Type = OrderTypes.Market,
                Comment = "OpExp"
            });
        }

        private bool IsOptionExpWeek(DateTime d)
        {
            // find third Friday
            var third = new DateTime(d.Year, d.Month, 1);
            while (third.DayOfWeek != DayOfWeek.Friday)
                third = third.AddDays(1);
            third = third.AddDays(14);
            return d >= third.AddDays(-4) && d <= third;
        }

        private decimal PositionBy(Security s) => Positions.TryGetValue(s, out var q) ? q : 0m;
    }
}
