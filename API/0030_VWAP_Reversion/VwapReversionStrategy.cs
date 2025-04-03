using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// VWAP Reversion strategy that trades on deviations from Volume Weighted Average Price.
    /// It opens positions when price deviates by a specified percentage from VWAP
    /// and exits when price returns to VWAP.
    /// </summary>
    public class VwapReversionStrategy : Strategy
    {
        private readonly StrategyParam<decimal> _deviationPercent;
        private readonly StrategyParam<decimal> _stopLossPercent;
        private readonly StrategyParam<DataType> _candleType;

        /// <summary>
        /// Deviation percentage from VWAP required for entry (default: 2%)
        /// </summary>
        public decimal DeviationPercent
        {
            get => _deviationPercent.Value;
            set => _deviationPercent.Value = value;
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
        /// Initialize the VWAP Reversion strategy
        /// </summary>
        public VwapReversionStrategy()
        {
            _deviationPercent = Param(nameof(DeviationPercent), 2.0m)
                .SetDisplayName("Deviation %")
                .SetDescription("Deviation percentage from VWAP required for entry")
                .SetGroup("Entry Parameters")
                .SetCanOptimize(true)
                .SetOptimize(1.0m, 5.0m, 0.5m);

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

            // Create the VWAP indicator
            var vwap = new VolumeWeightedAveragePrice();

            // Create subscription and bind VWAP indicator
            var subscription = SubscribeCandles(CandleType);
            subscription
                .Bind(vwap, ProcessCandle)
                .Start();

            // Configure chart
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, vwap);
                DrawOwnTrades(area);
            }

            // Setup protection with stop-loss
            StartProtection(
                new Unit(0), // No take profit
                new Unit(StopLossPercent, UnitTypes.Percent) // Stop loss as percentage of entry price
            );
        }

        /// <summary>
        /// Process candle and check for VWAP deviation signals
        /// </summary>
        private void ProcessCandle(ICandleMessage candle, decimal vwapValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Calculate deviation from VWAP
            decimal deviationRatio = 0;
            
            if (vwapValue > 0)
            {
                deviationRatio = (candle.ClosePrice - vwapValue) / vwapValue;
            }

            // Convert ratio to percentage
            decimal deviationPercent = deviationRatio * 100;
            
            var deviationThreshold = DeviationPercent / 100; // Convert percentage to ratio for comparison

            if (Position == 0)
            {
                // No position - check for entry signals
                if (deviationRatio < -deviationThreshold)
                {
                    // Price is below VWAP by required percentage - buy (long)
                    BuyMarket(Volume);
                }
                else if (deviationRatio > deviationThreshold)
                {
                    // Price is above VWAP by required percentage - sell (short)
                    SellMarket(Volume);
                }
            }
            else if (Position > 0)
            {
                // Long position - check for exit signal
                if (candle.ClosePrice > vwapValue)
                {
                    // Price has returned to or above VWAP - exit long
                    SellMarket(Position);
                }
            }
            else if (Position < 0)
            {
                // Short position - check for exit signal
                if (candle.ClosePrice < vwapValue)
                {
                    // Price has returned to or below VWAP - exit short
                    BuyMarket(Math.Abs(Position));
                }
            }
        }
    }
}
