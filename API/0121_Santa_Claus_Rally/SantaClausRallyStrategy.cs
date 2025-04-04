using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Implementation of Santa Claus Rally trading strategy.
    /// The strategy enters long position between December 20 and December 31,
    /// and exits on January 5 of the following year.
    /// </summary>
    public class SantaClausRallyStrategy : Strategy
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
        /// Initializes a new instance of the <see cref="SantaClausRallyStrategy"/>.
        /// </summary>
        public SantaClausRallyStrategy()
        {
            _stopLossPercent = Param(nameof(StopLossPercent), 2m)
                .SetNotNegative()
                .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Protection");
            
            _maPeriod = Param(nameof(MaPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("MA Period", "Moving average period for trend confirmation", "Strategy");
            
            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromDays(1)))
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
            var month = date.Month;
            var day = date.Day;
            
            // Santa Claus Rally period - Dec 20 to Dec 31
            if (month == 12 && day >= 20 && day <= 31 && Position <= 0 && candle.ClosePrice > maValue)
            {
                var volume = Volume + Math.Abs(Position);
                BuyMarket(volume);
                
                LogInfo($"Buy signal for Santa Claus Rally: Date={date:yyyy-MM-dd}, Price={candle.ClosePrice}, MA={maValue}, Volume={volume}");
            }
            // Exit position on January 5
            else if (month == 1 && day == 5 && Position > 0)
            {
                ClosePosition();
                LogInfo($"Closing position on January 5: Position={Position}");
            }
        }
    }
}
