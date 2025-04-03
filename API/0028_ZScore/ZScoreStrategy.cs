using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy based on Z-Score indicator for mean reversion trading.
    /// Z-Score measures the distance from the price to its moving average in standard deviations.
    /// </summary>
    public class ZScoreStrategy : Strategy
    {
        private readonly StrategyParam<decimal> _zScoreEntryThreshold;
        private readonly StrategyParam<decimal> _zScoreExitThreshold;
        private readonly StrategyParam<int> _maPeriod;
        private readonly StrategyParam<int> _stdDevPeriod;
        private readonly StrategyParam<decimal> _stopLossPercent;
        private readonly StrategyParam<DataType> _candleType;

        private decimal _prevZScore;

        /// <summary>
        /// Z-Score threshold for entry (default: 2.0)
        /// </summary>
        public decimal ZScoreEntryThreshold
        {
            get => _zScoreEntryThreshold.Value;
            set => _zScoreEntryThreshold.Value = value;
        }

        /// <summary>
        /// Z-Score threshold for exit (default: 0.0)
        /// </summary>
        public decimal ZScoreExitThreshold
        {
            get => _zScoreExitThreshold.Value;
            set => _zScoreExitThreshold.Value = value;
        }

        /// <summary>
        /// Period for Moving Average calculation (default: 20)
        /// </summary>
        public int MAPeriod
        {
            get => _maPeriod.Value;
            set => _maPeriod.Value = value;
        }

        /// <summary>
        /// Period for Standard Deviation calculation (default: 20)
        /// </summary>
        public int StdDevPeriod
        {
            get => _stdDevPeriod.Value;
            set => _stdDevPeriod.Value = value;
        }

        /// <summary>
        /// Stop-loss as percentage from entry price (default: 2%)
        /// </summary>
        public decimal StopLossPercent
        {
            get => _stopLossPercent.Value;
            set => _stopLossPercent.Value = value;
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
        /// Initialize the Z-Score strategy
        /// </summary>
        public ZScoreStrategy()
        {
            _zScoreEntryThreshold = Param(nameof(ZScoreEntryThreshold), 2.0m)
                .SetDisplayName("Z-Score Entry Threshold")
                .SetDescription("Distance from mean in std deviations required to enter position")
                .SetGroup("Z-Score Parameters")
                .SetCanOptimize(true)
                .SetOptimize(1.5m, 3.0m, 0.5m);

            _zScoreExitThreshold = Param(nameof(ZScoreExitThreshold), 0.0m)
                .SetDisplayName("Z-Score Exit Threshold")
                .SetDescription("Distance from mean in std deviations required to exit position")
                .SetGroup("Z-Score Parameters")
                .SetCanOptimize(true)
                .SetOptimize(0.0m, 1.0m, 0.2m);

            _maPeriod = Param(nameof(MAPeriod), 20)
                .SetDisplayName("MA Period")
                .SetDescription("Period for Moving Average calculation")
                .SetGroup("Technical Parameters")
                .SetCanOptimize(true)
                .SetOptimize(10, 50, 5);

            _stdDevPeriod = Param(nameof(StdDevPeriod), 20)
                .SetDisplayName("StdDev Period")
                .SetDescription("Period for Standard Deviation calculation")
                .SetGroup("Technical Parameters")
                .SetCanOptimize(true)
                .SetOptimize(10, 50, 5);

            _stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
                .SetDisplayName("Stop Loss %")
                .SetDescription("Stop loss as percentage from entry price")
                .SetGroup("Risk Management")
                .SetCanOptimize(true)
                .SetOptimize(1.0m, 5.0m, 0.5m);

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
            _prevZScore = 0;

            // Create indicators
            var sma = new SimpleMovingAverage { Length = MAPeriod };
            var stdDev = new StandardDeviation { Length = StdDevPeriod };

            // Create subscription and bind indicators
            var subscription = SubscribeCandles(CandleType);
            subscription
                .Bind(sma, stdDev, ProcessCandle)
                .Start();

            // Configure chart
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, sma);
                DrawOwnTrades(area);
            }

            // Setup protection with stop-loss
            StartProtection(
                new Unit(0), // No take profit
                new Unit(StopLossPercent, UnitTypes.Percent) // Stop loss as percentage of entry price
            );
        }

        /// <summary>
        /// Process candle and calculate Z-Score
        /// </summary>
        private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal stdDevValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Calculate Z-Score: (price - MA) / StdDev
            // Avoid division by zero
            if (stdDevValue == 0)
                return;

            decimal zScore = (candle.ClosePrice - maValue) / stdDevValue;

            // Process trading signals
            if (Position == 0)
            {
                // No position - check for entry signals
                if (zScore < -ZScoreEntryThreshold)
                {
                    // Price is below MA by more than threshold std deviations - buy (long)
                    BuyMarket(Volume);
                }
                else if (zScore > ZScoreEntryThreshold)
                {
                    // Price is above MA by more than threshold std deviations - sell (short)
                    SellMarket(Volume);
                }
            }
            else if (Position > 0)
            {
                // Long position - check for exit signal
                if (zScore > ZScoreExitThreshold)
                {
                    // Price has returned to or above mean - exit long
                    SellMarket(Position);
                }
            }
            else if (Position < 0)
            {
                // Short position - check for exit signal
                if (zScore < ZScoreExitThreshold)
                {
                    // Price has returned to or below mean - exit short
                    BuyMarket(Math.Abs(Position));
                }
            }

            // Store current Z-Score for later use
            _prevZScore = zScore;
        }
    }
}
