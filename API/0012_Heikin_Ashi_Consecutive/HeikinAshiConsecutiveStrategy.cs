using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy based on consecutive Heikin Ashi candles.
    /// It enters long position after a sequence of bullish Heikin Ashi candles and 
    /// short position after a sequence of bearish Heikin Ashi candles.
    /// </summary>
    public class HeikinAshiConsecutiveStrategy : Strategy
    {
        private readonly StrategyParam<int> _consecutiveCandles;
        private readonly StrategyParam<decimal> _stopLossPercent;
        private readonly StrategyParam<DataType> _candleType;

        // State tracking
        private int _bullishCount;
        private int _bearishCount;
        private decimal _prevHaOpen;
        private decimal _prevHaClose;
        private decimal _prevHaHigh;
        private decimal _prevHaLow;

        /// <summary>
        /// Number of consecutive candles required for signal.
        /// </summary>
        public int ConsecutiveCandles
        {
            get => _consecutiveCandles.Value;
            set => _consecutiveCandles.Value = value;
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
        /// Candle type.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Initialize the Heikin Ashi Consecutive strategy.
        /// </summary>
        public HeikinAshiConsecutiveStrategy()
        {
            _consecutiveCandles = Param(nameof(ConsecutiveCandles), 3)
                .SetDisplay("Consecutive Candles", "Number of consecutive candles required for signal", "Trading parameters")
                .SetCanOptimize(true)
                .SetOptimize(2, 5, 1);

            _stopLossPercent = Param(nameof(StopLossPercent), 2m)
                .SetDisplay("Stop Loss (%)", "Stop loss as a percentage of entry price", "Risk parameters")
                .SetCanOptimize(true)
                .SetOptimize(1, 3, 0.5m);

            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
                .SetDisplay("Candle Type", "Type of candles to use", "General");
            
            _bullishCount = 0;
            _bearishCount = 0;
            _prevHaOpen = 0;
            _prevHaClose = 0;
            _prevHaHigh = 0;
            _prevHaLow = 0;
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

            // Create subscription
            var subscription = SubscribeCandles(CandleType);
            
            // We need to calculate Heikin-Ashi candles in the ProcessCandle handler
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

            // Start protection with stop loss
            StartProtection(
                takeProfit: null,
                stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
            );
        }

        private void ProcessCandle(ICandleMessage candle)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Calculate Heikin-Ashi values
            decimal haOpen, haClose, haHigh, haLow;

            if (_prevHaOpen == 0)
            {
                // First candle - initialize Heikin-Ashi values
                haOpen = (candle.OpenPrice + candle.ClosePrice) / 2;
                haClose = (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4;
                haHigh = candle.HighPrice;
                haLow = candle.LowPrice;
            }
            else
            {
                // Calculate Heikin-Ashi values based on previous HA candle
                haOpen = (_prevHaOpen + _prevHaClose) / 2;
                haClose = (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4;
                haHigh = Math.Max(Math.Max(candle.HighPrice, haOpen), haClose);
                haLow = Math.Min(Math.Min(candle.LowPrice, haOpen), haClose);
            }

            // Determine if Heikin-Ashi candle is bullish or bearish
            var isBullish = haClose > haOpen;
            var isBearish = haClose < haOpen;

            // Update consecutive counts
            if (isBullish)
            {
                _bullishCount++;
                _bearishCount = 0;
            }
            else if (isBearish)
            {
                _bearishCount++;
                _bullishCount = 0;
            }
            else
            {
                // Neutral candle (rare case) - reset both counts
                _bullishCount = 0;
                _bearishCount = 0;
            }

            // Trading logic
            if (_bullishCount >= ConsecutiveCandles && Position <= 0)
            {
                // Enough consecutive bullish candles - Buy signal
                var volume = Volume + Math.Abs(Position);
                BuyMarket(volume);
                LogInfo($"Buy signal: {_bullishCount} consecutive bullish Heikin-Ashi candles");
            }
            else if (_bearishCount >= ConsecutiveCandles && Position >= 0)
            {
                // Enough consecutive bearish candles - Sell signal
                var volume = Volume + Math.Abs(Position);
                SellMarket(volume);
                LogInfo($"Sell signal: {_bearishCount} consecutive bearish Heikin-Ashi candles");
            }
            // Exit logic
            else if (Position > 0 && isBearish)
            {
                // Exit long position on first bearish candle
                SellMarket(Position);
                LogInfo($"Exit long: Bearish Heikin-Ashi candle appeared");
            }
            else if (Position < 0 && isBullish)
            {
                // Exit short position on first bullish candle
                BuyMarket(Math.Abs(Position));
                LogInfo($"Exit short: Bullish Heikin-Ashi candle appeared");
            }

            // Store current Heikin-Ashi values for next candle
            _prevHaOpen = haOpen;
            _prevHaClose = haClose;
            _prevHaHigh = haHigh;
            _prevHaLow = haLow;
        }
    }
}