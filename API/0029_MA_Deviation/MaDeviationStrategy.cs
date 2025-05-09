using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy that trades when price deviates significantly from its moving average.
    /// It opens positions when price deviates by a specified percentage from MA
    /// and closes when price returns to MA.
    /// </summary>
    public class MADeviationStrategy : Strategy
    {
        private readonly StrategyParam<int> _maPeriod;
        private readonly StrategyParam<decimal> _deviationPercent;
        private readonly StrategyParam<DataType> _candleType;
        private readonly StrategyParam<int> _atrPeriod;
        private readonly StrategyParam<decimal> _atrMultiplier;

        /// <summary>
        /// Period for Moving Average calculation (default: 20)
        /// </summary>
        public int MAPeriod
        {
            get => _maPeriod.Value;
            set => _maPeriod.Value = value;
        }

        /// <summary>
        /// Deviation percentage from MA required for entry (default: 5%)
        /// </summary>
        public decimal DeviationPercent
        {
            get => _deviationPercent.Value;
            set => _deviationPercent.Value = value;
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
        /// Period for ATR calculation (default: 14)
        /// </summary>
        public int AtrPeriod
        {
            get => _atrPeriod.Value;
            set => _atrPeriod.Value = value;
        }

        /// <summary>
        /// ATR multiplier for stop-loss calculation (default: 2.0)
        /// </summary>
        public decimal AtrMultiplier
        {
            get => _atrMultiplier.Value;
            set => _atrMultiplier.Value = value;
        }

        /// <summary>
        /// Initialize the MA Deviation strategy
        /// </summary>
        public MADeviationStrategy()
        {
            _maPeriod = Param(nameof(MAPeriod), 20)
                .SetDisplayName("MA Period")
                .SetDescription("Period for Moving Average calculation")
                .SetGroup("Technical Parameters")
                .SetCanOptimize(true)
                .SetOptimize(10, 50, 5);

            _deviationPercent = Param(nameof(DeviationPercent), 5m)
                .SetDisplayName("Deviation %")
                .SetDescription("Deviation percentage from MA required for entry")
                .SetGroup("Entry Parameters")
                .SetCanOptimize(true)
                .SetOptimize(2m, 10m, 1m);

            _atrPeriod = Param(nameof(AtrPeriod), 14)
                .SetDisplayName("ATR Period")
                .SetDescription("Period for ATR calculation")
                .SetGroup("Risk Management")
                .SetCanOptimize(true)
                .SetOptimize(7, 21, 7);

            _atrMultiplier = Param(nameof(AtrMultiplier), 2.0m)
                .SetDisplayName("ATR Multiplier")
                .SetDescription("ATR multiplier for stop-loss calculation")
                .SetGroup("Risk Management")
                .SetCanOptimize(true)
                .SetOptimize(1.0m, 3.0m, 0.5m);

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

            // Create indicators
            var sma = new SimpleMovingAverage { Length = MAPeriod };
            var atr = new AverageTrueRange { Length = AtrPeriod };

            // Create subscription and bind indicators
            var subscription = SubscribeCandles(CandleType);
            subscription
                .Bind(sma, atr, ProcessCandle)
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
        /// Process candle and check for MA deviation signals
        /// </summary>
        private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal atrValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Calculate deviation from MA as a percentage
            decimal deviation = (candle.ClosePrice - maValue) / maValue * 100;
            
            // Calculate stop-loss level based on ATR
            decimal stopLoss = atrValue * AtrMultiplier;

            if (Position == 0)
            {
                // No position - check for entry signals
                if (deviation < -DeviationPercent)
                {
                    // Price is below MA by required percentage - buy (long)
                    BuyMarket(Volume);
                }
                else if (deviation > DeviationPercent)
                {
                    // Price is above MA by required percentage - sell (short)
                    SellMarket(Volume);
                }
            }
            else if (Position > 0)
            {
                // Long position - check for exit signal
                if (candle.ClosePrice > maValue)
                {
                    // Price has returned to or above MA - exit long
                    SellMarket(Position);
                }
            }
            else if (Position < 0)
            {
                // Short position - check for exit signal
                if (candle.ClosePrice < maValue)
                {
                    // Price has returned to or below MA - exit short
                    BuyMarket(Math.Abs(Position));
                }
            }
        }
    }
}
