// SoccerClubsArbitrageStrategy.cs
// -----------------------------------------------------------------------------
// Two share classes of the same soccer club (pair length = 2).
// Long cheaper share, short expensive when relative premium > EntryThresh;
// exit when premium shrinks below ExitThresh.
// Triggered by daily candle of the first ticker — no Schedule used.
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
    public class SoccerClubsArbitrageStrategy : Strategy
    {
        #region Params
        private readonly StrategyParam<IEnumerable<Security>> _pair;
        private readonly StrategyParam<decimal> _entry;
        private readonly StrategyParam<decimal> _exit;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly DataType _tf = TimeSpan.FromDays(1).TimeFrame();

        public IEnumerable<Security> Pair { get => _pair.Value; set => _pair.Value = value; }
        public decimal EntryThreshold => _entry.Value;
        public decimal ExitThreshold => _exit.Value;
        public decimal MinTradeUsd => _minUsd.Value;
        #endregion

        private Security _a, _b;
        private readonly Dictionary<Security, decimal> _latestPrices = new();

        public SoccerClubsArbitrageStrategy()
        {
            _pair = Param<IEnumerable<Security>>(nameof(Pair), Array.Empty<Security>());
            _entry = Param(nameof(EntryThreshold), 0.05m);
            _exit = Param(nameof(ExitThreshold), 0.01m);
            _minUsd = Param(nameof(MinTradeUsd), 200m);
        }

        public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
        {
            if (Pair.Count() != 2)
                throw new InvalidOperationException("Pair must contain exactly two tickers.");
            _a = Pair.ElementAt(0);
            _b = Pair.ElementAt(1);
            yield return (_a, _tf);
            yield return (_b, _tf);
        }

        protected override void OnStarted(DateTimeOffset time)
        {
            base.OnStarted(time);

            // Subscribe to both securities to get price updates
            foreach (var (s, tf) in GetWorkingSecurities())
            {
                SubscribeCandles(tf, true, s)
                    .Bind(c => ProcessCandle(c, s))
                    .Start();
            }

            // Use first ticker's candle as daily trigger
            SubscribeCandles(_tf, true, _a)
                .Bind(c => TriggerDaily())
                .Start();
        }

        private void ProcessCandle(ICandleMessage candle, Security security)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Store the latest closing price for this security
            _latestPrices[security] = candle.ClosePrice;
        }

        private void TriggerDaily()
        {
            OnDaily();
        }

        private void OnDaily()
        {
            var pxA = GetLatestPrice(_a);
            var pxB = GetLatestPrice(_b);
            if (pxA == 0 || pxB == 0)
                return;

            var premium = pxA / pxB - 1m;

            if (Math.Abs(premium) < ExitThreshold)
            {
                Hedge(0);
                return;
            }

            if (premium > EntryThreshold)
                Hedge(-1);       // A overpriced → short A, long B
            else if (premium < -EntryThreshold)
                Hedge(+1);       // B overpriced → long A, short B
        }

        private decimal GetLatestPrice(Security security)
        {
            return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
        }

        // dir = +1 ⇒ long A / short B ; dir = –1 ⇒ short A / long B ; dir = 0 ⇒ flat
        private void Hedge(int dir)
        {
            var portfolioValue = Portfolio.CurrentValue ?? 0m;
            decimal half = portfolioValue / 2;
            var priceA = GetLatestPrice(_a);
            var priceB = GetLatestPrice(_b);
            if (priceA > 0)
                Move(_a, dir * half / priceA);
            if (priceB > 0)
                Move(_b, -dir * half / priceB);
        }

        private void Move(Security s, decimal tgtQty)
        {
            var diff = tgtQty - PositionBy(s);
            var price = GetLatestPrice(s);
            if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd)
                return;

            RegisterOrder(new Order
            {
                Security = s,
                Portfolio = Portfolio,
                Side = diff > 0 ? Sides.Buy : Sides.Sell,
                Volume = Math.Abs(diff),
                Type = OrderTypes.Market,
                Comment = "SoccerArb"
            });
        }

        private decimal PositionBy(Security s) =>
            GetPositionValue(s, Portfolio) ?? 0m;
    }
}