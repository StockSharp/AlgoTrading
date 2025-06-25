using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Implementation of Quarterly Expiry trading strategy.
    /// The strategy trades on quarterly expiration days based on price relative to MA.
    /// </summary>
    public class QuarterlyExpiryStrategy : Strategy
    {
        private readonly StrategyParam<decimal> _stopLossPercent;
        private readonly StrategyParam<int> _maPeriod;
        private readonly StrategyParam<DataType> _candleType;

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
        /// Initializes a new instance of the <see cref="QuarterlyExpiryStrategy"/>.
        /// </summary>
        public QuarterlyExpiryStrategy()
        {
            _stopLossPercent = Param(nameof(StopLossPercent), 2m)
                .SetNotNegative()
                .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Protection");
            
            _maPeriod = Param(nameof(MaPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("MA Period", "Moving average period for trend confirmation", "Strategy");
            
            _candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
                .SetDisplay("Candle Type", "Type of candles for strategy", "Strategy");
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
            
            // Create a simple moving average indicator
            var sma = new StockSharp.Algo.Indicators.SimpleMovingAverage { Length = MaPeriod };
            
            // Create subscription and bind indicator
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
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;
            
            // Skip if strategy is not ready
            if (!IsFormedAndOnlineAndAllowTrading())
                return;
            
            var date = candle.OpenTime;
            var dayOfWeek = date.DayOfWeek;
            
            // Check if this is a quarterly expiry day
            // Typically the third Friday of March, June, September, and December
            if (IsQuarterlyExpiryDay(date))
            {
                // BUY signal - price above MA
                if (candle.ClosePrice > maValue && Position <= 0)
                {
                    var volume = Volume + Math.Abs(Position);
                    BuyMarket(volume);
                    
                    LogInfo($"Buy signal on quarterly expiry day: Date={date:yyyy-MM-dd}, Price={candle.ClosePrice}, MA={maValue}, Volume={volume}");
                }
                // SELL signal - price below MA
                else if (candle.ClosePrice < maValue && Position >= 0)
                {
                    var volume = Volume + Math.Abs(Position);
                    SellMarket(volume);
                    
                    LogInfo($"Sell signal on quarterly expiry day: Date={date:yyyy-MM-dd}, Price={candle.ClosePrice}, MA={maValue}, Volume={volume}");
                }
            }
            // Exit position on Friday (if we're not already on a Friday)
            else if (dayOfWeek == DayOfWeek.Friday && Position != 0)
            {
                ClosePosition();
                LogInfo($"Closing position on Friday: Date={date:yyyy-MM-dd}, Position={Position}");
            }
        }
        
        private bool IsQuarterlyExpiryDay(DateTimeOffset date)
        {
            // Check if it's a Friday
            if (date.DayOfWeek != DayOfWeek.Friday)
                return false;
            
            // Check if it's March, June, September, or December
            int month = date.Month;
            if (month != 3 && month != 6 && month != 9 && month != 12)
                return false;
            
            // Check if it's the third Friday of the month
            // Find the first day of the month
            var firstDay = new DateTimeOffset(date.Year, date.Month, 1, 0, 0, 0, date.Offset);
            
            // Find the first Friday
            int daysUntilFirstFriday = ((int)DayOfWeek.Friday - (int)firstDay.DayOfWeek + 7) % 7;
            var firstFriday = firstDay.AddDays(daysUntilFirstFriday);
            
            // Calculate the third Friday
            var thirdFriday = firstFriday.AddDays(14);
            
            // Check if the date is the third Friday
            return date.Day == thirdFriday.Day;
        }
    }
}
