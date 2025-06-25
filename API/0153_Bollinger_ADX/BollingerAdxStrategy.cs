using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy combining Bollinger Bands and ADX indicators.
    /// Looks for breakouts with strong trend confirmation.
    /// </summary>
    public class BollingerAdxStrategy : Strategy
    {
        private readonly StrategyParam<int> _bollingerPeriod;
        private readonly StrategyParam<decimal> _bollingerDeviation;
        private readonly StrategyParam<int> _adxPeriod;
        private readonly StrategyParam<decimal> _adxThreshold;
        private readonly StrategyParam<decimal> _atrMultiplier;
        private readonly StrategyParam<DataType> _candleType;

        /// <summary>
        /// Bollinger Bands period.
        /// </summary>
        public int BollingerPeriod
        {
            get => _bollingerPeriod.Value;
            set => _bollingerPeriod.Value = value;
        }

        /// <summary>
        /// Bollinger Bands standard deviation multiplier.
        /// </summary>
        public decimal BollingerDeviation
        {
            get => _bollingerDeviation.Value;
            set => _bollingerDeviation.Value = value;
        }

        /// <summary>
        /// ADX indicator period.
        /// </summary>
        public int AdxPeriod
        {
            get => _adxPeriod.Value;
            set => _adxPeriod.Value = value;
        }

        /// <summary>
        /// ADX threshold for strong trend.
        /// </summary>
        public decimal AdxThreshold
        {
            get => _adxThreshold.Value;
            set => _adxThreshold.Value = value;
        }

        /// <summary>
        /// ATR multiplier for stop-loss.
        /// </summary>
        public decimal AtrMultiplier
        {
            get => _atrMultiplier.Value;
            set => _atrMultiplier.Value = value;
        }

        /// <summary>
        /// Candle type for strategy calculation.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Initialize strategy.
        /// </summary>
        public BollingerAdxStrategy()
        {
            _bollingerPeriod = Param(nameof(BollingerPeriod), 20)
                .SetGreaterThanZero()
                .SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(10, 30, 5);

            _bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
                .SetGreaterThanZero()
                .SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(1.5m, 2.5m, 0.5m);

            _adxPeriod = Param(nameof(AdxPeriod), 14)
                .SetGreaterThanZero()
                .SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(7, 21, 7);

            _adxThreshold = Param(nameof(AdxThreshold), 25m)
                .SetGreaterThanZero()
                .SetDisplay("ADX Threshold", "ADX level considered as strong trend", "Trading Levels")
                .SetCanOptimize(true)
                .SetOptimize(20, 30, 5);

            _atrMultiplier = Param(nameof(AtrMultiplier), 2.0m)
                .SetGreaterThanZero()
                .SetDisplay("ATR Multiplier", "Multiplier for ATR to set stop-loss", "Risk Management")
                .SetCanOptimize(true)
                .SetOptimize(1.0m, 3.0m, 0.5m);

            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
                .SetDisplay("Candle Type", "Type of candles to use", "General");
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
            var bollingerBands = new BollingerBands
            {
                Length = BollingerPeriod,
                Width = BollingerDeviation
            };

            var adx = new ADX { Length = AdxPeriod };
            var atr = new ATR { Length = AdxPeriod };

            // Create subscription
            var subscription = SubscribeCandles(CandleType);

            // Bind indicators to candles
            subscription
                .Bind(bollingerBands, adx, atr, ProcessCandle)
                .Start();

            // Enable stop-loss using ATR
            StartProtection(
                takeProfit: null,
                stopLoss: new Unit(AtrMultiplier, UnitTypes.Absolute),
                isStopTrailing: false,
                useMarketOrders: true
            );

            // Setup chart if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, bollingerBands);
                
                // Create second area for ADX
                var adxArea = CreateChartArea();
                DrawIndicator(adxArea, adx);
                
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand, decimal adxValue, decimal atrValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Trading logic - only trade when ADX indicates strong trend
            if (adxValue > AdxThreshold)
            {
                // Strong trend detected
                if (candle.ClosePrice > upperBand && Position <= 0)
                {
                    // Price breaks above upper Bollinger band - Buy
                    var volume = Volume + Math.Abs(Position);
                    BuyMarket(volume);
                }
                else if (candle.ClosePrice < lowerBand && Position >= 0)
                {
                    // Price breaks below lower Bollinger band - Sell
                    var volume = Volume + Math.Abs(Position);
                    SellMarket(volume);
                }
            }
            
            // Exit positions when price returns to middle band
            if ((Position > 0 && candle.ClosePrice < middleBand) ||
                (Position < 0 && candle.ClosePrice > middleBand))
            {
                ClosePosition();
            }
        }
    }
}
