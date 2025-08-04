// Month12CycleStrategy.cs
// 12‑Month Cycle in Cross‑Section of Stock Returns — High‑Level API implementation for StockSharp (S#)
// Date: 2 August 2025

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// 12‑Month Cycle strategy.
    /// Each rebalance date (month‑end by default):
    /// 1. Take the user‑supplied <see cref="Universe"/> of stocks.
    /// 2. Compute the 1‑month return lagged by <see cref="YearsBack"/> years.
    /// 3. Rank by that return, long the top decile, short the bottom decile (value‑weighted by market cap).
    /// 4. Rebalance to target weights.
    /// <para>
    /// <b>The <see cref="Universe"/> property is mandatory.</b> Populate it in the S# Designer, optimiser or code
    /// before starting the strategy.
    /// </para>
    /// </summary>
    public class Month12CycleStrategy : Strategy
    {
        #region Parameters

        private readonly StrategyParam<IEnumerable<Security>> _universe; // required list
        private readonly StrategyParam<int> _decileSize;
        private readonly StrategyParam<decimal> _leverage;
        private readonly StrategyParam<int> _yearsBack;

        /// <summary>Investment universe (must be non‑empty).</summary>
        public IEnumerable<Security> Universe { get => _universe.Value; set => _universe.Value = value; }
        public int DecileSize => _decileSize.Value;
        public decimal Leverage => _leverage.Value;
        public int YearsBack => _yearsBack.Value;

        #endregion

        private readonly Dictionary<Security, RollingWindow<decimal>> _monthCloses = new();
        private readonly Dictionary<Security, decimal> _cap = new();
        private readonly Dictionary<Security, decimal> _targetWeights = new();

        public Month12CycleStrategy()
        {
            _universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>())
                .SetDisplay("Universe", "List of securities (required)", "Universe");

            _decileSize = Param(nameof(DecileSize), 10)
                .SetDisplay("Deciles", "Number of portfolios", "Ranking");

            _leverage = Param(nameof(Leverage), 1m)
                .SetDisplay("Leverage", "Leverage per long/short leg", "Risk");

            _yearsBack = Param(nameof(YearsBack), 1)
                .SetDisplay("Years Back", "Lag in years (12 months)", "Ranking");
        }

        #region Universe & candles

        public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
        {
            if (Universe == null || !Universe.Any())
                throw new InvalidOperationException("Universe cannot be empty — populate the Universe property before starting the strategy.");

            var dt = DataType.TimeFrame(TimeSpan.FromDays(1));
            return Universe.Select(s => (s, dt));
        }

        protected override void OnStarted(DateTimeOffset time)
        {
            base.OnStarted(time);

            if (Universe == null || !Universe.Any())
                throw new InvalidOperationException("Universe is empty. Set Universe before starting.");

            foreach (var (sec, dt) in GetWorkingSecurities())
            {
                var sub = SubscribeCandles(sec, dt);
                sub.Start();

                _monthCloses[sec] = new RollingWindow<decimal>(13); // 13‑month window
            }

            Schedule(TimeSpan.FromMinutes(10), Exchange.TradingDay.Ends, Rebalance);

            LogInfo($"12‑Month Cycle strategy started. Universe = {Universe.Count()} tickers, Deciles = {DecileSize}");
        }

        protected override void OnCandleFinished(ICandleMessage candle)
        {
            var sec = (Security)candle.SecurityId;
            if (!_monthCloses.TryGetValue(sec, out var window))
                return;

            window.Add(candle.ClosePrice);
        }

        #endregion

        #region Rebalance logic

        private void Rebalance()
        {
            var ready = _monthCloses.Where(kv => kv.Value.IsFull()).ToList();
            if (ready.Count < DecileSize * 2)
            {
                LogInfo("Not enough securities formed for ranking yet.");
                return;
            }

            var perf = ready.ToDictionary(kv => kv.Key, kv => kv.Value[1] / kv.Value[0] - 1);

            foreach (var sec in perf.Keys)
                _cap[sec] = sec.Price * (sec.VolumeStep ?? 1m);

            var ranked = perf.OrderByDescending(p => p.Value).ToList();
            int decileLen = ranked.Count / DecileSize;
            if (decileLen == 0)
            {
                LogInfo("Decile length zero, check universe size.");
                return;
            }

            var winners = ranked.Take(decileLen);
            var losers = ranked.Skip(ranked.Count - decileLen);

            ComputeWeights(winners, losers);
            ExecuteTrades();
        }

        private void ComputeWeights(IEnumerable<KeyValuePair<Security, decimal>> winners,
                                    IEnumerable<KeyValuePair<Security, decimal>> losers)
        {
            _targetWeights.Clear();

            decimal capLong = winners.Sum(p => _cap[p.Key]);
            decimal capShort = losers.Sum(p => _cap[p.Key]);

            foreach (var (sec, _) in winners)
                _targetWeights[sec] = Leverage * (_cap[sec] / capLong);

            foreach (var (sec, _) in losers)
                _targetWeights[sec] = -Leverage * (_cap[sec] / capShort);
        }

        private void ExecuteTrades()
        {
            foreach (var pos in Positions.Where(p => !_targetWeights.ContainsKey(p.Key)))
                SendOrder(pos.Key, -pos.Value);

            foreach (var kv in _targetWeights)
            {
                var sec = kv.Key;
                var tgt = kv.Value * Portfolio.CurrentValue / sec.Price;
                var diff = tgt - PositionBy(sec);

                if (diff.Abs() * sec.Price < 50)
                    continue;

                SendOrder(sec, diff);
            }

            LogInfo($"Rebalance done. Long: {_targetWeights.Count(kv => kv.Value > 0)}, Short: {_targetWeights.Count(kv => kv.Value < 0)}");
        }

        private void SendOrder(Security sec, decimal qty)
        {
            if (qty == 0)
                return;

            var side = qty > 0 ? Sides.Buy : Sides.Sell;
            RegisterOrder(new Order
            {
                Security = sec,
                Portfolio = Portfolio,
                Direction = side,
                Volume = qty.Abs(),
                Type = OrderTypes.Market,
                Comment = "12‑MonthCycle"
            });
        }

        private decimal PositionBy(Security sec) => Positions.TryGetValue(sec, out var pos) ? pos : 0m;

        #endregion
    }

    #region RollingWindow helper

    public class RollingWindow<T>
    {
        private readonly Queue<T> _data;
        private readonly int _size;

        public RollingWindow(int size)
        {
            _size = size;
            _data = new Queue<T>(size);
        }

        public void Add(T value)
        {
            if (_data.Count == _size)
                _data.Dequeue();
            _data.Enqueue(value);
        }

        public bool IsFull() => _data.Count == _size;

        public T this[int idx] => _data.ElementAt(idx);
    }

    #endregion
}
