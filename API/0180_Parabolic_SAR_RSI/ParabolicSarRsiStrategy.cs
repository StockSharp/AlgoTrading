using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy based on Parabolic SAR and RSI indicators.
    /// Enters long when price is above SAR and RSI is oversold (< 30)
    /// Enters short when price is below SAR and RSI is overbought (> 70)
    /// </summary>
    public class ParabolicSarRsiStrategy : Strategy
    {
        private readonly StrategyParam<decimal> _sarAccelerationFactor;
        private readonly StrategyParam<decimal> _sarMaximumAcceleration;
        private readonly StrategyParam<int> _rsiPeriod;
        private readonly StrategyParam<DataType> _candleType;

        /// <summary>
        /// Parabolic SAR acceleration factor
        /// </summary>
        public decimal SarAccelerationFactor
        {
            get => _sarAccelerationFactor.Value;
            set => _sarAccelerationFactor.Value = value;
        }

        /// <summary>
        /// Parabolic SAR maximum acceleration
        /// </summary>
        public decimal SarMaximumAcceleration
        {
            get => _sarMaximumAcceleration.Value;
            set => _sarMaximumAcceleration.Value = value;
        }

        /// <summary>
        /// RSI period
        /// </summary>
        public int RsiPeriod
        {
            get => _rsiPeriod.Value;
            set => _rsiPeriod.Value = value;
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
        /// Constructor
        /// </summary>
        public ParabolicSarRsiStrategy()
        {
            _sarAccelerationFactor = Param(nameof(SarAccelerationFactor), 0.02m)
                .SetGreaterThanZero()
                .SetDisplay("SAR Acceleration Factor", "Acceleration factor for Parabolic SAR", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(0.01m, 0.05m, 0.01m);

            _sarMaximumAcceleration = Param(nameof(SarMaximumAcceleration), 0.2m)
                .SetGreaterThanZero()
                .SetDisplay("SAR Maximum Acceleration", "Maximum acceleration for Parabolic SAR", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(0.1m, 0.3m, 0.05m);

            _rsiPeriod = Param(nameof(RsiPeriod), 14)
                .SetGreaterThanZero()
                .SetDisplay("RSI Period", "Period for RSI indicator", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(10, 20, 2);

            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
                .SetDisplay("Candle Type", "Timeframe for strategy", "General");
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
            var sar = new ParabolicSar
            {
                AccelerationFactor = SarAccelerationFactor,
                AccelerationLimit = SarMaximumAcceleration
            };
            
            var rsi = new RelativeStrengthIndex { Length = RsiPeriod };

            // Subscribe to candles and bind indicators
            var subscription = SubscribeCandles(CandleType);
            
            subscription
                .Bind(sar, rsi, ProcessCandle)
                .Start();

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, sar);
                
                // Create a separate area for RSI
                var rsiArea = CreateChartArea();
                if (rsiArea != null)
                {
                    DrawIndicator(rsiArea, rsi);
                }
                
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal sarValue, decimal rsiValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;
            
            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Current price (close of the candle)
            var price = candle.ClosePrice;
            
            // Get SAR value and determine trend
            var sarValue2 = sarValue;
            var isAboveSar = price > sarValue2;
            var isBelowSar = price < sarValue2;

            // Trading logic
            if (isAboveSar && rsiValue < 30 && Position <= 0)
            {
                // Buy signal: price above SAR and RSI oversold
                BuyMarket(Volume + Math.Abs(Position));
            }
            else if (isBelowSar && rsiValue > 70 && Position >= 0)
            {
                // Sell signal: price below SAR and RSI overbought
                SellMarket(Volume + Math.Abs(Position));
            }
            // Exit conditions based only on SAR
            else if (isBelowSar && Position > 0)
            {
                // Exit long when price goes below SAR
                SellMarket(Position);
            }
            else if (isAboveSar && Position < 0)
            {
                // Exit short when price goes above SAR
                BuyMarket(Math.Abs(Position));
            }
        }
    }
}