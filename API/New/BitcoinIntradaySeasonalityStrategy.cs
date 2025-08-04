// BitcoinIntradaySeasonalityStrategy.cs
// -----------------------------------------------------------------------------
// Goes long BTC during historically strong hours, flat otherwise.
// Hourly candle subscription; seasons defined by parameter HoursLong.
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
    public class BitcoinIntradaySeasonalityStrategy : Strategy
    {
        #region Parameters
        private readonly StrategyParam<Security> _btc;
        private readonly StrategyParam<int[]> _hoursLong;   // e.g.,  0,1,2,3 == midnight–3am UTC
        private readonly StrategyParam<decimal> _minUsd;
        private readonly DataType _tf = TimeSpan.FromHours(1).TimeFrame();

        public Security BTC { get => _btc.Value; set => _btc.Value = value; }
        public int[] HoursLong => _hoursLong.Value;
        public decimal MinTradeUsd => _minUsd.Value;
        #endregion

        public BitcoinIntradaySeasonalityStrategy()
        {
            _btc = Param<Security>(nameof(BTC), null);
            _hoursLong = Param(nameof(HoursLong), new[] { 0, 1, 2, 3 });
            _minUsd = Param(nameof(MinTradeUsd), 200m);
        }

        public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
        {
            if (BTC == null)
                throw new InvalidOperationException("BTC security not set.");
            return new[] { (BTC, _tf) };
        }

        protected override void OnStarted(DateTimeOffset t)
        {
            base.OnStarted(t);

            SubscribeCandles(_tf, true, BTC)
                .Bind(c => OnHourClose(c))
                .Start();
        }

        private void OnHourClose(ICandleMessage c)
        {
            var hour = c.OpenTime.UtcDateTime.Hour;   // assume server UTC
            bool inSeason = HoursLong.Contains(hour);

            var tgt = inSeason ? Portfolio.CurrentValue / BTC.Price : 0m;
            var diff = tgt - PositionBy(BTC);
            if (Math.Abs(diff) * BTC.Price < MinTradeUsd)
                return;

            RegisterOrder(new Order
            {
                Security = BTC,
                Portfolio = Portfolio,
                Side = diff > 0 ? Sides.Buy : Sides.Sell,
                Volume = Math.Abs(diff),
                Type = OrderTypes.Market,
                Comment = "BTCSeason"
            });
        }

        private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;
    }
}