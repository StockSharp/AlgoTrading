// ValueMomentumAcrossAssetsStrategy.cs
// -----------------------------------------------------------------------------
// Value & Momentum across asset classes
// Rebalance frequency and data feeds stubbed; candle-trigger only.
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
    public class ValueMomentumAcrossAssetsStrategy : Strategy
    {
        // Parameters
        private readonly StrategyParam<IEnumerable<Security>> _univ;
        private readonly StrategyParam<decimal> _min;
        private readonly DataType _tf = TimeSpan.FromDays(1).TimeFrame();

        public IEnumerable<Security> Universe { get => _univ.Value; set => _univ.Value = value; }
        public decimal MinTradeUsd => _min.Value;

        public ValueMomentumAcrossAssetsStrategy()
        {
            _univ = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>());
            _min = Param(nameof(MinTradeUsd), 200m);
        }

        public override IEnumerable<(Security, DataType)> GetWorkingSecurities() =>
            Universe.Select(s => (s, _tf));

        protected override void OnStarted(DateTimeOffset t)
        {
            base.OnStarted(t);

            var trig = Universe.FirstOrDefault() ?? throw new InvalidOperationException("Universe empty");

            SubscribeCandles(_tf, true, trig).Bind(c => OnDay(c.OpenTime.Date)).Start();
        }

        private void OnDay(DateTime d)
        {
            // TODO: implement factor logic. Placeholder keeps portfolio flat.
        }
    }
}