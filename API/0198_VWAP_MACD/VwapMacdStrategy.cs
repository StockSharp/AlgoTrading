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
    /// Strategy based on VWAP and MACD.
    /// Enters long when price is above VWAP and MACD > Signal.
    /// Enters short when price is below VWAP and MACD < Signal.
    /// Exits when MACD crosses its signal line in the opposite direction.
    /// </summary>
    public class VwapMacdStrategy : Strategy
    {
        private readonly StrategyParam<int> _macdFastPeriod;
        private readonly StrategyParam<int> _macdSlowPeriod;
        private readonly StrategyParam<int> _macdSignalPeriod;
        private readonly StrategyParam<decimal> _stopLossPercent;
        private readonly StrategyParam<DataType> _candleType;

        private MovingAverageConvergenceDivergence _macd;
        
        private decimal _prevMacd;
        private decimal _prevSignal;

        /// <summary>
        /// MACD fast EMA period.
        /// </summary>
        public int MacdFastPeriod
        {
            get => _macdFastPeriod.Value;
            set => _macdFastPeriod.Value = value;
        }

        /// <summary>
        /// MACD slow EMA period.
        /// </summary>
        public int MacdSlowPeriod
        {
            get => _macdSlowPeriod.Value;
            set => _macdSlowPeriod.Value = value;
        }

        /// <summary>
        /// MACD signal line period.
        /// </summary>
        public int MacdSignalPeriod
        {
            get => _macdSignalPeriod.Value;
            set => _macdSignalPeriod.Value = value;
        }

        /// <summary>
        /// Stop loss percentage value.
        /// </summary>
        public decimal StopLossPercent
        {
            get => _stopLossPercent.Value;
            set => _stopLossPercent.Value = value;
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
        /// Initializes a new instance of the <see cref="VwapMacdStrategy"/>.
        /// </summary>
        public VwapMacdStrategy()
        {
            _macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
                .SetDisplayName("MACD Fast Period")
                .SetDescription("Fast EMA period for MACD calculation")
                .SetCategories("Indicators")
                .SetCanOptimize(true);

            _macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
                .SetDisplayName("MACD Slow Period")
                .SetDescription("Slow EMA period for MACD calculation")
                .SetCategories("Indicators")
                .SetCanOptimize(true);

            _macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
                .SetDisplayName("MACD Signal Period")
                .SetDescription("Signal line period for MACD calculation")
                .SetCategories("Indicators")
                .SetCanOptimize(true);

            _stopLossPercent = Param(nameof(StopLossPercent), 2m)
                .SetDisplayName("Stop Loss (%)")
                .SetDescription("Stop loss percentage from entry price")
                .SetCategories("Risk Management");

            _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
                .SetDisplayName("Candle Type")
                .SetDescription("Timeframe of data for strategy")
                .SetCategories("General");
        }

        /// <inheritdoc />
        public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
        {
            return new[] { (Security, CandleType) };
        }

        /// <inheritdoc />
        protected override void OnStarted(DateTimeOffset time)
        {
            base.OnStarted(time);

            // Create MACD indicator
            _macd = new MovingAverageConvergenceDivergence
            {
                FastMa = new ExponentialMovingAverage { Length = MacdFastPeriod },
                SlowMa = new ExponentialMovingAverage { Length = MacdSlowPeriod },
                SignalMa = new ExponentialMovingAverage { Length = MacdSignalPeriod }
            };

            // Initialize variables
            _prevMacd = 0;
            _prevSignal = 0;

            // Enable position protection
            StartProtection(new Unit(StopLossPercent, UnitTypes.Percent), new Unit(StopLossPercent, UnitTypes.Percent));

            // Create subscription
            var subscription = SubscribeCandles(CandleType);

            // Process candles with MACD
            subscription
                .BindEx(_macd, ProcessCandle)
                .Start();

            // Setup chart visualization
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawOwnTrades(area);

                // MACD in separate area
                var macdArea = CreateChartArea();
                if (macdArea != null)
                {
                    DrawIndicator(macdArea, _macd);
                }
            }
        }

        private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Get VWAP value (calculated per day)
            var vwap = candle.VolumeWeightedAveragePrice;

            // Extract MACD and Signal values
            var macd = macdValue[MovingAverageConvergenceDivergence.MacdLine];
            var signal = macdValue[MovingAverageConvergenceDivergence.SignalLine];

            // Detect MACD crosses
            bool macdCrossedAboveSignal = _prevMacd <= _prevSignal && macd > signal;
            bool macdCrossedBelowSignal = _prevMacd >= _prevSignal && macd < signal;

            // Check if strategy is ready for trading
            if (!IsFormedAndOnlineAndAllowTrading())
            {
                // Store current values for next candle
                _prevMacd = macd;
                _prevSignal = signal;
                return;
            }

            // Trading logic
            if (candle.ClosePrice > vwap && macd > signal && Position <= 0)
            {
                // Price above VWAP with bullish MACD - go long
                BuyMarket(Volume + Math.Abs(Position));
            }
            else if (candle.ClosePrice < vwap && macd < signal && Position >= 0)
            {
                // Price below VWAP with bearish MACD - go short
                SellMarket(Volume + Math.Abs(Position));
            }

            // Exit logic based on MACD crosses
            if (Position > 0 && macdCrossedBelowSignal)
            {
                // Exit long position when MACD crosses below Signal
                ClosePosition();
            }
            else if (Position < 0 && macdCrossedAboveSignal)
            {
                // Exit short position when MACD crosses above Signal
                ClosePosition();
            }

            // Store current values for next candle
            _prevMacd = macd;
            _prevSignal = signal;
        }
    }
}