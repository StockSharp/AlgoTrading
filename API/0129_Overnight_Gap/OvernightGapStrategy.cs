using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Implementation of Overnight Gap trading strategy.
    /// The strategy trades on gaps between the current open and previous close prices.
    /// </summary>
    public class OvernightGapStrategy : Strategy
    {
        private readonly StrategyParam<decimal> _stopLossPercent;
        private readonly StrategyParam<int> _maPeriod;
        private readonly StrategyParam<DataType> _candleType;
        
        private decimal _prevClosePrice;

        /// <summary>
        /// Stop loss percentage from entry price.
        /// </summary>
        public decimal StopLossPercent
        {
            get => _stopLossPercent.Value;
            set => _stopLossPercent.Value = value;
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
        /// Initializes a new instance of the <see cref="OvernightGapStrategy"/>.
        /// </summary>
        public OvernightGapStrategy()
        {
            _stopLossPercent = Param(nameof(StopLossPercent), 2m)
                .SetNotNegative()
                .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Protection");
            
            _maPeriod = Param(nameof(MaPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("MA Period", "Moving average period for trend confirmation", "Strategy");
            
            _candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
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
            var sma = new StockSharp.Algo.Indicators.SimpleMovingAverage { Length = MaPeriod };
            
            // Create subscription and bind indicators
            var subscription = SubscribeCandles(CandleType);
            subscription
                .Bind(sma, ProcessCandle)
                .Start();
            
            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, sma);
                DrawOwnTrades(area);
            }
            
            // Start position protection
            StartProtection(
                takeProfit: new Unit(0), // No take profit
                stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
            );
        }

        private void ProcessCandle(ICandleMessage candle, decimal maValue)
        {
            // Skip if we don't have previous close price yet
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
            
            // Calculate gap
            decimal gap = candle.OpenPrice - _prevClosePrice;
            bool isGapUp = gap > 0;
            bool isGapDown = gap < 0;
            
            // Upward gap with price above MA = Buy
            if (isGapUp && candle.OpenPrice > maValue && Position <= 0)
            {
                var volume = Volume + Math.Abs(Position);
                BuyMarket(volume);
                
                LogInfo($"Buy signal on upward gap: Gap={gap}, OpenPrice={candle.OpenPrice}, PrevClose={_prevClosePrice}, MA={maValue}, Volume={volume}");
            }
            // Downward gap with price below MA = Sell
            else if (isGapDown && candle.OpenPrice < maValue && Position >= 0)
            {
                var volume = Volume + Math.Abs(Position);
                SellMarket(volume);
                
                LogInfo($"Sell signal on downward gap: Gap={gap}, OpenPrice={candle.OpenPrice}, PrevClose={_prevClosePrice}, MA={maValue}, Volume={volume}");
            }
            
            // Exit condition - Gap fill (price returns to previous close)
            if ((Position > 0 && candle.LowPrice <= _prevClosePrice) || 
                (Position < 0 && candle.HighPrice >= _prevClosePrice))
            {
                ClosePosition();
                LogInfo($"Closing position on gap fill: Position={Position}, PrevClose={_prevClosePrice}");
            }
            
            // Update previous close price for next candle
            _prevClosePrice = candle.ClosePrice;
        }
    }
}
