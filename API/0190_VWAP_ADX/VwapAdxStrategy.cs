using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy based on VWAP and ADX indicators.
    /// Enters long when price is above VWAP and ADX > 25.
    /// Enters short when price is below VWAP and ADX > 25.
    /// Exits when ADX < 20.
    /// </summary>
    public class VwapAdxStrategy : Strategy
    {
        private readonly StrategyParam<decimal> _stopLossPercent;
        private readonly StrategyParam<int> _adxPeriod;
        private readonly StrategyParam<DataType> _candleType;

        private AverageDirectionalMovementIndex _adx;
        private decimal _prevAdxValue;

        /// <summary>
        /// Stop loss percentage value.
        /// </summary>
        public decimal StopLossPercent
        {
            get => _stopLossPercent.Value;
            set => _stopLossPercent.Value = value;
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
        /// Candle type for strategy.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VwapAdxStrategy"/>.
        /// </summary>
        public VwapAdxStrategy()
        {
            _stopLossPercent = Param(nameof(StopLossPercent), 2m)
                .SetDisplayName("Stop loss (%)")
                .SetDescription("Stop loss percentage from entry price")
                .SetCategories("Risk Management");

            _adxPeriod = Param(nameof(AdxPeriod), 14)
                .SetDisplayName("ADX Period")
                .SetDescription("Period for Average Directional Movement Index")
                .SetCategories("Indicators")
                .SetCanOptimize(true)
                .SetOptimize(10, 20, 1);

            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
                .SetDisplayName("Candle Type")
                .SetDescription("Timeframe of data for strategy")
                .SetCategories("General");
        }

        /// <inheritdoc />
        public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
        {
            return [(Security, CandleType)];
        }

        /// <inheritdoc />
        protected override void OnStarted(DateTimeOffset time)
        {
            base.OnStarted(time);

            // Create ADX indicator
            _adx = new AverageDirectionalMovementIndex { Length = AdxPeriod };

            // Enable position protection
            StartProtection(new Unit(StopLossPercent, UnitTypes.Percent), new Unit(StopLossPercent, UnitTypes.Percent));

            // Create subscription and subscribe to VWAP
            var subscription = SubscribeCandles(CandleType);

            // Process candles with ADX
            subscription
                .BindEx(_adx, ProcessCandle)
                .Start();

            // Setup chart visualization
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, _adx);
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Get current ADX value
            var currentAdxValue = adxValue.GetValue<decimal>();

            // Get VWAP value (calculated per day)
            var vwap = candle.VolumeWeightedAveragePrice;

            // Skip if not formed or online
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Trading logic
            if (currentAdxValue > 25)
            {
                // Strong trend detected
                if (candle.ClosePrice > vwap && Position <= 0)
                {
                    // Price above VWAP - go long
                    BuyMarket(Volume + Math.Abs(Position));
                }
                else if (candle.ClosePrice < vwap && Position >= 0)
                {
                    // Price below VWAP - go short
                    SellMarket(Volume + Math.Abs(Position));
                }
            }
            else if (currentAdxValue < 20 && _prevAdxValue > 20)
            {
                // Trend weakening - close position
                ClosePosition();
            }

            // Store current ADX value for next candle
            _prevAdxValue = currentAdxValue;
        }
    }
}