using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
    /// <summary>
    /// Strategy based on Donchian Channel.
    /// It enters long position when price breaks through the upper band and short position when price breaks through the lower band.
    /// </summary>
    public class DonchianChannelStrategy : Strategy
    {
        private readonly StrategyParam<int> _channelPeriod;
        private readonly StrategyParam<DataType> _candleType;

        // Current state
        private decimal _prevClosePrice;
        private decimal _prevUpperBand;
        private decimal _prevLowerBand;

        /// <summary>
        /// Period for Donchian Channel.
        /// </summary>
        public int ChannelPeriod
        {
            get => _channelPeriod.Value;
            set => _channelPeriod.Value = value;
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
        /// Initialize the Donchian Channel strategy.
        /// </summary>
        public DonchianChannelStrategy()
        {
            _channelPeriod = Param(nameof(ChannelPeriod), 20)
                .SetDisplay("Channel Period", "Period for Donchian Channel calculation", "Indicators")
                .SetCanOptimize(true)
                .SetOptimize(10, 50, 5);

            _candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
                .SetDisplay("Candle Type", "Type of candles to use", "General");
                
            _prevClosePrice = 0;
            _prevUpperBand = 0;
            _prevLowerBand = 0;
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
            var donchian = new DonchianChannel { Length = ChannelPeriod };

            // Create subscription and bind indicators
            var subscription = SubscribeCandles(CandleType);
            subscription
                .Bind(donchian, ProcessCandle)
                .Start();

            // Setup chart visualization if available
            var area = CreateChartArea();
            if (area != null)
            {
                DrawCandles(area, subscription);
                DrawIndicator(area, donchian);
                DrawOwnTrades(area);
            }
        }

        private void ProcessCandle(ICandleMessage candle, decimal midValue, decimal upperValue, decimal lowerValue)
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
                return;
            }

            // Check for breakouts
            var isUpperBreakout = candle.ClosePrice > _prevUpperBand && _prevClosePrice <= _prevUpperBand;
            var isLowerBreakout = candle.ClosePrice < _prevLowerBand && _prevClosePrice >= _prevLowerBand;

            // Check for exit conditions
            var shouldExitLong = candle.ClosePrice < midValue && Position > 0;
            var shouldExitShort = candle.ClosePrice > midValue && Position < 0;

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
                LogInfo($"Exit long: Price {candle.ClosePrice} dropped below middle line {midValue}");
            }
            else if (shouldExitShort)
            {
                BuyMarket(Math.Abs(Position));
                LogInfo($"Exit short: Price {candle.ClosePrice} rose above middle line {midValue}");
            }

            // Update previous values
            _prevClosePrice = candle.ClosePrice;
            _prevUpperBand = upperValue;
            _prevLowerBand = lowerValue;
        }
    }
}