namespace StockSharp.Strategies.Samples
{
    using System;
    using System.Collections.Generic;
    
    using Ecng.Common;
    
    using StockSharp.Algo;
    using StockSharp.Algo.Candles;
    using StockSharp.Algo.Strategies;
    using StockSharp.BusinessEntities;
    using StockSharp.Messages;
    
    /// <summary>
    /// Strategy that trades on the price movement fade during the lunch break.
    /// </summary>
    public class LunchBreakFadeStrategy : Strategy
    {
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<int> _lunchHour;
        private readonly StrategyParam<decimal> _stopLossPercent;
        
        // Store previous candles data for trend detection
        private decimal? _previousCandleClose;
        private decimal? _twoCandlesBackClose;
        
        /// <summary>
        /// Data type for candles.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }
        
        /// <summary>
        /// Hour when lunch break typically starts (default is 13:00).
        /// </summary>
        public int LunchHour
        {
            get => _lunchHour.Value;
            set => _lunchHour.Value = value;
        }
        
        /// <summary>
        /// Stop loss percentage from entry price.
        /// </summary>
        public decimal StopLossPercent
        {
            get => _stopLossPercent.Value;
            set => _stopLossPercent.Value = value;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="LunchBreakFadeStrategy"/>.
        /// </summary>
        public LunchBreakFadeStrategy()
        {
            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
                          .SetDisplay("Candle Type", "Type of candles to use", "General");
                          
            _lunchHour = Param(nameof(LunchHour), 13)
                         .SetRange(8, 16)
                         .SetDisplay("Lunch Hour", "Hour when lunch break typically starts (24-hour format)", "General");
                         
            _stopLossPercent = Param(nameof(StopLossPercent), 2m)
                               .SetRange(0.1m, 10m)
                               .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management");
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
            
            _previousCandleClose = null;
            _twoCandlesBackClose = null;
            
            // Set up stop loss protection
            StartProtection(
                new Unit(0), // No take profit
                new Unit(StopLossPercent, UnitTypes.Percent) // Stop loss based on parameter
            );
            
            // Create candle subscription for the specified timeframe
            var subscription = SubscribeCandles(CandleType);
            
            // Bind the candle processor
            subscription
                .Bind(ProcessCandle)
                .Start();
                
            // Set up chart if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawOwnTrades(area);
            }
        }
        
        /// <summary>
        /// Process incoming candle.
        /// </summary>
        /// <param name="candle">Candle to process.</param>
        private void ProcessCandle(ICandleMessage candle)
        {
            if (candle.State != CandleStates.Finished)
                return;
                
            if (!IsFormedAndOnlineAndAllowTrading())
                return;
                
            // Check if current hour is lunch hour
            if (candle.OpenTime.Hour != LunchHour)
            {
                // Update previous candles data
                _twoCandlesBackClose = _previousCandleClose;
                _previousCandleClose = candle.ClosePrice;
                return;
            }
                
            // Trading logic can only be applied if we have enough historical data
            if (_previousCandleClose == null || _twoCandlesBackClose == null)
            {
                _twoCandlesBackClose = _previousCandleClose;
                _previousCandleClose = candle.ClosePrice;
                return;
            }
            
            // Lunch break fade logic:
            
            // Check for price movement before lunch break
            bool priorUptrend = _previousCandleClose > _twoCandlesBackClose;
            
            // Check current candle direction
            bool currentBullish = candle.ClosePrice > candle.OpenPrice;
            
            // Look for fade pattern
            if (priorUptrend && !currentBullish)
            {
                // Uptrend before lunch, bearish candle at lunch - Sell signal
                if (Position >= 0)
                {
                    // Close any long positions and go short
                    SellMarket(Volume + Math.Abs(Position));
                    LogInfo("Lunch break fade signal: Selling after uptrend");
                }
            }
            else if (!priorUptrend && currentBullish)
            {
                // Downtrend before lunch, bullish candle at lunch - Buy signal
                if (Position <= 0)
                {
                    // Close any short positions and go long
                    BuyMarket(Volume + Math.Abs(Position));
                    LogInfo("Lunch break fade signal: Buying after downtrend");
                }
            }
            
            // Update previous candles data
            _twoCandlesBackClose = _previousCandleClose;
            _previousCandleClose = candle.ClosePrice;
        }
        
        /// <inheritdoc />
        protected override void OnStopped()
        {
            _previousCandleClose = null;
            _twoCandlesBackClose = null;
            
            base.OnStopped();
        }
    }
}