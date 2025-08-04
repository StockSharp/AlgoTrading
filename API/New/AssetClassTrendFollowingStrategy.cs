
// AssetClassTrendFollowingStrategy.cs  (revised)
// Uses SMA filter per ETF; monthly rebalance based on daily candle stream.
// Parameters: Universe (IEnumerable<Security>), CandleType.
// Date: 2 August 2025

using System;
using System.Collections.Generic;
using System.Linq;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    public class AssetClassTrendFollowingStrategy : Strategy
    {
        private readonly StrategyParam<IEnumerable<Security>> _universe;
        private readonly StrategyParam<int> _smaLen;
        private readonly StrategyParam<decimal> _minUsd;
        private readonly StrategyParam<DataType> _candleType;

        public IEnumerable<Security> Universe { get => _universe.Value; set => _universe.Value = value; }
        public int SmaLength => _smaLen.Value;
        public decimal MinTradeUsd => _minUsd.Value;
        public DataType CandleType => _candleType.Value;

        private readonly Dictionary<Security, SimpleMovingAverage> _sma = new();
        private readonly HashSet<Security> _held = new();
        private DateTime _lastDay = DateTime.MinValue;

        public AssetClassTrendFollowingStrategy()
        {
            _universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>());
            _smaLen = Param(nameof(SmaLength), 210);
            _minUsd = Param(nameof(MinTradeUsd), 50m);
            _candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame());
        }

        public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() =>
            Universe.Select(s => (s, CandleType));

        protected override void OnStarted(DateTimeOffset time)
        {
            base.OnStarted(time);
            foreach (var (sec, dt) in GetWorkingSecurities())
            {
                var sma = new SimpleMovingAverage { Length = SmaLength };
                _sma[sec] = sma;

                SubscribeCandles(sec, dt)
                    .Bind(CandleStates.Finished)
                    .Do(c =>
                    {
                        sma.Process(c);
                        var d = c.OpenTime.Date;
                        if (d == _lastDay)
                            return;
                        _lastDay = d;
                        if (d.Day == 1 && Exchange.IsTradingDay(d))
                            TryRebalance();
                    })
                    .Start();
            }
        }

        private void TryRebalance()
        {
            var longs = _sma.Where(kv => kv.Value.IsFormed &&
                                         kv.Key.LastPrice > kv.Value.GetCurrentValue<decimal>())
                            .Select(kv => kv.Key).ToList();

            foreach (var sec in _held.Where(h => !longs.Contains(h)).ToList())
                Move(sec, 0);

            if (longs.Any())
            {
                decimal cap = Portfolio.CurrentValue / longs.Count;
                foreach (var sec in longs)
                    Move(sec, cap / sec.Price);
            }

            _held.Clear();
            _held.UnionWith(longs);
        }

        private void Move(Security s, decimal tgt)
        {
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
                Comment = "ACTrend"
            });
        }

        private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;
    }
}