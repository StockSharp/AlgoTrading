using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Killer Sell 2.0 strategy converted from MetaTrader 4 implementation.
/// The strategy times short entries when multiple momentum indicators align
/// and applies a martingale position sizing scheme.
/// </summary>
public class KillerSell20Strategy : Strategy
{
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<int> _entryWprPeriod;
        private readonly StrategyParam<int> _exitWprPeriod;
        private readonly StrategyParam<int> _macdFastPeriod;
        private readonly StrategyParam<int> _macdSlowPeriod;
        private readonly StrategyParam<int> _macdSignalPeriod;
        private readonly StrategyParam<decimal> _macdThreshold;
        private readonly StrategyParam<int> _stochasticEntryKPeriod;
        private readonly StrategyParam<int> _stochasticEntryDPeriod;
        private readonly StrategyParam<int> _stochasticEntrySlow;
        private readonly StrategyParam<decimal> _entryStochasticLevel;
        private readonly StrategyParam<int> _stochasticExitKPeriod;
        private readonly StrategyParam<int> _stochasticExitDPeriod;
        private readonly StrategyParam<int> _stochasticExitSlow;
        private readonly StrategyParam<decimal> _exitStochasticLevel;
        private readonly StrategyParam<decimal> _entryWprThreshold;
        private readonly StrategyParam<decimal> _exitWprThreshold;
        private readonly StrategyParam<decimal> _lossExitPips;
        private readonly StrategyParam<decimal> _profitExitPips;
        private readonly StrategyParam<decimal> _takeProfitPips;
        private readonly StrategyParam<decimal> _initialVolume;
        private readonly StrategyParam<decimal> _martingaleMultiplier;
        private readonly StrategyParam<decimal> _maxVolume;

        private MovingAverageConvergenceDivergenceSignal _macd = null!;
        private WilliamsPercentRange _wprEntry = null!;
        private WilliamsPercentRange _wprExit = null!;
        private StochasticOscillator _stochasticEntry = null!;
        private StochasticOscillator _stochasticExit = null!;

        private readonly List<PositionLot> _openShortLots = new();
        private decimal _previousEntryK;
        private bool _hasPreviousEntryK;
        private decimal _pipSize;
        private decimal _currentVolume;
        private decimal _lastClosedPnL;

        /// <summary>
        /// Initializes a new instance of the <see cref="KillerSell20Strategy"/> class.
        /// </summary>
        public KillerSell20Strategy()
        {
                _candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
                        .SetDisplay("Candle Type", "Primary timeframe used to evaluate indicators", "General");

                _entryWprPeriod = Param(nameof(EntryWprPeriod), 350)
                        .SetGreaterThanZero()
                        .SetDisplay("Entry WPR Period", "Length of the Williams %R used for entry validation", "Indicators");

                _exitWprPeriod = Param(nameof(ExitWprPeriod), 350)
                        .SetGreaterThanZero()
                        .SetDisplay("Exit WPR Period", "Length of the Williams %R used for exit validation", "Indicators");

                _macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
                        .SetGreaterThanZero()
                        .SetDisplay("MACD Fast EMA", "Fast EMA period of the MACD filter", "Indicators");

                _macdSlowPeriod = Param(nameof(MacdSlowPeriod), 120)
                        .SetGreaterThanZero()
                        .SetDisplay("MACD Slow EMA", "Slow EMA period of the MACD filter", "Indicators");

                _macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
                        .SetGreaterThanZero()
                        .SetDisplay("MACD Signal", "Signal EMA period of the MACD filter", "Indicators");

                _macdThreshold = Param(nameof(MacdThreshold), 0.0014m)
                        .SetDisplay("MACD Threshold", "Minimum MACD value required to open a short", "Indicators");

                _stochasticEntryKPeriod = Param(nameof(StochasticEntryKPeriod), 10)
                        .SetGreaterThanZero()
                        .SetDisplay("Entry %K", "%K period for the entry Stochastic", "Indicators");

                _stochasticEntryDPeriod = Param(nameof(StochasticEntryDPeriod), 1)
                        .SetGreaterThanZero()
                        .SetDisplay("Entry %D", "%D period for the entry Stochastic", "Indicators");

                _stochasticEntrySlow = Param(nameof(StochasticEntrySlow), 3)
                        .SetGreaterThanZero()
                        .SetDisplay("Entry Smooth", "Slowing factor for the entry Stochastic", "Indicators");

                _entryStochasticLevel = Param(nameof(EntryStochasticLevel), 90m)
                        .SetDisplay("Entry Level", "%K crossing level used to trigger sells", "Indicators");

                _stochasticExitKPeriod = Param(nameof(StochasticExitKPeriod), 90)
                        .SetGreaterThanZero()
                        .SetDisplay("Exit %K", "%K period for the exit Stochastic", "Indicators");

                _stochasticExitDPeriod = Param(nameof(StochasticExitDPeriod), 7)
                        .SetGreaterThanZero()
                        .SetDisplay("Exit %D", "%D period for the exit Stochastic", "Indicators");

                _stochasticExitSlow = Param(nameof(StochasticExitSlow), 1)
                        .SetGreaterThanZero()
                        .SetDisplay("Exit Smooth", "Slowing factor for the exit Stochastic", "Indicators");

                _exitStochasticLevel = Param(nameof(ExitStochasticLevel), 12m)
                        .SetDisplay("Exit Level", "Upper bound that signals oversold conditions", "Indicators");

                _entryWprThreshold = Param(nameof(EntryWprThreshold), -10m)
                        .SetDisplay("Entry WPR", "Williams %R value that confirms an overbought market", "Indicators");

                _exitWprThreshold = Param(nameof(ExitWprThreshold), -80m)
                        .SetDisplay("Exit WPR", "Williams %R level that confirms oversold conditions", "Indicators");

                _lossExitPips = Param(nameof(LossExitPips), 10m)
                        .SetGreaterThanZero()
                        .SetDisplay("Protective Close", "Average profit threshold (in pips) to exit on adverse momentum", "Risk");

                _profitExitPips = Param(nameof(ProfitExitPips), 15m)
                        .SetGreaterThanZero()
                        .SetDisplay("Take Profit Trigger", "Average profit threshold (in pips) to secure wins", "Risk");

                _takeProfitPips = Param(nameof(TakeProfitPips), 100m)
                        .SetGreaterThanZero()
                        .SetDisplay("Take Profit", "Distance of the protective take profit assigned to each trade", "Risk");

                _initialVolume = Param(nameof(InitialVolume), 0.05m)
                        .SetGreaterThanZero()
                        .SetDisplay("Initial Volume", "Starting lot size for the martingale ladder", "Money Management");

                _martingaleMultiplier = Param(nameof(MartingaleMultiplier), 1.2m)
                        .SetGreaterThanZero()
                        .SetDisplay("Martingale Multiplier", "Factor applied after a losing cycle", "Money Management");

                _maxVolume = Param(nameof(MaxVolume), 5m)
                        .SetGreaterThanZero()
                        .SetDisplay("Max Volume", "Maximum allowed position size for a single sell", "Money Management");
        }

        /// <summary>
        /// Candle type used for signal calculations.
        /// </summary>
        public DataType CandleType
        {
                get => _candleType.Value;
                set => _candleType.Value = value;
        }

        /// <summary>
        /// Williams %R period for entry conditions.
        /// </summary>
        public int EntryWprPeriod
        {
                get => _entryWprPeriod.Value;
                set => _entryWprPeriod.Value = value;
        }

        /// <summary>
        /// Williams %R period for exit conditions.
        /// </summary>
        public int ExitWprPeriod
        {
                get => _exitWprPeriod.Value;
                set => _exitWprPeriod.Value = value;
        }

        /// <summary>
        /// Fast EMA length of the MACD filter.
        /// </summary>
        public int MacdFastPeriod
        {
                get => _macdFastPeriod.Value;
                set => _macdFastPeriod.Value = value;
        }

        /// <summary>
        /// Slow EMA length of the MACD filter.
        /// </summary>
        public int MacdSlowPeriod
        {
                get => _macdSlowPeriod.Value;
                set => _macdSlowPeriod.Value = value;
        }

        /// <summary>
        /// Signal EMA of the MACD filter.
        /// </summary>
        public int MacdSignalPeriod
        {
                get => _macdSignalPeriod.Value;
                set => _macdSignalPeriod.Value = value;
        }

        /// <summary>
        /// Minimum MACD value required to enter a sell trade.
        /// </summary>
        public decimal MacdThreshold
        {
                get => _macdThreshold.Value;
                set => _macdThreshold.Value = value;
        }

        /// <summary>
        /// %K period for the entry Stochastic oscillator.
        /// </summary>
        public int StochasticEntryKPeriod
        {
                get => _stochasticEntryKPeriod.Value;
                set => _stochasticEntryKPeriod.Value = value;
        }

        /// <summary>
        /// %D period for the entry Stochastic oscillator.
        /// </summary>
        public int StochasticEntryDPeriod
        {
                get => _stochasticEntryDPeriod.Value;
                set => _stochasticEntryDPeriod.Value = value;
        }

        /// <summary>
        /// Slowing factor for the entry Stochastic oscillator.
        /// </summary>
        public int StochasticEntrySlow
        {
                get => _stochasticEntrySlow.Value;
                set => _stochasticEntrySlow.Value = value;
        }

        /// <summary>
        /// Level that %K must cross downward to trigger sells.
        /// </summary>
        public decimal EntryStochasticLevel
        {
                get => _entryStochasticLevel.Value;
                set => _entryStochasticLevel.Value = value;
        }

        /// <summary>
        /// %K period for the exit Stochastic oscillator.
        /// </summary>
        public int StochasticExitKPeriod
        {
                get => _stochasticExitKPeriod.Value;
                set => _stochasticExitKPeriod.Value = value;
        }

        /// <summary>
        /// %D period for the exit Stochastic oscillator.
        /// </summary>
        public int StochasticExitDPeriod
        {
                get => _stochasticExitDPeriod.Value;
                set => _stochasticExitDPeriod.Value = value;
        }

        /// <summary>
        /// Slowing factor for the exit Stochastic oscillator.
        /// </summary>
        public int StochasticExitSlow
        {
                get => _stochasticExitSlow.Value;
                set => _stochasticExitSlow.Value = value;
        }

        /// <summary>
        /// Upper bound used to detect oversold conditions for exits.
        /// </summary>
        public decimal ExitStochasticLevel
        {
                get => _exitStochasticLevel.Value;
                set => _exitStochasticLevel.Value = value;
        }

        /// <summary>
        /// Williams %R threshold that confirms an overbought market.
        /// </summary>
        public decimal EntryWprThreshold
        {
                get => _entryWprThreshold.Value;
                set => _entryWprThreshold.Value = value;
        }

        /// <summary>
        /// Williams %R threshold that confirms oversold conditions.
        /// </summary>
        public decimal ExitWprThreshold
        {
                get => _exitWprThreshold.Value;
                set => _exitWprThreshold.Value = value;
        }

        /// <summary>
        /// Average profit level that triggers a defensive exit.
        /// </summary>
        public decimal LossExitPips
        {
                get => _lossExitPips.Value;
                set => _lossExitPips.Value = value;
        }

        /// <summary>
        /// Average profit level that locks in positive moves.
        /// </summary>
        public decimal ProfitExitPips
        {
                get => _profitExitPips.Value;
                set => _profitExitPips.Value = value;
        }

        /// <summary>
        /// Take profit distance assigned to newly opened positions.
        /// </summary>
        public decimal TakeProfitPips
        {
                get => _takeProfitPips.Value;
                set => _takeProfitPips.Value = value;
        }

        /// <summary>
        /// Initial trade volume of the martingale ladder.
        /// </summary>
        public decimal InitialVolume
        {
                get => _initialVolume.Value;
                set => _initialVolume.Value = value;
        }

        /// <summary>
        /// Multiplier applied after a losing cycle.
        /// </summary>
        public decimal MartingaleMultiplier
        {
                get => _martingaleMultiplier.Value;
                set => _martingaleMultiplier.Value = value;
        }

        /// <summary>
        /// Maximum allowed trade size.
        /// </summary>
        public decimal MaxVolume
        {
                get => _maxVolume.Value;
                set => _maxVolume.Value = value;
        }

        /// <inheritdoc />
        public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
        {
                return [(Security, CandleType)];
        }

        /// <inheritdoc />
        protected override void OnReseted()
        {
                base.OnReseted();

                _openShortLots.Clear();
                _macd = null!;
                _wprEntry = null!;
                _wprExit = null!;
                _stochasticEntry = null!;
                _stochasticExit = null!;
                _previousEntryK = 0m;
                _hasPreviousEntryK = false;
                _pipSize = 0m;
                _currentVolume = 0m;
                _lastClosedPnL = 0m;
        }

        /// <inheritdoc />
        protected override void OnStarted(DateTimeOffset time)
        {
                base.OnStarted(time);

                _macd = new MovingAverageConvergenceDivergenceSignal
                {
                        Macd =
                        {
                                ShortMa = { Length = MacdFastPeriod },
                                LongMa = { Length = MacdSlowPeriod }
                        },
                        SignalMa = { Length = MacdSignalPeriod }
                };

                _wprEntry = new WilliamsPercentRange { Length = EntryWprPeriod };
                _wprExit = new WilliamsPercentRange { Length = ExitWprPeriod };

                _stochasticEntry = new StochasticOscillator
                {
                        KPeriod = StochasticEntryKPeriod,
                        DPeriod = StochasticEntryDPeriod,
                        Smooth = StochasticEntrySlow
                };

                _stochasticExit = new StochasticOscillator
                {
                        KPeriod = StochasticExitKPeriod,
                        DPeriod = StochasticExitDPeriod,
                        Smooth = StochasticExitSlow
                };

                UpdatePipSize();
                _currentVolume = InitialVolume;

                var subscription = SubscribeCandles(CandleType);
                subscription
                        .BindEx(_macd, _stochasticEntry, _stochasticExit, _wprEntry, _wprExit, ProcessCandle)
                        .Start();

                var area = CreateChartArea();
                if (area != null)
                {
                        DrawCandles(area, subscription);
                        DrawIndicator(area, _macd);
                        DrawIndicator(area, _stochasticEntry);
                        DrawIndicator(area, _stochasticExit);
                }

                if (TakeProfitPips > 0m && _pipSize > 0m)
                {
                        var takeProfit = new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute);
                        StartProtection(takeProfit: takeProfit, useMarketOrders: true);
                }
        }

        /// <inheritdoc />
        protected override void OnNewMyTrade(MyTrade trade)
        {
                base.OnNewMyTrade(trade);

                if (trade.Trade == null)
                        return;

                var order = trade.Order;
                if (order?.Direction == null)
                        return;

                var volume = trade.Trade.Volume ?? order.Volume ?? 0m;
                if (volume <= 0m)
                        return;

                var price = trade.Trade.Price ?? order.Price ?? 0m;
                if (price <= 0m)
                        return;

                volume = Math.Abs(volume);

                if (order.Direction == Sides.Sell)
                {
                        // Remember every short entry to emulate the per-ticket accounting from MetaTrader.
                        _openShortLots.Add(new PositionLot(volume, price));
                        return;
                }

                if (order.Direction != Sides.Buy)
                        return;

                // Calculate realized PnL for the lots that were closed by the buy fill.
                var remaining = volume;
                var realized = 0m;

                while (remaining > 0m && _openShortLots.Count > 0)
                {
                        var lot = _openShortLots[0];
                        var used = Math.Min(lot.Volume, remaining);

                        realized += (lot.Price - price) * used;

                        if (used >= lot.Volume)
                        {
                                _openShortLots.RemoveAt(0);
                        }
                        else
                        {
                                _openShortLots[0] = new PositionLot(lot.Volume - used, lot.Price);
                        }

                        remaining -= used;
                }

                _lastClosedPnL += realized;
        }

        /// <inheritdoc />
        protected override void OnPositionChanged(decimal delta)
        {
                base.OnPositionChanged(delta);

                if (Position != 0m)
                        return;

                if (_lastClosedPnL >= 0m)
                {
                        // Positive or breakeven cycle resets the ladder.
                        _currentVolume = InitialVolume;
                }
                else if (MartingaleMultiplier > 0m)
                {
                        // Losing cycle increases the exposure for the next entry.
                        var nextVolume = _currentVolume * MartingaleMultiplier;
                        _currentVolume = Math.Min(nextVolume, MaxVolume);
                }

                _lastClosedPnL = 0m;
        }

        private void ProcessCandle(
                ICandleMessage candle,
                IIndicatorValue macdValue,
                IIndicatorValue stochasticEntryValue,
                IIndicatorValue stochasticExitValue,
                IIndicatorValue wprEntryValue,
                IIndicatorValue wprExitValue)
        {
                if (candle.State != CandleStates.Finished)
                        return;

                if (!macdValue.IsFinal || !stochasticEntryValue.IsFinal || !stochasticExitValue.IsFinal || !wprEntryValue.IsFinal || !wprExitValue.IsFinal)
                        return;

                if (!IsFormedAndOnlineAndAllowTrading())
                        return;

                UpdatePipSize();
                if (_pipSize <= 0m)
                        return;

                var macd = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
                if (macd.Macd is not decimal macdLine)
                        return;

                var entryStochastic = (StochasticOscillatorValue)stochasticEntryValue;
                if (entryStochastic.K is not decimal entryK)
                        return;

                var exitStochastic = (StochasticOscillatorValue)stochasticExitValue;
                var exitK = exitStochastic.K as decimal?;

                var wprEntry = wprEntryValue.ToDecimal();
                var wprExit = wprExitValue.ToDecimal();

                TryEnterShort(macdLine, entryK, wprEntry);
                TryExitShort(candle, exitK, wprExit);

                _previousEntryK = entryK;
                _hasPreviousEntryK = true;
        }

        private void TryEnterShort(decimal macdLine, decimal entryK, decimal wprEntry)
        {
                if (!IsFormedAndAllowTrading())
                        return;

                if (macdLine <= MacdThreshold)
                        return;

                if (wprEntry <= EntryWprThreshold)
                        return;

                if (!_hasPreviousEntryK)
                        return;

                // Look for %K crossing below the configured threshold.
                var crossedDown = _previousEntryK >= EntryStochasticLevel && entryK < EntryStochasticLevel;
                if (!crossedDown)
                        return;

                if (_currentVolume <= 0m)
                        _currentVolume = InitialVolume;

                var volume = Math.Min(_currentVolume, MaxVolume);
                if (volume <= 0m)
                        return;

                SellMarket(volume);
        }

        private void TryExitShort(ICandleMessage candle, decimal? exitK, decimal wprExit)
        {
                if (Position >= 0m)
                        return;

                var averageProfit = GetAverageShortProfitInPips(candle.ClosePrice);

                if (averageProfit < LossExitPips && wprExit < ExitWprThreshold)
                {
                        CloseShortPosition();
                        return;
                }

                if (averageProfit > ProfitExitPips && exitK is decimal exitKValue && exitKValue < ExitStochasticLevel)
                {
                        CloseShortPosition();
                }
        }

        private void CloseShortPosition()
        {
                var volume = Math.Abs(Position);
                if (volume <= 0m)
                        return;

                BuyMarket(volume);
        }

        private decimal GetAverageShortProfitInPips(decimal currentPrice)
        {
                if (_openShortLots.Count == 0 || _pipSize <= 0m)
                        return 0m;

                decimal totalPips = 0m;
                foreach (var lot in _openShortLots)
                {
                        totalPips += (lot.Price - currentPrice) / _pipSize;
                }

                return totalPips / _openShortLots.Count;
        }

        private void UpdatePipSize()
        {
                var step = Security?.PriceStep ?? 0m;
                if (step > 0m)
                        _pipSize = step;
        }

        private bool IsFormedAndAllowTrading()
        {
                return Volume > 0m && Security != null;
        }

        private readonly struct PositionLot
        {
                public PositionLot(decimal volume, decimal price)
                {
                        Volume = volume;
                        Price = price;
                }

                public decimal Volume { get; }
                public decimal Price { get; }
        }
}
