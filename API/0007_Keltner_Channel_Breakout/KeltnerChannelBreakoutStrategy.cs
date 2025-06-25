using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy based on Keltner Channel breakout.
    /// It enters long position when price breaks through the upper band and short position when price breaks through the lower band.
    /// </summary>
    public class KeltnerChannelBreakoutStrategy : Strategy
    {
        private readonly StrategyParam<int> _emaPeriod;
        private readonly StrategyParam<int> _atrPeriod;
        private readonly StrategyParam<decimal> _atrMultiplier;
        private readonly StrategyParam<DataType> _candleType;

        // Current state
        private decimal _prevClosePrice;
        private decimal _prevUpperBand;
        private decimal _prevLowerBand;
        private decimal _prevEma;

        /// <summary>
        /// Period for EMA calculation.
        /// </summary>
        public int EmaPeriod
        {
            get => _emaPeriod.Value;
            set => _emaPeriod.Value = value;
        }

        /// <summary>
        /// Period for ATR calculation.
        /// </summary>
        public int AtrPeriod
        {
            get => _atrPeriod.Value;
            set => _atrPeriod.Value = value;
        }

        /// <summary>
        /// Multiplier for ATR to determine channel width.
        /// </summary>
        public decimal AtrMultiplier
        {
            get => _atrMultiplier.Value;
            set => _atrMultiplier.Value = value;
        }

        /// <summary>
        /// Candle type.
        /// </summary>
        public DataType CandleType
        {
            get => _candleType.Value;
            set => _candleType.Value = value;
        }

        /// <summary>
        /// Initialize the Keltner Channel Breakout strategy.
        /// </summary>
        public KeltnerChannelBreakoutStrategy()
        {
            _emaPeriod = Param(nameof(EmaPeriod), 20)
                .SetDisplay("EMA Period", "Period for Exponential Moving Average", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(10, 50, 5);

            _atrPeriod = Param(nameof(AtrPeriod), 14)
                .SetDisplay("ATR Period", "Period for Average True Range", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(10, 30, 2);

            _atrMultiplier = Param(nameof(AtrMultiplier), 2m)
                .SetDisplay("ATR Multiplier", "Multiplier for ATR to determine channel width", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(1, 3, 0.5m);

            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
                .SetDisplay("Candle Type", "Type of candles to use", "General");
                
            _prevClosePrice = 0;
            _prevUpperBand = 0;
            _prevLowerBand = 0;
            _prevEma = 0;
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
            var keltnerChannel = new KeltnerChannels
            {
                Length = EmaPeriod,
                ATRLength = AtrPeriod,
                K = AtrMultiplier
            };

            // Create subscription and bind indicators
            var subscription = SubscribeCandles(CandleType);
            subscription
                .Bind(keltnerChannel, ProcessCandle)
                .Start();

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, keltnerChannel);
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal middleValue, decimal upperValue, decimal lowerValue)
        {
            // Skip unfinished candles
            if (candle.State != CandleStates.Finished)
                return;

            // Check if strategy is ready to trade
            if (!IsFormedAndOnlineAndAllowTrading())
                return;

            // Skip the first received value for proper comparison
            if (_prevUpperBand == 0)
            {
                _prevClosePrice = candle.ClosePrice;
                _prevUpperBand = upperValue;
                _prevLowerBand = lowerValue;
                _prevEma = middleValue;
                return;
            }

            // Check for breakouts
            var isUpperBreakout = candle.ClosePrice > _prevUpperBand && _prevClosePrice <= _prevUpperBand;
            var isLowerBreakout = candle.ClosePrice < _prevLowerBand && _prevClosePrice >= _prevLowerBand;

            // Check for exit conditions
            var shouldExitLong = candle.ClosePrice < _prevEma && Position > 0;
            var shouldExitShort = candle.ClosePrice > _prevEma && Position < 0;

            // Entry logic
            if (isUpperBreakout && Position <= 0)
            {
                var volume = Volume + Math.Abs(Position);
                BuyMarket(volume);
                LogInfo($"Buy signal: Price {candle.ClosePrice} broke above upper band {_prevUpperBand}");
            }
            else if (isLowerBreakout && Position >= 0)
            {
                var volume = Volume + Math.Abs(Position);
                SellMarket(volume);
                LogInfo($"Sell signal: Price {candle.ClosePrice} broke below lower band {_prevLowerBand}");
            }
            // Exit logic
            else if (shouldExitLong)
            {
                SellMarket(Position);
                LogInfo($"Exit long: Price {candle.ClosePrice} dropped below EMA {_prevEma}");
            }
            else if (shouldExitShort)
            {
                BuyMarket(Math.Abs(Position));
                LogInfo($"Exit short: Price {candle.ClosePrice} rose above EMA {_prevEma}");
            }

            // Update previous values
            _prevClosePrice = candle.ClosePrice;
            _prevUpperBand = upperValue;
            _prevLowerBand = lowerValue;
            _prevEma = middleValue;
        }
    }
}