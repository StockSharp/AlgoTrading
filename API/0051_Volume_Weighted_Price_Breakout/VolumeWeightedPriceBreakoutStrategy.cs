using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Volume Weighted Price Breakout Strategy
    /// Long entry: Price rises above the volume-weighted average price over N periods
    /// Short entry: Price falls below the volume-weighted average price over N periods
    /// Exit: Price crosses MA in the opposite direction
    /// </summary>
    public class VolumeWeightedPriceBreakoutStrategy : Strategy
    {
        private readonly StrategyParam<int> _maPeriod;
        private readonly StrategyParam<int> _vwapPeriod;
        private readonly StrategyParam<DataType> _candleType;

        /// <summary>
        /// MA Period
        /// </summary>
        public int MAPeriod
        {
            get => _maPeriod.Value;
            set => _maPeriod.Value = value;
        }

        /// <summary>
        /// VWAP Period
        /// </summary>
        public int VWAPPeriod
        {
            get => _vwapPeriod.Value;
            set => _vwapPeriod.Value = value;
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
        /// Initialize <see cref="VolumeWeightedPriceBreakoutStrategy"/>.
        /// </summary>
        public VolumeWeightedPriceBreakoutStrategy()
        {
            _maPeriod = Param(nameof(MAPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("MA Period", "Period for Moving Average calculation", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(10, 50, 10);

            _vwapPeriod = Param(nameof(VWAPPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("VWAP Period", "Period for Volume Weighted Average Price calculation", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(10, 30, 5);

            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
                .SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters");
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
            var vwma = new VolumeWeightedMovingAverage { Length = VWAPPeriod };

            // Create subscription and bind indicators
            var subscription = SubscribeCandles(CandleType);
            
            subscription
                .Bind(ma, vwma, ProcessCandle)
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
                DrawIndicator(area, vwma);
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal vwmaValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Log current values
            LogInfo($"Candle Close: {candle.ClosePrice}, MA: {maValue}, VWMA: {vwmaValue}");

            // Trading logic:
            // Long: Price above VWMA
            if (candle.ClosePrice > vwmaValue && Position <= 0)
            {
                LogInfo($"Buy Signal: Price ({candle.ClosePrice}) > VWMA ({vwmaValue})");
                BuyMarket(Volume + Math.Abs(Position));
            }
            // Short: Price below VWMA
            else if (candle.ClosePrice < vwmaValue && Position >= 0)
            {
                LogInfo($"Sell Signal: Price ({candle.ClosePrice}) < VWMA ({vwmaValue})");
                SellMarket(Volume + Math.Abs(Position));
            }
            
            // Exit logic: Price crosses MA in the opposite direction
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
        }
    }
}