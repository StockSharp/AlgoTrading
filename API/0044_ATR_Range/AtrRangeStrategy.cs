using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// ATR Range strategy
    /// Enters long when price moves up by at least ATR over N candles
    /// Enters short when price moves down by at least ATR over N candles
    /// </summary>
    public class ATRRangeStrategy : Strategy
    {
        private readonly StrategyParam<int> _maPeriod;
        private readonly StrategyParam<int> _atrPeriod;
        private readonly StrategyParam<int> _lookbackPeriod;
        private readonly StrategyParam<DataType> _candleType;
        
        private decimal _nBarsAgoPrice;
        private int _barCounter;

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
        /// Lookback Period (N candles for price movement)
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
        /// Initialize <see cref="ATRRangeStrategy"/>.
        /// </summary>
        public ATRRangeStrategy()
        {
            _maPeriod = Param(nameof(MAPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("MA Period", "Period for Moving Average calculation", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(10, 50, 10);

            _atrPeriod = Param(nameof(ATRPeriod), 14)
                .SetGreaterThanZero()
                .SetDisplay("ATR Period", "Period for Average True Range calculation", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(7, 28, 7);

            _lookbackPeriod = Param(nameof(LookbackPeriod), 5)
                .SetGreaterThanZero()
                .SetDisplay("Lookback Period", "Number of candles to measure price movement", "Strategy Parameters")
                .SetCanOptimize(true)
                .SetOptimize(3, 10, 1);

            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
                .SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters");
                
            _nBarsAgoPrice = 0;
            _barCounter = 0;
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
            var atr = new AverageTrueRange { Length = ATRPeriod };

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
                DrawIndicator(area, atr);
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

            // Increment bar counter
            _barCounter++;

            // Store price for first bar of lookback period
            if (_barCounter == 1 || _barCounter % LookbackPeriod == 1)
            {
                _nBarsAgoPrice = candle.ClosePrice;
                LogInfo($"Storing reference price: {_nBarsAgoPrice} at bar {_barCounter}");
                return;
            }

            // Only check for signals at the end of each lookback period
            if (_barCounter % LookbackPeriod != 0)
                return;

            // Calculate price movement over the lookback period
            var priceMovement = candle.ClosePrice - _nBarsAgoPrice;
            var absMovement = Math.Abs(priceMovement);
            
            // Log current values
            LogInfo($"Candle Close: {candle.ClosePrice}, Reference Price: {_nBarsAgoPrice}, Movement: {priceMovement}");
            LogInfo($"ATR: {atrValue}, MA: {maValue}, Absolute Movement: {absMovement}");

            // Check if price movement exceeds ATR
            if (absMovement >= atrValue)
            {
                // Long signal: Price moved up by at least ATR
                if (priceMovement > 0 && Position <= 0)
                {
                    LogInfo($"Buy Signal: Price movement ({priceMovement}) > ATR ({atrValue})");
                    BuyMarket(Volume + Math.Abs(Position));
                }
                // Short signal: Price moved down by at least ATR
                else if (priceMovement < 0 && Position >= 0)
                {
                    LogInfo($"Sell Signal: Price movement ({priceMovement}) < -ATR ({-atrValue})");
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
        }
    }
}