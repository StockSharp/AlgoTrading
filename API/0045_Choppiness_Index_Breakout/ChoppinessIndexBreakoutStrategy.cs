using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Choppiness Index Breakout strategy
    /// Enters trades when market transitions from choppy to trending state
    /// Long entry: Choppiness Index falls below 38.2 and price is above MA
    /// Short entry: Choppiness Index falls below 38.2 and price is below MA
    /// </summary>
    public class ChoppinessIndexBreakoutStrategy : Strategy
    {
        private readonly StrategyParam<int> _maPeriod;
        private readonly StrategyParam<int> _choppinessPeriod;
        private readonly StrategyParam<decimal> _choppinessThreshold;
        private readonly StrategyParam<decimal> _highChoppinessThreshold;
        private readonly StrategyParam<DataType> _candleType;
        
        private decimal _prevChoppiness;

        /// <summary>
        /// MA Period
        /// </summary>
        public int MAPeriod
        {
            get => _maPeriod.Value;
            set => _maPeriod.Value = value;
        }

        /// <summary>
        /// Choppiness Index Period
        /// </summary>
        public int ChoppinessPeriod
        {
            get => _choppinessPeriod.Value;
            set => _choppinessPeriod.Value = value;
        }

        /// <summary>
        /// Choppiness Threshold (low)
        /// </summary>
        public decimal ChoppinessThreshold
        {
            get => _choppinessThreshold.Value;
            set => _choppinessThreshold.Value = value;
        }

        /// <summary>
        /// High Choppiness Threshold (for exit)
        /// </summary>
        public decimal HighChoppinessThreshold
        {
            get => _highChoppinessThreshold.Value;
            set => _highChoppinessThreshold.Value = value;
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
        /// Initialize <see cref="ChoppinessIndexBreakoutStrategy"/>.
        /// </summary>
        public ChoppinessIndexBreakoutStrategy()
        {
            _maPeriod = Param(nameof(MAPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("MA Period", "Period for Moving Average calculation", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(10, 50, 10);

            _choppinessPeriod = Param(nameof(ChoppinessPeriod), 14)
                .SetGreaterThanZero()
                .SetDisplay("Choppiness Period", "Period for Choppiness Index calculation", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(10, 30, 5);

            _choppinessThreshold = Param(nameof(ChoppinessThreshold), 38.2m)
                .SetRange(20m, 50m)
                .SetDisplay("Choppiness Threshold", "Threshold below which market is considered trending", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(30m, 45m, 2.5m);

            _highChoppinessThreshold = Param(nameof(HighChoppinessThreshold), 61.8m)
                .SetRange(50m, 80m)
                .SetDisplay("High Choppiness Threshold", "Threshold above which to exit positions", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(55m, 70m, 2.5m);

            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
                .SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters");
                
            _prevChoppiness = 100m; // Initialize to high value
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
            var ma = new SimpleMovingAverage { Length = MAPeriod };
            var choppinessIndex = new ChoppinessIndex { Length = ChoppinessPeriod };

            // Create subscription and bind indicators
            var subscription = SubscribeCandles(CandleType);
            
            subscription
                .Bind(ma, choppinessIndex, ProcessCandle)
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
                DrawIndicator(area, choppinessIndex);
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal choppinessValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;
            
            // Log current values
            LogInfo($"Candle Close: {candle.ClosePrice}, MA: {maValue}, Choppiness: {choppinessValue}");
            LogInfo($"Previous Choppiness: {_prevChoppiness}, Threshold: {ChoppinessThreshold}");

            // Check for transition from choppy to trending (falling below threshold)
            var transitionToTrending = _prevChoppiness >= ChoppinessThreshold && choppinessValue < ChoppinessThreshold;
            
            // Trading logic:
            if (transitionToTrending)
            {
                LogInfo($"Market transitioning to trending state: {choppinessValue} < {ChoppinessThreshold}");
                
                // Long: Low choppiness and price above MA
                if (candle.ClosePrice > maValue && Position <= 0)
                {
                    LogInfo($"Buy Signal: Low choppiness ({choppinessValue}) and Price ({candle.ClosePrice}) > MA ({maValue})");
                    BuyMarket(Volume + Math.Abs(Position));
                }
                // Short: Low choppiness and price below MA
                else if (candle.ClosePrice < maValue && Position >= 0)
                {
                    LogInfo($"Sell Signal: Low choppiness ({choppinessValue}) and Price ({candle.ClosePrice}) < MA ({maValue})");
                    SellMarket(Volume + Math.Abs(Position));
                }
            }
            
            // Exit logic: Choppiness rises above high threshold (market becoming choppy again)
            if (choppinessValue > HighChoppinessThreshold)
            {
                LogInfo($"Market becoming choppy: {choppinessValue} > {HighChoppinessThreshold}");
                
                if (Position > 0)
                {
                    LogInfo($"Exit Long: High choppiness ({choppinessValue})");
                    SellMarket(Math.Abs(Position));
                }
                else if (Position < 0)
                {
                    LogInfo($"Exit Short: High choppiness ({choppinessValue})");
                    BuyMarket(Math.Abs(Position));
                }
            }

            // Store current choppiness for next comparison
            _prevChoppiness = choppinessValue;
        }
    }
}