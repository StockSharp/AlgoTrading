using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Implementation of Month of Year seasonal trading strategy.
    /// The strategy enters long position in November and short position in February.
    /// </summary>
    public class MonthOfYearStrategy : Strategy
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
        /// Initializes a new instance of the <see cref="MonthOfYearStrategy"/>.
        /// </summary>
        public MonthOfYearStrategy()
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
            
            var currentMonth = candle.OpenTime.Month;
            
            // November - BUY signal (Month = 11)
            if (currentMonth == 11 && Position <= 0 && candle.ClosePrice > maValue)
            {
                var volume = Volume + Math.Abs(Position);
                BuyMarket(volume);
                
                LogInfo($"Buy signal in November: Price={candle.ClosePrice}, MA={maValue}, Volume={volume}");
            }
            // February - SELL signal (Month = 2)
            else if (currentMonth == 2 && Position >= 0 && candle.ClosePrice < maValue)
            {
                var volume = Volume + Math.Abs(Position);
                SellMarket(volume);
                
                LogInfo($"Sell signal in February: Price={candle.ClosePrice}, MA={maValue}, Volume={volume}");
            }
            // Closing conditions
            else if ((currentMonth == 12 && Position > 0) || // Close long position in December
                    (currentMonth == 3 && Position < 0))     // Close short position in March
            {
                ClosePosition();
                LogInfo($"Closing position in month {currentMonth}: Position={Position}");
            }
        }
    }
}
