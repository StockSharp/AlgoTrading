using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Volume Contraction Pattern (VCP) strategy.
    /// The strategy looks for narrowing price ranges and breakouts after contraction.
    /// Long entry: Range contraction followed by a break above previous high
    /// Short entry: Range contraction followed by a break below previous low
    /// </summary>
    public class VCPStrategy : Strategy
    {
        private readonly StrategyParam<int> _maPeriod;
        private readonly StrategyParam<int> _lookbackPeriod;
        private readonly StrategyParam<DataType> _candleType;
        
        private decimal _prevCandleRange;

        /// <summary>
        /// MA Period
        /// </summary>
        public int MAPeriod
        {
            get => _maPeriod.Value;
            set => _maPeriod.Value = value;
        }

        /// <summary>
        /// Lookback Period (for breakout levels)
        /// </summary>
        public int LookbackPeriod
        {
            get => _lookbackPeriod.Value;
            set => _lookbackPeriod.Value = value;
        }

        /// <summary>
        /// Candle type for strategy calculation
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Initialize <see cref="VCPStrategy"/>.
        /// </summary>
        public VCPStrategy()
        {
            _maPeriod = Param(nameof(MAPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("MA Period", "Period for Moving Average calculation", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(10, 50, 10);

            _lookbackPeriod = Param(nameof(LookbackPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("Lookback Period", "Period for calculating breakout levels", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(10, 30, 5);

            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
                .SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters");
                
            _prevCandleRange = 0;
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
            var ma = new SimpleMovingAverage { Length = MAPeriod };
            var highest = new Highest { Length = LookbackPeriod };
            var lowest = new Lowest { Length = LookbackPeriod };

            // Create subscription and bind indicators
            var subscription = SubscribeCandles(CandleType);
            
            subscription
                .Bind(highest, lowest, ma, ProcessCandle)
                .Start();

            // Configure protection
            StartProtection(
                takeProfit: new Unit(3, UnitTypes.Percent),
                stopLoss: new Unit(2, UnitTypes.Percent)
            );

            // Setup chart visualization
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, ma);
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal highestValue, decimal lowestValue, decimal maValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Calculate current candle range
            var currentCandleRange = candle.HighPrice - candle.LowPrice;
            
            // If first candle, just store the range and return
            if (_prevCandleRange == 0)
            {
                _prevCandleRange = currentCandleRange;
                return;
            }

            // Check for range contraction (current range smaller than previous range)
            var isContraction = currentCandleRange < _prevCandleRange;
            
            // Log current values
            LogInfo($"Candle Range: {currentCandleRange}, Previous Range: {_prevCandleRange}, Contraction: {isContraction}");
            LogInfo($"Highest: {highestValue}, Lowest: {lowestValue}, MA: {maValue}");

            // Trading logic:
            if (isContraction)
            {
                // Long: Contraction and breakout above highest high
                if (candle.ClosePrice > highestValue && Position <= 0)
                {
                    LogInfo($"Buy Signal: Contraction and Price ({candle.ClosePrice}) > Highest ({highestValue})");
                    BuyMarket(Volume + Math.Abs(Position));
                }
                // Short: Contraction and breakout below lowest low
                else if (candle.ClosePrice < lowestValue && Position >= 0)
                {
                    LogInfo($"Sell Signal: Contraction and Price ({candle.ClosePrice}) < Lowest ({lowestValue})");
                    SellMarket(Volume + Math.Abs(Position));
                }
            }
            
            // Exit logic: Price crosses MA
            if (Position > 0 && candle.ClosePrice < maValue)
            {
                LogInfo($"Exit Long: Price ({candle.ClosePrice}) < MA ({maValue})");
                SellMarket(Math.Abs(Position));
            }
            else if (Position < 0 && candle.ClosePrice > maValue)
            {
                LogInfo($"Exit Short: Price ({candle.ClosePrice}) > MA ({maValue})");
                BuyMarket(Math.Abs(Position));
            }

            // Store current range for next comparison
            _prevCandleRange = currentCandleRange;
        }
    }
}