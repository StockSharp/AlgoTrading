using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy based on Bullish Engulfing candlestick pattern.
    /// This pattern occurs when a bullish (white) candlestick completely engulfs
    /// the previous bearish (black) candlestick, signaling a potential bullish reversal.
    /// </summary>
    public class EngulfingBullishStrategy : Strategy
    {
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<decimal> _stopLossPercent;
        private readonly StrategyParam<bool> _requireDowntrend;
        private readonly StrategyParam<int> _downtrendBars;

        private ICandleMessage _previousCandle;
        private int _consecutiveDownBars;

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
        /// Whether to require a prior downtrend before the pattern.
        /// </summary>
        public bool RequireDowntrend
        {
            get => _requireDowntrend.Value;
            set => _requireDowntrend.Value = value;
        }

        /// <summary>
        /// Number of consecutive bearish bars to define downtrend.
        /// </summary>
        public int DowntrendBars
        {
            get => _downtrendBars.Value;
            set => _downtrendBars.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EngulfingBullishStrategy"/>.
        /// </summary>
        public EngulfingBullishStrategy()
        {
            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
                .SetDisplay("Candle Type", "Type of candles to use", "General");

            _stopLossPercent = Param(nameof(StopLossPercent), 1.0m)
                .SetRange(0.5m, 3.0m)
                .SetDisplay("Stop Loss %", "Percentage below pattern's low for stop-loss", "Risk Management")
                .SetCanOptimize(true);

            _requireDowntrend = Param(nameof(RequireDowntrend), true)
                .SetDisplay("Require Downtrend", "Whether to require a prior downtrend", "Pattern Parameters");

            _downtrendBars = Param(nameof(DowntrendBars), 3)
                .SetRange(2, 5)
                .SetDisplay("Downtrend Bars", "Number of consecutive bearish bars for downtrend", "Pattern Parameters")
                .SetCanOptimize(true);
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

            _previousCandle = null;
            _consecutiveDownBars = 0;

            // Subscribe to candles
            var subscription = SubscribeCandles(CandleType);

            // Bind candle processing
            subscription
                .Bind(ProcessCandle)
                .Start();

            // Enable position protection
            StartProtection(
                new Unit(0, UnitTypes.Absolute), // No take profit (manual exit)
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

        private void ProcessCandle(ICandleMessage candle)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Already in position, no need to search for new patterns
            if (Position > 0)
            {
                // Store current candle for next iteration
                _previousCandle = candle;
                return;
            }

            // Track consecutive down bars for downtrend identification
            if (candle.ClosePrice < candle.OpenPrice)
            {
                _consecutiveDownBars++;
            }
            else
            {
                _consecutiveDownBars = 0;
            }

            // If we have a previous candle, check for engulfing pattern
            if (_previousCandle != null)
            {
                // Check for bullish engulfing pattern:
                // 1. Previous candle is bearish (close < open)
                // 2. Current candle is bullish (close > open)
                // 3. Current candle's body completely engulfs previous candle's body
                
                var isPreviousBearish = _previousCandle.ClosePrice < _previousCandle.OpenPrice;
                var isCurrentBullish = candle.ClosePrice > candle.OpenPrice;
                
                var isPreviousEngulfed = candle.ClosePrice > _previousCandle.OpenPrice && 
                                         candle.OpenPrice < _previousCandle.ClosePrice;
                
                var isDowntrendPresent = !RequireDowntrend || _consecutiveDownBars >= DowntrendBars;
                
                if (isPreviousBearish && isCurrentBullish && isPreviousEngulfed && isDowntrendPresent)
                {
                    // Bullish engulfing pattern detected
                    var patternLow = Math.Min(candle.LowPrice, _previousCandle.LowPrice);
                    
                    // Buy signal
                    BuyMarket(Volume);
                    LogInfo($"Bullish Engulfing pattern detected at {candle.OpenTime}: Open={candle.OpenPrice}, Close={candle.ClosePrice}");
                    LogInfo($"Previous candle: Open={_previousCandle.OpenPrice}, Close={_previousCandle.ClosePrice}");
                    LogInfo($"Stop Loss set at {patternLow * (1 - StopLossPercent / 100)}");
                    
                    // Reset consecutive down bars
                    _consecutiveDownBars = 0;
                }
            }

            // Store current candle for next iteration
            _previousCandle = candle;
        }
    }
}