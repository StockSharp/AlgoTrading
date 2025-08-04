// SyntheticLendingRatesStrategy.cs
// -----------------------------------------------------------------------------
// Uses change in synthetic lending-rate intensity (external feed) to take an
// overnight position in SPY.
//   • 15:57 ET  capture intensity I0
//   • 15:59 ET  capture I1; long if I1>I0 else short
//   • 15:58 ET next day exit to flat
// Triggered by 1‑minute SPY candles (SubscribeCandles…Bind). No Schedule().
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
    public class SyntheticLendingRatesStrategy : Strategy
    {
        private readonly StrategyParam<decimal> _minUsd;
        private readonly DataType _tf = TimeSpan.FromMinutes(1).TimeFrame();
        public decimal MinTradeUsd => _minUsd.Value;
        private decimal? _intensityT0;
        public SyntheticLendingRatesStrategy()
        {
            _minUsd = Param(nameof(MinTradeUsd), 200m);
        }

        protected override void OnStarted(DateTimeOffset t)
        {
            base.OnStarted(t);
            SubscribeCandles(_tf, true, Security).Bind(OnMinute).Start();
        }
        private void OnMinute(ICandleMessage c)
        {
            var utc = c.OpenTime.UtcDateTime;
            // 19:57/59 UTC ≈ 15:57/59 ET (summer); adjust in prod
            if (utc.hour == 19 && utc.minute == 57)
                _intensityT0 = GetIntensity();
            else if (utc.hour == 19 && utc.minute == 59 && _intensityT0 != null)
            {
                var dir = GetIntensity() > _intensityT0 ? 1 : -1;
                Trade(dir * Portfolio.CurrentValue / SPY.Price);
            }
            // next day exit 19:58 UTC
            else if (utc.hour == 19 && utc.minute == 58)
                Trade(0);
        }
        private void Trade(decimal tgt)
        {
            var diff = tgt - Position;
            if (Math.Abs(diff) * SPY.Price < MinTradeUsd) return;
            RegisterOrder(new Order { Security = SPY, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "SynLend" });
        }

        private decimal GetIntensity() => 0m; // stub replace with real feed
    }
}