using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Parabolic SAR Reversal Strategy.
    /// Enters long when SAR switches from above to below price.
    /// Enters short when SAR switches from below to above price.
    /// </summary>
    public class ParabolicSarReversalStrategy : Strategy
    {
        private readonly StrategyParam<decimal> _initialAcceleration;
        private readonly StrategyParam<decimal> _maxAcceleration;
        private readonly StrategyParam<DataType> _candleType;
        
        private bool? _prevIsSarAbovePrice;

        /// <summary>
        /// Initial acceleration factor for Parabolic SAR.
        /// </summary>
        public decimal InitialAcceleration
        {
            get => _initialAcceleration.Value;
            set => _initialAcceleration.Value = value;
        }

        /// <summary>
        /// Maximum acceleration factor for Parabolic SAR.
        /// </summary>
        public decimal MaxAcceleration
        {
            get => _maxAcceleration.Value;
            set => _maxAcceleration.Value = value;
        }

        /// <summary>
        /// Type of candles to use.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParabolicSarReversalStrategy"/>.
        /// </summary>
        public ParabolicSarReversalStrategy()
        {
            _initialAcceleration = Param(nameof(InitialAcceleration), 0.02m)
                .SetDisplay("Initial Acceleration", "Initial acceleration factor for Parabolic SAR", "SAR Settings")
                .SetRange(0.01m, 0.05m, 0.01m)
                .SetCanOptimize(true);
                
            _maxAcceleration = Param(nameof(MaxAcceleration), 0.2m)
                .SetDisplay("Max Acceleration", "Maximum acceleration factor for Parabolic SAR", "SAR Settings")
                .SetRange(0.1m, 0.3m, 0.05m)
                .SetCanOptimize(true);
                
            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
                .SetDisplay("Candle Type", "Type of candles to use", "General");
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

            // Initialize previous state
            _prevIsSarAbovePrice = null;

            // Create Parabolic SAR indicator
            var parabolicSar = new ParabolicSar
            {
                Acceleration = InitialAcceleration,
                MaxAcceleration = MaxAcceleration
            };

            // Create subscription
            var subscription = SubscribeCandles(CandleType);
            
            // Bind indicator and process candles
            subscription
                .Bind(parabolicSar, ProcessCandle)
                .Start();
                
            // Setup chart visualization
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, parabolicSar);
                DrawOwnTrades(area);
            }
        }

        /// <summary>
        /// Process candle with Parabolic SAR value.
        /// </summary>
        /// <param name="candle">Candle.</param>
        /// <param name="sarValue">Parabolic SAR value.</param>
        private void ProcessCandle(ICandleMessage candle, decimal sarValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Determine if SAR is above or below price
            bool isSarAbovePrice = sarValue > candle.ClosePrice;
            
            // If this is the first calculation, just store the state
            if (_prevIsSarAbovePrice == null)
            {
                _prevIsSarAbovePrice = isSarAbovePrice;
                return;
            }
            
            // Check for SAR reversal
            bool sarSwitchedBelow = _prevIsSarAbovePrice.Value && !isSarAbovePrice;
            bool sarSwitchedAbove = !_prevIsSarAbovePrice.Value && isSarAbovePrice;
            
            // Long entry: SAR switched from above to below price
            if (sarSwitchedBelow && Position <= 0)
            {
                BuyMarket(Volume + Math.Abs(Position));
                LogInfo($"Long entry: SAR ({sarValue}) switched below price ({candle.ClosePrice})");
            }
            // Short entry: SAR switched from below to above price
            else if (sarSwitchedAbove && Position >= 0)
            {
                SellMarket(Volume + Math.Abs(Position));
                LogInfo($"Short entry: SAR ({sarValue}) switched above price ({candle.ClosePrice})");
            }
            
            // Update the previous state
            _prevIsSarAbovePrice = isSarAbovePrice;
        }
    }
}