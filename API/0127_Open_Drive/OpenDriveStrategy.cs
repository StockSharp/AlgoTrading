using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Algo.Indicators;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Implementation of Open Drive trading strategy.
    /// The strategy trades on strong gap openings relative to previous close.
    /// </summary>
    public class OpenDriveStrategy : Strategy
    {
        private readonly StrategyParam<decimal> _atrMultiplier;
        private readonly StrategyParam<int> _atrPeriod;
        private readonly StrategyParam<int> _maPeriod;
        private readonly StrategyParam<DataType> _candleType;
        
        private decimal _prevClosePrice;
        private AverageTrueRange _atr;

        /// <summary>
        /// ATR multiplier for gap size.
        /// </summary>
        public decimal AtrMultiplier
        {
            get => _atrMultiplier.Value;
            set => _atrMultiplier.Value = value;
        }

        /// <summary>
        /// ATR period.
        /// </summary>
        public int AtrPeriod
        {
            get => _atrPeriod.Value;
            set => _atrPeriod.Value = value;
        }

        /// <summary>
        /// Moving average period.
        /// </summary>
        public int MaPeriod
        {
            get => _maPeriod.Value;
            set => _maPeriod.Value = value;
        }

        /// <summary>
        /// Candle type for strategy.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenDriveStrategy"/>.
        /// </summary>
        public OpenDriveStrategy()
        {
            _atrMultiplier = Param(nameof(AtrMultiplier), 1.0m)
                .SetGreaterThanZero()
                .SetDisplay("ATR Multiplier", "Multiplier for ATR to define gap size", "Strategy")
                .SetCanOptimize(true)
                .SetOptimize(0.5m, 2.0m, 0.5m);
            
            _atrPeriod = Param(nameof(AtrPeriod), 14)
                .SetGreaterThanZero()
                .SetDisplay("ATR Period", "Period for ATR calculation", "Strategy")
                .SetCanOptimize(true)
                .SetOptimize(7, 21, 7);
            
            _maPeriod = Param(nameof(MaPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("MA Period", "Moving average period for trend confirmation", "Strategy");
            
            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
                .SetDisplay("Candle Type", "Type of candles for strategy", "Strategy");
            
            _prevClosePrice = 0;
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
            
            // Create indicators
            var sma = new SimpleMovingAverage { Length = MaPeriod };
            _atr = new AverageTrueRange { Length = AtrPeriod };
            
            // Create subscription and bind indicators
            var subscription = SubscribeCandles(CandleType);
            
            // We need to process both indicators with the same candle
            subscription
                .Bind(sma, _atr, ProcessCandle)
                .Start();
            
            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, sma);
                DrawIndicator(area, _atr);
                DrawOwnTrades(area);
            }
            
            // Start position protection using ATR for stops
            StartProtection(
                takeProfit: new Unit(0), // No take profit
                stopLoss: new Unit(2 * AtrMultiplier, UnitTypes.Absolute), // 2 * ATR for stop loss
                isStopTrailing: true
            );
        }

        private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal atrValue)
        {
            // Skip if we don't have the previous close price yet
            if (_prevClosePrice == 0)
            {
                _prevClosePrice = candle.ClosePrice;
                return;
            }
            
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;
            
            // Skip if strategy is not ready
            if (!IsFormedAndOnlineAndAllowTrading())
                return;
            
            // Calculate gap size compared to previous close
            decimal gap = candle.OpenPrice - _prevClosePrice;
            decimal gapSize = Math.Abs(gap);
            
            // Check if we have a significant gap (> ATR * multiplier)
            if (gapSize > atrValue * AtrMultiplier)
            {
                // Upward gap (Open > Previous Close) with price above MA = Buy
                if (gap > 0 && candle.OpenPrice > smaValue && Position <= 0)
                {
                    var volume = Volume + Math.Abs(Position);
                    BuyMarket(volume);
                    
                    LogInfo($"Buy signal on upward gap: Gap={gap}, ATR={atrValue}, OpenPrice={candle.OpenPrice}, PrevClose={_prevClosePrice}, MA={smaValue}, Volume={volume}");
                }
                // Downward gap (Open < Previous Close) with price below MA = Sell
                else if (gap < 0 && candle.OpenPrice < smaValue && Position >= 0)
                {
                    var volume = Volume + Math.Abs(Position);
                    SellMarket(volume);
                    
                    LogInfo($"Sell signal on downward gap: Gap={gap}, ATR={atrValue}, OpenPrice={candle.OpenPrice}, PrevClose={_prevClosePrice}, MA={smaValue}, Volume={volume}");
                }
            }
            
            // Exit conditions
            if ((Position > 0 && candle.ClosePrice < smaValue) || 
                (Position < 0 && candle.ClosePrice > smaValue))
            {
                ClosePosition();
                LogInfo($"Closing position on MA crossover: Position={Position}, ClosePrice={candle.ClosePrice}, MA={smaValue}");
            }
            
            // Update previous close price for next candle
            _prevClosePrice = candle.ClosePrice;
        }
    }
}
