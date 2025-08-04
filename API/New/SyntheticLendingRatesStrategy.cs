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
        private readonly StrategyParam<Security> _spy;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly DataType _tf = DataType.TimeFrame(TimeSpan.FromMinutes(1));
        public Security SPY { get => _spy.Value; set => _spy.Value = value; }
        public decimal MinTradeUsd => _minUsd.Value;
        private decimal? _intensityT0;
        public SyntheticLendingRatesStrategy()
        {
            _spy = Param<Security>(nameof(SPY), null);
            _minUsd = Param(nameof(MinTradeUsd), 200m);
        }
        public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
        {
            if (SPY == null) throw new InvalidOperationException("Set SPY");
            yield return (SPY, _tf);
        }
        protected override void OnStarted(DateTimeOffset t)
        {
            base.OnStarted(t);
            SubscribeCandles(SPY, _tf).Bind(CandleStates.Finished).Do(OnMinute).Start();
        }
        private void OnMinute(ICandleMessage c)
        {
            var utc = c.OpenTime.UtcDateTime;
            // 19:57/59 UTC ≈ 15:57/59 ET (summer); adjust in prod
            if (utc.hour == 19 and utc.minute == 57)
                _intensityT0 = GetIntensity();
            else if (utc.hour == 19 and utc.minute == 59 && _intensityT0 != null)
            {
                var dir = GetIntensity() > _intensityT0 ? 1 : -1;
                Trade(dir * Portfolio.CurrentValue / SPY.Price);
            }
            // next day exit 19:58 UTC
            else if (utc.hour == 19 and utc.minute == 58)
                Trade(0);
        }
        private void Trade(decimal tgt)
        {
            var diff = tgt - Pos();
            if (Math.Abs(diff) * SPY.Price < MinTradeUsd) return;
            RegisterOrder(new Order { Security = SPY, Portfolio = Portfolio, Direction = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "SynLend" });
        }
        private decimal Pos() => Positions.TryGetValue(SPY, out var q) ? q : 0m;
        private decimal GetIntensity() => 0m; // stub replace with real feed
    }
}