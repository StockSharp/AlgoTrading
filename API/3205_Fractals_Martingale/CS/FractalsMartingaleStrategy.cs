using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fractals Martingale strategy converted from the MetaTrader expert "Fractals Martingale".
/// Combines Ichimoku trend filtering, monthly MACD confirmation and martingale position sizing.
/// </summary>
public class FractalsMartingaleStrategy : Strategy
{
        private sealed record CandleInfo(DateTimeOffset Time, decimal Open, decimal High, decimal Low, decimal Close);

        private readonly StrategyParam<decimal> _tradeVolume;
        private readonly StrategyParam<decimal> _multiplier;
        private readonly StrategyParam<int> _stopLossPips;
        private readonly StrategyParam<int> _takeProfitPips;
        private readonly StrategyParam<int> _fractalDepth;
        private readonly StrategyParam<int> _fractalLookback;
        private readonly StrategyParam<int> _startHour;
        private readonly StrategyParam<int> _endHour;
        private readonly StrategyParam<int> _maxConsecutiveLosses;
        private readonly StrategyParam<int> _pauseMinutes;
        private readonly StrategyParam<int> _tenkanPeriod;
        private readonly StrategyParam<int> _kijunPeriod;
        private readonly StrategyParam<int> _senkouPeriod;
        private readonly StrategyParam<int> _macdFastPeriod;
        private readonly StrategyParam<int> _macdSlowPeriod;
        private readonly StrategyParam<int> _macdSignalPeriod;
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<DataType> _ichimokuCandleType;
        private readonly StrategyParam<DataType> _macdCandleType;

        private readonly List<CandleInfo> _candles = new();

        private decimal _pipSize;
        private decimal _currentVolume;
        private decimal? _latestTenkan;
        private decimal? _latestKijun;
        private decimal? _lastMacd;
        private decimal? _lastMacdSignal;
        private decimal? _activeBullishFractal;
        private decimal? _activeBearishFractal;
        private long? _bullishFractalIndex;
        private long? _bearishFractalIndex;
        private long _processedCandles;
        private int _consecutiveLosses;
        private DateTimeOffset? _pauseUntil;

        /// <summary>
        /// Trade volume for the initial order.
        /// </summary>
        public decimal TradeVolume
        {
                get => _tradeVolume.Value;
                set => _tradeVolume.Value = value;
        }

        /// <summary>
        /// Multiplier applied after a losing trade.
        /// </summary>
        public decimal Multiplier
        {
                get => _multiplier.Value;
                set => _multiplier.Value = value;
        }

        /// <summary>
        /// Stop-loss distance expressed in pips.
        /// </summary>
        public int StopLossPips
        {
                get => _stopLossPips.Value;
                set => _stopLossPips.Value = value;
        }

        /// <summary>
        /// Take-profit distance expressed in pips.
        /// </summary>
        public int TakeProfitPips
        {
                get => _takeProfitPips.Value;
                set => _takeProfitPips.Value = value;
        }

        /// <summary>
        /// Number of candles on each side used to detect a fractal peak or trough.
        /// </summary>
        public int FractalDepth
        {
                get => _fractalDepth.Value;
                set => _fractalDepth.Value = value;
        }

        /// <summary>
        /// Maximum number of processed candles to keep a fractal valid.
        /// </summary>
        public int FractalLookback
        {
                get => _fractalLookback.Value;
                set => _fractalLookback.Value = value;
        }

        /// <summary>
        /// Start hour (inclusive) for trading.
        /// </summary>
        public int StartHour
        {
                get => _startHour.Value;
                set => _startHour.Value = value;
        }

        /// <summary>
        /// End hour (exclusive) for trading.
        /// </summary>
        public int EndHour
        {
                get => _endHour.Value;
                set => _endHour.Value = value;
        }

        /// <summary>
        /// Number of consecutive losses allowed before pausing.
        /// </summary>
        public int MaxConsecutiveLosses
        {
                get => _maxConsecutiveLosses.Value;
                set => _maxConsecutiveLosses.Value = value;
        }

        /// <summary>
        /// Pause duration in minutes when the consecutive loss limit is exceeded.
        /// </summary>
        public int PauseMinutes
        {
                get => _pauseMinutes.Value;
                set => _pauseMinutes.Value = value;
        }

        /// <summary>
        /// Tenkan-sen period for the Ichimoku indicator.
        /// </summary>
        public int TenkanPeriod
        {
                get => _tenkanPeriod.Value;
                set => _tenkanPeriod.Value = value;
        }

        /// <summary>
        /// Kijun-sen period for the Ichimoku indicator.
        /// </summary>
        public int KijunPeriod
        {
                get => _kijunPeriod.Value;
                set => _kijunPeriod.Value = value;
        }

        /// <summary>
        /// Senkou Span B period for the Ichimoku indicator.
        /// </summary>
        public int SenkouPeriod
        {
                get => _senkouPeriod.Value;
                set => _senkouPeriod.Value = value;
        }

        /// <summary>
        /// Fast EMA period for the MACD filter.
        /// </summary>
        public int MacdFastPeriod
        {
                get => _macdFastPeriod.Value;
                set => _macdFastPeriod.Value = value;
        }

        /// <summary>
        /// Slow EMA period for the MACD filter.
        /// </summary>
        public int MacdSlowPeriod
        {
                get => _macdSlowPeriod.Value;
                set => _macdSlowPeriod.Value = value;
        }

        /// <summary>
        /// Signal EMA period for the MACD filter.
        /// </summary>
        public int MacdSignalPeriod
        {
                get => _macdSignalPeriod.Value;
                set => _macdSignalPeriod.Value = value;
        }

        /// <summary>
        /// Primary candle series used for fractal detection.
        /// </summary>
        public DataType CandleType
        {
                get => _candleType.Value;
                set => _candleType.Value = value;
        }

        /// <summary>
        /// Candle series used to evaluate the Ichimoku trend filter.
        /// </summary>
        public DataType IchimokuCandleType
        {
                get => _ichimokuCandleType.Value;
                set => _ichimokuCandleType.Value = value;
        }

        /// <summary>
        /// Candle series used to compute the MACD confirmation filter.
        /// </summary>
        public DataType MacdCandleType
        {
                get => _macdCandleType.Value;
                set => _macdCandleType.Value = value;
        }

        /// <summary>
        /// Initialize the strategy parameters.
        /// </summary>
        public FractalsMartingaleStrategy()
        {
                _tradeVolume = Param(nameof(TradeVolume), 0.1m)
                        .SetDisplay("Trade Volume", "Base market order volume", "Risk Management");

                _multiplier = Param(nameof(Multiplier), 2m)
                        .SetGreaterThanZero()
                        .SetDisplay("Multiplier", "Volume multiplier applied after a losing trade", "Risk Management");

                _stopLossPips = Param(nameof(StopLossPips), 100)
                        .SetNotNegative()
                        .SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk Management");

                _takeProfitPips = Param(nameof(TakeProfitPips), 50)
                        .SetNotNegative()
                        .SetDisplay("Take Profit (pips)", "Target distance in pips", "Risk Management");

                _fractalDepth = Param(nameof(FractalDepth), 2)
                        .SetGreaterThanZero()
                        .SetDisplay("Fractal Depth", "Number of candles on each side for fractal detection", "Signals");

                _fractalLookback = Param(nameof(FractalLookback), 200)
                        .SetGreaterThanZero()
                        .SetDisplay("Fractal Lookback", "Maximum number of candles to keep a fractal active", "Signals");

                _startHour = Param(nameof(StartHour), 0)
                        .SetDisplay("Start Hour", "Trading window start hour (inclusive)", "Session");

                _endHour = Param(nameof(EndHour), 24)
                        .SetDisplay("End Hour", "Trading window end hour (exclusive)", "Session");

                _maxConsecutiveLosses = Param(nameof(MaxConsecutiveLosses), 3)
                        .SetNotNegative()
                        .SetDisplay("Max Losses", "Consecutive losses before triggering a pause", "Risk Management");

                _pauseMinutes = Param(nameof(PauseMinutes), 180)
                        .SetNotNegative()
                        .SetDisplay("Pause Minutes", "Duration of the cool-down after too many losses", "Risk Management");

                _tenkanPeriod = Param(nameof(TenkanPeriod), 9)
                        .SetGreaterThanZero()
                        .SetDisplay("Tenkan Period", "Ichimoku Tenkan-sen length", "Indicators");

                _kijunPeriod = Param(nameof(KijunPeriod), 26)
                        .SetGreaterThanZero()
                        .SetDisplay("Kijun Period", "Ichimoku Kijun-sen length", "Indicators");

                _senkouPeriod = Param(nameof(SenkouPeriod), 52)
                        .SetGreaterThanZero()
                        .SetDisplay("Senkou Period", "Ichimoku Senkou Span B length", "Indicators");

                _macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
                        .SetGreaterThanZero()
                        .SetDisplay("MACD Fast", "MACD fast EMA length", "Indicators");

                _macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
                        .SetGreaterThanZero()
                        .SetDisplay("MACD Slow", "MACD slow EMA length", "Indicators");

                _macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
                        .SetGreaterThanZero()
                        .SetDisplay("MACD Signal", "MACD signal EMA length", "Indicators");

                _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
                        .SetDisplay("Primary Candle", "Primary candle series for the strategy", "General");

                _ichimokuCandleType = Param(nameof(IchimokuCandleType), TimeSpan.FromMinutes(15).TimeFrame())
                        .SetDisplay("Ichimoku Candle", "Higher time frame used for Ichimoku", "General");

                _macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(30).TimeFrame())
                        .SetDisplay("MACD Candle", "Time frame used for the MACD confirmation", "General");
        }

        /// <inheritdoc />
        public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
        {
                if (Security is null)
                        yield break;

                var seen = new HashSet<string>(StringComparer.Ordinal);

                foreach (var dataType in new[] { CandleType, IchimokuCandleType, MacdCandleType })
                {
                        var key = dataType.ToString();
                        if (seen.Add(key))
                                yield return (Security, dataType);
                }
        }

        /// <inheritdoc />
        protected override void OnReseted()
        {
                base.OnReseted();

                _candles.Clear();
                _pipSize = 0m;
                _currentVolume = 0m;
                _latestTenkan = null;
                _latestKijun = null;
                _lastMacd = null;
                _lastMacdSignal = null;
                _activeBullishFractal = null;
                _activeBearishFractal = null;
                _bullishFractalIndex = null;
                _bearishFractalIndex = null;
                _processedCandles = 0;
                _consecutiveLosses = 0;
                _pauseUntil = null;

                Volume = AlignVolume(TradeVolume);
                _currentVolume = Volume;
        }

        /// <inheritdoc />
        protected override void OnStarted(DateTimeOffset time)
        {
                base.OnStarted(time);

                Volume = AlignVolume(TradeVolume);
                _currentVolume = Volume;

                _pipSize = Security?.PriceStep ?? 1m;
                if (_pipSize == 0m)
                        _pipSize = 1m;
                else if (_pipSize == 0.00001m || _pipSize == 0.001m)
                        _pipSize *= 10m;

                var ichimoku = new Ichimoku
                {
                        Tenkan = { Length = TenkanPeriod },
                        Kijun = { Length = KijunPeriod },
                        SenkouB = { Length = SenkouPeriod },
                };

                var macd = new MovingAverageConvergenceDivergenceSignal
                {
                        Macd =
                        {
                                ShortMa = { Length = MacdFastPeriod },
                                LongMa = { Length = MacdSlowPeriod },
                        },
                        SignalMa = { Length = MacdSignalPeriod },
                };

                var mainSubscription = SubscribeCandles(CandleType);
                mainSubscription
                        .Bind(ProcessMainCandle)
                        .Start();

                var ichimokuSubscription = SubscribeCandles(IchimokuCandleType);
                ichimokuSubscription
                        .BindEx(ichimoku, ProcessIchimoku)
                        .Start();

                var macdSubscription = SubscribeCandles(MacdCandleType);
                macdSubscription
                        .BindEx(macd, ProcessMacd)
                        .Start();

                var area = CreateChartArea();
                if (area != null)
                {
                        DrawCandles(area, mainSubscription);
                        DrawOwnTrades(area);
                }

                var stopLossUnit = StopLossPips > 0 ? new Unit(StopLossPips * _pipSize, UnitTypes.Absolute) : null;
                var takeProfitUnit = TakeProfitPips > 0 ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute) : null;

                StartProtection(takeProfit: takeProfitUnit, stopLoss: stopLossUnit);
        }

        private void ProcessMainCandle(ICandleMessage candle)
        {
                if (candle.State != CandleStates.Finished)
                        return;

                _processedCandles++;

                var info = new CandleInfo(candle.OpenTime, candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice);
                _candles.Add(info);

                var maxCandles = Math.Max(FractalDepth * 2 + 50, FractalLookback + FractalDepth + 5);
                if (_candles.Count > maxCandles)
                        _candles.RemoveAt(0);

                DetectFractals();
                UpdateFractalLifetime();

                if (!IsFormedAndOnlineAndAllowTrading())
                        return;

                if (_pauseUntil is DateTimeOffset pause)
                {
                        if (candle.OpenTime < pause)
                                return;

                        _pauseUntil = null;
                }

                if (!IsWithinTradingHours(candle.OpenTime))
                        return;

                if (_lastMacd is not decimal macd || _lastMacdSignal is not decimal macdSignal)
                        return;

                if (_activeBullishFractal is decimal bullishFractal && macd > macdSignal)
                {
                        if (Position <= 0 && candle.ClosePrice > bullishFractal)
                                TryEnterLong();
                }
                else if (_activeBearishFractal is decimal bearishFractal && macd < macdSignal)
                {
                        if (Position >= 0 && candle.ClosePrice < bearishFractal)
                                TryEnterShort();
                }
        }

        private void ProcessIchimoku(ICandleMessage candle, IIndicatorValue indicatorValue)
        {
                if (candle.State != CandleStates.Finished)
                        return;

                if (indicatorValue is not IchimokuValue ichimokuValue)
                        return;

                if (ichimokuValue.Tenkan is decimal tenkan)
                        _latestTenkan = tenkan;

                if (ichimokuValue.Kijun is decimal kijun)
                        _latestKijun = kijun;
        }

        private void ProcessMacd(ICandleMessage candle, IIndicatorValue indicatorValue)
        {
                if (candle.State != CandleStates.Finished)
                        return;

                if (indicatorValue is not MovingAverageConvergenceDivergenceSignalValue macdValue)
                        return;

                if (macdValue.Macd is decimal macd)
                        _lastMacd = macd;

                if (macdValue.Signal is decimal signal)
                        _lastMacdSignal = signal;
        }

        private void TryEnterLong()
        {
                var volume = CalculateOrderVolume(true);
                if (volume <= 0m)
                        return;

                BuyMarket(volume);
                _activeBullishFractal = null;
                _bullishFractalIndex = null;
        }

        private void TryEnterShort()
        {
                var volume = CalculateOrderVolume(false);
                if (volume <= 0m)
                        return;

                SellMarket(volume);
                _activeBearishFractal = null;
                _bearishFractalIndex = null;
        }

        private decimal CalculateOrderVolume(bool isLong)
        {
                var alignedVolume = AlignVolume(_currentVolume);
                if (alignedVolume <= 0m)
                        return 0m;

                if (isLong && Position < 0m)
                        return alignedVolume + Math.Abs(Position);

                if (!isLong && Position > 0m)
                        return alignedVolume + Position;

                return alignedVolume;
        }

        private decimal AlignVolume(decimal volume)
        {
                if (volume <= 0m)
                        return 0m;

                var step = Security?.VolumeStep ?? 0m;
                if (step > 0m)
                {
                        var multiplier = Math.Max(1m, Math.Round(volume / step));
                        volume = multiplier * step;
                }

                var minVolume = Security?.MinVolume ?? 0m;
                if (minVolume > 0m && volume < minVolume)
                        volume = minVolume;

                var maxVolume = Security?.MaxVolume ?? 0m;
                if (maxVolume > 0m && volume > maxVolume)
                        volume = maxVolume;

                return volume;
        }

        private void DetectFractals()
        {
                var depth = Math.Max(1, FractalDepth);
                var count = _candles.Count;

                if (count < depth * 2 + 1)
                        return;

                var centerIndex = count - depth - 1;
                if (centerIndex < depth)
                        return;

                if (centerIndex + 1 >= count)
                        return;

                if (_latestTenkan is not decimal tenkan || _latestKijun is not decimal kijun)
                        return;

                var center = _candles[centerIndex];

                var isUpFractal = true;
                var isDownFractal = true;

                for (var i = 1; i <= depth; i++)
                {
                        var before = _candles[centerIndex - i];
                        var after = _candles[centerIndex + i];

                        if (center.High <= before.High || center.High <= after.High)
                                isUpFractal = false;

                        if (center.Low >= before.Low || center.Low >= after.Low)
                                isDownFractal = false;

                        if (!isUpFractal && !isDownFractal)
                                break;
                }

                if (isUpFractal && tenkan > kijun)
                {
                        var confirmation = _candles[centerIndex + 1];
                        if (confirmation.Open > center.High)
                        {
                                _activeBullishFractal = center.High;
                                _bullishFractalIndex = _processedCandles - depth - 1;
                                LogInfo($"Detected bullish fractal at {center.Time:O} price {center.High}");
                        }
                }

                if (isDownFractal && tenkan < kijun)
                {
                        var confirmation = _candles[centerIndex + 1];
                        if (confirmation.Open < center.Low)
                        {
                                _activeBearishFractal = center.Low;
                                _bearishFractalIndex = _processedCandles - depth - 1;
                                LogInfo($"Detected bearish fractal at {center.Time:O} price {center.Low}");
                        }
                }
        }

        private void UpdateFractalLifetime()
        {
                if (_bullishFractalIndex.HasValue && _processedCandles - _bullishFractalIndex.Value > FractalLookback)
                {
                        _activeBullishFractal = null;
                        _bullishFractalIndex = null;
                }

                if (_bearishFractalIndex.HasValue && _processedCandles - _bearishFractalIndex.Value > FractalLookback)
                {
                        _activeBearishFractal = null;
                        _bearishFractalIndex = null;
                }
        }

        private bool IsWithinTradingHours(DateTimeOffset time)
        {
                var hour = time.Hour;

                if (StartHour == EndHour)
                        return true;

                if (StartHour < EndHour)
                        return hour >= StartHour && hour < EndHour;

                return hour >= StartHour || hour < EndHour;
        }

        /// <inheritdoc />
        protected override void OnOwnTradeReceived(MyTrade myTrade)
        {
                base.OnOwnTradeReceived(myTrade);

                if (Position != 0)
                        return;

                if (myTrade.PnL < 0m)
                {
                        _consecutiveLosses++;
                        _currentVolume = AlignVolume(_currentVolume * Multiplier);

                        if (MaxConsecutiveLosses > 0 && _consecutiveLosses > MaxConsecutiveLosses)
                        {
                                if (PauseMinutes > 0)
                                {
                                        var referenceTime = myTrade.Trade?.Time ?? CurrentTime ?? DateTimeOffset.UtcNow;
                                        _pauseUntil = referenceTime + TimeSpan.FromMinutes(PauseMinutes);
                                        LogInfo($"Trading paused until {_pauseUntil:O} after {_consecutiveLosses} losses.");
                                }

                                _consecutiveLosses = 0;
                        }
                }
                else
                {
                        _consecutiveLosses = 0;
                        _currentVolume = Volume;
                }
        }
}

