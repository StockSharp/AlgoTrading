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

using System.Reflection;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the MetaTrader 5 expert advisor Exp_XPVT.
/// Uses the Price and Volume Trend indicator with configurable smoothing
/// and applied price to trade crosses between the raw and smoothed series.
/// </summary>
public class ExpXpvtStrategy : Strategy
{
        private readonly StrategyParam<decimal> _orderVolume;
        private readonly StrategyParam<decimal> _stopLossPoints;
        private readonly StrategyParam<decimal> _takeProfitPoints;
        private readonly StrategyParam<bool> _allowBuyOpen;
        private readonly StrategyParam<bool> _allowSellOpen;
        private readonly StrategyParam<bool> _allowBuyClose;
        private readonly StrategyParam<bool> _allowSellClose;
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<AppliedVolumeOption> _volumeSource;
        private readonly StrategyParam<SmoothingMethod> _smoothingMethod;
        private readonly StrategyParam<int> _smoothingLength;
        private readonly StrategyParam<int> _smoothingPhase;
        private readonly StrategyParam<AppliedPriceOption> _priceSource;
        private readonly StrategyParam<int> _signalBar;

        private LengthIndicator<decimal> _signalSmoother = null!;
        private decimal _pvt;
        private decimal? _previousPrice;
        private readonly List<decimal> _pvtHistory = new();
        private readonly List<decimal> _signalHistory = new();

        /// <summary>
        /// Initializes a new instance of <see cref="ExpXpvtStrategy"/>.
        /// </summary>
        public ExpXpvtStrategy()
        {
                _orderVolume = Param(nameof(OrderVolume), 1m)
                        .SetDisplay("Order Volume", "Base volume used for new entries", "Orders")
                        .SetGreaterThanZero();

                _stopLossPoints = Param(nameof(StopLossPoints), 1000m)
                        .SetDisplay("Stop Loss", "Protective stop distance in price steps", "Risk")
                        .SetNotNegative();

                _takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
                        .SetDisplay("Take Profit", "Profit target distance in price steps", "Risk")
                        .SetNotNegative();

                _allowBuyOpen = Param(nameof(AllowBuyOpen), true)
                        .SetDisplay("Allow Buy Entry", "Enable opening long positions", "Trading");

                _allowSellOpen = Param(nameof(AllowSellOpen), true)
                        .SetDisplay("Allow Sell Entry", "Enable opening short positions", "Trading");

                _allowBuyClose = Param(nameof(AllowBuyClose), true)
                        .SetDisplay("Allow Buy Exit", "Enable closing long positions on opposite signals", "Trading");

                _allowSellClose = Param(nameof(AllowSellClose), true)
                        .SetDisplay("Allow Sell Exit", "Enable closing short positions on opposite signals", "Trading");

                _candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
                        .SetDisplay("Candle Type", "Timeframe used for Price and Volume Trend", "Data");

                _volumeSource = Param(nameof(VolumeSource), AppliedVolumeOption.TickVolume)
                        .SetDisplay("Volume Source", "Volume applied inside Price and Volume Trend", "Indicator");

                _smoothingMethod = Param(nameof(Smoothing), SmoothingMethod.Exponential)
                        .SetDisplay("Smoothing Method", "Moving average applied to the PVT signal", "Indicator");

                _smoothingLength = Param(nameof(SmoothingLength), 5)
                        .SetDisplay("Smoothing Length", "Period of the smoothing filter", "Indicator")
                        .SetGreaterThanZero();

                _smoothingPhase = Param(nameof(SmoothingPhase), 15)
                        .SetDisplay("Smoothing Phase", "Phase parameter used by Jurik-style averages", "Indicator");

                _priceSource = Param(nameof(PriceSource), AppliedPriceOption.Close)
                        .SetDisplay("Applied Price", "Price used to build Price and Volume Trend", "Indicator");

                _signalBar = Param(nameof(SignalBar), 1)
                        .SetDisplay("Signal Bar", "Historical shift used when reading signals", "Indicator")
                        .SetNotNegative();
        }

        /// <summary>
        /// Base volume used for new entries.
        /// </summary>
        public decimal OrderVolume
        {
                get => _orderVolume.Value;
                set => _orderVolume.Value = value;
        }

        /// <summary>
        /// Protective stop distance in price steps.
        /// </summary>
        public decimal StopLossPoints
        {
                get => _stopLossPoints.Value;
                set => _stopLossPoints.Value = value;
        }

        /// <summary>
        /// Profit target distance in price steps.
        /// </summary>
        public decimal TakeProfitPoints
        {
                get => _takeProfitPoints.Value;
                set => _takeProfitPoints.Value = value;
        }

        /// <summary>
        /// Enable opening long positions.
        /// </summary>
        public bool AllowBuyOpen
        {
                get => _allowBuyOpen.Value;
                set => _allowBuyOpen.Value = value;
        }

        /// <summary>
        /// Enable opening short positions.
        /// </summary>
        public bool AllowSellOpen
        {
                get => _allowSellOpen.Value;
                set => _allowSellOpen.Value = value;
        }

        /// <summary>
        /// Enable closing long positions on opposite signals.
        /// </summary>
        public bool AllowBuyClose
        {
                get => _allowBuyClose.Value;
                set => _allowBuyClose.Value = value;
        }

        /// <summary>
        /// Enable closing short positions on opposite signals.
        /// </summary>
        public bool AllowSellClose
        {
                get => _allowSellClose.Value;
                set => _allowSellClose.Value = value;
        }

        /// <summary>
        /// Timeframe used for Price and Volume Trend.
        /// </summary>
        public DataType CandleType
        {
                get => _candleType.Value;
                set => _candleType.Value = value;
        }

        /// <summary>
        /// Volume source applied to the PVT calculation.
        /// </summary>
        public AppliedVolumeOption VolumeSource
        {
                get => _volumeSource.Value;
                set => _volumeSource.Value = value;
        }

        /// <summary>
        /// Moving average applied to the PVT signal.
        /// </summary>
        public SmoothingMethod Smoothing
        {
                get => _smoothingMethod.Value;
                set => _smoothingMethod.Value = value;
        }

        /// <summary>
        /// Period of the smoothing filter.
        /// </summary>
        public int SmoothingLength
        {
                get => Math.Max(1, _smoothingLength.Value);
                set => _smoothingLength.Value = value;
        }

        /// <summary>
        /// Phase parameter used by Jurik-style averages.
        /// </summary>
        public int SmoothingPhase
        {
                get => _smoothingPhase.Value;
                set => _smoothingPhase.Value = value;
        }

        /// <summary>
        /// Price used to build Price and Volume Trend.
        /// </summary>
        public AppliedPriceOption PriceSource
        {
                get => _priceSource.Value;
                set => _priceSource.Value = value;
        }

        /// <summary>
        /// Historical shift used when reading signals.
        /// </summary>
        public int SignalBar
        {
                get => Math.Max(0, _signalBar.Value);
                set => _signalBar.Value = value;
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
                _pvt = 0m;
                _previousPrice = null;
                _pvtHistory.Clear();
                _signalHistory.Clear();
        }

        /// <inheritdoc />
        protected override void OnStarted(DateTimeOffset time)
        {
                base.OnStarted(time);

                Volume = OrderVolume;
                _signalSmoother = CreateSmoother(Smoothing, SmoothingLength, SmoothingPhase);
                _signalSmoother.Reset();

                var subscription = SubscribeCandles(CandleType);
                subscription
                        .Bind(ProcessCandle)
                        .Start();

                StartProtection();
        }

        private void ProcessCandle(ICandleMessage candle)
        {
                if (candle.State != CandleStates.Finished)
                        return;

                var price = GetPrice(candle, PriceSource);
                var volume = GetVolume(candle, VolumeSource);

                if (_previousPrice is null)
                {
                        _pvt = volume;
                }
                else if (_previousPrice.Value != 0m)
                {
                        var change = (price - _previousPrice.Value) / _previousPrice.Value;
                        _pvt += volume * change;
                }

                _previousPrice = price;

                var indicatorValue = _signalSmoother.Process(_pvt, candle.OpenTime, true).ToNullableDecimal();
                if (indicatorValue is null)
                        return;

                AppendHistory(_pvtHistory, _pvt);
                AppendHistory(_signalHistory, indicatorValue.Value);

                if (!IsFormedAndOnlineAndAllowTrading())
                        return;

                var required = SignalBar + 2;
                if (_pvtHistory.Count < required || _signalHistory.Count < required)
                        return;

                var currentIndex = _pvtHistory.Count - 1 - SignalBar;
                var previousIndex = currentIndex - 1;
                if (previousIndex < 0)
                        return;

                var currentPvt = _pvtHistory[currentIndex];
                var previousPvt = _pvtHistory[previousIndex];
                var currentSignal = _signalHistory[currentIndex];
                var previousSignal = _signalHistory[previousIndex];

                var closeShort = AllowSellClose && previousPvt > previousSignal;
                var closeLong = AllowBuyClose && previousPvt < previousSignal;
                var openLong = AllowBuyOpen && previousPvt > previousSignal && currentPvt <= currentSignal;
                var openShort = AllowSellOpen && previousPvt < previousSignal && currentPvt >= currentSignal;

                if (closeShort && Position < 0)
                {
                        ClosePosition();
                }

                if (closeLong && Position > 0)
                {
                        ClosePosition();
                }

                if (openLong && Position <= 0)
                {
                        OpenLong(candle);
                }
                else if (openShort && Position >= 0)
                {
                        OpenShort(candle);
                }
        }

        private void OpenLong(ICandleMessage candle)
        {
                var baseVolume = OrderVolume;
                var requiredVolume = baseVolume + Math.Max(0m, -Position);
                if (requiredVolume <= 0m)
                        return;

                BuyMarket(requiredVolume);

                var resultingPosition = Position + requiredVolume;
                ApplyRiskManagement(candle.ClosePrice, resultingPosition);
        }

        private void OpenShort(ICandleMessage candle)
        {
                var baseVolume = OrderVolume;
                var requiredVolume = baseVolume + Math.Max(0m, Position);
                if (requiredVolume <= 0m)
                        return;

                SellMarket(requiredVolume);

                var resultingPosition = Position - requiredVolume;
                ApplyRiskManagement(candle.ClosePrice, resultingPosition);
        }

        private void ApplyRiskManagement(decimal referencePrice, decimal resultingPosition)
        {
                if (TakeProfitPoints > 0m)
                        SetTakeProfit(TakeProfitPoints, referencePrice, resultingPosition);

                if (StopLossPoints > 0m)
                        SetStopLoss(StopLossPoints, referencePrice, resultingPosition);
        }

        private static decimal GetPrice(ICandleMessage candle, AppliedPriceOption price)
        {
                return price switch
                {
                        AppliedPriceOption.Close => candle.ClosePrice,
                        AppliedPriceOption.Open => candle.OpenPrice,
                        AppliedPriceOption.High => candle.HighPrice,
                        AppliedPriceOption.Low => candle.LowPrice,
                        AppliedPriceOption.Median => (candle.HighPrice + candle.LowPrice) / 2m,
                        AppliedPriceOption.Typical => (candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 3m,
                        AppliedPriceOption.Weighted => (2m * candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
                        AppliedPriceOption.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
                        AppliedPriceOption.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
                        AppliedPriceOption.TrendFollow0 => candle.ClosePrice > candle.OpenPrice
                                ? candle.HighPrice
                                : candle.ClosePrice < candle.OpenPrice
                                        ? candle.LowPrice
                                        : candle.ClosePrice,
                        AppliedPriceOption.TrendFollow1 => candle.ClosePrice > candle.OpenPrice
                                ? (candle.HighPrice + candle.ClosePrice) / 2m
                                : candle.ClosePrice < candle.OpenPrice
                                        ? (candle.LowPrice + candle.ClosePrice) / 2m
                                        : candle.ClosePrice,
                        AppliedPriceOption.Demark =>
                                DemarkPrice(candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice),
                        _ => candle.ClosePrice,
                };
        }

        private static decimal DemarkPrice(decimal open, decimal high, decimal low, decimal close)
        {
                var result = high + low + close;

                if (close < open)
                        result = (result + low) / 2m;
                else if (close > open)
                        result = (result + high) / 2m;
                else
                        result = (result + close) / 2m;

                return ((result - low) + (result - high)) / 2m;
        }

        private static decimal GetVolume(ICandleMessage candle, AppliedVolumeOption volumeSource)
        {
                return volumeSource switch
                {
                        AppliedVolumeOption.TickVolume => candle.TotalTicks.HasValue
                                ? candle.TotalTicks.Value
                                : candle.TotalVolume ?? 0m,
                        AppliedVolumeOption.RealVolume => candle.TotalVolume ?? (candle.TotalTicks.HasValue
                                ? candle.TotalTicks.Value
                                : 0m),
                        _ => candle.TotalVolume ?? 0m,
                };
        }

        private void AppendHistory(List<decimal> history, decimal value)
        {
                history.Add(value);

                var maxItems = Math.Max(4, SignalBar + 4);
                if (history.Count > maxItems)
                        history.RemoveAt(0);
        }

        private static LengthIndicator<decimal> CreateSmoother(SmoothingMethod method, int length, int phase)
        {
                var normalizedLength = Math.Max(1, length);

                return method switch
                {
                        SmoothingMethod.Simple => new SimpleMovingAverage { Length = normalizedLength },
                        SmoothingMethod.Exponential => new ExponentialMovingAverage { Length = normalizedLength },
                        SmoothingMethod.Smoothed => new SmoothedMovingAverage { Length = normalizedLength },
                        SmoothingMethod.LinearWeighted => new WeightedMovingAverage { Length = normalizedLength },
                        SmoothingMethod.Jjma => CreateJurik(normalizedLength, phase),
                        SmoothingMethod.Jurx => CreateJurik(normalizedLength, phase),
                        SmoothingMethod.Parabolic => new ExponentialMovingAverage { Length = normalizedLength },
                        SmoothingMethod.TripleExponential => new TripleExponentialMovingAverage { Length = normalizedLength },
                        SmoothingMethod.Vidya => new ExponentialMovingAverage { Length = normalizedLength },
                        SmoothingMethod.Adaptive => new KaufmanAdaptiveMovingAverage { Length = normalizedLength },
                        _ => new SimpleMovingAverage { Length = normalizedLength },
                };
        }

        private static LengthIndicator<decimal> CreateJurik(int length, int phase)
        {
                var jurik = new JurikMovingAverage { Length = length };
                var property = jurik.GetType().GetProperty("Phase", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null)
                {
                        var normalizedPhase = Math.Max(-100, Math.Min(100, phase));
                        property.SetValue(jurik, normalizedPhase);
                }

                return jurik;
        }

        /// <summary>
        /// Volume source options used by the strategy.
        /// </summary>
        public enum AppliedVolumeOption
        {
                /// <summary>
                /// Use tick volume (fallback to real volume if unavailable).
                /// </summary>
                TickVolume,

                /// <summary>
                /// Use real volume (fallback to tick volume if unavailable).
                /// </summary>
                RealVolume,
        }

        /// <summary>
        /// Moving average smoothing method applied to PVT.
        /// </summary>
        public enum SmoothingMethod
        {
                /// <summary>
                /// Simple moving average.
                /// </summary>
                Simple,

                /// <summary>
                /// Exponential moving average.
                /// </summary>
                Exponential,

                /// <summary>
                /// Smoothed moving average (RMA).
                /// </summary>
                Smoothed,

                /// <summary>
                /// Linear weighted moving average.
                /// </summary>
                LinearWeighted,

                /// <summary>
                /// Jurik moving average (JJMA).
                /// </summary>
                Jjma,

                /// <summary>
                /// Jurik moving average (JurX variant).
                /// </summary>
                Jurx,

                /// <summary>
                /// Parabolic moving average approximation.
                /// </summary>
                Parabolic,

                /// <summary>
                /// Triple exponential moving average (T3).
                /// </summary>
                TripleExponential,

                /// <summary>
                /// VIDYA adaptive moving average (approximated by EMA).
                /// </summary>
                Vidya,

                /// <summary>
                /// Kaufman adaptive moving average.
                /// </summary>
                Adaptive,
        }

        /// <summary>
        /// Price source choices for the PVT calculation.
        /// </summary>
        public enum AppliedPriceOption
        {
                /// <summary>
                /// Close price.
                /// </summary>
                Close,

                /// <summary>
                /// Open price.
                /// </summary>
                Open,

                /// <summary>
                /// High price.
                /// </summary>
                High,

                /// <summary>
                /// Low price.
                /// </summary>
                Low,

                /// <summary>
                /// Median price (high + low) / 2.
                /// </summary>
                Median,

                /// <summary>
                /// Typical price (close + high + low) / 3.
                /// </summary>
                Typical,

                /// <summary>
                /// Weighted close (2 * close + high + low) / 4.
                /// </summary>
                Weighted,

                /// <summary>
                /// Simple price (open + close) / 2.
                /// </summary>
                Simple,

                /// <summary>
                /// Quarter price (open + close + high + low) / 4.
                /// </summary>
                Quarter,

                /// <summary>
                /// Trend-following price variant 0.
                /// </summary>
                TrendFollow0,

                /// <summary>
                /// Trend-following price variant 1.
                /// </summary>
                TrendFollow1,

                /// <summary>
                /// DeMark price.
                /// </summary>
                Demark,
        }
}

