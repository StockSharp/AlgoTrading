// WTIBrentSpreadStrategy.cs
// -----------------------------------------------------------------------------
// WTI‑Brent spread vs 20‑day SMA. Daily WTI candle triggers evaluation.
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
    public class WTIBrentSpreadStrategy : Strategy
    {
        private readonly StrategyParam<Security> _wti;
        private readonly StrategyParam<Security> _brent;
        private readonly StrategyParam<int> _ma;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly DataType _tf = TimeSpan.FromDays(1).TimeFrame();
        private readonly Queue<decimal> _spr = new();
        private readonly Dictionary<Security, decimal> _latestPrices = new();

        public Security WTI { get => _wti.Value; set => _wti.Value = value; }
        public Security Brent { get => _brent.Value; set => _brent.Value = value; }
        public int MaPeriod => _ma.Value;
        public decimal MinTradeUsd => _minUsd.Value;

        public WTIBrentSpreadStrategy()
        {
            _wti = Param<Security>(nameof(WTI), null);
            _brent = Param<Security>(nameof(Brent), null);
            _ma = Param(nameof(MaPeriod), 20);
            _minUsd = Param(nameof(MinTradeUsd), 200m);
        }

        public override IEnumerable<(Security, DataType)> GetWorkingSecurities() 
        { 
            yield return (WTI, _tf); 
            yield return (Brent, _tf); 
        }

        protected override void OnStarted(DateTimeOffset t)
        {
            base.OnStarted(t);
            SubscribeCandles(_tf, true, WTI).Bind(c => ProcessCandle(c, WTI)).Start();
            SubscribeCandles(_tf, true, Brent).Bind(c => ProcessCandle(c, Brent)).Start();
        }

        private void ProcessCandle(ICandleMessage candle, Security security)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Store the latest closing price for this security
            _latestPrices[security] = candle.ClosePrice;

            // Only trigger evaluation when WTI candle comes in
            if (security == WTI)
                OnDaily();
        }

        private void OnDaily()
        {
            var p1 = GetLatestPrice(WTI);
            var p2 = GetLatestPrice(Brent);
            if (p1 == 0 || p2 == 0)
                return;

            var spr = p1 - p2;
            if (_spr.Count == MaPeriod)
                _spr.Dequeue();
            _spr.Enqueue(spr);
            if (_spr.Count < MaPeriod)
                return;

            var sma = _spr.Average();
            var portfolioValue = Portfolio.CurrentValue ?? 0m;
            var notional = portfolioValue / 2;

            if (Math.Abs(spr - sma) < 0.01m)
            { 
                Move(WTI, 0); 
                Move(Brent, 0); 
                return; 
            }

            if (spr > sma)
            { 
                Move(WTI, -notional / p1); 
                Move(Brent, notional / p2); 
            }
            else
            { 
                Move(WTI, notional / p1); 
                Move(Brent, -notional / p2); 
            }
        }

        private decimal GetLatestPrice(Security security)
        {
            return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
        }

        private void Move(Security s, decimal tgt)
        {
            var diff = tgt - Pos(s);
            var price = GetLatestPrice(s);
            if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd)
                return;
            RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "Spread" });
        }

        private decimal Pos(Security s) => GetPositionValue(s, Portfolio) ?? 0;
    }
}