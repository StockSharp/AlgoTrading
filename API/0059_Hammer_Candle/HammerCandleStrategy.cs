using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy based on Hammer candlestick pattern.
    /// Hammer is a bullish reversal pattern that forms after a decline
    /// and is characterized by a small body with a long lower shadow.
    /// </summary>
    public class HammerCandleStrategy : Strategy
    {
        private readonly StrategyParam<decimal> _shadowToBodyRatio;
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<decimal> _stopLossPercent;
        private readonly StrategyParam<bool> _confirmationRequired;
        
        private decimal? _hammerLow;
        private decimal? _hammerHigh;
        private bool _hammerDetected;

        /// <summary>
        /// Minimum ratio between lower shadow and body to qualify as a hammer.
        /// </summary>
        public decimal ShadowToBodyRatio
        {
            get => _shadowToBodyRatio.Value;
            set => _shadowToBodyRatio.Value = value;
        }

        /// <summary>
        /// Type of candles to use.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Stop-loss percentage below the hammer's low.
        /// </summary>
        public decimal StopLossPercent
        {
            get => _stopLossPercent.Value;
            set => _stopLossPercent.Value = value;
        }

        /// <summary>
        /// Whether to require a confirmation candle after the hammer.
        /// </summary>
        public bool ConfirmationRequired
        {
            get => _confirmationRequired.Value;
            set => _confirmationRequired.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HammerCandleStrategy"/>.
        /// </summary>
        public HammerCandleStrategy()
        {
            _shadowToBodyRatio = Param(nameof(ShadowToBodyRatio), 2.0m)
                .SetRange(1.5m, 5.0m)
                .SetDisplay("Shadow/Body Ratio", "Minimum ratio of lower shadow to body length", "Pattern Parameters")
                .SetCanOptimize(true);

            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(15)))
                .SetDisplay("Candle Type", "Type of candles to use", "General");

            _stopLossPercent = Param(nameof(StopLossPercent), 1.0m)
                .SetRange(0.5m, 3.0m)
                .SetDisplay("Stop Loss %", "Percentage below hammer's low for stop-loss", "Risk Management")
                .SetCanOptimize(true);
                
            _confirmationRequired = Param(nameof(ConfirmationRequired), true)
                .SetDisplay("Confirmation Required", "Whether to wait for a bullish confirmation candle", "Pattern Parameters");
        }

        /// <inheritdoc />
        public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
        {
            return new[] { (Security, CandleType) };
        }

        /// <inheritdoc />
        protected override void OnStarted(DateTimeOffset time)
        {
            base.OnStarted(time);

            _hammerLow = null;
            _hammerHigh = null;
            _hammerDetected = false;

            // Create lowest indicator for trend identification
            var lowest = new Lowest { Length = 10 };

            // Subscribe to candles
            var subscription = SubscribeCandles(CandleType);

            // Bind candle processing with the lowest indicator
            subscription
                .Bind(lowest, ProcessCandle)
                .Start();

            // Enable position protection
            StartProtection(
                new Unit(0, UnitTypes.Absolute), // No take profit (manual exit)
                new Unit(StopLossPercent, UnitTypes.Percent), // Stop loss below hammer's low
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
                return;
            
            // If we have detected a hammer and are waiting for confirmation
            if (_hammerDetected)
            {
                // If confirmation required and we get a bullish candle
                if (ConfirmationRequired && candle.ClosePrice > candle.OpenPrice)
                {
                    // Buy signal - Hammer with confirmation candle
                    BuyMarket(Volume);
                    LogInfo($"Hammer pattern confirmed: Buy at {candle.ClosePrice}, Stop Loss at {_hammerLow * (1 - StopLossPercent / 100)}");
                    
                    // Reset pattern detection
                    _hammerDetected = false;
                    _hammerLow = null;
                    _hammerHigh = null;
                }
                // If no confirmation required or we don't want to wait anymore
                else if (!ConfirmationRequired)
                {
                    // Buy signal - Hammer without waiting for confirmation
                    BuyMarket(Volume);
                    LogInfo($"Hammer pattern detected: Buy at {candle.ClosePrice}, Stop Loss at {_hammerLow * (1 - StopLossPercent / 100)}");
                    
                    // Reset pattern detection
                    _hammerDetected = false;
                    _hammerLow = null;
                    _hammerHigh = null;
                }
                // If we've seen a hammer but today's candle doesn't confirm, reset
                else if (candle.ClosePrice < candle.OpenPrice)
                {
                    _hammerDetected = false;
                    _hammerLow = null;
                    _hammerHigh = null;
                }
            }
            
            // Pattern detection logic
            else
            {
                // Identify hammer pattern
                // 1. Candle should appear after a decline (price near recent lows)
                // 2. Lower shadow should be at least X times longer than the body
                // 3. Candle should have small or no upper shadow
                
                // Check if we're near recent lows
                var isNearLows = Math.Abs(candle.LowPrice - lowestValue) / lowestValue < 0.03m;
                
                // Check if low is below previous low (market is declining)
                var isDecline = candle.LowPrice < lowestValue;
                
                // Calculate candle body and shadows
                var bodyLength = Math.Abs(candle.ClosePrice - candle.OpenPrice);
                var lowerShadow = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;
                var upperShadow = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);
                
                // Check for bullish hammer pattern
                var isBullish = candle.ClosePrice > candle.OpenPrice;
                var hasLongLowerShadow = lowerShadow > bodyLength * ShadowToBodyRatio;
                var hasSmallUpperShadow = upperShadow < bodyLength * 0.3m;
                
                // Identify hammer
                if ((isNearLows || isDecline) && hasLongLowerShadow && hasSmallUpperShadow)
                {
                    _hammerLow = candle.LowPrice;
                    _hammerHigh = candle.HighPrice;
                    _hammerDetected = true;
                    
                    LogInfo($"Potential hammer detected at {candle.OpenTime}: low={candle.LowPrice}, body ratio={lowerShadow/bodyLength:F2}");
                    
                    // If confirmation not required, buy immediately
                    if (!ConfirmationRequired)
                    {
                        BuyMarket(Volume);
                        LogInfo($"Hammer pattern detected: Buy at {candle.ClosePrice}, Stop Loss at {_hammerLow * (1 - StopLossPercent / 100)}");
                        
                        // Reset pattern detection
                        _hammerDetected = false;
                        _hammerLow = null;
                        _hammerHigh = null;
                    }
                }
            }