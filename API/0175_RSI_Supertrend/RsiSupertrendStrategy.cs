using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy based on RSI and Supertrend indicators.
    /// Enters long when RSI is oversold (< 30) and price is above Supertrend
    /// Enters short when RSI is overbought (> 70) and price is below Supertrend
    /// </summary>
    public class RsiSupertrendStrategy : Strategy
    {
        private readonly StrategyParam<int> _rsiPeriod;
        private readonly StrategyParam<int> _supertrendPeriod;
        private readonly StrategyParam<decimal> _supertrendMultiplier;
        private readonly StrategyParam<DataType> _candleType;
        
        // Custom Supertrend indicator
        private AverageTrueRange _atr;
        private decimal _upValue;
        private decimal _downValue;
        private decimal _currentTrend;
        private decimal _prevUpValue;
        private decimal _prevDownValue;
        private decimal _prevClose;
        private bool _isFirstValue = true;

        /// <summary>
        /// RSI period
        /// </summary>
        public int RsiPeriod
        {
            get => _rsiPeriod.Value;
            set => _rsiPeriod.Value = value;
        }

        /// <summary>
        /// Supertrend ATR period
        /// </summary>
        public int SupertrendPeriod
        {
            get => _supertrendPeriod.Value;
            set => _supertrendPeriod.Value = value;
        }

        /// <summary>
        /// Supertrend ATR multiplier
        /// </summary>
        public decimal SupertrendMultiplier
        {
            get => _supertrendMultiplier.Value;
            set => _supertrendMultiplier.Value = value;
        }

        /// <summary>
        /// Candle type for strategy calculation
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public RsiSupertrendStrategy()
        {
            _rsiPeriod = Param(nameof(RsiPeriod), 14)
                .SetGreaterThanZero()
                .SetDisplay("RSI Period", "Period for RSI indicator", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(10, 20, 2);

            _supertrendPeriod = Param(nameof(SupertrendPeriod), 10)
                .SetGreaterThanZero()
                .SetDisplay("Supertrend Period", "ATR period for Supertrend", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(7, 14, 1);

            _supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3.0m)
                .SetGreaterThanZero()
                .SetDisplay("Supertrend Multiplier", "ATR multiplier for Supertrend", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(2.0m, 4.0m, 0.5m);

            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
                .SetDisplay("Candle Type", "Timeframe for strategy", "General");
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

            // Create RSI indicator
            var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
            
            // Create ATR indicator for Supertrend calculation
            _atr = new AverageTrueRange { Length = SupertrendPeriod };
            
            // Reset state variables
            _isFirstValue = true;
            _currentTrend = 1; // Default to uptrend

            // Enable using Supertrend as a dynamic stop-loss
            // We'll implement our own stop management based on Supertrend

            // Subscribe to candles and bind indicators
            var subscription = SubscribeCandles(CandleType);
            subscription
                .Bind(rsi, _atr, ProcessCandle)
                .Start();

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                
                // Separate area for RSI
                var rsiArea = CreateChartArea();
                if (rsiArea != null)
                {
                    DrawIndicator(rsiArea, rsi);
                }
                
                // Note: We'll manually draw Supertrend lines in ProcessCandle method
                
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal atrValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;
            
            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Calculate Supertrend
            var closePrice = candle.ClosePrice;
            var highPrice = candle.HighPrice;
            var lowPrice = candle.LowPrice;
            
            // Basic bands calculation
            var basicUpperBand = (highPrice + lowPrice) / 2 + SupertrendMultiplier * atrValue;
            var basicLowerBand = (highPrice + lowPrice) / 2 - SupertrendMultiplier * atrValue;
            
            if (_isFirstValue)
            {
                // Initialize values for the first candle
                _upValue = basicUpperBand;
                _downValue = basicLowerBand;
                _prevUpValue = _upValue;
                _prevDownValue = _downValue;
                _prevClose = closePrice;
                _isFirstValue = false;
                return;
            }
            
            // Calculate final upper and lower bands
            _upValue = basicUpperBand;
            if (_upValue < _prevUpValue || _prevClose > _prevUpValue)
                _upValue = _prevUpValue;
            
            _downValue = basicLowerBand;
            if (_downValue > _prevDownValue || _prevClose < _prevDownValue)
                _downValue = _prevDownValue;
            
            // Determine trend direction
            var prevTrend = _currentTrend;
            
            if (_prevClose <= _prevUpValue)
                _currentTrend = -1; // Downtrend
            
            if (_prevClose >= _prevDownValue)
                _currentTrend = 1; // Uptrend
            
            // Store values for next iteration
            _prevUpValue = _upValue;
            _prevDownValue = _downValue;
            _prevClose = closePrice;
            
            // Get Supertrend value based on current trend
            var supertrendValue = _currentTrend == 1 ? _downValue : _upValue;
            
            // Trading logic
            var isTrendChange = prevTrend != _currentTrend;
            
            // Long condition: RSI oversold and price above Supertrend
            if (rsiValue < 30 && _currentTrend == 1 && Position <= 0)
            {
                BuyMarket(Volume + Math.Abs(Position));
                
                // Note: We're using Supertrend as our stop-loss level,
                // so we don't need to set a separate stop-loss order
            }
            // Short condition: RSI overbought and price below Supertrend
            else if (rsiValue > 70 && _currentTrend == -1 && Position >= 0)
            {
                SellMarket(Volume + Math.Abs(Position));
            }
            // Exit conditions - based on Supertrend direction change
            else if (isTrendChange)
            {
                if (_currentTrend == -1 && Position > 0)
                {
                    // Trend changed to down - exit long
                    SellMarket(Position);
                }
                else if (_currentTrend == 1 && Position < 0)
                {
                    // Trend changed to up - exit short
                    BuyMarket(Math.Abs(Position));
                }
            }
        }
    }
}