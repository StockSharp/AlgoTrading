using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy based on the Three-Bar Reversal Up pattern.
    /// This pattern consists of three consecutive bars where:
    /// 1. First bar is bearish (close < open)
    /// 2. Second bar is bearish with a lower low than the first
    /// 3. Third bar is bullish and closes above the high of the second bar
    /// </summary>
    public class ThreeBarReversalUpStrategy : Strategy
    {
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<decimal> _stopLossPercent;
        private readonly StrategyParam<bool> _requireDowntrend;
        private readonly StrategyParam<int> _downtrendLength;

        private readonly Queue<ICandleMessage> _lastThreeCandles;
        private Lowest _lowestIndicator;

        /// <summary>
        /// Type of candles to use.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Stop-loss percentage below the pattern's low.
        /// </summary>
        public decimal StopLossPercent
        {
            get => _stopLossPercent.Value;
            set => _stopLossPercent.Value = value;
        }

        /// <summary>
        /// Whether to require a downtrend before the pattern.
        /// </summary>
        public bool RequireDowntrend
        {
            get => _requireDowntrend.Value;
            set => _requireDowntrend.Value = value;
        }

        /// <summary>
        /// Number of bars to look back for downtrend confirmation.
        /// </summary>
        public int DowntrendLength
        {
            get => _downtrendLength.Value;
            set => _downtrendLength.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreeBarReversalUpStrategy"/>.
        /// </summary>
        public ThreeBarReversalUpStrategy()
        {
            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
                .SetDisplay("Candle Type", "Type of candles to use", "General");

            _stopLossPercent = Param(nameof(StopLossPercent), 1.0m)
                .SetRange(0.5m, 3.0m)
                .SetDisplay("Stop Loss %", "Percentage below pattern's low for stop-loss", "Risk Management")
                .SetCanOptimize(true);

            _requireDowntrend = Param(nameof(RequireDowntrend), true)
                .SetDisplay("Require Downtrend", "Whether to require a prior downtrend", "Pattern Parameters");

            _downtrendLength = Param(nameof(DowntrendLength), 5)
                .SetRange(3, 10)
                .SetDisplay("Downtrend Length", "Number of bars to check for downtrend", "Pattern Parameters")
                .SetCanOptimize(true);

            _lastThreeCandles = new Queue<ICandleMessage>(3);
        }

        /// <inheritdoc />
        public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
        {
            return [(Security, CandleType)];
        }

        /// <inheritdoc />
        protected override void OnStarted(DateTimeOffset time)
        {
            base.OnStarted(time);

            // Clear candle queue
            _lastThreeCandles.Clear();

            // Create lowest indicator for downtrend identification
            _lowestIndicator = new Lowest { Length = DowntrendLength };

            // Subscribe to candles
            var subscription = SubscribeCandles(CandleType);

            // Bind candle processing with the lowest indicator
            subscription
                .Bind(_lowestIndicator, ProcessCandle)
                .Start();

            // Enable position protection
            StartProtection(
                new Unit(0, UnitTypes.Absolute), // No take profit (manual exit or on next pattern)
                new Unit(StopLossPercent, UnitTypes.Percent), // Stop loss below pattern's low
                false // No trailing
            );

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal lowestValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Already in position, no need to search for new patterns
            if (Position > 0)
            {
                UpdateCandleQueue(candle);
                return;
            }

            // Add current candle to the queue and maintain its size
            UpdateCandleQueue(candle);

            // Check if we have enough candles for pattern detection
            if (_lastThreeCandles.Count < 3)
                return;

            // Get the three candles for pattern analysis
            var candles = _lastThreeCandles.ToArray();
            var firstCandle = candles[0];
            var secondCandle = candles[1];
            var thirdCandle = candles[2]; // Current candle

            // Check for Three-Bar Reversal Up pattern:
            // 1. First candle is bearish
            var isFirstBearish = firstCandle.ClosePrice < firstCandle.OpenPrice;

            // 2. Second candle is bearish with a lower low
            var isSecondBearish = secondCandle.ClosePrice < secondCandle.OpenPrice;
            var hasSecondLowerLow = secondCandle.LowPrice < firstCandle.LowPrice;

            // 3. Third candle is bullish and closes above second candle's high
            var isThirdBullish = thirdCandle.ClosePrice > thirdCandle.OpenPrice;
            var doesThirdCloseAboveSecondHigh = thirdCandle.ClosePrice > secondCandle.HighPrice;

            // 4. Check if we're in a downtrend (if required)
            var isInDowntrend = !RequireDowntrend || IsInDowntrend(lowestValue);

            // Check if the pattern is complete
            if (isFirstBearish && isSecondBearish && hasSecondLowerLow && 
                isThirdBullish && doesThirdCloseAboveSecondHigh && isInDowntrend)
            {
                // Pattern found - take long position
                var patternLow = Math.Min(secondCandle.LowPrice, thirdCandle.LowPrice);
                var stopLoss = patternLow * (1 - StopLossPercent / 100);

                BuyMarket(Volume);
                LogInfo($"Three-Bar Reversal Up pattern detected at {thirdCandle.OpenTime}");
                LogInfo($"First bar: O={firstCandle.OpenPrice}, C={firstCandle.ClosePrice}, L={firstCandle.LowPrice}");
                LogInfo($"Second bar: O={secondCandle.OpenPrice}, C={secondCandle.ClosePrice}, L={secondCandle.LowPrice}");
                LogInfo($"Third bar: O={thirdCandle.OpenPrice}, C={thirdCandle.ClosePrice}");
                LogInfo($"Stop Loss set at {stopLoss}");
            }
        }

        private void UpdateCandleQueue(ICandleMessage candle)
        {
            _lastThreeCandles.Enqueue(candle);
            while (_lastThreeCandles.Count > 3)
                _lastThreeCandles.Dequeue();
        }

        private bool IsInDowntrend(decimal lowestValue)
        {
            // If we have the lowest indicator value, check if current price is near it
            if (_lastThreeCandles.Count > 0)
            {
                var lastCandle = _lastThreeCandles.Peek();
                return Math.Abs(lastCandle.LowPrice - lowestValue) / lowestValue < 0.03m;
            }
            
            return false;
        }
    }
}