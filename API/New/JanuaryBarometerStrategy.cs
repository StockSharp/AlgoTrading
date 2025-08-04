
// JanuaryBarometerStrategy.cs
// -----------------------------------------------------------------------------
// If January monthly return is positive, stay long equity index ETF for rest
// of year; otherwise move to cash proxy.
// Uses daily candles to detect January close.
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
    public class JanuaryBarometerStrategy : Strategy
    {
        #region Params
        private readonly StrategyParam<Security> _equity;
        private readonly StrategyParam<Security> _cash;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly DataType _tf = DataType.TimeFrame(TimeSpan.FromDays(1));

        public Security EquityETF { get => _equity.Value; set => _equity.Value = value; }
        public Security CashETF { get => _cash.Value; set => _cash.Value = value; }
        public decimal MinTradeUsd => _minUsd.Value;
        #endregion

        private decimal _janOpenPrice = 0m;

        public JanuaryBarometerStrategy()
        {
            _equity = Param<Security>(nameof(EquityETF), null);
            _cash = Param<Security>(nameof(CashETF), null);
            _minUsd = Param(nameof(MinTradeUsd), 200m);
        }

        public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
        {
            if (EquityETF == null || CashETF == null)
                throw new InvalidOperationException("Both equity and cash ETFs must be set.");
            return new[] { (EquityETF, _tf) };
        }

        protected override void OnStarted(DateTimeOffset t)
        {
            base.OnStarted(t);

            SubscribeCandles(EquityETF, _tf)
                .Bind(CandleStates.Finished)
                .Do(c => OnDaily(c))
                .Start();
        }

        private void OnDaily(ICandleMessage c)
        {
            var d = c.OpenTime.Date;
            // capture open price on first trading day of January
            if (d.Month == 1 && d.Day == 1)
                _janOpenPrice = c.OpenPrice;

            // detect January close (31 Jan, or last trading day of Jan)
            if (d.Month == 1 && (d.Day == 31 || c.State == CandleStates.Finished && c.CloseTime.Date.Month == 2))
            {
                if (_janOpenPrice == 0m)
                    return;
                var janRet = (c.ClosePrice - _janOpenPrice) / _janOpenPrice;
                Rebalance(janRet > 0);
            }
        }

        private void Rebalance(bool bullish)
        {
            if (bullish)
            {
                Move(EquityETF, 1m);
                Move(CashETF, 0m);
            }
            else
            {
                Move(EquityETF, 0m);
                Move(CashETF, 1m);
            }
        }

        private void Move(Security s, decimal weight)
        {
            var tgt = weight * Portfolio.CurrentValue / s.Price;
            var diff = tgt - PositionBy(s);
            if (Math.Abs(diff) * s.Price < MinTradeUsd)
                return;

            RegisterOrder(new Order
            {
                Security = s,
                Portfolio = Portfolio,
                Side = diff > 0 ? Sides.Buy : Sides.Sell,
                Volume = Math.Abs(diff),
                Type = OrderTypes.Market,
                Comment = "JanBar"
            });
        }

        private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;
    }
}