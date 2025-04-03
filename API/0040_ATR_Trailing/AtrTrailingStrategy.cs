using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy that uses ATR (Average True Range) for trailing stop management.
    /// It enters positions using a simple moving average and manages exits with a dynamic
    /// trailing stop calculated as a multiple of ATR.
    /// </summary>
    public class AtrTrailingStrategy : Strategy
    {
        private readonly StrategyParam<int> _atrPeriod;
        private readonly StrategyParam<decimal> _atrMultiplier;
        private readonly StrategyParam<int> _maPeriod;
        private readonly StrategyParam<DataType> _candleType;

        private decimal _entryPrice;
        private decimal _trailingStopLevel;

        /// <summary>
        /// Period for ATR calculation (default: 14)
        /// </summary>
        public int AtrPeriod
        {
            get => _atrPeriod.Value;
            set => _atrPeriod.Value = value;
        }

        /// <summary>
        /// ATR multiplier for trailing stop calculation (default: 3.0)
        /// </summary>
        public decimal AtrMultiplier
        {
            get => _atrMultiplier.Value;
            set => _atrMultiplier.Value = value;
        }

        /// <summary>
        /// Period for Moving Average calculation for entry (default: 20)
        /// </summary>
        public int MAPeriod
        {
            get => _maPeriod.Value;
            set => _maPeriod.Value = value;
        }

        /// <summary>
        /// Type of candles used for strategy calculation
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Initialize the ATR Trailing strategy
        /// </summary>
        public AtrTrailingStrategy()
        {
            _atrPeriod = Param(nameof(AtrPeriod), 14)
                .SetDisplayName("ATR Period")
                .SetDescription("Period for ATR calculation")
                .SetGroup("Technical Parameters")
                .SetCanOptimize(true)
                .SetOptimize(7, 21, 7);

            _atrMultiplier = Param(nameof(AtrMultiplier), 3.0m)
                .SetDisplayName("ATR Multiplier")
                .SetDescription("ATR multiplier for trailing stop calculation")
                .SetGroup("Risk Management")
                .SetCanOptimize(true)
                .SetOptimize(2.0m, 4.0m, 0.5m);

            _maPeriod = Param(nameof(MAPeriod), 20)
                .SetDisplayName("MA Period")
                .SetDescription("Period for Moving Average calculation for entry")
                .SetGroup("Entry Parameters")
                .SetCanOptimize(true)
                .SetOptimize(10, 50, 5);

            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
                .SetDisplayName("Candle Type")
                .SetDescription("Type of candles to use")
                .SetGroup("Data");
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

            // Reset state variables
            _entryPrice = 0;
            _trailingStopLevel = 0;

            // Create indicators
            var atr = new AverageTrueRange { Length = AtrPeriod };
            var sma = new SimpleMovingAverage { Length = MAPeriod };

            // Create subscription and bind indicators
            var subscription = SubscribeCandles(CandleType);
            subscription
                .Bind(atr, sma, ProcessCandle)
                .Start();

            // Configure chart
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, sma);
                DrawIndicator(area, atr);
                DrawOwnTrades(area);
            }
        }

        /// <summary>
        /// Process candle and manage positions with ATR-based trailing stops
        /// </summary>
        private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal smaValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Calculate trailing stop distance based on ATR
            decimal trailingStopDistance = atrValue * AtrMultiplier;

            if (Position == 0)
            {
                // No position - check for entry signals
                if (candle.ClosePrice > smaValue)
                {
                    // Price above MA - buy (long)
                    BuyMarket(Volume);
                    
                    // Record entry price
                    _entryPrice = candle.ClosePrice;
                    
                    // Set initial trailing stop
                    _trailingStopLevel = _entryPrice - trailingStopDistance;
                }
                else if (candle.ClosePrice < smaValue)
                {
                    // Price below MA - sell (short)
                    SellMarket(Volume);
                    
                    // Record entry price
                    _entryPrice = candle.ClosePrice;
                    
                    // Set initial trailing stop
                    _trailingStopLevel = _entryPrice + trailingStopDistance;
                }
            }
            else if (Position > 0)
            {
                // Long position - update and check trailing stop
                
                // Calculate potential new trailing stop level
                decimal newTrailingStopLevel = candle.ClosePrice - trailingStopDistance;
                
                // Only move the trailing stop up, never down (for long positions)
                if (newTrailingStopLevel > _trailingStopLevel)
                {
                    _trailingStopLevel = newTrailingStopLevel;
                }
                
                // Check if price hit the trailing stop
                if (candle.LowPrice <= _trailingStopLevel)
                {
                    // Trailing stop hit - exit long
                    SellMarket(Position);
                }
            }
            else if (Position < 0)
            {
                // Short position - update and check trailing stop
                
                // Calculate potential new trailing stop level
                decimal newTrailingStopLevel = candle.ClosePrice + trailingStopDistance;
                
                // Only move the trailing stop down, never up (for short positions)
                if (newTrailingStopLevel < _trailingStopLevel || _trailingStopLevel == 0)
                {
                    _trailingStopLevel = newTrailingStopLevel;
                }
                
                // Check if price hit the trailing stop
                if (candle.HighPrice >= _trailingStopLevel)
                {
                    // Trailing stop hit - exit short
                    BuyMarket(Math.Abs(Position));
                }
            }
        }
    }
}
