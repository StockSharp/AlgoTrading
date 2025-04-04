using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Vol Adjusted MA strategy
    /// Strategy enters long when price is above MA + k*ATR, and short when price is below MA - k*ATR
    /// </summary>
    public class VolAdjustedMAStrategy : Strategy
    {
        private readonly StrategyParam<int> _maPeriod;
        private readonly StrategyParam<int> _atrPeriod;
        private readonly StrategyParam<decimal> _atrMultiplier;
        private readonly StrategyParam<DataType> _candleType;
        
        private decimal _prevAdjustedUpperBand;
        private decimal _prevAdjustedLowerBand;

        /// <summary>
        /// MA Period
        /// </summary>
        public int MAPeriod
        {
            get => _maPeriod.Value;
            set => _maPeriod.Value = value;
        }

        /// <summary>
        /// ATR Period
        /// </summary>
        public int ATRPeriod
        {
            get => _atrPeriod.Value;
            set => _atrPeriod.Value = value;
        }

        /// <summary>
        /// ATR multiplier (k)
        /// </summary>
        public decimal ATRMultiplier
        {
            get => _atrMultiplier.Value;
            set => _atrMultiplier.Value = value;
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
        /// Initialize <see cref="VolAdjustedMAStrategy"/>.
        /// </summary>
        public VolAdjustedMAStrategy()
        {
            _maPeriod = Param(nameof(MAPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("MA Period", "Period for Moving Average calculation", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(10, 50, 5);

            _atrPeriod = Param(nameof(ATRPeriod), 14)
                .SetGreaterThanZero()
                .SetDisplay("ATR Period", "Period for Average True Range calculation", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(7, 28, 7);

            _atrMultiplier = Param(nameof(ATRMultiplier), 2.0m)
                .SetGreaterThan(0.1m)
                .SetDisplay("ATR Multiplier", "Multiplier for ATR to adjust MA bands", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(1.0m, 3.0m, 0.5m);

            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
                .SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters");
                
            _prevAdjustedUpperBand = 0;
            _prevAdjustedLowerBand = 0;
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
            var ma = new SimpleMovingAverage() { Length = MAPeriod };
            var atr = new AverageTrueRange() { Length = ATRPeriod };

            // Create subscription and bind indicators
            var subscription = SubscribeCandles(CandleType);
            
            subscription
                .Bind(ma, atr, ProcessCandle)
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

        private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal atrValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Calculate adjusted bands
            var adjustedUpperBand = maValue + ATRMultiplier * atrValue;
            var adjustedLowerBand = maValue - ATRMultiplier * atrValue;

            // Log current values
            LogInfo($"Candle Close: {candle.ClosePrice}, MA: {maValue}, ATR: {atrValue}");
            LogInfo($"Upper Band: {adjustedUpperBand}, Lower Band: {adjustedLowerBand}");

            // Store for next comparison if needed
            _prevAdjustedUpperBand = adjustedUpperBand;
            _prevAdjustedLowerBand = adjustedLowerBand;

            // Trading logic:
            // Long: Price > MA + k*ATR
            if (candle.ClosePrice > adjustedUpperBand && Position <= 0)
            {
                LogInfo($"Buy Signal: Price ({candle.ClosePrice}) > Upper Band ({adjustedUpperBand})");
                BuyMarket(Volume + Math.Abs(Position));
            }
            // Short: Price < MA - k*ATR
            else if (candle.ClosePrice < adjustedLowerBand && Position >= 0)
            {
                LogInfo($"Sell Signal: Price ({candle.ClosePrice}) < Lower Band ({adjustedLowerBand})");
                SellMarket(Volume + Math.Abs(Position));
            }
            // Exit Long: Price < MA
            else if (candle.ClosePrice < maValue && Position > 0)
            {
                LogInfo($"Exit Long: Price ({candle.ClosePrice}) < MA ({maValue})");
                SellMarket(Math.Abs(Position));
            }
            // Exit Short: Price > MA
            else if (candle.ClosePrice > maValue && Position < 0)
            {
                LogInfo($"Exit Short: Price ({candle.ClosePrice}) > MA ({maValue})");
                BuyMarket(Math.Abs(Position));
            }
        }
    }
}