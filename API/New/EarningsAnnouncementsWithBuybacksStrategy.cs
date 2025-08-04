
// EarningsAnnouncementsWithBuybacksStrategy.cs — candle‑stream version
// Universe: IEnumerable<Security>
// Subscribes to daily candles (CandleType param) and runs DailyScan once per day
// when the finished candle of ANY stock arrives.
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
    public class EarningsAnnouncementsWithBuybacksStrategy : Strategy
    {
        #region Parameters
        private readonly StrategyParam<IEnumerable<Security>> _universe;
        private readonly StrategyParam<int> _daysBefore;
        private readonly StrategyParam<int> _daysAfter;
        private readonly StrategyParam<decimal> _capitalUsd;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly StrategyParam<DataType> _candleType;

        public IEnumerable<Security> Universe { get => _universe.Value; set => _universe.Value = value; }
        public int DaysBefore => _daysBefore.Value;
        public int DaysAfter => _daysAfter.Value;
        public decimal CapitalPerTradeUsd => _capitalUsd.Value;
        public decimal MinTradeUsd => _minUsd.Value;
        public DataType CandleType => _candleType.Value;
        #endregion

        private readonly Dictionary<Security, DateTimeOffset> _exit = new();
        private DateTime _lastProcessed = DateTime.MinValue;

        public EarningsAnnouncementsWithBuybacksStrategy()
        {
            _universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>());
            _daysBefore = Param(nameof(DaysBefore), 5);
            _daysAfter = Param(nameof(DaysAfter), 1);
            _capitalUsd = Param(nameof(CapitalPerTradeUsd), 5000m);
            _minUsd = Param(nameof(MinTradeUsd), 100m);
            _candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame());
        }

        public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() =>
            Universe.Select(s => (s, CandleType));

        protected override void OnStarted(DateTimeOffset time)
        {
            base.OnStarted(time);
            if (!Universe.Any())
                throw new InvalidOperationException("Universe is empty.");

            foreach (var (sec, dt) in GetWorkingSecurities())
            {
                SubscribeCandles(sec, dt)
                    .Bind(CandleStates.Finished)
                    .Do(c =>
                    {
                        var d = c.OpenTime.Date;
                        if (d == _lastProcessed)
                            return;
                        _lastProcessed = d;
                        DailyScan(d);
                    }).Start();
            }
        }

        private void DailyScan(DateTime today)
        {
            foreach (var stock in Universe)
            {
                if (!TryGetNextEarningsDate(stock, out var earnDate))
                    continue;

                var diff = (earnDate.Date - today).TotalDays;
                if (diff == DaysBefore && !_exit.ContainsKey(stock) && TryHasActiveBuyback(stock))
                {
                    var qty = CapitalPerTradeUsd / stock.Price;
                    if (qty * stock.Price >= MinTradeUsd)
                    {
                        Place(stock, qty, Sides.Buy, "Enter");
                        _exit[stock] = earnDate.Date.AddDays(DaysAfter);
                    }
                }
            }

            foreach (var kv in _exit.ToList())
            {
                if (today >= kv.Value)
                {
                    var pos = PositionBy(kv.Key);
                    if (pos > 0)
                        Place(kv.Key, pos, Sides.Sell, "Exit");
                    _exit.Remove(kv.Key);
                }
            }
        }

        private void Place(Security s, decimal qty, Sides side, string tag)
        {
            RegisterOrder(new Order
            {
                Security = s,
                Portfolio = Portfolio,
                Side = side,
                Volume = qty,
                Type = OrderTypes.Market,
                Comment = $"EarnBuyback-{tag}"
            });
        }

        private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;

        private bool TryGetNextEarningsDate(Security s, out DateTimeOffset dt) { dt = DateTimeOffset.MinValue; return false; }
        private bool TryHasActiveBuyback(Security s) => false;
    }
}