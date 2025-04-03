using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy #76: Fibonacci Retracement Reversal strategy.
    /// The strategy identifies significant swings in price and looks for reversals at key Fibonacci retracement levels.
    /// </summary>
    public class FibonacciRetracementReversalStrategy : Strategy
    {
        // Fibonacci retracement levels
        private readonly decimal[] _fibLevels = { 0.0m, 0.236m, 0.382m, 0.5m, 0.618m, 0.786m, 1.0m };
        
        private readonly StrategyParam<int> _swingLookbackPeriod;
        private readonly StrategyParam<decimal> _fibLevelBuffer;
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<decimal> _stopLossPercent;
        
        // Variables to store swing high and low
        private decimal _swingHigh = decimal.MinValue;
        private decimal _swingLow = decimal.MaxValue;
        private bool _trendIsUp = false;
        
        // Store recent candles for swing detection
        private readonly Queue<ICandleMessage> _recentCandles = new Queue<ICandleMessage>();

        /// <summary>
        /// Lookback period for swing detection.
        /// </summary>
        public int SwingLookbackPeriod
        {
            get => _swingLookbackPeriod.Value;
            set => _swingLookbackPeriod.Value = value;
        }

        /// <summary>
        /// Buffer percentage around Fibonacci levels.
        /// </summary>
        public decimal FibLevelBuffer
        {
            get => _fibLevelBuffer.Value;
            set => _fibLevelBuffer.Value = value;
        }

        /// <summary>
        /// Candle type.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Stop-loss percentage.
        /// </summary>
        public decimal StopLossPercent
        {
            get => _stopLossPercent.Value;
            set => _stopLossPercent.Value = value;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public FibonacciRetracementReversalStrategy()
        {
            _swingLookbackPeriod = Param(nameof(SwingLookbackPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("Swing Lookback Period", "Number of candles to look back for swing detection", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(10, 30, 5);

            _fibLevelBuffer = Param(nameof(FibLevelBuffer), 0.5m)
                .SetNotNegative()
                .SetDisplay("Fib Level Buffer %", "Buffer percentage around Fibonacci levels", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(0.2m, 1.0m, 0.2m);

            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
                .SetDisplay("Candle Type", "Type of candles to use", "General");

            _stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
                .SetNotNegative()
                .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
                .SetCanOptimize(true)
                .SetOptimize(1.0m, 3.0m, 0.5m);
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

            // Reset values
            _swingHigh = decimal.MinValue;
            _swingLow = decimal.MaxValue;
            _trendIsUp = false;
            _recentCandles.Clear();

            // Create subscription
            var subscription = SubscribeCandles(CandleType);
            
            subscription
                .Bind(ProcessCandle)
                .Start();

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawOwnTrades(area);
            }

            // Start position protection
            StartProtection(
                takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit, rely on the strategy's exit logic
                stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
            );
        }

        private void ProcessCandle(ICandleMessage candle)
        {
            if (candle.State != CandleStates.Finished)
                return;

            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Add current candle to the queue and maintain queue size
            _recentCandles.Enqueue(candle);
            while (_recentCandles.Count > SwingLookbackPeriod && _recentCandles.Count > 0)
                _recentCandles.Dequeue();

            // We need a sufficient number of candles to identify swings
            if (_recentCandles.Count < 3)
                return;

            // Update swing points if necessary
            UpdateSwingPoints();

            // Check for potential entry signals
            CheckForEntrySignals(candle);
        }

        private void UpdateSwingPoints()
        {
            // Get high and low from recent candles
            if (_recentCandles.Count < 3)
                return;

            var candles = _recentCandles.ToArray();
            var middleIndex = candles.Length / 2;

            // Check if we have a new swing high or low
            bool swingHighFound = false;
            bool swingLowFound = false;

            // Check for swing high - middle candle has the highest high
            if (candles.Length >= 3)
            {
                decimal middleHigh = 0;
                decimal middleLow = decimal.MaxValue;

                // Find the highest high and lowest low in the middle third of candles
                for (int i = Math.Max(0, middleIndex - 1); i <= Math.Min(candles.Length - 1, middleIndex + 1); i++)
                {
                    if (candles[i].HighPrice > middleHigh)
                        middleHigh = candles[i].HighPrice;
                    
                    if (candles[i].LowPrice < middleLow)
                        middleLow = candles[i].LowPrice;
                }

                // Check if this middle section forms a swing high/low
                bool isHigher = true;
                bool isLower = true;

                // Check candles before middle
                for (int i = 0; i < Math.Max(0, middleIndex - 1); i++)
                {
                    if (candles[i].HighPrice >= middleHigh)
                        isHigher = false;
                    
                    if (candles[i].LowPrice <= middleLow)
                        isLower = false;
                }

                // Check candles after middle
                for (int i = Math.Min(candles.Length - 1, middleIndex + 2); i < candles.Length; i++)
                {
                    if (candles[i].HighPrice >= middleHigh)
                        isHigher = false;
                    
                    if (candles[i].LowPrice <= middleLow)
                        isLower = false;
                }

                // If we found a swing high or low
                if (isHigher && middleHigh > _swingHigh)
                {
                    _swingHigh = middleHigh;
                    swingHighFound = true;
                    _trendIsUp = false; // After a swing high, the trend is down
                    this.AddInfoLog($"New swing high found: {_swingHigh}");
                }

                if (isLower && middleLow < _swingLow)
                {
                    _swingLow = middleLow;
                    swingLowFound = true;
                    _trendIsUp = true; // After a swing low, the trend is up
                    this.AddInfoLog($"New swing low found: {_swingLow}");
                }
            }

            // If we found both a new swing high and low, use the most recent one
            if (swingHighFound && swingLowFound)
            {
                var lastCandle = candles.Last();
                _trendIsUp = lastCandle.ClosePrice > ((_swingHigh + _swingLow) / 2);
            }
        }

        private void CheckForEntrySignals(ICandleMessage candle)
        {
            // Need valid swing points to calculate Fibonacci levels
            if (_swingHigh <= _swingLow || _swingHigh == decimal.MinValue || _swingLow == decimal.MaxValue)
                return;

            var currentPrice = candle.ClosePrice;
            var isBullish = candle.ClosePrice > candle.OpenPrice;
            var isBearish = candle.ClosePrice < candle.OpenPrice;

            // Calculate Fibonacci retracement levels
            decimal range = _swingHigh - _swingLow;
            
            // Check if price is near a Fibonacci retracement level
            foreach (var fibLevel in _fibLevels)
            {
                // Calculate price at this Fibonacci level
                decimal levelPrice;
                
                if (_trendIsUp)
                {
                    // For uptrend, calculate retracement levels from swing low
                    levelPrice = _swingLow + (range * fibLevel);
                }
                else
                {
                    // For downtrend, calculate retracement levels from swing high
                    levelPrice = _swingHigh - (range * fibLevel);
                }

                // Calculate buffer around Fibonacci level
                decimal buffer = range * (FibLevelBuffer / 100);
                
                // Check if price is within buffer of the Fibonacci level
                if (Math.Abs(currentPrice - levelPrice) <= buffer)
                {
                    // We're at a Fibonacci level - check if we should enter a position
                    this.AddInfoLog($"Price {currentPrice} is near Fibonacci {fibLevel*100}% level {levelPrice} (buffer: {buffer})");

                    // Look for long signal at 61.8% or 78.6% retracement in uptrend with bullish candle
                    if (_trendIsUp && (Math.Abs(fibLevel - 0.618m) < 0.001m || Math.Abs(fibLevel - 0.786m) < 0.001m) && 
                        isBullish && Position <= 0)
                    {
                        // Enter long position
                        CancelActiveOrders();
                        BuyMarket(Volume + Math.Abs(Position));
                        this.AddInfoLog($"Long entry at {currentPrice} near {fibLevel*100}% retracement level");
                        break;
                    }
                    // Look for short signal at 61.8% or 78.6% retracement in downtrend with bearish candle
                    else if (!_trendIsUp && (Math.Abs(fibLevel - 0.618m) < 0.001m || Math.Abs(fibLevel - 0.786m) < 0.001m) && 
                            isBearish && Position >= 0)
                    {
                        // Enter short position
                        CancelActiveOrders();
                        SellMarket(Volume + Math.Abs(Position));
                        this.AddInfoLog($"Short entry at {currentPrice} near {fibLevel*100}% retracement level");
                        break;
                    }
                }
            }

            // Exit logic - exit when price reaches the central Fibonacci level (50%)
            decimal centralLevel = _trendIsUp ? 
                _swingLow + (range * 0.5m) : 
                _swingHigh - (range * 0.5m);
            
            if (Position > 0 && currentPrice >= centralLevel)
            {
                SellMarket(Math.Abs(Position));
                this.AddInfoLog($"Long exit at {currentPrice}, reached 50% level {centralLevel}");
            }
            else if (Position < 0 && currentPrice <= centralLevel)
            {
                BuyMarket(Math.Abs(Position));
                this.AddInfoLog($"Short exit at {currentPrice}, reached 50% level {centralLevel}");
            }
        }
    }
}