// CryptoRebalancingPremiumStrategy.cs
// -----------------------------------------------------------------------------
// Equal‑weight BTC + ETH basket. Rebalance weekly (Monday open) via first
// hourly candle trigger. Assume universe has BTC and ETH.
// -----------------------------------------------------------------------------
// Date: 2 Aug 2025
// -----------------------------------------------------------------------------
using System;
using System.Linq;
using System.Collections.Generic;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
namespace StockSharp.Samples.Strategies
{
    public class CryptoRebalancingPremiumStrategy : Strategy
    {
        private readonly StrategyParam<Security> _btc;
        private readonly StrategyParam<Security> _eth;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly DataType _tf = TimeSpan.FromHours(1).TimeFrame();
        private DateTime _last = DateTime.MinValue;
        public Security BTC { get => _btc.Value; set => _btc.Value = value; }
        public Security ETH { get => _eth.Value; set => _eth.Value = value; }
        public decimal MinTradeUsd => _minUsd.Value;
        public CryptoRebalancingPremiumStrategy()
        {
            _btc = Param<Security>(nameof(BTC), null);
            _eth = Param<Security>(nameof(ETH), null);
            _minUsd = Param(nameof(MinTradeUsd), 200m);
        }
        public override IEnumerable<(Security, DataType)> GetWorkingSecurities() => new[] { (BTC, _tf), (ETH, _tf) };
        protected override void OnStarted(DateTimeOffset t)
        {
            base.OnStarted(t);
            SubscribeCandles(_tf, false, BTC).Bind(c => OnTick(c.OpenTime.UtcDateTime)).Start();
        }
        private void OnTick(DateTime utc)
        {
            if (utc == _last)
                return;
            _last = utc;
            if (utc.DayOfWeek != DayOfWeek.Monday || utc.Hour != 0)
                return;
            Rebalance();
        }
        private void Rebalance()
        {
            decimal half = Portfolio.CurrentValue / 2;
            Move(BTC, half / BTC.Price);
            Move(ETH, half / ETH.Price);
        }
        private void Move(Security s, decimal tgt) { var diff = tgt - Pos(s); if (Math.Abs(diff) * s.Price < MinTradeUsd) return; RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "RebalPrem" }); }
        private decimal Pos(Security s) => GetPositionValue(s, Portfolio) ?? 0;
    }
}